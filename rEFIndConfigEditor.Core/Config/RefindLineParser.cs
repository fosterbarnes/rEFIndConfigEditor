namespace rEFIndConfigEditor.Config;

public static class RefindLineParser
{
    public static bool TryParseGlobalLine(string line, out string token, out List<string> values, out bool isCommented)
    {
        token = "";
        values = [];
        isCommented = false;
        var trimmed = line.Trim();
        if (trimmed.Length == 0)
            return false;

        if (trimmed.StartsWith('#'))
        {
            trimmed = trimmed[1..].TrimStart();
            if (trimmed.Length == 0)
                return false;
            isCommented = true;
        }

        if (trimmed.StartsWith("menuentry", StringComparison.OrdinalIgnoreCase))
            return false;

        var parts = SplitArguments(trimmed);
        if (parts.Count == 0)
            return false;

        token = RefindTokens.Canonicalize(parts[0]);
        if (!RefindTokens.GlobalTokens.Contains(token))
            return false;

        if (RefindTokens.AcceptsCommaSeparatedValues(token))
        {
            var rawValuesText = "";
            var firstSpace = trimmed.IndexOfAny([' ', '\t']);
            if (firstSpace >= 0)
                rawValuesText = trimmed[firstSpace..].Trim();

            if (RefindTokens.IsFolderListToken(token))
                values = RefindWriter.ParseFolderListFromEditor(rawValuesText);
            else
                values = RefindWriter.ParseCommaListFromEditor(rawValuesText);
        }
        else
        {
            values = parts.Skip(1).ToList();
        }
        return true;
    }

    public static bool TryParseStanzaLine(string line, out string token, out List<string> values)
    {
        token = "";
        values = [];
        var trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed == "}")
            return false;

        if (trimmed.StartsWith('#') || trimmed.StartsWith("submenuentry", StringComparison.OrdinalIgnoreCase))
            return false;

        var parts = SplitArguments(trimmed);
        if (parts.Count == 0)
            return false;

        token = parts[0];
        if (!RefindTokens.StanzaTokens.Contains(token) && !string.Equals(token, "disabled", StringComparison.OrdinalIgnoreCase))
            return false;

        values = parts.Skip(1).ToList();
        return true;
    }

    public static List<string> SplitArguments(string line)
    {
        line = StripTrailingComment(line);
        var result = new List<string>();
        var i = 0;
        while (i < line.Length)
        {
            while (i < line.Length && char.IsWhiteSpace(line[i]))
                i++;
            if (i >= line.Length)
                break;

            if (line[i] == '"')
            {
                i++;
                var start = i;
                while (i < line.Length)
                {
                    if (line[i] == '"' && (i == 0 || line[i - 1] != '\\'))
                        break;
                    i++;
                }
                result.Add(line[start..i]);
                if (i < line.Length)
                    i++;
            }
            else
            {
                var start = i;
                while (i < line.Length && !char.IsWhiteSpace(line[i]))
                    i++;
                result.Add(line[start..i]);
            }
        }
        return result;
    }

    private static string StripTrailingComment(string line)
    {
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"' && (i == 0 || line[i - 1] != '\\'))
                inQuotes = !inQuotes;
            else if (c == '#' && !inQuotes && (i == 0 || char.IsWhiteSpace(line[i - 1])))
                return line[..i].TrimEnd();
        }
        return line;
    }

    public static bool TryParseBlockHeader(string line, string keyword, out string title)
    {
        title = "";
        var trimmed = line.Trim();
        if (!trimmed.StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
            return false;

        var afterKeyword = trimmed[keyword.Length..].TrimStart();
        if (afterKeyword.Length == 0)
            return true;

        var braceIndex = FindUnquotedBrace(afterKeyword);
        var titlePart = (braceIndex >= 0 ? afterKeyword[..braceIndex] : afterKeyword).Trim();
        if (titlePart.Length == 0)
            return true;

        if (titlePart.StartsWith('"'))
        {
            var parts = SplitArguments(titlePart);
            title = parts.Count > 0 ? parts[0] : "";
        }
        else
        {
            title = titlePart;
        }
        return true;
    }

    private static int FindUnquotedBrace(string s)
    {
        var inQuotes = false;
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (c == '"' && (i == 0 || s[i - 1] != '\\'))
                inQuotes = !inQuotes;
            else if (!inQuotes && (c == '{' || c == '}'))
                return i;
        }
        return -1;
    }
}
