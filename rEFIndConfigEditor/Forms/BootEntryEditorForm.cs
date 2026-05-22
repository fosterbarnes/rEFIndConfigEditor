using rEFIndConfigEditor.Config;
using rEFIndConfigEditor.UI;

namespace rEFIndConfigEditor.Forms;

internal sealed class BootEntryEditorForm : StanzaEditorBase
{
    private readonly TextBox _title = new();
    private readonly TextBox _volume = new();
    private readonly TextBox _loader = new();
    private readonly TextBox _initrd = new();
    private readonly TextBox _icon = new();
    private readonly FlowLayoutPanel _iconHost = new()
    {
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        FlowDirection = FlowDirection.LeftToRight,
        WrapContents = false
    };
    private readonly Button _iconBrowse = new() { Text = "Browse...", AutoSize = true };
    private readonly Button _iconChoose = new() { Text = "Choose icon...", AutoSize = true };
    private readonly TextBox _firmwareBootnum = new();
    private readonly ComboBox _ostype = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _graphics = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _options = new() { Multiline = true };
    private readonly TextBox _addOptions = new() { Multiline = true };
    private readonly CheckBox _clearInitrd = new() { Text = "Clear initrd (empty line)", AutoSize = true };
    private readonly CheckBox _disabled = new() { Text = "Disabled", AutoSize = true };
    private readonly ListBox _submenus = new();
    private readonly Func<string?>? _getRefindConfPath;
    private readonly Func<RefindDocument?>? _getDocument;
    private Button? _ok;
    private Button? _cancel;
    private Button? _btnAdd;
    private Button? _btnEdit;
    private Button? _btnRemove;
    private Label? _submenuHeader;

    public MenuEntry Entry { get; }

    public BootEntryEditorForm(
        MenuEntry? existing = null,
        Func<string?>? getRefindConfPath = null,
        Func<RefindDocument?>? getDocument = null)
    {
        Entry = existing ?? new MenuEntry();
        _getRefindConfPath = getRefindConfPath;
        _getDocument = getDocument;
        Text = existing is null ? "Add boot entry" : "Edit boot entry";

        _ostype.Items.AddRange(["", "MacOS", "Linux", "ELILO", "Windows", "XOM"]);
        _graphics.Items.AddRange(["", "on", "off"]);
        _firmwareBootnum.TextChanged += (_, _) => UpdateFirmwareMode();

        AddRow("menuentry", _title);
        AddRow("volume", _volume);
        AddRow("loader", _loader);
        AddRow("initrd", _initrd);
        AddIconRow();
        AddRow("firmware_bootnum", _firmwareBootnum);
        AddRow("ostype", _ostype);
        AddRow("graphics", _graphics);
        AddRow("options", _options, tall: true);
        AddRow("add_options", _addOptions, tall: true);
        Controls.Add(_clearInitrd);
        AddRowLabelOnly("disabled", _disabled);
        Controls.Add(_disabled);

        _submenuHeader = new Label { Text = "Submenu entries:", AutoSize = true };
        Controls.Add(_submenuHeader);
        Controls.Add(_submenus);

        _btnAdd = new Button { Text = "Add" };
        _btnEdit = new Button { Text = "Edit" };
        _btnRemove = new Button { Text = "Remove" };
        _btnAdd.Click += (_, _) => EditSubmenu(null);
        _btnEdit.Click += (_, _) =>
        {
            if (_submenus.SelectedIndex >= 0)
                EditSubmenu(Entry.Submenus[_submenus.SelectedIndex]);
        };
        _btnRemove.Click += (_, _) =>
        {
            if (_submenus.SelectedIndex >= 0)
            {
                Entry.Submenus.RemoveAt(_submenus.SelectedIndex);
                RefreshSubmenuList();
            }
        };
        Controls.AddRange([_btnAdd, _btnEdit, _btnRemove]);

        _ok = new Button { Text = "OK", DialogResult = DialogResult.OK };
        _cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
        Controls.AddRange([_ok, _cancel]);
        AcceptButton = _ok;
        CancelButton = _cancel;

        LoadFromEntry();
        UpdateFirmwareMode();
        DpiChanged += (_, e) => ApplyLayoutMetrics(e.DeviceDpiNew);
    }

