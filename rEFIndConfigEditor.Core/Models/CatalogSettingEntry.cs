namespace rEFIndConfigEditor.Models;

public sealed class CatalogSettingEntry
{
    public bool? Enabled { get; set; }
    public bool? Bool { get; set; }
    public decimal? Number { get; set; }
    public int? Index { get; set; }
    public string? Text { get; set; }
    public List<string>? Selected { get; set; }
    public List<string>? Items { get; set; }

    public CatalogSettingEntry Clone() => new()
    {
        Enabled = Enabled,
        Bool = Bool,
        Number = Number,
        Index = Index,
        Text = Text,
        Selected = Selected is null ? null : [.. Selected],
        Items = Items is null ? null : [.. Items],
    };

    public bool ValueEquals(CatalogSettingEntry? other)
    {
        if (other is null)
            return false;

        return Enabled == other.Enabled
            && Bool == other.Bool
            && Number == other.Number
            && Index == other.Index
            && string.Equals(Text, other.Text, StringComparison.Ordinal)
            && SequenceEqual(Selected, other.Selected)
            && SequenceEqual(Items, other.Items);
    }

    private static bool SequenceEqual(IReadOnlyList<string>? left, IReadOnlyList<string>? right)
    {
        if (left is null && right is null)
            return true;
        if (left is null || right is null || left.Count != right.Count)
            return false;

        for (var i = 0; i < left.Count; i++)
        {
            if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
                return false;
        }

        return true;
    }
}
