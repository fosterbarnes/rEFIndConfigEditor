using System.IO.Compression;
using System.Net.Http;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using rEFIndConfigEditor.Models;
using rEFIndConfigEditor.Storage;
using rEFIndConfigEditor.UI;

namespace rEFIndConfigEditor.Config;

internal sealed class ThemeInstallResult
{
    public bool Success { get; init; }
    public string? IncludePath { get; init; }
    public string? Error { get; init; }
}

internal static class ThemeInstaller
{
    private static readonly HttpClient Http = CreateHttp();
    private static readonly Regex GithubNamePattern = new("^[A-Za-z0-9._-]+$", RegexOptions.CultureInvariant);

    private static readonly string[] BranchFallbacks = ["main", "master"];

    public static async Task<ThemeInstallResult> InstallAsync(
        ThemeCatalogEntry entry,
        string refindConfPath,
        CancellationToken cancellationToken = default)
    {
        var refindDir = RefindPathHelper.GetRefindDirectory(refindConfPath);
        if (refindDir is null)
            return Fail("Could not determine the rEFInd directory for the open refind.conf.");

        var repo = ParseGithubRepo(entry.Github);
        if (repo is null)
            return Fail("Could not parse the theme repository URL.");

        var (owner, repoName) = repo.Value;
        var themesDir = Path.Combine(refindDir, "themes");
        Directory.CreateDirectory(themesDir);

        var dest = Path.Combine(themesDir, repoName);
        if (!IsPathUnderDirectory(dest, themesDir))
            return Fail("Theme install path is invalid.");

        if (Directory.Exists(dest))
            return Fail($"Theme folder already exists:\nthemes/{repoName}");

        var staging = Path.Combine(themesDir, $".{repoName}.dl-{Guid.NewGuid():N}");
        Directory.CreateDirectory(staging);

        try
        {
            var zipPath = Path.Combine(staging, "archive.zip");
            if (!await DownloadArchiveAsync(owner, repoName, zipPath, cancellationToken))
                return Fail("Could not download theme from GitHub.");

            ExtractZipSafely(zipPath, staging);
            File.Delete(zipPath);

            var roots = Directory.GetDirectories(staging)
                .Where(d => !Path.GetFileName(d).StartsWith("__MACOSX", StringComparison.Ordinal))
                .ToList();
            if (roots.Count == 0)
                return Fail("Download archive was empty.");

            Directory.Move(roots[0], dest);
            TryDeleteDirectory(staging);
        }
        catch (Exception ex)
        {
            TryDeleteDirectory(staging);
            TryDeleteDirectory(dest);
            return Fail(ex.Message);
        }

        var hits = RefindPathHelper.FindConfFiles(dest, refindConfPath);
        if (hits.Count == 0)
        {
            TryDeleteDirectory(dest);
            return Fail("Download finished but no .conf file was found in the theme.");
        }

        return new ThemeInstallResult { Success = true, IncludePath = hits[0] };
    }

    private static ThemeInstallResult Fail(string message) =>
        new() { Success = false, Error = message };

    private static HttpClient CreateHttp()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("rEFIndConfigEditor/1.0");
        return client;
    }

    private static (string Owner, string Repo)? ParseGithubRepo(string githubUrl)
    {
        if (string.IsNullOrWhiteSpace(githubUrl))
            return null;
        if (!Uri.TryCreate(githubUrl.Trim(), UriKind.Absolute, out var uri))
            return null;
        if (!uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            return null;

        var segments = uri.AbsolutePath.Trim('/').Split('/');
        if (segments.Length < 2)
            return null;

        var owner = segments[^2];
        var repo = segments[^1];
        if (repo.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            repo = repo[..^4];

        if (!GithubNamePattern.IsMatch(owner) || !GithubNamePattern.IsMatch(repo))
            return null;

        return string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo)
            ? null
            : (owner, repo);
    }

    private static bool IsPathUnderDirectory(string path, string rootDir)
    {
        var fullPath = Path.GetFullPath(path);
        var fullRoot = Path.GetFullPath(rootDir);
        if (!fullRoot.EndsWith(Path.DirectorySeparatorChar))
            fullRoot += Path.DirectorySeparatorChar;
        return fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<bool> DownloadArchiveAsync(
        string owner,
        string repo,
        string zipPath,
        CancellationToken cancellationToken)
    {
        var branches = await ResolveBranchesAsync(owner, repo, cancellationToken);
        foreach (var branch in branches)
        {
            var url = $"https://github.com/{owner}/{repo}/archive/refs/heads/{Uri.EscapeDataString(branch)}.zip";
            using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
                continue;

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            if (response.Content.Headers.ContentLength is long len && len > SafeFileIO.MaxThemeZipBytes)
                return false;

            await using var file = File.Create(zipPath);
            var buffer = new byte[81920];
            long total = 0;
            int read;
            while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                total += read;
                if (total > SafeFileIO.MaxThemeZipBytes)
                    return false;
                await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }

            return true;
        }

        return false;
    }

    private static async Task<IReadOnlyList<string>> ResolveBranchesAsync(
        string owner,
        string repo,
        CancellationToken cancellationToken)
    {
        var ordered = new List<string>();

        try
        {
            using var response = await Http.GetAsync(
                $"https://api.github.com/repos/{owner}/{repo}",
                cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var branch = JObject.Parse(json)["default_branch"]?.Value<string>();
                if (!string.IsNullOrWhiteSpace(branch))
                    ordered.Add(branch);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"ThemeInstaller branch resolve failed: {ex}");
        }

        foreach (var fallback in BranchFallbacks)
        {
            if (!ordered.Contains(fallback, StringComparer.OrdinalIgnoreCase))
                ordered.Add(fallback);
        }

        return ordered;
    }

    private static void ExtractZipSafely(string zipPath, string destDir)
    {
        var root = Path.GetFullPath(destDir);
        if (!root.EndsWith(Path.DirectorySeparatorChar))
            root += Path.DirectorySeparatorChar;

        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name))
                continue;

            var destPath = Path.GetFullPath(Path.Combine(root, entry.FullName));
            if (!destPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Archive entry escapes the destination directory.");

            var parent = Path.GetDirectoryName(destPath);
            if (parent is not null)
                Directory.CreateDirectory(parent);

            entry.ExtractToFile(destPath, overwrite: true);
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"ThemeInstaller delete '{path}' failed: {ex}");
        }
    }
}
