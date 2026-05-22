namespace rEFIndConfigEditor.Config;

internal static class RefindLogPicker
{
    public static string? LogPathBesideConf(string? confPath)
    {
        if (string.IsNullOrEmpty(confPath))
            return null;
        var dir = Path.GetDirectoryName(confPath);
        if (string.IsNullOrEmpty(dir))
            return null;
        var log = Path.Combine(dir, "refind.log");
        return File.Exists(log) ? log : null;
    }

    public static string? PickLogPath(string? confPath, IWin32Window owner)
    {
        var beside = LogPathBesideConf(confPath);
        if (beside is not null)
            return beside;

        using var dlg = new OpenFileDialog
        {
            Filter = "rEFInd log (refind.log)|refind.log|Log files (*.log)|*.log|All files (*.*)|*.*",
            Title = "Open refind.log",
            FileName = "refind.log"
        };
        if (!string.IsNullOrEmpty(confPath))
        {
            var dir = Path.GetDirectoryName(confPath);
            if (!string.IsNullOrEmpty(dir))
                dlg.InitialDirectory = dir;
        }

        return dlg.ShowDialog(owner) == DialogResult.OK ? dlg.FileName : null;
    }
}
