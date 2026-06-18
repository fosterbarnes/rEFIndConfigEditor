using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using rEFIndConfigEditor.Models;
using rEFIndConfigEditor.Settings;

namespace rEFIndConfigEditor.UI;

internal static class FontFamilyPanelBuilder
{
    public static (StackPanel Panel, IReadOnlyList<FontFamilyBinding> Bindings) Build(
        IReadOnlyList<SettingDefinition>? defs = null)
    {
        defs ??= SettingCatalog.ForCategory(SettingCategory.App)
            .Where(d => d.Kind == SettingControlKind.Choice && d.UiSection == SettingCatalog.AppSectionFonts)
            .ToList();

        var bindings = new List<FontFamilyBinding>();
        var panel = new StackPanel { Spacing = UiMetrics.AppSettingsControlGapPx };

        foreach (var def in defs)
            panel.Children.Add(CreateRow(bindings, def));

        return (panel, bindings);
    }

    private static Control CreateRow(List<FontFamilyBinding> bindings, SettingDefinition def)
    {
        var tooltip = SettingTooltipHelper.Build(def);
        var mono = def.Token == "ui_font_family_mono";
        var options = ChoicesFor(def.Token);

        var label = new TextBlock
        {
            Text = def.Label,
            Classes = { "setting-label" },
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = UiMetrics.AppSettingsFontLabelWidthPx,
        };
        ToolTip.SetTip(label, tooltip);

        var combo = new ComboBox
        {
            Width = UiMetrics.AppSettingsFontComboWidthPx,
            ItemsSource = options,
            ItemTemplate = CreateOptionTemplate(mono),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
        };
        ToolTip.SetTip(combo, tooltip);

        bindings.Add(new FontFamilyBinding
        {
            Token = def.Token,
            Definition = def,
            Combo = combo,
            FocusTarget = combo,
        });

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = UiMetrics.AppSettingsFontLabelGapPx,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { label, combo },
        };
    }

    private static FontFamilyOption[] ChoicesFor(string token) => token switch
    {
        "ui_font_family_main" => UiFontFamilies.MainChoices,
        "ui_font_family_mono" => UiFontFamilies.MonoChoices,
        _ => [],
    };

    private static FuncDataTemplate<FontFamilyOption> CreateOptionTemplate(bool mono) =>
        new((option, _) => new TextBlock
        {
            Text = option.Label,
            FontFamily = UiFontService.CreatePreviewFamily(option.Value, mono),
            VerticalAlignment = VerticalAlignment.Center,
        });
}
