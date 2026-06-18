#requires -Version 7.0
param(
    [switch]$mac,
    [switch]$win
)
$ErrorActionPreference = 'Stop'

function Join-RepoPath {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Segments)
    if (-not $Segments.Count) { throw 'Join-RepoPath requires at least one segment.' }
    $path = $Segments[0]
    for ($i = 1; $i -lt $Segments.Count; $i++) {
        $segment = $Segments[$i].TrimStart('/')
        $path = if ($path.EndsWith('/')) { "$path$segment" } else { "$path/$segment" }
    }
    return $path
}

function Normalize-PathSlashes {
    param([Parameter(Mandatory)][string]$Path)
    return $Path.Replace('\', '/')
}

function Resolve-CanonicalRepoRoot {
    param([Parameter(Mandatory)][string]$Root)

    $normalized = (Normalize-PathSlashes $Root).TrimEnd('/')
    if ($IsLinux -or $IsMacOS) {
        $resolved = (& readlink -f $normalized 2>$null | Out-String).Trim()
        if ($resolved) { return (Normalize-PathSlashes $resolved).TrimEnd('/') }
    }

    try {
        return (Normalize-PathSlashes (Get-Item -LiteralPath $normalized -ErrorAction Stop).FullName).TrimEnd('/')
    } catch {
        return $normalized
    }
}

$objBinPathKey = '(?:projectPath|projectUniqueName|outputPath|projectFilePath)'

if (-not $repoRoot) {
    $repoRoot = Resolve-CanonicalRepoRoot (Split-Path $PSScriptRoot -Parent)
}

$copyRepoWinPath = '/Volumes/[C] Windows 11/Users/foster/Documents/GitHub/rEFIndConfigEditor'
$copyRepoWinVolume = '/Volumes/[C] Windows 11'

$copyRepoMacPath = ''
$copyRepoMacPathWin = 'Z:\Users\foster\Documents\GitHub\rEFIndConfigEditor'
$copyRepoMacVolumeWin = 'Z:\'

$foreignOsProfiles = @{
    Mac     = @{ ObjBinRegex = "`"$objBinPathKey`":\s*`"[^`"]*/Users/"; PublishSubdirs = 'osx-arm64', 'osx-x64'; RepoFileMarkers = '.DS_Store'; PublishLabel = 'macOS' }
    Windows = @{ ObjBinRegex = "`"$objBinPathKey`":\s*`"[^`"]*(?:[A-Z]:\\\\|\\\\Users\\\\)"; PublishSubdirs = 'x86', 'x64', 'arm64'; RepoFileMarkers = @(); PublishLabel = 'Windows' }
    Linux   = @{ ObjBinRegex = "`"$objBinPathKey`":\s*`"[^`"]*/home/"; PublishSubdirs = 'linux-x64', 'linux-arm64'; RepoFileMarkers = @(); PublishLabel = 'Linux' }
}
function Get-HostPlatformKey {
    if ($IsMacOS) { return 'Mac' }
    if ($IsLinux) { return 'Linux' }
    return 'Windows'
}

function Test-RepoHasForeignOwner {
    param(
        [Parameter(Mandatory)][string]$Root,
        [object[]]$Scope = @()
    )

    if (-not $IsMacOS) { return $false }
    $args = @($Root) + $Scope + '!', '-user', (whoami).Trim(), '-print', '-quit'
    return [bool](& /usr/bin/find @args 2>$null)
}

function Repair-RepoDeleteAccessAt {
    param([Parameter(Mandatory)][string]$Root)

    if (-not (Test-Path -LiteralPath $Root)) { return }

    if ($IsMacOS) {
        if (Test-RepoHasForeignOwner $Root) {
            $owner = "$(whoami):staff"
            Write-Host "Repairing ownership under $Root..."
            & sudo -n chown -R $owner $Root
            if ($LASTEXITCODE) {
                Write-Host 'Administrator password required to repair shared-folder ownership.'
                & sudo chown -R $owner $Root
            }
            if ($LASTEXITCODE) { throw "Could not repair ownership under $Root. Run once: sudo chown -R $owner `"$Root`"" }
        }
        & chmod -R u+rwX $Root
    }

    Get-ChildItem -LiteralPath $Root -Recurse -Force -ErrorAction SilentlyContinue |
        ForEach-Object { try { $_.Attributes = 'Normal' } catch { } }
}

function Remove-TreeForce {
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) { return }

    if ($IsMacOS -or $IsLinux) { & chmod -R u+rwX $Path 2>$null }

    try { Remove-Item -LiteralPath $Path -Recurse -Force -ErrorAction Stop }
    catch { if (-not $IsMacOS -and -not $IsLinux) { throw } }

    if (($IsMacOS -or $IsLinux) -and (Test-Path -LiteralPath $Path)) {
        & /bin/rm -rf -- $Path 2>$null
    }

    if (Test-Path -LiteralPath $Path) {
        $hint = if ($IsMacOS) {
            "Close dotnet/MSBuild on the Windows VM if this repo is on a shared folder. If ownership is wrong, run once: sudo chown -R $(whoami):staff `"$repoRoot`""
        } elseif ($IsLinux) {
            "Close dotnet/MSBuild. If this repo is on a shared folder, run once: chmod -R u+rwX `"$repoRoot`""
        } else { 'Check folder permissions and retry.' }
        throw "Could not remove '$Path'. $hint"
    }
}

function Clear-RepoObjBin {
    param([Parameter(Mandatory)][string]$Root)

    Get-ChildItem -LiteralPath $Root -Recurse -Directory -Force -ErrorAction SilentlyContinue |
        Where-Object Name -in obj, bin |
        Sort-Object { $_.FullName.Length } -Descending |
        ForEach-Object { Remove-TreeForce $_.FullName }
}

function Ensure-RepoWritable {
    param([Parameter(Mandatory)][string]$Root)

    $writable = {
        if (-not (Test-Path -LiteralPath $Root)) { return $true }
        $probe = Join-Path $Root ".copy-probe-$PID"
        try {
            [IO.File]::WriteAllText($probe, '')
            Remove-Item -LiteralPath $probe -Force
            return $true
        } catch { return $false }
    }

    if (& $writable) { return }

    Repair-RepoDeleteAccessAt $Root
    if (& $writable) { return }

    foreach ($hint in @(
        @{ Test = { $IsMacOS -and $Root.StartsWith($copyRepoWinVolume, [StringComparison]::Ordinal) }; Msg = "Cannot write to $Root on the mounted Windows volume. Fix folder permissions in Windows, or delete that folder and run copyToOS.ps1 -win again." }
        @{ Test = { -not $IsMacOS -and $Root.StartsWith($copyRepoMacVolumeWin, [StringComparison]::OrdinalIgnoreCase) }; Msg = "Cannot write to $Root on the mounted Mac volume. Fix folder permissions on Mac, or delete that folder and run copyToOS.ps1 -mac again." }
        @{ Test = { $true }; Msg = $(if ($IsMacOS) { "Cannot write to $Root. Run once: sudo chown -R $(whoami):staff `"$Root`"" } else { "Cannot write to $Root. Check folder permissions and retry." }) }
    )) {
        if (& $hint.Test) { throw $hint.Msg }
    }
}

function Resolve-CopyDestination {
    param(
        [switch]$mac,
        [switch]$win
    )

    if ($win -and -not $IsMacOS) {
        throw 'Copy-ToOS -win is for macOS hosts (sync to the mounted Windows volume). On Windows, use -mac to sync to Mac.'
    }

    $dest = if ($mac) {
        if ($IsMacOS) { $copyRepoMacPath } else { $copyRepoMacPathWin }
    } else {
        $copyRepoWinPath
    }

    if ($mac -and $IsMacOS -and [string]::IsNullOrWhiteSpace($copyRepoMacPath)) {
        throw 'Set $copyRepoMacPath in copyToOS.ps1 for Copy-ToOS -mac on macOS.'
    }

    $destNorm = (Normalize-PathSlashes $dest).TrimEnd('/')
    if ($destNorm -eq (Normalize-PathSlashes $repoRoot).TrimEnd('/')) { throw 'Copy-ToOS destination must differ from $repoRoot.' }

    $volume = if ($win -and $IsMacOS) { $copyRepoWinVolume } elseif ($mac -and -not $IsMacOS) { $copyRepoMacVolumeWin }
    if ($volume -and -not (Test-Path -LiteralPath $volume)) {
        $label = if ($win) { 'Windows volume not mounted' } else { 'Mac volume not available' }
        throw "$label. Expected: $volume"
    }

    $parent = Split-Path $dest -Parent
    if (-not (Test-Path -LiteralPath $parent)) {
        $hint = if ($win -and $IsMacOS) { 'is the Windows volume mounted?' }
                elseif ($mac -and -not $IsMacOS) { 'is the Mac volume mounted?' }
                else { 'does the parent folder exist?' }
        throw "Copy destination parent not found ($hint): $parent"
    }

    return $dest
}

function Sync-CopyRepo {
    param(
        [Parameter(Mandatory)][string]$Source,
        [Parameter(Mandatory)][string]$Destination,
        [switch]$StripMacMetadata
    )

    $parent = Split-Path $Destination -Parent
    Ensure-RepoWritable $parent
    if (Test-Path -LiteralPath $Destination) { Ensure-RepoWritable $Destination }
    New-Item -ItemType Directory -Path $Destination -Force | Out-Null

    Write-Host 'Syncing files...'
    if ($IsMacOS) {
        $excludes = @('--exclude=obj/', '--exclude=bin/')
        if ($StripMacMetadata) {
            $excludes += '--exclude=._*', '--exclude=.DS_Store'
            $env:COPYFILE_DISABLE = '1'
        }
        try {
            & rsync -a --delete --progress --stats @excludes "$Source/" "$Destination/"
            if ($LASTEXITCODE) { throw "rsync failed (exit $LASTEXITCODE)" }
        }
        finally {
            if ($StripMacMetadata) { Remove-Item Env:COPYFILE_DISABLE -ErrorAction SilentlyContinue }
        }
    }
    else {
        $robocopyArgs = @(
            $Source, $Destination,
            '/MIR', '/XD', 'obj', 'bin',
            '/R:2', '/W:2', '/NP'
        )
        if ($StripMacMetadata) { $robocopyArgs += '/XF', '.DS_Store' }

        & robocopy @robocopyArgs
        if ($LASTEXITCODE -ge 8) { throw "robocopy failed (exit $LASTEXITCODE)" }
    }
}

function Clear-MacMetadataAt {
    param([Parameter(Mandatory)][string]$Root)

    if (-not (Test-Path -LiteralPath $Root)) { return }

    $files = @(Get-ChildItem -LiteralPath $Root -Filter '._*' -Recurse -Force -ErrorAction SilentlyContinue)
    if ($files.Count) { $files | Remove-Item -Force; Write-Host "Removed $($files.Count) macOS AppleDouble file(s)." }
}

function Test-ObjBinForeignArtifacts {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][hashtable]$Profile
    )

    foreach ($dir in Get-ChildItem -LiteralPath $Root -Recurse -Directory -Force -ErrorAction SilentlyContinue | Where-Object Name -in obj, bin) {
        foreach ($file in Get-ChildItem -LiteralPath $dir.FullName -Include '*.nuget.dgspec.json', 'project.assets.json', 'project.nuget.cache' -File -Recurse -Force -ErrorAction SilentlyContinue) {
            if ([IO.File]::ReadAllText($file.FullName) -match $Profile.ObjBinRegex) { return $true }
        }
    }

    return $false
}

