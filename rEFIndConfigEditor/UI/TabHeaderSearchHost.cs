namespace rEFIndConfigEditor.UI;

internal sealed class TabHeaderSearchHost : Panel
{
    private CenteredTabControl? _tabs;

    internal TextBox SearchBox { get; }

    public TabHeaderSearchHost(TextBox searchBox)
    {
        SearchBox = searchBox;
        TabStop = false;
        Padding = Padding.Empty;
        Margin = Padding.Empty;
        Controls.Add(searchBox);
    }

    internal void Attach(CenteredTabControl tabs, Control layoutParent)
    {
        _tabs = tabs;
        layoutParent.Resize += (_, _) => Reposition();
        tabs.Resize += (_, _) => Reposition();
        tabs.FontChanged += (_, _) => Reposition();
    }

    internal void ApplyMetrics(int dpi)
    {
        SearchBox.Size = UiMetrics.SearchBoxSize(dpi);
        Reposition();
    }

    internal void SyncHeaderColors(CenteredTabControl tabs)
    {
        BackColor = tabs.TabStripBack;
        SearchBox.BackColor = tabs.TabStripBack;
        SearchBox.ForeColor = tabs.TabHeaderFore;
    }

    internal void Reposition()
    {
        if (_tabs is null || Parent is null)
            return;

        var dpi = _tabs.DeviceDpi;
        var margin = UiMetrics.Scale(6, dpi);
        var rowH = _tabs.ItemSize.Height + _tabs.Padding.Y * 2;
        var hostW = SearchBox.Width + margin * 2;
        var priorInset = _tabs.HeaderRightInset;
        Bounds = new Rectangle(Parent.ClientSize.Width - hostW, 0, hostW, rowH);
        SearchBox.Location = new Point(margin, Math.Max(0, (rowH - SearchBox.Height) / 2));
        _tabs.HeaderRightInset = hostW;
        if (priorInset != hostW && _tabs.IsHandleCreated)
            _tabs.Invalidate(new Rectangle(0, 0, _tabs.Width, rowH));
    }
}
