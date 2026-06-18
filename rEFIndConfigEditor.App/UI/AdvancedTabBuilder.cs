using Avalonia.Controls;
using Avalonia.Layout;
using rEFIndConfigEditor.Settings;
using rEFIndConfigEditor.ViewModels;

namespace rEFIndConfigEditor.UI;

internal sealed class CommentCleanupControls
{
    public required CheckBox StripOnApply { get; init; }
    public required Button ClearNow { get; init; }
    public required TextBlock Hint { get; init; }
}

internal static class AdvancedTabBuilder
{
    public static (ScrollViewer Host, List<OptionBinding> Bindings, CommentCleanupControls Cleanup) Build(
        TabItem tab,
        Action<string, Control> registerNav)
    {
        var stripOnApply = new CheckBox { Content = "On apply" };
        ToolTip.SetTip(stripOnApply,
            "Removes # comment lines and commented-out global options from the config.\n" +
            "Does not change rEFInd boot behavior — editor convenience only.");

        var clearNow = new Button { Content = "Clear now" };
        ToolTip.SetTip(clearNow, ToolTip.GetTip(stripOnApply));

        var cleanupPanel = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 12,
            Children = { stripOnApply, clearNow },
        };
        Grid.SetColumn(clearNow, 1);

        var cleanupHint = new TextBlock
        {
            Text = "open/create a config first",
            Classes = { "setting-token" },
            Opacity = 0.65,
        };

        var cleanupStack = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = "Strip comments when applying", Classes = { "setting-label" } },
                cleanupHint,
                cleanupPanel,
            },
        };

        var (settingsPanel, bindings) = OptionPanelBuilder.BuildContent(
            SettingCategory.Other,
            MainWindowViewModel.OtherTabKey,
            (token, target) => registerNav(token, target));

        var contentStack = new StackPanel
        {
            Spacing = 12,
            Margin = UiMetrics.TabContentPadding,
            Children =
            {
                UiTheme.CreateGroupBox("Comment cleanup", cleanupStack),
                settingsPanel,
            },
        };

        var cleanup = new CommentCleanupControls
        {
            StripOnApply = stripOnApply,
            ClearNow = clearNow,
            Hint = cleanupHint,
        };

        return (OptionPanelBuilder.CreateScrollHost(contentStack), bindings, cleanup);
    }
}
