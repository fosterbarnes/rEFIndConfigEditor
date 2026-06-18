using Avalonia;
using Avalonia.Controls;
using rEFIndConfigEditor.Platform;
using System.Windows.Input;

namespace rEFIndConfigEditor.UI;

internal static class UiLinks
{
    public static HyperlinkButton Command(string text, ICommand command) =>
        new()
        {
            Content = new TextBlock { Text = text },
            Command = command,
            Classes = { "app-link" },
        };

    public static HyperlinkButton Url(string text, string url)
    {
        var link = new HyperlinkButton
        {
            Content = new TextBlock { Text = text },
            Classes = { "app-link" },
            Padding = new Thickness(0),
        };
        link.Click += (_, _) => PlatformServices.Current.OpenUrl(url);
        return link;
    }
}
