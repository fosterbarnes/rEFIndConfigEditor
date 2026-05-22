. "$PSScriptRoot\scriptHelper.ps1"; Set-Location $repoRoot
if ([string]::IsNullOrWhiteSpace($versionContents)) { throw "Version is empty: $version" }
if ($readmeContents -notmatch '<!-- Quick Reference --') { throw 'Could not find Quick Reference block in README.md.' }

$releaseBase = "$appURL/releases/download/v$versionContents"
$quickReferenceLines = @("version = $versionContents")
$downloadLinks = @{}

foreach ($target in $buildTargets) {
    $architecture = $target.Architecture
    $label = if ($architecture -eq 'arm64') { 'ARM64' } else { $architecture }

    foreach ($packageType in 'Installer', 'Portable') {
        $extension = if ($packageType -eq 'Installer') { 'exe' } else { 'zip' }
        $downloadUrl = "$releaseBase/${projectName}${packageType}_v${versionContents}_$architecture.$extension"
        $quickReferenceLines += "${label}${packageType} = $downloadUrl"
        if ($packageType -eq 'Portable') { $svgFile = "download_portable_$architecture.svg"} 
        elseif ($architecture -eq 'arm64') { $svgFile = 'download_arm.svg' } 
        else { $svgFile = "download_$architecture.svg" }
        $downloadLinks[$svgFile] = $downloadUrl
    }
}

$quickReferenceBlock = "<!-- Quick Reference --`n$($quickReferenceLines -join ("`n`n"))`n-->"
$readmeUpdated = [regex]::Replace($readmeContents, '(?s)<!-- Quick Reference --.*?-->', $quickReferenceBlock)
foreach ($svgFile in $downloadLinks.Keys) {
    $downloadUrl = $downloadLinks[$svgFile]
    $readmeUpdated = $readmeUpdated -replace "(<a href=`")[^`"]+(`"><img src=`"\./\.resources/svg/$svgFile`")", "`${1}$downloadUrl`${2}"
}

if ($readmeUpdated -eq $readmeContents) {
    Write-Host "README already up to date (v$versionContents)."
    exit 0
}
Set-Content -LiteralPath $readme -Value $readmeUpdated -NoNewline -Encoding utf8NoBOM
Write-Host "Updated README.md for v$versionContents"
