using System.Diagnostics;
using rEFIndConfigEditor;
using rEFIndConfigEditor.Storage;

namespace rEFIndConfigEditor.Update;

public static class UpdateInstallRunner
{
    public static async Task RunAsync(
        string installDirectory,
        GitHubRelease? release,
        IProgress<(float Progress, string Status)>? progress,
        CancellationToken cancellationToken,
        int? hostProcessId = null)
    {
        await EnsureHostExitedAsync(hostProcessId, progress, cancellationToken).ConfigureAwait(false);

        release ??= await GitHubReleaseService.FetchLatestReleaseAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("No published releases were found.");

        var zipPath = await ReleaseDownloadService
            .DownloadPortableZipAsync(release, progress, cancellationToken)
            .ConfigureAwait(false);

        var keepZip = false;
        try
        {
            await ApplyUpdateService
                .ApplyPortableZipAsync(installDirectory, zipPath, progress, cancellationToken)
                .ConfigureAwait(false);

            try
            {
                PostUpdateUpdaterRefresh.WritePendingRefresh(installDirectory, zipPath);
                keepZip = true;
            }
            catch (Exception ex)
            {
                UpdaterLogger.Write($"Failed to write pending updater refresh marker: {ex}");
            }
        }
        finally
        {
            if (!keepZip)
                FileDeleteHelper.TryDeleteFile(zipPath);
        }
    }

    private static async Task EnsureHostExitedAsync(
        int? hostProcessId,
        IProgress<(float Progress, string Status)>? progress,
        CancellationToken cancellationToken)
    {
        if (hostProcessId is not int pid || pid <= 0)
            return;

        Process host;
        try
        {
            host = Process.GetProcessById(pid);
        }
        catch (ArgumentException)
        {
            return;
        }

        using (host)
        {
            if (host.HasExited)
                return;

            progress?.Report((0f, $"Waiting for {AppBranding.DisplayName} to close..."));

            try
            {
                if (host.MainWindowHandle != IntPtr.Zero)
                    host.CloseMainWindow();
            }
            catch
            {
            }

            await host.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
