using rEFIndConfigEditor.Config;
using rEFIndConfigEditor.Forms;
using rEFIndConfigEditor.Models;
using rEFIndConfigEditor.Storage;
using rEFIndConfigEditor.UI;

namespace rEFIndConfigEditor;

internal sealed class MainForm : Form
{
    private readonly Panel _tabArea = new();
    private readonly CenteredTabControl _tabs = new();
    private TabHeaderSearchHost _searchHeader = null!;
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _statusPath = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ToolStripStatusLabel _statusDirty = new() { Text = "" };
    private readonly TextBox _rawText = new()
    {
        Multiline = true,
        AcceptsReturn = true,
        Dock = DockStyle.Fill,
        ScrollBars = ScrollBars.Both,
        WordWrap = false,
        MaxLength = 0
    };
    private readonly ListBox _bootList = new()
    {
        Dock = DockStyle.Fill,
        IntegralHeight = false,
        ScrollAlwaysVisible = true
    };
    private readonly List<OptionBinding> _bindings = [];
    private readonly Dictionary<string, OptionBinding> _bindingByToken = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<OptionCategory, TabPage> _tabByCategory = new();
    private readonly TextBox _searchBox = new();
    private readonly ToolStripDropDown _searchDrop = new()
    {
        AutoClose = false,
        Padding = Padding.Empty,
        DropShadowEnabled = true,
    };
    private readonly ListBox _searchResults = new()
    {
        BorderStyle = BorderStyle.FixedSingle,
        IntegralHeight = false,
        TabStop = false,
    };
    private ToolStripControlHost _searchResultsHost = null!;
    private OptionBinding? _highlighted;
    private readonly List<(Control Ctrl, Font Font)> _highlightOriginalFonts = [];
    private HighlightInteractionFilter? _highlightFilter;
    private ThemeIncludePanel? _themeInclude;
    private AppPreferencesPanel? _appPreferences;
    private AboutPanel? _about;
    private CommentCleanupControls? _commentCleanup;

    private readonly List<(Panel Host, List<OptionBinding> Bindings)> _optionPanels = [];
    private readonly List<Button> _bootButtons = [];
    private TableLayoutPanel? _bootSplit;
    private Panel? _bootButtonPanel;
    private CheckBox? _skipOtherEntriesCheck;
    private GroupBox? _bootSection;
    private readonly List<(Button Button, ToolStripControlHost Host)> _statusFileButtons = [];
    private Button? _applyButton;
    private Button? _rawApplyButton;
    private int _layoutDpi = UiMetrics.BaselineDpi;

    private readonly AppConfigStore _store = new();
    private UiPreferences _uiPrefs = new();
    private TabPage? _rawTabPage;
    private bool _rawRefreshNeeded;
    private readonly System.Windows.Forms.Timer _searchDebounceTimer = new() { Interval = 150 };

    private RefindDocument _document = new();
    private string? _filePath;
    private bool _dirty;
    private bool _suppressDirty;
    private bool _rawEdited;
    private bool _guiEdited;
    private bool _inRawEdit;
    private bool _suppressTabRestore;

    public MainForm()
    {
        AppFormIcon.Apply(this);
        Text = "rEFInd Config Editor";

        BuildMenu();
        BuildSearch();
        BuildStatusStrip();
        _tabs.SuspendLayout();
        try { BuildTabs(); }
        finally { _tabs.ResumeLayout(false); }
        LayoutChrome();
        ClientSize = UiMetrics.ScaleSize(880, 680, DeviceDpi);

        DpiChanged += (_, e) => ApplyUiMetrics(e.DeviceDpiNew);
        Shown += (_, _) =>
        {
            ResyncDwmFrame();
            AlignBootButtonsWithFooter();
        };

        _uiPrefs = _store.LoadUiPreferences();
        if (!TryLoadLastConfOnStartup(_uiPrefs))
            NewDocument();
        _appPreferences?.SetTheme(_uiPrefs.Theme);
        _appPreferences?.SetAutoLoadLastConfOnLaunch(_uiPrefs.AutoLoadLastConfOnLaunch);
        _appPreferences?.SetRememberLastSelectedTab(_uiPrefs.RememberLastSelectedTab);
        ApplyLastSelectedTab(_uiPrefs);
        ApplyUiTheme();
        ApplyStartPosition(_uiPrefs);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        ApplyUiMetrics(DeviceDpi);
    }

    private void ApplyUiMetrics(int dpi)
    {
        _layoutDpi = dpi;
        MinimumSize = UiMetrics.ScaleSize(880, 560, dpi);
        _tabs.ApplyTabMetrics(dpi);
        _searchHeader.ApplyMetrics(dpi);
        _searchHeader.Reposition();
        _rawText.Font = UiMetrics.MonoFont();

        foreach (var (host, bindings) in _optionPanels)
            OptionPanelBuilder.ApplyMetrics(host, bindings, dpi);

        _themeInclude?.ApplyMetrics(dpi);
        _appPreferences?.ApplyMetrics(dpi);
        _about?.ApplyMetrics(dpi);

        if (_bootSplit is not null)
        {
            _bootSplit.ColumnStyles[0] = new ColumnStyle(SizeType.Percent, 100f);
            _bootSplit.ColumnStyles[1] = new ColumnStyle(SizeType.Absolute, UiMetrics.BootButtonsPanelWidth(dpi));
        }

        ApplyBootButtonLayout(dpi);

        if (_rawApplyButton is not null)
            _rawApplyButton.Height = UiMetrics.Scale(32, dpi);

        _status.Padding = UiMetrics.StatusFooterPadding(dpi);
        _status.Height = UiMetrics.StatusFooterHeight(dpi);
        for (var i = 0; i < _statusFileButtons.Count; i++)
        {
            var (btn, host) = _statusFileButtons[i];
            btn.Width = UiMetrics.ToolbarFileButtonWidth(dpi);
            btn.Height = UiMetrics.ButtonHeight(dpi);
            host.AutoSize = false;
            host.Size = btn.Size;
            host.Margin = UiMetrics.StatusFooterButtonHostMargin(dpi, rightmost: i == _statusFileButtons.Count - 1);
        }

        PerformLayout();
        AlignBootButtonsWithFooter();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        AlignBootButtonsWithFooter();
    }

