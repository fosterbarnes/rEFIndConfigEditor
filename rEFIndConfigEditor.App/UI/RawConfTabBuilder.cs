using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace rEFIndConfigEditor.UI;

internal static class RawConfTabBuilder
{
    public static (Control Content, TextBox Editor, Button ApplyButton) Build()
    {
        var editor = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(UiMetrics.TabContentPaddingPx, UiMetrics.TabContentPaddingPx, UiMetrics.TabContentPaddingPx, 0),
            Classes = { "raw-editor" },
        };

        var apply = new Button
        {
            Content = "Apply from raw",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinHeight = UiMetrics.ControlHeight + 8,
            Margin = new Thickness(UiMetrics.TabContentPaddingPx, 8, UiMetrics.TabContentPaddingPx, UiMetrics.TabContentPaddingPx),
        };

        var host = new Grid
        {
            RowDefinitions = new RowDefinitions("*,Auto"),
            Children = { editor, apply },
        };
        Grid.SetRow(editor, 0);
        Grid.SetRow(apply, 1);

        return (host, editor, apply);
    }
}
