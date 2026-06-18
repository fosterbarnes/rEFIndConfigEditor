param([string]$Architecture)

#requires -Version 7.0
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'scriptHelper.ps1')
Set-Location -LiteralPath $repoRoot

if ($IsLinux) {
    Write-Host 'Skipping updater build on Linux (not supported in v1).'
    return
}

$buildTargets = Select-BuildTargets $Architecture
foreach ($target in $buildTargets) {
    Write-Host "Building updater for $($target.RuntimeIdentifier)... ($($target.BinFolder))"
    & dotnet publish $updaterCsproj -c Release -r $target.RuntimeIdentifier --no-self-contained -o $target.BinFolder
    if ($LASTEXITCODE) { throw "dotnet publish failed for updater ($($target.RuntimeIdentifier) exit $LASTEXITCODE)" }
    if ($IsMacOS) { Set-UnixHostExecutable (Join-Path $target.BinFolder 'updater') }
}