    private void AlignBootButtonsWithFooter()
    {
        if (_bootSection is null || _applyButton is null || _bootButtons.Count < 6 || _statusFileButtons.Count == 0)
            return;

        var openBtn = _statusFileButtons[0].Button;
        var importBtn = _bootButtons[0];
        var moveUpBtn = _bootButtons[5];
        if (!_applyButton.IsHandleCreated || !moveUpBtn.IsHandleCreated)
            return;

        int ClientX(Control c, int x) =>
            PointToClient(c.Parent!.PointToScreen(new Point(x, 0))).X;

        var dRight = ClientX(moveUpBtn, moveUpBtn.Right) - ClientX(_applyButton, _applyButton.Right);
        var dLeft = ClientX(openBtn, openBtn.Left) - ClientX(importBtn, importBtn.Left);

        var p = _bootSection.Padding;
        var newLeft = Math.Max(0, p.Left + dLeft);
        var newRight = Math.Max(0, p.Right + dRight);
        if (newLeft == p.Left && newRight == p.Right)
            return;

        _bootSection.Padding = new Padding(newLeft, p.Top, newRight, p.Bottom);
    }

    private void LayoutChrome()
    {
        _status.Dock = DockStyle.Bottom;
        _tabArea.Dock = DockStyle.Fill;
        _tabs.Dock = DockStyle.Fill;
        _tabs.Multiline = true;

        _tabArea.Controls.Add(_tabs);
        _tabArea.Controls.Add(_searchHeader);
        _searchHeader.BringToFront();
        _searchHeader.Attach(_tabs, _tabArea);

        SuspendLayout();
        Controls.Add(_tabArea);
        Controls.Add(_status);
        ResumeLayout(true);
        PerformLayout();
    }

    private void BuildMenu()
    {
        var file = new ToolStripMenuItem("&File");
        file.DropDownItems.Add("&Open...", null, (_, _) => OpenFile());
        file.DropDownItems.Add("&Apply", null, (_, _) => ApplyChanges());
        file.DropDownItems.Add("Save &As...", null, (_, _) => SaveFileAs());
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add("&New", null, (_, _) => NewDocument());
        file.DropDownItems.Add("E&xit", null, (_, _) => Close());

        var help = new ToolStripMenuItem("&Help");
        help.DropDownItems.Add("rEFInd configuration docs", null, (_, _) =>
            TokenDocLinks.OpenInBrowser("http://rodsbooks.com/refind/configfile.html"));

        MainMenuStrip = new MenuStrip();
        MainMenuStrip.Items.AddRange([file, help]);
    }

    private void GuiThemeChanged(object? sender, EventArgs e)
    {
        _uiPrefs.Theme = GetSelectedTheme();
        PersistUiPreferences();
        ApplyUiTheme();
    }

    private void AppSettingsChanged(object? sender, EventArgs e)
    {
        _uiPrefs.AutoLoadLastConfOnLaunch = _appPreferences!.AutoLoadLastConfOnLaunch;
        _uiPrefs.RememberLastSelectedTab = _appPreferences.RememberLastSelectedTab;
        PersistUiPreferences();
    }

    private void ApplyLastSelectedTab(UiPreferences prefs)
    {
        if (!prefs.RememberLastSelectedTab || prefs.LastSelectedTabIndex is not int idx)
            return;
        if (idx < 0 || idx >= _tabs.TabCount)
            return;

        _suppressTabRestore = true;
        _tabs.SelectedIndex = idx;
        _suppressTabRestore = false;
    }

    private void RememberSelectedTab()
    {
        if (_suppressTabRestore || _appPreferences?.RememberLastSelectedTab != true)
            return;

        _uiPrefs.RememberLastSelectedTab = true;
        _uiPrefs.LastSelectedTabIndex = _tabs.SelectedIndex;
        PersistUiPreferences();
    }

