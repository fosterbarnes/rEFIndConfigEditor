using rEFIndConfigEditor.Config;

namespace rEFIndConfigEditor.UI;

internal static class OptionPanelBuilder
{
    private const int CellPadH = 8;
    private const int CellPadV = 1;
    private const int WidthNumeric = 88;
    private const int WidthCombo = 128;
    private const int WidthMulti = 280;
    private const int WidthShort = 112;
    private const int WidthMedium = 220;
    private const int WidthLong = 360;

    public static (Panel Panel, List<OptionBinding> Bindings) Build(OptionCategory category, int dpi)
    {
        var bindings = new List<OptionBinding>();
        var host = CreateHost(dpi);
        var grid = CreateGrid(dpi);
        var tt = AttachToolTip(host);
        host.SuspendLayout();
        grid.SuspendLayout();
        host.Controls.Add(grid);

        var row = 0;
        foreach (var def in OptionCatalog.ForCategory(category))
            AddOptionRow(grid, bindings, def, host, tt, ref row, dpi);

        grid.ResumeLayout(false);
        host.ResumeLayout(false);
        return (host, bindings);
    }

    public static (Panel Panel, List<OptionBinding> Bindings, CommentCleanupControls Cleanup) BuildAdvanced(int dpi)
    {
        var bindings = new List<OptionBinding>();
        var host = CreateHost(dpi);
        var grid = CreateGrid(dpi);
        var tt = AttachToolTip(host);
        host.SuspendLayout();
        grid.SuspendLayout();
        host.Controls.Add(grid);

        var row = 0;
        var cleanup = AddCommentCleanupRows(grid, tt, ref row, dpi);
        foreach (var def in OptionCatalog.ForCategory(OptionCategory.Advanced))
            AddOptionRow(grid, bindings, def, host, tt, ref row, dpi);

        grid.ResumeLayout(false);
        host.ResumeLayout(false);
        return (host, bindings, cleanup);
    }

    private static ToolTip AttachToolTip(Panel host)
    {
        var tt = new ToolTip();
        host.Disposed += (_, _) => tt.Dispose();
        return tt;
    }

    public static void ApplyMetrics(Panel host, IEnumerable<OptionBinding> bindings, int dpi)
    {
        if (host.Controls.Count == 0 || host.Controls[0] is not TableLayoutPanel grid)
            return;

        host.Padding = UiMetrics.ScalePadding(CellPadH, CellPadV, dpi);
        grid.ColumnStyles[0].Width = UiMetrics.TokenColWidth(dpi);

        var tokenWidth = UiMetrics.TokenColWidth(dpi) - UiMetrics.Scale(8, dpi);
        var ctrlH = UiMetrics.ControlHeight(dpi);
        for (var r = 0; r < grid.RowStyles.Count; r++)
        {
            var rowCtrl = grid.GetControlFromPosition(1, r);
            if (rowCtrl is null)
                continue;
            if (rowCtrl is CheckedListBox)
                continue;
            if (rowCtrl is Panel { Visible: false })
                continue;
            rowCtrl.Margin = rowCtrl is TextBox
                ? UiMetrics.OptionRowTextFieldMargin(dpi)
                : UiMetrics.OptionRowValueMargin(dpi);
        }

        foreach (var b in bindings)
        {
            if (b.RowAnchor is Panel labelHost)
                ApplyLabelPanelMetrics(labelHost, tokenWidth, dpi);

            if (b.Definition.Kind == OptionControlKind.Boolean)
                continue;

            ApplyValueSizing(b.ValueControl, b.Definition, dpi);
            if (b.LogPickButton is not null)
                b.LogPickButton.Height = ctrlH;
            if (b.FolderAddButton is not null)
                b.FolderAddButton.Height = ctrlH;
            if (b.FileAddButton is not null)
                b.FileAddButton.Height = ctrlH;
            if (b.BrowseButton is not null)
                b.BrowseButton.Height = ctrlH;
            if (b.ValueControl is CheckedListBox clb)
            {
                var multiH = MultiRowHeight(b.Definition, dpi);
                clb.Height = multiH;
                for (var r = 0; r < grid.RowStyles.Count; r++)
                {
                    if (ReferenceEquals(grid.GetControlFromPosition(1, r), clb))
                        grid.RowStyles[r] = new RowStyle(SizeType.Absolute, multiH);
                }
            }
            else if (b.ValueControl.Height > 0 && b.ValueControl is not CheckBox)
                b.ValueControl.Height = ctrlH;
        }

        host.PerformLayout();
        grid.PerformLayout();
    }

