using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace rEFIndConfigEditor.UI;

internal static class RawTextTabBuilder
{
    internal const string DefaultPlaceholder = "# Raw text editor\r\n";

    public static (Control Content, TextBox Editor) Build(string? initialText = null)
    {
        var editor = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Text = initialText ?? DefaultPlaceholder,
            Classes = { "raw-editor" },
        };

        return (editor, editor);
    }
}
