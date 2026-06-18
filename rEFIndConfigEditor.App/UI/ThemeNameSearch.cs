using rEFIndConfigEditor.Models;

namespace rEFIndConfigEditor.UI;

internal sealed record ThemeSearchHit(ThemeCatalogEntry Entry)
{
    public string DisplayText => Entry.Name;
}

internal static class ThemeNameSearch
{
    private const int MaxResults = 25;

    public static IReadOnlyList<ThemeSearchHit> Find(IReadOnlyList<ThemeCatalogEntry> entries, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var q = Normalize(query.Trim());
        var hits = new List<ThemeSearchHit>();

        foreach (var entry in entries)
        {
            if (!Normalize(entry.Name).Contains(q, StringComparison.Ordinal))
                continue;
            hits.Add(new ThemeSearchHit(entry));
        }

        hits.Sort((a, b) => string.Compare(a.Entry.Name, b.Entry.Name, StringComparison.OrdinalIgnoreCase));
        if (hits.Count > MaxResults)
            hits.RemoveRange(MaxResults, hits.Count - MaxResults);

        return hits;
    }

    private static string Normalize(string value) =>
        value.Replace("'", "", StringComparison.Ordinal)
            .Replace("\u2019", "", StringComparison.Ordinal);
}