function Test-CopiedFromForeignOs {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][hashtable]$Profile
    )

    if (Test-ObjBinForeignArtifacts $Root $Profile) { return $true }
    if (Test-RepoHasForeignOwner $Root '(', '-type', 'd', '-name', 'obj', '-o', '-type', 'd', '-name', 'bin', ')') { return $true }

    $publish = Join-RepoPath $Root 'publish'
    if ($Profile.PublishSubdirs | Where-Object { Test-Path -LiteralPath (Join-RepoPath $publish $_) } | Select-Object -First 1) { return $true }

    if ($Profile.RepoFileMarkers) {
        foreach ($marker in $Profile.RepoFileMarkers) {
            if (Get-ChildItem -LiteralPath $Root -Filter $marker -Recurse -Force -ErrorAction SilentlyContinue | Select-Object -First 1) { return $true }
        }
    }

    return $false
}

function Clear-ForeignPublishArtifactsAt {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][hashtable]$Profile
    )

    foreach ($marker in $Profile.RepoFileMarkers) {
        Get-ChildItem -LiteralPath $Root -Filter $marker -Recurse -Force -ErrorAction SilentlyContinue | Remove-Item -Force
    }

    $publish = Join-RepoPath $Root 'publish'
    foreach ($name in $Profile.PublishSubdirs) {
        $path = Join-RepoPath $publish $name
        if (Test-Path -LiteralPath $path) {
            Remove-TreeForce $path
            Write-Host "Removed $($Profile.PublishLabel) publish output: $path"
        }
    }
}

