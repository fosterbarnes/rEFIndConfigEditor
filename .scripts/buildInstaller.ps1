param([string]$Architecture)

#requires -Version 7.0
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'scriptHelper.ps1')
Set-Location -LiteralPath $repoRoot

$targets = Select-BuildTargets $Architecture

if ($IsLinux) {
    $format = Get-LinuxPackageFormat
    foreach ($t in $targets) {
        if ($format -eq 'rpm') {
            if (-not (Test-LinuxRpmPackageSupported $t.Architecture)) {
                $hostCpu = (& uname -m).Trim()
                $needHost = if ($t.Architecture -eq 'x64') { 'x86_64' } else { 'aarch64' }
                if ($Architecture) {
                    throw "$($t.Architecture) RPM cannot be built on $hostCpu Fedora. Build $($t.Architecture) RPM on $needHost Fedora, or use -$(Get-LinuxRpmHostArchitecture) on this host."
                }
                Write-Host "Skipping $($t.Architecture) .rpm (rpmbuild on $hostCpu cannot package $(Get-LinuxRpmArchitecture $t.Architecture) RPMs; build on $needHost Fedora)." -ForegroundColor Yellow
                continue
            }
            New-LinuxRpmPackage $t
        } else {
            New-LinuxDebPackage $t
        }
    }
    Write-Host "$(if ($format -eq 'rpm') { 'Fedora RPM' } else { 'Debian' }) packages built."
    return
}

if ($IsMacOS) {
    Invoke-MacAppBundleCodesign $targets
    Write-Host 'App bundles sealed.'
    return
}

Sync-InstallerDefines
if (-not (Test-Path -LiteralPath $appIcon)) { throw "App icon not found: $appIcon" }
Ensure-InstallerWizardImages

$issDefines = Get-InstallerIssDefines
$isccArgs = 'AppVersion', 'AppPublisher', 'AppURL', 'SetupIconFile', 'WizardSmallImageFile', 'WizardImageFile' | ForEach-Object { "/D$_=$($issDefines[$_])" }

if (-not $Architecture) { Write-Host "Clearing old installers..."; Remove-Item $installerOutput -Recurse -Force -ErrorAction SilentlyContinue }
New-Item -ItemType Directory -Path $installerOutput -Force | Out-Null

foreach ($t in $targets) {
    if ($Architecture) {
        $out = Join-Path $installerOutput $t.InstallerName
        if (Test-Path -LiteralPath $out) { Remove-Item -LiteralPath $out -Force }
    }
    $iss = Get-Content -LiteralPath $t.InstallerScript -Raw
    foreach ($name in $issDefines.Keys) {
        if (-not (Test-IssDefine -Content $iss -Name $name -ExpectedValue $issDefines[$name])) { throw "$($t.InstallerScript): $name must match scriptHelper ('$($issDefines[$name])')" }
    }
    if (-not (Test-Path -LiteralPath $t.ExePath)) { throw "Missing publish output (run build.ps1 first): $($t.ExePath)" }
    $updaterExe = Join-Path $t.BinFolder 'updater.exe'
    if (-not (Test-Path -LiteralPath $updaterExe)) { throw "Missing updater.exe (run build.ps1 first): $updaterExe" }
    Write-Host "Building $($t.Architecture) installer (AppVersion=$versionContents)"
    & $ISCC @isccArgs $t.InstallerScript; if ($LASTEXITCODE) { throw "ISCC failed ($LASTEXITCODE): $($t.InstallerScript)" }
}
Write-Host "Done. Output: $installerOutput"
