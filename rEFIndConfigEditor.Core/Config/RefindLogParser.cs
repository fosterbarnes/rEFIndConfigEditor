using System.Text.RegularExpressions;

namespace rEFIndConfigEditor.Config;

public sealed record RefindLogBootCandidate(
    string Title,
    string Volume,
    string Loader,
    string? Initrd,
    string? SuggestedOstype,
    bool IsManualEntry = false)
{
    public string Summary
    {
        get
        {
            var prefix = IsManualEntry ? "[Manual Entry] " : "";
            if (string.IsNullOrEmpty(Volume) && string.IsNullOrEmpty(Loader))
                return prefix + Title;
            return $"{prefix}{Title} — {Volume} — {Loader}";
        }
    }

    public static RefindLogBootCandidate FromMenuEntry(MenuEntry entry) =>
        new(
            entry.Title,
            entry.GetField("volume") ?? "",
            entry.GetField("loader") ?? entry.GetField("firmware_bootnum") ?? "",
            entry.GetField("initrd"),
            entry.GetField("ostype"),
            IsManualEntry: true);

    public MenuEntry ToMenuEntry()
    {
        var entry = new MenuEntry { Title = Title };
        entry.SetField("volume", Volume);
        entry.SetField("loader", Loader);
        entry.SetField("initrd", Initrd);
        if (SuggestedOstype is not null)
            entry.SetField("ostype", SuggestedOstype);
        return entry;
    }
}

public static partial class RefindLogParser
{
    [GeneratedRegex(@"^\d{2}:\d{2}:\d{2}\s+-\s+")]
    private static partial Regex TimestampPrefix();

    [GeneratedRegex(@"^Scanning EFI files on (.+)$")]
    private static partial Regex ScanVolume();

    [GeneratedRegex(@"^Adding loader entry for '(.+)'$")]
    private static partial Regex AddingLoader();

    [GeneratedRegex(@"^Adding EFI loader entry for '(.+)'$")]
    private static partial Regex AddingFirmwareLoader();

    [GeneratedRegex(@"^Loader path is '(.+)'$")]
    private static partial Regex LoaderPath();

    [GeneratedRegex(@"^Creating subscreen 'Boot Options for .+ on (.+)'$")]
    private static partial Regex SubscreenVolume();

    [GeneratedRegex(@"^Located initrd is '(.+)'$")]
    private static partial Regex LocatedInitrd();

    [GeneratedRegex(@"^Adding Windows Recovery tag for '(.+)' on '(.+)'$")]
    private static partial Regex AddingWindowsRecovery();

    public static IReadOnlyList<RefindLogBootCandidate> Parse(string text)
    {
        var results = ParseInternal(text);
        if (results.Count == 0)
            throw new InvalidOperationException("No boot loader entries were found in this log file.");
        return results;
    }

    private static List<RefindLogBootCandidate> ParseInternal(string text)
    {
        var results = new List<RefindLogBootCandidate>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var scanVolume = "";
        PendingLoader? pending = null;

        foreach (var rawLine in text.Split('\n'))
        {
            var line = TimestampPrefix().Replace(rawLine.TrimEnd('\r'), "");
            if (line.Length == 0)
                continue;

            var scanMatch = ScanVolume().Match(line);
            if (scanMatch.Success)
            {
                FinalizePending(results, seen, ref pending);
                scanVolume = scanMatch.Groups[1].Value.Trim();
                continue;
            }

            var addMatch = AddingLoader().Match(line);
            if (addMatch.Success)
            {
                FinalizePending(results, seen, ref pending);
                var title = addMatch.Groups[1].Value.Trim();
                if (title.StartsWith("Boot Options for", StringComparison.OrdinalIgnoreCase))
                    continue;

                pending = new PendingLoader(title, scanVolume);
                continue;
            }

            if (pending is null)
                continue;

            var pathMatch = LoaderPath().Match(line);
            if (pathMatch.Success)
            {
                pending.Loader = RefindPathHelper.NormalizeSlashes(pathMatch.Groups[1].Value.Trim());
                continue;
            }

            var subscreenMatch = SubscreenVolume().Match(line);
            if (subscreenMatch.Success)
            {
                pending.Volume = subscreenMatch.Groups[1].Value.Trim();
                continue;
            }

            var initrdMatch = LocatedInitrd().Match(line);
            if (initrdMatch.Success && string.IsNullOrEmpty(pending.Initrd))
                pending.Initrd = RefindPathHelper.NormalizeSlashes(initrdMatch.Groups[1].Value.Trim());
        }

        FinalizePending(results, seen, ref pending);

        results.Sort(static (a, b) =>
        {
            var vol = string.Compare(a.Volume, b.Volume, StringComparison.OrdinalIgnoreCase);
            return vol != 0 ? vol : string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase);
        });