function Invoke-HostBuildPrep {
    param([string]$Root = $repoRoot)

    $hostKey = Get-HostPlatformKey
    $foreignDetected = $false
    foreach ($key in $foreignOsProfiles.Keys) {
        if ($key -eq $hostKey) { continue }
        if (Test-CopiedFromForeignOs $Root $foreignOsProfiles[$key]) {
            $foreignDetected = $true
            break
        }
    }
    if (-not $foreignDetected) { return }

    Write-Host 'Foreign build remnants detected. Preparing host...'
    Repair-RepoDeleteAccessAt $Root
    foreach ($key in $foreignOsProfiles.Keys) {
        if ($key -eq $hostKey) { continue }
        Clear-ForeignPublishArtifactsAt $Root $foreignOsProfiles[$key]
    }
    Write-Host 'Removing all obj/bin...'
    Clear-RepoObjBin $Root
}

function Clear-MacBuildArtifacts { if (-not $IsMacOS) { Invoke-HostBuildPrep } }
function Clear-WindowsBuildArtifacts { if ($IsMacOS -or $IsLinux) { Invoke-HostBuildPrep } }
function Clear-LinuxBuildArtifacts { if (-not $IsLinux) { Invoke-HostBuildPrep } }

function Initialize-CopiedRepo {
    Set-Location -LiteralPath $repoRoot
    Write-Host "Preparing folder copy for $(switch (Get-HostPlatformKey) { 'Mac' { 'macOS' } 'Linux' { 'Linux' } default { 'Windows' } })..."
    if ($IsMacOS) { Ensure-RepoWritable $repoRoot }
    Invoke-HostBuildPrep
    Write-Host 'Ready.'
}