    private bool TryLoadLastConfOnStartup(UiPreferences prefs)
    {
        if (!prefs.AutoLoadLastConfOnLaunch)
            return false;
        if (string.IsNullOrWhiteSpace(prefs.LastConfPath))
            return false;
        if (!File.Exists(prefs.LastConfPath))
        {
            MessageBox.Show(this,
                $"Could not auto-load the last config file:\n\n{prefs.LastConfPath}\n\nThe file was not found.",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return false;
        }
        return LoadFromPath(prefs.LastConfPath);
    }

    private void RememberLastConfPath(string path)
    {
        _uiPrefs.LastConfPath = path;
        PersistUiPreferences();
    }

    private void ClearIncludeIfMatches(string themeConfPath)
    {
        var opt = _document.FindGlobal("include");
        if (opt is not { IsActive: true } || opt.Values.Count == 0)
            return;

        var includePath = RefindPathHelper.NormalizeSlashes(opt.Values[0].Trim());
        var removedPath = RefindPathHelper.NormalizeSlashes(themeConfPath.Trim());
        if (!string.Equals(includePath, removedPath, StringComparison.OrdinalIgnoreCase))
            return;

        _document.RemoveGlobal("include");
        _themeInclude?.LoadFrom(_document);
        SetDirty();
    }

    private void PersistUiPreferences() => _store.SaveUiPreferences(_uiPrefs);

    private void ApplyStartPosition(UiPreferences prefs)
    {
        var saved = TryGetSavedBounds(prefs);
        if (saved is null)
        {
            StartPosition = FormStartPosition.CenterScreen;
            return;
        }

        StartPosition = FormStartPosition.Manual;
        DesktopBounds = saved.Value;
        if (prefs.WindowMaximized)
            WindowState = FormWindowState.Maximized;
    }

    private static Rectangle? TryGetSavedBounds(UiPreferences p)
    {
        if (p.WindowX is null || p.WindowY is null || p.WindowWidth is null || p.WindowHeight is null)
            return null;

        var rect = new Rectangle(p.WindowX.Value, p.WindowY.Value, p.WindowWidth.Value, p.WindowHeight.Value);
        if (rect.Width < 200 || rect.Height < 150)
            return null;

        foreach (var s in Screen.AllScreens)
            if (s.WorkingArea.IntersectsWith(rect))
                return rect;
        return null;
    }

    private void SaveWindowState()
    {
        try
        {
            _uiPrefs.Theme = GetSelectedTheme();
            var b = WindowState == FormWindowState.Normal ? DesktopBounds : RestoreBounds;
            _uiPrefs.WindowX = b.X;
            _uiPrefs.WindowY = b.Y;
            _uiPrefs.WindowWidth = b.Width;
            _uiPrefs.WindowHeight = b.Height;
            _uiPrefs.WindowMaximized = WindowState == FormWindowState.Maximized;
            _uiPrefs.AutoLoadLastConfOnLaunch = _appPreferences?.AutoLoadLastConfOnLaunch ?? _uiPrefs.AutoLoadLastConfOnLaunch;
            _uiPrefs.RememberLastSelectedTab = _appPreferences?.RememberLastSelectedTab ?? _uiPrefs.RememberLastSelectedTab;
            if (_uiPrefs.RememberLastSelectedTab)
                _uiPrefs.LastSelectedTabIndex = _tabs.SelectedIndex;
            PersistUiPreferences();
        }
        catch
        {
        }
    }

    private UiThemeKind GetSelectedTheme()
    {
        return _appPreferences?.SelectedTheme ?? UiThemeKind.System;
    }

    private void ApplyUiTheme()
    {
        UiThemeKind t = GetSelectedTheme();
        UiTheme.Apply(this, t, _rawText);
        UiTheme.ApplyThemedChildChrome(this, t);
        UiTheme.Apply(_searchResults, t, null);
        _searchHeader.SyncHeaderColors(_tabs);
    }

    private void ResyncDwmFrame()
    {
        UiThemeKind t = GetSelectedTheme();
        UiTheme.ApplyWindowFrame(this, t);
        UiTheme.ApplyThemedChildChrome(this, t);
    }

    private DialogResult ShowThemedDialog(Form form)
    {
        UiTheme.Apply(form, GetSelectedTheme(), null);
        return form.ShowDialog(this);
    }

    private void BuildSearch()
    {
        _searchBox.PlaceholderText = "Search";

        _searchResults.DisplayMember = nameof(SettingSearchHit.DisplayText);
        _searchResults.Click += (_, _) => PickResult();
        _searchResultsHost = new ToolStripControlHost(_searchResults)
        {
            AutoSize = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        _searchDrop.Items.Add(_searchResultsHost);

        _searchDebounceTimer.Tick += (_, _) =>
        {
            _searchDebounceTimer.Stop();
            UpdateSearch();
        };
        _searchBox.TextChanged += (_, _) =>
        {
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        };
        _searchBox.LostFocus += (_, _) => BeginInvoke(() =>
        {
            if (IsDisposed || Disposing)
                return;
            if (!_searchResults.Focused)
                _searchDrop.Close();
        });
        _searchBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                _searchDrop.Close();
                e.Handled = true;
            }
        };

        _searchHeader = new TabHeaderSearchHost(_searchBox);
        _searchHeader.ApplyMetrics(_layoutDpi);
        _searchHeader.SyncHeaderColors(_tabs);
    }

    private void BuildStatusStrip()
    {
        _status.SizingGrip = false;
        _status.Items.AddRange([_statusPath, _statusDirty]);
        AddStatusFileButton("Open", (_, _) => OpenFile(), out _);
        AddStatusFileButton("Save As", (_, _) => SaveFileAs(), out _);
        AddStatusFileButton("Apply", (_, _) => ApplyChanges(), out _applyButton);
        AcceptButton = _applyButton;
    }

    private void AddStatusFileButton(string text, EventHandler click, out Button button)
    {
        button = new Button
        {
            Text = text,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
        };
        button.Click += click;
        var host = new ToolStripControlHost(button)
        {
            Alignment = ToolStripItemAlignment.Right,
            AutoSize = false,
        };
        _statusFileButtons.Add((button, host));
        _status.Items.Add(host);
    }

    private void BuildTabs()
    {
        AddGeneralTab();
        AddOptionTab("Display", OptionCategory.Display);
        AddThemeTab();
        AddOptionTab("Input", OptionCategory.Input);
        AddOptionTab("Scanning", OptionCategory.Scanning);
        AddAdvancedTab();
        AddAppTab();
        AddAboutTab();
        AddRawTab();
        _tabs.SelectedIndexChanged += (_, _) =>
        {
            ClearHighlight();
            RememberSelectedTab();
            FlushRawRefreshIfNeeded();
        };
    }

    private void AddGeneralTab()
    {
        var (panel, bindings) = OptionPanelBuilder.Build(OptionCategory.General, _layoutDpi);
        _optionPanels.Add((panel, bindings));
        _bindings.AddRange(bindings);
        RegisterBindings(bindings);
        panel.AutoScroll = false;
        panel.AutoSize = true;
        panel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        panel.Dock = DockStyle.Top;

        var bootSection = BuildBootEntriesSection(_layoutDpi);
        bootSection.Dock = DockStyle.Fill;

        var pad = UiMetrics.Scale(4, _layoutDpi);
        var page = new TabPage("General") { Padding = new Padding(pad) };
        page.Controls.Add(bootSection);
        page.Controls.Add(panel);
        _tabs.TabPages.Add(page);
        _tabByCategory[OptionCategory.General] = page;
    }

