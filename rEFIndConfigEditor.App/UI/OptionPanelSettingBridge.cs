using Avalonia.Controls;
using rEFIndConfigEditor.Models;
using rEFIndConfigEditor.Settings;

namespace rEFIndConfigEditor.UI;

internal static class OptionPanelSettingBridge
{
    public static void SyncFromBindings(
        IEnumerable<OptionBinding> bindings,
        CatalogSettingsDocument document,
        int selectedDemoRadioIndex,
        IEnumerable<string> demoListItems)
    {
        foreach (var binding in bindings)
            document.Values[binding.Definition.Token] = ReadEntry(binding);

        document.Values["demo_radio"] = new CatalogSettingEntry { Index = selectedDemoRadioIndex };
        document.Values["demo_entry_list"] = new CatalogSettingEntry
        {
            Items = demoListItems.ToList(),
        };
    }

    public static void ApplyToBindings(IEnumerable<OptionBinding> bindings, CatalogSettingsDocument document)
    {
        foreach (var binding in bindings)
        {
            if (document.TryGet(binding.Definition.Token, out var entry))
                ApplyEntry(binding, entry);
        }
    }

    public static CatalogSettingEntry ReadEntry(OptionBinding binding)
    {
        var def = binding.Definition;
        var enabled = binding.EnableCheck?.IsChecked == true;

        return def.Kind switch
        {
            SettingControlKind.Boolean => new CatalogSettingEntry
            {
                Bool = binding.EnableCheck?.IsChecked == true,
            },
            SettingControlKind.Numeric when binding.ValueControl is NumericUpDown numeric => new CatalogSettingEntry
            {
                Enabled = def.ShowEnableCheckbox ? enabled : null,
                Number = numeric.Value ?? def.NumericDefault,
            },
            SettingControlKind.Decimal when binding.ValueControl is NumericUpDown dec => new CatalogSettingEntry
            {
                Enabled = def.ShowEnableCheckbox ? enabled : null,
                Number = dec.Value ?? def.DecimalDefault,
            },
            SettingControlKind.Choice when binding.ValueControl is ComboBox combo => new CatalogSettingEntry
            {
                Enabled = def.ShowEnableCheckbox ? enabled : null,
                Index = combo.SelectedIndex,
            },
            SettingControlKind.Text when binding.ValueControl is TextBox textBox => new CatalogSettingEntry
            {
                Enabled = def.ShowEnableCheckbox ? enabled : null,
                Text = textBox.Text ?? string.Empty,
            },
            SettingControlKind.MultiSelect => ReadMultiSelectEntry(binding, def, enabled),
            _ => new CatalogSettingEntry(),
        };
    }

    public static void ApplyEntry(OptionBinding binding, CatalogSettingEntry entry)
    {
        var def = binding.Definition;

        if (def.ShowEnableCheckbox && binding.EnableCheck is not null)
            binding.EnableCheck.IsChecked = entry.Enabled == true;

        switch (def.Kind)
        {
            case SettingControlKind.Boolean:
                if (binding.EnableCheck is not null)
                    binding.EnableCheck.IsChecked = entry.Bool == true;
                break;
            case SettingControlKind.Numeric:
                if (binding.ValueControl is NumericUpDown numeric)
                    numeric.Value = entry.Number ?? def.NumericDefault;
                break;
            case SettingControlKind.Decimal:
                if (binding.ValueControl is NumericUpDown dec)
                    dec.Value = entry.Number ?? def.DecimalDefault;
                break;
            case SettingControlKind.Choice:
                if (binding.ValueControl is ComboBox combo)
                {
                    var index = entry.Index ?? 0;
                    if (index >= 0 && index < combo.ItemCount)
                        combo.SelectedIndex = index;
                }
                break;
            case SettingControlKind.Text:
                if (binding.ValueControl is TextBox textBox)
                    textBox.Text = entry.Text ?? string.Empty;
                break;
            case SettingControlKind.MultiSelect:
                ApplyMultiSelect(binding, def, entry);
                break;
        }
    }

