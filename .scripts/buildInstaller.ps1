. "$PSScriptRoot\scriptHelper.ps1"; Set-Location $repoRoot
$appIconIss = "..\.resources\icon\$([IO.Path]::GetFileName($appIcon))"
Write-Host "Clearing old installers..."; Remove-Item $installerOutput -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $installerOutput -Force | Out-Null
if (-not (Test-Path -LiteralPath $appIcon)) { throw "App icon not found: $appIcon" }
$issDefines = @{ AppPublisher = $appPublisher; AppURL = $appURL; SetupIconFile = $appIconIss }
$isccArgs = @("/DAppVersion=$versionContents", "/DAppPublisher=$appPublisher", "/DAppURL=$appURL", "/DSetupIconFile=$appIconIss")
foreach ($target in $buildTargets) {
    $iss = Get-Content -LiteralPath $target.InstallerScript -Raw
    foreach ($name in $issDefines.Keys) {
        $line = "#define $name `"$($issDefines[$name])`""
        if ($iss -notmatch [regex]::Escape($line)) { throw "$($target.InstallerScript): $name must match scriptHelper ('$($issDefines[$name])')" }
    }
    if (-not (Test-Path -LiteralPath $target.ExePath)) { throw "Missing publish output (run build.ps1 first): $($target.ExePath)" }
    Write-Host "Building $($target.Architecture) installer (AppVersion=$versionContents)"
    & $ISCC @isccArgs $target.InstallerScript
    if ($LASTEXITCODE) { throw "ISCC failed ($LASTEXITCODE): $($target.InstallerScript)" }
}
Write-Host "Done. Output: $installerOutput"
