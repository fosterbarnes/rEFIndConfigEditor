using rEFIndConfigEditor;

namespace rEFIndConfigEditor.Update;

public static class SilentUpdateRunner
{
    public const int ExitSuccess = 0;
    public const int ExitInstallFailed = 1;
    public const int ExitLaunchFailed = 2;

    public static async Task<int> RunAsync(
        string installDirectory,
        int? hostProcessId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var progress = new Progress<(float Progress, string Status)>(report =>
                UpdaterLogger.Write($"{report.Status} ({Math.Clamp((int)(report.Progress * 100f), 0, 100)}%)"));

            await UpdateInstallRunner
                .RunAsync(installDirectory, release: null, progress, cancellationToken, hostProcessId)
                .ConfigureAwait(false);

            UpdaterLogger.Write("Silent update completed. Launching app...");
            var process = ProcessLaunchService.LaunchApp(installDirectory);
            if (process is null)
            {
                UpdaterLogger.Write("Silent update completed but app launch returned no process.");
                return ExitLaunchFailed;
            }

            ProcessLaunchService.WaitForAppWindowReady(process);
            return ExitSuccess;
        }
        catch (OperationCanceledException)
        {
            UpdaterLogger.Write("Silent update cancelled.");
            return ExitInstallFailed;
        }
        catch (Exception ex)
        {
            UpdaterLogger.Write($"Silent update failed: {ex}");
            return ExitInstallFailed;
        }
    }
}
