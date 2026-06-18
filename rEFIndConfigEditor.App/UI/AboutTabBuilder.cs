using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using rEFIndConfigEditor;
using rEFIndConfigEditor.ViewModels;

namespace rEFIndConfigEditor.UI;

internal static class AboutTabBuilder
{
    public static Control Build(MainWindowViewModel viewModel)
    {
        var icon = new Image
        {
            Width = UiMetrics.AboutIconPx,
            Height = UiMetrics.AboutIconPx,
            Margin = new Thickness(0, 2, 8, 0),
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        AppIconLoader.TryLoadAboutIcon(icon);

        var version = new SelectableTextBlock { Text = viewModel.VersionText };
        var repo = UiLinks.Command(viewModel.RepoUrl, viewModel.OpenRepoCommand);

        var checkBtn = new Button { Content = "Check for updates" };
        checkBtn.Click += (_, _) => viewModel.CheckForUpdatesManualCommand.Execute(null);

        var lead = InlineText(
            "A gui config editor for ",
            ("rEFInd", AppBranding.RefindDocsUrl),
            ", a UEFI boot manager compatible with Linux, MacOS & Windows.");

        var blurb = InlineText(
            "Create a config with a gui with friendly names, tooltips, and references for each token. " +
            "Includes extras like theme management and a theme browser. Covers every setting listed on the ",
            ("config file instruction page", AppBranding.ConfigDocUrl),
            ", with links for each.");

        var wiki = new WrapPanel
        {
            MaxWidth = UiMetrics.AboutTextMaxWidthPx,
            Children =
            {
                UiLinks.Url("Wiki: Tokens", AppBranding.WikiTokensUrl),
                new SelectableTextBlock { Text = " · " },
                UiLinks.Url("Wiki: Themes", AppBranding.WikiThemesUrl),
            },
        };

        var textStack = new StackPanel
        {
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Left,
            Children = { version, repo, lead, blurb, wiki, checkBtn },
        };

        var layout = new Grid
        {
            Margin = UiMetrics.TabContentPadding,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            RowDefinitions = new RowDefinitions("Auto"),
            Children = { icon, textStack },
        };
        Grid.SetColumn(textStack, 1);
        return layout;
    }

    private static WrapPanel InlineText(string prefix, (string Phrase, string Url) link, string suffix)
    {
        var panel = new WrapPanel { MaxWidth = UiMetrics.AboutTextMaxWidthPx };
        if (!string.IsNullOrEmpty(prefix))
            panel.Children.Add(new SelectableTextBlock { Text = prefix, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(UiLinks.Url(link.Phrase, link.Url));
        if (!string.IsNullOrEmpty(suffix))
            panel.Children.Add(new SelectableTextBlock { Text = suffix, TextWrapping = TextWrapping.Wrap });
        return panel;
    }
}
