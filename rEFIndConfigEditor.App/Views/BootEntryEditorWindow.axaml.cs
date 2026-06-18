using Avalonia.Controls;
using Avalonia.Layout;
using rEFIndConfigEditor.Config;
using rEFIndConfigEditor.Platform;
using rEFIndConfigEditor.UI;

namespace rEFIndConfigEditor.Views;

internal sealed partial class BootEntryEditorWindow : Window
{
    private readonly TaskCompletionSource<bool> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Func<string?> _getRefindConfPath;
    private readonly Func<RefindDocument?> _getDocument;

    private readonly TextBox _title = new();
    private readonly TextBox _volume = new();
    private readonly TextBox _loader = new();
    private readonly TextBox _initrd = new();
    private readonly TextBox _icon = new();
    private readonly TextBox _firmwareBootnum = new();
    private readonly ComboBox _ostype = new();
    private readonly ComboBox _graphics = new();
    private readonly TextBox _options = new() { AcceptsReturn = true, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
    private readonly TextBox _addOptions = new() { AcceptsReturn = true, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
    private readonly CheckBox _clearInitrd = new() { Content = "Clear initrd (empty line)" };
    private readonly CheckBox _disabled = new() { Content = "Disabled" };
    private readonly ListBox _submenus = new() { MinHeight = 100, Classes = { "setting-input-frame" } };

    public MenuEntry Entry { get; }

    private BootEntryEditorWindow(
        MenuEntry entry,
        Func<string?> getRefindConfPath,
        Func<RefindDocument?> getDocument,
        bool isNew)
    {
        Entry = entry;
        _getRefindConfPath = getRefindConfPath;
        _getDocument = getDocument;
        Title = isNew ? "Add boot entry" : "Edit boot entry";

        InitializeComponent();
        AppIconLoader.TryApplyWindowIcon(this);
        BuildForm();
        LoadFromEntry();
        UpdateFirmwareMode();

        _firmwareBootnum.TextChanged += (_, _) => UpdateFirmwareMode();
        OkButton.Click += (_, _) =>
        {
            if (TrySave())
                Finish(true);
        };
        CancelButton.Click += (_, _) => Finish(false);
        Closed += (_, _) =>
        {
            if (!_result.Task.IsCompleted)
                _result.TrySetResult(false);
        };
    }

    public static async Task<MenuEntry?> EditAsync(
        Window owner,
        MenuEntry? existing,
        Func<string?> getRefindConfPath,
        Func<RefindDocument?> getDocument,
        bool isNew)
    {
        var entry = existing ?? new MenuEntry();
        var window = new BootEntryEditorWindow(entry, getRefindConfPath, getDocument, isNew);
        await window.ShowDialog(owner).ConfigureAwait(true);
        return window._result.Task.Result ? entry : null;
    }

    private void BuildForm()
    {
        _ostype.ItemsSource = new[] { "", "MacOS", "Linux", "ELILO", "Windows", "XOM" };
        _graphics.ItemsSource = new[] { "", "on", "off" };

        RootPanel.Children.Add(StanzaEditorUi.CreateFieldRow("menuentry", _title));
        RootPanel.Children.Add(StanzaEditorUi.CreateFieldRow("volume", _volume));
        RootPanel.Children.Add(StanzaEditorUi.CreateFieldRow("loader", _loader));
        RootPanel.Children.Add(StanzaEditorUi.CreateFieldRow("initrd", _initrd));

        var iconBrowse = new Button { Content = "Browse...", MinHeight = UiMetrics.ControlHeight };
        var iconChoose = new Button { Content = "Choose icon...", MinHeight = UiMetrics.ControlHeight };
        iconBrowse.Click += async (_, _) => await BrowseIconAsync().ConfigureAwait(true);
        iconChoose.Click += async (_, _) => await ChooseIconAsync().ConfigureAwait(true);
        ToolTip.SetTip(iconBrowse, "Pick an image file from disk (PNG, ICNS, BMP, or JPEG).");
        ToolTip.SetTip(iconChoose, "Pick a standard rEFInd icon or one already in your icons folder.");
        var iconButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Children = { _icon, iconBrowse, iconChoose },
        };
        RootPanel.Children.Add(StanzaEditorUi.CreateFieldRow("icon", iconButtons));

        RootPanel.Children.Add(StanzaEditorUi.CreateFieldRow("firmware_bootnum", _firmwareBootnum));
        RootPanel.Children.Add(StanzaEditorUi.CreateFieldRow("ostype", _ostype));
        RootPanel.Children.Add(StanzaEditorUi.CreateFieldRow("graphics", _graphics));
        RootPanel.Children.Add(StanzaEditorUi.CreateFieldRow("options", _options, tall: true));
        RootPanel.Children.Add(StanzaEditorUi.CreateFieldRow("add_options", _addOptions, tall: true));

        RootPanel.Children.Add(_clearInitrd);
        RootPanel.Children.Add(StanzaEditorUi.CreateFieldRow("disabled", _disabled));

        RootPanel.Children.Add(new TextBlock { Text = "Submenu entries:", Classes = { "setting-label" } });
        RootPanel.Children.Add(_submenus);

        var addSub = new Button { Content = "Add", MinWidth = 70 };
        var editSub = new Button { Content = "Edit", MinWidth = 70 };
        var removeSub = new Button { Content = "Remove", MinWidth = 70 };
        addSub.Click += async (_, _) => await EditSubmenuAsync(null).ConfigureAwait(true);
        editSub.Click += async (_, _) =>
        {
            if (_submenus.SelectedIndex >= 0)
                await EditSubmenuAsync(Entry.Submenus[_submenus.SelectedIndex]).ConfigureAwait(true);
        };
        removeSub.Click += (_, _) =>
        {
            if (_submenus.SelectedIndex < 0)
                return;
            Entry.Submenus.RemoveAt(_submenus.SelectedIndex);
            RefreshSubmenuList();
        };

        RootPanel.Children.Add(new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 6,
            Children = { addSub, editSub, removeSub },
        });
    }

