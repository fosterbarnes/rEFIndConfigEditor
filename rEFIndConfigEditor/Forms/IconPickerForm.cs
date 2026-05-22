using rEFIndConfigEditor.Config;
using rEFIndConfigEditor.UI;

namespace rEFIndConfigEditor.Forms;

internal sealed class IconPickerForm : Form
{
    private readonly ListView _list = new()
    {
        View = View.Details,
        FullRowSelect = true,
        HideSelection = false,
        MultiSelect = false,
        ShowGroups = true
    };
    private readonly ImageList _thumbs = new();
    private readonly Button _ok = new() { Text = "OK" };
    private readonly Button _cancel = new() { Text = "Cancel", DialogResult = DialogResult.Cancel };
    private readonly IReadOnlyList<RefindIconChoice> _choices;

    public string? SelectedPath { get; private set; }

    public IconPickerForm(int dpi, string? refindConfPath, RefindDocument? document)
    {
        AppFormIcon.Apply(this);
        Text = "Choose icon";
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;
        StartPosition = FormStartPosition.CenterParent;

        _choices = RefindIconHelper.BuildChoices(refindConfPath, document);

        var iconSize = UiMetrics.Scale(32, dpi);
        _thumbs.ImageSize = new Size(iconSize, iconSize);
        _thumbs.ColorDepth = ColorDepth.Depth32Bit;
        _list.SmallImageList = _thumbs;

        _list.Columns.Add("Name", UiMetrics.Scale(140, dpi));
        _list.Columns.Add("Path", UiMetrics.Scale(280, dpi));

        var onDiskGroup = new ListViewGroup("On this install");
        var catalogGroup = new ListViewGroup("Standard rEFInd icons");
        _list.Groups.Add(onDiskGroup);
        _list.Groups.Add(catalogGroup);

        foreach (var choice in _choices)
        {
            var group = choice.OnDisk ? onDiskGroup : catalogGroup;
            var displayLabel = choice.CatalogOnly ? $"{choice.Label} (not on disk)" : choice.Label;
            var item = new ListViewItem(displayLabel, -1, group) { Tag = choice };
            item.SubItems.Add(choice.ConfigPath);
            if (RefindIconHelper.CanLoadThumbnail(choice.AbsolutePath))
            {
                try
                {
                    using var img = Image.FromFile(choice.AbsolutePath!);
                    _thumbs.Images.Add(choice.ConfigPath, new Bitmap(img, _thumbs.ImageSize));
                    item.ImageKey = choice.ConfigPath;
                }
                catch
                {
                    // skip thumbnail
                }
            }

            _list.Items.Add(item);
        }

        _list.DoubleClick += (_, _) => ConfirmSelection();
        _ok.Click += (_, _) => ConfirmSelection();

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = UiMetrics.ScalePadding(8, 8, dpi)
        };
        footer.Controls.AddRange([_ok, _cancel]);

        _list.Dock = DockStyle.Fill;
        Controls.Add(_list);
        Controls.Add(footer);

        AcceptButton = _ok;
        CancelButton = _cancel;

        ClientSize = UiMetrics.ScaleSize(520, 420, dpi);
        DpiChanged += (_, e) => ResizeForDpi(e.DeviceDpiNew);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (_list.Items.Count > 0)
            _list.Items[0].Selected = true;
        ResizeColumns(DeviceDpi > 0 ? DeviceDpi : UiMetrics.BaselineDpi);
    }

    private void ConfirmSelection()
    {
        if (_list.SelectedItems.Count == 0
            || _list.SelectedItems[0].Tag is not RefindIconChoice choice)
            return;

        SelectedPath = choice.ConfigPath;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void ResizeForDpi(int dpi)
    {
        ClientSize = UiMetrics.ScaleSize(520, 420, dpi);
        ResizeColumns(dpi);
    }

    private void ResizeColumns(int dpi)
    {
        if (_list.Columns.Count < 2)
            return;
        var w = ClientSize.Width - UiMetrics.Scale(24, dpi);
        _list.Columns[0].Width = UiMetrics.Scale(160, dpi);
        _list.Columns[1].Width = Math.Max(UiMetrics.Scale(120, dpi), w - _list.Columns[0].Width);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _thumbs.Dispose();
        base.Dispose(disposing);
    }
}
