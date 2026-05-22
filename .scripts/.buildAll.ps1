. "$PSScriptRoot\scriptHelper.ps1"; Set-Location $repoRoot
& "$PSScriptRoot\build.ps1"; & "$PSScriptRoot\buildInstaller.ps1"
& "$PSScriptRoot\updateReadme.ps1"