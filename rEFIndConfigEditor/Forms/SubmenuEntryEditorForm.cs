using rEFIndConfigEditor.Config;
using rEFIndConfigEditor.UI;

namespace rEFIndConfigEditor.Forms;

internal sealed class SubmenuEntryEditorForm : StanzaEditorBase
{
    private readonly TextBox _title = new();
    private readonly TextBox _loader = new();
    private readonly TextBox _initrd = new();
    private readonly TextBox _firmwareBootnum = new();
    private readonly ComboBox _graphics = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _options = new() { Multiline = true };
    private readonly TextBox _addOptions = new() { Multiline = true };
    private readonly CheckBox _clearInitrd = new() { Text = "Clear initrd (empty line)", AutoSize = true };
    private readonly CheckBox _clearOptions = new() { Text = "Clear options (empty line)", AutoSize = true };
    private readonly CheckBox _disabled = new() { Text = "Disabled", AutoSize = true };
    private Button? _ok;
    private Button? _cancel;

    public SubmenuEntry Entry { get; }

    public SubmenuEntryEditorForm(SubmenuEntry? existing = null)
    {
        Entry = existing ?? new SubmenuEntry();
        Text = existing is null ? "Add submenu entry" : "Edit submenu entry";

        _graphics.Items.AddRange(["", "on", "off"]);
        _firmwareBootnum.TextChanged += (_, _) => UpdateFirmwareMode();

        AddRow("submenuentry", _title);
        AddRow("loader", _loader);
        AddRow("initrd", _initrd);
        AddRow("firmware_bootnum", _firmwareBootnum);
        AddRow("graphics", _graphics);
        AddRow("options", _options, tall: true);
        AddRow("add_options", _addOptions, tall: true);

        Controls.AddRange([_clearInitrd, _clearOptions, _disabled]);
        AddRowLabelOnly("disabled", _disabled);

        _ok = new Button { Text = "OK", DialogResult = DialogResult.OK };
        _cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
        Controls.AddRange([_ok, _cancel]);
        AcceptButton = _ok;
        CancelButton = _cancel;

        LoadFromEntry();
        UpdateFirmwareMode();
        DpiChanged += (_, e) => ApplyLayoutMetrics(e.DeviceDpiNew);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        ApplyLayoutMetrics(DeviceDpi);
    }

    private void ApplyLayoutMetrics(int dpi)
    {
        ClientSize = UiMetrics.ScaleSize(460, 520, dpi);
        var margin = UiMetrics.Scale(12, dpi);
        var valueLeft = UiMetrics.Scale(120, dpi);
        var labelW = UiMetrics.Scale(100, dpi);
        var rowStep = UiMetrics.FieldRowStep(dpi);
        var fieldW = UiMetrics.Scale(320, dpi);
        var ctrlH = UiMetrics.ControlHeight(dpi);
        var multiH = UiMetrics.Scale(60, dpi);

        var y = margin;
        foreach (var row in Rows)
        {
            if (ReferenceEquals(row.Field, _disabled))
                continue;

            row.Friendly.SetBounds(margin, y + UiMetrics.Scale(2, dpi), labelW, 0);
            row.Friendly.MaximumSize = new Size(labelW, 0);
            row.Token.SetBounds(margin, y + UiMetrics.Scale(18, dpi), labelW, 0);
            row.Token.Font = UiMetrics.TokenLabelFont();

            var w = row.Field is ComboBox ? UiMetrics.Scale(120, dpi) : fieldW;
            var h = row.Tall ? multiH : ctrlH;
            var fieldY = y + (row.Field is TextBox ? UiMetrics.TextFieldTopNudge(dpi) : 0);
            row.Field.SetBounds(valueLeft, fieldY, w, h);
            y += row.Tall ? multiH + UiMetrics.Scale(18, dpi) : rowStep;
        }

        _clearInitrd.SetBounds(valueLeft, y + UiMetrics.Scale(4, dpi), fieldW, 0);
        y += UiMetrics.Scale(28, dpi);
        _clearOptions.SetBounds(valueLeft, y + UiMetrics.Scale(4, dpi), fieldW, 0);
        y += UiMetrics.Scale(32, dpi);

        foreach (var row in Rows)
        {
            if (!ReferenceEquals(row.Field, _disabled))
                continue;
            row.Friendly.SetBounds(margin, y + UiMetrics.Scale(2, dpi), labelW, 0);
            row.Friendly.MaximumSize = new Size(labelW, 0);
            row.Token.SetBounds(margin, y + UiMetrics.Scale(18, dpi), labelW, 0);
            row.Token.Font = UiMetrics.TokenLabelFont();
            _disabled.SetBounds(valueLeft, y + UiMetrics.Scale(4, dpi), fieldW, 0);
            y += rowStep;
            break;
        }

        _ok!.SetBounds(UiMetrics.Scale(240, dpi), y, UiMetrics.Scale(90, dpi), UiMetrics.Scale(28, dpi));
        _cancel!.SetBounds(UiMetrics.Scale(340, dpi), y, UiMetrics.Scale(90, dpi), UiMetrics.Scale(28, dpi));
    }

    private void LoadFromEntry()
    {
        _title.Text = Entry.Title;
        _loader.Text = Entry.GetField("loader") ?? "";
        _initrd.Text = Entry.GetField("initrd") ?? "";
        _firmwareBootnum.Text = Entry.GetField("firmware_bootnum") ?? "";
        _options.Text = Entry.GetField("options") ?? "";
        _addOptions.Text = Entry.GetField("add_options") ?? "";
        _graphics.SelectedItem = Entry.GetField("graphics") ?? "";
        _clearInitrd.Checked = Entry.Fields.TryGetValue("initrd", out var i) && i.IsExplicitEmpty;
        _clearOptions.Checked = Entry.Fields.TryGetValue("options", out var o) && o.IsExplicitEmpty;
        _disabled.Checked = Entry.Disabled;
    }

    private void UpdateFirmwareMode()
    {
        var firmware = _firmwareBootnum.Text.Trim().Length > 0;
        _loader.Enabled = !firmware;
        _initrd.Enabled = !firmware;
        _clearInitrd.Enabled = !firmware;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult != DialogResult.OK)
        {
            base.OnFormClosing(e);
            return;
        }

        if (string.IsNullOrWhiteSpace(_title.Text))
        {
            MessageBox.Show(this, "Title is required.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true;
            return;
        }

        var firmware = _firmwareBootnum.Text.Trim();
        if (string.IsNullOrEmpty(firmware) && string.IsNullOrWhiteSpace(_loader.Text))
        {
            MessageBox.Show(this, "Loader or firmware boot number is required.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true;
            return;
        }

        Entry.Title = _title.Text.Trim();
        StanzaEditorFields.RemoveManagedFields(Entry.Fields, StanzaEditorFields.SubmenuEntryTokens);
        if (string.IsNullOrEmpty(firmware))
            Entry.SetField("loader", NullIfEmpty(_loader.Text));
        else
            Entry.SetField("firmware_bootnum", NullIfEmpty(firmware));
        if (_clearInitrd.Checked)
            Entry.SetField("initrd", null, allowEmpty: true);
        else
            Entry.SetField("initrd", NullIfEmpty(_initrd.Text));
        if (_clearOptions.Checked)
            Entry.SetField("options", null, allowEmpty: true);
        else
            Entry.SetField("options", NullIfEmpty(_options.Text));
        Entry.SetField("add_options", NullIfEmpty(_addOptions.Text));
        var g = _graphics.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(g))
            Entry.SetField("graphics", g);
        Entry.Disabled = _disabled.Checked;
        base.OnFormClosing(e);
    }

    private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
