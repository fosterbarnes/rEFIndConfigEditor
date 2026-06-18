namespace rEFIndConfigEditor.Storage;

public static class AtomicFile
{
    public static void WriteAllBytes(string path, byte[] contents)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var temp = path + "." + Path.GetRandomFileName() + ".tmp";
        var backup = path + ".bak";
        try
        {
            File.WriteAllBytes(temp, contents);
            if (File.Exists(path))
            {
                File.Replace(temp, path, backup);
                FileDeleteHelper.TryDeleteFile(backup);
                return;
            }

            File.Move(temp, path);
        }
        catch
        {
            FileDeleteHelper.TryDeleteFile(temp);
            throw;
        }
    }
}
