namespace rEFIndConfigEditor.Models;

public readonly record struct FontFamilyOption(string Value, string Label);

public static class UiFontFamilies
{
    public const string SegoeUi = "Segoe UI";
    public const string SegoeUiStack = "Segoe UI, Segoe UI Variable";
    public const string Consolas = "Consolas";
    public const string Menlo = "Menlo";
    public const string MenloStack = "Menlo, SF Mono, Monaco";
    public const string Cantarell = "Cantarell";
    public const string CantarellStack = "Cantarell, sans-serif";
    public const string LiberationSans = "Liberation Sans";
    public const string LiberationMono = "Liberation Mono";

    public static string DefaultMain =>
        OperatingSystem.IsMacOS() ? "Tahoma"
        : OperatingSystem.IsLinux() ? LiberationSans
        : SegoeUi;

    public static string DefaultMono =>
        OperatingSystem.IsMacOS() ? Menlo
        : OperatingSystem.IsLinux() ? LiberationMono
        : Consolas;

    private static readonly FontFamilyOption[] WindowsMainChoices =
    [
        new(SegoeUi, SegoeUi),
        new("Arial", "Arial"),
        new("Calibri", "Calibri"),
        new("Tahoma", "Tahoma"),
        new("Verdana", "Verdana"),
        new("Georgia", "Georgia"),
        new("Times New Roman", "Times New Roman"),
    ];

    private static readonly FontFamilyOption[] WindowsMonoChoices =
    [
        new(Consolas, Consolas),
        new("Cascadia Mono", "Cascadia Mono"),
        new("Cascadia Code", "Cascadia Code"),
        new("Courier New", "Courier New"),
        new("Lucida Console", "Lucida Console"),
    ];

    private static readonly FontFamilyOption[] MacMonoChoices =
    [
        new(Menlo, Menlo),
        new("SF Mono", "SF Mono"),
        new("Monaco", "Monaco"),
        new(Consolas, Consolas),
        new("Cascadia Mono", "Cascadia Mono"),
        new("Cascadia Code", "Cascadia Code"),
        new("Courier New", "Courier New"),
    ];

    private static readonly FontFamilyOption[] LinuxMainChoices =
    [
        new(LiberationSans, LiberationSans),
        new(Cantarell, Cantarell),
        new("Adwaita Sans", "Adwaita Sans"),
        new("Noto Sans", "Noto Sans"),
        new("Droid Sans", "Droid Sans"),
    ];

    private static readonly FontFamilyOption[] LinuxMonoChoices =
    [
        new(LiberationMono, LiberationMono),
        new("Adwaita Mono", "Adwaita Mono"),
        new("Noto Sans Mono", "Noto Sans Mono"),
    ];

    public static FontFamilyOption[] MainChoices =>
        OperatingSystem.IsLinux() ? LinuxMainChoices : WindowsMainChoices;

    public static FontFamilyOption[] MonoChoices =>
        OperatingSystem.IsMacOS() ? MacMonoChoices
        : OperatingSystem.IsLinux() ? LinuxMonoChoices
        : WindowsMonoChoices;

    public static string[] MainValues => MainChoices.Select(c => c.Value).ToArray();
    public static string[] MainLabels => MainChoices.Select(c => c.Label).ToArray();
    public static string[] MonoValues => MonoChoices.Select(c => c.Value).ToArray();
    public static string[] MonoLabels => MonoChoices.Select(c => c.Label).ToArray();

    public static string NormalizeMain(string? value) => Normalize(value, MainChoices, DefaultMain);

    public static string NormalizeMono(string? value)
    {
        if (OperatingSystem.IsMacOS()
            && string.Equals(value, Consolas, StringComparison.OrdinalIgnoreCase))
            return DefaultMono;

        return Normalize(value, MonoChoices, DefaultMono);
    }

    public static int IndexOfMain(string? value) => IndexOf(value, MainChoices, DefaultMain);

    public static int IndexOfMono(string? value) => IndexOf(value, MonoChoices, DefaultMono);

    public static string ResolveMainStack(string name)
    {
        if (string.Equals(name, SegoeUi, StringComparison.OrdinalIgnoreCase))
            return SegoeUiStack;

        if (OperatingSystem.IsLinux()
            && string.Equals(name, Cantarell, StringComparison.OrdinalIgnoreCase))
            return CantarellStack;

        return name;
    }

    public static string ResolveMonoStack(string name) =>
        string.Equals(name, Menlo, StringComparison.OrdinalIgnoreCase)
            ? MenloStack
            : name;

    private static string Normalize(string? value, FontFamilyOption[] choices, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        foreach (var choice in choices)
        {
            if (string.Equals(choice.Value, value, StringComparison.OrdinalIgnoreCase))
                return choice.Value;
        }

        return fallback;
    }

    private static int IndexOf(string? value, FontFamilyOption[] choices, string fallback)
    {
        var normalized = Normalize(value, choices, fallback);
        for (var i = 0; i < choices.Length; i++)
        {
            if (string.Equals(choices[i].Value, normalized, StringComparison.Ordinal))
                return i;
        }

        return 0;
    }
}
