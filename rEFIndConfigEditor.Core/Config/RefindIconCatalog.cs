namespace rEFIndConfigEditor.Config;

public static class RefindIconCatalog
{
    public sealed record Entry(string Label, string FileName);

    public static IReadOnlyList<Entry> Standard { get; } =
    [
        new("Windows", "os_win.png"),
        new("Linux (generic)", "os_linux.png"),
        new("macOS", "os_mac.png"),
        new("Arch Linux", "os_arch.png"),
        new("Debian", "os_debian.png"),
        new("Ubuntu", "os_ubuntu.png"),
        new("Fedora", "os_fedora.png"),
        new("Red Hat", "os_redhat.png"),
        new("CentOS", "os_centos.png"),
        new("openSUSE", "os_opensuse.png"),
        new("Gentoo", "os_gentoo.png"),
        new("Manjaro", "os_manjaro.png"),
        new("Linux Mint", "os_mint.png"),
        new("Pop!_OS", "os_pop.png"),
        new("elementary OS", "os_elementary.png"),
        new("Kali Linux", "os_kali.png"),
        new("NixOS", "os_nixos.png"),
        new("Void Linux", "os_void.png"),
        new("FreeBSD", "os_freebsd.png"),
        new("NetBSD", "os_netbsd.png"),
        new("OpenBSD", "os_openbsd.png"),
        new("Chrome OS", "os_chrome.png"),
        new("Clover", "os_clover.png"),
        new("Slackware", "os_slackware.png"),
        new("Mageia", "os_mageia.png"),
        new("Zorin OS", "os_zorin.png"),
        new("Antergos", "os_antergos.png"),
        new("Deepin", "os_deepin.png"),
        new("EndeavourOS", "os_endeavouros.png"),
        new("Garuda", "os_garuda.png"),
        new("MX Linux", "os_mx.png"),
        new("Solus", "os_solus.png"),
        new("SteamOS", "os_steamos.png"),
        new("Android", "os_android.png"),
        new("Chromebook", "os_chromebook.png"),
        new("Hardware / generic", "os_hw.png"),
        new("EFI shell", "tool_shell.png"),
        new("MOK manager", "tool_mok.png"),
        new("Recovery", "tool_rescue.png"),
    ];
}
