using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using rEFIndConfigEditor.Config;
using rEFIndConfigEditor.Settings;

namespace rEFIndConfigEditor.UI;

internal static class RefindDocumentBridge
{
    public static void LoadBindings(IEnumerable<OptionBinding> bindings, RefindDocument doc)
    {
        foreach (var b in bindings)
        {
            var opt = doc.FindGlobal(b.Definition.Token);
            if (b.Definition.Kind == SettingControlKind.Boolean)
            {
                if (b.EnableCheck is not null)
                {
                    b.EnableCheck.IsChecked = opt is { IsActive: true } &&
                        (opt.Values.Count == 0 || IsTruthy(opt.Values[0]));
                }
                continue;
            }

            if (b.EnableCheck is not null)
                b.EnableCheck.IsChecked = opt?.IsActive ?? false;
            ApplyOptionToControl(b, opt);
        }
    }

    public static void SaveBindings(IEnumerable<OptionBinding> bindings, RefindDocument doc)
    {
        foreach (var b in bindings)
        {
            if (b.Definition.Kind == SettingControlKind.Boolean)
            {
                if (b.EnableCheck?.IsChecked == true)
                    doc.SetGlobal(b.Definition.Token, true, []);
                else if (doc.FindGlobal(b.Definition.Token) is not null)
                    doc.SetGlobal(b.Definition.Token, true, ["false"]);
                continue;
            }

            if (b.EnableCheck?.IsChecked != true)
            {
                var existing = doc.FindGlobal(b.Definition.Token);
                if (existing is not null)
                {
                    var inactiveValues = ReadValuesFromControl(b);
                    doc.SetGlobal(
                        b.Definition.Token,
                        false,
                        inactiveValues.Count > 0 ? inactiveValues : [.. existing.Values]);
                }
                else
                    doc.RemoveGlobal(b.Definition.Token);
                continue;
            }

            doc.SetGlobal(b.Definition.Token, true, ReadValuesFromControl(b));
        }
    }

    private static void ApplyOptionToControl(OptionBinding b, GlobalOption? opt)
    {
        var values = opt?.Values ?? [];
        switch (b.Definition.Kind)
        {
            case SettingControlKind.Numeric:
            case SettingControlKind.Decimal:
                if (b.ValueControl is not NumericUpDown nud)
                    break;
                b.PreservedInvalidNumeric = null;
                if (values.Count > 0 && decimal.TryParse(values[0], out var n))
                    nud.Value = Math.Clamp(n, nud.Minimum, nud.Maximum);
                else if (values.Count > 0)
                    b.PreservedInvalidNumeric = values[0];
                else
                    nud.Value = b.Definition.Kind == SettingControlKind.Decimal
                        ? b.Definition.DecimalDefault
                        : b.Definition.NumericDefault;
                break;
            case SettingControlKind.Choice:
                if (b.ValueControl is ComboBox combo)
                    SelectChoice(combo, b.Definition, values);
                break;
            case SettingControlKind.MultiSelect:
                ApplyMultiSelect(b, values);
                break;
            default:
                if (b.ValueControl is TextBox tb)
                {
                    tb.Text = RefindWriter.IsFolderListToken(b.Definition.Token)
                        ? RefindWriter.JoinFolderListForEditor(values)
                        : string.Join(", ", values);
                }
                break;
        }
    }

    private static void ApplyMultiSelect(OptionBinding b, IReadOnlyList<string> values)
    {
        var boxes = GetMultiSelectBoxes(b.ValueControl);
        var choices = b.Definition.Choices ?? [];
        for (var i = 0; i < boxes.Count; i++)
        {
            var token = i < choices.Length ? choices[i] : boxes[i].Content?.ToString() ?? "";
            boxes[i].IsChecked = values.Any(v => string.Equals(v, token, StringComparison.OrdinalIgnoreCase));
        }
    }

    private static List<string> ReadValuesFromControl(OptionBinding b)
    {
        switch (b.Definition.Kind)
        {
            case SettingControlKind.Numeric:
            case SettingControlKind.Decimal:
                if (!string.IsNullOrEmpty(b.PreservedInvalidNumeric))
                    return [b.PreservedInvalidNumeric];
                return b.ValueControl is NumericUpDown nud
                    ? [nud.Value?.ToString() ?? "0"]
                    : [];
            case SettingControlKind.Choice:
                return b.ValueControl is ComboBox combo
                    ? ReadChoiceValues(combo, b.Definition)
                    : [];
            case SettingControlKind.MultiSelect:
                return ReadMultiSelectValues(b);
            default:
                if (b.ValueControl is not TextBox tb)
                    return [];
                var text = tb.Text?.Trim() ?? "";
                if (text.Length == 0)
                    return [];
                if (RefindWriter.IsFolderListToken(b.Definition.Token))
                    return RefindWriter.ParseFolderListFromEditor(text);
                if (RefindTokens.AcceptsCommaSeparatedValues(b.Definition.Token))
                    return RefindWriter.ParseCommaListFromEditor(text);
                return [text];
        }
    }

    private static List<string> ReadMultiSelectValues(OptionBinding b)
    {
        var boxes = GetMultiSelectBoxes(b.ValueControl);
        var choices = b.Definition.Choices ?? [];
        var picked = new List<string>();
        for (var i = 0; i < boxes.Count; i++)
        {
            if (boxes[i].IsChecked != true || i >= choices.Length)
                continue;
            picked.Add(choices[i]);
        }
        return picked;
    }

    private static IReadOnlyList<CheckBox> GetMultiSelectBoxes(Control? valueControl)
    {
        if (valueControl is Border { Child: StackPanel panel })
            return panel.Children.OfType<CheckBox>().ToList();
        return [];
    }

    private static void SelectChoice(ComboBox combo, SettingDefinition def, IReadOnlyList<string> values)
    {
        var choices = def.Choices ?? [];
        if (choices.Length == 0)
            return;

        var current = def.Token == "resolution"
            ? string.Join(" ", values)
            : values.FirstOrDefault() ?? "";

        var display = (combo.ItemsSource as System.Collections.IList)?.Cast<object>().Select(o => o?.ToString() ?? "").ToList()
            ?? combo.Items.Select(o => o?.ToString() ?? "").ToList();
        if (display.Count == 0)
            display = (def.ChoiceLabels ?? choices).ToList();

        for (var i = 0; i < choices.Length; i++)
        {
            if (!string.Equals(choices[i], current, StringComparison.OrdinalIgnoreCase))
                continue;
            combo.SelectedIndex = i;
            return;
        }

        if (string.IsNullOrEmpty(current))
            return;

        var items = display.ToList();
        if (!items.Any(v => string.Equals(v, current, StringComparison.OrdinalIgnoreCase)))
            items.Add(current);
        combo.ItemsSource = items;
        combo.SelectedIndex = items.Count - 1;
    }

    private static List<string> ReadChoiceValues(ComboBox combo, SettingDefinition def)
    {
        var choices = def.Choices ?? [];
        var idx = combo.SelectedIndex;
        if (idx < 0)
            return [];

        var sel = idx < choices.Length
            ? choices[idx]
            : combo.SelectedItem?.ToString() ?? "";

        if (def.Token == "resolution")
            return sel.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        return [sel];
    }

    private static bool IsTruthy(string v) =>
        !string.Equals(v, "false", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(v, "off", StringComparison.OrdinalIgnoreCase) &&
        v != "0";
}
