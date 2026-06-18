using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using rEFIndConfigEditor.Models;
using rEFIndConfigEditor.Settings;
using rEFIndConfigEditor.ViewModels;

namespace rEFIndConfigEditor.UI;

internal static class AppSettingsPanelBuilder
{
    public static Control Build(TabItem tab, MainWindowViewModel viewModel)
    {
        var sections = new StackPanel
        {
            Spacing = UiMetrics.AppSettingsSectionGapPx,
            Margin = UiMetrics.TabContentPadding,
        };

        var themeRadios = new List<RadioButton>();
        var titleBarRadios = new List<RadioButton>();
        var checkboxes = new Dictionary<string, CheckBox>(StringComparer.Ordinal);

        foreach (var group in SettingCatalog.ForCategory(SettingCategory.App)
                     .GroupBy(d => d.UiSection ?? string.Empty)
                     .OrderBy(g => SectionOrder(g.Key)))
        {
            var sectionContent = BuildSection(group.Key, group.ToList(), tab, viewModel, themeRadios, titleBarRadios, checkboxes);
            if (sectionContent is null)
                continue;

            var title = group.Key switch
            {
                SettingCatalog.AppSectionPreferences => "Theme",
                _ => group.Key,
            };
            sections.Children.Add(UiTheme.CreateGroupBox(title, sectionContent));
        }

        viewModel.RegisterAppSettingsControls(themeRadios, titleBarRadios, checkboxes);
        return sections;
    }

    private static int SectionOrder(string section) => section switch
    {
        SettingCatalog.AppSectionPreferences => 0,
        SettingCatalog.AppSectionTitleBar => 1,
        SettingCatalog.AppSectionFonts => 2,
        SettingCatalog.AppSectionTextSizes => 3,
        SettingCatalog.AppSectionLogging => 4,
        SettingCatalog.AppSectionUpdates => 5,
        _ => 99,
    };

    private static Control? BuildSection(
        string section,
        IReadOnlyList<SettingDefinition> defs,
        TabItem tab,
        MainWindowViewModel viewModel,
        List<RadioButton> themeRadios,
        List<RadioButton> titleBarRadios,
        Dictionary<string, CheckBox> checkboxes)
    {
        return section switch
        {
            SettingCatalog.AppSectionPreferences => BuildPreferencesSection(defs, tab, viewModel, themeRadios, checkboxes),
            SettingCatalog.AppSectionTitleBar => BuildTitleBarSection(defs, tab, viewModel, titleBarRadios),
            SettingCatalog.AppSectionFonts => BuildFontsSection(defs, tab, viewModel),
            SettingCatalog.AppSectionTextSizes => BuildTextSizesSection(defs, tab, viewModel),
            SettingCatalog.AppSectionLogging => BuildLoggingSection(defs, tab, viewModel, checkboxes),
            SettingCatalog.AppSectionUpdates => BuildUpdatesSection(defs, tab, viewModel, checkboxes),
            _ => null,
        };
    }

    private static Control BuildPreferencesSection(
        IReadOnlyList<SettingDefinition> defs,
        TabItem tab,
        MainWindowViewModel viewModel,
        List<RadioButton> themeRadios,
        Dictionary<string, CheckBox> checkboxes)
    {
        var stack = new StackPanel { Spacing = UiMetrics.AppSettingsControlGapPx };

        foreach (var def in defs)
        {
            switch (def.Token)
            {
                case "ui_theme":
                    stack.Children.Add(BuildThemePanel(def, tab, viewModel, themeRadios));
                    break;
                case "ui_remember_last_tab":
                    stack.Children.Add(BuildBooleanSetting(def, tab, viewModel, checkboxes,
                        () => viewModel.RememberLastSelectedTab,
                        v => viewModel.RememberLastSelectedTab = v));
                    break;
                case "ui_auto_load_last_conf":
                    stack.Children.Add(BuildBooleanSetting(def, tab, viewModel, checkboxes,
                        () => viewModel.AutoLoadLastConfOnLaunch,
                        v => viewModel.AutoLoadLastConfOnLaunch = v));
                    break;
            }
        }

        return stack;
    }

    private static Control BuildThemePanel(
        SettingDefinition def,
        TabItem tab,
        MainWindowViewModel viewModel,
        List<RadioButton> themeRadios)
    {
        var themeRadioPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = UiMetrics.ThemeRadioSpacingPx,
        };

        foreach (var (label, theme) in viewModel.ThemeOptions)
        {
            var rb = new RadioButton
            {
                Content = label,
                GroupName = "Theme",
                Tag = theme,
                IsChecked = viewModel.SelectedTheme == theme,
            };
            rb.IsCheckedChanged += (_, _) =>
            {
                if (rb.IsChecked == true && rb.Tag is UiThemeKind t)
                    viewModel.SelectedTheme = t;
            };
            themeRadioPanel.Children.Add(rb);
            themeRadios.Add(rb);
        }