    private void AddIconRow()
    {
        var info = StanzaFieldHelp.Get("icon");
        var friendly = new Label { Text = info.Label, AutoSize = true };
        var tok = TokenDocLinks.CreateLabel("icon", DeviceDpi > 0 ? DeviceDpi : UiMetrics.BaselineDpi);
        Controls.AddRange([friendly, tok]);

        _iconBrowse.Click += (_, _) => BrowseIcon();
        _iconChoose.Click += (_, _) => ChooseIcon();
        _iconHost.Controls.AddRange([_icon, _iconBrowse, _iconChoose]);
        Controls.Add(_iconHost);

        Rows.Add(new StanzaEditorRow(friendly, tok, _iconHost, false));
        var text = StanzaFieldHelp.Tooltip("icon");
        ToolTip.SetToolTip(friendly, text);
        ToolTip.SetToolTip(tok, text);
        ToolTip.SetToolTip(_icon, text);
        ToolTip.SetToolTip(_iconBrowse, "Pick an image file from disk (PNG, ICNS, BMP, or JPEG).");
        ToolTip.SetToolTip(_iconChoose, "Pick a standard rEFInd icon or one already in your icons folder.");
    }

    private string? RefindConfPath => _getRefindConfPath?.Invoke();

    private RefindDocument? Document => _getDocument?.Invoke();

