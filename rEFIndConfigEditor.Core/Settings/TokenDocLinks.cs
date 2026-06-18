namespace rEFIndConfigEditor.Settings;

public static class TokenDocLinks
{
    public static string? UrlFor(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || !IsDocumentedToken(token))
            return null;

        return $"{AppBranding.WikiUrl}#{Canonicalize(token)}";
    }

    public static string Canonicalize(string token)
    {
        var lowered = token.ToLowerInvariant();
        return lowered switch
        {
            "don't_scan_volumes" => "dont_scan_volumes",
            "don't_scan_dirs" => "dont_scan_dirs",
            "don't_scan_files" => "dont_scan_files",
            "don't_scan_tools" => "dont_scan_tools",
            "don't_scan_firmware" => "dont_scan_firmware",
            _ => lowered
        };
    }

    private static bool IsDocumentedToken(string token)
    {
        if (SettingCatalog.All.Any(d => string.Equals(d.Token, token, StringComparison.OrdinalIgnoreCase)))
            return true;

        return Canonicalize(token) switch
        {
            "menuentry" or "submenuentry" or "volume" or "loader" or "initrd" or "icon"
                or "firmware_bootnum" or "ostype" or "graphics" or "options" or "add_options"
                or "disabled" => true,
            _ => false
        };
    }
}
