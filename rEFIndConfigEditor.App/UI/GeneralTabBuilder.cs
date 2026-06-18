using Avalonia.Controls;
using Avalonia.Layout;
using rEFIndConfigEditor.Settings;
using rEFIndConfigEditor.ViewModels;

namespace rEFIndConfigEditor.UI;

internal static class GeneralTabBuilder
{
    public static (ScrollViewer Host, List<OptionBinding> Bindings, BootEntriesPanelHandles BootPanel) Build(
        TabItem tab,
        Action<string, Control> registerNav)
    {
        var (settingsPanel, bindings) = OptionPanelBuilder.BuildContent(
            SettingCategory.General,
            MainWindowViewModel.GeneralTabKey,
            (token, target) => registerNav(token, target));

        var (bootPanel, bootHandles) = BootEntriesPanelBuilder.Build();

        var contentStack = new StackPanel
        {
            Spacing = 12,
            Margin = UiMetrics.TabContentPadding,
            Children =
            {
                settingsPanel,
                UiTheme.CreateGroupBox("Boot entries", bootPanel),
            },
        };

        return (OptionPanelBuilder.CreateScrollHost(contentStack), bindings, bootHandles);
    }
}
