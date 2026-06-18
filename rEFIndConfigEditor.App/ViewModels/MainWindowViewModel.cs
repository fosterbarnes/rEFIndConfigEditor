using System.Collections.ObjectModel;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using rEFIndConfigEditor.Config;
using rEFIndConfigEditor.Diagnostics;
using rEFIndConfigEditor.Models;
using rEFIndConfigEditor.Platform;
using rEFIndConfigEditor.Services;
using rEFIndConfigEditor.Settings;
using rEFIndConfigEditor.Storage;
using rEFIndConfigEditor.UI;
using rEFIndConfigEditor.Update;
using rEFIndConfigEditor.Views;

namespace rEFIndConfigEditor.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly AppConfigStore _store;
    private readonly RefindDocumentService _documentService = new();
    private readonly RawConfTabSession _rawSession;
    private readonly Dictionary<string, (TabItem Page, Control FocusTarget)> _navByToken = new(StringComparer.Ordinal);
    private readonly List<(ScrollViewer Panel, List<OptionBinding> Bindings)> _optionPanels = [];
    private IReadOnlyList<FontSizeBinding> _fontSizeBindings = [];
    private IReadOnlyList<FontFamilyBinding> _fontFamilyBindings = [];
    private IReadOnlyList<RadioButton>? _themeRadios;
    private IReadOnlyList<RadioButton>? _titleBarRadios;
    private IReadOnlyDictionary<string, CheckBox>? _appCheckboxes;
    private CommentCleanupControls? _commentCleanup;
    private BootEntriesPanelHandles? _bootPanel;
    private ThemeIncludePanelHandles? _themeInclude;
    private bool _updateCheckStarted;
    private bool _suppressTabRestore;
    private bool _deferTabContentBuild = true;
    private bool _suppressDocumentDirty;
    private readonly HashSet<Button> _wiredPickers = [];
    private readonly HashSet<string> _builtTabs = new(StringComparer.Ordinal);

    [ObservableProperty] private string _filePath = string.Empty;
    [ObservableProperty] private bool _isDirty;
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private UiThemeKind _selectedTheme = UiThemeKind.System;
    [ObservableProperty] private bool _rememberLastSelectedTab;
    [ObservableProperty] private bool _autoLoadLastConfOnLaunch = true;
    [ObservableProperty] private bool _checkForUpdates = true;
    [ObservableProperty] private bool _automaticallyInstallUpdates;
    [ObservableProperty] private bool _enableDebugLogging;
    [ObservableProperty] private MacTitleBarStyle _macTitleBarStyle = MacTitleBarStyle.Separate;
    [ObservableProperty] private bool _searchEnabled = true;
    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private string _versionText = string.Empty;
    [ObservableProperty] private string _repoUrl = AppBranding.RepoUrl;

    public ObservableCollection<SettingSearchHit> SearchResults { get; } = [];
    public ObservableCollection<TabItem> TabItems { get; } = [];
    public ObservableCollection<(string Label, UiThemeKind Theme)> ThemeOptions { get; } =
    [
        ("System", UiThemeKind.System),
        ("Light", UiThemeKind.Light),
        ("Dark", UiThemeKind.Dark),
        ("Dracula", UiThemeKind.Dracula),
    ];
    public ObservableCollection<(string Label, MacTitleBarStyle Style)> TitleBarStyleOptions { get; } =
    [
        ("Separate title & tab bar", MacTitleBarStyle.Separate),
        ("Combined title & tab bar", MacTitleBarStyle.Combined),
    ];

    internal const string GeneralTabKey = "general";
    internal const string DisplayTabKey = "display";
    internal const string ThemeTabKey = "theme";
    internal const string InputTabKey = "input";
    internal const string ScanningTabKey = "scanning";
    internal const string OtherTabKey = "other";
    internal const string AppTabKey = "app";
    internal const string AboutTabKey = "about";
    internal const string RawTabKey = "raw";

    internal TextBox? RawEditor { get; private set; }

    public MainWindowViewModel(AppConfigStore store, UiPreferences prefs)
    {
        _store = store;
        _rawSession = new RawConfTabSession(_documentService);
        LoadPreferences(prefs);
        BuildTabs();
        ApplyPendingStartupStatusMessage();
        TryLoadLastConfOnStartup();
    }

    partial void OnSearchQueryChanged(string value) => UpdateSearch(value);

    partial void OnSelectedThemeChanged(UiThemeKind value)
    {
        if (_loadingPreferences)
            return;
        SyncPreferencesFromPanel();
        ThemeChanged?.Invoke(value);
    }

    partial void OnRememberLastSelectedTabChanged(bool value)
    {
        if (_loadingPreferences)
            return;
        SyncPreferencesFromPanel();
    }

    partial void OnAutoLoadLastConfOnLaunchChanged(bool value)
    {
        if (_loadingPreferences)
            return;
        SyncPreferencesFromPanel();
    }

    partial void OnCheckForUpdatesChanged(bool value)
    {
        if (_loadingPreferences)
            return;
        if (!value)
            AutomaticallyInstallUpdates = false;
        SyncPreferencesFromPanel();
    }

    partial void OnAutomaticallyInstallUpdatesChanged(bool value)
    {
        if (_loadingPreferences)
            return;
        SyncPreferencesFromPanel();
    }

    partial void OnEnableDebugLoggingChanged(bool value)
    {
        DebugLog.Enabled = value;
        if (_loadingPreferences)
            return;
        SyncPreferencesFromPanel();
    }

    partial void OnMacTitleBarStyleChanged(MacTitleBarStyle value)
    {
        if (_loadingPreferences)
            return;
        SyncPreferencesFromPanel();
        MacTitleBarStyleChanged?.Invoke();
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        if (value >= 0 && value < TabItems.Count)
            EnsureTabContent(TabItems[value].Name);

        _rawSession.OnTabSelected(IsRawTab(value));

        if (_suppressTabRestore || !RememberLastSelectedTab)
            return;
        if (SelectedTabIndex >= 0 && SelectedTabIndex < TabItems.Count)
            _uiPrefs.LastSelectedTabKey = TabItems[SelectedTabIndex].Name;
    }

    public event Action<UiThemeKind>? ThemeChanged;
    public event Action? MacTitleBarStyleChanged;
    public event Action? FontsChanged;

    private UiPreferences _uiPrefs = new();
    private UiPreferences _lastPersistedPrefs = new();
    private bool _loadingPreferences;

    public void EndDeferTabContentBuild() => _deferTabContentBuild = false;

    public void EnsureSelectedTabContent()
    {
        if (SelectedTabIndex < 0 || SelectedTabIndex >= TabItems.Count)
            return;
        EnsureTabContent(TabItems[SelectedTabIndex].Name);
    }

    public void EnsureTabContent(string? tabKey)
    {
        if (_deferTabContentBuild || string.IsNullOrEmpty(tabKey) || _builtTabs.Contains(tabKey))
            return;

        var tab = FindTab(tabKey);
        if (tab is null)
            return;

        switch (tabKey)
        {
            case GeneralTabKey:
            {
                var built = GeneralTabBuilder.Build(tab, RegisterNav);
                tab.Content = built.Host;
                RegisterOptionPanel(built.Host, built.Bindings);
                _bootPanel = built.BootPanel;
                BootPanelWiring.Wire(_bootPanel, _documentService, MarkGuiDirty);
                _bootPanel.RefreshList(_documentService.Document);
                _bootPanel.LoadSkipOther(_documentService.Document);
                break;
            }
            case DisplayTabKey:
                BuildSimpleOptionTab(tab, SettingCategory.Display, DisplayTabKey);
                break;
            case ThemeTabKey:
            {
                var built = ThemeTabBuilder.Build(tab, RegisterNav, () => _documentService.FilePath);
                tab.Content = built.Host;
                RegisterOptionPanel(built.Host, built.Bindings);
                _themeInclude = built.ThemeInclude;
                _themeInclude.Changed += () => MarkGuiDirty();
                _themeInclude.ThemeRemoved += ClearIncludeIfMatches;
                break;
            }
            case InputTabKey:
                BuildSimpleOptionTab(tab, SettingCategory.Input, InputTabKey);
                break;
            case ScanningTabKey:
                BuildSimpleOptionTab(tab, SettingCategory.Scanning, ScanningTabKey);
                break;
            case OtherTabKey:
                BuildAdvancedTab(tab);
                break;
            case AppTabKey:
                tab.Content = AppSettingsPanelBuilder.Build(tab, this);
                break;
            case AboutTabKey:
                tab.Content = AboutTabBuilder.Build(this);
                break;
            case RawTabKey:
                BuildRawTab(tab);
                break;
            default:
                return;
        }

        _builtTabs.Add(tabKey);
    }

    internal void RegisterNavTarget(TabItem tab, string token, Control focusTarget) =>
        _navByToken[token] = (tab, focusTarget);

    internal void WireAppFontSizes(TabItem tab, IReadOnlyList<FontSizeBinding> bindings)
    {
        _fontSizeBindings = bindings;
        OptionPanelPreferenceBridge.WireDirectFontSizes(bindings, _uiPrefs, OnFontPreferencesChanged);
        foreach (var binding in bindings)
            _navByToken[binding.Token] = (tab, binding.FocusTarget);
    }

    internal void WireAppFontFamilies(TabItem tab, IReadOnlyList<FontFamilyBinding> bindings)
    {
        _fontFamilyBindings = bindings;
        OptionPanelPreferenceBridge.WireFontFamilies(bindings, _uiPrefs, OnFontPreferencesChanged);
        foreach (var binding in bindings)
            _navByToken[binding.Token] = (tab, binding.FocusTarget);
    }

    internal void RegisterAppSettingsControls(
        IReadOnlyList<RadioButton> themeRadios,
        IReadOnlyList<RadioButton> titleBarRadios,
        IReadOnlyDictionary<string, CheckBox> checkboxes)
    {
        _themeRadios = themeRadios;
        _titleBarRadios = titleBarRadios;
        _appCheckboxes = checkboxes;
    }

    public void RegisterPickerButtons()
    {
        foreach (var (_, bindings) in _optionPanels)
        {
            foreach (var binding in bindings)
            {
                if (binding.PickerButton is not null && binding.ValueControl is not null
                    && _wiredPickers.Add(binding.PickerButton))
                {
                    binding.PickerButton.Click += async (_, _) =>
                    {
                        if (binding.ValueControl is not TextBox textBox)
                            return;

                        var def = binding.Definition;
                        string? picked = def.PathPicker switch
                        {
                            PathPickerKind.File => await PlatformServices.Current.PickFileAsync(def.Label, def.FileFilter),
                            PathPickerKind.Directory => await PlatformServices.Current.PickFolderAsync(def.Label),
                            _ => null,
                        };
                        if (picked is null)
                            return;

                        textBox.Text = picked;
                        if (binding.EnableCheck is not null)
                            binding.EnableCheck.IsChecked = true;
                        MarkGuiDirty();
                    };
                }

                if (binding.LogPickButton is not null && _wiredPickers.Add(binding.LogPickButton))
                {
                    binding.LogPickButton.Click += async (_, _) =>
                    {
                        if (string.Equals(binding.Definition.Token, "default_selection", StringComparison.OrdinalIgnoreCase))
                            await ChooseDefaultFromLogAsync(binding).ConfigureAwait(true);
                        else
                            await ChooseListFromLogAsync(binding).ConfigureAwait(true);
                    };
                }

                if (binding.FolderAddButton is not null && _wiredPickers.Add(binding.FolderAddButton))
                    binding.FolderAddButton.Click += async (_, _) => await AddFolderToOptionAsync(binding).ConfigureAwait(true);

                if (binding.FileAddButton is not null && _wiredPickers.Add(binding.FileAddButton))
                    binding.FileAddButton.Click += async (_, _) => await AddFileToOptionAsync(binding).ConfigureAwait(true);

                if (binding.BrowseButton is not null && _wiredPickers.Add(binding.BrowseButton))
                    binding.BrowseButton.Click += async (_, _) => await BrowseThemeOptionAsync(binding).ConfigureAwait(true);
            }
        }
    }

    public async Task<bool> ConfirmDiscardIfDirtyAsync()
    {
        if (!IsDirty)
            return true;
        return await PlatformServices.Current.ConfirmAsync(AppBranding.DisplayName, "Discard unsaved changes?");
    }

    public void SaveWindowState(Window window)
    {
        SyncPreferencesFromPanel();
        if (window.WindowState != WindowState.Maximized)
        {
            _uiPrefs.WindowX = window.Position.X;
            _uiPrefs.WindowY = window.Position.Y;
            _uiPrefs.WindowWidth = (int)window.Width;
            _uiPrefs.WindowHeight = (int)window.Height;
        }
        _uiPrefs.WindowMaximized = window.WindowState == WindowState.Maximized;
        if (_uiPrefs.RememberLastSelectedTab && SelectedTabIndex >= 0 && SelectedTabIndex < TabItems.Count)
            _uiPrefs.LastSelectedTabKey = TabItems[SelectedTabIndex].Name;
        PersistUiPreferences(PrefsSaveReason.WindowClose);
    }

    public void RestoreWindowBounds(Window window)
    {
        if (_uiPrefs.WindowX is null || _uiPrefs.WindowY is null
            || _uiPrefs.WindowWidth is null || _uiPrefs.WindowHeight is null
            || _uiPrefs.WindowWidth < 200 || _uiPrefs.WindowHeight < 150)
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return;
        }

        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Position = new PixelPoint(_uiPrefs.WindowX.Value, _uiPrefs.WindowY.Value);
        window.Width = _uiPrefs.WindowWidth.Value;
        window.Height = _uiPrefs.WindowHeight.Value;
        if (_uiPrefs.WindowMaximized)
            window.WindowState = WindowState.Maximized;
    }

    public void BeginStartupUpdateCheck()
    {
        if (_updateCheckStarted || !CheckForUpdates || AutomaticallyInstallUpdates)
            return;

        _updateCheckStarted = true;
        var installDirectory = AppContext.BaseDirectory;
        _ = Task.Run(async () =>
        {
            try
            {
                var result = await UpdateCheckService.CheckAsync(installDirectory).ConfigureAwait(false);
                if (!result.IsOutdated)
                    return;

                if (DebugLog.Enabled)
                    DebugLog.Write("Update", $"Startup update available: {result.LatestVersion}");

                await Dispatcher.UIThread.InvokeAsync(async () =>
                    await HandleStartupUpdateAsync(result, installDirectory).ConfigureAwait(true));
            }
            catch (Exception ex)
            {
                UpdaterLogger.Write($"Startup update check failed: {ex}");
                if (DebugLog.Enabled)
                    DebugLog.Write("Update", $"Startup update check failed: {ex}");
            }
        });
    }

    [RelayCommand]
    private void OpenRepo() => PlatformServices.Current.OpenUrl(AppBranding.RepoUrl);

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        if (!await ConfirmDiscardIfDirtyAsync().ConfigureAwait(true))
            return;

        var path = await PlatformServices.Current.PickFileAsync(
            "Open refind.conf",
            "rEFInd config (refind.conf)|refind.conf|All files (*.*)|*.*").ConfigureAwait(true);
        if (path is null)
            return;

        LoadFromPath(path);
    }

    [RelayCommand]
    private async Task NewFileAsync()
    {
        if (!await ConfirmDiscardIfDirtyAsync().ConfigureAwait(true))
            return;

        _documentService.NewDocument();
        RefreshDocumentUi();
    }

    [RelayCommand]
    private async Task ApplyAsync()
    {
        if (string.IsNullOrEmpty(_documentService.FilePath))
        {
            await SaveAsAsync().ConfigureAwait(true);
            return;
        }

        await SaveToPathAsync(_documentService.FilePath).ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrEmpty(_documentService.FilePath))
        {
            await SaveAsAsync().ConfigureAwait(true);
            return;
        }

        await SaveToPathAsync(_documentService.FilePath).ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task SaveAsAsync()
    {
        var path = await PlatformServices.Current.PickSaveFileAsync(
            "Save refind.conf as",
            "rEFInd config (refind.conf)|refind.conf|All files (*.*)|*.*",
            "refind.conf").ConfigureAwait(true);
        if (path is null)
            return;

        await SaveToPathAsync(path).ConfigureAwait(true);
    }

    [RelayCommand]
    private void ApplyAppSettings()
    {
        SyncPreferencesFromPanel();
        if (Application.Current is { } app)
            UiFontService.Apply(app, _uiPrefs);
        ThemeChanged?.Invoke(SelectedTheme);
        FontsChanged?.Invoke();
        PersistUiPreferences(PrefsSaveReason.Apply);
    }

    [RelayCommand]
    private void CheckForUpdatesManual() =>
        _ = Task.Run(async () =>
        {
            try
            {
                await UpdaterLauncher.StartInteractiveAsync(AppContext.BaseDirectory).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                UpdaterLogger.Write($"Interactive updater launch failed: {ex}");
                if (DebugLog.Enabled)
                    DebugLog.Write("Update", $"Interactive updater launch failed: {ex}");
            }
        });

    [RelayCommand]
    private void PickSearchResult(SettingSearchHit? hit)
    {
        if (hit is null)
            return;

        SearchQuery = string.Empty;
        SearchResults.Clear();
        NavigateToToken(hit.Token);
    }

    internal void NavigateToToken(string token)
    {
        if (string.Equals(token, "include", StringComparison.OrdinalIgnoreCase))
        {
            EnsureTabContent(ThemeTabKey);
            SelectTab(ThemeTabKey);
            _themeInclude?.BringIntoView();
            return;
        }

        var def = SettingCatalog.All.FirstOrDefault(d =>
            string.Equals(d.Token, token, StringComparison.OrdinalIgnoreCase));
        if (def is null)
            return;

        var tabKey = CategoryToTabKey(def.Category);
        EnsureTabContent(tabKey);

        if (!_navByToken.TryGetValue(token, out var entry))
            return;

        SelectTab(tabKey);

        for (var i = 0; i < TabItems.Count; i++)
        {
            if (ReferenceEquals(TabItems[i], entry.Page))
            {
                SelectedTabIndex = i;
                break;
            }
        }

        BringNavTargetIntoView(token);
        entry.FocusTarget?.Focus();
    }

    private void BringNavTargetIntoView(string token)
    {
        foreach (var (_, bindings) in _optionPanels)
        {
            foreach (var binding in bindings)
            {
                if (!string.Equals(binding.Definition.Token, token, StringComparison.OrdinalIgnoreCase))
                    continue;

                binding.RowAnchor?.BringIntoView();
                return;
            }
        }

        if (_navByToken.TryGetValue(token, out var entry))
            entry.FocusTarget?.BringIntoView();
    }

    private void BuildTabs()
    {
        TabItems.Clear();
        _navByToken.Clear();
        _optionPanels.Clear();
        _builtTabs.Clear();
        _wiredPickers.Clear();
        _bootPanel = null;
        _themeInclude = null;

        TabItems.Add(new TabItem { Header = "General", Name = GeneralTabKey, Content = null });
        TabItems.Add(new TabItem { Header = "Display", Name = DisplayTabKey, Content = null });
        TabItems.Add(new TabItem { Header = "Theme", Name = ThemeTabKey, Content = null });
        TabItems.Add(new TabItem { Header = "Input", Name = InputTabKey, Content = null });
        TabItems.Add(new TabItem { Header = "Scanning", Name = ScanningTabKey, Content = null });
        TabItems.Add(new TabItem { Header = "Other", Name = OtherTabKey, Content = null });
        TabItems.Add(new TabItem { Header = "Raw .conf", Name = RawTabKey, Content = null });
        TabItems.Add(new TabItem { Header = "App", Name = AppTabKey, Content = null });
        TabItems.Add(new TabItem { Header = "About", Name = AboutTabKey, Content = null });

        ApplyLastSelectedTab();
    }

    private void BuildSimpleOptionTab(TabItem tab, SettingCategory category, string tabKey)
    {
        var (panel, bindings) = OptionPanelBuilder.BuildContent(
            category,
            tabKey,
            (token, target) => RegisterNav(token, target));
        tab.Content = OptionPanelBuilder.CreateScrollHost(new StackPanel
        {
            Margin = UiMetrics.TabContentPadding,
            Children = { panel },
        });
        RegisterOptionPanel(tab.Content as ScrollViewer ?? throw new InvalidOperationException(), bindings);
    }

    private void BuildOptionTab(TabItem tab, (ScrollViewer Host, List<OptionBinding> Bindings) built)
    {
        tab.Content = built.Host;
        RegisterOptionPanel(built.Host, built.Bindings);
    }

    private void BuildAdvancedTab(TabItem tab)
    {
        var built = AdvancedTabBuilder.Build(tab, RegisterNav);
        tab.Content = built.Host;
        RegisterOptionPanel(built.Host, built.Bindings);
        _commentCleanup = built.Cleanup;
        _commentCleanup.ClearNow.Click += async (_, _) => await ClearCommentsNowAsync().ConfigureAwait(true);
        SyncCommentCleanupState();
    }

    private void BuildRawTab(TabItem tab)
    {
        var (content, editor, applyButton) = RawConfTabBuilder.Build();
        tab.Content = content;
        RawEditor = editor;
        _rawSession.Wire(editor, SyncDirtyFromService);
        applyButton.Click += async (_, _) => await ApplyRawAsync().ConfigureAwait(true);
    }

    private void RegisterOptionPanel(ScrollViewer host, List<OptionBinding> bindings)
    {
        _optionPanels.Add((host, bindings));
        WireBindingDirty(bindings);
        _suppressDocumentDirty = true;
        _documentService.IsApplyingFromDocument = true;
        try
        {
            RefindDocumentBridge.LoadBindings(bindings, _documentService.Document);
        }
        finally
        {
            _documentService.IsApplyingFromDocument = false;
            _suppressDocumentDirty = false;
        }
    }

    private void RegisterNav(string token, Control target)
    {
        if (FindTabByTokenCategory(token) is { } tab)
            _navByToken[token] = (tab, target);
    }

    private TabItem? FindTabByTokenCategory(string token)
    {
        var def = SettingCatalog.All.FirstOrDefault(d =>
            string.Equals(d.Token, token, StringComparison.OrdinalIgnoreCase));
        if (def is null)
            return null;
        return FindTab(CategoryToTabKey(def.Category));
    }

    private void WireBindingDirty(IEnumerable<OptionBinding> bindings)
    {
        foreach (var binding in bindings)
        {
            if (binding.EnableCheck is not null)
                binding.EnableCheck.IsCheckedChanged += (_, _) => MarkGuiDirty();

            if (binding.Definition.Kind == SettingControlKind.Boolean)
                continue;

            WireValueDirty(binding.ValueControl);
        }
    }

    private void WireValueDirty(Control? value)
    {
        switch (value)
        {
            case CheckBox cb:
                cb.IsCheckedChanged += (_, _) => MarkGuiDirty();
                break;
            case NumericUpDown nud:
                nud.ValueChanged += (_, _) =>
                {
                    foreach (var (_, bindings) in _optionPanels)
                    {
                        foreach (var b in bindings)
                        {
                            if (ReferenceEquals(b.ValueControl, nud))
                                b.PreservedInvalidNumeric = null;
                        }
                    }
                    MarkGuiDirty();
                };
                break;
            case ComboBox combo:
                combo.SelectionChanged += (_, _) => MarkGuiDirty();
                break;
            case TextBox tb:
                tb.TextChanged += (_, _) => MarkGuiDirty();
                break;
            case Border { Child: StackPanel panel }:
                foreach (var child in panel.Children.OfType<CheckBox>())
                    child.IsCheckedChanged += (_, _) => MarkGuiDirty();
                break;
            case Grid grid:
                foreach (var child in grid.Children)
                    WireValueDirty(child);
                break;
        }
    }

    private void MarkGuiDirty()
    {
        if (_suppressDocumentDirty)
            return;
        _documentService.MarkGuiEdited();
        SyncDirtyFromService();
    }

    private void SyncDirtyFromService()
    {
        IsDirty = _documentService.IsDirty;
        FilePath = string.IsNullOrEmpty(_documentService.FilePath)
            ? "(unsaved)"
            : _documentService.FilePath;
        SyncCommentCleanupState();
    }

    private void SyncCommentCleanupState()
    {
        if (_commentCleanup is null)
            return;

        var hasDocument = _documentService.HasOpenDocument;
        _commentCleanup.StripOnApply.IsEnabled = hasDocument;
        _commentCleanup.ClearNow.IsEnabled = hasDocument;
        _commentCleanup.Hint.IsVisible = !hasDocument;
    }

    private void RefreshDocumentUi()
    {
        _suppressDocumentDirty = true;
        _documentService.IsApplyingFromDocument = true;
        try
        {
            _documentService.RefreshUi(AllBindings, LoadDocumentExtras);
            _rawSession.OnDocumentRefreshed(IsRawTab(SelectedTabIndex));
            SyncDirtyFromService();
        }
        finally
        {
            _documentService.IsApplyingFromDocument = false;
            _suppressDocumentDirty = false;
        }
    }

    private void LoadDocumentExtras(RefindDocument doc)
    {
        _themeInclude?.LoadFrom(doc);
        _bootPanel?.LoadSkipOther(doc);
        _bootPanel?.RefreshList(doc);
    }

    private void ClearIncludeIfMatches(string themeConfPath)
    {
        var opt = _documentService.Document.FindGlobal("include");
        if (opt is not { IsActive: true } || opt.Values.Count == 0)
            return;

        var includePath = RefindPathHelper.NormalizeSlashes(opt.Values[0].Trim());
        var removedPath = RefindPathHelper.NormalizeSlashes(themeConfPath.Trim());
        if (!string.Equals(includePath, removedPath, StringComparison.OrdinalIgnoreCase))
            return;

        _documentService.Document.RemoveGlobal("include");
        _themeInclude?.LoadFrom(_documentService.Document);
        MarkGuiDirty();
    }

    private void SaveDocumentExtras(RefindDocument doc)
    {
        _themeInclude?.SaveTo(doc);
        _bootPanel?.ApplySkipOther(doc);
    }

    private void LoadFromPath(string path)
    {
        if (!_documentService.LoadFromFile(path, out var warning))
        {
            PlatformServices.Current.ShowWarning(
                AppBranding.DisplayName,
                $"Could not open this file as a rEFInd config:\n\n{warning}");
            return;
        }

        if (!string.IsNullOrWhiteSpace(warning))
        {
            PlatformServices.Current.ShowWarning(AppBranding.DisplayName, warning);
        }

        RememberLastConfPath(path);
        RefreshDocumentUi();
    }

    private async Task SaveToPathAsync(string path)
    {
        SaveConflictResolution? conflict = null;
        if (_documentService.NeedsSaveConflictResolution)
        {
            conflict = await PlatformServices.Current.AskSaveConflictAsync(
                AppBranding.DisplayName,
                "You have unapplied edits in BOTH the Raw tab and the GUI tabs.\n\n" +
                "Apply Raw — discard GUI edits\n" +
                "Apply GUI — discard Raw edits\n" +
                "Cancel — don't save").ConfigureAwait(true);
            if (conflict is null or SaveConflictResolution.Cancel)
                return;
        }

        var result = _documentService.SaveToPath(
            path,
            conflict,
            AllBindings,
            _rawSession.CurrentRawText,
            _commentCleanup?.StripOnApply.IsChecked == true,
            SaveDocumentExtras);

        switch (result)
        {
            case SaveResult.Success:
                RememberLastConfPath(path);
                RefreshDocumentUi();
                break;
            case SaveResult.ValidationFailed:
                if (!_documentService.Validate(out var err))
                    PlatformServices.Current.ShowWarning(AppBranding.DisplayName, err);
                break;
            case SaveResult.StructureFailed:
                PlatformServices.Current.ShowError(AppBranding.DisplayName, "Generated config failed structure validation.");
                break;
            case SaveResult.ParseFailed:
                PlatformServices.Current.ShowError(AppBranding.DisplayName, "Could not parse raw config text.");
                break;
            case SaveResult.WriteFailed:
                PlatformServices.Current.ShowError(AppBranding.DisplayName, $"Could not save this file:\n\n{path}");
                break;
        }

        SyncDirtyFromService();
    }

    private async Task ApplyRawAsync()
    {
        if (_documentService.GuiEdited)
        {
            var ok = await PlatformServices.Current.ConfirmAsync(
                AppBranding.DisplayName,
                "You have unapplied edits in the GUI tabs.\n\nApplying from raw will discard those GUI edits.\n\nContinue?")
                .ConfigureAwait(true);
            if (!ok)
                return;
        }

        if (!_rawSession.ApplyFromRawConfirmed(out var error))
        {
            PlatformServices.Current.ShowError(AppBranding.DisplayName, error ?? "Could not parse config.");
            return;
        }

        RefreshDocumentUi();
        SyncDirtyFromService();
    }

    private async Task ClearCommentsNowAsync()
    {
        if (!await ApplyPendingDocumentEditsAsync(
                "You have unapplied edits in BOTH the Raw tab and the GUI tabs.\n\n" +
                "Apply Raw — discard GUI edits\n" +
                "Apply GUI — discard Raw edits\n" +
                "Cancel — don't clear comments").ConfigureAwait(true))
            return;

        _documentService.StripComments();
        RefreshDocumentUi();
        SyncDirtyFromService();
    }

    private async Task<bool> ApplyPendingDocumentEditsAsync(string conflictMessage)
    {
        if (_documentService.NeedsSaveConflictResolution)
        {
            var conflict = await PlatformServices.Current.AskSaveConflictAsync(
                AppBranding.DisplayName,
                conflictMessage).ConfigureAwait(true);
            if (conflict is null or SaveConflictResolution.Cancel)
                return false;

            if (conflict == SaveConflictResolution.ApplyRaw)
            {
                if (!_documentService.TryParseRaw(_rawSession.CurrentRawText ?? string.Empty, out var error))
                {
                    PlatformServices.Current.ShowError(AppBranding.DisplayName, error ?? "Could not parse config.");
                    return false;
                }
            }
            else
                _documentService.ApplyFromUi(AllBindings, SaveDocumentExtras);

            return true;
        }

        if (_documentService.RawEdited)
        {
            if (!_documentService.TryParseRaw(_rawSession.CurrentRawText ?? string.Empty, out var error))
            {
                PlatformServices.Current.ShowError(AppBranding.DisplayName, error ?? "Could not parse config.");
                return false;
            }
            return true;
        }

        _documentService.ApplyFromUi(AllBindings, SaveDocumentExtras);
        return true;
    }

    private IEnumerable<OptionBinding> AllBindings =>
        _optionPanels.SelectMany(p => p.Bindings);

    private void TryLoadLastConfOnStartup()
    {
        if (!AutoLoadLastConfOnLaunch || string.IsNullOrWhiteSpace(_uiPrefs.LastConfPath))
        {
            _documentService.NewDocument();
            SyncDirtyFromService();
            return;
        }

        if (!File.Exists(_uiPrefs.LastConfPath))
        {
            PlatformServices.Current.ShowWarning(
                AppBranding.DisplayName,
                $"Could not auto-load the last config file:\n\n{_uiPrefs.LastConfPath}\n\nThe file was not found.");
            _documentService.NewDocument();
            SyncDirtyFromService();
            return;
        }

        LoadFromPath(_uiPrefs.LastConfPath);
    }

    private void RememberLastConfPath(string path)
    {
        _uiPrefs.LastConfPath = path;
        PersistUiPreferences(PrefsSaveReason.Apply);
    }

    private void LoadPreferences(UiPreferences prefs)
    {
        _loadingPreferences = true;
        _uiPrefs = prefs.Clone();
        _uiPrefs.MainFontSize = UiFontService.Clamp(_uiPrefs.MainFontSize);
        _uiPrefs.TabFontSize = UiFontService.Clamp(_uiPrefs.TabFontSize);
        _uiPrefs.TokenFontSize = UiFontService.Clamp(_uiPrefs.TokenFontSize);
        _uiPrefs.MainFontFamily = UiFontFamilies.NormalizeMain(_uiPrefs.MainFontFamily);
        _uiPrefs.MonoFontFamily = UiFontFamilies.NormalizeMono(_uiPrefs.MonoFontFamily);
        SelectedTheme = _uiPrefs.Theme;
        RememberLastSelectedTab = _uiPrefs.RememberLastSelectedTab;
        AutoLoadLastConfOnLaunch = _uiPrefs.AutoLoadLastConfOnLaunch;
        CheckForUpdates = _uiPrefs.CheckForUpdates;
        AutomaticallyInstallUpdates = _uiPrefs.AutomaticallyInstallUpdates;
        EnableDebugLogging = _uiPrefs.EnableDebugLogging;
        MacTitleBarStyle = _uiPrefs.MacTitleBarStyle;
        _loadingPreferences = false;
        SyncPreferencesFromPanel();
        _lastPersistedPrefs = _uiPrefs.Clone();

        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?";
        VersionText = $"{AppBranding.DisplayName} v{version}  - Copyright © 2026 {AppBranding.CopyrightHolder}";
    }

    private void ApplyLastSelectedTab()
    {
        if (!_uiPrefs.RememberLastSelectedTab || string.IsNullOrEmpty(_uiPrefs.LastSelectedTabKey))
            return;

        for (var i = 0; i < TabItems.Count; i++)
        {
            if (string.Equals(TabItems[i].Name, _uiPrefs.LastSelectedTabKey, StringComparison.Ordinal))
            {
                _suppressTabRestore = true;
                SelectedTabIndex = i;
                _suppressTabRestore = false;
                return;
            }
        }
    }

    private void SelectTab(string tabKey)
    {
        for (var i = 0; i < TabItems.Count; i++)
        {
            if (string.Equals(TabItems[i].Name, tabKey, StringComparison.Ordinal))
            {
                SelectedTabIndex = i;
                return;
            }
        }
    }

    private static string CategoryToTabKey(SettingCategory category) => category switch
    {
        SettingCategory.General => GeneralTabKey,
        SettingCategory.Display => DisplayTabKey,
        SettingCategory.Theme => ThemeTabKey,
        SettingCategory.Input => InputTabKey,
        SettingCategory.Scanning => ScanningTabKey,
        SettingCategory.Other => OtherTabKey,
        SettingCategory.App => AppTabKey,
        _ => GeneralTabKey
    };

    private bool IsRawTab(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= TabItems.Count)
            return false;
        return string.Equals(TabItems[tabIndex].Name, RawTabKey, StringComparison.Ordinal);
    }

    private TabItem? FindTab(string name)
    {
        foreach (var item in TabItems)
        {
            if (string.Equals(item.Name, name, StringComparison.Ordinal))
                return item;
        }
        return null;
    }

    private void SyncPreferencesFromPanel()
    {
        _uiPrefs.Theme = SelectedTheme;
        _uiPrefs.RememberLastSelectedTab = RememberLastSelectedTab;
        _uiPrefs.AutoLoadLastConfOnLaunch = AutoLoadLastConfOnLaunch;
        _uiPrefs.CheckForUpdates = CheckForUpdates;
        _uiPrefs.AutomaticallyInstallUpdates = AutomaticallyInstallUpdates;
        _uiPrefs.EnableDebugLogging = EnableDebugLogging;
        _uiPrefs.MacTitleBarStyle = MacTitleBarStyle;
    }

    private enum PrefsSaveReason { Apply, WindowClose }

    private void PersistUiPreferences(PrefsSaveReason reason)
    {
        try
        {
            _store.SaveUiPreferences(_uiPrefs);
            _lastPersistedPrefs = _uiPrefs.Clone();
            if (DebugLog.Enabled && reason == PrefsSaveReason.Apply)
            {
                DebugLog.Write("Prefs", $"Apply: ui_theme={_uiPrefs.Theme}");
                DebugLog.Write("Prefs", $"Apply: ui_auto_load_last_conf={_uiPrefs.AutoLoadLastConfOnLaunch}");
            }
        }
        catch (Exception ex)
        {
            UpdaterLogger.Write($"Failed to save UI preferences: {ex}");
        }
    }

    private void OnFontPreferencesChanged()
    {
        if (Application.Current is { } app)
            UiFontService.Apply(app, _uiPrefs);
        FontsChanged?.Invoke();
    }

    private void ApplyPendingStartupStatusMessage()
    {
        var message = StartupUpdateState.ConsumePendingStatusMessage();
        if (!string.IsNullOrWhiteSpace(message) && DebugLog.Enabled)
            DebugLog.Write("Startup", message);
    }

    private async Task HandleStartupUpdateAsync(UpdateCheckResult result, string installDirectory)
    {
        if (!await UpdatePromptWindow.PromptAsync(result).ConfigureAwait(true))
            return;

        try
        {
            await UpdaterLauncher.LaunchInstallAsync(installDirectory).ConfigureAwait(false);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            UpdaterLogger.Write($"Manual update launch failed: {ex}");
            PlatformServices.Current.ShowWarning(
                AppBranding.DisplayName,
                $"Could not start the updater:\n{ex.Message}");
        }
    }

    private void UpdateSearch(string query)
    {
        SearchResults.Clear();
        if (string.IsNullOrWhiteSpace(query))
            return;

        foreach (var hit in SettingSearch.Find(query))
            SearchResults.Add(hit);
    }

    internal void SyncAppSettingsControls()
    {
        if (_themeRadios is not null)
        {
            foreach (var radio in _themeRadios)
            {
                if (radio.Tag is UiThemeKind theme)
                    radio.IsChecked = theme == SelectedTheme;
            }
        }

        if (_titleBarRadios is not null)
        {
            foreach (var radio in _titleBarRadios)
            {
                if (radio.Tag is MacTitleBarStyle style)
                    radio.IsChecked = style == MacTitleBarStyle;
            }
        }

        if (_appCheckboxes is null)
            return;

        if (_appCheckboxes.TryGetValue("ui_remember_last_tab", out var rememberTab))
            rememberTab.IsChecked = RememberLastSelectedTab;
        if (_appCheckboxes.TryGetValue("ui_auto_load_last_conf", out var autoLoad))
            autoLoad.IsChecked = AutoLoadLastConfOnLaunch;
        if (_appCheckboxes.TryGetValue("ui_check_for_updates", out var checkUpdates))
            checkUpdates.IsChecked = CheckForUpdates;
        if (_appCheckboxes.TryGetValue("ui_auto_install_updates", out var autoInstall))
        {
            autoInstall.IsChecked = AutomaticallyInstallUpdates;
            autoInstall.IsEnabled = CheckForUpdates;
        }
        if (_appCheckboxes.TryGetValue("ui_enable_debug_logging", out var debugLogging))
            debugLogging.IsChecked = EnableDebugLogging;

        SyncFontFamilyCombos();
    }

    private void SyncFontFamilyCombos()
    {
        foreach (var binding in _fontFamilyBindings)
        {
            var index = binding.Token switch
            {
                "ui_font_family_main" => UiFontFamilies.IndexOfMain(_uiPrefs.MainFontFamily),
                "ui_font_family_mono" => UiFontFamilies.IndexOfMono(_uiPrefs.MonoFontFamily),
                _ => -1,
            };
            if (index >= 0)
                binding.Combo.SelectedIndex = index;
        }
    }

    private async Task ChooseListFromLogAsync(OptionBinding binding)
    {
        if (!LogPickerController.TryGet(binding.Definition.Token, out var picker))
            return;

        var owner = DialogHost.GetOwner();
        if (owner is null)
            return;

        var selected = await LogPickerController.PickItemsAsync(
            owner,
            picker,
            _documentService.FilePath,
            _documentService.Document.MenuEntries).ConfigureAwait(true);
        if (selected.Count == 0 || binding.ValueControl is not TextBox textBox)
            return;

        textBox.Text = RefindWriter.AppendCommaList(textBox.Text ?? string.Empty, selected);
        if (binding.EnableCheck is not null)
            binding.EnableCheck.IsChecked = true;
        MarkGuiDirty();
    }

    private async Task ChooseDefaultFromLogAsync(OptionBinding binding)
    {
        var owner = DialogHost.GetOwner();
        if (owner is null)
            return;

        var menuEntries = _documentService.Document.MenuEntries.ToList();
        IReadOnlyList<RefindLogBootCandidate> fromLog = [];

        var logPath = await RefindLogPickerHelper.PickLogPathAsync(_documentService.FilePath).ConfigureAwait(true);
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
                    PlatformServices.Current.ShowError(AppBranding.DisplayName, ex.Message);
                    return;
                }
            }
        }
        else if (menuEntries.Count == 0)
            return;

        var candidates = RefindLogParser.ForDefaultSelection(menuEntries, fromLog);
        if (candidates.Count == 0)
        {
            PlatformServices.Current.ShowWarning(AppBranding.DisplayName, "No boot entries found in the config or log.");
            return;
        }

        var selected = await RefindLogImportWindow.PickBootCandidatesAsync(
            owner,
            candidates,
            RefindLogImportPurpose.DefaultSelection).ConfigureAwait(true);
        if (selected.Count != 1 || binding.ValueControl is not TextBox textBox)
            return;

        if (binding.EnableCheck is not null)
            binding.EnableCheck.IsChecked = true;
        textBox.Text = selected[0].Title;
        MarkGuiDirty();
    }

    private async Task AddFolderToOptionAsync(OptionBinding binding)
    {
        if (binding.ValueControl is not TextBox textBox)
            return;

        var picked = await PlatformServices.Current.PickFolderAsync(
            "Select a folder on the EFI system partition (or volume root)").ConfigureAwait(true);
        if (picked is null)
            return;

        textBox.Text = RefindScanPathHelper.AppendToCommaList(
            textBox.Text ?? string.Empty,
            RefindScanPathHelper.FormatPickedFolder(picked, _documentService.FilePath));
        if (binding.EnableCheck is not null)
            binding.EnableCheck.IsChecked = true;
        MarkGuiDirty();
    }

    private async Task AddFileToOptionAsync(OptionBinding binding)
    {
        if (binding.ValueControl is not TextBox textBox)
            return;

        var filter = binding.Definition.Token == "dont_scan_files"
            ? "EFI programs (*.efi)|*.efi|All files (*.*)|*.*"
            : "EFI programs (*.efi)|*.efi|All files (*.*)|*.*";

        var picked = await PlatformServices.Current.PickFilesAsync(
            binding.Definition.Token == "dont_scan_files"
                ? "Select boot loader files to hide"
                : "Select tool files to hide",
            filter).ConfigureAwait(true);
        if (picked.Count == 0)
            return;

        var paths = picked
            .Select(f => RefindScanFileHelper.FormatPickedFile(f, _documentService.FilePath))
            .ToList();
        textBox.Text = RefindScanFileHelper.AppendFilesToCommaList(textBox.Text ?? string.Empty, paths);
        if (binding.EnableCheck is not null)
            binding.EnableCheck.IsChecked = true;
        MarkGuiDirty();
    }

    private async Task BrowseThemeOptionAsync(OptionBinding binding)
    {
        if (binding.ValueControl is not TextBox textBox)
            return;

        var token = binding.Definition.Token;
        if (RefindThemePathHelper.IsIconsDirToken(token))
        {
            var picked = await PlatformServices.Current.PickFolderAsync(
                "Select a custom icons folder under the rEFInd directory").ConfigureAwait(true);
            if (picked is null)
                return;

            textBox.Text = RefindThemePathHelper.FormatPickedIconsDir(picked, _documentService.FilePath);
            if (binding.EnableCheck is not null)
                binding.EnableCheck.IsChecked = true;
            MarkGuiDirty();
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
            _ => "Select image file",
        };

        var filter = token == "font"
            ? RefindThemePathHelper.FontFileDialogFilter
            : RefindIconHelper.IconFileDialogFilter;

        var file = await PlatformServices.Current.PickFileAsync(title, filter).ConfigureAwait(true);
        if (file is null)
            return;

        textBox.Text = RefindThemePathHelper.FormatPickedThemeFile(file, _documentService.FilePath);
        if (binding.EnableCheck is not null)
            binding.EnableCheck.IsChecked = true;
        MarkGuiDirty();
    }
}
