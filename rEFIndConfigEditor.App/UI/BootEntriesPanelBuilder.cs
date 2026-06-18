using Avalonia.Controls;
using Avalonia.Layout;
using rEFIndConfigEditor.Config;

namespace rEFIndConfigEditor.UI;

internal sealed class BootEntriesPanelHandles
{
    internal ListBox BootList { get; }
    internal CheckBox SkipOtherEntries { get; }
    internal Button ImportButton { get; }
    internal Button AddButton { get; }
    internal Button EditButton { get; }
    internal Button DuplicateButton { get; }
    internal Button RemoveButton { get; }
    internal Button MoveUpButton { get; }
    internal Button MoveDownButton { get; }

    internal BootEntriesPanelHandles(
        ListBox bootList,
        CheckBox skipOther,
        Button import,
        Button add,
        Button edit,
        Button duplicate,
        Button remove,
        Button moveUp,
        Button moveDown)
    {
        BootList = bootList;
        SkipOtherEntries = skipOther;
        ImportButton = import;
        AddButton = add;
        EditButton = edit;
        DuplicateButton = duplicate;
        RemoveButton = remove;
        MoveUpButton = moveUp;
        MoveDownButton = moveDown;
    }

    public void RefreshList(RefindDocument doc)
    {
        var selected = BootList.SelectedIndex;
        BootList.ItemsSource = doc.MenuEntries.Select(e => e.Summary).ToList();
        if (selected >= 0 && selected < BootList.ItemCount)
            BootList.SelectedIndex = selected;
    }

    public void LoadSkipOther(RefindDocument doc) =>
        SkipOtherEntries.IsChecked = IsManualOnlyScanfor(doc.FindGlobal("scanfor"));

    public void ApplySkipOther(RefindDocument doc)
    {
        if (SkipOtherEntries.IsChecked == true)
        {
            doc.SetGlobal("scanfor", true, ["manual"]);
            return;
        }

        if (IsManualOnlyScanfor(doc.FindGlobal("scanfor")))
            doc.RemoveGlobal("scanfor");
    }

    private static bool IsManualOnlyScanfor(GlobalOption? opt) =>
        opt is { IsActive: true } &&
        opt.Values.Count == 1 &&
        opt.Values[0].Equals("manual", StringComparison.OrdinalIgnoreCase);
}

internal static class BootEntriesPanelBuilder
{
    public static (Control Panel, BootEntriesPanelHandles Handles) Build()
    {
        var bootList = new ListBox
        {
            MinHeight = 140,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Classes = { "setting-input-frame" },
        };

        Button MakeBtn(string text) => new()
        {
            Content = text,
            MinHeight = UiMetrics.BootButtonRowHeight,
            Height = UiMetrics.BootButtonRowHeight,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        var import = MakeBtn("Import from log");
        var add = MakeBtn("Add");
        var edit = MakeBtn("Edit");
        var duplicate = MakeBtn("Duplicate");
        var remove = MakeBtn("Remove");
        var moveUp = MakeBtn("Move up");
        var moveDown = MakeBtn("Move down");

        var skipOther = new CheckBox
        {
            Content = "Skip all other entries",
            MinHeight = UiMetrics.BootButtonRowHeight,
        };

        var leftCol = new StackPanel
        {
            Spacing = UiMetrics.BootButtonRowGapPx,
            Width = UiMetrics.BootButtonWidth,
            Children = { import, add, edit, duplicate, remove },
        };

        var rightCol = new StackPanel
        {
            Spacing = UiMetrics.BootButtonRowGapPx,
            Width = UiMetrics.BootButtonWidth,
            Children = { moveUp, moveDown },
        };

        var buttonGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto"),
            ColumnSpacing = UiMetrics.BootButtonGapPx,
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            Children = { leftCol, rightCol, skipOther },
        };
        Grid.SetColumn(leftCol, 0);
        Grid.SetColumn(rightCol, 1);
        Grid.SetColumn(skipOther, 0);
        Grid.SetColumnSpan(skipOther, 2);
        Grid.SetRow(skipOther, 1);

        var split = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = UiMetrics.BootListButtonGapPx,
            Children = { bootList, buttonGrid },
        };
        Grid.SetColumn(bootList, 0);
        Grid.SetColumn(buttonGrid, 1);

        var handles = new BootEntriesPanelHandles(
            bootList, skipOther, import, add, edit, duplicate, remove, moveUp, moveDown);

        return (split, handles);
    }
}
