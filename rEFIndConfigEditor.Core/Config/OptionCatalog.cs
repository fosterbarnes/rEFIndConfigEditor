namespace rEFIndConfigEditor.Config;

public enum OptionCategory
{
    General,
    Display,
    Theme,
    Input,
    Scanning,
    Advanced
}

public enum OptionControlKind
{
    Numeric,
    Boolean,
    Text,
    MultiSelect,
    Choice
}

public sealed class OptionDefinition
{
    public required string Token { get; init; }
    public required string Label { get; init; }
    public required OptionCategory Category { get; init; }
    public required OptionControlKind Kind { get; init; }
    public string? HelpText { get; init; }
    public string[]? Choices { get; init; }
    public string[]? ChoiceLabels { get; init; }
    public decimal NumericMin { get; init; }
    public decimal NumericMax { get; init; } = 99999;
    public decimal NumericDefault { get; init; }
}

public static class OptionCatalog
{
    public static IReadOnlyList<OptionDefinition> All { get; } = Build();

    private static readonly Dictionary<OptionCategory, IReadOnlyList<OptionDefinition>> ByCategory =
        All.GroupBy(d => d.Category).ToDictionary(g => g.Key, g => (IReadOnlyList<OptionDefinition>)g.ToList());

    public static IReadOnlyList<OptionDefinition> ForCategory(OptionCategory category) =>
        ByCategory.TryGetValue(category, out var list) ? list : [];

    private static OptionDefinition D(
        string token,
        string label,
        OptionCategory cat,
        OptionControlKind kind,
        string? help = null,
        string[]? choices = null,
        string[]? choiceLabels = null,
        decimal numMin = 0,
        decimal numMax = 99999,
        decimal numDefault = 0) =>
        new()
        {
            Token = token,
            Label = label,
            Category = cat,
            Kind = kind,
            HelpText = help,
            Choices = choices,
            ChoiceLabels = choiceLabels,
            NumericMin = numMin,
            NumericMax = numMax,
            NumericDefault = numDefault
        };

