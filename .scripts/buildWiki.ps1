#requires -Version 7.0
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path $PSScriptRoot -Parent
$tokens = "$repoRoot\.resources\docs\tokens.md"
$themes = "$repoRoot\.resources\docs\themes.md"
$wikiHome = "$repoRoot\.resources\docs\wiki\Home.md"

if (-not (Test-Path -LiteralPath $tokens)) {
    throw "Missing $tokens — run .scripts\scrapeTokens.ps1 first."
}
if (-not (Test-Path -LiteralPath $themes)) {
    throw "Missing $themes — run .scripts\scrapeThemes.ps1 first."
}

$wikiDir = Split-Path $wikiHome -Parent
if (-not (Test-Path -LiteralPath $wikiDir)) {
    New-Item -ItemType Directory -Path $wikiDir -Force | Out-Null
}

$tokenText = [IO.File]::ReadAllText($tokens).TrimEnd()
$themeText = [IO.File]::ReadAllText($themes).TrimStart()
$homeText = "$tokenText`n`n$themeText`n"

[IO.File]::WriteAllText($wikiHome, $homeText)
Write-Host "Wrote $wikiHome"
Write-Host "Paste this file into the GitHub wiki Home page (or push via git clone of the wiki repo)."
