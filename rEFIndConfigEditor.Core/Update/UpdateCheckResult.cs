namespace rEFIndConfigEditor.Update;

public sealed class UpdateCheckResult
{
    public required string CurrentVersion { get; init; }
    public string? LatestVersion { get; init; }
    public string? LatestTag { get; init; }
    public string? ReleaseUrl { get; init; }
    public bool IsOutdated { get; init; }
    public bool CurrentIsNewer { get; init; }
    public GitHubRelease? Release { get; init; }
    public string? ErrorMessage { get; init; }

    public static UpdateCheckResult Failed(string currentVersion, string message) => new()
    {
        CurrentVersion = currentVersion,
        ErrorMessage = message,
    };
}
