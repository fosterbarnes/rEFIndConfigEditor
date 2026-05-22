namespace rEFIndConfigEditor.UI;

internal static class StanzaFieldHelp
{
    internal sealed record FieldInfo(string Label, string Token, string Help);

    private static readonly Dictionary<string, FieldInfo> Fields = new(StringComparer.OrdinalIgnoreCase)
    {
        ["menuentry"] = new("Menu title", "menuentry",
            "Name shown next to the icon in the boot menu. Use quotes if the title contains spaces."),
        ["volume"] = new("Boot from this disk", "volume",
            "Disk or partition to use for loader, icon, and initrd paths. Enter a volume label, partition label, or GUID."),
        ["loader"] = new("Boot loader file", "loader",
            "Path to the .efi boot loader on the ESP, relative to the volume root. Use / or \\ as separators."),
        ["initrd"] = new("Initial RAM disk", "initrd",
            "Path to the initrd for Linux kernels with EFI stub support. Must be on the same volume as the kernel."),
        ["firmware_bootnum"] = new("Firmware boot number", "firmware_bootnum",
            "Boot a firmware-defined option by its four-digit hex boot number instead of using loader."),
        ["icon"] = new("Menu icon", "icon",
            "Custom icon file for this entry. Path is from the volume root; if omitted, rEFInd picks a default icon. " +
            "Use Browse or Choose icon (ostype does not set the menu icon)."),
        ["ostype"] = new("Operating system type", "ostype",
            "Controls Insert-key submenu options. Case-sensitive: MacOS, Linux, ELILO, Windows, or XOM."),
        ["graphics"] = new("Graphical boot handoff", "graphics",
            "Whether rEFInd uses a graphical handoff when launching this entry. Choose on or off."),
        ["options"] = new("Boot loader options", "options",
            "Options passed to the boot loader or kernel, such as root= and ro for Linux."),
        ["add_options"] = new("Extra boot options", "add_options",
            "Options added on top of the main entry's options line. Submenu entries only."),
        ["disabled"] = new("Disable this entry", "disabled",
            "Keeps the entry in refind.conf but hides it from the menu until you remove this flag."),
        ["submenuentry"] = new("Submenu title", "submenuentry",
            "Name for a choice inside a boot entry's submenu. Use quotes if the title contains spaces.")
    };

    public static FieldInfo Get(string token) =>
        Fields.TryGetValue(token, out var info) ? info : new FieldInfo(token, token, "");

    public static string Tooltip(string token)
    {
        var info = Get(token);
        if (string.IsNullOrEmpty(info.Help))
            return $"Config token: {info.Token}";
        return $"{info.Help}\n\nConfig token: {info.Token}";
    }
}
