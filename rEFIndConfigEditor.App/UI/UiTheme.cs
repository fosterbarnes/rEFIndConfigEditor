using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using rEFIndConfigEditor.Models;

namespace rEFIndConfigEditor.UI;

internal static class UiTheme
{
    private readonly struct ThemePalette
    {
        public Color MainBorder { get; init; }
        public Color MainText { get; init; }
        public Color InputBg { get; init; }
        public Color FieldBg { get; init; }
        public Color FormBg { get; init; }
        public Color SurfaceBg { get; init; }
        public Color MutedText { get; init; }
        public Color Accent { get; init; }
        public Color Link { get; init; }
        public Color LinkHover { get; init; }
        public Color MenuBack { get; init; }

        public Color? TabStripBg { get; init; }
        public Color? TabHeaderBg { get; init; }
        public Color? TabSelectedBg { get; init; }
        public Color? TabHoverBg { get; init; }
        public Color? TabBevel { get; init; }

        public Color? TabBorder { get; init; }
        public Color? SearchBorder { get; init; }
        public Color? CheckBoxBorder { get; init; }
        public Color? NumberBoxBorder { get; init; }
        public Color? ChoiceBoxBorder { get; init; }
        public Color? MultiSelectBoxBorder { get; init; }
        public Color? StringBoxBorder { get; init; }
        public Color? ButtonBorder { get; init; }
        public Color? CompositeControlBoxBorder { get; init; }
        public Color? EntryListBorder { get; init; }
        public Color? RadioGroupBorder { get; init; }

        public Color? TabText { get; init; }
        public Color? SearchText { get; init; }
        public Color? SettingText { get; init; }
        public Color? TokenText { get; init; }
        public Color? NumberBoxText { get; init; }
        public Color? ChoiceBoxText { get; init; }
        public Color? MultiSelectBoxText { get; init; }
        public Color? StringBoxText { get; init; }
        public Color? ButtonForeground { get; init; }
        public Color? CompositeControlBoxText { get; init; }
        public Color? EntryListText { get; init; }
        public Color? RadioGroupText { get; init; }

        public Color? TabBg { get; init; }
        public Color? SearchBg { get; init; }
        public Color? CheckBoxBg { get; init; }
        public Color? ActivatedCheckBox { get; init; }
        public Color? NumberBoxBg { get; init; }
        public Color? ChoiceBoxBg { get; init; }
        public Color? MultiSelectBoxBg { get; init; }
        public Color? StringBoxBg { get; init; }
        public Color? ButtonBg { get; init; }
        public Color? ButtonHoverBg { get; init; }
        public Color? ButtonPressedBg { get; init; }
        public Color? CompositeControlBoxBg { get; init; }
        public Color? EntryListBg { get; init; }
    }

    private static readonly ThemePalette LightPalette = new()
    {
        MainBorder = Color.Parse("#ADADAD"),
        MainText = Color.Parse("#000000"),
        InputBg = Color.Parse("#FFFFFF"),
        FieldBg = Color.Parse("#FFFFFF"),
        FormBg = Color.Parse("#F0F0F0"),
        SurfaceBg = Color.Parse("#E8E8E8"),
        MutedText = Color.Parse("#666666"),
        TokenText = Color.Parse("#0066CC"),
        Accent = Color.Parse("#0078D4"),
        Link = Color.Parse("#0066CC"),
        LinkHover = Color.Parse("#004E9A"),
        MenuBack = Color.Parse("#F0F0F0"),

        TabSelectedBg = Color.Parse("#F0F0F0"),
        TabHeaderBg = Color.Parse("#E8E8E8"),
        TabHoverBg = Color.Parse("#E2E1E1"),
        TabBorder = Color.Parse("#A0A0A0"),
        TabBevel = Color.Parse("#FFFFFF"),
    };

    private static readonly ThemePalette VsDarkPalette = new()
    {
        MainBorder = Color.Parse("#646464"),
        MainText = Color.Parse("#DCDCDC"),
        InputBg = Color.Parse("#1E1E1E"),
        FieldBg = Color.Parse("#3C3C3C"),
        FormBg = Color.Parse("#1E1E1E"),
        SurfaceBg = Color.Parse("#2D2D30"),
        MutedText = Color.Parse("#969696"),
        TokenText = Color.Parse("#12A0FF"),
        Accent = Color.Parse("#007ACC"),
        Link = Color.Parse("#64B4FF"),
        LinkHover = Color.Parse("#99CCFF"),
        MenuBack = Color.Parse("#282828"),
    };

