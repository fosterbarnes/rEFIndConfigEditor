param([Alias('h')][switch]$Help, [Parameter(ValueFromRemainingArguments = $true)][string[]]$BuildArgs)

#requires -Version 7.0
$ErrorActionPreference = 'Stop'

$buildAllHelp = if ($IsMacOS) {
@"
.buildAll.ps1 [-arm64 | -arm | -x64 | -arm -64 | -64]
Publish main app, updater, adhoc-sealed app bundle, and portable zip for macOS (osx-arm64, osx-x64) under publish/. Developer ID signing optional via buildInstaller.ps1.
"@
}
elseif ($IsLinux) {
@"
.buildAll.ps1 [-arm64 | -arm | -x64 | -arm -64 | -64]
Publish main app and Linux packages under publish/. Debian/Ubuntu builds .deb; Fedora builds .rpm. Updater not built on Linux in v1.
"@
}
else {
@"
.buildAll.ps1 [-arm64 | -arm | -x64 | -x86 | -arm -64 | -64 | -86]
Build app, updater, installer, and portable zip under publish/. Omit flags to build all architectures.
"@
}

if ($Help) { Write-Host $buildAllHelp; exit }

. (Join-Path $PSScriptRoot 'scriptHelper.ps1')
Set-Location -LiteralPath $repoRoot

$architecture = Resolve-BuildArchitecture (@($BuildArgs) + @($args))
$archParams = @{}
if ($architecture) { $archParams['Architecture'] = $architecture }

$steps = @(
    @{ Name = 'build';          Script = 'build.ps1';          UseArch = $true  },
    @{ Name = 'buildInstaller'; Script = 'buildInstaller.ps1'; UseArch = $true  },
    @{ Name = 'updateReadme';   Script = 'updateReadme.ps1';   UseArch = $false }
)

foreach ($step in $steps) {
    $stepScript = Join-Path $PSScriptRoot $step.Script
    if ($step.UseArch) { & $stepScript @archParams } else { & $stepScript }
    if ($LASTEXITCODE) { throw "$($step.Name) failed (exit $LASTEXITCODE)." }

    if ($step.Name -eq 'buildInstaller') {
        Copy-ReleaseArtifactsToPublish @archParams
    }
}