function Copy-ToOS {
    param(
        [switch]$mac,
        [switch]$win
    )

    if ($mac -and $win) { throw 'Use exactly one of -mac or -win.' }
    if (-not $mac -and -not $win) { throw 'Specify -mac or -win as the copy destination.' }

    if ($IsMacOS -eq $mac.IsPresent) {
        $hostOs = if ($IsMacOS) { 'macOS' } else { 'Windows' }
        $switchName = if ($mac) { 'mac' } else { 'win' }
        $targetOs = if ($IsMacOS) { 'Windows' } else { 'Mac' }
        Write-Host "On $hostOs, -$switchName targets this machine (already your repo). Syncing to $targetOs instead."
        $mac = -not $IsMacOS; $win = $IsMacOS
    }

    $dest = Resolve-CopyDestination -mac:$mac -win:$win

    Write-Host "Syncing $repoRoot -> $dest"
    Sync-CopyRepo -Source $repoRoot -Destination $dest -StripMacMetadata:($win.IsPresent)
    Clear-RepoObjBin $dest

    $destProfile = $foreignOsProfiles[$(if ($mac) { 'Windows' } else { 'Mac' })]
    Clear-ForeignPublishArtifactsAt $dest $destProfile
    if (-not $mac) { Clear-MacMetadataAt $dest }

    Write-Host 'Copy complete.'
}

if ($MyInvocation.InvocationName -ne '.') {
    Copy-ToOS -mac:$mac -win:$win
}
