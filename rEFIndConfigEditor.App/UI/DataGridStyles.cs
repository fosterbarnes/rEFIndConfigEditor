using Avalonia;
using Avalonia.Markup.Xaml.Styling;

namespace rEFIndConfigEditor.UI;

internal static class DataGridStyles
{
    private static readonly Uri FluentThemeSource =
        new("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml");

    private static bool _loaded;

    public static void EnsureLoaded()
    {
        if (_loaded || Application.Current?.Styles is not { } styles)
            return;

        styles.Add(new StyleInclude(FluentThemeSource) { Source = FluentThemeSource });
        _loaded = true;
    }
}