    private void AddOptionTab(string title, OptionCategory category)
    {
        var (panel, bindings) = OptionPanelBuilder.Build(category, _layoutDpi);
        _optionPanels.Add((panel, bindings));
        _bindings.AddRange(bindings);
        RegisterBindings(bindings);
        var pad = UiMetrics.Scale(4, _layoutDpi);
        var page = new TabPage(title) { Padding = new Padding(pad) };
        page.Controls.Add(panel);
        _tabs.TabPages.Add(page);
        _tabByCategory[category] = page;
    }

    private void AddAdvancedTab()
    {
        var page = new TabPage("Other") { Padding = new Padding(UiMetrics.Scale(4, _layoutDpi)) };

        var (panel, bindings, cleanup) = OptionPanelBuilder.BuildAdvanced(_layoutDpi);
        _optionPanels.Add((panel, bindings));
        _commentCleanup = cleanup;
        _commentCleanup.ClearNow.Click += (_, _) => ClearCommentsNow();

        _bindings.AddRange(bindings);
        RegisterBindings(bindings);

        panel.Dock = DockStyle.Fill;
        page.Controls.Add(panel);
        _tabs.TabPages.Add(page);
        _tabByCategory[OptionCategory.Advanced] = page;
    }

    private void AddThemeTab()
    {
        var page = new TabPage("Theme") { Padding = new Padding(UiMetrics.Scale(4, _layoutDpi)) };

        _themeInclude = new ThemeIncludePanel(_layoutDpi);
        _themeInclude.SetRefindConfPathProvider(() => _filePath);
        _themeInclude.Changed += (_, _) => SetDirty();
        _themeInclude.ThemeRemoved += (_, path) => ClearIncludeIfMatches(path);

        var (panel, bindings) = OptionPanelBuilder.Build(OptionCategory.Theme, _layoutDpi);
        _optionPanels.Add((panel, bindings));
        _bindings.AddRange(bindings);
        RegisterBindings(bindings);

        panel.Dock = DockStyle.Fill;
        page.Controls.Add(panel);
        page.Controls.Add(_themeInclude);
        _tabs.TabPages.Add(page);
        _tabByCategory[OptionCategory.Theme] = page;
    }

    private void AddAppTab()
    {
        var page = new TabPage("App") { Padding = new Padding(UiMetrics.Scale(4, _layoutDpi)) };

        _appPreferences = new AppPreferencesPanel(_layoutDpi);
        _appPreferences.ThemeChanged += GuiThemeChanged;
        _appPreferences.AutoLoadLastConfChanged += AppSettingsChanged;
        _appPreferences.RememberLastTabChanged += AppSettingsChanged;

        page.Controls.Add(_appPreferences);
        _tabs.TabPages.Add(page);
    }

    private void AddAboutTab()
    {
        var page = new TabPage("About") { Padding = new Padding(UiMetrics.Scale(4, _layoutDpi)) };
        _about = new AboutPanel(_layoutDpi);
        page.Controls.Add(_about);
        _tabs.TabPages.Add(page);
    }