        ToolTip.SetTip(themeRadioPanel, SettingTooltipHelper.Build(def));
        viewModel.RegisterNavTarget(tab, def.Token, themeRadioPanel);

        return themeRadioPanel;
    }

    private static Control? BuildTitleBarSection(
        IReadOnlyList<SettingDefinition> defs,
        TabItem tab,
        MainWindowViewModel viewModel,
        List<RadioButton> titleBarRadios)
    {
        if (!OperatingSystem.IsMacOS())
            return null;

        var def = defs.FirstOrDefault(d => d.Token == "ui_mac_title_bar");
        if (def is null)
            return null;

        var radioPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = UiMetrics.ThemeRadioSpacingPx,
        };

        foreach (var (label, style) in viewModel.TitleBarStyleOptions)
        {
            var rb = new RadioButton
            {
                Content = label,
                GroupName = "TitleBar",
                Tag = style,
                IsChecked = viewModel.MacTitleBarStyle == style,
            };
            rb.IsCheckedChanged += (_, _) =>
            {
                if (rb.IsChecked == true && rb.Tag is MacTitleBarStyle s)
                    viewModel.MacTitleBarStyle = s;
            };
            radioPanel.Children.Add(rb);
            titleBarRadios.Add(rb);
        }

        ToolTip.SetTip(radioPanel, SettingTooltipHelper.Build(def));
        viewModel.RegisterNavTarget(tab, def.Token, radioPanel);
        return radioPanel;
    }

    private static Control BuildFontsSection(
        IReadOnlyList<SettingDefinition> defs,
        TabItem tab,
        MainWindowViewModel viewModel)
    {
        var fontDefs = defs.Where(d => d.Kind == SettingControlKind.Choice).ToList();
        var (fontPanel, fontBindings) = FontFamilyPanelBuilder.Build(fontDefs);
        viewModel.WireAppFontFamilies(tab, fontBindings);
        return fontPanel;
    }

    private static Control BuildTextSizesSection(
        IReadOnlyList<SettingDefinition> defs,
        TabItem tab,
        MainWindowViewModel viewModel)
    {
        var fontDefs = defs.Where(d => d.Kind == SettingControlKind.Numeric).ToList();
        var (fontRow, fontBindings) = FontSizePanelBuilder.Build(fontDefs);
        viewModel.WireAppFontSizes(tab, fontBindings);
        return fontRow;
    }

    private static Control BuildLoggingSection(
        IReadOnlyList<SettingDefinition> defs,
        TabItem tab,
        MainWindowViewModel viewModel,
        Dictionary<string, CheckBox> checkboxes)
    {
        var stack = new StackPanel { Spacing = UiMetrics.AppSettingsControlGapPx };

        foreach (var def in defs)
        {
            if (def.Token == "ui_enable_debug_logging")
            {
                stack.Children.Add(BuildBooleanSetting(def, tab, viewModel, checkboxes,
                    () => viewModel.EnableDebugLogging,
                    v => viewModel.EnableDebugLogging = v));
            }
        }

        return stack;
    }

    private static Control BuildUpdatesSection(
        IReadOnlyList<SettingDefinition> defs,
        TabItem tab,
        MainWindowViewModel viewModel,
        Dictionary<string, CheckBox> checkboxes)
    {
        var stack = new StackPanel { Spacing = UiMetrics.AppSettingsControlGapPx };
        CheckBox? autoInstall = null;

        foreach (var def in defs)
        {
            switch (def.Token)
            {
                case "ui_check_for_updates":
                    stack.Children.Add(BuildBooleanSetting(def, tab, viewModel, checkboxes,
                        () => viewModel.CheckForUpdates,
                        v => viewModel.CheckForUpdates = v));
                    break;
                case "ui_auto_install_updates":
                    autoInstall = BuildBooleanSetting(def, tab, viewModel, checkboxes,
                        () => viewModel.AutomaticallyInstallUpdates,
                        v => viewModel.AutomaticallyInstallUpdates = v);
                    autoInstall.IsEnabled = viewModel.CheckForUpdates;
                    stack.Children.Add(autoInstall);
                    break;
            }
        }

        if (autoInstall is not null)
        {
            viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.CheckForUpdates))
                    autoInstall.IsEnabled = viewModel.CheckForUpdates;
            };
        }

        return stack;
    }

    private static CheckBox BuildBooleanSetting(
        SettingDefinition def,
        TabItem tab,
        MainWindowViewModel viewModel,
        Dictionary<string, CheckBox> checkboxes,
        Func<bool> getValue,
        Action<bool> setValue)
    {
        var check = new CheckBox
        {
            Content = def.Label,
            IsChecked = getValue(),
        };
        check.IsCheckedChanged += (_, _) => setValue(check.IsChecked == true);
        ToolTip.SetTip(check, SettingTooltipHelper.Build(def));
        viewModel.RegisterNavTarget(tab, def.Token, check);
        checkboxes[def.Token] = check;
        return check;
    }
}