    private async Task BrowseIconAsync()
    {
        var picked = await PlatformServices.Current.PickFileAsync(
            "Select menu icon",
            RefindIconHelper.IconFileDialogFilter).ConfigureAwait(true);
        if (picked is null)
            return;
        _icon.Text = RefindIconHelper.FormatIconPath(picked, _getRefindConfPath(), _getDocument());
    }

    private async Task ChooseIconAsync()
    {
        var picked = await IconPickerWindow.PickAsync(this, _getRefindConfPath(), _getDocument()).ConfigureAwait(true);
        if (picked is not null)
            _icon.Text = picked;
    }

    private async Task EditSubmenuAsync(SubmenuEntry? sub)
    {
        var result = await SubmenuEntryEditorWindow.ShowAsync(this, sub).ConfigureAwait(true);
        if (result is null)
            return;
        if (sub is null)
            Entry.Submenus.Add(result);
        RefreshSubmenuList();
    }

    private void RefreshSubmenuList() =>
        _submenus.ItemsSource = Entry.Submenus.Select(s => s.Title).ToList();

    private void LoadFromEntry()
    {
        _title.Text = Entry.Title;
        _volume.Text = Entry.GetField("volume") ?? "";
        _loader.Text = Entry.GetField("loader") ?? "";
        _initrd.Text = Entry.GetField("initrd") ?? "";
        _icon.Text = Entry.GetField("icon") ?? "";
        _firmwareBootnum.Text = Entry.GetField("firmware_bootnum") ?? "";
        SelectCombo(_ostype, Entry.GetField("ostype"));
        SelectCombo(_graphics, Entry.GetField("graphics") ?? "");
        _options.Text = Entry.GetField("options") ?? "";
        _addOptions.Text = Entry.GetField("add_options") ?? "";
        _clearInitrd.IsChecked = Entry.Fields.TryGetValue("initrd", out var initrd) && initrd.IsExplicitEmpty;
        _disabled.IsChecked = Entry.Disabled;
        RefreshSubmenuList();
    }

    private static void SelectCombo(ComboBox combo, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            combo.SelectedIndex = 0;
            return;
        }

        if (combo.ItemsSource is IEnumerable<string> items)
        {
            var list = items.ToList();
            var index = list.FindIndex(i => string.Equals(i, value, StringComparison.OrdinalIgnoreCase));
            combo.SelectedIndex = index >= 0 ? index : 0;
        }
    }

    private void UpdateFirmwareMode()
    {
        var firmware = _firmwareBootnum.Text?.Trim().Length > 0;
        _loader.IsEnabled = !firmware;
        _initrd.IsEnabled = !firmware;
        _clearInitrd.IsEnabled = !firmware;
        _volume.IsEnabled = !firmware;
    }

    private bool TrySave()
    {
        if (string.IsNullOrWhiteSpace(_title.Text))
        {
            PlatformServices.Current.ShowWarning(Title ?? AppBranding.DisplayName, "Title is required.");
            return false;
        }

        var firmware = _firmwareBootnum.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(firmware) && string.IsNullOrWhiteSpace(_loader.Text))
        {
            PlatformServices.Current.ShowWarning(Title ?? AppBranding.DisplayName,
                "Loader or firmware boot number is required.");
            return false;
        }

        Entry.Title = _title.Text.Trim();
        StanzaEditorFields.RemoveManagedFields(Entry.Fields, StanzaEditorFields.BootEntryTokens);
        Entry.SetField("volume", T(_volume.Text));
        if (string.IsNullOrEmpty(firmware))
            Entry.SetField("loader", T(_loader.Text));
        else
            Entry.SetField("firmware_bootnum", T(firmware));
        if (_clearInitrd.IsChecked == true)
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
        Entry.Disabled = _disabled.IsChecked == true;
        return true;
    }

    private static string? T(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private void Finish(bool accepted)
    {
        _result.TrySetResult(accepted);
        Close();
    }
}
