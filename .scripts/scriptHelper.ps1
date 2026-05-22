#requires -Version 7.0
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path $PSScriptRoot -Parent
$projectName = "rEFIndConfigEditor"
$source = "$repoRoot\$projectName"
$csproj = "$source\$projectName.csproj"
$sln = "$repoRoot\$projectName.sln"
$dotnetFramework = 'net8.0-windows'
$versionFolder = "$repoRoot\.version"
$version = "$versionFolder\version"; $versionContents = ([IO.File]::ReadAllText($version)).Trim()
$versionBuild = "$versionFolder\versionBuild"
$versionTag = "$versionFolder\versionTag"
$versionBuildContents = if (Test-Path -LiteralPath $versionBuild) { ([IO.File]::ReadAllText($versionBuild)).Trim() } else { '' }
$versionTagContents = if (Test-Path -LiteralPath $versionTag) { ([IO.File]::ReadAllText($versionTag)).Trim() } else { '' }

function Set-VersionBuildPlatform {
    param(
        [Parameter(Mandatory)][string]$Platform,
        [string]$LiteralPath = $versionBuild
    )
    [IO.File]::WriteAllText($LiteralPath, $Platform)
}
$readme = "$repoRoot\README.md"
$readmeContents = Get-Content -LiteralPath $readme -Raw
$installerFolder = "$repoRoot\.installer"
$installerOutput = "$installerFolder\Output"
$ISCC = 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe'; if (-not (Test-Path -LiteralPath $ISCC)) { throw "Inno Setup compiler not found: $ISCC" }
$appPublisher = "fosterbarnes"
$appURL = "https://github.com/$appPublisher/$projectName"
$tag = if ([string]::IsNullOrWhiteSpace($versionTagContents)) { "v$versionContents" } else { $versionTagContents }
$ghRepo = "$appPublisher/$projectName"
$appIcon = "$repoRoot\.resources\icon\refind.ico"
$buildNotes = "$repoRoot\.md\.buildNotes.txt"
$publishFolder = "$repoRoot\publish"
$buildTargets = @(
    @{ Architecture = 'x64';   RuntimeIdentifier = 'win-x64';   BinFolder = "$publishFolder\x64";   ExePath = "$publishFolder\x64\$projectName.exe";   InstallerName = "$projectName-x64-installer.exe";   InstallerScript = "$installerFolder\$projectName.x64.installer.iss" }
    @{ Architecture = 'x86';   RuntimeIdentifier = 'win-x86';   BinFolder = "$publishFolder\x86";   ExePath = "$publishFolder\x86\$projectName.exe";   InstallerName = "$projectName-x86-installer.exe";   InstallerScript = "$installerFolder\$projectName.x86.installer.iss" }
    @{ Architecture = 'arm64'; RuntimeIdentifier = 'win-arm64'; BinFolder = "$publishFolder\arm64"; ExePath = "$publishFolder\arm64\$projectName.exe"; InstallerName = "$projectName-arm64-installer.exe"; InstallerScript = "$installerFolder\$projectName.arm64.installer.iss" }
)
