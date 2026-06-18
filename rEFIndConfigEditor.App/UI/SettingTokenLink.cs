using Avalonia.Controls;
using Avalonia.Input;
using rEFIndConfigEditor.Platform;
using rEFIndConfigEditor.Settings;

namespace rEFIndConfigEditor.UI;

internal static class SettingTokenLink
{
    public static Control Create(string token)
    {
        var url = TokenDocLinks.UrlFor(token);
        if (url is null)
        {
            return new TextBlock
            {
                Text = token,
                Classes = { "setting-token" },
            };
        }

        var link = new TextBlock
        {
            Text = token,
            Classes = { "setting-token", "setting-token-link" },
        };
        link.PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(link).Properties.IsLeftButtonPressed)
            {
                PlatformServices.Current.OpenUrl(url);
                e.Handled = true;
            }
        };
        return link;
    }
}
