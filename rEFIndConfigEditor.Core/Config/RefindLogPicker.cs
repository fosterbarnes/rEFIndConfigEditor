namespace rEFIndConfigEditor.Config;

public static class RefindLogPicker
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
}