    private static readonly ThemePalette DraculaPalette = new()
    {
        MainBorder = Color.Parse("#6272A4"),
        MainText = Color.Parse("#F8F8F2"),
        InputBg = Color.Parse("#282A36"),
        FieldBg = Color.Parse("#424450"),
        FormBg = Color.Parse("#282A36"),
        SurfaceBg = Color.Parse("#343746"),
        MutedText = Color.Parse("#6272A4"),
        SettingText = Color.Parse("#FFFFFF"),
        TokenText = Color.Parse("#FC7DC6"),
        Accent = Color.Parse("#BD93F9"),
        Link = Color.Parse("#8BE9FD"),
        LinkHover = Color.Parse("#BD93F9"),
        MenuBack = Color.Parse("#191A21"),
    };

    public static ThemeVariant ResolveThemeVariant(UiThemeKind theme) =>
        EffectiveTheme(theme) == UiThemeKind.Light ? ThemeVariant.Light : ThemeVariant.Dark;

    public static UiThemeKind EffectiveTheme(UiThemeKind theme)
    {
        if (theme == UiThemeKind.System)
            return Platform.PlatformServices.Current.IsSystemDarkTheme()
                ? UiThemeKind.Dark
                : UiThemeKind.Light;
        return theme;
    }

    public static void ApplyWindowThemeVariant(Window window, UiThemeKind theme) =>
        window.RequestedThemeVariant = ResolveThemeVariant(theme);

    public static void ApplyWindowTheme(Window window, UiThemeKind theme)
    {
        ApplyWindowThemeVariant(window, theme);
        if (Application.Current is { } app)
            ApplyAppTheme(app, theme);
    }

    public static void ApplyAppTheme(Application app, UiThemeKind theme)
    {
        var effective = EffectiveTheme(theme);
        var palette = GetThemePalette(effective);
        ApplyThemeBrushes(app.Resources, palette);
        ApplyFluentControlResources(app.Resources, palette);
        if (GetFluentTheme(app) is { } fluent)
        {
            var paletteVariant = effective is UiThemeKind.Light
                ? ThemeVariant.Light
                : ThemeVariant.Dark;
            ApplyFluentPalette(fluent, palette, paletteVariant);
        }
    }

    private static ThemePalette GetThemePalette(UiThemeKind effective) => effective switch
    {
        UiThemeKind.Dracula => DraculaPalette,
        UiThemeKind.Dark => VsDarkPalette,
        _ => LightPalette,
    };

