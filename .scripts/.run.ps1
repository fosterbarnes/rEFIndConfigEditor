param(
    [Alias('h')][switch]$Help,
    [Parameter(ValueFromRemainingArguments = $true)][string[]]$AppLaunchArgs
)

#requires -Version 7.0
$ErrorActionPreference = 'Stop'

$runHelp = @"
.run.ps1 [$(if ($IsMacOS -or $IsLinux) { '--x64 | --arm64 | --portable' } else { '--x86 | --x64 | --arm64 | --portable' })] [-- app args...]
While running: q = quit, r = restart (fresh dotnet run)
"@

function Get-Platform([string]$f) {
    switch -Regex ($f) {
        '(?i)^(--x86|--86|-x86|-86)$' { if (-not $IsMacOS -and -not $IsLinux) { return @{ Tag = 'x86'; MsBuild = 'x86' } }; break }
        '(?i)^(--x64|--64|-x64|-64)$' { return @{ Tag = 'x64'; MsBuild = 'x64' } }
        '(?i)^(--arm64|--arm|-arm64|-arm)$' { return @{ Tag = 'arm64'; MsBuild = 'ARM64' } }
        '(?i)^(--portable|--p|-portable|-p|portable)$' { return @{ Tag = 'portable'; MsBuild = 'AnyCPU' } }
    }
}

function Stop-Run($proc) {
    if ($proc -and -not $proc.HasExited) { Stop-Process -Id $proc.Id -Force -EA SilentlyContinue; $proc.WaitForExit(5000) }
    Stop-Process -Name $projectName -Force -EA SilentlyContinue
}

if ($Help) { Write-Host $runHelp; exit }

. (Join-Path $PSScriptRoot 'scriptHelper.ps1')
Set-Location -LiteralPath $repoRoot

$defaultRunFlag = if ($IsMacOS -or $IsLinux) {
    if ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq [System.Runtime.InteropServices.Architecture]::Arm64) { '--arm64' } else { '--x64' }
} else { 'portable' }
$plat = Get-Platform $defaultRunFlag
$appArgs = @()

foreach ($a in $AppLaunchArgs + $args) {
    if ([string]::IsNullOrWhiteSpace($a)) { continue }
    $t = $a.Trim()
    if ($t -match '(?i)^(-h|--help|--h)$') { Write-Host $runHelp; exit }
    if ($p = Get-Platform $t) { $plat = $p; continue }
    $appArgs += $a
}

while ($true) {
    Set-VersionBuildPlatform $plat.Tag
    if ($IsMacOS -and ($rid = Get-UpdaterRuntimeIdentifier $plat.MsBuild)) {
        & dotnet restore $updaterCsproj -r $rid
        if ($LASTEXITCODE) { throw "Updater restore failed for $rid (exit $LASTEXITCODE)." }
    }
    $dotnetArgs = @('run', '--project', $csproj, '--framework', $dotnetFramework, '-c', 'Release', "-p:Platform=$($plat.MsBuild)")
    if ($appArgs.Count) { $dotnetArgs += '--', $appArgs }
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new('dotnet')
    $startInfo.WorkingDirectory = $repoRoot
    $startInfo.UseShellExecute = $false
    foreach ($arg in $dotnetArgs) { [void]$startInfo.ArgumentList.Add($arg) }
    $proc = [System.Diagnostics.Process]::Start($startInfo)
    if ($null -eq $proc) { throw 'Could not start dotnet.' }
    Write-Host "$projectName running. q=quit, r=restart."
    $act = $null
    while (-not $proc.HasExited -and -not $act) {
        Start-Sleep -Milliseconds 100
        try { if ([Console]::KeyAvailable) {
            switch ([Console]::ReadKey($true).Key) { 'Q' { $act = 'quit' } { $_ -in 'R', 'UpArrow' } { $act = 'restart' } }
        } } catch { }
    }
    if ($act -eq 'restart') { Write-Host 'Restarting (dotnet run)...'; Stop-Run $proc; continue }
    if ($act -eq 'quit') { Stop-Run $proc }
    elseif ($proc.ExitCode) { Write-Host "dotnet exit $($proc.ExitCode)" -ForegroundColor Red }
    break
}
