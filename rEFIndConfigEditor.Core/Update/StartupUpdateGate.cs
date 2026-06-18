using System.Net.Http;
using System.Net.Sockets;
using rEFIndConfigEditor.Diagnostics;
using rEFIndConfigEditor.Models;

namespace rEFIndConfigEditor.Update;

public enum StartupUpdateGateResult
{
    ProceedToMainUi,
    ExitWithoutMainUi,
}

public static class StartupUpdateGate
{
    public static Task<StartupUpdateGateResult> EvaluateAsync(
        string installDirectory,
        UiPreferences prefs,
        CancellationToken cancellationToken = default) =>
        EvaluateCoreAsync(installDirectory, prefs, cancellationToken);

    // Pre-main-window gate: silent auto-install only. Check-for-updates alone must never await network here.
    private static async Task<StartupUpdateGateResult> EvaluateCoreAsync(
        string installDirectory,
        UiPreferences prefs,
        CancellationToken cancellationToken)
    {
        if (!prefs.CheckForUpdates || !prefs.AutomaticallyInstallUpdates)
        {
            if (DebugLog.Enabled)
                DebugLog.Write("Update", "Automatic startup update skipped");
            return StartupUpdateGateResult.ProceedToMainUi;
        }

        if (DebugLog.Enabled)
            DebugLog.Write("Update", "Automatic startup update check started");

        try
        {
            var result = await UpdateCheckService.CheckAsync(installDirectory, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                UpdaterLogger.Write($"Pre-launch update check failed: {result.ErrorMessage}");
                if (DebugLog.Enabled)
                    DebugLog.Write("Update", $"Pre-launch update check failed: {result.ErrorMessage}");
                StartupUpdateState.SetPendingStatusMessage(
                    FormatUpdateFailureMessage(result.ErrorMessage));
                return StartupUpdateGateResult.ProceedToMainUi;
            }

            if (!result.IsOutdated)
            {
                if (DebugLog.Enabled)
                    DebugLog.Write("Update", "Pre-launch update check: already up to date");
                return StartupUpdateGateResult.ProceedToMainUi;
            }

            if (DebugLog.Enabled)
                DebugLog.Write("Update", $"Pre-launch automatic update starting for v{result.LatestVersion}");

            await UpdaterLauncher.LaunchInstallAsync(installDirectory, silent: true, cancellationToken)
                .ConfigureAwait(false);

            if (DebugLog.Enabled)
                DebugLog.Write("Update", "Pre-launch automatic update completed; exiting for install");

            return StartupUpdateGateResult.ExitWithoutMainUi;
        }
        catch (Exception ex)
        {
            UpdaterLogger.Write($"Pre-launch automatic update failed: {ex}");
            if (DebugLog.Enabled)
                DebugLog.Write("Update", $"Pre-launch automatic update failed: {ex}");
            StartupUpdateState.SetPendingStatusMessage(FormatUpdateFailureMessage(ex.Message, ex));
            return StartupUpdateGateResult.ProceedToMainUi;
        }
    }

    private static string FormatUpdateFailureMessage(string? detail, Exception? ex = null)
    {
        if (IsNetworkError(detail, ex))
            return "Update failed: no internet connection";

        var text = detail ?? ex?.Message ?? "Unknown error";
        if (text.Contains("updater", StringComparison.OrdinalIgnoreCase)
            && text.Contains("not found", StringComparison.OrdinalIgnoreCase))
            return "Update failed: updater not found";

        return $"Update failed: {Shorten(text)}";
    }

    private static bool IsNetworkError(string? detail, Exception? ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is HttpRequestException or SocketException)
                return true;

            if (current is TaskCanceledException && current.InnerException is HttpRequestException or SocketException)
                return true;
        }

        if (ex is TaskCanceledException)
            return false;

        if (string.IsNullOrWhiteSpace(detail))
            return false;

        return detail.Contains("network", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("connection", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("internet", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("timed out", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("host", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("resolve", StringComparison.OrdinalIgnoreCase);
    }

    private static string Shorten(string text)
    {
        const int maxLength = 80;
        var trimmed = text.Trim();
        if (trimmed.Length <= maxLength)
            return trimmed;

        return trimmed[..(maxLength - 3)] + "...";
    }
}
