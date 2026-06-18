using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using rEFIndConfigEditor.Settings;

namespace rEFIndConfigEditor.UI;

internal static class OptionPanelBuilder
{
    public static (Control Content, List<OptionBinding> Bindings) BuildContent(
        SettingCategory category,
        string tabKey,
        Action<string, Control>? registerNav = null)
    {
        var rowDefs = SettingCatalog.ForCategory(category).Where(SettingCatalog.IsPanelRow).ToList();
        var bindings = new List<OptionBinding>();
        var bindingByToken = new Dictionary<string, OptionBinding>(StringComparer.Ordinal);

        foreach (var def in rowDefs)
        {
            var shell = new OptionBinding
            {
                Definition = def,
                TabKey = tabKey,
            };
            bindingByToken[def.Token] = shell;
            bindings.Add(shell);
        }

        var itemsControl = new ItemsControl
        {
            ItemsSource = rowDefs,
            Classes = { "settings-panel" },
        };
        itemsControl.ItemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel());
        itemsControl.ItemTemplate = new FuncDataTemplate<SettingDefinition?>((def, _) =>
        {
            if (def is null)
                return new Grid();

            var (row, binding) = CreateOptionRow(def, tabKey);
            AttachShell(bindingByToken[def.Token], binding, def.Token, registerNav);
            return row;
        });