    private static OptionDefinition[] Build() =>
    [
        D("timeout", "Boot menu countdown", OptionCategory.General, OptionControlKind.Numeric,
            "How long rEFInd waits before booting the default OS, in seconds. Use 0 to wait until you pick one. Use -1 to boot immediately unless you press a key.",
            numMin: -1, numMax: 3600, numDefault: 20),
        D("shutdown_after_timeout", "Shut down when countdown ends", OptionCategory.General, OptionControlKind.Boolean,
            "When enabled, rEFInd shuts down the computer instead of booting when the countdown expires. Many older EFIs cannot shut down cleanly and may reboot or hang."),
        D("default_selection", "Default boot choice", OptionCategory.General, OptionControlKind.Text,
            "Which entry boots automatically: a menu number, part of a title, or + for the last OS you booted."),
        D("screensaver", "Screensaver (seconds)", OptionCategory.General, OptionControlKind.Numeric,
            "Seconds of inactivity before the screen blanks to prevent burn-in. Use 0 to disable. Use -1 to keep the screen blank until the countdown ends or you press a key.",
            numMin: -1, numMax: 86400, numDefault: 0),
        D("max_tags", "1st row limit", OptionCategory.General, OptionControlKind.Numeric,
            "Limits how many OS icons appear on the first row. Use 0 for no limit.",
            numMin: 0, numMax: 99, numDefault: 0),
        D("log_level", "Log detail", OptionCategory.General, OptionControlKind.Choice,
            "How much detail rEFInd writes to refind.log on the ESP. Keep off for normal use; higher levels slow boot. Level 1 shows scanned volumes and boot loaders; 3 adds directory scan detail; 4 is full filesystem debug.",
            ["0", "1", "2", "3", "4"],
            ["Off — no logging", "Basic — volumes and boot loaders", "Detailed — scan paths and handles",
                "Verbose — per-directory scanning", "Maximum — full filesystem debug (slow)"]),

        D("hideui", "Remove elements", OptionCategory.Display, OptionControlKind.MultiSelect,
            "Pick UI pieces to remove from the boot menu.",
            ["banner", "label", "singleuser", "safemode", "hwtest", "arrows", "hints", "editor", "badges", "all"],
            ["Banner graphic", "Tag labels and countdown", "macOS single-user mode", "macOS safe mode",
                "Macintosh hardware test", "Scroll arrows", "Keypress hints", "Options editor", "Device-type badges", "All of the above"]),
        D("textonly", "Text-only boot menu", OptionCategory.Display, OptionControlKind.Boolean,
            "Shows a plain text menu instead of icons and graphics. Also used automatically when no icons directory is found."),
        D("textmode", "Text mode resolution", OptionCategory.Display, OptionControlKind.Numeric,
            "Sets the text-mode screen resolution. Use 1024 to leave the current resolution unchanged.",
            numMin: 0, numMax: 9999, numDefault: 0),
        D("resolution", "Graphics resolution", OptionCategory.Display, OptionControlKind.Choice,
            "Screen size for graphics mode. rEFInd stores width and height as two values, or max for the largest available mode.",
            ["0 0", "1280 720", "1366 768", "1600 900", "1920 1080", "1920 1200", "2560 1080", "2560 1440", "3440 1440", "3840 2160", "max"],
            ["System default", "1280×720", "1366×768", "1600×900", "1920×1080", "1920×1200", "2560×1080", "2560×1440", "3440×1440", "3840×2160", "Maximum available"]),
        D("use_graphics_for", "Alternative graphics handoff", OptionCategory.Display, OptionControlKind.MultiSelect,
            "When launching these OS types, clear the screen with a simple method instead of the full graphics handoff.",
            ["osx", "linux", "elilo", "grub", "windows"],
            ["macOS", "Linux", "ELILO", "GRUB", "Windows"]),
        D("showtools", "Manually select visible tools", OptionCategory.Display, OptionControlKind.MultiSelect,
            "Which utility icons appear on the second row. Order here sets left-to-right order in the menu.",
            ["shell", "memtest", "memtest86", "gdisk", "gptsync", "install", "bootorder", "apple_recovery", "csr_rotate",
                "mok_tool", "fwupdate", "netboot", "about", "hidden_tags", "exit", "shutdown", "reboot",
                "firmware", "windows_recovery"],
            ["EFI shell", "Memtest86", "Memtest86 (alias)", "GPT fdisk (gdisk)", "Hybrid MBR tool (gptsync)",
                "Install rEFInd to another ESP", "EFI boot order editor", "macOS Recovery HD", "Rotate SIP (csr_rotate)",
                "Machine Owner Key tool", "Firmware update tool", "Network boot (deprecated)", "About / system info",
                "Restore hidden tags", "Return to previous boot manager", "Shut down", "Reboot",
                "Reboot to firmware setup", "Windows recovery"]),

        D("banner", "Custom banner image", OptionCategory.Theme, OptionControlKind.Text,
            "Image file that replaces the default rEFInd banner. Supports ICNS, BMP, PNG, or JPEG in the rEFInd directory."),
        D("banner_scale", "Banner scaling", OptionCategory.Theme, OptionControlKind.Choice,
            "Whether the banner is shown at its native size or stretched to fill the screen.",
            ["noscale", "fillscreen"]),
        D("icons_dir", "Custom icons folder", OptionCategory.Theme, OptionControlKind.Text,
            "Subdirectory (under rEFInd) containing custom OS and tool icons. Missing icons fall back to the default icons folder."),
        D("font", "Custom menu font (PNG)", OptionCategory.Theme, OptionControlKind.Text,
            "PNG font file in the rEFInd directory used for text in the boot menu."),
        D("big_icon_size", "OS icon size", OptionCategory.Theme, OptionControlKind.Numeric,
            "Pixel size for OS icons on the first row. Icons are scaled if the file does not match this size.",
            numMin: 32, numMax: 512, numDefault: 128),
        D("small_icon_size", "Tool icon size", OptionCategory.Theme, OptionControlKind.Numeric,
            "Pixel size for tool icons on the second row.",
            numMin: 32, numMax: 256, numDefault: 48),
        D("selection_big", "OS selection highlight image", OptionCategory.Theme, OptionControlKind.Text,
            "Image file that highlights the selected OS icon. Best at 144x144 in the rEFInd directory."),
        D("selection_small", "Tool selection highlight image", OptionCategory.Theme, OptionControlKind.Text,
            "Image file that highlights the selected tool icon. Best at 64x64 in the rEFInd directory."),

        D("enable_touch", "Touch screen support", OptionCategory.Input, OptionControlKind.Boolean,
            "Enables touch input for selecting boot entries and tools."),
        D("enable_mouse", "Mouse support", OptionCategory.Input, OptionControlKind.Boolean,
            "Shows a mouse pointer and enables clicking to select entries."),
        D("mouse_size", "Mouse pointer size", OptionCategory.Input, OptionControlKind.Numeric,
            "Size of the on-screen mouse pointer in pixels.",
            numMin: 8, numMax: 64, numDefault: 16),
        D("mouse_speed", "Mouse tracking speed", OptionCategory.Input, OptionControlKind.Numeric,
            "How fast the pointer moves relative to physical mouse movement.",
            numMin: 1, numMax: 10, numDefault: 1),

        D("scanfor", "Devices/entities to scan", OptionCategory.Scanning, OptionControlKind.MultiSelect,
            "Which devices and boot methods rEFInd searches. Order here also sets menu order for scan types.",
            ["internal", "external", "optical", "netboot", "hdbios", "biosexternal", "cd", "manual", "firmware"],
            ["Internal disks (EFI)", "External disks (EFI)", "Optical drives (EFI)", "Network boot (PXE)",
                "Legacy BIOS on public disks", "Legacy BIOS on external disks", "Legacy BIOS on optical drives",
                "Manual entries only", "Firmware boot manager entries"]),
        D("scan_delay", "Scan delay (seconds)", OptionCategory.Scanning, OptionControlKind.Numeric,
            "Seconds to wait before scanning disks. Can help slow or removable drives show up reliably.",
            numMin: 0, numMax: 60, numDefault: 0),
        D("follow_symlinks", "Follow symbolic links", OptionCategory.Scanning, OptionControlKind.Boolean,
            "When scanning, follow symlinks to find boot loaders and tools."),
        D("uefi_deep_legacy_scan", "Deep legacy BIOS scan on PCs", OptionCategory.Scanning, OptionControlKind.Boolean,
            "Scans all legacy boot devices instead of only those listed in firmware NVRAM. Helps USB disks show up on UEFI PCs."),
        D("scan_driver_dirs", "Extra EFI driver folders", OptionCategory.Scanning, OptionControlKind.Text,
            "Comma-separated directories (relative to each volume root) where rEFInd looks for EFI filesystem drivers."),
        D("also_scan_dirs", "Extra boot loader folders", OptionCategory.Scanning, OptionControlKind.Text,
            "Comma-separated directories to scan for EFI boot loaders. Prefix with + to add to the default boot folder."),
        D("also_scan_tool_dirs", "Extra tool folders", OptionCategory.Scanning, OptionControlKind.Text,
            "Comma-separated directories to scan for external tools such as shells and Memtest86."),
        D("dont_scan_volumes", "Volumes to skip", OptionCategory.Scanning, OptionControlKind.Text,
            "Comma-separated volume labels or GUIDs to exclude from scanning. Default hides Lenovo recovery ESP (LRS_ESP)."),
        D("dont_scan_dirs", "Folders to skip", OptionCategory.Scanning, OptionControlKind.Text,
            "Comma-separated directories to exclude when scanning for boot loaders."),
        D("dont_scan_files", "Files to skip", OptionCategory.Scanning, OptionControlKind.Text,
            "Comma-separated filenames to exclude from the OS list."),
        D("dont_scan_tools", "Tools to exclude", OptionCategory.Scanning, OptionControlKind.Text,
            "Comma-separated tool filenames to exclude from the tools row."),
        D("dont_scan_firmware", "Skip firmware boot entries", OptionCategory.Scanning, OptionControlKind.Text,
            "Comma-separated text that matches firmware boot option names to hide when firmware scanning is enabled."),
        D("windows_recovery_files", "Windows recovery paths", OptionCategory.Scanning, OptionControlKind.Text,
            "Comma-separated paths recognized as Windows recovery tools for the windows_recovery tool icon."),

        D("scan_all_linux_kernels", "Scan raw Linux kernels", OptionCategory.Advanced, OptionControlKind.Boolean,
            "Detect vmlinuz and bzImage files even when they do not use a .efi extension."),
        D("fold_linux_kernels", "Group Linux kernels", OptionCategory.Advanced, OptionControlKind.Boolean,
            "Shows one icon per directory when multiple Linux kernels are found, with a submenu to pick the kernel."),
        D("linux_prefixes", "Linux kernel name prefixes", OptionCategory.Advanced, OptionControlKind.Text,
            "Comma-separated filename prefixes treated as Linux kernels (default includes vmlinuz, bzImage, etc.)."),
        D("extra_kernel_version_strings", "Extra kernel version strings", OptionCategory.Advanced, OptionControlKind.Text,
            "Comma-separated version strings used when matching initrd files to kernel filenames."),
        D("support_gzipped_loaders", "Allow Gzip-compressed EFI loaders", OptionCategory.Advanced, OptionControlKind.Boolean,
            "Allows booting from gzip-compressed EFI loader files."),
        D("write_systemd_vars", "Pass boot disk info to systemd", OptionCategory.Advanced, OptionControlKind.Boolean,
            "Writes LoaderDevicePartUUID so systemd can identify the boot partition."),

        D("use_nvram", "Store settings in NVRAM", OptionCategory.Advanced, OptionControlKind.Boolean,
            "Saves rEFInd variables in firmware NVRAM instead of on disk. NVRAM ties settings to one machine; disk storage travels with rEFInd."),
        D("enable_and_lock_vmx", "Enable and lock Intel VT-x", OptionCategory.Advanced, OptionControlKind.Boolean,
            "Turns on VMX (needed for Hyper-V) and locks it so operating systems cannot disable it. Use with care."),
        D("spoof_osx_version", "Spoof macOS version", OptionCategory.Advanced, OptionControlKind.Text,
            "Makes the firmware think a specific macOS version is installed (e.g. 10.9). Used for older Mac compatibility tricks."),
        D("csr_values", "SIP values for csr_rotate", OptionCategory.Advanced, OptionControlKind.Text,
            "Comma-separated hex values cycled by the csr_rotate tool when changing System Integrity Protection."),
    ];
}
