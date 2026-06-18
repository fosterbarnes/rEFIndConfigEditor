#requires -Version 7.0
param(
    [Alias('b')][switch]$build,
    [Alias('f')][switch]$foundOnly
)
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'scriptHelper.ps1')
Set-Location -LiteralPath $repoRoot

if ($build) {
    & (Join-Path $PSScriptRoot '.buildAll.ps1')
    if ($LASTEXITCODE) { throw "buildAll failed (exit $LASTEXITCODE)." }
}

function Resolve-ReleaseAssetPath([string]$Path, [string]$Message) {
    if (Test-Path -LiteralPath $Path) { return $Path }
    if ($foundOnly) {
        Write-Host "  Skipping (not found): $([IO.Path]::GetFileName($Path))" -ForegroundColor Yellow
        return $null
    }
    throw $Message
}

function Get-ReleaseNotes {
    if (Test-Path -LiteralPath $buildNotes) {
        $text = (Get-Content -LiteralPath $buildNotes -Raw -Encoding UTF8).Trim()
        if ($text) {
            Write-Host "`nUsing .md/buildNotes.txt for release notes." -ForegroundColor Cyan
            return $text
        }
    }

    Write-Host "`nEnter release notes (tabs -> spaces; finish with two empty lines):" -ForegroundColor Yellow
    $lines = [System.Collections.Generic.List[string]]::new()
    $empty = 0
    do {
        $line = Read-Host '>'
        if ($line -eq '') { $empty++; $lines.Add('') }
        else { $lines.Add(($line -replace "`t", '    ')); $empty = 0 }
    } until ($empty -ge 2)

    $text = ($lines -join "`n").Trim()
    if (-not $text) { throw 'No release notes entered.' }
    return $text
}

function Invoke-DraftRelease {
    if ([string]::IsNullOrWhiteSpace($versionContents)) { throw "Version is empty: $version" }

    Write-Host "Version: $versionContents  Tag: $tag  Repo: $ghRepo"
    if ($foundOnly) {
        Write-Host 'Uploading only release assets found under publish/ (-foundOnly).' -ForegroundColor Cyan
    }
    Write-Host "`nExpected release outputs (under publish/):"
    foreach ($t in $winReleaseTargets) {
        $a = $t.Architecture
        $portable = Join-Path $publishFolder (Get-WinReleaseAssetName -Kind Portable -Architecture $a)
        $installer = Join-Path $publishFolder (Get-WinReleaseAssetName -Kind Installer -Architecture $a)
        Write-Host "  Windows $a`: $portable$(if ($foundOnly) { if (Test-Path -LiteralPath $portable) { ' [found]' } else { ' [missing]' } })"
        Write-Host "    installer: $installer$(if ($foundOnly) { if (Test-Path -LiteralPath $installer) { ' [found]' } else { ' [missing]' } })"
    }
    foreach ($t in $macReleaseTargets) {
        $portable = Join-Path $publishFolder (Get-MacPortableReleaseAssetName -AssetTag $t.AssetTag)
        Write-Host "  macOS $($t.AssetTag): $portable$(if ($foundOnly) { if (Test-Path -LiteralPath $portable) { ' [found]' } else { ' [missing]' } })"
    }
    foreach ($t in $linuxReleaseTargets) {
        $deb = Join-Path $publishFolder (Get-LinuxDebReleaseAssetName -Architecture $t.Architecture)
        Write-Host "  Linux deb $($t.DebTag): $deb$(if ($foundOnly) { if (Test-Path -LiteralPath $deb) { ' [found]' } else { ' [missing]' } })"
    }
    foreach ($t in $linuxRpmReleaseTargets) {
        $rpm = Join-Path $publishFolder (Get-LinuxRpmReleaseAssetName -Architecture $t.Architecture)
        Write-Host "  Linux rpm $($t.RpmTag): $rpm$(if ($foundOnly) { if (Test-Path -LiteralPath $rpm) { ' [found]' } else { ' [missing]' } })"
    }

    $notesFile = Join-Path $env:TEMP "releaseNotes_$tag.txt"
    try {
        Set-Content -LiteralPath $notesFile -Value (Get-ReleaseNotes) -NoNewline -Encoding utf8NoBOM

        Write-Host "`nStaging release assets..."
        $uploadFiles = @(
            foreach ($t in $winReleaseTargets) {
                foreach ($kind in 'Portable', 'Installer') {
                    $p = Join-Path $publishFolder (Get-WinReleaseAssetName -Kind $kind -Architecture $t.Architecture)
                    Resolve-ReleaseAssetPath $p "Missing Windows $($kind.ToLower()) ($($t.Architecture)). Run .buildAll.ps1 on Windows first, or pass -foundOnly to upload found assets only: $p"
                }
            }
            foreach ($t in $macReleaseTargets) {
                $p = Join-Path $publishFolder (Get-MacPortableReleaseAssetName -AssetTag $t.AssetTag)
                Resolve-ReleaseAssetPath $p "Missing macOS portable ($($t.AssetTag)). Run .buildAll.ps1 on macOS first, or pass -foundOnly to upload found assets only: $p"
            }
            foreach ($t in $linuxReleaseTargets) {
                $p = Join-Path $publishFolder (Get-LinuxDebReleaseAssetName -Architecture $t.Architecture)
                Resolve-ReleaseAssetPath $p "Missing Linux .deb ($($t.DebTag)). Run .buildAll.ps1 on Debian Linux first, or pass -foundOnly to upload found assets only: $p"
            }
            foreach ($t in $linuxRpmReleaseTargets) {
                $p = Join-Path $publishFolder (Get-LinuxRpmReleaseAssetName -Architecture $t.Architecture)
                Resolve-ReleaseAssetPath $p "Missing Linux .rpm ($($t.RpmTag)). Run .buildAll.ps1 on Fedora Linux first, or pass -foundOnly to upload found assets only: $p"
            }
        ) | Where-Object { $_ }
        if (-not $uploadFiles.Count) {
            throw 'No release assets found under publish/. Run .buildAll.ps1 first.'
        }
        Write-Host 'Uploading:'
        $uploadFiles | ForEach-Object { Write-Host "  $([IO.Path]::GetFileName($_))" }

        if (git tag -l $tag 2>$null) { Write-Host "Removing local tag $tag..."; git tag -d $tag | Out-Null }
        if (git ls-remote --tags origin "refs/tags/$tag" 2>$null) {
            Write-Host "Removing remote tag $tag..."
            git push origin --delete $tag
            if ($LASTEXITCODE) { throw "Failed to delete remote tag: $tag" }
        }

        git tag $tag; if ($LASTEXITCODE) { throw "git tag failed: $tag" }
        git push origin $tag; if ($LASTEXITCODE) { throw "git push tag failed: $tag" }

        Write-Host "`nCreating GitHub release..."
        & gh release create $tag @uploadFiles --repo $ghRepo --title $tag --notes-file $notesFile
        if ($LASTEXITCODE) { throw "gh release create failed (exit $LASTEXITCODE)" }

        Write-Host "Release $tag published: https://github.com/$ghRepo/releases/tag/$tag" -ForegroundColor Green
    }
    finally {
        Remove-Item -LiteralPath $notesFile -Force -ErrorAction SilentlyContinue
    }
}

Invoke-DraftRelease
