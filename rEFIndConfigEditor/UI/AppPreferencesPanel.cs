using rEFIndConfigEditor.Models;

namespace rEFIndConfigEditor.UI;

internal sealed class AppPreferencesPanel : Panel
{
    private readonly Label _themeLabel = new() { Text = "App theme", AutoSize = true };
    private readonly RadioButton _system = new() { Text = "System", AutoSize = true };
    private readonly RadioButton _light = new() { Text = "Light", AutoSize = true };
    private readonly RadioButton _dark = new() { Text = "Dark", AutoSize = true };
    private readonly RadioButton _draculaLight = new() { Text = "Dracula Light", AutoSize = true };
    private readonly RadioButton _draculaDark = new() { Text = "Dracula Dark", AutoSize = true };
    private readonly CheckBox _autoLoadLastConf = new()
    {
        Text = "Automatically load last .conf on launch",
        AutoSize = true
    };
    private readonly CheckBox _rememberLastTab = new()
    {
        Text = "Remember last selected tab",
        AutoSize = true
    };
    private readonly GroupBox _box;
    private bool _suppressThemeEvent;
    private bool _suppressAutoLoadEvent;
    private bool _suppressRememberTabEvent;

    public event EventHandler? ThemeChanged;
    public event EventHandler? AutoLoadLastConfChanged;
    public event EventHandler? RememberLastTabChanged;

    public UiThemeKind SelectedTheme
    {
        get
        {
            if (_system.Checked)
                return UiThemeKind.System;
            if (_light.Checked)
                return UiThemeKind.Light;
            if (_dark.Checked)
                return UiThemeKind.Dark;
            if (_draculaLight.Checked)
                return UiThemeKind.DraculaLight;
            if (_draculaDark.Checked)
                return UiThemeKind.DraculaDark;
            return UiThemeKind.System;
        }
    }

    public bool AutoLoadLastConfOnLaunch => _autoLoadLastConf.Checked;

    public bool RememberLastSelectedTab => _rememberLastTab.Checked;

    public AppPreferencesPanel(int dpi)
    {
        Dock = DockStyle.Top;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        _box = new GroupBox
        {
            Text = "Preferences (these settings auto-apply)",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
        };

        var content = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };

        var themeFlow = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };

        _system.CheckedChanged += ThemeRadioCheckedChanged;
        _light.CheckedChanged += ThemeRadioCheckedChanged;
        _dark.CheckedChanged += ThemeRadioCheckedChanged;
        _draculaLight.CheckedChanged += ThemeRadioCheckedChanged;
        _draculaDark.CheckedChanged += ThemeRadioCheckedChanged;
        _autoLoadLastConf.CheckedChanged += AutoLoadLastConfCheckedChanged;
        _rememberLastTab.CheckedChanged += RememberLastTabCheckedChanged;

        themeFlow.Controls.AddRange([_system, _light, _dark, _draculaLight, _draculaDark]);
        content.Controls.Add(_themeLabel);
        content.Controls.Add(themeFlow);
        content.Controls.Add(_autoLoadLastConf);
        content.Controls.Add(_rememberLastTab);
        _box.Controls.Add(content);
        Controls.Add(_box);

        ApplyMetrics(dpi);
    }

    public void ApplyMetrics(int dpi)
    {
        Margin = UiMetrics.StackedItemMargin(dpi);
        Padding = UiMetrics.ScalePadding(8, 4, dpi);
        _box.Padding = new Padding(
            UiMetrics.Scale(8, dpi),
            UiMetrics.Scale(4, dpi),
            UiMetrics.Scale(8, dpi),
            UiMetrics.Scale(8, dpi));

        var sectionGap = UiMetrics.Scale(8, dpi);
        _themeLabel.Margin = new Padding(0, UiMetrics.Scale(4, dpi), 0, UiMetrics.Scale(2, dpi));

        var radioMargin = new Padding(0, UiMetrics.Scale(4, dpi), UiMetrics.Scale(20, dpi), UiMetrics.Scale(4, dpi));
        var radioMarginLast = new Padding(0, UiMetrics.Scale(4, dpi), 0, UiMetrics.Scale(4, dpi));
        _system.Margin = radioMargin;
        _light.Margin = radioMargin;
        _dark.Margin = radioMargin;
        _draculaLight.Margin = radioMargin;
        _draculaDark.Margin = radioMarginLast;
        _autoLoadLastConf.Margin = new Padding(0, sectionGap, 0, UiMetrics.Scale(4, dpi));
        _rememberLastTab.Margin = new Padding(0, 0, 0, UiMetrics.Scale(4, dpi));
    }

    public void SetTheme(UiThemeKind theme)
    {
        _suppressThemeEvent = true;
        switch (theme)
        {
            case UiThemeKind.Light:
                _light.Checked = true;
                break;
            case UiThemeKind.Dark:
                _dark.Checked = true;
                break;
            case UiThemeKind.DraculaLight:
                _draculaLight.Checked = true;
                break;
            case UiThemeKind.DraculaDark:
                _draculaDark.Checked = true;
                break;
            default:
                _system.Checked = true;
                break;
        }
        _suppressThemeEvent = false;
    }

    public void SetAutoLoadLastConfOnLaunch(bool enabled)
    {
        _suppressAutoLoadEvent = true;
        _autoLoadLastConf.Checked = enabled;
        _suppressAutoLoadEvent = false;
    }

    public void SetRememberLastSelectedTab(bool enabled)
    {
        _suppressRememberTabEvent = true;
        _rememberLastTab.Checked = enabled;
        _suppressRememberTabEvent = false;
    }

    private void ThemeRadioCheckedChanged(object? sender, EventArgs e)
    {
        if (_suppressThemeEvent)
            return;
        if (sender is not RadioButton { Checked: true })
            return;
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void AutoLoadLastConfCheckedChanged(object? sender, EventArgs e)
    {
        if (_suppressAutoLoadEvent)
            return;
        AutoLoadLastConfChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RememberLastTabCheckedChanged(object? sender, EventArgs e)
    {
        if (_suppressRememberTabEvent)
            return;
        RememberLastTabChanged?.Invoke(this, EventArgs.Empty);
    }
}
