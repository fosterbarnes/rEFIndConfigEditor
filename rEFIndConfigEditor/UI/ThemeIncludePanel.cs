using rEFIndConfigEditor.Config;
using rEFIndConfigEditor.Forms;

namespace rEFIndConfigEditor.UI;

internal sealed class ThemeIncludePanel : Panel
{
    private sealed class ThemeListItem(string path, bool applied)
    {
        public string Path { get; } = path;

        public override string ToString() => applied ? $"{Path} (applied)" : Path;
    }

    private readonly Button _browserBtn = new() { Text = "Theme browser", AutoSize = false, TextAlign = ContentAlignment.MiddleCenter };
    private readonly Button _add = new() { Text = "Add", AutoSize = false, TextAlign = ContentAlignment.MiddleCenter };
    private readonly Button _remove = new() { Text = "Remove", AutoSize = false, TextAlign = ContentAlignment.MiddleCenter };
    private readonly Button _apply = new() { Text = "Apply", AutoSize = false, TextAlign = ContentAlignment.MiddleCenter };
    private readonly ListBox _found = new()
    {
        IntegralHeight = false,
        SelectionMode = SelectionMode.One,
        ScrollAlwaysVisible = true,
        Dock = DockStyle.Fill
    };
    private readonly Label _foundLabel = new() { Text = "Downloaded themes:", AutoSize = true };
    private readonly Panel _topRow;
    private readonly Panel _downloadedHost;
    private readonly TableLayoutPanel _split;
    private readonly Panel _buttonPanel;

    private Func<string?>? _getRefindConfPath;
    private bool _suppressDirty;
    private string _appliedPath = "";
    private int _dpi = UiMetrics.BaselineDpi;

    public event EventHandler? Changed;
    public event EventHandler<string>? ThemeRemoved;