    private static CommentCleanupControls AddCommentCleanupRows(TableLayoutPanel grid, ToolTip tt, ref int row, int dpi)
    {
        const string sublabel = "open/create a config first";
        const string tooltip =
            "Removes # comment lines and commented-out global options from the config.\n" +
            "Does not change rEFInd boot behavior — editor convenience only.";

        var strip = new CheckBox { Text = "On apply", AutoSize = true, Padding = new Padding(0, 3, 0, 0) };
        AddEditorBoolRow(grid, tt, ref row, "Strip comments when applying", sublabel, strip, tooltip, dpi);

        var clear = new Button { Text = "Clear now", AutoSize = true };
        clear.Height = UiMetrics.ButtonHeight(dpi);
        AddEditorActionRow(grid, tt, ref row, "Clear comments now", sublabel, clear, tooltip, dpi);

        return new CommentCleanupControls { StripOnApply = strip, ClearNow = clear };
    }

    private static void AddEditorBoolRow(
        TableLayoutPanel grid,
        ToolTip tt,
        ref int row,
        string label,
        string sublabel,
        CheckBox value,
        string tooltip,
        int dpi)
    {
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var labelPanel = CreateEditorLabelOnlyPanel(label, sublabel, dpi);

        tt.SetToolTip(labelPanel, tooltip);
        tt.SetToolTip(value, tooltip);

        grid.Controls.Add(labelPanel, 0, row);
        grid.Controls.Add(value, 1, row);
        row++;
    }

    private static void AddEditorActionRow(
        TableLayoutPanel grid,
        ToolTip tt,
        ref int row,
        string label,
        string sublabel,
        Control action,
        string tooltip,
        int dpi)
    {
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var labelPanel = CreateEditorLabelOnlyPanel(label, sublabel, dpi);
        action.Anchor = AnchorStyles.Left;

        tt.SetToolTip(labelPanel, tooltip);
        tt.SetToolTip(action, tooltip);

        grid.Controls.Add(labelPanel, 0, row);
        grid.Controls.Add(action, 1, row);
        row++;
    }

    private static Panel CreateEditorLabelOnlyPanel(string label, string sublabel, int dpi)
    {
        var def = new OptionDefinition
        {
            Token = sublabel,
            Label = label,
            Category = OptionCategory.Advanced,
            Kind = OptionControlKind.Boolean
        };
        var (panel, use) = CreateLabelPanel(def, dpi, monospaceToken: false);
        use.Enabled = false;
        use.AutoCheck = false;
        return panel;
    }

