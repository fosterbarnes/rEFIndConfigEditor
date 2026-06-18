using Avalonia.Controls;
using rEFIndConfigEditor.Settings;

namespace rEFIndConfigEditor.UI;

internal sealed class OptionBinding
{
    public required SettingDefinition Definition { get; init; }
    public CheckBox? EnableCheck { get; set; }
    public Control? FocusTarget { get; set; }
    public Control? ValueControl { get; set; }
    public Button? PickerButton { get; set; }
    public Button? LogPickButton { get; set; }
    public Button? FolderAddButton { get; set; }
    public Button? FileAddButton { get; set; }
    public Button? BrowseButton { get; set; }
    public Control? RowAnchor { get; set; }
    public string TabKey { get; init; } = string.Empty;
    public string? PreservedInvalidNumeric { get; set; }
}
