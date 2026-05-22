using rEFIndConfigEditor.Config;
using rEFIndConfigEditor.UI;

namespace rEFIndConfigEditor.Forms;

internal enum RefindLogImportPurpose
{
    BootEntries,
    DefaultSelection
}

internal sealed class RefindLogImportForm : Form
{
    private readonly ListBox _list;
    private readonly Label _hint = new() { Dock = DockStyle.Top, AutoSize = false };
    private readonly Button _cancel = new() { Text = "Cancel", DialogResult = DialogResult.Cancel };
    private readonly Button _accept;

    public IReadOnlyList<RefindLogBootCandidate> SelectedCandidates { get; private set; } = [];
    public IReadOnlyList<string> SelectedVolumes { get; private set; } = [];

    private readonly bool _singleSelect;
    private readonly bool _volumeMode;
    private readonly string _pickPrompt;

    public RefindLogImportForm(
        IReadOnlyList<string> items,
        string windowTitle,
        string hint,
        string acceptText = "Add",
        string? pickPrompt = null)
        : this(volumeMode: true, singleSelect: false)
    {
        _pickPrompt = pickPrompt ?? "Select at least one item.";
        Text = windowTitle;
        _hint.Text = hint;
        _accept.Text = acceptText;
        foreach (var item in items)
            _list.Items.Add(item);
        if (_list.Items.Count > 0)
            _list.SelectedIndex = 0;
    }

    public RefindLogImportForm(IReadOnlyList<RefindLogBootCandidate> candidates, RefindLogImportPurpose purpose = RefindLogImportPurpose.BootEntries)
        : this(volumeMode: false, singleSelect: purpose == RefindLogImportPurpose.DefaultSelection)
    {
        Text = _singleSelect ? "Choose default from refind.log" : "Import from refind.log";
        _list.DisplayMember = nameof(RefindLogBootCandidate.Summary);
        _hint.Text = _singleSelect
            ? "Config boot entries are listed first ([Manual Entry]), then loaders from the log. Select one for the default boot choice:"
            : "Select one or more boot loaders discovered in the log:";
        _accept.Text = _singleSelect ? "Use" : "Create entries";
        foreach (var candidate in candidates)
            _list.Items.Add(candidate);
        if (_list.Items.Count > 0)
            _list.SelectedIndex = 0;
    }

    private RefindLogImportForm(bool volumeMode, bool singleSelect)
    {
        AppFormIcon.Apply(this);
        _volumeMode = volumeMode;
        _singleSelect = singleSelect;
        _pickPrompt = volumeMode ? "Select at least one volume." : "";
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        _list = new ListBox
        {
            Dock = DockStyle.Fill,
            IntegralHeight = false,
            SelectionMode = singleSelect ? SelectionMode.One : SelectionMode.MultiExtended
        };
        _accept = new Button();
        _accept.Click += (_, _) =>
        {
            if (!TryAcceptSelection())
                return;
            DialogResult = DialogResult.OK;
            Close();
        };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        buttons.Controls.AddRange([_cancel, _accept]);

        var listHost = new Panel { Dock = DockStyle.Fill };
        listHost.Controls.Add(_list);

        Controls.Add(listHost);
        Controls.Add(buttons);
        Controls.Add(_hint);

        AcceptButton = _accept;
        CancelButton = _cancel;

        _list.DoubleClick += (_, _) =>
        {
            if (_list.SelectedIndex < 0)
                return;
            if (!TryAcceptSelection())
                return;
            DialogResult = DialogResult.OK;
            Close();
        };

        DpiChanged += (_, e) => ApplyLayoutMetrics(e.DeviceDpiNew);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        ApplyLayoutMetrics(DeviceDpi);
    }

    private void ApplyLayoutMetrics(int dpi)
    {
        ClientSize = UiMetrics.ScaleSize(640, 360, dpi);
        MinimumSize = UiMetrics.ScaleSize(480, 280, dpi);
        _hint.Height = UiMetrics.Scale(28, dpi);
        _hint.Padding = new Padding(UiMetrics.Scale(8, dpi), UiMetrics.Scale(8, dpi), UiMetrics.Scale(8, dpi), 0);
        var pad = UiMetrics.Scale(8, dpi);
        var buttons = Controls.OfType<FlowLayoutPanel>().First();
        buttons.Padding = new Padding(pad);
        _cancel.SetBounds(0, 0, UiMetrics.Scale(90, dpi), UiMetrics.Scale(28, dpi));
        var acceptW = _volumeMode ? 70 : _singleSelect ? 70 : 110;
        _accept.SetBounds(0, 0, UiMetrics.Scale(acceptW, dpi), UiMetrics.Scale(28, dpi));
    }

    private bool TryAcceptSelection()
    {
        if (_list.SelectedIndices.Count == 0)
        {
            MessageBox.Show(this, PickPrompt(), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        if (_volumeMode)
        {
            var volumes = new List<string>(_list.SelectedIndices.Count);
            foreach (int i in _list.SelectedIndices)
            {
                if (_list.Items[i]?.ToString() is { Length: > 0 } v)
                    volumes.Add(v);
            }

            if (volumes.Count == 0)
            {
                MessageBox.Show(this, PickPrompt(), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            SelectedVolumes = volumes;
            return true;
        }

        var selected = new List<RefindLogBootCandidate>(_list.SelectedIndices.Count);
        foreach (int i in _list.SelectedIndices)
        {
            if (_list.Items[i] is RefindLogBootCandidate c)
                selected.Add(c);
        }

        if (selected.Count == 0)
        {
            MessageBox.Show(this, PickPrompt(), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        if (_singleSelect && selected.Count != 1)
        {
            MessageBox.Show(this, "Select exactly one boot loader entry.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        SelectedCandidates = selected;
        return true;
    }

    private string PickPrompt()
    {
        if (_volumeMode)
            return _pickPrompt;
        return _singleSelect ? "Select a boot loader entry." : "Select at least one boot loader entry.";
    }
}