    private static void ApplyThemeBrushes(IResourceDictionary resources, ThemePalette p)
    {
        static Color Role(Color? o, Color group) => o ?? group;

        var tabStripBg = Role(p.TabStripBg, p.FormBg);
        var tabHeaderBg = Role(p.TabHeaderBg, p.SurfaceBg);
        var tabSelectedBg = Role(p.TabSelectedBg, p.FormBg);
        var tabHoverBg = Role(p.TabHoverBg, p.FieldBg);

        SetBrush(resources, ThemeBrushKeys.MainBorder, p.MainBorder);
        SetBrush(resources, ThemeBrushKeys.MainText, p.MainText);
        SetBrush(resources, ThemeBrushKeys.InputBg, p.InputBg);
        SetBrush(resources, ThemeBrushKeys.FieldBg, p.FieldBg);
        SetBrush(resources, ThemeBrushKeys.FormBg, p.FormBg);
        SetBrush(resources, ThemeBrushKeys.SurfaceBg, p.SurfaceBg);
        SetBrush(resources, ThemeBrushKeys.MutedText, p.MutedText);
        SetBrush(resources, ThemeBrushKeys.Accent, p.Accent);
        SetBrush(resources, ThemeBrushKeys.Link, p.Link);
        SetBrush(resources, ThemeBrushKeys.LinkHover, p.LinkHover);
        SetBrush(resources, ThemeBrushKeys.MenuBack, p.MenuBack);

        SetBrush(resources, ThemeBrushKeys.TabStripBg, tabStripBg);
        SetBrush(resources, ThemeBrushKeys.TabHeaderBg, tabHeaderBg);
        SetBrush(resources, ThemeBrushKeys.TabSelectedBg, tabSelectedBg);
        SetBrush(resources, ThemeBrushKeys.TabHoverBg, tabHoverBg);

        resources[ThemeBrushKeys.TabBevelHighlight] = p.TabBevel is { } bevel
            ? new BoxShadows(new BoxShadow { IsInset = true, OffsetY = 1, Color = bevel })
            : default(BoxShadows);

        SetBrush(resources, ThemeBrushKeys.TabBorder, Role(p.TabBorder, p.MainBorder));
        SetBrush(resources, ThemeBrushKeys.SearchBorder, Role(p.SearchBorder, p.MainBorder));
        SetBrush(resources, ThemeBrushKeys.CheckBoxBorder, Role(p.CheckBoxBorder, p.MainBorder));
        SetBrush(resources, ThemeBrushKeys.NumberBoxBorder, Role(p.NumberBoxBorder, p.MainBorder));
        SetBrush(resources, ThemeBrushKeys.ChoiceBoxBorder, Role(p.ChoiceBoxBorder, p.MainBorder));
        SetBrush(resources, ThemeBrushKeys.MultiSelectBoxBorder, Role(p.MultiSelectBoxBorder, p.MainBorder));
        SetBrush(resources, ThemeBrushKeys.StringBoxBorder, Role(p.StringBoxBorder, p.MainBorder));
        SetBrush(resources, ThemeBrushKeys.ButtonBorder, Role(p.ButtonBorder, p.MainBorder));
        SetBrush(resources, ThemeBrushKeys.CompositeControlBoxBorder, Role(p.CompositeControlBoxBorder, p.MainBorder));
        SetBrush(resources, ThemeBrushKeys.EntryListBorder, Role(p.EntryListBorder, p.MainBorder));
        SetBrush(resources, ThemeBrushKeys.RadioGroupBorder, Role(p.RadioGroupBorder, p.MainBorder));

        SetBrush(resources, ThemeBrushKeys.TabText, Role(p.TabText, p.MainText));
        SetBrush(resources, ThemeBrushKeys.SearchText, Role(p.SearchText, p.MainText));
        SetBrush(resources, ThemeBrushKeys.SettingText, Role(p.SettingText, p.MainText));
        SetBrush(resources, ThemeBrushKeys.TokenText, Role(p.TokenText, p.MutedText));
        SetBrush(resources, ThemeBrushKeys.NumberBoxText, Role(p.NumberBoxText, p.MainText));
        SetBrush(resources, ThemeBrushKeys.ChoiceBoxText, Role(p.ChoiceBoxText, p.MainText));
        SetBrush(resources, ThemeBrushKeys.MultiSelectBoxText, Role(p.MultiSelectBoxText, p.MainText));
        SetBrush(resources, ThemeBrushKeys.StringBoxText, Role(p.StringBoxText, p.MainText));
        SetBrush(resources, ThemeBrushKeys.ButtonForeground, Role(p.ButtonForeground, p.MainText));
        SetBrush(resources, ThemeBrushKeys.CompositeControlBoxText, Role(p.CompositeControlBoxText, p.MainText));
        SetBrush(resources, ThemeBrushKeys.EntryListText, Role(p.EntryListText, p.MainText));
        SetBrush(resources, ThemeBrushKeys.RadioGroupText, Role(p.RadioGroupText, p.MainText));

        SetBrush(resources, ThemeBrushKeys.TabBg, Role(p.TabBg, tabHeaderBg));
        SetBrush(resources, ThemeBrushKeys.SearchBg, Role(p.SearchBg, p.InputBg));
        SetBrush(resources, ThemeBrushKeys.CheckBoxBg, Role(p.CheckBoxBg, p.FieldBg));
        SetBrush(resources, ThemeBrushKeys.ActivatedCheckBox, Role(p.ActivatedCheckBox, p.Accent));
        SetBrush(resources, ThemeBrushKeys.NumberBoxBg, Role(p.NumberBoxBg, p.InputBg));
        SetBrush(resources, ThemeBrushKeys.ChoiceBoxBg, Role(p.ChoiceBoxBg, p.InputBg));
        SetBrush(resources, ThemeBrushKeys.MultiSelectBoxBg, Role(p.MultiSelectBoxBg, p.InputBg));
        SetBrush(resources, ThemeBrushKeys.StringBoxBg, Role(p.StringBoxBg, p.InputBg));
        var buttonBg = Role(p.ButtonBg, p.InputBg);
        SetBrush(resources, ThemeBrushKeys.ButtonBg, buttonBg);
        SetBrush(resources, ThemeBrushKeys.ButtonHoverBg, Role(p.ButtonHoverBg, Blend(buttonBg, p.Accent, 0.14)));
        SetBrush(resources, ThemeBrushKeys.ButtonPressedBg, Role(p.ButtonPressedBg, Blend(buttonBg, p.Accent, 0.24)));
        SetBrush(resources, ThemeBrushKeys.CompositeControlBoxBg, Role(p.CompositeControlBoxBg, p.InputBg));
        SetBrush(resources, ThemeBrushKeys.EntryListBg, Role(p.EntryListBg, p.InputBg));
        SetBrush(resources, ThemeBrushKeys.RadioGroupBg, Colors.Transparent);
    }