    public static string FormatLogValue(SettingDefinition def, CatalogSettingEntry entry)
    {
        var enabled = entry.Enabled == true;

        return def.Kind switch
        {
            SettingControlKind.Boolean => (entry.Bool == true).ToString(),
            SettingControlKind.Numeric =>
                def.ShowEnableCheckbox
                    ? $"enabled={enabled},value={entry.Number ?? def.NumericDefault}"
                    : $"value={entry.Number ?? def.NumericDefault}",
            SettingControlKind.Decimal =>
                def.ShowEnableCheckbox
                    ? $"enabled={enabled},value={entry.Number ?? def.DecimalDefault}"
                    : $"value={entry.Number ?? def.DecimalDefault}",
            SettingControlKind.Choice =>
                $"enabled={enabled},index={entry.Index ?? 0}",
            SettingControlKind.Text =>
                $"enabled={enabled},text={entry.Text ?? string.Empty}",
            SettingControlKind.MultiSelect =>
                $"enabled={enabled},checked={FormatSelectedLabels(def, entry.Selected)}",
            SettingControlKind.EntryList =>
                string.Join("|", entry.Items ?? []),
            SettingControlKind.RadioGroup =>
                (entry.Index ?? 0).ToString(),
            _ => string.Empty,
        };
    }

    public static int ReadRadioIndex(CatalogSettingsDocument document, int choiceCount, int defaultIndex = 0)
    {
        if (!document.TryGet("demo_radio", out var entry) || entry.Index is not int index)
            return defaultIndex;

        return index >= 0 && index < choiceCount ? index : defaultIndex;
    }

    public static IReadOnlyList<string> ReadEntryListItems(
        CatalogSettingsDocument document,
        IReadOnlyList<string> defaults)
    {
        if (!document.TryGet("demo_entry_list", out var entry) || entry.Items is not { Count: > 0 } items)
            return defaults;

        return items;
    }

    public static IEnumerable<string> CollectCatalogLogLines(
        CatalogSettingsDocument document,
        string prefix)
    {
        foreach (var def in SettingCatalog.ForCategory(SettingCategory.General)
                     .OrderBy(d => d.Token, StringComparer.Ordinal))
        {
            if (!document.TryGet(def.Token, out var entry))
                continue;

            yield return $"{prefix}: {def.Token}={FormatLogValue(def, entry)}";
        }
    }

    public static List<string> CollectCatalogChanges(
        CatalogSettingsDocument current,
        CatalogSettingsDocument baseline,
        string prefix)
    {
        var changes = new List<string>();
        var tokens = current.Values.Keys
            .Union(baseline.Values.Keys, StringComparer.Ordinal)
            .OrderBy(t => t, StringComparer.Ordinal);

        foreach (var token in tokens)
        {
            current.Values.TryGetValue(token, out var currentEntry);
            baseline.Values.TryGetValue(token, out var baselineEntry);
            if (currentEntry?.ValueEquals(baselineEntry) != false)
                continue;

            var def = SettingCatalog.All.FirstOrDefault(d =>
                string.Equals(d.Token, token, StringComparison.Ordinal));
            if (def is null)
                continue;

            var before = baselineEntry is null ? "(none)" : FormatLogValue(def, baselineEntry);
            var after = currentEntry is null ? "(none)" : FormatLogValue(def, currentEntry);
            changes.Add($"{prefix}: {token} {before} → {after}");
        }

        return changes;
    }

    private static CatalogSettingEntry ReadMultiSelectEntry(
        OptionBinding binding,
        SettingDefinition def,
        bool enabled)
    {
        var selected = new List<string>();
        if (binding.ValueControl is Border { Child: StackPanel panel })
        {
            var choices = def.Choices ?? [];
            for (var i = 0; i < panel.Children.Count && i < choices.Length; i++)
            {
                if (panel.Children[i] is CheckBox { IsChecked: true })
                    selected.Add(choices[i]);
            }
        }

        return new CatalogSettingEntry
        {
            Enabled = def.ShowEnableCheckbox ? enabled : null,
            Selected = selected,
        };
    }

    private static void ApplyMultiSelect(OptionBinding binding, SettingDefinition def, CatalogSettingEntry entry)
    {
        if (binding.ValueControl is not Border { Child: StackPanel panel })
            return;

        var selected = entry.Selected ?? [];
        var choices = def.Choices ?? [];
        for (var i = 0; i < panel.Children.Count && i < choices.Length; i++)
        {
            if (panel.Children[i] is CheckBox checkBox)
                checkBox.IsChecked = selected.Contains(choices[i], StringComparer.Ordinal);
        }
    }

    private static string FormatSelectedLabels(SettingDefinition def, IReadOnlyList<string>? selected)
    {
        if (selected is not { Count: > 0 })
            return string.Empty;

        var labels = def.ChoiceLabels ?? def.Choices ?? [];
        var values = def.Choices ?? [];
        var parts = new List<string>();
        foreach (var value in selected)
        {
            var index = Array.IndexOf(values, value);
            parts.Add(index >= 0 && index < labels.Length ? labels[index] : value);
        }

        return string.Join(",", parts);
    }
}
