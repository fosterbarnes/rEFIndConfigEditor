using Avalonia.Controls;
using rEFIndConfigEditor.Settings;

namespace rEFIndConfigEditor.UI;

internal sealed class FontSizeBinding
{
    public required string Token { get; init; }
    public required SettingDefinition Definition { get; init; }
    public required NumericUpDown Numeric { get; init; }
    public required Control FocusTarget { get; init; }
}
