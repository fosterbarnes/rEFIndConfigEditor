using rEFIndConfigEditor.Config;

namespace rEFIndConfigEditor.UI;

internal sealed class OptionBinding
{
    public required OptionDefinition Definition { get; init; }
    public required CheckBox UseCheck { get; init; }
    public Control ValueControl { get; init; } = null!;
    public Button? LogPickButton { get; init; }
    public Button? FolderAddButton { get; init; }
    public Button? FileAddButton { get; init; }
    public Button? BrowseButton { get; init; }
    public Panel? ScrollHost { get; init; }
    public Control? RowAnchor { get; init; }
    public string? PreservedInvalidNumeric { get; set; }
}