    private void BrowseIcon()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Select menu icon",
            Filter = RefindIconHelper.IconFileDialogFilter
        };
        var initial = RefindIconHelper.GetInitialBrowseDirectory(RefindConfPath, Document);
        if (initial is not null && Directory.Exists(initial))
            dlg.InitialDirectory = initial;

        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        _icon.Text = RefindIconHelper.FormatIconPath(dlg.FileName, RefindConfPath, Document);
    }

    private void ChooseIcon()
    {
        var dpi = DeviceDpi > 0 ? DeviceDpi : UiMetrics.BaselineDpi;
        using var picker = new IconPickerForm(dpi, RefindConfPath, Document);
        UiTheme.ApplySaved(picker);
        if (picker.ShowDialog(this) != DialogResult.OK || picker.SelectedPath is null)
            return;

        _icon.Text = picker.SelectedPath;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        ApplyLayoutMetrics(DeviceDpi);
    }

    private void ApplyLayoutMetrics(int dpi)
    {
        ClientSize = UiMetrics.ScaleSize(560, 640, dpi);
        var margin = UiMetrics.Scale(12, dpi);
        var valueLeft = UiMetrics.ValueColumnLeft(dpi);
        var labelW = UiMetrics.LabelColumnWidth(dpi);
        var rowStep = UiMetrics.FieldRowStep(dpi);
        var tallStep = UiMetrics.Scale(78, dpi);
        var fieldW = UiMetrics.Scale(360, dpi);
        var ctrlH = UiMetrics.ControlHeight(dpi);
        var optionsH = UiMetrics.Scale(60, dpi);
        var iconBtnH = ctrlH;
        var chooseW = UiMetrics.Scale(100, dpi);
        var browseW = UiMetrics.Scale(76, dpi);
        var iconBtnGap = UiMetrics.Scale(4, dpi);
        var iconTextW = fieldW - browseW - chooseW - iconBtnGap * 2;

        var y = margin;
        foreach (var row in Rows)
        {
            row.Friendly.SetBounds(margin, y + UiMetrics.Scale(2, dpi), labelW, 0);
            row.Friendly.MaximumSize = new Size(labelW, 0);
            row.Token.SetBounds(margin, y + UiMetrics.Scale(18, dpi), labelW, 0);
            row.Token.Font = UiMetrics.TokenLabelFont();

            if (row.Field == _iconHost)
            {
                _icon.SetBounds(0, 0, iconTextW, ctrlH);
                _iconBrowse.SetBounds(0, 0, browseW, iconBtnH);
                _iconChoose.SetBounds(0, 0, chooseW, iconBtnH);
                _iconHost.SetBounds(valueLeft, y, fieldW, ctrlH);
                y += rowStep;
                continue;
            }

            if (ReferenceEquals(row.Field, _disabled))
                continue;

            var w = row.Field is ComboBox
                ? UiMetrics.Scale(row.Field == _ostype ? 160 : 120, dpi)
                : fieldW;
            var h = row.Tall ? optionsH : ctrlH;
            var fieldY = y + (row.Field is TextBox ? UiMetrics.TextFieldTopNudge(dpi) : 0);
            row.Field.SetBounds(valueLeft, fieldY, w, h);
            y += row.Tall ? tallStep : rowStep;
        }

        _clearInitrd.SetBounds(valueLeft, y + UiMetrics.Scale(4, dpi), fieldW, 0);
        y += UiMetrics.Scale(28, dpi);

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

        _submenuHeader!.Left = margin;
        _submenuHeader.Top = y + UiMetrics.Scale(8, dpi);
        y += UiMetrics.Scale(30, dpi);
        _submenus.SetBounds(margin, y, fieldW, UiMetrics.Scale(100, dpi));

        var btnY = y + UiMetrics.Scale(108, dpi);
        var btnLeft = margin;
        foreach (var btn in new[] { _btnAdd!, _btnEdit!, _btnRemove! })
        {
            btn.SetBounds(btnLeft, btnY, UiMetrics.Scale(70, dpi), UiMetrics.Scale(28, dpi));
            btnLeft += btn.Width + UiMetrics.Scale(6, dpi);
        }

        var btnTop = btnY + UiMetrics.Scale(32, dpi);
        _cancel!.SetBounds(UiMetrics.Scale(440, dpi), btnTop, UiMetrics.Scale(90, dpi), UiMetrics.Scale(28, dpi));
        _ok!.SetBounds(UiMetrics.Scale(340, dpi), btnTop, UiMetrics.Scale(90, dpi), UiMetrics.Scale(28, dpi));
    }

    private void EditSubmenu(SubmenuEntry? sub)
    {
        using var dlg = new SubmenuEntryEditorForm(sub);
        UiTheme.ApplySaved(dlg);
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;
        if (sub is null)
            Entry.Submenus.Add(dlg.Entry);
        RefreshSubmenuList();
    }

    private void RefreshSubmenuList()
    {
        _submenus.Items.Clear();
        foreach (var s in Entry.Submenus)
            _submenus.Items.Add(s.Title);
    }

    private void LoadFromEntry()
    {
        _title.Text = Entry.Title;
        _volume.Text = Entry.GetField("volume") ?? "";
        _loader.Text = Entry.GetField("loader") ?? "";
        _initrd.Text = Entry.GetField("initrd") ?? "";
        _icon.Text = Entry.GetField("icon") ?? "";
        _firmwareBootnum.Text = Entry.GetField("firmware_bootnum") ?? "";
        SelectOstype(Entry.GetField("ostype"));
        _graphics.SelectedItem = Entry.GetField("graphics") ?? "";
        _options.Text = Entry.GetField("options") ?? "";
        _addOptions.Text = Entry.GetField("add_options") ?? "";
        _clearInitrd.Checked = Entry.Fields.TryGetValue("initrd", out var initrd) && initrd.IsExplicitEmpty;
        _disabled.Checked = Entry.Disabled;
        RefreshSubmenuList();
    }

    private void UpdateFirmwareMode()
    {
        var firmware = _firmwareBootnum.Text.Trim().Length > 0;
        _loader.Enabled = !firmware;
        _initrd.Enabled = !firmware;
        _clearInitrd.Enabled = !firmware;
        _volume.Enabled = !firmware;
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
        StanzaEditorFields.RemoveManagedFields(Entry.Fields, StanzaEditorFields.BootEntryTokens);
        Entry.SetField("volume", T(_volume.Text));
        if (string.IsNullOrEmpty(firmware))
            Entry.SetField("loader", T(_loader.Text));
        else
            Entry.SetField("firmware_bootnum", T(firmware));
        if (_clearInitrd.Checked)
            Entry.SetField("initrd", null, allowEmpty: true);
        else
            Entry.SetField("initrd", T(_initrd.Text));
        Entry.SetField("icon", T(_icon.Text));
        var ost = _ostype.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(ost))
            Entry.SetField("ostype", ost);
        var g = _graphics.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(g))
            Entry.SetField("graphics", g);
        Entry.SetField("options", T(_options.Text));
        Entry.SetField("add_options", T(_addOptions.Text));
        Entry.Disabled = _disabled.Checked;
        base.OnFormClosing(e);
    }

    private static string? T(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private void SelectOstype(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            _ostype.SelectedItem = "";
            return;
        }

        for (var i = 0; i < _ostype.Items.Count; i++)
        {
            if (!string.Equals(_ostype.Items[i]?.ToString(), value, StringComparison.OrdinalIgnoreCase))
                continue;
            _ostype.SelectedIndex = i;
            return;
        }

        _ostype.SelectedItem = value;
    }
}
