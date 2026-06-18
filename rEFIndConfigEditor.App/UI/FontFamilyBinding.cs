using Avalonia.Controls;
using rEFIndConfigEditor.Settings;

namespace rEFIndConfigEditor.UI;

internal sealed class FontFamilyBinding
{
    public required string Token { get; init; }
    public required SettingDefinition Definition { get; init; }
    public required ComboBox Combo { get; init; }
    public required Control FocusTarget { get; init; }
}
