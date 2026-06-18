using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using rEFIndConfigEditor.Models;
using rEFIndConfigEditor.UI;
using rEFIndConfigEditor.ViewModels;

namespace rEFIndConfigEditor.Views;

public partial class MainWindow : Window
{
    private bool _tabStripTrailingRuleSyncScheduled;

    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

    public MainWindow()
    {
        InitializeComponent();
        AppIconLoader.TryApplyWindowIcon(this);
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        ViewModel.RestoreWindowBounds(this);
        UiTheme.ApplyWindowThemeVariant(this, ViewModel.SelectedTheme);
        ViewModel.ThemeChanged += theme => UiTheme.ApplyWindowTheme(this, theme);
        ViewModel.MacTitleBarStyleChanged += ApplyMacTitleBarStyle;
        ApplyMacTitleBarStyle();
        WireSearch();
        WireTabStripTrailingRule();
        ScheduleDeferredStartupWork();
    }

    private void ApplyMacTitleBarStyle()
    {
        MacWindowChrome.ApplyMode(
            this,
            ViewModel.MacTitleBarStyle,
            MacChromeBar,
            MacTabDragRegion,
            MacTabHeaders,
            MainTabs,
            TabContentHost,
            SearchBox,
            SearchPopup);
        SyncTabStripTrailingRules();
    }

    private void WireTabStripTrailingRule()
    {
        TabHost.LayoutUpdated += OnTabStripTrailingRuleLayoutUpdated;
        TabHost.SizeChanged += (_, _) => SyncTabStripTrailingRules();
        MainTabs.LayoutUpdated += OnTabStripTrailingRuleLayoutUpdated;
        MacTabHeaders.LayoutUpdated += OnTabStripTrailingRuleLayoutUpdated;
        SyncTabStripTrailingRules();
    }

    private void OnTabStripTrailingRuleLayoutUpdated(object? sender, EventArgs e)
    {
        if (_tabStripTrailingRuleSyncScheduled)
            return;

        _tabStripTrailingRuleSyncScheduled = true;
        Dispatcher.UIThread.Post(() =>
        {
            _tabStripTrailingRuleSyncScheduled = false;
            SyncTabStripTrailingRules();
        }, DispatcherPriority.Background);
    }

    private void SyncTabStripTrailingRules()
    {
        if (OperatingSystem.IsMacOS() && ViewModel.MacTitleBarStyle.UsesUnifiedChrome())
        {
            TabStripTrailingRule.IsVisible = false;
            MacTabStripTrailingRule.IsVisible = true;
            SyncTabStripTrailingRule(MacTabStripTrailingRule, MacTabHeaders, MacChromeBar, SearchBox);
            return;
        }

        MacTabStripTrailingRule.IsVisible = false;
        TabStripTrailingRule.IsVisible = true;
        SyncTabStripTrailingRule(TabStripTrailingRule, MainTabs, TabContentHost);
    }

    private static void SyncTabStripTrailingRule(
        Border ruleHost,
        ItemsControl itemsControl,
        Visual coordinateRoot,
        Visual? trailingEndBefore = null)
    {
        var startX = 0.0;
        var baselineY = 0.0;
        var measuredTab = false;
        var measuredBaseline = false;
        for (var i = 0; i < itemsControl.ItemCount; i++)
        {
            if (itemsControl.ContainerFromIndex(i) is not Visual tab)
                continue;

            measuredTab = true;
            var topLeft = tab.TranslatePoint(new Point(), coordinateRoot);
            if (topLeft is not { } origin)
                continue;

            startX = Math.Max(startX, origin.X + tab.Bounds.Width);

            if (itemsControl switch
                {
                    TabControl tabs => i == tabs.SelectedIndex,
                    ListBox list => i == list.SelectedIndex,
                    _ => false
                })
                continue;

            var bottomLeft = MeasureTabBottomLeft(tab, coordinateRoot);
            if (bottomLeft is not { } baseline)
                continue;

            baselineY = Math.Max(
                baselineY,
                baseline.Y - 1 + UiMetrics.TabStripTrailingRuleBaselineOffsetPx);
            measuredBaseline = true;
        }

        if (!measuredTab)
        {
            ruleHost.IsVisible = false;
            return;
        }

        if (!measuredBaseline)
            baselineY = UiMetrics.TabStripHeight - 1 + UiMetrics.TabStripTrailingRuleBaselineOffsetPx;

        var endX = coordinateRoot.Bounds.Width;
        if (trailingEndBefore?.TranslatePoint(new Point(), coordinateRoot) is { X: > 0 and var searchLeft })
        {
            if (searchLeft <= startX)
            {
                ruleHost.IsVisible = false;
                return;
            }

            endX = searchLeft;
        }

        var trailingWidth = endX - startX;
        if (trailingWidth <= 0.5)
        {
            ruleHost.IsVisible = false;
            return;
        }

        ruleHost.IsVisible = true;
        ruleHost.Margin = new Thickness(startX, baselineY, 0, 0);
        ruleHost.Width = trailingWidth;
    }

    private static Point? MeasureTabBottomLeft(Visual tab, Visual coordinateRoot)
    {
        foreach (var layoutRoot in tab.GetVisualDescendants().OfType<Border>())
        {
            if (layoutRoot.Name != "PART_LayoutRoot")
                continue;

            return layoutRoot.TranslatePoint(new Point(0, layoutRoot.Bounds.Height), coordinateRoot);
        }

        return tab.TranslatePoint(new Point(0, tab.Bounds.Height), coordinateRoot);
    }

    private void ScheduleDeferredStartupWork()
    {
        Dispatcher.UIThread.Post(() =>
        {
            ViewModel.EndDeferTabContentBuild();
            ViewModel.EnsureSelectedTabContent();
            ViewModel.RegisterPickerButtons();
        }, DispatcherPriority.Background);

        Dispatcher.UIThread.Post(ViewModel.BeginStartupUpdateCheck, DispatcherPriority.ApplicationIdle);
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        if (!await ViewModel.ConfirmDiscardIfDirtyAsync().ConfigureAwait(true))
            e.Cancel = true;

        if (!e.Cancel)
            ViewModel.SaveWindowState(this);

        base.OnClosing(e);
    }

    private void WireSearch()
    {
        SearchResults.ItemsSource = ViewModel.SearchResults;
        SearchResults.SelectionChanged += (_, _) =>
        {
            if (SearchResults.SelectedItem is SettingSearchHit hit)
            {
                ViewModel.PickSearchResultCommand.Execute(hit);
                SearchPopup.IsOpen = false;
                SearchResults.SelectedItem = null;
            }
        };

        ViewModel.SearchResults.CollectionChanged += (_, _) =>
            SearchPopup.IsOpen = ViewModel.SearchResults.Count > 0;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (ViewModel.SearchEnabled && e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.F)
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
            e.Handled = true;
        }
    }
}
