# rEFIndConfigEditor

<img src="./.resources/icon/refind256.png" align="left" width="160"> A gui config editor for [rEFInd](https://www.rodsbooks.com/refind/), a UEFI boot manager compatible with Linux, MacOS & Windows. 

Create a config with a gui with friendly names, tooltips, and references for each token. Includes some extras like theme management and a theme browser. Covers every setting listed on the [config file instruction page](https://www.rodsbooks.com/refind/configfile.html), with links for each. 

**Wiki:** [Tokens](https://github.com/fosterbarnes/rEFIndConfigEditor/wiki#refind-configuration-tokens) · [Themes](https://github.com/fosterbarnes/rEFIndConfigEditor/wiki#refind-supported-themes)

<!-- Quick Reference --
version = 1.1.0

x64Installer = https://github.com/fosterbarnes/rEFIndConfigEditor/releases/download/v1.1.0/rEFIndConfigEditor_v1.1.0_windows-x64.exe

x64Portable = https://github.com/fosterbarnes/rEFIndConfigEditor/releases/download/v1.1.0/rEFIndConfigEditor_v1.1.0_windows-x64.zip

x86Installer = https://github.com/fosterbarnes/rEFIndConfigEditor/releases/download/v1.1.0/rEFIndConfigEditor_v1.1.0_windows-x86.exe

x86Portable = https://github.com/fosterbarnes/rEFIndConfigEditor/releases/download/v1.1.0/rEFIndConfigEditor_v1.1.0_windows-x86.zip

ARM64Installer = https://github.com/fosterbarnes/rEFIndConfigEditor/releases/download/v1.1.0/rEFIndConfigEditor_v1.1.0_windows-arm64.exe

ARM64Portable = https://github.com/fosterbarnes/rEFIndConfigEditor/releases/download/v1.1.0/rEFIndConfigEditor_v1.1.0_windows-arm64.zip

osxX64Portable = https://github.com/fosterbarnes/rEFIndConfigEditor/releases/download/v1.1.0/rEFIndConfigEditor_v1.1.0_macOS-intel.zip

osxArm64Portable = https://github.com/fosterbarnes/rEFIndConfigEditor/releases/download/v1.1.0/rEFIndConfigEditor_v1.1.0_macOS-arm.zip

linuxAmd64Deb = https://github.com/fosterbarnes/rEFIndConfigEditor/releases/download/v1.1.0/rEFIndConfigEditor_v1.1.0_debian-amd64.deb

linuxArm64Deb = https://github.com/fosterbarnes/rEFIndConfigEditor/releases/download/v1.1.0/rEFIndConfigEditor_v1.1.0_debian-arm64.deb

linuxAmd64Rpm = https://github.com/fosterbarnes/rEFIndConfigEditor/releases/download/v1.1.0/rEFIndConfigEditor_v1.1.0_fedora-amd64.rpm

linuxArm64Rpm = https://github.com/fosterbarnes/rEFIndConfigEditor/releases/download/v1.1.0/rEFIndConfigEditor_v1.1.0_fedora-arm64.rpm
-->

<br><br>

| <h3>General</h3> |
|:---:|
| ![General](./.resources/scr/1.png) |

## Downloads

### Windows

<table border="0">
<tbody>
<tr>
<td align="center" valign="top"><a href="https://github.com/fosterbarnes/rEFIndConfigEditor/releases/download/v1.1.0/rEFIndConfigEditor_v1.1.0_windows-x64.exe"><img src="./.resources/svg/download_x64.svg" width="180" height="auto" alt="x64 installer"/></a></td>
<td align="center" valign="top"><a href="https://github.com/fosterbarnes/rEFIndConfigEditor/releases/download/v1.1.0/rEFIndConfigEditor_v1.1.0_windows-x86.exe"><img src="./.resources/svg/download_x86.svg" width="180" height="auto" alt="x86 installer"/></a></td>
<td align="center" valign="top"><a href="https://github.com/fosterbarnes/rEFIndConfigEditor/releases/download/v1.1.0/rEFIndConfigEditor_v1.1.0_windows-arm64.exe"><img src="./.resources/svg/download_arm.svg" width="180" height="auto" alt="ARM64 installer"/></a></td>
</tr>
</tbody>
</table>

<table border="0">
<tbody>
<tr>
<td align="center" valign="top"><a href="https://github.com/fosterbarnes/rEFIndConfigEditor/releases/download/v1.1.0/rEFIndConfigEditor_v1.1.0_windows-x64.zip"><img src="./.resources/svg/download_portable_x64.svg" width="180" height="auto" alt="x64 portable"/></a></td>
<td align="center" valign="top"><a href="https://github.com/fosterbarnes/rEFIndConfigEditor/releases/download/v1.1.0/rEFIndConfigEditor_v1.1.0_windows-x86.zip"><img src="./.resources/svg/download_portable_x86.svg" width="180" height="auto" alt="x86 portable"/></a></td>
<td align="center" valign="top"><a href="https://github.com/fosterbarnes/rEFIndConfigEditor/releases/download/v1.1.0/rEFIndConfigEditor_v1.1.0_windows-arm64.zip"><img src="./.resources/svg/download_portable_arm64.svg" width="180" height="auto" alt="ARM64 portable"/></a></td>
</tr>
</tbody>
</table>

### macOS

<table border="0"><tbody><tr>
<td align="center" valign="top">
<a href="https://github.com/fosterbarnes/rEFIndConfigEditor/releases/download/v1.1.0/rEFIndConfigEditor_v1.1.0_macOS-intel.zip">
<img src="./.resources/svg/download_appleIntel.svg" width="180" height="auto" alt="Intel portable"/></a></td>
<td align="center" valign="top">
<a href="https://github.com/fosterbarnes/rEFIndConfigEditor/releases/download/v1.1.0/rEFIndConfigEditor_v1.1.0_macOS-arm.zip"><img src="./.resources/svg/download_appleArm.svg" width="180" height="auto" alt="Apple Silicon portable"/></a></td>
</tr></tbody></table>

### Debian Linux (Debian 12–13, Ubuntu 24.04–26.04)

<details>
<summary>[Click to Expand]</summary>

#### Install dependencies:

Add the Microsoft package signing key to your list of trusted keys and add the package repository, then install .NET 10 runtime

```bash
wget https://packages.microsoft.com/config/debian/13/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt-get update && sudo apt-get install -y dotnet-runtime-10.0
```

Reference: [.NET 10 runtime](https://learn.microsoft.com/en-us/dotnet/core/install/linux-debian)

#### Install rEFInd Config Editor:

amd64:

```bash
wget https://github.com/fosterbarnes/rEFIndConfigEditor/releases/download/v1.1.0/rEFIndConfigEditor_v1.1.0_debian-amd64.deb
sudo apt install ./rEFIndConfigEditor_v1.1.0_debian-amd64.deb
```

arm64:

```bash
wget https://github.com/fosterbarnes/rEFIndConfigEditor/releases/download/v1.1.0/rEFIndConfigEditor_v1.1.0_debian-arm64.deb
sudo apt install ./rEFIndConfigEditor_v1.1.0_debian-arm64.deb
```
</details>

### Fedora Linux (Fedora 43–44)

<details>
<summary>[Click to Expand]</summary>

#### Install dependencies:

Install .NET 10 runtime from Fedora repos:

```bash
sudo dnf install -y dotnet-runtime-10.0
```

Reference: [.NET 10 runtime](https://learn.microsoft.com/en-us/dotnet/core/install/linux-fedora)

#### Install rEFInd Config Editor:

amd64:

```bash
wget https://github.com/fosterbarnes/rEFIndConfigEditor/releases/download/v1.1.0/rEFIndConfigEditor_v1.1.0_fedora-amd64.rpm
sudo dnf install ./rEFIndConfigEditor_v1.1.0_fedora-amd64.rpm
```

arm64:

```bash
wget https://github.com/fosterbarnes/rEFIndConfigEditor/releases/download/v1.1.0/rEFIndConfigEditor_v1.1.0_fedora-arm64.rpm
sudo dnf install ./rEFIndConfigEditor_v1.1.0_fedora-arm64.rpm
```
</details>

## Tabs

<details>
<summary>[Click to Expand]</summary>

| <h3>General</h3> |
|:---:|
| ![General](./.resources/scr/1.png) |

| <h3>Display</h3> |
|:---:|
| ![Display](./.resources/scr/2.png) |

| <h3>Theme</h3> |
|:---:|
| ![Theme](./.resources/scr/3.png) |

| <h3>Input</h3> |
|:---:|
| ![Input](./.resources/scr/4.png) |

| <h3>Scanning</h3> |
|:---:|
| ![Scanning](./.resources/scr/5.png) |

| <h3>Other</h3> |
|:---:|
| ![Other](./.resources/scr/6.png) |

| <h3>App</h3> |
|:---:|
| ![App](./.resources/scr/7.png) |

| <h3>About</h3> |
|:---:|
| ![About](./.resources/scr/8.png) |

| <h3>Raw .conf</h3> |
|:---:|
| ![Raw .conf](./.resources/scr/9.png) |

</details>

## App Themes

<details>
<summary>[Click to Expand]</summary>

| <h3>Light</h3> |
|:---:|
| ![Light](./.resources/scr/9.png) |

| <h3>Dark</h3> |
|:---:|
| ![Light](./.resources/scr/10.png) |

| <h3>Dracula</h3> |
|:---:|
| ![Light](./.resources/scr/12.png) |

</details>


## Compatibility

| Platform  | Architecture |
|------------|-----------------|
| Windows 10 | x86, x64, arm64 |
| Windows 11 | x86, x64, arm64 |
| macOS      | x64, arm64      |
| Debian Linux | x64, arm64    |
| Fedora Linux | x64, arm64    |