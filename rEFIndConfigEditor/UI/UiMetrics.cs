namespace rEFIndConfigEditor.UI;

internal static class UiMetrics
{
    public const int BaselineDpi = 96;

    private static Font? _subLabel;
    private static Font? _tokenLabel;
    private static Font? _mono;
    private static string? _baseFontKey;

    public static int Scale(int px, int dpi) =>
        Math.Max(1, (int)Math.Round(px * dpi / (float)BaselineDpi));

    public static Size ScaleSize(int width, int height, int dpi) =>
        new(Scale(width, dpi), Scale(height, dpi));

    public static Padding ScalePadding(int h, int v, int dpi) =>
        new(Scale(h, dpi), Scale(v, dpi), Scale(h, dpi), Scale(v, dpi));

    private static void InvalidateFontCacheIfBaseChanged()
    {
        var baseFont = SystemFonts.DefaultFont;
        var key = $"{baseFont.FontFamily.Name}|{baseFont.SizeInPoints}|{baseFont.Style}";
        if (_baseFontKey == key)
            return;
        _baseFontKey = key;
        _subLabel?.Dispose();
        _tokenLabel?.Dispose();
        _mono?.Dispose();
        _subLabel = _tokenLabel = _mono = null;
    }

    public static Font SubLabelFont()
    {
        InvalidateFontCacheIfBaseChanged();
        if (_subLabel is not null)
            return _subLabel;
        var baseFont = SystemFonts.DefaultFont;
        _subLabel = new Font(baseFont.FontFamily, SubLabelPointSize(baseFont), baseFont.Style);
        return _subLabel;
    }

    public static Font TokenLabelFont()
    {
        InvalidateFontCacheIfBaseChanged();
        if (_tokenLabel is not null)
            return _tokenLabel;
        var baseFont = SystemFonts.DefaultFont;
        var size = MonoLabelPointSize(baseFont);
        try { _tokenLabel = new Font("Consolas", size, baseFont.Style); }
        catch { _tokenLabel = new Font(FontFamily.GenericMonospace, size, baseFont.Style); }
        return _tokenLabel;
    }

    private static float SubLabelPointSize(Font baseFont) =>
        Math.Max(7f, baseFont.SizeInPoints * 0.92f);

    private static float MonoLabelPointSize(Font baseFont) =>
        Math.Max(7.5f, baseFont.SizeInPoints * 0.95f);

    public static Font MonoFont()
    {
        InvalidateFontCacheIfBaseChanged();
        if (_mono is not null)
            return _mono;
        var baseFont = SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont ?? new Font("Segoe UI", 9f);
        var size = MonoEditorPointSize(baseFont);
        try { _mono = new Font("Consolas", size, baseFont.Style); }
        catch { _mono = new Font(FontFamily.GenericMonospace, size, baseFont.Style); }
        return _mono;
    }

    private static float MonoEditorPointSize(Font baseFont) =>
        Math.Max(8.5f, baseFont.SizeInPoints * 1.03f);

    public static int TabHeight(int dpi) => Scale(24, dpi);
    public static int TabHorizontalPadding(int dpi) => Scale(10, dpi);
    public static int TabVerticalPadding(int dpi) => Scale(4, dpi);
    public static int ControlHeight(int dpi) => Scale(22, dpi);

    public static Size SearchBoxSize(int dpi) =>
        new(Scale(200, dpi), ControlHeight(dpi));
    public static int ButtonHeight(int dpi) => Scale(26, dpi);
    public static int FieldRowStep(int dpi) => Scale(38, dpi);

    public const int OptionRowGapPx = 8;

    public static int OptionRowVGap(int dpi) => Scale(OptionRowGapPx, dpi);

    private static int OptionRowHalfGap(int dpi) => Math.Max(1, OptionRowVGap(dpi) / 2);

    public static Padding OptionRowMargin(int dpi) =>
        new(0, OptionRowHalfGap(dpi), Scale(6, dpi), OptionRowHalfGap(dpi));

    public static Padding OptionRowValueMargin(int dpi) =>
        new(0, OptionRowHalfGap(dpi), 0, OptionRowHalfGap(dpi));

    public const int TextFieldTopNudgePx = 2;

    public static int TextFieldTopNudge(int dpi) => Scale(TextFieldTopNudgePx, dpi);

    public static Padding OptionRowTextFieldMargin(int dpi) =>
        new(0, OptionRowHalfGap(dpi) + TextFieldTopNudge(dpi), 0, OptionRowHalfGap(dpi));

    public static Padding StackedItemMargin(int dpi) =>
        new(0, 0, 0, OptionRowVGap(dpi));

    public const int ToolbarFileButtonGapPx = 6;
    public const int ToolbarFileButtonWidthPx = 80;

    public static int ToolbarFileButtonWidth(int dpi) => Scale(ToolbarFileButtonWidthPx, dpi);

    public static Padding ToolbarFileButtonHostMargin(int dpi) =>
        new(0, 0, Scale(ToolbarFileButtonGapPx, dpi), 0);

    public const int StatusFooterHeightPx = 36;
    public const int StatusFooterButtonRightInsetPx = 12;
    public const int StatusFooterButtonGapPx = 8;

    public static int StatusFooterHeight(int dpi) => Scale(StatusFooterHeightPx, dpi);

    public static Padding StatusFooterPadding(int dpi) =>
        new(Scale(4, dpi), Scale(5, dpi), Scale(StatusFooterButtonRightInsetPx, dpi), Scale(5, dpi));

    public static Padding StatusFooterButtonHostMargin(int dpi, bool rightmost) =>
        rightmost ? Padding.Empty : new Padding(0, 0, Scale(StatusFooterButtonGapPx, dpi), 0);

    public const int BootButtonWidthPx = 118;
    public const int BootButtonGapPx = 8;
    public const int BootButtonRowHeightPx = 28;
    public const int BootButtonRowGapPx = 4;
    public const int BootListButtonGapPx = 8;
    public const int BootButtonBorderPx = 4;
    public const int BootButtonsPanelChromePx = 4;

    public static int BootButtonWidth(int dpi) => Scale(BootButtonWidthPx, dpi);
    public static int BootButtonRowHeight(int dpi) => Scale(BootButtonRowHeightPx, dpi);

    public static int BootButtonColumnWidth(int dpi) =>
        BootButtonWidth(dpi) + Scale(BootButtonBorderPx, dpi);

    public static int BootButtonsPanelWidth(int dpi) =>
        BootButtonColumnWidth(dpi) * 2 + Scale(BootButtonGapPx, dpi) + Scale(BootButtonsPanelChromePx, dpi);
    public static int LabelColumnWidth(int dpi) => Scale(110, dpi);
    public static int ValueColumnLeft(int dpi) => Scale(130, dpi);
    public static int TokenColWidth(int dpi) => Scale(268, dpi);
    public const int MultiRowItemPx = 19;
    public const int MultiRowPadPx = 10;

    public static int MultiRowHeight(int dpi) => Scale(92, dpi);

    public static int MultiRowHeight(int dpi, int itemCount) =>
        Scale(Math.Max(92, itemCount * MultiRowItemPx + MultiRowPadPx), dpi);
}
