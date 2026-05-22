using rEFIndConfigEditor.Config;

namespace rEFIndConfigEditor.UI;

internal static class TokenDocLinks
{
    private const string BaseUrl =
        "https://github.com/fosterbarnes/rEFIndConfigEditor/blob/main/.resources/docs/tokens.md";

    public static string UrlFor(string token) =>
        $"{BaseUrl}#{RefindTokens.Canonicalize(token)}";

    public static LinkLabel CreateLabel(string token, int dpi, bool monospace = true)
    {
        var display = RefindTokens.Canonicalize(token);
        var link = new LinkLabel
        {
            Text = display,
            AutoSize = true,
            LinkBehavior = LinkBehavior.HoverUnderline,
            Tag = monospace ? "token" : "sublabel"
        };
        link.LinkArea = new LinkArea(0, link.Text.Length);
        link.Font = monospace ? UiMetrics.TokenLabelFont() : UiMetrics.SubLabelFont();
        link.Margin = new Padding(UiMetrics.Scale(18, dpi), 0, 0, 0);
        link.LinkClicked += (_, _) => OpenInBrowser(UrlFor(token));
        return link;
    }

    public static void OpenInBrowser(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "rEFInd Config Editor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
