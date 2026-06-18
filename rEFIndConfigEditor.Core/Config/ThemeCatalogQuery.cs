using rEFIndConfigEditor.Models;

namespace rEFIndConfigEditor.Config;

public enum ThemeBrowserCategory
{
    All,
    Dark,
    Light,
    Minimal,
    Oled,
    Gaming,
    ColorScheme,
    IconSet,
    Scenic,
    Retro,
    MultiPack,
    RecentlyAdded
}

public enum ThemeBrowserSort
{
    NameAsc,
    NameDesc,
    StarsDesc,
    StarsAsc,
    Newest,
    Oldest
}

public static class ThemeCatalogQuery
{
    public static string CategoryLabel(ThemeBrowserCategory category) => category switch
    {
        ThemeBrowserCategory.All => "All themes",
        ThemeBrowserCategory.Dark => "Dark",
        ThemeBrowserCategory.Light => "Light",
        ThemeBrowserCategory.Minimal => "Minimal",
        ThemeBrowserCategory.Oled => "OLED / pure black",
        ThemeBrowserCategory.Gaming => "Gaming / pop culture",
        ThemeBrowserCategory.ColorScheme => "Color scheme",
        ThemeBrowserCategory.IconSet => "Icon sets",
        ThemeBrowserCategory.Scenic => "Scenic / nature",
        ThemeBrowserCategory.Retro => "Retro / neon",
        ThemeBrowserCategory.MultiPack => "Multi-theme packs",
        ThemeBrowserCategory.RecentlyAdded => "Recently added",
        _ => category.ToString()
    };

    public static string SortLabel(ThemeBrowserSort sort) => sort switch
    {
        ThemeBrowserSort.NameAsc => "Name (A–Z)",
        ThemeBrowserSort.NameDesc => "Name (Z–A)",
        ThemeBrowserSort.StarsDesc => "Most popular",
        ThemeBrowserSort.StarsAsc => "Least popular",
        ThemeBrowserSort.Newest => "Newest",
        ThemeBrowserSort.Oldest => "Oldest",
        _ => sort.ToString()
    };

    public static IReadOnlyList<ThemeCatalogEntry> FilterAndSort(
        IReadOnlyList<ThemeCatalogEntry> entries,
        ThemeBrowserCategory category,
        ThemeBrowserSort sort)
    {
        IEnumerable<ThemeCatalogEntry> filtered = entries;

        switch (category)
        {
            case ThemeBrowserCategory.Dark:
                filtered = filtered.Where(e => e.IsDark);
                break;
            case ThemeBrowserCategory.Light:
                filtered = filtered.Where(e => e.IsLight);
                break;
            case ThemeBrowserCategory.Minimal:
                filtered = filtered.Where(e => e.IsMinimal);
                break;
            case ThemeBrowserCategory.Oled:
                filtered = filtered.Where(e => HasTag(e, "oled"));
                break;
            case ThemeBrowserCategory.Gaming:
                filtered = filtered.Where(e => HasTag(e, "gaming"));
                break;
            case ThemeBrowserCategory.ColorScheme:
                filtered = filtered.Where(e => HasTag(e, "color-scheme"));
                break;
            case ThemeBrowserCategory.IconSet:
                filtered = filtered.Where(e => HasTag(e, "icon-set"));
                break;
            case ThemeBrowserCategory.Scenic:
                filtered = filtered.Where(e => HasTag(e, "scenic"));
                break;
            case ThemeBrowserCategory.Retro:
                filtered = filtered.Where(e => HasTag(e, "retro"));
                break;
            case ThemeBrowserCategory.MultiPack:
                filtered = filtered.Where(e => HasTag(e, "multi-pack"));
                break;
            case ThemeBrowserCategory.RecentlyAdded:
                filtered = filtered.Where(e => e.RecentlyAdded);
                break;
        }

        filtered = sort switch
        {
            ThemeBrowserSort.NameAsc => filtered.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase),
            ThemeBrowserSort.NameDesc => filtered.OrderByDescending(e => e.Name, StringComparer.OrdinalIgnoreCase),
            ThemeBrowserSort.StarsDesc => filtered
                .OrderByDescending(e => e.GithubStars)
                .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase),
            ThemeBrowserSort.StarsAsc => filtered
                .OrderBy(e => e.GithubStars)
                .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase),
            ThemeBrowserSort.Newest => filtered
                .OrderByDescending(e => e.Created)
                .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase),
            ThemeBrowserSort.Oldest => filtered
                .OrderBy(e => e.Created == default ? DateTimeOffset.MaxValue : e.Created)
                .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase),
            _ => filtered
        };

        return filtered.ToList();
    }

    private static bool HasTag(ThemeCatalogEntry entry, string tag) =>
        entry.Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
}
