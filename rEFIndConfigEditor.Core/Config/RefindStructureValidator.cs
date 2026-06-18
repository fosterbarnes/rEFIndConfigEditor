namespace rEFIndConfigEditor.Config;

public static class RefindStructureValidator
{
    public static bool TryValidate(string text, out string message)
    {
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var blockStack = new Stack<(string Keyword, int LineNumber)>();

        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                continue;

            if (TryParseBlockOpen(trimmed, out var keyword, out var consumedLine))
            {
                blockStack.Push((keyword, i + 1));
                if (consumedLine)
                    continue;
            }

            if (trimmed == "}")
            {
                if (blockStack.Count == 0)
                {
                    message = $"Unexpected '}}' at line {i + 1}.";
                    return false;
                }

                blockStack.Pop();
            }
        }

        if (blockStack.Count > 0)
        {
            var (keyword, line) = blockStack.Peek();
            message = keyword switch
            {
                "menuentry" => $"Unclosed menuentry block starting at line {line}.",
                "submenuentry" => $"Unclosed submenuentry block starting at line {line}.",
                _ => $"Unclosed block starting at line {line}."
            };
            return false;
        }

        message = "";
        return true;
    }

    private static bool TryParseBlockOpen(string trimmed, out string keyword, out bool consumedLine)
    {
        consumedLine = false;
        keyword = "";

        if (trimmed.StartsWith("menuentry", StringComparison.OrdinalIgnoreCase))
        {
            keyword = "menuentry";
            consumedLine = trimmed.EndsWith('{');
            return true;
        }

        if (trimmed.StartsWith("submenuentry", StringComparison.OrdinalIgnoreCase))
        {
            keyword = "submenuentry";
            consumedLine = trimmed.EndsWith('{');
            return true;
        }

        return false;
    }
}
