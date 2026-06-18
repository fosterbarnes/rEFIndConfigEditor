using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;
using rEFIndConfigEditor.Models;

namespace rEFIndConfigEditor.UI;

internal static class MacWindowChrome
{
    private const double TabClickMaxMoveSq = 16;

    private static readonly ConditionalWeakTable<Window, ChromeState> States = new();

    private sealed class ChromeState
    {
        public MacTitleBarStyle? AppliedStyle;
        public bool HandlersWired;
        public bool TabTemplateHandlersWired;
    }

    public static void ApplyMode(
        Window window,
        MacTitleBarStyle style,
        Grid macChromeBar,
        Border macTabDragRegion,
        ListBox macTabHeaders,
        TabControl mainTabs,
        Grid tabContentHost,
        TextBox searchBox,
        Popup searchPopup)
    {
        if (!OperatingSystem.IsMacOS())
            return;

        var state = States.GetOrCreateValue(window);
        EnsureHandlers(window, macTabDragRegion, macTabHeaders, mainTabs, state, searchBox, searchPopup);

        if (state.AppliedStyle == style)
            return;

        state.AppliedStyle = style;

        if (style.UsesUnifiedChrome())
            ApplyCombined(window, macChromeBar, macTabDragRegion, mainTabs, searchBox);
        else
            ApplySeparate(window, macChromeBar, macTabDragRegion, mainTabs, tabContentHost, searchBox);
    }

    private static void EnsureHandlers(
        Window window,
        Border macTabDragRegion,
        ListBox macTabHeaders,
        TabControl mainTabs,
        ChromeState state,
        TextBox searchBox,
        Popup searchPopup)
    {
        if (!state.HandlersWired)
        {
            WireMacTabHeaderSelection(macTabDragRegion, macTabHeaders);
            WireMacSearchFocusClear(window, searchBox, searchPopup);
            state.HandlersWired = true;
        }

        if (state.TabTemplateHandlersWired)
            return;

        mainTabs.TemplateApplied += (_, e) => SyncBuiltInTabHeaders(mainTabs, e.NameScope, state);
        mainTabs.Loaded += (_, _) => SyncBuiltInTabHeaders(mainTabs, null, state);
        state.TabTemplateHandlersWired = true;
    }

    private static void ApplyCombined(
        Window window,
        Grid macChromeBar,
        Border macTabDragRegion,
        TabControl mainTabs,
        TextBox searchBox)
    {
        window.ExtendClientAreaToDecorationsHint = true;
        window.ExtendClientAreaTitleBarHeightHint = (int)UiMetrics.TabStripHeight;
        if (!window.Classes.Contains("mac-unified-chrome"))
            window.Classes.Add("mac-unified-chrome");

        macChromeBar.IsVisible = true;
        if (macChromeBar.ColumnDefinitions.Count > 0)
            macChromeBar.ColumnDefinitions[0].Width = new GridLength(UiMetrics.MacTrafficLightLeadingInset);

        ReparentSearchToChromeBar(macChromeBar, searchBox);
        HideBuiltInTabHeaders(mainTabs, null);

        WindowDecorationProperties.SetElementRole(macChromeBar, WindowDecorationsElementRole.TitleBar);
        WindowDecorationProperties.SetElementRole(macTabDragRegion, WindowDecorationsElementRole.TitleBar);
        WindowDecorationProperties.SetElementRole(searchBox, WindowDecorationsElementRole.User);
    }

    private static void ApplySeparate(
        Window window,
        Grid macChromeBar,
        Border macTabDragRegion,
        TabControl mainTabs,
        Grid tabContentHost,
        TextBox searchBox)
    {
        window.ExtendClientAreaToDecorationsHint = false;
        window.ExtendClientAreaTitleBarHeightHint = 0;
        window.Classes.Remove("mac-unified-chrome");

        macChromeBar.IsVisible = false;
        ReparentSearchToContentHost(tabContentHost, searchBox);
        RestoreBuiltInTabHeaders(mainTabs, null);

        WindowDecorationProperties.SetElementRole(macChromeBar, WindowDecorationsElementRole.None);
        WindowDecorationProperties.SetElementRole(macTabDragRegion, WindowDecorationsElementRole.None);
        WindowDecorationProperties.SetElementRole(searchBox, WindowDecorationsElementRole.None);
    }

    private static void SyncBuiltInTabHeaders(TabControl mainTabs, INameScope? nameScope, ChromeState state)
    {
        if (state.AppliedStyle?.UsesUnifiedChrome() == true)
            HideBuiltInTabHeaders(mainTabs, nameScope);
        else
            RestoreBuiltInTabHeaders(mainTabs, nameScope);
    }

