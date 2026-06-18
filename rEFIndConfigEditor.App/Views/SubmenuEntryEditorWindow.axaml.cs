using Avalonia.Controls;
using Avalonia.Interactivity;
using rEFIndConfigEditor;
using rEFIndConfigEditor.Config;
using rEFIndConfigEditor.Platform;
using rEFIndConfigEditor.UI;

namespace rEFIndConfigEditor.Views;

public partial class SubmenuEntryEditorWindow : Window
{
    private readonly TextBox _title = new();
    private readonly TextBox _loader = new();
    private readonly TextBox _initrd = new();
    private readonly TextBox _firmwareBootnum = new();
    private readonly ComboBox _graphics = new();
    private readonly TextBox _options = new();
    private readonly TextBox _addOptions = new();
    private readonly CheckBox _clearInitrd = new() { Content = "Clear initrd (empty line)" };
    private readonly CheckBox _clearOptions = new() { Content = "Clear options (empty line)" };
    private readonly CheckBox _disabled = new() { Content = "Disabled" };

    public SubmenuEntry Entry { get; }
    public bool Accepted { get; private set; }

    public SubmenuEntryEditorWindow() : this(null) { }

    public SubmenuEntryEditorWindow(SubmenuEntry? existing = null)
    {
        Entry = existing ?? new SubmenuEntry();
        InitializeComponent();
        AppIconLoader.TryApplyWindowIcon(this);
        Title = existing is null ? "Add submenu entry" : "Edit submenu entry";
        BuildUi();
        LoadFromEntry();
        UpdateFirmwareMode();
        OkButton.Click += OnOk;
        CancelButton.Click += (_, _) => Close();
    }

    public static async Task<SubmenuEntry?> ShowAsync(Window owner, SubmenuEntry? existing)
    {
        var window = new SubmenuEntryEditorWindow(existing);
        await window.ShowDialog(owner).ConfigureAwait(true);
        return window.Accepted ? window.Entry : null;
    }

    private void BuildUi()
    {
        _graphics.ItemsSource = new[] { "", "on", "off" };
        _firmwareBootnum.TextChanged += (_, _) => UpdateFirmwareMode();

        RootPanel.Children.Add(StanzaEditorUi.CreateFieldRow("submenuentry", _title));
        RootPanel.Children.Add(StanzaEditorUi.CreateFieldRow("loader", _loader));
        RootPanel.Children.Add(StanzaEditorUi.CreateFieldRow("initrd", _initrd));
        RootPanel.Children.Add(StanzaEditorUi.CreateFieldRow("firmware_bootnum", _firmwareBootnum));
        RootPanel.Children.Add(StanzaEditorUi.CreateFieldRow("graphics", _graphics));
        RootPanel.Children.Add(StanzaEditorUi.CreateFieldRow("options", _options, tall: true));
        RootPanel.Children.Add(StanzaEditorUi.CreateFieldRow("add_options", _addOptions, tall: true));
        RootPanel.Children.Add(_clearInitrd);
        RootPanel.Children.Add(_clearOptions);
        RootPanel.Children.Add(StanzaEditorUi.CreateFieldRow("disabled", _disabled));
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
        _clearInitrd.IsChecked = Entry.Fields.TryGetValue("initrd", out var i) && i.IsExplicitEmpty;
        _clearOptions.IsChecked = Entry.Fields.TryGetValue("options", out var o) && o.IsExplicitEmpty;
        _disabled.IsChecked = Entry.Disabled;
    }

    private void UpdateFirmwareMode()
    {
        var firmware = _firmwareBootnum.Text?.Trim().Length > 0;
        _loader.IsEnabled = !firmware;
        _initrd.IsEnabled = !firmware;
        _clearInitrd.IsEnabled = !firmware;
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_title.Text))
        {
            PlatformServices.Current.ShowWarning(Title ?? AppBranding.DisplayName, "Title is required.");
            return;
        }

        var firmware = _firmwareBootnum.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(firmware) && string.IsNullOrWhiteSpace(_loader.Text))
        {
            PlatformServices.Current.ShowWarning(Title ?? AppBranding.DisplayName, "Loader or firmware boot number is required.");
            return;
        }

        Entry.Title = _title.Text.Trim();
        StanzaEditorFields.RemoveManagedFields(Entry.Fields, StanzaEditorFields.SubmenuEntryTokens);
        if (string.IsNullOrEmpty(firmware))
            Entry.SetField("loader", NullIfEmpty(_loader.Text));
        else
            Entry.SetField("firmware_bootnum", NullIfEmpty(firmware));
        if (_clearInitrd.IsChecked == true)
            Entry.SetField("initrd", null, allowEmpty: true);
        else
            Entry.SetField("initrd", NullIfEmpty(_initrd.Text));
        if (_clearOptions.IsChecked == true)
            Entry.SetField("options", null, allowEmpty: true);
        else
            Entry.SetField("options", NullIfEmpty(_options.Text));
        Entry.SetField("add_options", NullIfEmpty(_addOptions.Text));
        var g = _graphics.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(g))
            Entry.SetField("graphics", g);
        Entry.Disabled = _disabled.IsChecked == true;

        Accepted = true;
        Close();
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
