using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace rEFIndConfigEditor.UI;

internal static class StanzaEditorUi
{
    public static Grid CreateFieldRow(string token, Control field, bool tall = false)
    {
        var row = CreateRow(token, field, tall);
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("120,*"),
            Margin = new Thickness(0, 4, 0, 4),
        };
        Grid.SetColumn(row.LabelPanel, 0);
        Grid.SetColumn(row.Field, 1);
        grid.Children.Add(row.LabelPanel);
        grid.Children.Add(row.Field);
        return grid;
    }

    internal sealed record Row(Control LabelPanel, Control Field);

    public static Row CreateRow(string token, Control field, bool tall = false)
    {
        var info = StanzaFieldHelp.Get(token);
        var tooltip = StanzaFieldHelp.Tooltip(token);

        var label = new TextBlock
        {
            Text = info.Label,
            Classes = { "setting-label" },
        };

        var tokenBlock = SettingTokenLink.Create(token);
        var labelPanel = new StackPanel { Spacing = 2, Children = { label, tokenBlock } };
        ToolTip.SetTip(labelPanel, tooltip);
        ToolTip.SetTip(field, tooltip);

        if (tall)
        {
            field.MinHeight = 60;
            field.Height = 60;
        }
        else
        {
            field.MinHeight = UiMetrics.ControlHeight;
            field.Height = UiMetrics.ControlHeight;
        }

        field.HorizontalAlignment = HorizontalAlignment.Stretch;
        field.VerticalAlignment = VerticalAlignment.Center;

        return new Row(labelPanel, field);
    }

    public static Grid CreateFormGrid(IEnumerable<Row> rows)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("220,*"),
            RowDefinitions = new RowDefinitions(string.Join(",", Enumerable.Repeat("Auto", rows.Count()))),
        };

        var rowIndex = 0;
        foreach (var row in rows)
        {
            row.LabelPanel.Margin = new Thickness(0, 0, 12, 8);
            row.Field.Margin = new Thickness(0, 0, 0, 8);
            Grid.SetRow(row.LabelPanel, rowIndex);
            Grid.SetColumn(row.LabelPanel, 0);
            Grid.SetRow(row.Field, rowIndex);
            Grid.SetColumn(row.Field, 1);
            grid.Children.Add(row.LabelPanel);
            grid.Children.Add(row.Field);
            rowIndex++;
        }

        return grid;
    }
}
