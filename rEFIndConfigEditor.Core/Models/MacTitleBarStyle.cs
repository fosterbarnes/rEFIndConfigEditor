namespace rEFIndConfigEditor.Models;

public enum MacTitleBarStyle
{
    Combined,
    Separate,
}

public static class MacTitleBarStyleExtensions
{
    public static bool UsesUnifiedChrome(this MacTitleBarStyle style) =>
        style == MacTitleBarStyle.Combined;
}
