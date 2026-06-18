#requires -Version 7.0
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'scriptHelper.ps1')
Set-Location -LiteralPath $repoRoot

if ([string]::IsNullOrWhiteSpace($versionContents)) { throw "Version is empty: $version" }
if ($readmeContents -notmatch '<!-- Quick Reference --') { throw 'Could not find Quick Reference block in README.md.' }

$releaseBase = "$appURL/releases/download/$tag"
$quickReference = [ordered]@{ version = $versionContents }

foreach ($target in $winReleaseTargets) {
    $arch = $target.Architecture
    $label = if ($arch -eq 'arm64') { 'ARM64' } else { $arch }
    $quickReference["${label}Installer"] = "$releaseBase/$(Get-WinReleaseAssetName -Kind Installer -Architecture $arch)"
    $quickReference["${label}Portable"] = "$releaseBase/$(Get-WinReleaseAssetName -Kind Portable -Architecture $arch)"
}
$quickReference['osxX64Portable'] = "$releaseBase/$(Get-MacPortableReleaseAssetName -AssetTag 'macOS-intel')"
$quickReference['osxArm64Portable'] = "$releaseBase/$(Get-MacPortableReleaseAssetName -AssetTag 'macOS-arm')"
$quickReference['linuxAmd64Deb'] = "$releaseBase/$(Get-LinuxDebReleaseAssetName -Architecture 'x64')"
$quickReference['linuxArm64Deb'] = "$releaseBase/$(Get-LinuxDebReleaseAssetName -Architecture 'arm64')"
$quickReference['linuxAmd64Rpm'] = "$releaseBase/$(Get-LinuxRpmReleaseAssetName -Architecture 'x64')"
$quickReference['linuxArm64Rpm'] = "$releaseBase/$(Get-LinuxRpmReleaseAssetName -Architecture 'arm64')"

$refLines = ($quickReference.GetEnumerator() | ForEach-Object { "$($_.Key) = $($_.Value)" }) -join "`n`n"
$readmeUpdated = [regex]::Replace($readmeContents, '(?s)<!-- Quick Reference --?>?\s*.*?\s*-->', "<!-- Quick Reference --`n$refLines`n-->")

$svgSections = @(
    @{
        Pattern = '(?s)(?<section>### Windows\b.*?(?=### macOS))'
        Links = @{
            'download_x64.svg' = 'x64Installer'; 'download_x86.svg' = 'x86Installer'; 'download_arm.svg' = 'ARM64Installer'
            'download_portable_x64.svg' = 'x64Portable'; 'download_portable_x86.svg' = 'x86Portable'; 'download_portable_arm64.svg' = 'ARM64Portable'
        }
    }
    @{
        Pattern = '(?s)(?<section>### macOS\b.*?(?=### Debian Linux))'
        Links = @{ 'download_appleIntel.svg' = 'osxX64Portable'; 'download_appleArm.svg' = 'osxArm64Portable' }
    }
)

foreach ($section in $svgSections) {
    if ($readmeUpdated -notmatch $section.Pattern) { continue }
    $text = $Matches['section']
    $updated = $text
    foreach ($svg in $section.Links.Keys) {
        $url = $quickReference[$section.Links[$svg]]
        if ([string]::IsNullOrWhiteSpace($url)) { continue }
        $svgPattern = [regex]::Escape($svg)
        $updated = [regex]::Replace($updated, "(?s)(<a href=`")[^`"]+(`">\s*<img src=`"\./\.resources/svg/$svgPattern`")", "`${1}$url`${2}")
    }
    $readmeUpdated = $readmeUpdated.Replace($text, $updated)
}

if ($readmeUpdated -match '(?s)(?<section>### Debian Linux\b.*?(?=### Fedora Linux))') {
    $text = $Matches['section']
    $amd64Deb = [IO.Path]::GetFileName($quickReference['linuxAmd64Deb'])
    $arm64Deb = [IO.Path]::GetFileName($quickReference['linuxArm64Deb'])
    $updated = $text
    $updated = [regex]::Replace($updated, 'wget https://github\.com/[^/\s]+/[^/\s]+/releases/download/v[\d.]+/[^/\s]+_v[\d.]+_debian-amd64\.deb', "wget $($quickReference['linuxAmd64Deb'])")
    $updated = [regex]::Replace($updated, 'wget https://github\.com/[^/\s]+/[^/\s]+/releases/download/v[\d.]+/[^/\s]+_v[\d.]+_debian-arm64\.deb', "wget $($quickReference['linuxArm64Deb'])")
    $updated = [regex]::Replace($updated, 'sudo apt install \./[^/\s]+_v[\d.]+_debian-amd64\.deb', "sudo apt install ./$amd64Deb")
    $updated = [regex]::Replace($updated, 'sudo apt install \./[^/\s]+_v[\d.]+_debian-arm64\.deb', "sudo apt install ./$arm64Deb")
    $readmeUpdated = $readmeUpdated.Replace($text, $updated)
}

if ($readmeUpdated -match '(?s)(?<section>### Fedora Linux\b.*?(?=## Tabs))') {
    $text = $Matches['section']
    $amd64Rpm = [IO.Path]::GetFileName($quickReference['linuxAmd64Rpm'])
    $arm64Rpm = [IO.Path]::GetFileName($quickReference['linuxArm64Rpm'])
    $updated = $text
    $updated = [regex]::Replace($updated, 'wget https://github\.com/[^/\s]+/[^/\s]+/releases/download/v[\d.]+/[^/\s]+_v[\d.]+_fedora-amd64\.rpm', "wget $($quickReference['linuxAmd64Rpm'])")
    $updated = [regex]::Replace($updated, 'wget https://github\.com/[^/\s]+/[^/\s]+/releases/download/v[\d.]+/[^/\s]+_v[\d.]+_fedora-arm64\.rpm', "wget $($quickReference['linuxArm64Rpm'])")
    $updated = [regex]::Replace($updated, 'sudo dnf install \./[^/\s]+_v[\d.]+_fedora-amd64\.rpm', "sudo dnf install ./$amd64Rpm")
    $updated = [regex]::Replace($updated, 'sudo dnf install \./[^/\s]+_v[\d.]+_fedora-arm64\.rpm', "sudo dnf install ./$arm64Rpm")
    $readmeUpdated = $readmeUpdated.Replace($text, $updated)
}

if ($readmeUpdated -eq $readmeContents) {
    Write-Host "README already up to date (v$versionContents)."
    exit 0
}
Set-Content -LiteralPath $readme -Value $readmeUpdated -NoNewline -Encoding utf8NoBOM
Write-Host "Updated README.md for v$versionContents"
exit 0
