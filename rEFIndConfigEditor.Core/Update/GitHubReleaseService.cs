using System.Net.Http.Headers;
using System.Text.Json;

namespace rEFIndConfigEditor.Update;

public static class GitHubReleaseService
{
    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30),
        };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(UpdateConstants.UserAgent, "1.0"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }

    public static async Task<GitHubRelease?> FetchLatestReleaseAsync(CancellationToken cancellationToken = default)
    {
        using var response = await Http.GetAsync(UpdateConstants.LatestReleaseApiUrl, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var dto = await JsonSerializer.DeserializeAsync(
            stream,
            GitHubReleaseJsonContext.Default.GitHubReleaseDto,
            cancellationToken).ConfigureAwait(false);

        return dto is null ? null : MapRelease(dto);
    }

    public static async Task<UpdateCheckResult> CheckForUpdateAsync(
        string installDirectory,
        CancellationToken cancellationToken = default)
    {
        var current = InstallVersionReader.ReadInstalledVersion(installDirectory);
        try
        {
            var release = await FetchLatestReleaseAsync(cancellationToken).ConfigureAwait(false);
            if (release is null || string.IsNullOrWhiteSpace(release.Version))
                return UpdateCheckResult.Failed(current, "No published releases were found.");

            var (isOutdated, currentIsNewer) = VersionComparer.GetComparison(current, release.Version);
            if (isOutdated)
            {
                var assetTag = ReleaseAssetNames.GetPortableAssetTag();
                if (release.FindPortableAsset(assetTag) is null)
                {
                    return UpdateCheckResult.Failed(
                        current,
                        $"No update package for this platform ({assetTag}).");
                }
            }

            return new UpdateCheckResult
            {
                CurrentVersion = current,
                LatestVersion = release.Version,
                LatestTag = release.TagName,
                ReleaseUrl = release.HtmlUrl ?? UpdateConstants.ReleasesPageUrl,
                IsOutdated = isOutdated,
                CurrentIsNewer = currentIsNewer,
                Release = release,
            };
        }
        catch (Exception ex)
        {
            return UpdateCheckResult.Failed(current, ex.Message);
        }
    }

    public static async Task DownloadAssetAsync(
        GitHubReleaseAsset asset,
        string destinationPath,
        IProgress<(float Progress, string Status)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

        using var response = await Http.GetAsync(asset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var destination = File.Create(destinationPath);

        var buffer = new byte[81920];
        long readBytes = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            readBytes += read;
            if (totalBytes > 0)
            {
                var fraction = (float)readBytes / totalBytes;
                progress?.Report((fraction, $"Downloading {asset.Name}..."));
            }
            else
            {
                progress?.Report((0f, $"Downloading {asset.Name}..."));
            }
        }

        if (totalBytes > 0 && readBytes != totalBytes)
            throw new InvalidOperationException(
                $"Download of {asset.Name} was truncated ({readBytes} of {totalBytes} bytes).");
    }

    private static GitHubRelease MapRelease(GitHubReleaseDto dto)
    {
        var assets = new List<GitHubReleaseAsset>();
        if (dto.Assets is not null)
        {
            foreach (var asset in dto.Assets)
            {
                if (string.IsNullOrWhiteSpace(asset.Name) || string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl))
                    continue;

                assets.Add(new GitHubReleaseAsset
                {
                    Name = asset.Name,
                    BrowserDownloadUrl = asset.BrowserDownloadUrl,
                });
            }
        }

        return new GitHubRelease
        {
            TagName = dto.TagName,
            HtmlUrl = dto.HtmlUrl,
            IsPrerelease = dto.IsPrerelease,
            Assets = assets,
        };
    }
}
