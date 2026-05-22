using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace rEFIndConfigEditor.UI;

internal sealed class CenteredTabControl : TabControl
{
    private const int WM_PAINT = 0x000F;
    private int _horizontalPadPx = 10;
    private bool _flatChrome;
    private SolidBrush? _stripBrush;
    private SolidBrush? _headerBrush;
    private SolidBrush? _selectedHeaderBrush;

    internal Color TabHeaderBack { get; set; } = SystemColors.Control;
    internal Color TabHeaderFore { get; set; } = SystemColors.ControlText;
    internal Color SelectedTabHeaderBack { get; set; } = SystemColors.Window;
    internal Color SelectedTabHeaderFore { get; set; } = SystemColors.ControlText;
    internal Color TabStripBack { get; set; } = SystemColors.Control;
    internal int HeaderRightInset { get; set; }

    internal bool FlatChrome
    {
        get => _flatChrome;
        set
        {
            if (_flatChrome == value)
                return;
            _flatChrome = value;
            ApplyChromeTheme();
            Invalidate(true);
        }
    }

    public CenteredTabControl()
    {
        DrawMode = TabDrawMode.OwnerDrawFixed;
        SizeMode = TabSizeMode.Fixed;
        DrawItem += OnDrawItem;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyChromeTheme();
    }

    private void ApplyChromeTheme()
    {
        if (!IsHandleCreated)
            return;
        SetWindowTheme(Handle, _flatChrome ? "" : null, _flatChrome ? "" : null);
    }

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hWnd, string? pszSubAppName, string? pszSubIdList);

    internal void ApplyTabMetrics(int dpi)
    {
        _horizontalPadPx = UiMetrics.TabHorizontalPadding(dpi);
        Padding = new Point(_horizontalPadPx, UiMetrics.TabVerticalPadding(dpi));
        EnsureTabLabels();
        ItemSize = new Size(UniformTabWidth(), UiMetrics.TabHeight(dpi));
        if (IsHandleCreated)
            Invalidate(true);
    }

    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        if (e.Control is TabPage page)
            EnsureTabLabel(page);
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (SizeMode == TabSizeMode.Fixed && TabPages.Count > 0)
            ItemSize = new Size(UniformTabWidth(), ItemSize.Height);
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg != WM_PAINT)
            return;

        using var g = Graphics.FromHwnd(Handle);
        var rowH = ItemSize.Height + Padding.Y * 2;
        var stripBrush = StripBrush();

        if (_flatChrome)
        {
            g.FillRectangle(stripBrush, new Rectangle(0, 0, Width, rowH));

            for (int i = 0; i < TabPages.Count; i++)
                DrawTabItem(g, i);

            var displayRect = DisplayRectangle;
            int topGap = displayRect.Y - rowH;
            if (topGap > 0)
                g.FillRectangle(stripBrush, 0, rowH, Width, topGap);
            int leftGap = displayRect.X;
            if (leftGap > 0)
                g.FillRectangle(stripBrush, 0, displayRect.Y, leftGap, Height - displayRect.Y);
            int rightStart = displayRect.Right;
            if (rightStart < Width)
                g.FillRectangle(stripBrush, rightStart, displayRect.Y, Width - rightStart, Height - displayRect.Y);
            int bottomStart = displayRect.Bottom;
            if (bottomStart < Height)
                g.FillRectangle(stripBrush, 0, bottomStart, Width, Height - bottomStart);
        }

        if (HeaderRightInset > 0)
            g.FillRectangle(stripBrush, new Rectangle(Width - HeaderRightInset, 0, HeaderRightInset, rowH));
    }

    private SolidBrush StripBrush() => Resync(ref _stripBrush, TabStripBack);
    private SolidBrush HeaderBrush() => Resync(ref _headerBrush, TabHeaderBack);
    private SolidBrush SelectedHeaderBrush() => Resync(ref _selectedHeaderBrush, SelectedTabHeaderBack);

    private static SolidBrush Resync(ref SolidBrush? brush, Color color)
    {
        if (brush is null || brush.Color != color)
        {
            brush?.Dispose();
            brush = new SolidBrush(color);
        }
        return brush;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _stripBrush?.Dispose();
            _headerBrush?.Dispose();
            _selectedHeaderBrush?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void DrawTabItem(Graphics g, int index)
    {
        var page = TabPages[index];
        var label = page.Tag as string ?? page.Text;
        var selected = index == SelectedIndex;
        var brush = selected ? SelectedHeaderBrush() : HeaderBrush();
        var fore = selected ? SelectedTabHeaderFore : TabHeaderFore;
        var bounds = GetTabRect(index);
        var fillBounds = new Rectangle(bounds.X, bounds.Y, Math.Max(0, bounds.Width - 1), bounds.Height);

        g.FillRectangle(brush, fillBounds);

        TextRenderer.DrawText(
            g,
            label,
            Font,
            bounds,
            fore,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
    }

    private void EnsureTabLabels()
    {
        foreach (TabPage page in TabPages)
            EnsureTabLabel(page);
    }

    private static void EnsureTabLabel(TabPage page)
    {
        if (page.Tag is not string)
            page.Tag = page.Text;
    }

    private int UniformTabWidth()
    {
        if (TabPages.Count == 0)
            return UiMetrics.Scale(96, DeviceDpi);

        var maxText = 0;
        foreach (TabPage page in TabPages)
        {
            var label = page.Tag as string ?? page.Text;
            var w = TextRenderer.MeasureText(label, Font, Size.Empty, TextFormatFlags.NoPadding).Width;
            if (w > maxText)
                maxText = w;
        }

        return maxText + _horizontalPadPx * 2;
    }

    private void OnDrawItem(object? sender, DrawItemEventArgs e)
    {
        if (_flatChrome)
            return;
        if (e.Index < 0 || e.Index >= TabPages.Count)
            return;

        var page = TabPages[e.Index];
        var label = page.Tag as string ?? page.Text;
        var selected = e.Index == SelectedIndex;
        var brush = selected ? SelectedHeaderBrush() : HeaderBrush();
        var fore = selected ? SelectedTabHeaderFore : TabHeaderFore;

        e.Graphics.FillRectangle(brush, e.Bounds);

        TextRenderer.DrawText(
            e.Graphics,
            label,
            Font,
            e.Bounds,
            fore,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
    }
}
