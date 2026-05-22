namespace rEFIndConfigEditor.UI;

internal static class BootButtonsLayout
{
    public static void Apply(
        Panel host,
        IReadOnlyList<Button> buttons,
        CheckBox? skipOther,
        TableLayoutPanel? split,
        int dpi)
    {
        if (buttons.Count < 7)
            return;

        var bw = UiMetrics.BootButtonWidth(dpi);
        var bh = UiMetrics.BootButtonRowHeight(dpi);
        var rowGap = UiMetrics.Scale(UiMetrics.BootButtonRowGapPx, dpi);
        var x1 = UiMetrics.BootButtonColumnWidth(dpi) + UiMetrics.Scale(UiMetrics.BootButtonGapPx, dpi);
        var step = bh + rowGap;

        for (var row = 0; row < 5; row++)
            Place(buttons[row], 0, row * step, bw, bh);

        for (var row = 0; row < 2; row++)
            Place(buttons[5 + row], x1, row * step, bw, bh);

        var panelW = UiMetrics.BootButtonsPanelWidth(dpi);

        if (skipOther is not null)
            Place(skipOther, 0, 5 * step + rowGap, panelW, bh);

        var panelH = skipOther?.Bottom ?? 5 * step - rowGap;
        host.Size = new Size(panelW, panelH);

        if (split is not null)
            split.ColumnStyles[1] = new ColumnStyle(SizeType.Absolute, panelW);
    }

    private static void Place(Control c, int x, int y, int w, int h)
    {
        c.Size = new Size(w, h);
        c.Location = new Point(x, y);
        c.Margin = Padding.Empty;
        c.Anchor = AnchorStyles.Top | AnchorStyles.Left;
    }
}
