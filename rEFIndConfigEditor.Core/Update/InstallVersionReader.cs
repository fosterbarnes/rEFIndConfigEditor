using System.Diagnostics;
using System.Reflection;

namespace rEFIndConfigEditor.Update;

public static class InstallVersionReader
{
    public static string ReadInstalledVersion(string installDirectory)
    {
        var versionPath = Path.Combine(installDirectory, UpdateConstants.VersionFileName);
        if (File.Exists(versionPath))
        {
            var text = File.ReadAllText(versionPath).Trim();
            if (!string.IsNullOrWhiteSpace(text))
                return GitHubRelease.NormalizeTag(text);
        }

        var fromExe = ReadExeVersion(Path.Combine(installDirectory, UpdateConstants.AppExeName));
        if (fromExe != "0.0.0")
            return fromExe;

        try
        {
            var current = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(current))
                return GitHubRelease.NormalizeTag(current.Split('+')[0]);
        }
        catch
        {
        }

        return "0.0.0";
    }

    public static string ReadUpdaterVersion(string installDirectory) =>
        ReadExeVersion(Path.Combine(installDirectory, UpdateConstants.UpdaterExeName));

    private static string ReadExeVersion(string exePath)
    {
        if (!File.Exists(exePath))
            return "0.0.0";

        var info = FileVersionInfo.GetVersionInfo(exePath);
        var product = info.ProductVersion?.Trim();
        if (!string.IsNullOrWhiteSpace(product))
            return GitHubRelease.NormalizeTag(product.Split('+')[0]);

        try
        {
            var asmName = AssemblyName.GetAssemblyName(exePath);
            var informational = asmName.Version?.ToString();
            if (!string.IsNullOrWhiteSpace(informational))
                return GitHubRelease.NormalizeTag(informational);
        }
        catch
        {
        }

        return "0.0.0";
    }
}
