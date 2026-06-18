using Avalonia.Controls;
using Avalonia.Layout;
using rEFIndConfigEditor.Settings;
using rEFIndConfigEditor.ViewModels;

namespace rEFIndConfigEditor.UI;

internal static class ThemeTabBuilder
{
    public static (ScrollViewer Host, List<OptionBinding> Bindings, ThemeIncludePanelHandles ThemeInclude) Build(
        TabItem tab,
        Action<string, Control> registerNav,
        Func<string?> getRefindConfPath)
    {
        var (includePanel, includeHandles) = ThemeIncludePanelBuilder.Build(getRefindConfPath);

        var (settingsPanel, bindings) = OptionPanelBuilder.BuildContent(
            SettingCategory.Theme,
            MainWindowViewModel.ThemeTabKey,
            (token, target) => registerNav(token, target));

        registerNav("include", includeHandles.BrowserButton);

        var contentStack = new StackPanel
        {
            Spacing = 12,
            Margin = UiMetrics.TabContentPadding,
            Children =
            {
                UiTheme.CreateGroupBox("Theme include", includePanel),
                settingsPanel,
            },
        };

        return (OptionPanelBuilder.CreateScrollHost(contentStack), bindings, includeHandles);
    }
}
