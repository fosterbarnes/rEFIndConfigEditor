using rEFIndConfigEditor.Config;
using rEFIndConfigEditor.Models;

namespace rEFIndConfigEditor.Settings;

public enum SettingCategory
{
    General,
    Display,
    Theme,
    Input,
    Scanning,
    Other,
    App
}

public enum SettingControlKind
{
    Numeric,
    Decimal,
    Boolean,
    Text,
    MultiSelect,
    Choice,
    EntryList,
    RadioGroup
}

public enum PathPickerKind
{
    None,
    File,
    Directory
}

public sealed class SettingDefinition
{
    public required string Token { get; init; }
    public required string Label { get; init; }
    public required SettingCategory Category { get; init; }
    public required SettingControlKind Kind { get; init; }
    public SettingDisplayFlags DisplayFlags { get; init; }
    public string? UiSection { get; init; }
    public string? HelpText { get; init; }
    public string? DocUrl { get; init; }
    public string[]? Choices { get; init; }
    public string[]? ChoiceLabels { get; init; }
    public decimal NumericMin { get; init; }
    public decimal NumericMax { get; init; } = 99999;
    public decimal NumericDefault { get; init; }
    public decimal DecimalMin { get; init; }
    public decimal DecimalMax { get; init; } = 99999;
    public decimal DecimalDefault { get; init; }
    public int DecimalPlaces { get; init; } = 2;
    public int TextWidthBaseline { get; init; } = 220;
    public PathPickerKind PathPicker { get; init; } = PathPickerKind.None;
    public string? PickerButtonText { get; init; }
    public string? PickerButtonTooltip { get; init; }
    public string? FileFilter { get; init; }

    public bool ShowEnableCheckbox =>
        Kind != SettingControlKind.Boolean && DisplayFlags.HasFlag(SettingDisplayFlags.EnableCheckbox);

    public bool ShowToken => DisplayFlags.HasFlag(SettingDisplayFlags.Token);
}

public static class SettingCatalog
{
    public const string AppSectionPreferences = "Preferences";
    public const string AppSectionTitleBar = "Title bar";
    public const string AppSectionFonts = "Fonts";
    public const string AppSectionTextSizes = "Text size";
    public const string AppSectionUpdates = "Updates";
    public const string AppSectionLogging = "Logging";

    private const SettingDisplayFlags RefindFlags =
        SettingDisplayFlags.EnableCheckbox | SettingDisplayFlags.Token;

    public static IReadOnlyList<SettingDefinition> All { get; } = Build();

    private static readonly Dictionary<SettingCategory, IReadOnlyList<SettingDefinition>> ByCategory =
        All.GroupBy(d => d.Category).ToDictionary(g => g.Key, g => (IReadOnlyList<SettingDefinition>)g.ToList());

    public static IReadOnlyList<SettingDefinition> ForCategory(SettingCategory category) =>
        ByCategory.TryGetValue(category, out var list) ? list : [];

    public static bool IsPanelRow(SettingDefinition def) =>
        def.Kind is not SettingControlKind.EntryList and not SettingControlKind.RadioGroup;

    private static SettingDefinition[] Build()
    {
        var all = RefindOptions().Concat(AppSettings()).ToArray();
        var dupes = all.GroupBy(d => d.Token).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (dupes.Count > 0)
            throw new InvalidOperationException($"Duplicate setting tokens: {string.Join(", ", dupes)}");
        return all;
    }

    private static SettingDefinition[] RefindOptions() =>
        OptionCatalog.All.Select(MapRefindOption).ToArray();

    private static SettingDefinition MapRefindOption(OptionDefinition opt) =>
        Define(
            opt.Token,
            opt.Label,
            MapCategory(opt.Category),
            MapKind(opt.Kind),
            help: opt.HelpText,
            choices: opt.Choices,
            choiceLabels: opt.ChoiceLabels,
            numericMin: opt.NumericMin,
            numericMax: opt.NumericMax,
            numericDefault: opt.NumericDefault,
            textWidthBaseline: opt.Category == OptionCategory.Advanced ? 360 : 220,
            displayFlags: opt.Kind == OptionControlKind.Boolean
                ? SettingDisplayFlags.Token
                : RefindFlags);

    private static SettingCategory MapCategory(OptionCategory category) => category switch
    {
        OptionCategory.General => SettingCategory.General,
        OptionCategory.Display => SettingCategory.Display,
        OptionCategory.Theme => SettingCategory.Theme,
        OptionCategory.Input => SettingCategory.Input,
        OptionCategory.Scanning => SettingCategory.Scanning,
        OptionCategory.Advanced => SettingCategory.Other,
        _ => SettingCategory.General
    };

    private static SettingControlKind MapKind(OptionControlKind kind) => kind switch
    {
        OptionControlKind.Numeric => SettingControlKind.Numeric,
        OptionControlKind.Boolean => SettingControlKind.Boolean,
        OptionControlKind.Text => SettingControlKind.Text,
        OptionControlKind.MultiSelect => SettingControlKind.MultiSelect,
        OptionControlKind.Choice => SettingControlKind.Choice,
        _ => SettingControlKind.Text
    };

