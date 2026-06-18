namespace rEFIndConfigEditor.Config;

public static class RefindWriter
{
    private const string Lf = "\n";

    private static void AppendLn(System.Text.StringBuilder sb, string line) =>
        sb.Append(line).Append(Lf);

    public static string Write(RefindDocument doc)
    {
        var sb = new System.Text.StringBuilder();
        var writtenGlobals = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in doc.Rows)
        {
            switch (row)
            {
                case RawConfigRow raw:
                    AppendLn(sb, raw.Text);
                    break;
                case GlobalOptionRow g:
                    var token = RefindTokens.Canonicalize(g.Option.Token);
                    if (!writtenGlobals.Add(token))
                        break;
                    AppendLn(sb, FormatGlobalLine(token, g.Option.Values, commented: !g.Option.IsActive));
                    break;
                case MenuEntryRow m:
                    WriteMenuEntry(sb, m.Entry);
                    break;
            }
        }
        return sb.ToString().TrimEnd() + Lf;
    }

    private static string FormatGlobalLine(string token, List<string> values, bool commented)
    {
        if (values.Count == 0)
            return commented ? "# " + token : token;

        var separator = RefindTokens.AcceptsCommaSeparatedValues(token) ? ", " : " ";
        var body = token + " " + string.Join(separator, values.Select(v =>
            IsFolderListToken(token) ? QuotePathListEntry(v) : QuoteIfNeeded(v)));
        return commented ? "# " + body : body;
    }

    private static void WriteMenuEntry(System.Text.StringBuilder sb, MenuEntry entry)
    {
        var name = QuoteIfNeeded(entry.Title);
        AppendLn(sb, $"menuentry {name} {{");
        WriteStanzaBody(sb, entry.Fields, entry.Disabled);
        foreach (var sub in entry.Submenus)
        {
            AppendLn(sb, $"    submenuentry {QuoteIfNeeded(sub.Title)} {{");
            WriteStanzaBody(sb, sub.Fields, sub.Disabled, indent: 8);
            WriteTrailers(sb, sub.Trailers, indent: 8);
            AppendLn(sb, "    }");
        }
        WriteTrailers(sb, entry.Trailers, indent: 4);
        AppendLn(sb, "}");
    }

    private static void WriteTrailers(System.Text.StringBuilder sb, List<string> trailers, int indent)
    {
        if (trailers.Count == 0)
            return;

        var pad = new string(' ', indent);
        foreach (var raw in trailers)
        {
            var trimmed = raw.TrimStart();
            AppendLn(sb, trimmed.Length == 0 ? raw : pad + trimmed);
        }
    }

    private static void WriteStanzaBody(
        System.Text.StringBuilder sb,
        Dictionary<string, StanzaValue> fields,
        bool disabled,
        int indent = 4)
    {
        var pad = new string(' ', indent);
        string[] order = ["volume", "loader", "initrd", "icon", "ostype", "graphics", "options", "firmware_bootnum", "add_options"];
        var keys = fields.Keys.OrderBy(k =>
        {
            var i = Array.FindIndex(order, s => string.Equals(s, k, StringComparison.OrdinalIgnoreCase));
            return i < 0 ? 1000 : i;
        }).ThenBy(k => k, StringComparer.OrdinalIgnoreCase);
        foreach (var key in keys)
        {
            var val = fields[key];
            var token = RefindTokens.Canonicalize(key);
            if (val.IsExplicitEmpty)
                AppendLn(sb, pad + token);
            else if (val.PreserveMultiToken && val.Arguments.Count > 0)
                AppendLn(sb, pad + token + " " + string.Join(" ", val.Arguments.Select(QuoteIfNeeded)));
            else
                AppendLn(sb, pad + token + " " + QuoteIfNeeded(val.Value));
        }

        if (disabled)
            AppendLn(sb, pad + "disabled");
    }

    public static bool IsFolderListToken(string token) => RefindTokens.IsFolderListToken(token);

    public static string QuotePathListEntry(string value)
    {
        if (string.Equals(value, "+", StringComparison.Ordinal))
            return value;
        if (value.Length == 0)
            return "\"\"";
        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }

    public static string UnquoteListEntry(string value)
    {
        var v = value.Trim();
        if (v.Length >= 2 && v[0] == '"' && v[^1] == '"')
            return v[1..^1].Replace("\\\"", "\"");
        return v;
    }

    public static string JoinFolderListForEditor(IReadOnlyList<string> values) =>
        string.Join(", ", values.Select(QuotePathListEntry));

    public static string AppendCommaList(string currentText, IEnumerable<string> newItems)
    {
        var items = ParseCommaListFromEditor(currentText);
        foreach (var item in newItems)
        {
            if (string.IsNullOrWhiteSpace(item))
                continue;
            var v = item.Trim();
            if (items.Any(x => string.Equals(x, v, StringComparison.OrdinalIgnoreCase)))
                continue;
            items.Add(v);
        }

        return string.Join(", ", items);
    }

    public static List<string> ParseCommaListFromEditor(string text) =>
        ParseCommaSeparatedList(text);

    public static List<string> ParseFolderListFromEditor(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        return ParseCommaSeparatedList(text).Select(UnquoteListEntry).ToList();
    }

    private static List<string> ParseCommaSeparatedList(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var items = new List<string>();
        var i = 0;
        while (i < text.Length)
        {
            while (i < text.Length && (char.IsWhiteSpace(text[i]) || text[i] == ','))
                i++;
            if (i >= text.Length)
                break;

            if (text[i] == '"')
            {
                i++;
                var start = i;
                while (i < text.Length)
                {
                    if (text[i] == '\\' && i + 1 < text.Length)
                    {
                        i += 2;
                        continue;
                    }
                    if (text[i] == '"')
                        break;
                    i++;
                }
                items.Add(text[start..i]);
                if (i < text.Length)
                    i++;
                continue;
            }

            var tokenStart = i;
            while (i < text.Length && text[i] != ',')
                i++;
            var token = text[tokenStart..i].Trim();
            if (token.Length > 0)
                items.Add(token);
        }

        return items;
    }

    public static string QuoteIfNeeded(string value)
    {
        if (value.Length == 0)
            return "\"\"";
        if (value.Any(char.IsWhiteSpace) || value.Contains('"') || value.Contains('#') || value.Contains(','))
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        return value;
    }
}
