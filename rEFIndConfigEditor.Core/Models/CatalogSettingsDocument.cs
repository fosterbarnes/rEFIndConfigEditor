namespace rEFIndConfigEditor.Models;

public sealed class CatalogSettingsDocument
{
    public Dictionary<string, CatalogSettingEntry> Values { get; set; } =
        new(StringComparer.Ordinal);

    public bool TryGet(string token, out CatalogSettingEntry entry) =>
        Values.TryGetValue(token, out entry!);

    public CatalogSettingsDocument Clone()
    {
        var clone = new CatalogSettingsDocument();
        foreach (var (token, entry) in Values)
            clone.Values[token] = entry.Clone();
        return clone;
    }
}
