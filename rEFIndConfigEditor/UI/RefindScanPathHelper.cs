using rEFIndConfigEditor.Config;

namespace rEFIndConfigEditor.UI;

internal static class RefindScanPathHelper
{
    public static bool IsFolderListToken(string token) => RefindWriter.IsFolderListToken(token);

    public static string FormatPickedFolder(string absoluteFolder, string? refindConfPath)
    {
        var full = Path.GetFullPath(absoluteFolder);
        var root = Path.GetPathRoot(full);
        if (!string.IsNullOrEmpty(root))
        {
            var rel = Path.GetRelativePath(root, full);
            if (!rel.StartsWith("..", StringComparison.Ordinal))
                return RefindPathHelper.NormalizeSlashes(rel.TrimEnd('/', '\\'));
        }

        var underRefind = RefindPathHelper.ToRefindRelative(refindConfPath, full);
        if (underRefind is not null)
            return underRefind;

        return RefindPathHelper.NormalizeSlashes(full);
    }

    public static string AppendToCommaList(string currentText, string newItem)
    {
        var items = RefindWriter.ParseFolderListFromEditor(currentText);
        if (items.Any(v => string.Equals(v, newItem, StringComparison.OrdinalIgnoreCase)))
            return currentText;

        items.Add(newItem);
        return RefindWriter.JoinFolderListForEditor(items);
    }
}
