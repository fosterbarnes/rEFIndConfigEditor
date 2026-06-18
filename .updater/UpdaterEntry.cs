using rEFIndConfigEditor;
using rEFIndConfigEditor.Update;
using System.Reflection;

namespace rEFIndConfigEditor.Updater;

internal static class UpdaterEntry
{
    public static int? TryRunSilent(string[] args)
    {
        TryParseArgs(args, out var installDirectory, out var installMode, out var silentMode, out _, out var hostProcessId);

        if (!silentMode)
            return null;

        installDirectory = ResolveInstallDirectory(installDirectory);
        if (installDirectory is null)
        {
            UpdaterLogger.Write("Silent mode: install folder resolution failed.");
            return SilentUpdateRunner.ExitInstallFailed;
        }

        if (!installMode)
        {
            UpdaterLogger.Write("Silent mode requires --install.");
            return SilentUpdateRunner.ExitInstallFailed;
        }

        return SilentUpdateRunner.RunAsync(installDirectory, hostProcessId).GetAwaiter().GetResult();
    }

    public static UpdaterLaunchContext CreateContext(string[] args)
    {
        TryParseArgs(args, out var installDirectory, out var installMode, out var silentMode, out var forceUpdate, out var hostProcessId);

        installDirectory = ResolveInstallDirectory(installDirectory);
        if (installDirectory is null)
        {
            var message =
                $"Could not determine the {AppBranding.DisplayName} install folder.\n\n"
                + "Run updater from your install or portable folder, or pass the folder as an argument.";
            UpdaterLogger.Write(message);
            throw new InvalidOperationException(message);
        }

        var version = typeof(UpdaterEntry).Assembly
            .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?";

        return new UpdaterLaunchContext
        {
            InstallDirectory = installDirectory,
            InstallMode = installMode,
            ForceUpdate = forceUpdate,
            HostProcessId = hostProcessId,
            UpdaterVersion = version,
        };
    }

    private static void TryParseArgs(
        IReadOnlyList<string> args,
        out string installDirectory,
        out bool installMode,
        out bool silentMode,
        out bool forceUpdate,
        out int? hostProcessId)
    {
        installDirectory = string.Empty;
        installMode = false;
        silentMode = false;
        forceUpdate = false;
        hostProcessId = null;

        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            if (string.Equals(arg, "--install", StringComparison.OrdinalIgnoreCase)) { installMode = true; continue; }
            if (string.Equals(arg, "--silent", StringComparison.OrdinalIgnoreCase)) { silentMode = true; continue; }
            if (string.Equals(arg, "--force", StringComparison.OrdinalIgnoreCase)) { forceUpdate = true; continue; }
            if (string.Equals(arg, "--host-pid", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Count)
            {
                if (int.TryParse(args[++i], out var pid) && pid > 0)
                    hostProcessId = pid;
                continue;
            }
            if (arg.StartsWith('-'))
                continue;
            installDirectory = SanitizePathArgument(arg);
        }
    }

    private static string? ResolveInstallDirectory(string installDirectory)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(installDirectory))
        {
            foreach (var candidate in ExpandInstallDirectoryCandidates(installDirectory))
            {
                if (seen.Add(candidate) && IsInstallDirectory(candidate))
                    return candidate;
            }
        }

        var exeDirectory = AppContext.BaseDirectory;
        if (string.IsNullOrWhiteSpace(exeDirectory))
            return null;

        foreach (var candidate in ExpandInstallDirectoryCandidates(exeDirectory))
        {
            if (seen.Add(candidate) && IsInstallDirectory(candidate))
                return candidate;
        }

        return null;
    }

    private static IEnumerable<string> ExpandInstallDirectoryCandidates(string path)
    {
        path = SanitizePathArgument(path);
        if (string.IsNullOrWhiteSpace(path))
            yield break;

        string fullPath;
        try { fullPath = Path.GetFullPath(path); }
        catch { yield break; }

        yield return fullPath;

        if (File.Exists(fullPath))
        {
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
                yield return Path.GetFullPath(directory);
        }
    }

    private static string SanitizePathArgument(string path) =>
        path.Trim().Trim('"').TrimEnd('\\', '/');

    private static bool IsInstallDirectory(string path) =>
        File.Exists(Path.Combine(path, UpdateConstants.AppExeName));
}
