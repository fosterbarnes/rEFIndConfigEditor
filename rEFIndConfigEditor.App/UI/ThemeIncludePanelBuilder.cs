using Avalonia.Controls;
using Avalonia.Layout;
using rEFIndConfigEditor.Config;
using rEFIndConfigEditor.Platform;
using rEFIndConfigEditor.Views;

namespace rEFIndConfigEditor.UI;

internal sealed class ThemeIncludePanelHandles
{
    private sealed class ThemeListItem(string path, bool applied)
    {
        public string Path { get; } = path;
        public override string ToString() => applied ? $"{Path} (applied)" : Path;
    }

    private readonly ListBox _found;
    private readonly Func<string?> _getRefindConfPath;
    private bool _suppressDirty;
    private string _appliedPath = "";

    internal Button BrowserButton { get; }
    internal Button AddButton { get; }
    internal Button RemoveButton { get; }
    internal Button ApplyButton { get; }

    public event Action? Changed;
    public event Action<string>? ThemeRemoved;

    internal ThemeIncludePanelHandles(
        ListBox foundList,
        Button browser,
        Button add,
        Button remove,
        Button apply,
        Func<string?> getRefindConfPath)
    {
        _found = foundList;
        BrowserButton = browser;
        AddButton = add;
        RemoveButton = remove;
        ApplyButton = apply;
        _getRefindConfPath = getRefindConfPath;

        browser.Click += async (_, _) => await OpenThemeBrowserAsync().ConfigureAwait(true);
        add.Click += async (_, _) => await AddThemeAsync().ConfigureAwait(true);
        remove.Click += async (_, _) => await RemoveThemeAsync().ConfigureAwait(true);
        apply.Click += (_, _) => ApplySelectedTheme();
    }

    public void LoadFrom(RefindDocument doc)
    {
        _suppressDirty = true;
        var opt = doc.FindGlobal("include");
        _appliedPath = opt is { IsActive: true, Values.Count: > 0 } ? opt.Values[0] : "";
        _suppressDirty = false;
        RefreshFoundThemes();
    }

    public void SaveTo(RefindDocument doc)
    {
        var path = _appliedPath.Trim();
        if (path.Length == 0)
        {
            doc.RemoveGlobal("include");
            return;
        }

        doc.SetGlobal("include", true, [RefindPathHelper.NormalizeSlashes(path)]);
    }

    public void RefreshFoundThemes() => RefreshFoundThemesFromRefindDir();

    public void BringIntoView() => _found.BringIntoView();

    private string? RefindConfPath => _getRefindConfPath();

    private string? RefindDirectory => RefindPathHelper.GetRefindDirectory(RefindConfPath);

    private async Task OpenThemeBrowserAsync()
    {
        var owner = DialogHost.GetOwner();
        if (owner is null)
            return;

        var browser = new ThemeBrowserWindow(_getRefindConfPath);
        browser.ThemeDownloaded += (_, _) => RefreshFoundThemes();
        await browser.ShowDialog(owner).ConfigureAwait(true);
    }

    private void RefreshFoundThemesFromRefindDir()
    {
        var refindDir = RefindDirectory;
        if (refindDir is null)
        {
            SetFoundThemes([]);
            return;
        }

        var themes = Path.Combine(refindDir, "themes");
        if (!Directory.Exists(themes))
        {
            SetFoundThemes([]);
            return;
        }

        SetFoundThemes(RefindPathHelper.FindConfFiles(themes, RefindConfPath));
    }

