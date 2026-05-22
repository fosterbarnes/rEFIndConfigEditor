param([Alias('h')][switch]$Help, [Parameter(ValueFromRemainingArguments = $true)][string[]]$AppLaunchArgs)

$runHelp = @"
.run.ps1 [--x86 | --x64 | --arm64 | --portable] [-- app args...]
While running: q = quit, r = restart (fresh dotnet run)
"@

function Get-Platform([string]$f) {
    switch -Regex ($f) {
        '(?i)^(--x86|--86|-x86|-86)$' { return @{ Tag = 'x86'; MsBuild = 'x86' } }
        '(?i)^(--x64|--64|-x64|-64)$' { return @{ Tag = 'x64'; MsBuild = 'x64' } }
        '(?i)^(--arm64|--arm|-arm64|-arm)$' { return @{ Tag = 'arm64'; MsBuild = 'ARM64' } }
        '(?i)^(--portable|--p|-portable|-p|portable)$' { return @{ Tag = 'portable'; MsBuild = 'AnyCPU' } }
    }
}

function Stop-Run([System.Diagnostics.Process]$proc) {
    if ($proc -and -not $proc.HasExited) {
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
        $proc.WaitForExit(5000)
    }
    Stop-Process -Name $projectName -Force -ErrorAction SilentlyContinue
}

if ($Help) { Write-Host $runHelp; exit }
. "$PSScriptRoot\scriptHelper.ps1"; Set-Location $repoRoot
$plat = Get-Platform 'portable'
$appArgs = @()

foreach ($a in @($AppLaunchArgs) + @($args)) {
    if ([string]::IsNullOrWhiteSpace($a)) { continue }
    $t = $a.Trim()
    if ($t -match '(?i)^(-h|--help|--h)$') { Write-Host $runHelp; exit }
    $p = Get-Platform $t
    if ($p) { $plat = $p; continue }
    $appArgs += $a
}

while ($true) {
    Set-VersionBuildPlatform $plat.Tag
    $dotnetArgs = @('run', '--project', $csproj, '--framework', $dotnetFramework, '-c', 'Release', "-p:Platform=$($plat.MsBuild)")
    if ($appArgs.Count) { $dotnetArgs += '--'; $dotnetArgs += $appArgs }
    $proc = Start-Process dotnet -ArgumentList $dotnetArgs -WorkingDirectory $repoRoot -NoNewWindow -PassThru
    Write-Host "$projectName running. q=quit, r=restart."
    $act = $null
    :wait while (-not $proc.HasExited) {
        Start-Sleep -Milliseconds 100
        try { if (-not [Console]::KeyAvailable) { continue } } catch { continue }
        switch ([Console]::ReadKey($true).Key) {
            'Q' { $act = 'quit'; break wait }
            'R' { $act = 'restart'; break wait }
        }
    }

    if ($act -eq 'restart') { Write-Host 'Restarting (dotnet run)...'; Stop-Run $proc; continue }

    if ($act -eq 'quit') { Stop-Run $proc }
    elseif ($proc.ExitCode) { Write-Host "dotnet exit $($proc.ExitCode)" -ForegroundColor Red }
    break
}