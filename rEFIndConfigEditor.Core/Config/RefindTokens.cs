namespace rEFIndConfigEditor.Config;

public static class RefindTokens
{
    public static string Canonicalize(string token)
    {
        var lowered = token.ToLowerInvariant();
        var t = lowered switch
        {
            "don't_scan_volumes" => "dont_scan_volumes",
            "don't_scan_dirs" => "dont_scan_dirs",
            "don't_scan_files" => "dont_scan_files",
            "don't_scan_tools" => "dont_scan_tools",
            "don't_scan_firmware" => "dont_scan_firmware",
            _ => lowered
        };

        if (GlobalTokens.Contains(t) || StanzaTokens.Contains(t))
            return t;
        return token;
    }

    public static bool TokenEquals(string a, string b) =>
        string.Equals(Canonicalize(a), Canonicalize(b), StringComparison.Ordinal);

    public static readonly HashSet<string> GlobalTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "timeout", "shutdown_after_timeout", "log_level", "use_nvram", "screensaver", "hideui",
        "icons_dir", "banner", "banner_scale", "big_icon_size", "small_icon_size",
        "selection_big", "selection_small", "showtools", "font", "textonly", "textmode",
        "resolution", "enable_touch", "enable_mouse", "mouse_size", "mouse_speed",
        "use_graphics_for", "scan_driver_dirs", "scanfor", "follow_symlinks",
        "uefi_deep_legacy_scan", "scan_delay", "also_scan_dirs", "dont_scan_volumes",
        "dont_scan_dirs", "dont_scan_files", "also_scan_tool_dirs", "dont_scan_tools",
        "dont_scan_firmware", "windows_recovery_files", "scan_all_linux_kernels", "support_gzipped_loaders",
        "fold_linux_kernels", "linux_prefixes", "extra_kernel_version_strings",
        "write_systemd_vars", "max_tags", "default_selection", "enable_and_lock_vmx",
        "spoof_osx_version", "csr_values", "include"
    };

    public static readonly HashSet<string> StanzaTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "volume", "loader", "initrd", "firmware_bootnum", "icon", "ostype", "graphics",
        "options", "disabled", "add_options"
    };

    public enum ListKind { None, FolderList, FileList }

    private static readonly Dictionary<string, ListKind> ListKindByToken = new(StringComparer.OrdinalIgnoreCase)
    {
        ["scan_driver_dirs"] = ListKind.FolderList,
        ["also_scan_dirs"] = ListKind.FolderList,
        ["also_scan_tool_dirs"] = ListKind.FolderList,
        ["dont_scan_dirs"] = ListKind.FolderList,
        ["dont_scan_files"] = ListKind.FileList,
        ["dont_scan_tools"] = ListKind.FileList
    };

    public static ListKind GetListKind(string token) =>
        ListKindByToken.TryGetValue(token, out var k) ? k : ListKind.None;

    public static bool IsFolderListToken(string token) => GetListKind(token) == ListKind.FolderList;

    public static bool IsFileListToken(string token) => GetListKind(token) == ListKind.FileList;

    private static readonly HashSet<string> CommaListTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "scanfor", "hideui", "showtools", "use_graphics_for",
        "dont_scan_volumes", "dont_scan_firmware", "windows_recovery_files",
        "linux_prefixes", "extra_kernel_version_strings", "csr_values"
    };

    public static bool AcceptsCommaSeparatedValues(string token)
    {
        token = Canonicalize(token);
        return CommaListTokens.Contains(token)
            || GetListKind(token) != ListKind.None;
    }
}