    private static Panel CreateHost(int dpi) =>
        new()
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = UiMetrics.ScalePadding(CellPadH, CellPadV, dpi)
        };

    private static TableLayoutPanel CreateGrid(int dpi)
    {
        var grid = new TableLayoutPanel
        {
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            GrowStyle = TableLayoutPanelGrowStyle.AddRows,
            Dock = DockStyle.Top
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, UiMetrics.TokenColWidth(dpi)));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        return grid;
    }

    private static void AddOptionRow(
        TableLayoutPanel grid,
        List<OptionBinding> bindings,
        OptionDefinition def,
        Panel scrollHost,
        ToolTip tt,
        ref int row,
        int dpi)
    {
        var multi = def.Kind == OptionControlKind.MultiSelect;
        grid.RowStyles.Add(multi
            ? new RowStyle(SizeType.Absolute, MultiRowHeight(def, dpi))
            : new RowStyle(SizeType.AutoSize));

        var (labelPanel, use) = CreateLabelPanel(def, dpi);
        var tooltip = BuildTooltip(def);
        tt.SetToolTip(labelPanel, tooltip);
        tt.SetToolTip(use, tooltip);

        if (def.Kind == OptionControlKind.Boolean)
        {
            grid.Controls.Add(labelPanel, 0, row);
            bindings.Add(new OptionBinding
            {
                Definition = def,
                UseCheck = use,
                ValueControl = CreatePlaceholder(),
                ScrollHost = scrollHost,
                RowAnchor = labelPanel
            });
            row++;
            return;
        }

        var valueCtrl = CreateValueControl(def, dpi);
        Button? logPick = null;
        Button? folderAdd = null;
        Button? fileAdd = null;
        Button? browse = null;
        Control gridValue;
        if (def.Token is "default_selection" or "dont_scan_volumes" or "dont_scan_firmware" or "windows_recovery_files"
            && valueCtrl is TextBox logText)
        {
            var logTip = def.Token switch
            {
                "default_selection" => "Pick a boot loader title from refind.log (same scan as General → Import from log…).",
                "dont_scan_volumes" => "Pick volume labels from refind.log and config menu entries; appends to this list (multi-select).",
                "dont_scan_firmware" => "Pick firmware boot option names from refind.log (scanfor must include firmware; log level ≥ 1). Multi-select.",
                "windows_recovery_files" => "Pick recovery .efi file paths (not volume names). From refind.log when windows_recovery is in showtools, or matching loader paths from the scan.",
                _ => ""
            };
            var logButtonText = def.Token == "windows_recovery_files" ? "Choose paths from log" : "Choose from log";
            (gridValue, logPick) = CreateTextButtonRow(tt, logText, def, dpi, logButtonText, tooltip, logTip);
        }
        else if (RefindScanPathHelper.IsFolderListToken(def.Token) && valueCtrl is TextBox folderText)
        {
            (gridValue, folderAdd) = CreateTextButtonRow(tt, folderText, def, dpi, "Add folder", tooltip,
                "Pick a folder and append its path (relative to the volume root, forward slashes). " +
                "Use + as the first entry on also_scan_dirs or dont_scan_dirs to keep defaults.");
        }
        else if (RefindScanFileHelper.IsFileListToken(def.Token) && valueCtrl is TextBox fileText)
        {
            var fileTip = def.Token == "dont_scan_files"
                ? "Pick .efi files to hide from the OS boot list. Paths are relative to the volume root (or filename only). Use + as the first entry to keep defaults."
                : "Pick .efi files to hide from the tools row. Paths are relative to the volume root (or filename only).";
            (gridValue, fileAdd) = CreateTextButtonRow(tt, fileText, def, dpi, "Add files", tooltip, fileTip);
        }
        else if (RefindThemePathHelper.IsThemeFileToken(def.Token) && valueCtrl is TextBox themeFileText)
        {
            var browseTip = def.Token == "font"
                ? "Pick a PNG font file in the rEFInd directory."
                : "Pick an image file in the rEFInd directory (PNG, ICNS, BMP, or JPEG).";
            (gridValue, browse) = CreateTextButtonRow(tt, themeFileText, def, dpi, "Browse...", tooltip, browseTip);
        }
        else if (RefindThemePathHelper.IsIconsDirToken(def.Token) && valueCtrl is TextBox iconsDirText)
        {
            (gridValue, browse) = CreateTextButtonRow(tt, iconsDirText, def, dpi, "Browse...", tooltip,
                "Pick a custom icons folder under the rEFInd directory.");
        }
        else
        {
            ApplyValueSizing(valueCtrl, def, dpi);
            tt.SetToolTip(valueCtrl, tooltip);
            gridValue = valueCtrl;
        }

        grid.Controls.Add(labelPanel, 0, row);
        grid.Controls.Add(gridValue, 1, row);
        bindings.Add(new OptionBinding
        {
            Definition = def,
            UseCheck = use,
            ValueControl = valueCtrl,
            LogPickButton = logPick,
            FolderAddButton = folderAdd,
            FileAddButton = fileAdd,
            BrowseButton = browse,
            ScrollHost = scrollHost,
            RowAnchor = labelPanel
        });
        row++;
    }

    private static (FlowLayoutPanel Host, Button Button) CreateTextButtonRow(
        ToolTip tt,
        TextBox text,
        OptionDefinition def,
        int dpi,
        string buttonText,
        string fieldTooltip,
        string buttonTooltip)
    {
        var button = new Button { Text = buttonText, AutoSize = true };
        var host = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = UiMetrics.OptionRowValueMargin(dpi)
        };
        ApplyValueSizing(text, def, dpi);
        button.Height = UiMetrics.ControlHeight(dpi);
        host.Controls.Add(text);
        host.Controls.Add(button);
        tt.SetToolTip(text, fieldTooltip);
        tt.SetToolTip(button, buttonTooltip);
        return (host, button);
    }

    private static Panel CreatePlaceholder() =>
        new() { Visible = false, Size = Size.Empty };

    private static Control CreateValueControl(OptionDefinition def, int dpi) => def.Kind switch
    {
        OptionControlKind.Numeric => CreateNumeric(def, dpi),
        OptionControlKind.MultiSelect => CreateCheckedList(def, dpi),
        OptionControlKind.Choice => CreateCombo(def, dpi),
        _ => CreateText(dpi)
    };

    private static string BuildTooltip(OptionDefinition def)
    {
        if (string.IsNullOrEmpty(def.HelpText))
            return $"Config token: {def.Token}";
        return $"{def.HelpText}\n\nConfig token: {def.Token}";
    }

    private static (Panel Panel, CheckBox Check) CreateLabelPanel(OptionDefinition def, int dpi, bool monospaceToken = true)
    {
        var tokenWidth = UiMetrics.TokenColWidth(dpi) - UiMetrics.Scale(8, dpi);
        var host = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = UiMetrics.OptionRowMargin(dpi),
            AutoSize = true
        };

        var layout = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 2,
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var use = new CheckBox
        {
            Text = def.Label,
            AutoSize = true,
            MaximumSize = new Size(tokenWidth, 0),
            Margin = new Padding(0)
        };
        var token = monospaceToken
            ? TokenDocLinks.CreateLabel(def.Token, dpi)
            : new Label
            {
                Text = def.Token,
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Font = UiMetrics.SubLabelFont(),
                Margin = new Padding(UiMetrics.Scale(18, dpi), 0, 0, 0),
                Tag = "sublabel"
            };

        layout.Controls.Add(use, 0, 0);
        layout.Controls.Add(token, 0, 1);
        host.Controls.Add(layout);
        return (host, use);
    }

    private static void ApplyLabelPanelMetrics(Panel host, int tokenWidth, int dpi)
    {
        host.Margin = UiMetrics.OptionRowMargin(dpi);
        if (host.Controls.Count == 0 || host.Controls[0] is not TableLayoutPanel layout)
            return;

        foreach (Control c in layout.Controls)
        {
            switch (c)
            {
                case CheckBox cb:
                    cb.MaximumSize = new Size(tokenWidth, 0);
                    break;
                case Label lbl when lbl is not LinkLabel && lbl.Tag as string is "sublabel" or "token":
                    lbl.Font = lbl.Tag as string == "sublabel"
                        ? UiMetrics.SubLabelFont()
                        : UiMetrics.TokenLabelFont();
                    lbl.Margin = new Padding(UiMetrics.Scale(18, dpi), 0, 0, 0);
                    break;
                case LinkLabel ll when ll.Tag as string == "token" || ll.Tag as string == "sublabel":
                    ll.Font = ll.Tag as string == "sublabel"
                        ? UiMetrics.SubLabelFont()
                        : UiMetrics.TokenLabelFont();
                    ll.Margin = new Padding(UiMetrics.Scale(18, dpi), 0, 0, 0);
                    break;
            }
        }
    }

    private static void ApplyValueSizing(Control ctrl, OptionDefinition def, int dpi)
    {
        ctrl.Anchor = AnchorStyles.Left;
        ctrl.Dock = DockStyle.None;

        switch (def.Kind)
        {
            case OptionControlKind.Numeric:
                ctrl.Width = UiMetrics.Scale(WidthNumeric, dpi);
                return;
            case OptionControlKind.Choice:
                ctrl.Width = UiMetrics.Scale(
                    def.Token is "resolution" or "log_level" ? WidthMedium : WidthCombo, dpi);
                return;
            case OptionControlKind.MultiSelect:
                ctrl.Width = UiMetrics.Scale(WidthMulti, dpi);
                return;
            default:
                var textW = def.Category == OptionCategory.Advanced
                    ? WidthLong
                    : TextWidth(def.Token);
                ctrl.Width = UiMetrics.Scale(textW, dpi);
                break;
        }
    }

    private static int TextWidth(string token) => token switch
    {
        "spoof_osx_version" => WidthShort,
        "default_selection" or "icons_dir" or "font" or "banner" or "selection_big" or "selection_small" or "linux_prefixes" => WidthMedium,
        "csr_values" or "extra_kernel_version_strings" or "scan_driver_dirs" or "also_scan_dirs" or "also_scan_tool_dirs"
            or "dont_scan_volumes" or "dont_scan_dirs" or "dont_scan_files" or "dont_scan_tools" or "dont_scan_firmware"
            or "windows_recovery_files" => WidthLong,
        _ => WidthMedium
    };

    private static NumericUpDown CreateNumeric(OptionDefinition def, int dpi) =>
        new()
        {
            Minimum = def.NumericMin,
            Maximum = def.NumericMax,
            Value = def.NumericDefault,
            Height = UiMetrics.ControlHeight(dpi)
        };

    private static TextBox CreateText(int dpi) =>
        new() { Height = UiMetrics.ControlHeight(dpi) };

    private static ComboBox CreateCombo(OptionDefinition def, int dpi)
    {
        var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Height = UiMetrics.ControlHeight(dpi) };
        var display = def.ChoiceLabels ?? def.Choices;
        if (display is not null)
            c.Items.AddRange(display);
        if (c.Items.Count > 0)
            c.SelectedIndex = 0;
        return c;
    }

    private static CheckedListBox CreateCheckedList(OptionDefinition def, int dpi)
    {
        var list = new CheckedListBox
        {
            CheckOnClick = true,
            IntegralHeight = false,
            Height = MultiRowHeight(def, dpi)
        };
        if (def.Choices is null)
            return list;

        var display = def.ChoiceLabels ?? def.Choices;
        list.Items.AddRange(display);
        return list;
    }

    private static int MultiRowHeight(OptionDefinition def, int dpi)
    {
        if (def.Kind != OptionControlKind.MultiSelect)
            return UiMetrics.MultiRowHeight(dpi);
        var count = def.Choices?.Length ?? 0;
        return UiMetrics.MultiRowHeight(dpi, count);
    }

    public static void LoadBindings(IEnumerable<OptionBinding> bindings, RefindDocument doc)
    {
        foreach (var b in bindings)
        {
            var opt = doc.FindGlobal(b.Definition.Token);
            if (b.Definition.Kind == OptionControlKind.Boolean)
            {
                b.UseCheck.Checked = opt is { IsActive: true } &&
                    (opt.Values.Count == 0 || IsTruthy(opt.Values[0]));
                continue;
            }

            b.UseCheck.Checked = opt?.IsActive ?? false;
            ApplyOptionToControl(b, opt);
        }
    }

    public static void SaveBindings(IEnumerable<OptionBinding> bindings, RefindDocument doc)
    {
        foreach (var b in bindings)
        {
            if (b.Definition.Kind == OptionControlKind.Boolean)
            {
                if (b.UseCheck.Checked)
                    doc.SetGlobal(b.Definition.Token, true, []);
                else if (doc.FindGlobal(b.Definition.Token) is not null)
                    doc.SetGlobal(b.Definition.Token, true, ["false"]);
                continue;
            }

            if (!b.UseCheck.Checked)
            {
                var existing = doc.FindGlobal(b.Definition.Token);
                if (existing is not null)
                {
                    var inactiveValues = ReadValuesFromControl(b);
                    doc.SetGlobal(b.Definition.Token, false, inactiveValues.Count > 0 ? inactiveValues : [.. existing.Values]);
                }
                else
                    doc.RemoveGlobal(b.Definition.Token);
                continue;
            }

            var values = ReadValuesFromControl(b);
            doc.SetGlobal(b.Definition.Token, true, values);
        }
    }

    private static void ApplyOptionToControl(OptionBinding b, GlobalOption? opt)
    {
        var values = opt?.Values ?? [];
        switch (b.Definition.Kind)
        {
            case OptionControlKind.Numeric:
                var nud = (NumericUpDown)b.ValueControl;
                b.PreservedInvalidNumeric = null;
                if (values.Count > 0 && decimal.TryParse(values[0], out var n))
                    nud.Value = Math.Clamp(n, nud.Minimum, nud.Maximum);
                else if (values.Count > 0)
                    b.PreservedInvalidNumeric = values[0];
                else
                    nud.Value = b.Definition.NumericDefault;
                break;
            case OptionControlKind.Choice:
                var combo = (ComboBox)b.ValueControl;
                SelectChoice(combo, b.Definition, values);
                break;
            case OptionControlKind.MultiSelect:
                var clb = (CheckedListBox)b.ValueControl;
                var choices = b.Definition.Choices ?? [];
                for (var i = 0; i < clb.Items.Count; i++)
                {
                    var token = i < choices.Length ? choices[i] : clb.Items[i]?.ToString() ?? "";
                    clb.SetItemChecked(i, values.Any(v => string.Equals(v, token, StringComparison.OrdinalIgnoreCase)));
                }
                break;
            default:
                var tb = (TextBox)b.ValueControl;
                tb.Text = RefindScanPathHelper.IsFolderListToken(b.Definition.Token)
                    ? RefindWriter.JoinFolderListForEditor(values)
                    : string.Join(", ", values);
                break;
        }
    }

    private static List<string> ReadValuesFromControl(OptionBinding b)
    {
        switch (b.Definition.Kind)
        {
            case OptionControlKind.Numeric:
                if (!string.IsNullOrEmpty(b.PreservedInvalidNumeric))
                    return [b.PreservedInvalidNumeric];
                return [((NumericUpDown)b.ValueControl).Value.ToString()];
            case OptionControlKind.Choice:
                return ReadChoiceValues((ComboBox)b.ValueControl, b.Definition);
            case OptionControlKind.MultiSelect:
                var clb = (CheckedListBox)b.ValueControl;
                var choices = b.Definition.Choices ?? [];
                var picked = new List<string>();
                for (var i = 0; i < clb.Items.Count; i++)
                {
                    if (!clb.GetItemChecked(i) || i >= choices.Length)
                        continue;
                    picked.Add(choices[i]);
                }
                return picked;
            default:
                var text = ((TextBox)b.ValueControl).Text.Trim();
                if (text.Length == 0)
                    return [];
                if (RefindScanPathHelper.IsFolderListToken(b.Definition.Token))
                    return RefindWriter.ParseFolderListFromEditor(text);
                if (RefindTokens.AcceptsCommaSeparatedValues(b.Definition.Token))
                    return RefindWriter.ParseCommaListFromEditor(text);
                return [text];
        }
    }

    private static void SelectChoice(ComboBox combo, OptionDefinition def, IReadOnlyList<string> values)
    {
        var choices = def.Choices ?? [];
        if (choices.Length == 0)
            return;

        var current = def.Token == "resolution"
            ? string.Join(" ", values)
            : values.FirstOrDefault() ?? "";

        for (var i = 0; i < choices.Length; i++)
        {
            if (!string.Equals(choices[i], current, StringComparison.OrdinalIgnoreCase))
                continue;
            combo.SelectedIndex = i;
            return;
        }

        if (string.IsNullOrEmpty(current))
            return;

        while (combo.Items.Count > choices.Length)
            combo.Items.RemoveAt(combo.Items.Count - 1);
        combo.Items.Add(current);
        combo.SelectedIndex = combo.Items.Count - 1;
    }

    private static List<string> ReadChoiceValues(ComboBox combo, OptionDefinition def)
    {
        var choices = def.Choices ?? [];
        var idx = combo.SelectedIndex;
        if (idx < 0)
            return [];

        var sel = idx < choices.Length
            ? choices[idx]
            : combo.Items[idx]?.ToString() ?? "";

        if (def.Token == "resolution")
            return sel.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        return [sel];
    }

    private static bool IsTruthy(string v) =>
        !string.Equals(v, "false", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(v, "off", StringComparison.OrdinalIgnoreCase) &&
        v != "0";
}
