using rEFIndConfigEditor.Diagnostics;
using rEFIndConfigEditor;
using rEFIndConfigEditor.Storage;

namespace rEFIndConfigEditor.Update;

public static class PostUpdateUpdaterRefresh
{
    public static void WritePendingRefresh(string installDirectory, string zipPath)
    {
        var markerPath = GetMarkerPath(installDirectory);
        File.WriteAllText(markerPath, zipPath);
    }

    public static void TryApplyPendingRefresh(string installDirectory)
    {
        var markerPath = GetMarkerPath(installDirectory);
        if (!File.Exists(markerPath))
            return;

        if (DebugLog.Enabled)
            DebugLog.Write("Update", "Pending updater refresh found");

        string zipPath;
        try
        {
            zipPath = File.ReadAllText(markerPath).Trim();
        }
        catch (Exception ex)
        {
            UpdaterLogger.Write($"Pending updater refresh marker read failed: {ex}");
            if (DebugLog.Enabled)
                DebugLog.Write("Update", $"Pending updater refresh marker read failed: {ex}");
            return;
        }

        if (string.IsNullOrWhiteSpace(zipPath))
        {
            FileDeleteHelper.TryDeleteFile(markerPath);
            return;
        }

        if (!File.Exists(zipPath))
        {
            UpdaterLogger.Write($"Pending updater refresh zip not found: {zipPath}");
            if (DebugLog.Enabled)
                DebugLog.Write("Update", $"Pending updater refresh zip not found: {zipPath}");
            FileDeleteHelper.TryDeleteFile(markerPath);
            return;
        }

        try
        {
            ReleaseDownloadService.ExtractUpdaterFromZip(zipPath, installDirectory);
            FileDeleteHelper.TryDeleteFile(zipPath);
            FileDeleteHelper.TryDeleteFile(markerPath);
            if (DebugLog.Enabled)
                DebugLog.Write("Update", "Pending updater refresh applied");
        }
        catch (Exception ex)
        {
            UpdaterLogger.Write($"Pending updater refresh failed: {ex}");
            if (DebugLog.Enabled)
                DebugLog.Write("Update", $"Pending updater refresh failed: {ex}");
        }
    }

    private static string GetMarkerPath(string installDirectory) =>
        Path.Combine(installDirectory, UpdateConstants.PendingUpdaterRefreshFileName);
}