    private static SettingDefinition[] AppSettings() =>
    [
        Define("ui_theme", "App theme", SettingCategory.App, SettingControlKind.Choice,
            help: "Application color theme.",
            uiSection: AppSectionPreferences,
            choices: ["System", "Light", "Dark", "Dracula"]),
        Define("ui_remember_last_tab", "Remember last selected tab", SettingCategory.App, SettingControlKind.Boolean,
            help: "Restore the selected tab on startup.",
            uiSection: AppSectionPreferences),
        Define("ui_auto_load_last_conf", "Auto-load last config on launch", SettingCategory.App,
            SettingControlKind.Boolean,
            help: "Open the last edited refind.conf automatically at startup.",
            uiSection: AppSectionPreferences),
        Define("ui_mac_title_bar", "Title bar layout", SettingCategory.App, SettingControlKind.Choice,
            help: "Combined merges tabs into the window title bar. Separate uses a standard title bar with tabs below, similar to Windows.",
            uiSection: AppSectionTitleBar,
            choices: ["separate", "combined"],
            choiceLabels: ["Separate title & tab bar", "Combined title & tab bar"]),
        Define("ui_font_family_main", "General font", SettingCategory.App, SettingControlKind.Choice,
            help: "Font for labels, buttons, tabs, and most UI text.",
            uiSection: AppSectionFonts,
            choices: UiFontFamilies.MainValues,
            choiceLabels: UiFontFamilies.MainLabels),
        Define("ui_font_family_mono", "Monospace font", SettingCategory.App, SettingControlKind.Choice,
            help: "Font for tokens, raw editor, and status bar.",
            uiSection: AppSectionFonts,
            choices: UiFontFamilies.MonoValues,
            choiceLabels: UiFontFamilies.MonoLabels),
        Define("ui_font_main", "Main text", SettingCategory.App, SettingControlKind.Numeric,
            help: "Body text, menus, status bar, and section headers.",
            uiSection: AppSectionTextSizes,
            numericMin: UiFontDefaults.Min, numericMax: UiFontDefaults.Max, numericDefault: UiFontDefaults.Main),
        Define("ui_font_tab", "Tabs", SettingCategory.App, SettingControlKind.Numeric,
            help: "Tab strip header labels.",
            uiSection: AppSectionTextSizes,
            numericMin: UiFontDefaults.Min, numericMax: UiFontDefaults.Max, numericDefault: UiFontDefaults.Tab),
        Define("ui_font_token", "Tokens", SettingCategory.App, SettingControlKind.Numeric,
            help: "Monospace setting token labels under each option name.",
            uiSection: AppSectionTextSizes,
            numericMin: UiFontDefaults.Min, numericMax: UiFontDefaults.Max, numericDefault: UiFontDefaults.Token),
        Define("ui_check_for_updates", "Automatically check for updates", SettingCategory.App, SettingControlKind.Boolean,
            help: "Check for new releases on startup.",
            uiSection: AppSectionUpdates),
        Define("ui_auto_install_updates", "Download and install updates automatically", SettingCategory.App,
            SettingControlKind.Boolean,
            help: "Silently download and install updates when available.",
            uiSection: AppSectionUpdates),
        Define("ui_enable_debug_logging", "Enable debug logging", SettingCategory.App, SettingControlKind.Boolean,
            help: "Append diagnostic output to debug.log in app data. Off by default.",
            uiSection: AppSectionLogging),
    ];

    public static SettingDefinition Define(
        string token,
        string label,
        SettingCategory category,
        SettingControlKind kind,
        string? help = null,
        string? docUrl = null,
        string[]? choices = null,
        string[]? choiceLabels = null,
        decimal numericMin = 0,
        decimal numericMax = 99999,
        decimal numericDefault = 0,
        decimal decimalMin = 0,
        decimal decimalMax = 99999,
        decimal decimalDefault = 0,
        int decimalPlaces = 2,
        int textWidthBaseline = 220,
        PathPickerKind pathPicker = PathPickerKind.None,
        string? pickerButtonText = null,
        string? pickerButtonTooltip = null,
        string? fileFilter = null,
        SettingDisplayFlags displayFlags = SettingDisplayFlags.None,
        string? uiSection = null) =>
        new()
        {
            Token = token,
            Label = label,
            Category = category,
            Kind = kind,
            DisplayFlags = displayFlags,
            UiSection = uiSection,
            HelpText = help,
            DocUrl = docUrl,
            Choices = choices,
            ChoiceLabels = choiceLabels,
            NumericMin = numericMin,
            NumericMax = numericMax,
            NumericDefault = numericDefault,
            DecimalMin = decimalMin,
            DecimalMax = decimalMax,
            DecimalDefault = decimalDefault,
            DecimalPlaces = decimalPlaces,
            TextWidthBaseline = textWidthBaseline,
            PathPicker = pathPicker,
            PickerButtonText = pickerButtonText,
            PickerButtonTooltip = pickerButtonTooltip,
            FileFilter = fileFilter
        };
}
