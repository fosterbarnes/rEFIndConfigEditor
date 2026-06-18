using Avalonia.Controls;
using Avalonia.Layout;
using Material.Icons;
using Material.Icons.Avalonia;
using rEFIndConfigEditor.Settings;

namespace rEFIndConfigEditor.UI;

internal static class MdiIcons
{
    public static MaterialIcon Create(MaterialIconKind kind, double size = UiMetrics.IconSize) =>
        new()
        {
            Kind = kind,
            IconSize = size,
        };

    public static MaterialIconKind ForPathPicker(PathPickerKind kind) => kind switch
    {
        PathPickerKind.File => MaterialIconKind.FileOutline,
        PathPickerKind.Directory => MaterialIconKind.FolderOpenOutline,
        _ => MaterialIconKind.FolderOutline,
    };

    public static Control IconText(MaterialIconKind kind, string text, double iconSize = UiMetrics.IconSize) =>
        new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = UiMetrics.IconTextSpacingPx,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                Create(kind, iconSize),
                new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center },
            },
        };
}

internal static class MdiButtons
{
    public static Button IconOnly(MaterialIconKind kind, string? tooltip = null)
    {
        var button = new Button
        {
            Content = MdiIcons.Create(kind),
            Classes = { "icon-btn" },
            VerticalAlignment = VerticalAlignment.Center,
            MinHeight = UiMetrics.ControlHeight,
            Height = UiMetrics.ControlHeight,
        };

        if (!string.IsNullOrEmpty(tooltip))
            ToolTip.SetTip(button, tooltip);

        return button;
    }

    public static Button WithIcon(MaterialIconKind kind, string text, string? tooltip = null)
    {
        var button = new Button
        {
            Content = MdiIcons.IconText(kind, text),
            Classes = { "icon-text-btn" },
            MinHeight = UiMetrics.ControlHeight,
            Height = UiMetrics.ControlHeight,
        };

        if (!string.IsNullOrEmpty(tooltip))
            ToolTip.SetTip(button, tooltip);

        return button;
    }
}
