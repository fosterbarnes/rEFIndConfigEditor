param([string]$Architecture)

#requires -Version 7.0
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'scriptHelper.ps1')
Set-Location -LiteralPath $repoRoot

Ensure-VersionBuildPlatform $Architecture
& dotnet restore (Join-RepoPath $repoRoot "$projectName.sln")
if ($LASTEXITCODE) { throw "dotnet restore failed (exit $LASTEXITCODE)" }

$targets = Select-BuildTargets $Architecture
foreach ($target in $targets) {
    Write-Host "Building $($target.RuntimeIdentifier)... ($($target.BinFolder))"
    Remove-TreeForce $target.BinFolder
    & dotnet publish $csproj -c Release -r $target.RuntimeIdentifier --no-self-contained -p:PublishReadyToRun=true -o $target.BinFolder
    if ($LASTEXITCODE) { throw "dotnet publish failed ($($target.RuntimeIdentifier) exit $LASTEXITCODE)" }
}

if (-not $IsLinux) {
    & (Join-Path $PSScriptRoot 'buildUpdater.ps1') -Architecture $Architecture
    if ($LASTEXITCODE) { throw "buildUpdater failed (exit $LASTEXITCODE)" }
}

foreach ($target in $targets) {
    Copy-Item -LiteralPath $version -Destination (Join-Path $target.BinFolder 'Version') -Force
    Remove-PublishArtifacts $target.BinFolder
    if ($IsMacOS) { New-MacAppBundle $target }
    elseif ($IsLinux) { Set-UnixHostExecutable $target.HostPath }
}