    private static void SetBrush(IResourceDictionary resources, string key, Color color) =>
        resources[key] = new SolidColorBrush(color);

    private static Color Blend(Color background, Color foreground, double foregroundWeight)
    {
        var w = foregroundWeight;
        var iw = 1.0 - w;
        return Color.FromArgb(
            255,
            (byte)(background.R * iw + foreground.R * w),
            (byte)(background.G * iw + foreground.G * w),
            (byte)(background.B * iw + foreground.B * w));
    }

    private static void ApplyFluentControlResources(IResourceDictionary resources, ThemePalette palette)
    {
        var input = new SolidColorBrush(palette.InputBg);

        resources["TextControlBackground"] = input;
        resources["TextControlBackgroundPointerOver"] = input;
        resources["TextControlBackgroundFocused"] = input;
        resources["TextControlBackgroundDisabled"] = input;
        resources["TextControlBorderThemeThicknessFocused"] = new Thickness(1);

        resources["ComboBoxBackground"] = input;
        resources["ComboBoxBackgroundPointerOver"] = input;
        resources["ComboBoxBackgroundPressed"] = input;
        resources["ComboBoxBackgroundDisabled"] = input;
        resources["ComboBoxBackgroundUnfocused"] = input;

        resources["SystemControlBackgroundChromeMediumLowBrush"] = input;
    }

    private static void ApplyFluentPalette(FluentTheme fluent, ThemePalette palette, ThemeVariant variant)
    {
        if (!fluent.Palettes.TryGetValue(variant, out var fluentPalette))
            return;

        fluentPalette.RegionColor = palette.FormBg;
        fluentPalette.Accent = palette.Accent;
        fluentPalette.BaseHigh = palette.MainText;
        fluentPalette.BaseMedium = palette.MutedText;
        fluentPalette.BaseLow = palette.FieldBg;
        fluentPalette.ChromeLow = palette.SurfaceBg;
        fluentPalette.ChromeMedium = palette.MainBorder;
        fluentPalette.ChromeHigh = palette.MainText;
        fluentPalette.ChromeDisabledLow = palette.MutedText;
        fluentPalette.ListLow = palette.FieldBg;
        fluentPalette.ListMedium = palette.SurfaceBg;
    }

    private static FluentTheme? GetFluentTheme(Application app)
    {
        foreach (var style in app.Styles)
        {
            if (style is FluentTheme fluent)
                return fluent;
        }
        return null;
    }

    public static Control CreateGroupBox(string header, Control content, bool nested = false)
    {
        var headerBlock = new TextBlock
        {
            Text = header,
            Classes = { "group-box-header" },
        };

        var panel = new DockPanel();
        DockPanel.SetDock(headerBlock, Dock.Top);
        panel.Children.Add(headerBlock);
        panel.Children.Add(content);

        var border = new Border
        {
            Child = panel,
            Classes = { "group-box" },
        };
        if (nested)
            border.Classes.Add("nested");

        return border;
    }
}