    private GroupBox BuildBootEntriesSection(int dpi)
    {
        var group = new GroupBox
        {
            Text = "Boot entries",
            Dock = DockStyle.Fill,
            Padding = UiMetrics.ScalePadding(8, 6, dpi)
        };

        var split = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, UiMetrics.BootButtonsPanelWidth(dpi)));
        _bootList.Margin = new Padding(0, 0, UiMetrics.Scale(UiMetrics.BootListButtonGapPx, dpi), 0);
        split.Controls.Add(_bootList, 0, 0);
        _bootSplit = split;

        _bootButtonPanel = new Panel
        {
            AutoSize = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        PopulateBootButtons(_bootButtonPanel);
        ApplyBootButtonLayout(dpi);
        split.Controls.Add(_bootButtonPanel, 1, 0);
        group.Controls.Add(split);
        _bootSection = group;
        return group;
    }

    private void ApplyBootButtonLayout(int dpi)
    {
        if (_bootButtonPanel is null)
            return;
        BootButtonsLayout.Apply(_bootButtonPanel, _bootButtons, _skipOtherEntriesCheck, _bootSplit, dpi);
    }

    private void PopulateBootButtons(Panel host)
    {
        if (_bootButtons.Count > 0)
            return;

        void AddBtn(string text, EventHandler click)
        {
            var b = new Button
            {
                Text = text,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Margin = Padding.Empty,
                UseVisualStyleBackColor = true
            };
            b.Click += click;
            _bootButtons.Add(b);
            host.Controls.Add(b);
        }

        AddBtn("Import from log", (_, _) => ImportBootFromLog());
        AddBtn("Add", (_, _) => EditBootEntry(null));
        AddBtn("Edit", (_, _) => EditSelectedBoot());
        AddBtn("Duplicate", (_, _) => DuplicateBoot());
        AddBtn("Remove", (_, _) => RemoveBoot());
        AddBtn("Move up", (_, _) => MoveBoot(-1));
        AddBtn("Move down", (_, _) => MoveBoot(1));

        _skipOtherEntriesCheck = new CheckBox
        {
            Text = "Skip all other entries",
            AutoSize = false,
            Margin = Padding.Empty,
            UseVisualStyleBackColor = true
        };
        _skipOtherEntriesCheck.CheckedChanged += (_, _) => SetDirty();
        host.Controls.Add(_skipOtherEntriesCheck);
    }

    private void AddRawTab()
    {
        var page = new TabPage("Raw .conf");
        _rawTabPage = page;
        var apply = new Button { Text = "Apply from raw", Dock = DockStyle.Top, Height = UiMetrics.Scale(32, _layoutDpi) };
        _rawApplyButton = apply;
        apply.Click += (_, _) => ApplyRaw();
        _rawText.TextChanged += (_, _) =>
        {
            if (_suppressDirty)
                return;
            _rawEdited = true;
            _inRawEdit = true;
            try { SetDirty(); }
            finally { _inRawEdit = false; }
        };
        page.Controls.Add(_rawText);
        page.Controls.Add(apply);
        _tabs.TabPages.Add(page);
    }

    private void RegisterBindings(IEnumerable<OptionBinding> bindings)
    {
        foreach (var binding in bindings)
        {
            _bindingByToken[binding.Definition.Token] = binding;
            binding.UseCheck.CheckedChanged += (_, _) => SetDirty();
            if (binding.Definition.Kind != OptionControlKind.Boolean)
            {
                if (binding.ValueControl is NumericUpDown nud)
                {
                    nud.ValueChanged += (_, _) =>
                    {
                        binding.PreservedInvalidNumeric = null;
                        SetDirty();
                    };
                }
                else
                    WireValueDirty(binding.ValueControl);
            }
            if (binding.LogPickButton is not null)
            {
                if (LogPickerController.TryGet(binding.Definition.Token, out _))
                    binding.LogPickButton.Click += (_, _) => ChooseListFromLog(binding);
                else
                    binding.LogPickButton.Click += (_, _) => ChooseDefaultFromLog(binding);
            }
            if (binding.FolderAddButton is not null)
                binding.FolderAddButton.Click += (_, _) => AddFolderToOption(binding);
            if (binding.FileAddButton is not null)
                binding.FileAddButton.Click += (_, _) => AddFileToOption(binding);
            if (binding.BrowseButton is not null)
                binding.BrowseButton.Click += (_, _) => BrowseThemeOption(binding);
        }
    }

    private void AddFileToOption(OptionBinding binding)
    {
        var refindDir = RefindPathHelper.GetRefindDirectory(_filePath);
        using var dlg = new OpenFileDialog
        {
            Title = binding.Definition.Token == "dont_scan_files"
                ? "Select boot loader files to hide"
                : "Select tool files to hide",
            Filter = "EFI programs (*.efi)|*.efi|All files (*.*)|*.*",
            Multiselect = true
        };
        if (refindDir is not null && Directory.Exists(refindDir))
            dlg.InitialDirectory = refindDir;

        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        var paths = dlg.FileNames
            .Select(f => RefindScanFileHelper.FormatPickedFile(f, _filePath))
            .ToList();
        if (paths.Count == 0)
            return;

        var tb = (TextBox)binding.ValueControl;
        tb.Text = RefindScanFileHelper.AppendFilesToCommaList(tb.Text, paths);
        binding.UseCheck.Checked = true;
        SetDirty();
    }

    private void AddFolderToOption(OptionBinding binding)
    {
        var refindDir = RefindPathHelper.GetRefindDirectory(_filePath);
        using var dlg = new FolderBrowserDialog
        {
            Description = "Select a folder on the EFI system partition (or volume root)",
            UseDescriptionForTitle = true
        };
        if (refindDir is not null && Directory.Exists(refindDir))
            dlg.SelectedPath = refindDir;

        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        var path = RefindScanPathHelper.FormatPickedFolder(dlg.SelectedPath, _filePath);
        var tb = (TextBox)binding.ValueControl;
        tb.Text = RefindScanPathHelper.AppendToCommaList(tb.Text, path);
        binding.UseCheck.Checked = true;
        SetDirty();
    }

    private void BrowseThemeOption(OptionBinding binding)
    {
        var token = binding.Definition.Token;
        var tb = (TextBox)binding.ValueControl;

        if (RefindThemePathHelper.IsIconsDirToken(token))
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = "Select a custom icons folder under the rEFInd directory",
                UseDescriptionForTitle = true
            };
            var initial = RefindThemePathHelper.ResolveIconsDirBrowsePath(_filePath, tb.Text);
            if (initial is not null && Directory.Exists(initial))
                dlg.SelectedPath = initial;

            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            tb.Text = RefindThemePathHelper.FormatPickedIconsDir(dlg.SelectedPath, _filePath);
            binding.UseCheck.Checked = true;
            SetDirty();
            return;
        }

        if (!RefindThemePathHelper.IsThemeFileToken(token))
            return;

        var title = token switch
        {
            "banner" => "Select banner image",
            "font" => "Select menu font",
            "selection_big" => "Select OS selection highlight image",
            "selection_small" => "Select tool selection highlight image",
            _ => "Select image file"
        };

        using var fileDlg = new OpenFileDialog
        {
            Title = title,
            Filter = token == "font"
                ? RefindThemePathHelper.FontFileDialogFilter
                : RefindIconHelper.IconFileDialogFilter
        };
        var refindDir = RefindPathHelper.GetRefindDirectory(_filePath);
        if (refindDir is not null && Directory.Exists(refindDir))
            fileDlg.InitialDirectory = refindDir;

        if (fileDlg.ShowDialog(this) != DialogResult.OK)
            return;

        tb.Text = RefindThemePathHelper.FormatPickedThemeFile(fileDlg.FileName, _filePath);
        binding.UseCheck.Checked = true;
        SetDirty();
    }

    private void WireValueDirty(Control value)
    {
        switch (value)
        {
            case CheckBox cb:
                cb.CheckedChanged += (_, _) => SetDirty();
                break;
            case NumericUpDown nud:
                nud.ValueChanged += (_, _) => SetDirty();
                break;
            case ComboBox combo:
                combo.SelectedIndexChanged += (_, _) => SetDirty();
                combo.TextChanged += (_, _) => SetDirty();
                break;
            case CheckedListBox clb:
                clb.ItemCheck += (_, _) => SetDirty();
                break;
            case TextBox tb:
                tb.TextChanged += (_, _) => SetDirty();
                break;
        }
    }

    private void FocusSearchBox()
    {
        _searchBox.Focus();
        _searchBox.SelectAll();
    }

    private void UpdateSearch()
    {
        var q = _searchBox.Text;
        if (string.IsNullOrWhiteSpace(q))
        {
            _searchDrop.Close();
            return;
        }

        var hits = SettingSearch.Find(q);
        if (hits.Count == 0)
        {
            _searchDrop.Close();
            return;
        }

        _searchResults.BeginUpdate();
        _searchResults.Items.Clear();
        foreach (var h in hits)
            _searchResults.Items.Add(h);
        _searchResults.SelectedIndex = -1;
        _searchResults.EndUpdate();

        var itemHeight = Math.Max(_searchResults.ItemHeight, 16);
        int w = Math.Max(_searchBox.Width, UiMetrics.Scale(360, _layoutDpi));
        int h2 = Math.Min(UiMetrics.Scale(240, _layoutDpi), hits.Count * itemHeight + 4);
        _searchResultsHost.Size = new Size(w, h2);

        if (!_searchDrop.Visible)
            _searchDrop.Show(_searchBox, new Point(0, _searchBox.Height));
    }

    private void PickResult()
    {
        if (_searchResults.SelectedItem is not SettingSearchHit hit)
            return;

        _searchDrop.Close();
        _searchBox.Text = "";
        NavigateToSetting(hit.Token);
    }

    private void NavigateToSetting(string token)
    {
        if (string.Equals(token, "include", StringComparison.OrdinalIgnoreCase))
        {
            SelectTab(OptionCategory.Theme);
            _themeInclude?.BringIntoView();
            _themeInclude?.FocusPrimary();
            return;
        }

        if (!_bindingByToken.TryGetValue(token, out var binding))
            return;

        SelectTab(binding.Definition.Category);
        ScrollRowIntoView(binding);
        binding.UseCheck.Focus();
        HighlightBinding(binding);
    }

    private void HighlightBinding(OptionBinding binding)
    {
        ClearHighlight();
        if (binding.RowAnchor is null)
            return;

        _highlighted = binding;

        BoldControl(binding.UseCheck);
        foreach (var lbl in FindLabels(binding.RowAnchor))
            BoldControl(lbl);

        _highlightFilter = new HighlightInteractionFilter(ClearHighlight);
        Application.AddMessageFilter(_highlightFilter);
    }

    private void BoldControl(Control c)
    {
        var original = c.Font;
        if (original.Style.HasFlag(FontStyle.Bold))
            return;

        _highlightOriginalFonts.Add((c, original));
        c.Font = new Font(original, original.Style | FontStyle.Bold);
    }

    private void ClearHighlight()
    {
        if (_highlightFilter is not null)
        {
            Application.RemoveMessageFilter(_highlightFilter);
            _highlightFilter = null;
        }
        foreach (var (ctrl, font) in _highlightOriginalFonts)
        {
            if (ctrl.IsDisposed)
                continue;
            var current = ctrl.Font;
            if (!ReferenceEquals(current, font))
                current.Dispose();
            ctrl.Font = font;
        }
        _highlightOriginalFonts.Clear();
        _highlighted = null;
    }

    private static IEnumerable<Label> FindLabels(Control root)
    {
        foreach (Control c in root.Controls)
        {
            if (c is Label l)
                yield return l;
            if (c.HasChildren)
                foreach (var inner in FindLabels(c))
                    yield return inner;
        }
    }

    private void SelectTab(OptionCategory category)
    {
        if (_tabByCategory.TryGetValue(category, out var page))
            _tabs.SelectedTab = page;
    }

    private void ScrollRowIntoView(OptionBinding binding)
    {
        if (binding.ScrollHost is null || binding.RowAnchor is null)
            return;

        var y = 0;
        Control? ctrl = binding.RowAnchor;
        while (ctrl is not null && !ReferenceEquals(ctrl, binding.ScrollHost))
        {
            y += ctrl.Top;
            ctrl = ctrl.Parent;
        }

        binding.ScrollHost.AutoScrollPosition = new Point(0, Math.Max(0, y - UiMetrics.Scale(12, _layoutDpi)));
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.F))
        {
            FocusSearchBox();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void NewDocument()
    {
        if (!ConfirmDiscard())
            return;
        _document = new RefindDocument();
        _document.Rows.Add(new RawConfigRow("# rEFInd configuration — open refind.conf or edit below"));
        _filePath = null;
        RefreshUiFromDocument();
        SetDirty(false);
    }

    private void OpenFile()
    {
        if (!ConfirmDiscard())
            return;
        using var dlg = new OpenFileDialog
        {
            Filter = "rEFInd config (refind.conf)|refind.conf|All files (*.*)|*.*",
            Title = "Open refind.conf"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;
        LoadFromPath(dlg.FileName);
    }

    private bool LoadFromPath(string path)
    {
        try
        {
            var text = SafeFileIO.ReadAllText(path, SafeFileIO.MaxConfigBytes);
            if (!RefindStructureValidator.TryValidate(text, out var structureErr))
            {
                MessageBox.Show(this,
                    structureErr + "\n\nThe file was loaded, but fix structure errors before saving.",
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            _document = RefindParser.Parse(text);
            _filePath = path;
            RefreshUiFromDocument();
            SetDirty(false);
            RememberLastConfPath(path);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                $"Could not open this file as a rEFInd config:\n\n{ex.Message}",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }
    }

    private void ApplyChanges()
    {
        if (string.IsNullOrEmpty(_filePath))
        {
            SaveFileAs();
            return;
        }

        SaveToPath(_filePath);
    }

    private void SaveFileAs()
    {
        using var dlg = new SaveFileDialog
        {
            Filter = "rEFInd config (refind.conf)|refind.conf|All files (*.*)|*.*",
            FileName = "refind.conf",
            Title = "Save refind.conf as"
        };
        if (!string.IsNullOrEmpty(_filePath))
        {
            dlg.FileName = Path.GetFileName(_filePath);
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir))
                dlg.InitialDirectory = dir;
        }

        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;
        SaveToPath(dlg.FileName);
    }

    private void SaveToPath(string path)
    {
        if (_rawEdited && _guiEdited)
        {
            var r = MessageBox.Show(this,
                "You have unapplied edits in BOTH the Raw tab and the GUI tabs.\n\n" +
                "Yes  — apply Raw (discard GUI edits)\n" +
                "No   — apply GUI (discard Raw edits)\n" +
                "Cancel — don't save",
                Text,
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);
            switch (r)
            {
                case DialogResult.Yes:
                    if (!TryParseRawIntoDocument())
                        return;
                    break;
                case DialogResult.No:
                    ApplyDocumentFromUi();
                    break;
                default:
                    return;
            }
        }
        else if (_rawEdited)
        {
            if (!TryParseRawIntoDocument())
                return;
        }
        else
            ApplyDocumentFromUi();
        if (_commentCleanup?.StripOnApply.Checked == true)
            RefindDocumentCleaner.StripComments(_document);
        _document.CollapseDuplicateGlobals();
        if (!RefindValidator.TryValidate(_document, out var err))
        {
            MessageBox.Show(this, err, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var text = RefindWriter.Write(_document);
        if (!RefindStructureValidator.TryValidate(text, out var structureErr))
        {
            MessageBox.Show(this, structureErr, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            AtomicFile.WriteAllBytes(path, new System.Text.UTF8Encoding(false).GetBytes(text));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                $"Could not save this file:\n\n{ex.Message}",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }
        _filePath = path;
        SetDirty(false);
        _suppressDirty = true;
        RefreshUiFromDocument();
        _suppressDirty = false;
        RememberLastConfPath(path);
    }

    private void ClearCommentsNow()
    {
        if (_rawEdited && _guiEdited)
        {
            var r = MessageBox.Show(this,
                "You have unapplied edits in BOTH the Raw tab and the GUI tabs.\n\n" +
                "Yes  — apply Raw (discard GUI edits)\n" +
                "No   — apply GUI (discard Raw edits)\n" +
                "Cancel — don't clear comments",
                Text,
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);
            switch (r)
            {
                case DialogResult.Yes:
                    if (!TryParseRawIntoDocument())
                        return;
                    break;
                case DialogResult.No:
                    ApplyDocumentFromUi();
                    break;
                default:
                    return;
            }
        }
        else if (_rawEdited)
        {
            if (!TryParseRawIntoDocument())
                return;
        }
        else
            ApplyDocumentFromUi();

        RefindDocumentCleaner.StripComments(_document);
        _rawEdited = false;
        _guiEdited = false;
        RefreshUiFromDocument();
        SetDirty(true);
    }

    private void ApplyDocumentFromUi()
    {
        _themeInclude?.SaveTo(_document);
        OptionPanelBuilder.SaveBindings(_bindings, _document);
        ApplySkipOtherEntriesFromUi();
    }

    private void RefreshUiFromDocument()
    {
        _suppressDirty = true;
        _themeInclude?.LoadFrom(_document);
        OptionPanelBuilder.LoadBindings(_bindings, _document);
        RefreshSkipOtherEntriesFromDocument();
        RefreshBootList();
        _rawRefreshNeeded = true;
        if (IsRawTabSelected())
        {
            RefreshRawOnly();
            _rawRefreshNeeded = false;
        }
        UpdateStatus();
        _suppressDirty = false;
    }

    private void RefreshBootList()
    {
        _bootList.Items.Clear();
        foreach (var e in _document.MenuEntries)
            _bootList.Items.Add(e.Summary);
    }

    private void ApplySkipOtherEntriesFromUi()
    {
        if (_skipOtherEntriesCheck?.Checked == true)
        {
            _document.SetGlobal("scanfor", true, ["manual"]);
            return;
        }

        if (IsManualOnlyScanfor(_document.FindGlobal("scanfor")))
            _document.RemoveGlobal("scanfor");
    }

    private void RefreshSkipOtherEntriesFromDocument()
    {
        if (_skipOtherEntriesCheck is null)
            return;

        _skipOtherEntriesCheck.Checked = IsManualOnlyScanfor(_document.FindGlobal("scanfor"));
    }

    private static bool IsManualOnlyScanfor(GlobalOption? opt) =>
        opt is { IsActive: true } &&
        opt.Values.Count == 1 &&
        opt.Values[0].Equals("manual", StringComparison.OrdinalIgnoreCase);

    private bool IsRawTabSelected() => _rawTabPage is not null && _tabs.SelectedTab == _rawTabPage;

    private void FlushRawRefreshIfNeeded()
    {
        if (!_rawRefreshNeeded || !IsRawTabSelected())
            return;

        RefreshRawOnly();
        _rawRefreshNeeded = false;
    }

    private void RefreshRawOnly()
    {
        _suppressDirty = true;
        try
        {
            _rawText.Text = ToRawEditorText(RefindWriter.Write(_document));
            _rawEdited = false;
        }
        finally
        {
            _suppressDirty = false;
        }
    }

    private static string ToRawEditorText(string lfText) =>
        lfText.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", Environment.NewLine);

    private void ApplyRaw()
    {
        if (_guiEdited)
        {
            var r = MessageBox.Show(this,
                "You have unapplied edits in the GUI tabs.\n\n" +
                "Applying from raw will discard those GUI edits.\n\nContinue?",
                Text,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (r != DialogResult.Yes)
                return;
        }

        if (!TryParseRawIntoDocument())
            return;
        RefreshUiFromDocument();
        SetDirty(true);
    }

    private bool TryParseRawIntoDocument()
    {
        var raw = _rawText.Text;
        if (!RefindStructureValidator.TryValidate(raw, out var structureErr))
        {
            MessageBox.Show(this, structureErr, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        try
        {
            _document = RefindParser.Parse(raw);
            _rawEdited = false;
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Could not parse config:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private void EditBootEntry(MenuEntry? entry)
    {
        var isNew = entry is null || !_document.MenuEntries.Contains(entry);
        using var dlg = new BootEntryEditorForm(entry, () => _filePath, () => _document);
        if (ShowThemedDialog(dlg) != DialogResult.OK)
            return;

        if (isNew)
            _document.Rows.Add(new MenuEntryRow(dlg.Entry));
        RefreshBootList();
        SetDirty();
    }

    private void ImportBootFromLog()
    {
        // Log-discovered loaders only; does not list existing menuentry rows.
        var logPath = RefindLogPicker.PickLogPath(_filePath, this);
        if (logPath is null)
            return;

        try
        {
            var candidates = RefindLogParser.Parse(RefindLogIO.ReadText(logPath));
            using var picker = new RefindLogImportForm(candidates);
            if (ShowThemedDialog(picker) != DialogResult.OK)
                return;

            var selected = picker.SelectedCandidates;
            if (selected.Count == 0)
                return;

            if (selected.Count == 1)
            {
                EditBootEntry(selected[0].ToMenuEntry());
                return;
            }

            foreach (var c in selected)
                _document.Rows.Add(new MenuEntryRow(c.ToMenuEntry()));
            RefreshBootList();
            SetDirty();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ChooseListFromLog(OptionBinding binding)
    {
        if (!LogPickerController.TryGet(binding.Definition.Token, out var picker))
            return;

        var selected = LogPickerController.PickItems(this, picker, _filePath, _document.MenuEntries, ShowThemedDialog);
        if (selected.Count == 0)
            return;

        var tb = (TextBox)binding.ValueControl;
        tb.Text = RefindWriter.AppendCommaList(tb.Text, selected);
        binding.UseCheck.Checked = true;
        SetDirty();
    }

    private void ChooseDefaultFromLog(OptionBinding binding)
    {
        // default_selection picker: config menuentry rows first, then log scan.
        var menuEntries = _document.MenuEntries.ToList();
        IReadOnlyList<RefindLogBootCandidate> fromLog = [];

        var logPath = RefindLogPicker.PickLogPath(_filePath, this);
        if (logPath is not null)
        {
            try
            {
                fromLog = RefindLogParser.Parse(RefindLogIO.ReadText(logPath));
            }
            catch (Exception ex)
            {
                if (menuEntries.Count == 0)
                {
                    MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }
        else if (menuEntries.Count == 0)
            return;

        var candidates = RefindLogParser.ForDefaultSelection(menuEntries, fromLog);
        if (candidates.Count == 0)
        {
            MessageBox.Show(this, "No boot entries found in the config or log.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var picker = new RefindLogImportForm(candidates, RefindLogImportPurpose.DefaultSelection);
        if (ShowThemedDialog(picker) != DialogResult.OK)
            return;

        var selected = picker.SelectedCandidates;
        if (selected.Count != 1)
            return;

        binding.UseCheck.Checked = true;
        ((TextBox)binding.ValueControl).Text = selected[0].Title;
        SetDirty();
    }

    private void EditSelectedBoot()
    {
        if (_bootList.SelectedIndex < 0)
            return;
        var entries = _document.MenuEntries.ToList();
        EditBootEntry(entries[_bootList.SelectedIndex]);
    }

    private void RemoveBoot()
    {
        if (_bootList.SelectedIndex < 0)
            return;
        var entry = _document.MenuEntries.ElementAt(_bootList.SelectedIndex);
        _document.Rows.RemoveAll(r => r is MenuEntryRow m && ReferenceEquals(m.Entry, entry));
        RefreshBootList();
        SetDirty();
    }

    private void MoveBoot(int delta)
    {
        var i = _bootList.SelectedIndex;
        if (i < 0)
            return;
        var j = i + delta;
        var menuRows = _document.Rows.OfType<MenuEntryRow>().ToList();
        if (j < 0 || j >= menuRows.Count)
            return;
        var all = _document.Rows.ToList();
        var idxA = all.IndexOf(menuRows[i]);
        var idxB = all.IndexOf(menuRows[j]);
        (all[idxA], all[idxB]) = (all[idxB], all[idxA]);
        _document.Rows.Clear();
        _document.Rows.AddRange(all);
        RefreshBootList();
        _bootList.SelectedIndex = j;
        SetDirty();
    }

    private void DuplicateBoot()
    {
        if (_bootList.SelectedIndex < 0)
            return;
        var src = _document.MenuEntries.ElementAt(_bootList.SelectedIndex);
        var copy = CloneEntry(src);
        _document.Rows.Add(new MenuEntryRow(copy));
        RefreshBootList();
        SetDirty();
    }

    private static MenuEntry CloneEntry(MenuEntry src)
    {
        var e = new MenuEntry { Title = src.Title + " (copy)", Disabled = src.Disabled };
        foreach (var (k, v) in src.Fields)
            e.Fields[k] = v.Copy();
        e.Trailers.AddRange(src.Trailers);
        foreach (var sub in src.Submenus)
        {
            var s = new SubmenuEntry { Title = sub.Title, Disabled = sub.Disabled };
            foreach (var (k, v) in sub.Fields)
                s.Fields[k] = v.Copy();
            s.Trailers.AddRange(sub.Trailers);
            e.Submenus.Add(s);
        }
        return e;
    }

    private void SetDirty(bool dirty = true)
    {
        if (_suppressDirty)
            return;
        _dirty = dirty;
        if (dirty && !_inRawEdit)
            _guiEdited = true;
        else if (!dirty)
            _guiEdited = false;
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        _statusPath.Text = string.IsNullOrEmpty(_filePath) ? "(unsaved)" : _filePath;
        _statusDirty.Text = _dirty ? "*" : "";
        Text = _dirty ? "rEFInd Config Editor *" : "rEFInd Config Editor";
        if (_applyButton is not null)
            _applyButton.Enabled = _dirty;
    }

    private bool ConfirmDiscard()
    {
        if (!_dirty)
            return true;
        var r = MessageBox.Show(this, "Discard unsaved changes?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        return r == DialogResult.Yes;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_dirty && !ConfirmDiscard())
            e.Cancel = true;
        base.OnFormClosing(e);
        if (!e.Cancel)
        {
            ClearHighlight();
            _searchDebounceTimer.Dispose();
            SaveWindowState();
        }
    }

}
