using System.Text.Json.Serialization;

namespace rEFIndConfigEditor.Update;

[JsonSerializable(typeof(GitHubReleaseDto))]
internal partial class GitHubReleaseJsonContext : JsonSerializerContext;

internal sealed class GitHubReleaseDto
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; init; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; init; }

    [JsonPropertyName("prerelease")]
    public bool IsPrerelease { get; init; }

    [JsonPropertyName("assets")]
    public GitHubReleaseAssetDto[]? Assets { get; init; }
}

internal sealed class GitHubReleaseAssetDto
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("browser_download_url")]
    public string? BrowserDownloadUrl { get; init; }
}
