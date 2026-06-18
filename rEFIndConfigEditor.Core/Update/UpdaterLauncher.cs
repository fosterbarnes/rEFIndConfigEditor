using System.Diagnostics;
using rEFIndConfigEditor.Platform;

namespace rEFIndConfigEditor.Update;

public static class UpdaterLauncher
{
    public static async Task StartInteractiveAsync(
        string installDirectory,
        CancellationToken cancellationToken = default)
    {
        installDirectory = Path.GetFullPath(installDirectory);
        await TryRefreshUpdaterAsync(installDirectory, cancellationToken).ConfigureAwait(false);

        var updaterPath = ResolveUpdaterPath(installDirectory);
        if (updaterPath is null)
        {
            PlatformServices.Current.ShowWarning(
                AppBranding.DisplayName,
                $"{UpdateConstants.UpdaterExeName} was not found in:\n{installDirectory}\n\n"
                + "Build the updater first (.scripts/buildUpdater.ps1 or .buildAll.ps1).");
            return;
        }

        try
        {
            StartUpdater(updaterPath, installDirectory, installMode: false);
        }
        catch (Exception ex)
        {
            UpdaterLogger.Write($"Interactive updater launch failed: {ex}");
            PlatformServices.Current.ShowWarning(
                AppBranding.DisplayName,
                $"Could not start the updater:\n{ex.Message}");
        }
    }

    public static async Task LaunchInstallAsync(
        string installDirectory,
        bool silent = false,
        CancellationToken cancellationToken = default)
    {
        installDirectory = Path.GetFullPath(installDirectory);
        await TryRefreshUpdaterAsync(installDirectory, cancellationToken).ConfigureAwait(false);

        var updaterPath = ResolveUpdaterPath(installDirectory)
            ?? throw new FileNotFoundException(
                $"{UpdateConstants.UpdaterExeName} was not found. Build the updater first (.scripts/buildUpdater.ps1 or .buildAll.ps1).",
                installDirectory);

        StartUpdater(updaterPath, installDirectory, installMode: true, silent);
    }

    private static async Task TryRefreshUpdaterAsync(
        string installDirectory,
        CancellationToken cancellationToken)
    {
        try
        {
            var check = await UpdateCheckService.CheckAsync(installDirectory, cancellationToken).ConfigureAwait(false);
            if (check.Release is null)
                return;

            var updaterVersion = InstallVersionReader.ReadUpdaterVersion(installDirectory);
            if (!VersionComparer.IsOutdated(updaterVersion, check.Release.Version))
                return;

            await ReleaseDownloadService.RefreshUpdaterExecutableAsync(
                    installDirectory,
                    check.Release,
                    cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            UpdaterLogger.Write($"Updater refresh failed: {ex}");
        }
    }

    private static string? ResolveUpdaterPath(string installDirectory)
    {
        var direct = Path.Combine(installDirectory, UpdateConstants.UpdaterExeName);
        if (File.Exists(direct))
            return direct;

        foreach (var candidate in GetFallbackPaths(installDirectory))
        {
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    private static IEnumerable<string> GetFallbackPaths(string installDirectory)
    {
        var arch = ArchitectureHelper.GetCurrentArchitecture();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var dir = new DirectoryInfo(installDirectory);

        while (dir is not null)
        {
            var publishRoot = Path.Combine(dir.FullName, "publish");
            if (Directory.Exists(publishRoot))
            {
                foreach (var platform in GetPublishFolderNames(arch))
                {
                    var candidate = Path.Combine(publishRoot, platform, UpdateConstants.UpdaterExeName);
                    if (seen.Add(candidate))
                        yield return candidate;
                }
            }

            if (File.Exists(Path.Combine(dir.FullName, AppBranding.SolutionFileName)))
                break;

            dir = dir.Parent;
        }
    }

    private static IEnumerable<string> GetPublishFolderNames(string arch)
    {
        if (OperatingSystem.IsMacOS())
        {
            yield return $"osx-{arch}";
            yield return "osx-arm64";
            yield return "osx-x64";
            yield break;
        }

        yield return arch;
        yield return "x64";
        yield return "arm64";
        yield return "x86";
    }

    private static void StartUpdater(string updaterPath, string installDirectory, bool installMode, bool silent = false)
    {
        installDirectory = SanitizeInstallDirectory(installDirectory);

        var startInfo = new ProcessStartInfo
        {
            FileName = updaterPath,
            WorkingDirectory = installDirectory,
            UseShellExecute = false,
        };

        startInfo.ArgumentList.Add(installDirectory);
        startInfo.ArgumentList.Add("--host-pid");
        startInfo.ArgumentList.Add(Environment.ProcessId.ToString());
        if (installMode)
        {
            startInfo.ArgumentList.Add("--install");
            if (silent)
                startInfo.ArgumentList.Add("--silent");
        }

        Process? process;
        try
        {
            process = Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Could not start {UpdateConstants.UpdaterExeName}: {ex.Message}", ex);
        }

        if (process is null)
            throw new InvalidOperationException($"Could not start {UpdateConstants.UpdaterExeName}.");
    }

    private static string SanitizeInstallDirectory(string installDirectory)
    {
        installDirectory = installDirectory.Trim().Trim('"').TrimEnd('\\', '/');
        return Path.GetFullPath(installDirectory);
    }
}