    private void SetFoundThemes(IReadOnlyList<string> hits)
    {
        var selectedPath = _found.SelectedItem is ThemeListItem selected ? selected.Path : null;
        var items = new List<ThemeListItem>();
        var applied = RefindPathHelper.NormalizeSlashes(_appliedPath.Trim());

        foreach (var hit in hits)
        {
            var normalized = RefindPathHelper.NormalizeSlashes(hit);
            var item = new ThemeListItem(hit, applied.Length > 0 && applied == normalized);
            items.Add(item);
        }

        _found.ItemsSource = items;

        if (selectedPath is not null)
        {
            for (var i = 0; i < items.Count; i++)
            {
                if (string.Equals(items[i].Path, selectedPath, StringComparison.OrdinalIgnoreCase))
                {
                    _found.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    private void RefreshAppliedMarkers()
    {
        if (_found.ItemsSource is not IEnumerable<ThemeListItem> current)
        {
            RefreshFoundThemesFromRefindDir();
            return;
        }

        SetFoundThemes(current.Select(i => i.Path).ToList());
    }

    private async Task AddThemeAsync()
    {
        var picked = await PlatformServices.Current.PickFileAsync(
            "Select theme configuration file",
            "Theme config (*.conf)|*.conf|All files (*.*)|*.*").ConfigureAwait(true);
        if (picked is null)
            return;

        ApplyPickedPath(picked);
        RefreshFoundThemesFromRefindDir();
    }

    private async Task RemoveThemeAsync()
    {
        if (_found.SelectedItem is not ThemeListItem item)
            return;

        var relPath = item.Path;
        var refindDir = RefindDirectory;
        if (refindDir is null)
        {
            PlatformServices.Current.ShowWarning("Remove theme", "Open a refind.conf file first.");
            return;
        }

        if (!await PlatformServices.Current.ConfirmAsync(
                "Remove theme",
                $"Remove this theme?\n\n{relPath}\n\n" +
                "Files are removed from disk immediately. If this theme is in include, it will be removed from the config (save to write refind.conf).")
            .ConfigureAwait(true))
            return;

        var recycleChoice = await PlatformServices.Current.AskYesNoCancelAsync(
            "Remove theme",
            "Move files to Recycle Bin? (No = delete permanently)").ConfigureAwait(true);
        if (recycleChoice is null or YesNoCancelChoice.Cancel)
            return;

        var recycle = recycleChoice == YesNoCancelChoice.Yes;

        var absoluteConf = Path.GetFullPath(Path.Combine(
            refindDir,
            relPath.Replace('/', Path.DirectorySeparatorChar)));
        var themesRoot = Path.GetFullPath(Path.Combine(refindDir, "themes"));
        var themeFolder = Path.GetFullPath(Path.GetDirectoryName(absoluteConf)!);
        if (!themeFolder.StartsWith(themesRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            PlatformServices.Current.ShowWarning("Remove theme", "Theme path is outside the themes folder.");
            return;
        }

        if (!Directory.Exists(themeFolder))
        {
            PlatformServices.Current.ShowWarning("Remove theme", "Theme folder was not found.");
            return;
        }

        var removed = recycle
            ? RecycleBinHelper.TrySendToRecycleBin(themeFolder)
            : RecycleBinHelper.TryDeleteDirectory(themeFolder);

        if (!removed)
        {
            PlatformServices.Current.ShowWarning("Remove theme", "Could not remove the theme folder.");
            return;
        }

        var normalized = RefindPathHelper.NormalizeSlashes(relPath);
        var appliedCleared = RefindPathHelper.NormalizeSlashes(_appliedPath.Trim()) == normalized;
        if (appliedCleared)
        {
            _suppressDirty = true;
            _appliedPath = "";
            _suppressDirty = false;
        }

        ThemeRemoved?.Invoke(relPath);
        if (appliedCleared)
            OnUserChange();
        RefreshFoundThemesFromRefindDir();
    }

    private void ApplySelectedTheme()
    {
        if (_found.SelectedItem is not ThemeListItem item)
            return;

        _suppressDirty = true;
        _appliedPath = item.Path;
        _suppressDirty = false;
        RefreshAppliedMarkers();
        OnUserChange();
    }

    private void ApplyPickedPath(string absolutePath)
    {
        var refindPath = RefindConfPath;
        var resolved = RefindPathHelper.ResolveForRefind(refindPath, absolutePath);

        if (refindPath is not null &&
            RefindPathHelper.ToRefindRelative(refindPath, absolutePath) is null &&
            RefindPathHelper.TryExtractThemesRelative(absolutePath) is null)
        {
            PlatformServices.Current.ShowWarning(
                "Theme path",
                "That file is not under the rEFInd directory for the open refind.conf.\n" +
                "The path was stored as selected; save refind.conf in the rEFInd folder for automatic relative paths.");
        }

        _suppressDirty = true;
        _appliedPath = resolved;
        _suppressDirty = false;
        RefreshAppliedMarkers();
        OnUserChange();
    }

    private void OnUserChange()
    {
        if (_suppressDirty)
            return;
        Changed?.Invoke();
    }
}

internal static class ThemeIncludePanelBuilder
{
    public static (Control Panel, ThemeIncludePanelHandles Handles) Build(Func<string?> getRefindConfPath)
    {
        var browser = new Button
        {
            Content = "Theme browser",
            MinHeight = UiMetrics.ButtonHeight,
            HorizontalAlignment = HorizontalAlignment.Left,
            MinWidth = 168,
        };

        var found = new ListBox
        {
            MinHeight = 140,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Classes = { "setting-input-frame" },
        };

        Button MakeBtn(string text) => new()
        {
            Content = text,
            MinHeight = UiMetrics.BootButtonRowHeight,
            Height = UiMetrics.BootButtonRowHeight,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinWidth = UiMetrics.BootButtonWidth,
        };

        var add = MakeBtn("Add");
        var remove = MakeBtn("Remove");
        var apply = MakeBtn("Apply");

        var buttonCol = new StackPanel
        {
            Spacing = UiMetrics.BootButtonRowGapPx,
            Children = { add, remove, apply },
        };

        var downloadedLabel = new TextBlock
        {
            Text = "Downloaded themes:",
            Classes = { "setting-label" },
            Margin = new Avalonia.Thickness(0, 8, 0, 2),
        };

        var split = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = UiMetrics.BootListButtonGapPx,
            RowDefinitions = new RowDefinitions("Auto,*"),
            Children = { downloadedLabel, found, buttonCol },
        };
        Grid.SetColumnSpan(downloadedLabel, 2);
        Grid.SetRow(found, 1);
        Grid.SetColumn(found, 0);
        Grid.SetRow(buttonCol, 1);
        Grid.SetColumn(buttonCol, 1);

        var stack = new StackPanel
        {
            Spacing = 8,
            Children = { browser, split },
        };

        var handles = new ThemeIncludePanelHandles(found, browser, add, remove, apply, getRefindConfPath);
        return (stack, handles);
    }
}

internal static class DialogHost
{
    public static Window? GetOwner()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }
}
