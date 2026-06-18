using Avalonia.Controls;
using Avalonia.Layout;
using rEFIndConfigEditor.Settings;

namespace rEFIndConfigEditor.UI;

internal static class FontSizePanelBuilder
{
    public static (StackPanel Row, IReadOnlyList<FontSizeBinding> Bindings) Build(
        IReadOnlyList<SettingDefinition>? defs = null)
    {
        defs ??= SettingCatalog.ForCategory(SettingCategory.App)
            .Where(d => d.Kind == SettingControlKind.Numeric)
            .ToList();

        var bindings = new List<FontSizeBinding>();
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = UiMetrics.AppSettingsFontRowSpacingPx,
        };

        foreach (var def in defs)
            row.Children.Add(CreatePair(bindings, def));

        return (row, bindings);
    }

    private static Control CreatePair(List<FontSizeBinding> bindings, SettingDefinition def)
    {
        var tooltip = SettingTooltipHelper.Build(def);
        var labelPanel = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };

        var label = new TextBlock
        {
            Text = def.Label,
            Classes = { "setting-label" },
            VerticalAlignment = VerticalAlignment.Center,
        };
        ToolTip.SetTip(label, tooltip);
        labelPanel.Children.Add(label);

        if (def.ShowToken)
            labelPanel.Children.Add(SettingTokenLink.Create(def.Token));

        var numeric = new NumericUpDown
        {
            Minimum = def.NumericMin,
            Maximum = def.NumericMax,
            Value = def.NumericDefault,
            Width = UiMetrics.WidthNumeric,
            FormatString = "0",
            VerticalAlignment = VerticalAlignment.Center,
        };
        ToolTip.SetTip(numeric, tooltip);

        bindings.Add(new FontSizeBinding
        {
            Token = def.Token,
            Definition = def,
            Numeric = numeric,
            FocusTarget = numeric,
        });

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = UiMetrics.AppSettingsFontLabelGapPx,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { labelPanel, numeric },
        };
    }
}
