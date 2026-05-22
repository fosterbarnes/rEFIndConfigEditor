namespace rEFIndConfigEditor.Config;

internal sealed class MenuEntry
{
    public string Title { get; set; } = "";
    public Dictionary<string, StanzaValue> Fields { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<SubmenuEntry> Submenus { get; } = [];
    public List<string> Trailers { get; } = [];
    public bool Disabled { get; set; }

    public string? GetField(string key) =>
        Fields.TryGetValue(key, out var v) ? v.Value : null;

    public void SetField(string key, string? value, bool clearIfEmpty = true, bool allowEmpty = false)
    {
        if (value is null && !allowEmpty)
        {
            if (clearIfEmpty)
                Fields.Remove(key);
            return;
        }
        if (allowEmpty && value is null or { Length: 0 })
        {
            Fields[key] = new StanzaValue([], isExplicitEmpty: true, preserveMultiToken: StanzaValue.ShouldPreserveMultiToken(key, Fields));
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            if (clearIfEmpty)
                Fields.Remove(key);
            return;
        }
        StanzaFieldHelper.SetMultiAwareField(Fields, key, value.Trim());
    }

    public string Summary
    {
        get
        {
            var loader = GetField("loader") ?? GetField("firmware_bootnum");
            var suffix = Disabled ? " [disabled]" : "";
            return string.IsNullOrEmpty(loader) ? Title + suffix : $"{Title} — {loader}{suffix}";
        }
    }
}

internal sealed class SubmenuEntry
{
    public string Title { get; set; } = "";
    public Dictionary<string, StanzaValue> Fields { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> Trailers { get; } = [];
    public bool Disabled { get; set; }

    public string? GetField(string key) =>
        Fields.TryGetValue(key, out var v) ? v.Value : null;

    public void SetField(string key, string? value, bool allowEmpty = false)
    {
        if (value is null && !allowEmpty)
        {
            Fields.Remove(key);
            return;
        }
        if (allowEmpty && value is null or { Length: 0 })
        {
            Fields[key] = new StanzaValue([], isExplicitEmpty: true, preserveMultiToken: StanzaValue.ShouldPreserveMultiToken(key, Fields));
            return;
        }

        StanzaFieldHelper.SetMultiAwareField(Fields, key, value!.Trim());
    }
}

internal static class StanzaFieldHelper
{
    internal static void SetMultiAwareField(Dictionary<string, StanzaValue> fields, string key, string value)
    {
        var preserve = StanzaValue.ShouldPreserveMultiToken(key, fields);
        if (preserve)
        {
            var args = RefindLineParser.SplitArguments(value);
            fields[key] = new StanzaValue(args, preserveMultiToken: true);
            return;
        }

        fields[key] = new StanzaValue(value);
    }
}

internal sealed class StanzaValue
{
    private readonly List<string> _arguments;

    public IReadOnlyList<string> Arguments => _arguments;
    public bool IsExplicitEmpty { get; }
    public bool PreserveMultiToken { get; }
    public string Value => _arguments.Count == 0 ? "" : string.Join(" ", _arguments);

    public StanzaValue(string value, bool isExplicitEmpty = false, bool preserveMultiToken = false)
    {
        IsExplicitEmpty = isExplicitEmpty;
        PreserveMultiToken = preserveMultiToken;
        _arguments = isExplicitEmpty ? [] : [value];
    }

    public StanzaValue(IReadOnlyList<string> arguments, bool isExplicitEmpty = false, bool preserveMultiToken = false)
    {
        IsExplicitEmpty = isExplicitEmpty;
        PreserveMultiToken = preserveMultiToken;
        _arguments = [.. arguments];
    }

    public StanzaValue Copy() =>
        new(_arguments, IsExplicitEmpty, PreserveMultiToken);

    public static bool ShouldPreserveMultiToken(string key, Dictionary<string, StanzaValue> fields)
    {
        if (fields.TryGetValue(key, out var existing) && existing.PreserveMultiToken)
            return true;
        return string.Equals(key, "options", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "add_options", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "loader", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "initrd", StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class GlobalOption
{
    public string Token { get; set; } = "";
    public List<string> Values { get; } = [];
    public bool IsActive { get; set; } = true;

    public string DisplayValue => string.Join(" ", Values);
}

internal abstract class ConfigRow;

internal sealed class RawConfigRow(string text) : ConfigRow
{
    public string Text { get; } = text;
}

internal sealed class GlobalOptionRow(GlobalOption option) : ConfigRow
{
    public GlobalOption Option { get; } = option;
}

internal sealed class MenuEntryRow(MenuEntry entry) : ConfigRow
{
    public MenuEntry Entry { get; } = entry;
}

internal sealed class RefindDocument
{
    public List<ConfigRow> Rows { get; } = [];

    public IEnumerable<GlobalOption> GlobalOptions =>
        Rows.OfType<GlobalOptionRow>().Select(r => r.Option);

    public IEnumerable<MenuEntry> MenuEntries =>
        Rows.OfType<MenuEntryRow>().Select(r => r.Entry);

    public GlobalOption? FindGlobal(string token)
    {
        token = RefindTokens.Canonicalize(token);
        return GlobalOptions.FirstOrDefault(o =>
            RefindTokens.TokenEquals(o.Token, token));
    }

    public GlobalOption GetOrCreateGlobal(string token)
    {
        token = RefindTokens.Canonicalize(token);
        var existing = FindGlobal(token);
        if (existing is not null)
            return existing;

        var option = new GlobalOption { Token = token, IsActive = false };
        Rows.Add(new GlobalOptionRow(option));
        return option;
    }

    public void SetGlobal(string token, bool active, IEnumerable<string>? values = null)
    {
        token = RefindTokens.Canonicalize(token);
        GlobalOptionRow? keeper = null;

        for (var i = 0; i < Rows.Count; i++)
        {
            if (Rows[i] is not GlobalOptionRow row || !RefindTokens.TokenEquals(row.Option.Token, token))
                continue;

            if (keeper is null)
                keeper = row;
            else
                Rows.RemoveAt(i--);
        }

        if (keeper is null)
        {
            if (!active)
                return;

            var created = new GlobalOption { Token = token, IsActive = true };
            if (values is not null)
                created.Values.AddRange(values);
            Rows.Add(new GlobalOptionRow(created));
            return;
        }

        keeper.Option.Token = token;
        keeper.Option.IsActive = active;
        keeper.Option.Values.Clear();
        if (values is not null)
            keeper.Option.Values.AddRange(values);
    }

    public void RemoveGlobal(string token)
    {
        token = RefindTokens.Canonicalize(token);
        Rows.RemoveAll(r => r is GlobalOptionRow g && RefindTokens.TokenEquals(g.Option.Token, token));
    }

    public void CollapseDuplicateGlobals()
    {
        var keepers = new Dictionary<string, GlobalOptionRow>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < Rows.Count; i++)
        {
            if (Rows[i] is not GlobalOptionRow row)
                continue;

            var token = RefindTokens.Canonicalize(row.Option.Token);
            if (keepers.TryGetValue(token, out var keeper))
            {
                if (row.Option.IsActive)
                {
                    keeper.Option.IsActive = true;
                    if (row.Option.Values.Count > 0)
                    {
                        keeper.Option.Values.Clear();
                        keeper.Option.Values.AddRange(row.Option.Values);
                    }
                }

                Rows.RemoveAt(i--);
                continue;
            }

            keepers[token] = row;
        }
    }

}