    public ThemeIncludePanel(int dpi)
    {
        _dpi = dpi;
        SuspendLayout();
        Dock = DockStyle.Top;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = UiMetrics.ScalePadding(8, 4, dpi);

        _topRow = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 0, 0, UiMetrics.Scale(4, dpi))
        };
        _browserBtn.Margin = Padding.Empty;
        _topRow.Controls.Add(_browserBtn);

        _split = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        _split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        _split.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, UiMetrics.BootButtonColumnWidth(dpi)));
        _found.Margin = new Padding(0, 0, UiMetrics.Scale(UiMetrics.BootListButtonGapPx, dpi), 0);
        _split.Controls.Add(_found, 0, 0);

        _buttonPanel = new Panel
        {
            AutoSize = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _buttonPanel.Controls.AddRange([_add, _remove, _apply]);
        _split.Controls.Add(_buttonPanel, 1, 0);

        _foundLabel.Dock = DockStyle.Top;
        _foundLabel.Padding = new Padding(0, UiMetrics.Scale(8, dpi), 0, UiMetrics.Scale(2, dpi));

        _downloadedHost = new Panel { Dock = DockStyle.Top };
        _downloadedHost.Controls.Add(_split);
        _downloadedHost.Controls.Add(_foundLabel);

        Controls.Add(_downloadedHost);
        Controls.Add(_topRow);

        ApplyMetrics(dpi);

        _browserBtn.Click += (_, _) => OpenThemeBrowser();
        _add.Click += (_, _) => AddTheme();
        _remove.Click += (_, _) => RemoveTheme();
        _apply.Click += (_, _) => ApplySelectedTheme();
        ResumeLayout(false);
    }

    public void ApplyMetrics(int dpi)
    {
        _dpi = dpi;
        Margin = UiMetrics.StackedItemMargin(dpi);
        Padding = UiMetrics.ScalePadding(8, 4, dpi);
        _browserBtn.Size = UiMetrics.ScaleSize(168, 32, dpi);
        _foundLabel.Padding = new Padding(0, UiMetrics.Scale(8, dpi), 0, UiMetrics.Scale(2, dpi));
        _downloadedHost.Height = UiMetrics.Scale(168, dpi);
        ApplyButtonLayout(dpi);
        PerformLayout();
    }

    private void ApplyButtonLayout(int dpi)
    {
        var bw = UiMetrics.BootButtonWidth(dpi);
        var bh = UiMetrics.BootButtonRowHeight(dpi);
        var gap = UiMetrics.Scale(UiMetrics.BootButtonRowGapPx, dpi);
        var panelW = UiMetrics.BootButtonColumnWidth(dpi) + UiMetrics.Scale(UiMetrics.BootButtonsPanelChromePx, dpi);
        var buttons = new[] { _add, _remove, _apply };

        for (var i = 0; i < buttons.Length; i++)
        {
            var btn = buttons[i];
            btn.Size = new Size(bw, bh);
            btn.Location = new Point(0, i * (bh + gap));
            btn.Margin = Padding.Empty;
            btn.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        }

        _buttonPanel.Size = new Size(panelW, buttons.Length * bh + (buttons.Length - 1) * gap);
        _split.ColumnStyles[1] = new ColumnStyle(SizeType.Absolute, panelW);
    }

    public void SetRefindConfPathProvider(Func<string?> provider) => _getRefindConfPath = provider;

    public void RefreshFoundThemes() => RefreshFoundThemesFromRefindDir();

    public void LoadFrom(RefindDocument doc)
    {
        _suppressDirty = true;
        var opt = doc.FindGlobal("include");
        _appliedPath = opt is { IsActive: true, Values.Count: > 0 } ? opt.Values[0] : "";
        _suppressDirty = false;
        RefreshFoundThemesFromRefindDir();
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

    private string? RefindConfPath => _getRefindConfPath?.Invoke();

    private string? RefindDirectory => RefindPathHelper.GetRefindDirectory(RefindConfPath);

    private void OpenThemeBrowser()
    {
        using var browser = new ThemeBrowserForm(_dpi, _getRefindConfPath);
        browser.ThemeDownloaded += (_, _) => RefreshFoundThemes();
        browser.ShowDialog(FindForm());
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
        _found.Items.Clear();

        var applied = RefindPathHelper.NormalizeSlashes(_appliedPath.Trim());
        foreach (var hit in hits)
        {
            var normalized = RefindPathHelper.NormalizeSlashes(hit);
            var item = new ThemeListItem(hit, applied.Length > 0 && applied == normalized);
            _found.Items.Add(item);
            if (selectedPath is not null &&
                string.Equals(selectedPath, hit, StringComparison.OrdinalIgnoreCase))
                _found.SelectedItem = item;
        }
    }

    private void RefreshAppliedMarkers()
    {
        var paths = new List<string>();
        foreach (var item in _found.Items)
        {
            if (item is ThemeListItem theme)
                paths.Add(theme.Path);
        }

        if (paths.Count == 0)
            RefreshFoundThemesFromRefindDir();
        else
            SetFoundThemes(paths);
    }

    private void AddTheme()
    {
        var refindDir = RefindDirectory;
        using var dlg = new OpenFileDialog
        {
            Filter = "Theme config (*.conf)|*.conf|All files (*.*)|*.*",
            Title = "Select theme configuration file"
        };
        if (refindDir is not null)
        {
            dlg.InitialDirectory = refindDir;
            var themes = Path.Combine(refindDir, "themes");
            if (Directory.Exists(themes))
                dlg.InitialDirectory = themes;
        }

        if (dlg.ShowDialog(FindForm()) != DialogResult.OK)
            return;

        ApplyPickedPath(dlg.FileName);
        RefreshFoundThemesFromRefindDir();
    }

    private void RemoveTheme()
    {
        if (_found.SelectedItem is not ThemeListItem item)
            return;

        var relPath = item.Path;
        var refindDir = RefindDirectory;
        if (refindDir is null)
        {
            MessageBox.Show(FindForm(),
                "Open a refind.conf file first.",
                "Remove theme",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var choice = MessageBox.Show(FindForm(),
            $"Remove this theme?\n\n{relPath}\n\n" +
            "Files are removed from disk immediately. If this theme is in include, it will be removed from the config (save to write refind.conf).\n\n" +
            "Move files to Recycle Bin?",
            "Remove theme",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question);

        if (choice == DialogResult.Cancel)
            return;

        var absoluteConf = Path.GetFullPath(Path.Combine(
            refindDir,
            relPath.Replace('/', Path.DirectorySeparatorChar)));
        var themesRoot = Path.GetFullPath(Path.Combine(refindDir, "themes"));
        var themeFolder = Path.GetFullPath(Path.GetDirectoryName(absoluteConf)!);
        if (!themeFolder.StartsWith(themesRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(FindForm(),
                "Theme path is outside the themes folder.",
                "Remove theme",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (!Directory.Exists(themeFolder))
        {
            MessageBox.Show(FindForm(),
                "Theme folder was not found.",
                "Remove theme",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var removed = choice switch
        {
            DialogResult.Yes => RecycleBinHelper.TrySendToRecycleBin(themeFolder),
            DialogResult.No => TryDeleteDirectory(themeFolder),
            _ => false
        };

        if (!removed)
        {
            MessageBox.Show(FindForm(),
                "Could not remove the theme folder.",
                "Remove theme",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
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

        ThemeRemoved?.Invoke(this, relPath);
        if (appliedCleared)
            OnUserChange();
        RefreshFoundThemesFromRefindDir();
    }

    private static bool TryDeleteDirectory(string path)
    {
        try
        {
            Directory.Delete(path, recursive: true);
            return true;
        }
        catch
        {
            return false;
        }
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
            MessageBox.Show(FindForm(),
                "That file is not under the rEFInd directory for the open refind.conf.\n" +
                "The path was stored as selected; save refind.conf in the rEFInd folder for automatic relative paths.",
                "Theme path",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
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
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void BringIntoView()
    {
        var parent = Parent;
        while (parent is not null)
        {
            if (parent is ScrollableControl { AutoScroll: true } scrollable)
            {
                scrollable.ScrollControlIntoView(this);
                return;
            }
            parent = parent.Parent;
        }
    }

    public void FocusPrimary() => _browserBtn.Focus();
}
