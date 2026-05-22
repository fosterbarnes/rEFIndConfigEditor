#requires -Version 7.0
. "$PSScriptRoot\scriptHelper.ps1"
Set-Location -LiteralPath $repoRoot

if ([string]::IsNullOrWhiteSpace($versionContents)) { throw "Version is empty: $version" }

function Assert-CleanGitTree {
    param([string]$Stage)
    $status = @(git status --porcelain 2>$null)
    if ($status.Count -gt 0) {
        throw "Working tree is not clean $Stage. Commit or stash changes before releasing:`n$($status -join "`n")"
    }
}

function Assert-GhAuthenticated {
    gh auth status 2>&1 | Out-Null
    if ($LASTEXITCODE) { throw 'GitHub CLI is not authenticated. Run: gh auth login' }
}

Assert-CleanGitTree -Stage 'before build'
Assert-GhAuthenticated

& "$PSScriptRoot\.buildAll.ps1"

Assert-CleanGitTree -Stage 'after build (commit README/build output if updateReadme.ps1 changed files)'

Write-Host "Version: $versionContents  Tag: $tag  Repo: $ghRepo"
Write-Host "`nBuild outputs:"
foreach ($target in $buildTargets) {
    Write-Host "  $($target.Architecture): $($target.BinFolder)  installer: $installerOutput\$($target.InstallerName)"
}

function Get-ReleaseAssetName {
    param([ValidateSet('Installer', 'Portable')][string]$Kind, [string]$Architecture)
    $ext = if ($Kind -eq 'Installer') { 'exe' } else { 'zip' }
    '{0}{1}_v{2}_{3}.{4}' -f $projectName, $Kind, $versionContents, $Architecture, $ext
}

function Get-ReleaseNotes {
    if (Test-Path -LiteralPath $buildNotes) {
        $fromFile = (Get-Content -LiteralPath $buildNotes -Raw -Encoding UTF8).Trim()
        if ($fromFile.Length -gt 0) {
            $rel = $buildNotes.Substring($repoRoot.Length).TrimStart('\')
            Write-Host "`nUsing $rel for release notes." -ForegroundColor Cyan
            return $fromFile
        }
    }

    Write-Host "`nEnter release notes (tabs -> spaces; finish with two empty lines):" -ForegroundColor Yellow
    $lines = [System.Collections.Generic.List[string]]::new()
    $emptyLines = 0
    $hasContent = $false
    while ($true) {
        $line = Read-Host '>'
        if ($line -eq '') {
            $emptyLines++
            if ($emptyLines -ge 2) { break }
            $lines.Add('')
        }
        else {
            $lines.Add(($line -replace "`t", '    '))
            $emptyLines = 0
            $hasContent = $true
        }
    }
    if (-not $hasContent) { throw 'No release notes entered.' }
    return ($lines -join "`n")
}

function Reset-GitTag {
    if (git tag -l $tag 2>$null) {
        Write-Host "Removing local tag $tag..."
        git tag -d $tag | Out-Null
    }
    $remoteRef = "refs/tags/$tag"
    $onRemote = @(git ls-remote --tags origin 2>$null | ForEach-Object { ($_ -split "`t", 2)[1] }) -contains $remoteRef
    if ($onRemote) {
        Write-Host "Removing remote tag $tag..."
        git push origin --delete $tag
        if ($LASTEXITCODE) { throw "Failed to delete remote tag: $tag" }
    }
}

$uploadFiles = @()
try {
    $releaseNotes = Get-ReleaseNotes

    Write-Host "`nStaging release assets..."
    foreach ($target in $buildTargets) {
        $arch = $target.Architecture

        $portablePath = Join-Path $env:TEMP (Get-ReleaseAssetName -Kind Portable -Architecture $arch)
        if (Test-Path -LiteralPath $portablePath) { Remove-Item -LiteralPath $portablePath -Force }
        Compress-Archive -Path (Join-Path $target.BinFolder '*') -DestinationPath $portablePath -Force
        $uploadFiles += $portablePath
        Write-Host "  $([IO.Path]::GetFileName($portablePath))"

        $builtInstaller = Join-Path $installerOutput $target.InstallerName
        if (-not (Test-Path -LiteralPath $builtInstaller)) { throw "Missing installer (run buildInstaller.ps1): $builtInstaller" }
        $installerPath = Join-Path $env:TEMP (Get-ReleaseAssetName -Kind Installer -Architecture $arch)
        if (Test-Path -LiteralPath $installerPath) { Remove-Item -LiteralPath $installerPath -Force }
        Copy-Item -LiteralPath $builtInstaller -Destination $installerPath -Force
        $uploadFiles += $installerPath
        Write-Host "  $([IO.Path]::GetFileName($installerPath))"
    }

    Reset-GitTag
    git tag $tag
    if ($LASTEXITCODE) { throw "git tag failed: $tag" }
    git push origin $tag
    if ($LASTEXITCODE) { throw "git push tag failed: $tag" }

    Write-Host "`nCreating GitHub release..."
    $notesFile = Join-Path $env:TEMP "rEFIndConfigEditor-release-notes-$tag.txt"
    Set-Content -LiteralPath $notesFile -Value $releaseNotes -Encoding utf8NoBOM
    try {
        & gh release create $tag @uploadFiles --repo $ghRepo --title $tag --notes-file $notesFile
        if ($LASTEXITCODE) { throw "gh release create failed (exit $LASTEXITCODE)" }
    }
    finally {
        Remove-Item -LiteralPath $notesFile -Force -ErrorAction SilentlyContinue
    }

    Write-Host "Release $tag published: https://github.com/$ghRepo/releases/tag/$tag" -ForegroundColor Green
}
finally {
    foreach ($path in $uploadFiles) {
        Remove-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue
    }
}
