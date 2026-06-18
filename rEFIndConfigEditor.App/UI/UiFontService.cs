using Avalonia;
using Avalonia.Media;
using rEFIndConfigEditor.Models;
namespace rEFIndConfigEditor.UI;

internal static class UiFontService
{
    public static void Apply(Application app, UiPreferences prefs)
    {
        var main = (double)Clamp(prefs.MainFontSize);
        var resources = app.Resources;
        resources[UiFontKeys.Body] = main;
        resources[UiFontKeys.Menu] = main;
        resources[UiFontKeys.Status] = main;
        resources[UiFontKeys.Section] = main;
        resources[UiFontKeys.Tab] = (double)Clamp(prefs.TabFontSize);
        resources[UiFontKeys.Token] = (double)Clamp(prefs.TokenFontSize);
        resources[UiFontKeys.MainFamily] = CreateMainFamily(prefs.MainFontFamily);
        resources[UiFontKeys.MonoFamily] = CreateMonoFamily(prefs.MonoFontFamily);
    }

    public static FontFamily CreateMainFamily(string? name) =>
        new(UiFontFamilies.ResolveMainStack(UiFontFamilies.NormalizeMain(name)));

    public static FontFamily CreateMonoFamily(string? name) =>
        new($"{UiFontFamilies.ResolveMonoStack(UiFontFamilies.NormalizeMono(name))}, monospace");

    public static FontFamily CreatePreviewFamily(string name, bool mono) =>
        mono
            ? new FontFamily($"{UiFontFamilies.ResolveMonoStack(name)}, monospace")
            : new FontFamily(UiFontFamilies.ResolveMainStack(name));

    public static int Clamp(int size) => UiFontDefaults.Clamp(size);
}
