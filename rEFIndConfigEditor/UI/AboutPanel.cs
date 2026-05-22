using System.Reflection;

namespace rEFIndConfigEditor.UI;

internal sealed class AboutPanel : Panel
{
    private const int IconBaselinePx = 160;
    private const int BlurbMaxWidthBaselinePx = 680;
    private const string RepoUrl = "https://github.com/fosterbarnes/rEFIndConfigEditor";
    private const string RefindUrl = "https://www.rodsbooks.com/refind/";
    private const string ConfigDocUrl = "https://www.rodsbooks.com/refind/configfile.html";

    private readonly PictureBox _icon;
    private readonly TableLayoutPanel _blurbRow;
    private readonly Label _versionLabel;
    private readonly LinkLabel _repoLink;
    private readonly LinkLabel _leadText;
    private readonly LinkLabel _blurbText;
    private int _dpi;

    public AboutPanel(int dpi)
    {
        Dock = DockStyle.Fill;

        _blurbRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 1,
            Padding = Padding.Empty,
            Margin = Padding.Empty
        };
        _blurbRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, IconBaselinePx + 8));
        _blurbRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        _blurbRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _icon = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Zoom,
            Margin = new Padding(0, 2, 8, 0),
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };
        _icon.Image = TryLoadIconBitmap();

        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?";

        _versionLabel = new Label
        {
            Text = $"rEFInd Config Editor v{version}  - Copyright © 2026 FosterBarnes",
            AutoSize = true,
            TextAlign = ContentAlignment.TopLeft
        };

        _repoLink = CreateLink(RepoUrl, RepoUrl);

        _leadText = CreateLinkedText(
            "A gui config editor for rEFInd, a UEFI boot manager compatible with Linux, MacOS & Windows.",
            [("rEFInd", RefindUrl)]);

        _blurbText = CreateLinkedText(
            "Create a config with a gui with friendly names, tooltips, and references for each token. "
            + "Includes some extras like theme management and a theme browser. Covers every setting listed on the "
            + "config file instruction page, with links for each.",
            [("config file instruction page", ConfigDocUrl)]);

        var textStack = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = Padding.Empty,
            Margin = new Padding(0, 3, 0, 0)
        };
        textStack.Controls.Add(_versionLabel);
        textStack.Controls.Add(_repoLink);
        textStack.Controls.Add(_leadText);
        textStack.Controls.Add(_blurbText);

        _blurbRow.Controls.Add(_icon, 0, 0);
        _blurbRow.Controls.Add(textStack, 1, 0);
        Controls.Add(_blurbRow);

        ApplyMetrics(dpi);
    }

    public void ApplyMetrics(int dpi)
    {
        _dpi = dpi;
        Padding = UiMetrics.ScalePadding(12, 12, dpi);

        var iconPx = UiMetrics.Scale(IconBaselinePx, dpi);
        _icon.Size = new Size(iconPx, iconPx);
        _blurbRow.ColumnStyles[0].Width = iconPx + UiMetrics.Scale(8, dpi);

        var tight = new Padding(0, 0, 0, UiMetrics.Scale(2, dpi));
        var between = new Padding(0, 0, 0, UiMetrics.Scale(12, dpi));
        var blurbWidth = UiMetrics.Scale(BlurbMaxWidthBaselinePx, dpi);

        _versionLabel.Margin = tight;
        _repoLink.Margin = between;
        _leadText.Margin = tight;
        _leadText.MaximumSize = new Size(blurbWidth, 0);
        _blurbText.Margin = Padding.Empty;
        _blurbText.MaximumSize = new Size(blurbWidth, 0);
    }

    private static LinkLabel CreateLink(string text, string url)
    {
        var link = new LinkLabel
        {
            Text = text,
            AutoSize = true,
            LinkBehavior = LinkBehavior.HoverUnderline,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            UseCompatibleTextRendering = true
        };
        link.LinkArea = new LinkArea(0, link.Text.Length);
        link.LinkClicked += (_, _) => TokenDocLinks.OpenInBrowser(url);
        return link;
    }

    private static LinkLabel CreateLinkedText(string text, (string Phrase, string Url)[] links)
    {
        var label = new LinkLabel
        {
            Text = text,
            AutoSize = true,
            LinkBehavior = LinkBehavior.HoverUnderline,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            UseCompatibleTextRendering = true
        };

        foreach (var (phrase, url) in links)
        {
            var idx = text.IndexOf(phrase, StringComparison.Ordinal);
            if (idx >= 0)
                label.Links.Add(idx, phrase.Length, url);
        }

        label.LinkClicked += (_, e) =>
        {
            if (e.Link?.LinkData is string url)
                TokenDocLinks.OpenInBrowser(url);
        };
        return label;
    }

    private Image? TryLoadIconBitmap()
    {
        try
        {
            var dir = AppContext.BaseDirectory;
            if (string.IsNullOrEmpty(dir))
                dir = Application.StartupPath ?? ".";

            var pngPath = Path.Combine(dir, "refind256.png");
            if (File.Exists(pngPath))
            {
                using var fs = File.OpenRead(pngPath);
                using var temp = Image.FromStream(fs);
                return new Bitmap(temp);
            }

            var icoPath = Path.Combine(dir, "refind.ico");
            if (!File.Exists(icoPath))
                return null;

            var iconPx = UiMetrics.Scale(IconBaselinePx, _dpi > 0 ? _dpi : UiMetrics.BaselineDpi);
            using var ico = new Icon(icoPath, iconPx, iconPx);
            return ico.ToBitmap();
        }
        catch
        {
            return null;
        }
    }
}
