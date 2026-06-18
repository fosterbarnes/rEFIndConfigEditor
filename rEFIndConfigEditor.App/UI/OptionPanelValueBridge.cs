using Avalonia.Controls;
using rEFIndConfigEditor.Settings;

namespace rEFIndConfigEditor.UI;

internal static class OptionPanelValueBridge
{
    public static void ResetToDefaults(IEnumerable<OptionBinding> bindings)
    {
        foreach (var binding in bindings)
            ResetBinding(binding);
    }

    private static void ResetBinding(OptionBinding binding)
    {
        var def = binding.Definition;

        if (def.ShowEnableCheckbox && binding.EnableCheck is not null)
            binding.EnableCheck.IsChecked = false;

        switch (def.Kind)
        {
            case SettingControlKind.Boolean:
                if (binding.EnableCheck is not null)
                    binding.EnableCheck.IsChecked = false;
                break;
            case SettingControlKind.Numeric:
                if (binding.ValueControl is not null)
                    SetNumericValue(binding.ValueControl, def.NumericDefault);
                break;
            case SettingControlKind.Decimal:
                if (binding.ValueControl is not null)
                    SetNumericValue(binding.ValueControl, def.DecimalDefault);
                break;
            case SettingControlKind.Choice:
                if (binding.ValueControl is ComboBox combo)
                    combo.SelectedIndex = 0;
                break;
            case SettingControlKind.Text:
                if (binding.ValueControl is TextBox textBox)
                    textBox.Text = string.Empty;
                break;
            case SettingControlKind.MultiSelect:
                if (binding.ValueControl is not null)
                    ResetMultiSelect(binding.ValueControl);
                break;
        }
    }

    private static void SetNumericValue(Control control, decimal value)
    {
        if (control is NumericUpDown numeric)
            numeric.Value = value;
    }

    private static void ResetMultiSelect(Control control)
    {
        if (control is not Border { Child: StackPanel panel })
            return;

        foreach (var child in panel.Children)
        {
            if (child is CheckBox checkBox)
                checkBox.IsChecked = false;
        }
    }
}
