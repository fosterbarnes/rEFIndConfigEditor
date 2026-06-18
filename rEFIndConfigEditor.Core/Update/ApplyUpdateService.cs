using System.IO.Compression;
using rEFIndConfigEditor;
using rEFIndConfigEditor.Platform;
using rEFIndConfigEditor.Storage;

namespace rEFIndConfigEditor.Update;

public static class ApplyUpdateService
{
    public static async Task ApplyPortableZipAsync(
        string installDirectory,
        string zipPath,
        IProgress<(float Progress, string Status)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var installRoot = Path.GetFullPath(installDirectory);
        var extractRoot = Path.Combine(Path.GetTempPath(), $"{AppBranding.TempUpdateFolderPrefix}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(extractRoot);
        var backups = new List<(string Destination, string Backup)>();

        try
        {
            progress?.Report((0.05f, "Extracting update..."));
            await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, extractRoot, overwriteFiles: true), cancellationToken)
                .ConfigureAwait(false);

            var files = Directory.GetFiles(extractRoot, "*", SearchOption.AllDirectories);
            if (files.Length == 0)
                throw new InvalidOperationException("The update archive was empty.");

            for (var i = 0; i < files.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sourcePath = files[i];
                var relativePath = Path.GetRelativePath(extractRoot, sourcePath);
                if (PlatformBinaryNames.IsUpdaterHostFileName(Path.GetFileName(relativePath)))
                    continue;

                relativePath = RemapInstallRelativePath(relativePath);
                var destinationPath = Path.GetFullPath(Path.Combine(installRoot, relativePath));
                if (!IsContainedIn(destinationPath, installRoot))
                    throw new InvalidOperationException(
                        $"Update archive entry escapes the install directory: {relativePath}");

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                BackupIfExists(destinationPath, backups);
                File.Copy(sourcePath, destinationPath, overwrite: true);
                EnsureExecutable(destinationPath);

                var fraction = 0.1f + (0.85f * ((i + 1) / (float)files.Length));
                progress?.Report((fraction, $"Installing {relativePath}..."));
            }

            CommitBackups(backups);
            progress?.Report((1f, "Update complete!"));
        }
        catch
        {
            RollbackBackups(backups);
            throw;
        }
        finally
        {
            FileDeleteHelper.TryDeleteDirectory(extractRoot);
        }
    }

    private static string RemapInstallRelativePath(string relativePath)
    {
        if (!OperatingSystem.IsMacOS())
            return relativePath;

        var normalized = relativePath.Replace('\\', '/');
        const string macOsMarker = "/Contents/MacOS/";
        var markerIndex = normalized.IndexOf(macOsMarker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
            return relativePath;

        var appPath = normalized[..markerIndex];
        if (!appPath.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
            return relativePath;

        var remapped = normalized[(markerIndex + macOsMarker.Length)..];
        return remapped.Replace('/', Path.DirectorySeparatorChar);
    }

    private static bool IsContainedIn(string fullPath, string rootFullPath)
    {
        var normalizedRoot = rootFullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static void BackupIfExists(string destinationPath, List<(string Destination, string Backup)> backups)
    {
        if (!File.Exists(destinationPath))
            return;

        var backupPath = $"{destinationPath}.bak.{Guid.NewGuid():N}";
        File.Move(destinationPath, backupPath);
        backups.Add((destinationPath, backupPath));
    }

    private static void CommitBackups(List<(string Destination, string Backup)> backups)
    {
        foreach (var (_, backup) in backups)
        {
            try
            {
                if (File.Exists(backup))
                    File.Delete(backup);
            }
            catch (Exception ex)
            {
                UpdaterLogger.Write($"Failed to remove backup '{backup}': {ex.Message}");
            }
        }
        backups.Clear();
    }

    private static void RollbackBackups(List<(string Destination, string Backup)> backups)
    {
        for (var i = backups.Count - 1; i >= 0; i--)
        {
            var (destination, backup) = backups[i];
            try
            {
                if (File.Exists(destination))
                    File.Delete(destination);
                if (File.Exists(backup))
                    File.Move(backup, destination);
            }
            catch (Exception ex)
            {
                UpdaterLogger.Write($"Rollback failed for '{destination}': {ex.Message}");
            }
        }
        backups.Clear();
    }

    internal static void EnsureExecutable(string filePath)
    {
        if (OperatingSystem.IsWindows())
            return;

        var name = Path.GetFileName(filePath);
        if (!PlatformBinaryNames.IsAppHostFileName(name) && !PlatformBinaryNames.IsUpdaterHostFileName(name))
            return;

        var mode = File.GetUnixFileMode(filePath);
        mode |= UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
        File.SetUnixFileMode(filePath, mode);
    }
}
