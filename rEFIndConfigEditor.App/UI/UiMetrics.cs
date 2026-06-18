using Avalonia;

namespace rEFIndConfigEditor.UI;

internal static class UiMetrics
{
    public const int BaselineDpi = 96;
    public const int DefaultWidthWindows = 820;
    public const int DefaultWidthMac = 700;
    public const int DefaultHeightWindows = 649;
    public const int DefaultHeightMac = 635;
    public const int MinWidthWindows = 820;
    public const int MinWidthMac = 700;
    public const int MinHeightWindows = 200;
    public const int MinHeightMac = 200;
    public static double DefaultWidth => OperatingSystem.IsMacOS() ? DefaultWidthMac : DefaultWidthWindows;
    public static double DefaultHeight => OperatingSystem.IsMacOS() ? DefaultHeightMac : DefaultHeightWindows;
    public static double MinWidth => OperatingSystem.IsMacOS() ? MinWidthMac : MinWidthWindows;
    public static double MinHeight => OperatingSystem.IsMacOS() ? MinHeightMac : MinHeightWindows;
    public const int TokenColWidth = 268;
    public const double ControlHeight = 23;
    public const double ButtonHeight = ControlHeight;
    public const double WidthNumeric = 96;
    public const int WidthCombo = 128;
    public const int WidthMulti = TextWidthLong;
    public const int WidthShort = 112;
    public const int TextWidthMedium = 220;
    public const int TextWidthLong = 360;
    public const double SearchBoxWidth = 200;
    public const double SearchBoxHeight = ControlHeight + 1;
    public const double SearchBoxBottomInsetPx = 2;
    public const double SearchDropMaxHeight = 240;
    public const int OptionRowGapPx = 8;
    public const double FieldRowStepPx = 38;
    public const double TabHeight = 22;
    public const double TabStripHeight = 24;
    public const int MacTrafficLightLeadingInset = 58;
    public const int TabHorizontalPadding = 6;
    public const int TabVerticalPadding = 3;
    public const double TabFixedWidth = 73;
    public static readonly CornerRadius TabCornerRadius = new(1.69, 1.69, 0, 0);
    public const double TabUnselectedTopInset = 2;
    public const double TabStripTrailingRuleBaselineOffsetPx = 0;
    public const double StatusFooterHeightPx = 36;
    public const double StatusFooterButtonSpacingPx = 8;
    public const int CellPadH = 8;
    public const int CellPadV = 1;
    public const int DataGridCellPad = 4;
    public const int MultiRowItemPx = 19;
    public const int MultiRowItemSpacingPx = 2;
    public const int MultiRowFramePadV = 8;
    public const int CompositeControlGapPx = 6;
    public const int CompositeRadioSpacingPx = 6;
    public const int EntryListPadV = 16;
    public const int AboutIconPx = 160;
    public const int AboutTextMaxWidthPx = 680;
    public const int TabFrameInsetPx = 9;
    public const int TabContentPaddingPx = 12;
    public const int GroupBoxPaddingH = 10;
    public const int GroupBoxPaddingTop = 12;
    public const int GroupBoxPaddingBottom = 10;
    public const int GroupBoxHeaderInsetPx = 5;
    public const int GroupBoxHeaderOverlapPx = 7;
    public const int GroupBoxHeaderBottomPx = 4;
    public const int GroupBoxBottomGapPx = 8;
    public const int GroupBoxNestedPaddingH = 8;
    public const int GroupBoxNestedPaddingTop = 10;
    public const int GroupBoxNestedPaddingBottom = 8;
    public const int AppSettingsSectionGapPx = 10;
    public const double AppSettingsControlGapPx = 6;
    public const int AppSettingsFontRowSpacingPx = 16;
    public const int AppSettingsFontLabelGapPx = 4;
    public const int AppSettingsFontLabelWidthPx = 120;
    public const int AppSettingsFontComboWidthPx = TextWidthMedium;
    public const int ThemeRadioSpacingPx = 12;
    public const double ClassicGlyphSize = 13;
    public const double IconSize = 16;
    public const double IconTextSpacingPx = 6;

    public static Thickness TabFrameMargin => new(TabFrameInsetPx, 0, TabFrameInsetPx, TabFrameInsetPx);

    public static Thickness TabContentPadding => new(TabContentPaddingPx);

    public static Thickness DataGridCellPadding => new(DataGridCellPad);

    public static Thickness GroupBoxPadding =>
        new(GroupBoxPaddingH, GroupBoxPaddingTop, GroupBoxPaddingH, GroupBoxPaddingBottom);

    public static Thickness GroupBoxNestedPadding =>
        new(GroupBoxNestedPaddingH, GroupBoxNestedPaddingTop, GroupBoxNestedPaddingH, GroupBoxNestedPaddingBottom);

    public static Thickness GroupBoxHeaderMargin =>
        new(GroupBoxHeaderInsetPx - GroupBoxPaddingH - 1, -GroupBoxHeaderOverlapPx, 8, GroupBoxHeaderBottomPx);

    public static Thickness GroupBoxNestedHeaderMargin =>
        new(GroupBoxHeaderInsetPx - GroupBoxNestedPaddingH - 1, -GroupBoxHeaderOverlapPx, 8, GroupBoxHeaderBottomPx);

    public static Thickness GroupBoxMargin => new(0, 0, 0, GroupBoxBottomGapPx);

    public static Thickness OptionRowMargin =>
        new(0, OptionRowGapPx / 2.0, 6, OptionRowGapPx / 2.0);

    public static Thickness StatusFooterPadding => new(12, 5, 12, 5);

    public static Thickness TabItemPadding =>
        new(TabHorizontalPadding, TabVerticalPadding, TabHorizontalPadding, TabVerticalPadding);

    public static Thickness TabUnselectedMargin =>
        new(0, TabUnselectedTopInset, -1, 0);

    public static Thickness SearchBoxMargin =>
        new(0, SearchBoxBottomFromStripTop - SearchBoxHeight, 8, 0);

    public static Thickness TabStripItemsMargin => new(4, 0, 0, 0);

    public static double SearchBoxBottomFromStripTop =>
        (TabStripHeight - ControlHeight) / 2.0 + ControlHeight + SearchBoxBottomInsetPx;

    public static int MultiRowHeight(int count) =>
        count <= 0
            ? (int)ControlHeight
            : count * MultiRowItemPx + Math.Max(0, count - 1) * MultiRowItemSpacingPx + MultiRowFramePadV;

    public static int EntryListHeight(int count) =>
        count <= 0 ? (int)ControlHeight : count * MultiRowItemPx + EntryListPadV;

    public const int BootButtonWidth = 118;
    public const int BootButtonGapPx = 8;
    public const int BootButtonRowHeight = 28;
    public const int BootButtonRowGapPx = 4;
    public const int BootListButtonGapPx = 8;
}
