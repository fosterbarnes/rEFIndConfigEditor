using rEFIndConfigEditor.Settings;

namespace rEFIndConfigEditor.UI;

internal static class SettingTooltipHelper
{
    public static string Build(SettingDefinition def)
    {
        if (string.IsNullOrEmpty(def.HelpText))
            return $"Setting token: {def.Token}";
        return $"{def.HelpText}\n\nSetting token: {def.Token}";
    }
}
