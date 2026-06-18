namespace rEFIndConfigEditor.Update;

public static class VersionComparer
{
    public static int Compare(string? current, string? latest)
    {
        var left = GitHubRelease.NormalizeTag(current);
        var right = GitHubRelease.NormalizeTag(latest);
        if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase) ? 0 : -1;

        if (Version.TryParse(left, out var leftVersion) && Version.TryParse(right, out var rightVersion))
            return leftVersion.CompareTo(rightVersion);

        return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsOutdated(string? current, string? latest)
    {
        if (string.IsNullOrWhiteSpace(latest))
            return false;
        return Compare(current, latest) < 0;
    }

    public static (bool IsOutdated, bool CurrentIsNewer) GetComparison(string? current, string? latest)
    {
        if (string.IsNullOrWhiteSpace(latest))
            return (false, false);

        var cmp = Compare(current, latest);
        return (cmp < 0, cmp > 0);
    }
}
