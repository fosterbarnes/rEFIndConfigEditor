namespace rEFIndConfigEditor.Update;

public sealed class GitHubRelease
{
    public string? TagName { get; init; }
    public string? HtmlUrl { get; init; }
    public bool IsPrerelease { get; init; }
    public IReadOnlyList<GitHubReleaseAsset> Assets { get; init; } = [];

    public string Version => NormalizeTag(TagName);

    public static string NormalizeTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return string.Empty;
        return tag.Trim().TrimStart('v', 'V');
    }
}

public sealed class GitHubReleaseAsset
{
    public required string Name { get; init; }
    public required string BrowserDownloadUrl { get; init; }
}
