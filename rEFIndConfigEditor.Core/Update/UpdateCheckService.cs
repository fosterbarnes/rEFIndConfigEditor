namespace rEFIndConfigEditor.Update;

public static class UpdateCheckService
{
    public static Task<UpdateCheckResult> CheckAsync(
        string installDirectory,
        CancellationToken cancellationToken = default) =>
        GitHubReleaseService.CheckForUpdateAsync(installDirectory, cancellationToken);
}
