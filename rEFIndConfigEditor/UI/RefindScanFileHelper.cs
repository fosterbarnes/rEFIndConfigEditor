using rEFIndConfigEditor.Config;

namespace rEFIndConfigEditor.UI;

internal static class RefindScanFileHelper
{
    public static bool IsFileListToken(string token) => RefindTokens.IsFileListToken(token);

    public static string FormatPickedFile(string absoluteFile, string? refindConfPath)
    {
        var full = Path.GetFullPath(absoluteFile);
        var root = Path.GetPathRoot(full);
        if (!string.IsNullOrEmpty(root))
        {
            var rel = Path.GetRelativePath(root, full);
            if (!rel.StartsWith("..", StringComparison.Ordinal))
                return RefindPathHelper.NormalizeSlashes(rel);
        }

        var underRefind = RefindPathHelper.ToRefindRelative(refindConfPath, full);
        if (underRefind is not null)
            return underRefind;

        return Path.GetFileName(full);
    }

    public static string AppendFilesToCommaList(string currentText, IEnumerable<string> newItems)
    {
        var text = currentText;
        foreach (var item in newItems)
            text = RefindWriter.AppendCommaList(text, [item]);
        return text;
    }
}