        return results;
    }

    public static IReadOnlyList<string> CollectVolumes(string? logText, IEnumerable<MenuEntry> menuEntries)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<string>();

        void Add(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;
            var v = value.Trim();
            if (seen.Add(v))
                results.Add(v);
        }

        foreach (var entry in menuEntries)
            Add(entry.GetField("volume"));

        if (string.IsNullOrEmpty(logText))
        {
            results.Sort(StringComparer.OrdinalIgnoreCase);
            return results;
        }

        foreach (var rawLine in logText.Split('\n'))
        {
            var line = TimestampPrefix().Replace(rawLine.TrimEnd('\r'), "");
            if (line.Length == 0)
                continue;

            var scanMatch = ScanVolume().Match(line);
            if (scanMatch.Success)
                Add(scanMatch.Groups[1].Value);
        }

        foreach (var candidate in ParseInternal(logText))
            Add(candidate.Volume);

        results.Sort(StringComparer.OrdinalIgnoreCase);
        return results;
    }

    public static IReadOnlyList<string> CollectFirmwareBootNames(string? logText, IEnumerable<MenuEntry> menuEntries)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<string>();

        void Add(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;
            var v = value.Trim();
            if (seen.Add(v))
                results.Add(v);
        }

        foreach (var entry in menuEntries)
        {
            if (string.IsNullOrWhiteSpace(entry.GetField("firmware_bootnum")))
                continue;
            Add(entry.Title);
        }

        if (string.IsNullOrEmpty(logText))
        {
            results.Sort(StringComparer.OrdinalIgnoreCase);
            return results;
        }

        foreach (var rawLine in logText.Split('\n'))
        {
            var line = TimestampPrefix().Replace(rawLine.TrimEnd('\r'), "");
            if (line.Length == 0)
                continue;

            var fwMatch = AddingFirmwareLoader().Match(line);
            if (fwMatch.Success)
                Add(fwMatch.Groups[1].Value);
        }

        results.Sort(StringComparer.OrdinalIgnoreCase);
        return results;
    }

    public static IReadOnlyList<string> CollectWindowsRecoveryPaths(string? logText, IEnumerable<MenuEntry> menuEntries)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<string>();

        void Add(string? path, string? volume = null)
        {
            var formatted = FormatWindowsRecoveryPath(path, volume);
            if (formatted is null || !seen.Add(formatted))
                return;
            results.Add(formatted);
        }

        foreach (var entry in menuEntries)
        {
            var loader = entry.GetField("loader");
            if (!LooksLikeWindowsRecoveryPath(loader))
                continue;
            Add(loader, entry.GetField("volume"));
        }

        if (string.IsNullOrEmpty(logText))
        {
            results.Sort(StringComparer.OrdinalIgnoreCase);
            return results;
        }

        foreach (var rawLine in logText.Split('\n'))
        {
            var line = TimestampPrefix().Replace(rawLine.TrimEnd('\r'), "");
            if (line.Length == 0)
                continue;

            var recoveryMatch = AddingWindowsRecovery().Match(line);
            if (recoveryMatch.Success)
            {
                Add(recoveryMatch.Groups[1].Value, recoveryMatch.Groups[2].Value);
                continue;
            }
        }

        foreach (var candidate in ParseInternal(logText))
        {
            if (!LooksLikeWindowsRecoveryPath(candidate.Loader))
                continue;
            Add(candidate.Loader, candidate.Volume);
        }

        results.Sort(StringComparer.OrdinalIgnoreCase);
        return results;
    }

    private static string? FormatWindowsRecoveryPath(string? path, string? volume)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var normalized = RefindPathHelper.NormalizeSlashes(path.Trim());
        if (string.IsNullOrWhiteSpace(volume))
            return normalized;

        return $"{volume.Trim()}:{normalized}";
    }

    private static bool LooksLikeWindowsRecoveryPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var lower = RefindPathHelper.NormalizeSlashes(path).ToLowerInvariant();
        return lower.Contains("lrsbootmgr")
            || lower.Contains("winre.wim")
            || lower.Contains("microsoft/boot/recovery")
            || lower.EndsWith("/recoveryagent.efi", StringComparison.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<RefindLogBootCandidate> ForDefaultSelection(
        IEnumerable<MenuEntry> menuEntries,
        IReadOnlyList<RefindLogBootCandidate> fromLog)
    {
        var results = new List<RefindLogBootCandidate>();
        var seenTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in menuEntries)
        {
            if (string.IsNullOrWhiteSpace(entry.Title))
                continue;
            var manual = RefindLogBootCandidate.FromMenuEntry(entry);
            if (!seenTitles.Add(manual.Title))
                continue;
            results.Add(manual);
        }

        foreach (var log in fromLog)
        {
            if (!seenTitles.Add(log.Title))
                continue;
            results.Add(log);
        }

        return results;
    }

    private static void FinalizePending(
        List<RefindLogBootCandidate> results,
        HashSet<string> seen,
        ref PendingLoader? pending)
    {
        if (pending is null || string.IsNullOrEmpty(pending.Loader))
        {
            pending = null;
            return;
        }

        var key = $"{pending.Volume}\0{pending.Loader}";
        if (!seen.Add(key))
        {
            pending = null;
            return;
        }

        results.Add(new RefindLogBootCandidate(
            pending.Title,
            pending.Volume,
            pending.Loader,
            pending.Initrd,
            InferOstype(pending.Loader)));

        pending = null;
    }

    private static string? InferOstype(string loader)
    {
        var lower = loader.ToLowerInvariant();
        if (lower.Contains("bootmgfw") || lower.Contains("bootmgr"))
            return "Windows";
        if (lower.Contains("vmlinuz") || lower.Contains("grub"))
            return "Linux";
        if (lower.Contains("clover"))
            return "MacOS";
        return null;
    }

    private sealed class PendingLoader(string title, string volume)
    {
        public string Title { get; } = title;
        public string Volume { get; set; } = volume;
        public string Loader { get; set; } = "";
        public string? Initrd { get; set; }
    }
}
