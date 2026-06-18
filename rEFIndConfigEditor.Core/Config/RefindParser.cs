namespace rEFIndConfigEditor.Config;

public static class RefindParser
{
    public static RefindDocument Parse(string text)
    {
        var doc = new RefindDocument();
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var i = 0;
        while (i < lines.Length)
        {
            var line = lines[i];
            var trimmed = line.Trim();

            if (trimmed.StartsWith("menuentry", StringComparison.OrdinalIgnoreCase))
            {
                var entry = ParseMenuEntry(lines, ref i);
                doc.Rows.Add(new MenuEntryRow(entry));
                continue;
            }

            if (RefindLineParser.TryParseGlobalLine(line, out var token, out var values, out var commented))
            {
                token = RefindTokens.Canonicalize(token);
                var existing = doc.FindGlobal(token);
                if (existing is not null)
                {
                    if (!commented)
                    {
                        existing.Token = token;
                        existing.IsActive = true;
                        existing.Values.Clear();
                        existing.Values.AddRange(values);
                    }
                    else
                    {
                        doc.Rows.Add(new RawConfigRow(line));
                    }
                }
                else
                {
                    var option = new GlobalOption
                    {
                        Token = token,
                        IsActive = !commented
                    };
                    option.Values.AddRange(values);
                    doc.Rows.Add(new GlobalOptionRow(option));
                }
                i++;
                continue;
            }

            doc.Rows.Add(new RawConfigRow(line));
            i++;
        }
        return doc;
    }

    private static MenuEntry ParseMenuEntry(string[] lines, ref int index)
    {
        var header = lines[index];
        RefindLineParser.TryParseBlockHeader(header, "menuentry", out var title);
        var entry = new MenuEntry { Title = title };
        index++;

        while (index < lines.Length)
        {
            var line = lines[index];
            var trimmed = line.Trim();
            if (trimmed == "}")
            {
                index++;
                break;
            }

            if (trimmed.StartsWith("submenuentry", StringComparison.OrdinalIgnoreCase))
            {
                entry.Submenus.Add(ParseSubmenuEntry(lines, ref index));
                continue;
            }

            var outcome = ApplyStanzaLine(line, entry.Fields);
            if (outcome == StanzaLineOutcome.Disabled)
                entry.Disabled = true;
            else if (outcome == StanzaLineOutcome.Unrecognized)
                entry.Trailers.Add(line);
            index++;
        }
        return entry;
    }

    private static SubmenuEntry ParseSubmenuEntry(string[] lines, ref int index)
    {
        var header = lines[index];
        RefindLineParser.TryParseBlockHeader(header, "submenuentry", out var title);
        var sub = new SubmenuEntry { Title = title };
        index++;

        while (index < lines.Length)
        {
            var line = lines[index];
            var trimmed = line.Trim();
            if (trimmed == "}")
            {
                index++;
                break;
            }

            var outcome = ApplyStanzaLine(line, sub.Fields);
            if (outcome == StanzaLineOutcome.Disabled)
                sub.Disabled = true;
            else if (outcome == StanzaLineOutcome.Unrecognized)
                sub.Trailers.Add(line);
            index++;
        }
        return sub;
    }

    private enum StanzaLineOutcome { Applied, Disabled, Unrecognized }

    private static StanzaLineOutcome ApplyStanzaLine(string line, Dictionary<string, StanzaValue> fields)
    {
        if (!RefindLineParser.TryParseStanzaLine(line, out var token, out var values))
            return StanzaLineOutcome.Unrecognized;

        token = RefindTokens.Canonicalize(token);
        if (string.Equals(token, "disabled", StringComparison.OrdinalIgnoreCase))
            return StanzaLineOutcome.Disabled;

        var preserveMulti = values.Count > 1
            || StanzaValue.ShouldPreserveMultiToken(token, fields)
            || (fields.TryGetValue(token, out var prev) && prev.PreserveMultiToken);

        if (values.Count == 0)
            fields[token] = new StanzaValue([], isExplicitEmpty: true);
        else if (preserveMulti)
            fields[token] = new StanzaValue(values, preserveMultiToken: true);
        else
            fields[token] = new StanzaValue(values[0]);
        return StanzaLineOutcome.Applied;
    }


}
