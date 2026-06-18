using rEFIndConfigEditor.Config;
using rEFIndConfigEditor.Settings;

namespace rEFIndConfigEditor;

public sealed record SettingSearchHit(string Token, string Label, SettingCategory Category)
{
    public string DisplayText =>
        $"{Label}  —  {Token}  ({SettingSearch.CategoryDisplayName(Category)})";
}

public static class SettingSearch
{
    private const int MaxResults = 25;

    private static readonly SettingSearchHit IncludeHit = new(
        "include", "Theme include", SettingCategory.Theme);

    public static string CategoryDisplayName(SettingCategory category) => category switch
    {
        SettingCategory.General => "General",
        SettingCategory.Display => "Display",
        SettingCategory.Theme => "Theme",
        SettingCategory.Input => "Input",
        SettingCategory.Scanning => "Scanning",
        SettingCategory.Other => "Other",
        SettingCategory.App => "App",
        _ => category.ToString()
    };

    public static IReadOnlyList<SettingSearchHit> Find(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var q = Normalize(query.Trim());
        var labelHits = new List<SettingSearchHit>();
        var tokenHits = new List<SettingSearchHit>();

        foreach (var def in SettingCatalog.All)
        {
            if (def.Category == SettingCategory.App)
                continue;

            if (Normalize(def.Label).Contains(q, StringComparison.Ordinal))
                labelHits.Add(new SettingSearchHit(def.Token, def.Label, def.Category));
            else if (Normalize(def.Token).Contains(q, StringComparison.Ordinal))
                tokenHits.Add(new SettingSearchHit(def.Token, def.Label, def.Category));
        }

        if (Normalize(IncludeHit.Label).Contains(q, StringComparison.Ordinal))
            labelHits.Add(IncludeHit);
        else if (Normalize(IncludeHit.Token).Contains(q, StringComparison.Ordinal))
            tokenHits.Add(IncludeHit);

        labelHits.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase));
        tokenHits.Sort((a, b) => string.Compare(a.Token, b.Token, StringComparison.OrdinalIgnoreCase));

        return MergeWithQuota(labelHits, tokenHits, MaxResults);
    }

    private static List<SettingSearchHit> MergeWithQuota(
        List<SettingSearchHit> primary,
        List<SettingSearchHit> secondary,
        int max)
    {
        var half = max / 2;
        var primaryTake = Math.Min(primary.Count, Math.Max(half, max - secondary.Count));
        var secondaryTake = Math.Min(secondary.Count, max - primaryTake);
        var result = new List<SettingSearchHit>(primaryTake + secondaryTake);
        result.AddRange(primary.Take(primaryTake));
        result.AddRange(secondary.Take(secondaryTake));
        return result;
    }

    private static string Normalize(string value) =>
        value.Replace("'", "", StringComparison.Ordinal)
            .Replace("\u2019", "", StringComparison.Ordinal);
}
