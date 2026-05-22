namespace rEFIndConfigEditor.Models;

public sealed class UiPreferences
{
    public UiThemeKind Theme { get; set; } = UiThemeKind.System;
    public int? WindowX { get; set; }
    public int? WindowY { get; set; }
    public int? WindowWidth { get; set; }
    public int? WindowHeight { get; set; }
    public bool WindowMaximized { get; set; }
    public bool AutoLoadLastConfOnLaunch { get; set; } = true;
    public bool RememberLastSelectedTab { get; set; }
    public int? LastSelectedTabIndex { get; set; }
    public string? LastConfPath { get; set; }
}
