using Avalonia.Controls;
using rEFIndConfigEditor.Models;

namespace rEFIndConfigEditor.UI;

internal static class OptionPanelPreferenceBridge
{
    private sealed record FontSpec(
        Func<UiPreferences, int> Get,
        Action<UiPreferences, int> Set,
        int Default);

    private sealed record FontFamilySpec(
        FontFamilyOption[] Choices,
        Func<UiPreferences, string> Get,
        Action<UiPreferences, string> Set,
        string Default,
        Func<string, int> IndexOf);

    private static readonly Dictionary<string, FontSpec> FontSpecs = new(StringComparer.Ordinal)
    {
        ["ui_font_main"] = new(p => p.MainFontSize, (p, v) => p.MainFontSize = v, UiFontDefaults.Main),
        ["ui_font_tab"] = new(p => p.TabFontSize, (p, v) => p.TabFontSize = v, UiFontDefaults.Tab),
        ["ui_font_token"] = new(p => p.TokenFontSize, (p, v) => p.TokenFontSize = v, UiFontDefaults.Token),
    };

    private static readonly Dictionary<string, FontFamilySpec> FontFamilySpecs = new(StringComparer.Ordinal)
    {
        ["ui_font_family_main"] = new(
            UiFontFamilies.MainChoices,
            p => p.MainFontFamily,
            (p, v) => p.MainFontFamily = v,
            UiFontFamilies.DefaultMain,
            UiFontFamilies.IndexOfMain),
        ["ui_font_family_mono"] = new(
            UiFontFamilies.MonoChoices,
            p => p.MonoFontFamily,
            (p, v) => p.MonoFontFamily = v,
            UiFontFamilies.DefaultMono,
            UiFontFamilies.IndexOfMono),
    };

    public static void WireDirectFontSizes(
        IEnumerable<FontSizeBinding> bindings,
        UiPreferences prefs,
        Action changed)
    {
        foreach (var binding in bindings)
        {
            if (!FontSpecs.TryGetValue(binding.Token, out var spec))
                continue;

            binding.Numeric.Minimum = UiFontDefaults.Min;
            binding.Numeric.Maximum = UiFontDefaults.Max;
            binding.Numeric.Value = spec.Get(prefs);

            binding.Numeric.ValueChanged += (_, _) =>
            {
                spec.Set(prefs, UiFontService.Clamp((int)(binding.Numeric.Value ?? spec.Default)));
                changed();
            };
        }
    }

    public static void ResetFontSizesToDefaults(
        IEnumerable<FontSizeBinding> bindings,
        UiPreferences prefs,
        Action changed)
    {
        foreach (var binding in bindings)
        {
            if (!FontSpecs.TryGetValue(binding.Token, out var spec))
                continue;

            spec.Set(prefs, spec.Default);
            binding.Numeric.Value = spec.Default;
        }

        changed();
    }

    public static void WireFontFamilies(
        IEnumerable<FontFamilyBinding> bindings,
        UiPreferences prefs,
        Action changed)
    {
        foreach (var binding in bindings)
        {
            if (!FontFamilySpecs.TryGetValue(binding.Token, out var spec))
                continue;

            binding.Combo.SelectedIndex = spec.IndexOf(spec.Get(prefs));
            binding.Combo.SelectionChanged += (_, _) =>
            {
                var index = binding.Combo.SelectedIndex;
                if (index < 0 || index >= spec.Choices.Length)
                    return;

                spec.Set(prefs, spec.Choices[index].Value);
                changed();
            };
        }
    }

    public static void ResetFontFamiliesToDefaults(
        IEnumerable<FontFamilyBinding> bindings,
        UiPreferences prefs,
        Action changed)
    {
        foreach (var binding in bindings)
        {
            if (!FontFamilySpecs.TryGetValue(binding.Token, out var spec))
                continue;

            spec.Set(prefs, spec.Default);
            binding.Combo.SelectedIndex = spec.IndexOf(spec.Default);
        }

        changed();
    }
}