    private static void ReparentSearchToChromeBar(Grid macChromeBar, TextBox searchBox)
    {
        if (searchBox.Parent is Panel parent)
            parent.Children.Remove(searchBox);

        Grid.SetColumn(searchBox, 2);
        searchBox.VerticalAlignment = VerticalAlignment.Top;
        searchBox.HorizontalAlignment = HorizontalAlignment.Stretch;
        searchBox.Margin = new Thickness(0, UiMetrics.TabUnselectedTopInset, 8, 0);
        searchBox.MinHeight = UiMetrics.TabHeight;
        searchBox.Height = UiMetrics.TabHeight;
        macChromeBar.Children.Add(searchBox);
    }

    private static void ReparentSearchToContentHost(Grid tabContentHost, TextBox searchBox)
    {
        if (searchBox.Parent is Panel parent)
            parent.Children.Remove(searchBox);

        searchBox.ClearValue(Grid.ColumnProperty);
        searchBox.VerticalAlignment = VerticalAlignment.Top;
        searchBox.HorizontalAlignment = HorizontalAlignment.Right;
        searchBox.Margin = UiMetrics.SearchBoxMargin;
        searchBox.ClearValue(Layoutable.MinHeightProperty);
        searchBox.Height = UiMetrics.SearchBoxHeight;
        tabContentHost.Children.Add(searchBox);
    }

    private static ItemsPresenter? FindTabHeaderPresenter(TabControl mainTabs, INameScope? nameScope) =>
        nameScope?.Find<ItemsPresenter>("PART_ItemsPresenter")
        ?? mainTabs.GetVisualDescendants().OfType<ItemsPresenter>()
            .FirstOrDefault(p => p.Name == "PART_ItemsPresenter");

    private static void HideBuiltInTabHeaders(TabControl mainTabs, INameScope? nameScope)
    {
        var itemsPresenter = FindTabHeaderPresenter(mainTabs, nameScope);
        if (itemsPresenter is null)
            return;

        itemsPresenter.IsVisible = false;
        itemsPresenter.Height = 0;
        itemsPresenter.MinHeight = 0;
        itemsPresenter.MaxHeight = 0;
    }

    private static void RestoreBuiltInTabHeaders(TabControl mainTabs, INameScope? nameScope)
    {
        var itemsPresenter = FindTabHeaderPresenter(mainTabs, nameScope);
        if (itemsPresenter is null)
            return;

        itemsPresenter.ClearValue(Visual.IsVisibleProperty);
        itemsPresenter.ClearValue(Layoutable.HeightProperty);
        itemsPresenter.ClearValue(Layoutable.MinHeightProperty);
        itemsPresenter.ClearValue(Layoutable.MaxHeightProperty);
    }

    private static void WireMacTabHeaderSelection(Border macTabDragRegion, ListBox macTabHeaders)
    {
        Point? pressOrigin = null;

        macTabDragRegion.AddHandler(
            InputElement.PointerPressedEvent,
            (_, e) =>
            {
                if (e.GetCurrentPoint(macTabDragRegion).Properties.IsLeftButtonPressed)
                    pressOrigin = e.GetPosition(macTabHeaders);
            },
            RoutingStrategies.Tunnel);

        macTabDragRegion.AddHandler(
            InputElement.PointerReleasedEvent,
            (_, e) =>
            {
                if (pressOrigin is not Point origin)
                    return;

                var release = e.GetPosition(macTabHeaders);
                pressOrigin = null;
                var dx = release.X - origin.X;
                var dy = release.Y - origin.Y;
                if (dx * dx + dy * dy >= TabClickMaxMoveSq)
                    return;

                var index = HitTestTabHeaderIndex(macTabHeaders, release);
                if (index >= 0)
                    macTabHeaders.SelectedIndex = index;
            },
            RoutingStrategies.Tunnel);
    }

    private static void WireMacSearchFocusClear(Window window, TextBox searchBox, Popup searchPopup)
    {
        window.AddHandler(
            InputElement.PointerPressedEvent,
            (_, e) =>
            {
                if (!searchBox.IsFocused || e.Source is not Visual source)
                    return;

                var popupRoot = searchPopup.IsOpen ? searchPopup.Child as Visual : null;
                if (Contains(searchBox, source) || popupRoot is not null && Contains(popupRoot, source))
                    return;

                if (e.Source is InputElement { Focusable: true } target)
                    target.Focus();
                else
                    window.FocusManager?.Focus(null, NavigationMethod.Unspecified, KeyModifiers.None);
            },
            RoutingStrategies.Bubble);

        static bool Contains(Visual root, Visual node) =>
            ReferenceEquals(node, root) || root.IsVisualAncestorOf(node);
    }

    private static int HitTestTabHeaderIndex(ListBox macTabHeaders, Point position)
    {
        for (var i = 0; i < macTabHeaders.Items.Count; i++)
        {
            if (macTabHeaders.ContainerFromIndex(i) is not Visual item)
                continue;

            var topLeft = item.TranslatePoint(new Point(), macTabHeaders);
            if (topLeft is null)
                continue;

            var rect = new Rect(topLeft.Value, item.Bounds.Size);
            if (rect.Contains(position))
                return i;
        }

        return -1;
    }
}