        return (itemsControl, bindings);
    }

    public static ScrollViewer CreateScrollHost(Control content) =>
        new()
        {
            Content = content,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            BringIntoViewOnFocusChange = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

    private static void AttachShell(
        OptionBinding shell,
        OptionBinding source,
        string token,
        Action<string, Control>? registerNav)
    {
        shell.EnableCheck = source.EnableCheck;
        shell.FocusTarget = source.FocusTarget;
        shell.ValueControl = source.ValueControl;
        shell.PickerButton = source.PickerButton;
        shell.LogPickButton = source.LogPickButton;
        shell.FolderAddButton = source.FolderAddButton;
        shell.FileAddButton = source.FileAddButton;
        shell.BrowseButton = source.BrowseButton;
        shell.RowAnchor = source.RowAnchor;
        if (source.FocusTarget is not null)
            registerNav?.Invoke(token, source.FocusTarget);
    }

    private static bool UsesTopAlignedRow(SettingDefinition def) =>
        def.Kind == SettingControlKind.MultiSelect;

    private static (Control Row, OptionBinding Binding) CreateOptionRow(SettingDefinition def, string tabKey)
    {
        var topAligned = UsesTopAlignedRow(def);
        var (labelPanel, enableCheck, focusTarget) = CreateLabelPanel(def);
        var tooltip = SettingTooltipHelper.Build(def);
        ToolTip.SetTip(labelPanel, tooltip);
        if (enableCheck is not null)
            ToolTip.SetTip(enableCheck, tooltip);

        labelPanel.Margin = UiMetrics.OptionRowMargin;
        if (topAligned)
            labelPanel.MinHeight = UiMetrics.MultiRowHeight(def.Choices?.Length ?? def.ChoiceLabels?.Length ?? 0);
        labelPanel.Classes.Add("option-row");

        Control valueCtrl;
        Button? picker = null;
        Button? logPick = null;
        Button? folderAdd = null;
        Button? fileAdd = null;
        Button? browse = null;
        Control? gridValue = null;

        if (def.Kind == SettingControlKind.Boolean)
        {
            valueCtrl = enableCheck!;
        }
        else
        {
            valueCtrl = CreateValueControl(def);

            if (def.Kind == SettingControlKind.Text && def.PathPicker != PathPickerKind.None && valueCtrl is TextBox textBox)
            {
                picker = MdiButtons.IconOnly(
                    MdiIcons.ForPathPicker(def.PathPicker),
                    def.PickerButtonTooltip ?? def.PickerButtonText ?? tooltip);
                textBox.Width = double.NaN;
                textBox.MinHeight = UiMetrics.ControlHeight;
                textBox.Height = UiMetrics.ControlHeight;
                textBox.HorizontalAlignment = HorizontalAlignment.Stretch;
                textBox.VerticalAlignment = VerticalAlignment.Center;
                ToolTip.SetTip(textBox, tooltip);
                var pickerRow = new Grid
                {
                    Width = UiMetrics.TextWidthLong,
                    ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                    Margin = UiMetrics.OptionRowMargin,
                    Classes = { "option-value" },
                };
                picker.Margin = new Thickness(6, 0, 0, 0);
                Grid.SetColumn(textBox, 0);
                Grid.SetColumn(picker, 1);
                pickerRow.Children.Add(textBox);
                pickerRow.Children.Add(picker);
                gridValue = pickerRow;
            }
            else if (valueCtrl is TextBox tb)
            {
                Button? extra = null;
                if (IsLogPickToken(def.Token))
                {
                    var logTip = LogPickTooltip(def.Token);
                    var logButtonText = def.Token == "windows_recovery_files" ? "Choose paths from log" : "Choose from log";
                    (gridValue, extra) = CreateTextButtonRow(tb, def, logButtonText, tooltip, logTip);
                }
                else if (RefindScanPathHelper.IsFolderListToken(def.Token))
                {
                    (gridValue, extra) = CreateTextButtonRow(tb, def, "Add folder", tooltip,
                        "Pick a folder and append its path (relative to the volume root, forward slashes). " +
                        "Use + as the first entry on also_scan_dirs or dont_scan_dirs to keep defaults.");
                    folderAdd = extra;
                }
                else if (RefindScanFileHelper.IsFileListToken(def.Token))
                {
                    var fileTip = def.Token == "dont_scan_files"
                        ? "Pick .efi files to hide from the OS boot list. Paths are relative to the volume root (or filename only). Use + as the first entry to keep defaults."
                        : "Pick .efi files to hide from the tools row. Paths are relative to the volume root (or filename only).";
                    (gridValue, extra) = CreateTextButtonRow(tb, def, "Add files", tooltip, fileTip);
                    fileAdd = extra;
                }
                else if (RefindThemePathHelper.IsThemeFileToken(def.Token))
                {
                    var browseTip = def.Token == "font"
                        ? "Pick a PNG font file in the rEFInd directory."
                        : "Pick an image file in the rEFInd directory (PNG, ICNS, BMP, or JPEG).";
                    (gridValue, extra) = CreateTextButtonRow(tb, def, "Browse...", tooltip, browseTip);
                    browse = extra;
                }
                else if (RefindThemePathHelper.IsIconsDirToken(def.Token))
                {
                    (gridValue, extra) = CreateTextButtonRow(tb, def, "Browse...", tooltip,
                        "Pick a custom icons folder under the rEFInd directory.");
                    browse = extra;
                }
                else
                {
                    ToolTip.SetTip(valueCtrl, tooltip);
                    valueCtrl.Margin = UiMetrics.OptionRowMargin;
                    valueCtrl.HorizontalAlignment = HorizontalAlignment.Left;
                    valueCtrl.Classes.Add("option-value");
                    gridValue = valueCtrl;
                }

                if (extra is not null && IsLogPickToken(def.Token))
                    logPick = extra;
            }
            else
            {
                ToolTip.SetTip(valueCtrl, tooltip);
                valueCtrl.Margin = UiMetrics.OptionRowMargin;
                valueCtrl.HorizontalAlignment = HorizontalAlignment.Left;
                valueCtrl.Classes.Add("option-value");
                gridValue = valueCtrl;
            }
        }

        var rowGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions($"{UiMetrics.TokenColWidth},Auto"),
        };

        var labelCell = WrapRowCell(labelPanel, topAligned);
        Grid.SetColumn(labelCell, 0);
        rowGrid.Children.Add(labelCell);

        if (gridValue is not null)
        {
            var valueCell = WrapRowCell(gridValue, topAligned);
            Grid.SetColumn(valueCell, 1);
            rowGrid.Children.Add(valueCell);
        }

        var binding = new OptionBinding
        {
            Definition = def,
            EnableCheck = enableCheck,
            FocusTarget = focusTarget,
            ValueControl = valueCtrl,
            PickerButton = picker,
            LogPickButton = logPick,
            FolderAddButton = folderAdd,
            FileAddButton = fileAdd,
            BrowseButton = browse,
            RowAnchor = labelPanel,
            TabKey = tabKey,
        };

        return (rowGrid, binding);
    }

    private static bool IsLogPickToken(string token) =>
        token is "default_selection" or "dont_scan_volumes" or "dont_scan_firmware" or "windows_recovery_files";

    private static string LogPickTooltip(string token) => token switch
    {
        "default_selection" => "Pick a boot loader title from refind.log (same scan as General → Import from log…).",
        "dont_scan_volumes" => "Pick volume labels from refind.log and config menu entries; appends to this list (multi-select).",
        "dont_scan_firmware" => "Pick firmware boot option names from refind.log (scanfor must include firmware; log level ≥ 1). Multi-select.",
        "windows_recovery_files" => "Pick recovery .efi file paths (not volume names). From refind.log when windows_recovery is in showtools, or matching loader paths from the scan.",
        _ => ""
    };

    private static (Grid Host, Button Button) CreateTextButtonRow(
        TextBox text,
        SettingDefinition def,
        string buttonText,
        string fieldTooltip,
        string buttonTooltip)
    {
        text.Width = double.NaN;
        text.MinHeight = UiMetrics.ControlHeight;
        text.Height = UiMetrics.ControlHeight;
        text.HorizontalAlignment = HorizontalAlignment.Stretch;
        text.VerticalAlignment = VerticalAlignment.Center;
        if (def.TextWidthBaseline >= UiMetrics.TextWidthLong)
            text.Width = UiMetrics.TextWidthLong;

        var button = new Button
        {
            Content = buttonText,
            MinHeight = UiMetrics.ControlHeight,
            Height = UiMetrics.ControlHeight,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(6, 0, 0, 0),
        };

        var host = new Grid
        {
            Width = def.TextWidthBaseline >= UiMetrics.TextWidthLong ? UiMetrics.TextWidthLong : UiMetrics.TextWidthMedium,
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = UiMetrics.OptionRowMargin,
            Classes = { "option-value" },
        };
        Grid.SetColumn(text, 0);
        Grid.SetColumn(button, 1);
        host.Children.Add(text);
        host.Children.Add(button);
        ToolTip.SetTip(text, fieldTooltip);
        ToolTip.SetTip(button, buttonTooltip);
        return (host, button);
    }

    private static Grid WrapRowCell(Control content, bool topAlign)
    {
        content.VerticalAlignment = topAlign ? VerticalAlignment.Top : VerticalAlignment.Center;
        content.HorizontalAlignment = HorizontalAlignment.Left;

        return new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Children = { content },
        };
    }

    private static (Panel Panel, CheckBox? EnableCheck, Control FocusTarget) CreateLabelPanel(SettingDefinition def)
    {
        var children = new List<Control>();
        CheckBox? enableCheck = null;
        Control focusTarget;
        Control? token = def.ShowToken ? SettingTokenLink.Create(def.Token) : null;

        if (def.Kind == SettingControlKind.Boolean || def.ShowEnableCheckbox)
        {
            var labelStack = new StackPanel { Spacing = 2 };
            labelStack.Children.Add(new TextBlock { Text = def.Label, Classes = { "setting-label" } });
            if (token is not null)
                labelStack.Children.Add(token);

            enableCheck = new CheckBox
            {
                Content = labelStack,
                MaxWidth = UiMetrics.TokenColWidth - UiMetrics.CellPadH,
                VerticalAlignment = VerticalAlignment.Center,
            };
            children.Add(enableCheck);
            focusTarget = enableCheck;
        }
        else
        {
            var label = new TextBlock
            {
                Text = def.Label,
                Classes = { "setting-label" },
                MaxWidth = UiMetrics.TokenColWidth - UiMetrics.CellPadH,
                VerticalAlignment = VerticalAlignment.Center,
            };
            children.Add(label);
            focusTarget = label;
            if (token is not null)
                children.Add(token);
        }

        var stack = new StackPanel { Spacing = 2 };
        foreach (var child in children)
            stack.Children.Add(child);
        return (stack, enableCheck, focusTarget);
    }

    private static Control CreateValueControl(SettingDefinition def) => def.Kind switch
    {
        SettingControlKind.Numeric => CreateNumeric(def),
        SettingControlKind.Decimal => CreateDecimal(def),
        SettingControlKind.MultiSelect => CreateMultiSelect(def),
        SettingControlKind.Choice => CreateCombo(def),
        _ => CreateText(def),
    };

    private static NumericUpDown CreateNumeric(SettingDefinition def) =>
        new()
        {
            Minimum = def.NumericMin,
            Maximum = def.NumericMax,
            Value = def.NumericDefault,
            Width = UiMetrics.WidthNumeric,
            FormatString = "0",
        };

    private static NumericUpDown CreateDecimal(SettingDefinition def)
    {
        var places = Math.Max(0, def.DecimalPlaces);
        return new NumericUpDown
        {
            Minimum = def.DecimalMin,
            Maximum = def.DecimalMax,
            Value = def.DecimalDefault,
            Width = UiMetrics.WidthNumeric,
            FormatString = places == 0 ? "0" : $"F{places}",
        };
    }

    private static TextBox CreateText(SettingDefinition def) =>
        new()
        {
            Width = def.TextWidthBaseline,
            HorizontalAlignment = HorizontalAlignment.Left,
            MinHeight = UiMetrics.ControlHeight,
            Height = UiMetrics.ControlHeight,
            VerticalAlignment = VerticalAlignment.Center,
        };

    private static ComboBox CreateCombo(SettingDefinition def)
    {
        var display = def.ChoiceLabels ?? def.Choices ?? [];
        var c = new ComboBox
        {
            Width = UiMetrics.WidthCombo,
            ItemsSource = display,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        if (display.Length > 0)
            c.SelectedIndex = 0;
        return c;
    }

    private static Control CreateMultiSelect(SettingDefinition def)
    {
        var display = def.ChoiceLabels ?? def.Choices ?? [];
        var panel = new StackPanel { Spacing = UiMetrics.MultiRowItemSpacingPx };
        foreach (var label in display)
            panel.Children.Add(new CheckBox { Content = label });

        return new Border
        {
            Classes = { "setting-input-frame" },
            Width = UiMetrics.WidthMulti,
            Height = UiMetrics.MultiRowHeight(display.Length),
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = panel,
        };
    }
}
