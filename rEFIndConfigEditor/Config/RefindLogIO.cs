using System.Text;
using rEFIndConfigEditor.Storage;

namespace rEFIndConfigEditor.Config;

internal static class RefindLogIO
{
    public static string ReadText(string path)
    {
        SafeFileIO.EnsureWithinSize(path, SafeFileIO.MaxLogBytes);
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Encoding.Unicode.GetString(bytes);
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(bytes);
        if (LooksLikeUtf16LeText(bytes))
            return Encoding.Unicode.GetString(bytes);
        return Encoding.UTF8.GetString(bytes);
    }

    private static bool LooksLikeUtf16LeText(byte[] bytes)
    {
        if (bytes.Length < 4 || bytes.Length % 2 != 0)
            return false;

        var nullPairs = 0;
        var checkedLen = Math.Min(bytes.Length, 256);
        for (var i = 1; i < checkedLen; i += 2)
        {
            if (bytes[i] == 0)
                nullPairs++;
        }

        return nullPairs > checkedLen / 4;
    }
}
