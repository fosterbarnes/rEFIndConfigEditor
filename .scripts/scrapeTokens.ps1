param(
    [string]$SourcePath = '',
    [string]$OutputPath = ''
)

#requires -Version 7.0
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path $PSScriptRoot -Parent
$defaultSource = "$repoRoot\.resources\docs\configfile-source.html"
$defaultOutput = "$repoRoot\.resources\docs\tokens.md"

if ([string]::IsNullOrWhiteSpace($SourcePath)) { $SourcePath = $defaultSource }
if ([string]::IsNullOrWhiteSpace($OutputPath)) { $OutputPath = $defaultOutput }

if (-not (Test-Path -LiteralPath $SourcePath)) {
    throw "Source not found: $SourcePath"
}

function Unescape-TokenText {
    param([string]$Text)
    $t = $Text -replace '\\_', '_'
    $t = $t -replace '\\\*', '*'
    $t = $t -replace '\\\.', '.'
    $t = $t -replace '\\-', '-'
    $t = $t -replace '\\~', '~'
    return $t.Trim()
}

function Normalize-TokenName {
    param([string]$Raw)
    $name = Unescape-TokenText $Raw
    if ($name -match "^dont_scan_\w+") { return ($Matches[0]) }
    if ($name -match "^([a-z][a-z0-9_]*)") { return $Matches[1] }
    return ($name -split '\s+')[0]
}

function Clean-Cell {
    param([string]$Text)
    $t = Unescape-TokenText $Text
    $t = $t -replace 'drivers_ _arch_', 'drivers_<arch>'
    $t = $t -replace '\*\*([^*]+)\*\*', '$1'
    $t = $t -replace '(?<![a-zA-Z0-9_])_([^_]+)_(?![a-zA-Z0-9_])', '$1'
    $t = $t -replace '\[([^\]]+)\]\([^)]+\)', '$1'
    $t = $t -replace '\s+', ' '
    return $t.Trim().TrimEnd('_')
}

function Format-ListExplanation {
    param([string]$Canonical, [string]$Text)
    if ($Canonical -ne 'hideui') { return $Text }
    $t = $Text
    foreach ($item in @('banner', 'label', 'singleuser', 'safemode', 'hwtest', 'arrows', 'hints', 'editor', 'badges', 'all')) {
        $t = $t -replace " $item (removes|disables)", "; $item `$1"
    }
    return ($t -replace '^Removes the specified user interface features:;', 'Removes the specified user interface features:')
}

function Parse-TableRow {
    param([string]$Line)
    if ($Line -notmatch '^\|\s*(.+?)\s*\|\s*(.+?)\s*\|\s*(.+?)\s*\|$') { return $null }
    $tokenRaw = $Matches[1].Trim()
    if ($tokenRaw -match '^-+$' -or $tokenRaw -eq 'Token') { return $null }
    $canonical = Normalize-TokenName $tokenRaw
    return [pscustomobject]@{
        Canonical  = $canonical
        TokenRaw   = $tokenRaw
        Parameters = (Clean-Cell $Matches[2])
        Explanation = (Format-ListExplanation $canonical (Clean-Cell $Matches[3]))
    }
}

$lines = [IO.File]::ReadAllLines($SourcePath)
$sections = [ordered]@{
    'Global options'      = @{ Start = '__**Table 1:'; Rows = [System.Collections.Generic.List[object]]::new() }
    'OS stanza options'   = @{ Start = '__**Table 2:'; Rows = [System.Collections.Generic.List[object]]::new() }
    'Submenu options'     = @{ Start = '__**Table 3:'; Rows = [System.Collections.Generic.List[object]]::new() }
}

$current = $null
foreach ($line in $lines) {
    foreach ($key in $sections.Keys) {
        if ($line -like "$($sections[$key].Start)*") {
            $current = $key
            break
        }
    }
    if ($null -eq $current) { continue }
    if ($line -like '__**Table *' -and $line -notlike "$($sections[$current].Start)*") {
        continue
    }
    $row = Parse-TableRow $line
    if ($row) { $sections[$current].Rows.Add($row) | Out-Null }
}

$dontScanFirmware = 'This token specifies strings that match (case-insensitively) as substrings of EFI firmware boot options that are to be excluded from automatic scanning when scanfor specifies firmware as an option.'
$globalRows = $sections['Global options'].Rows
$hasFirmware = $false
foreach ($r in $globalRows) {
    if ($r.Canonical -eq 'dont_scan_firmware') { $hasFirmware = $true }
}
if (-not $hasFirmware) {
    $globalRows.Add([pscustomobject]@{
        Canonical   = 'dont_scan_firmware'
        TokenRaw    = 'dont_scan_firmware'
        Parameters  = 'substring(s)'
        Explanation = $dontScanFirmware
    }) | Out-Null
}

$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine('# rEFInd configuration tokens')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('> Source: [rEFInd 0.14.2 — Configuring the Boot Manager](https://www.rodsbooks.com/refind/configfile.html)')
[void]$sb.AppendLine('')

$seenAnchors = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)

foreach ($sectionName in $sections.Keys) {
    [void]$sb.AppendLine("## $sectionName")
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('| Token | Parameters | Explanation |')
    [void]$sb.AppendLine('| --- | --- | --- |')
    foreach ($row in $sections[$sectionName].Rows) {
        $anchor = ''
        if ($seenAnchors.Add($row.Canonical)) {
            $anchor = "<a name=`"$($row.Canonical)`"></a>"
        }
        $tokenCell = "$anchor``$($row.Canonical)``"
        [void]$sb.AppendLine("| $tokenCell | $($row.Parameters) | $($row.Explanation) |")
    }
    [void]$sb.AppendLine('')
}

[IO.File]::WriteAllText($OutputPath, $sb.ToString())
Write-Host "Wrote $OutputPath ($($sections['Global options'].Rows.Count) global, $($sections['OS stanza options'].Rows.Count) stanza, $($sections['Submenu options'].Rows.Count) submenu rows)"
