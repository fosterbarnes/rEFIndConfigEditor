using rEFIndConfigEditor.Config;
using rEFIndConfigEditor.Forms;

namespace rEFIndConfigEditor.UI;

internal static class LogPickerController
{
    public delegate IReadOnlyList<string> Collector(string? logText, IEnumerable<MenuEntry> menuEntries);

    public sealed class ListPicker
    {
        public required string Token { get; init; }
        public required Collector Collect { get; init; }
        public required string DialogTitle { get; init; }
        public required string Subtitle { get; init; }
        public required string PickPrompt { get; init; }
        public required string EmptyMessage { get; init; }
    }

    private static readonly Dictionary<string, ListPicker> Pickers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dont_scan_volumes"] = new()
        {
            Token = "dont_scan_volumes",
            Collect = RefindLogParser.CollectVolumes,
            DialogTitle = "Choose volumes from refind.log",
            Subtitle = "Volumes from config menuentry lines and from the log scan. Select one or more to add:",
            PickPrompt = "Select at least one volume.",
            EmptyMessage = "No volumes found in the config or log."
        },
        ["dont_scan_firmware"] = new()
        {
            Token = "dont_scan_firmware",
            Collect = RefindLogParser.CollectFirmwareBootNames,
            DialogTitle = "Choose firmware boot entries from refind.log",
            Subtitle = "Names from firmware NVRAM scan (Adding EFI loader entry…) and menuentry titles with firmware_bootnum. Select one or more:",
            PickPrompt = "Select at least one firmware boot entry.",
            EmptyMessage = "No firmware boot entries found. Enable scanfor firmware, set log_level to at least 1, reboot, then open refind.log."
        },
        ["windows_recovery_files"] = new()
        {
            Token = "windows_recovery_files",
            Collect = RefindLogParser.CollectWindowsRecoveryPaths,
            DialogTitle = "Choose Windows recovery file paths",
            Subtitle = "Each entry is a path to a recovery .efi (optionally VOL:path). From “Adding Windows Recovery tag…” lines or scanned loaders. Select one or more:",
            PickPrompt = "Select at least one recovery file path.",
            EmptyMessage = "No Windows recovery .efi paths found in the log or config.\n\n" +
                           "This option needs file paths (e.g. LRS_ESP:EFI/Microsoft/Boot/LrsBootmgr.efi), not volume labels.\n" +
                           "Include windows_recovery in showtools, set log_level ≥ 1, reboot, then try again."
        }
    };

    public static bool TryGet(string token, out ListPicker picker) =>
        Pickers.TryGetValue(token, out picker!);

    public static IReadOnlyList<string> PickItems(
        Form owner,
        ListPicker picker,
        string? configFilePath,
        IEnumerable<MenuEntry> menuEntries,
        Func<Form, DialogResult> showDialog)
    {
        var entries = menuEntries.ToList();
        string? logText = null;

        var logPath = RefindLogPicker.PickLogPath(configFilePath, owner);
        if (logPath is not null)
        {
            try { logText = RefindLogIO.ReadText(logPath); }
            catch (Exception ex)
            {
                if (picker.Collect(null, entries).Count == 0)
                {
                    MessageBox.Show(owner, ex.Message, owner.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return [];
                }
            }
        }
        else if (picker.Collect(null, entries).Count == 0)
            return [];

        var items = picker.Collect(logText, entries);
        if (items.Count == 0)
        {
            MessageBox.Show(owner, picker.EmptyMessage, owner.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return [];
        }

        using var dlg = new RefindLogImportForm(items, picker.DialogTitle, picker.Subtitle, pickPrompt: picker.PickPrompt);
        return showDialog(dlg) == DialogResult.OK ? dlg.SelectedVolumes : [];
    }
}
