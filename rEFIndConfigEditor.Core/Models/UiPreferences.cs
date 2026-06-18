namespace rEFIndConfigEditor.Models;

public sealed class UiPreferences
{
    public UiThemeKind Theme { get; set; } = UiThemeKind.System;
    public int? WindowX { get; set; }
    public int? WindowY { get; set; }
    public int? WindowWidth { get; set; }
    public int? WindowHeight { get; set; }
    public bool WindowMaximized { get; set; }
    public bool RememberLastSelectedTab { get; set; }
    public bool AutoLoadLastConfOnLaunch { get; set; } = true;
    public string? LastConfPath { get; set; }
    public string? LastSelectedTabKey { get; set; }
    public bool CheckForUpdates { get; set; } = true;
    public bool AutomaticallyInstallUpdates { get; set; }
    public bool EnableDebugLogging { get; set; }
    public int MainFontSize { get; set; } = UiFontDefaults.Main;
    public int TabFontSize { get; set; } = UiFontDefaults.Tab;
    public int TokenFontSize { get; set; } = UiFontDefaults.Token;
    public string MainFontFamily { get; set; } = UiFontFamilies.DefaultMain;
    public string MonoFontFamily { get; set; } = UiFontFamilies.DefaultMono;
    public MacTitleBarStyle MacTitleBarStyle { get; set; } = MacTitleBarStyle.Separate;

    public UiPreferences Clone() => new()
    {
        Theme = Theme,
        WindowX = WindowX,
        WindowY = WindowY,
        WindowWidth = WindowWidth,
        WindowHeight = WindowHeight,
        WindowMaximized = WindowMaximized,
        RememberLastSelectedTab = RememberLastSelectedTab,
        AutoLoadLastConfOnLaunch = AutoLoadLastConfOnLaunch,
        LastConfPath = LastConfPath,
        LastSelectedTabKey = LastSelectedTabKey,
        CheckForUpdates = CheckForUpdates,
        AutomaticallyInstallUpdates = AutomaticallyInstallUpdates,
        EnableDebugLogging = EnableDebugLogging,
        MainFontSize = MainFontSize,
        TabFontSize = TabFontSize,
        TokenFontSize = TokenFontSize,
        MainFontFamily = MainFontFamily,
        MonoFontFamily = MonoFontFamily,
        MacTitleBarStyle = MacTitleBarStyle,
    };
}
