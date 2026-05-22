param(
    [string]$SupportedList = '',
    [string]$OutputMd = '',
    [string]$OutputJson = '',
    [string]$PreviewDir = '',
    [switch]$AllWithImages,
    [switch]$MetadataOnly,
    [string]$GitHubToken = ''
)

#requires -Version 7.0
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path $PSScriptRoot -Parent
$defaultSupported = "$repoRoot\.resources\data\supported-themes.txt"
$defaultOutputMd = "$repoRoot\.resources\docs\themes.md"
$defaultOutputJson = "$repoRoot\.resources\data\themes.json"
$defaultPreviewDir = "$repoRoot\.resources\themes\previews"

if ([string]::IsNullOrWhiteSpace($SupportedList)) { $SupportedList = $defaultSupported }
if ([string]::IsNullOrWhiteSpace($OutputMd)) { $OutputMd = $defaultOutputMd }
if ([string]::IsNullOrWhiteSpace($OutputJson)) { $OutputJson = $defaultOutputJson }
if ([string]::IsNullOrWhiteSpace($PreviewDir)) { $PreviewDir = $defaultPreviewDir }

$upstreamUrl = 'https://raw.githubusercontent.com/martinmilani/rEFInd-theme-collection/main/src/data/themes.json'
$collectionRawBase = 'https://raw.githubusercontent.com/martinmilani/rEFInd-theme-collection/main'

function Normalize-GithubLink {
    param([string]$Url)
    $u = $Url.Trim().TrimEnd('/')
    $q = $u.IndexOf('?')
    if ($q -ge 0) { $u = $u.Substring(0, $q) }
    if ($u -match '^https://github\.com/([^/]+/[^/]+)$') {
        return "https://github.com/$($Matches[1])"
    }
    return $u
}

function Get-RepoSlug {
    param([string]$GithubLink)
    if ($GithubLink -match 'github\.com/([^/]+)/([^/]+)$') {
        return "$($Matches[1])-$($Matches[2])"
    }
    throw "Could not derive slug from: $GithubLink"
}

function Escape-MdCell {
    param([string]$Text)
    if ([string]::IsNullOrEmpty($Text)) { return '' }
    return ($Text -replace '\|', '\|').Trim()
}

function Format-Iso8601 {
    param([object]$Value)

    if ($null -eq $Value -or [string]::IsNullOrWhiteSpace([string]$Value)) { return '' }

    if ($Value -is [DateTime]) {
        return $Value.ToUniversalTime().ToString('o')
    }

    if ($Value -is [DateTimeOffset]) {
        return $Value.ToUniversalTime().ToString('o')
    }

    $text = [string]$Value
    $parsed = [DateTimeOffset]::MinValue
    if ([DateTimeOffset]::TryParse($text, [ref]$parsed)) {
        return $parsed.ToUniversalTime().ToString('o')
    }

    return $text
}

function Get-GithubRepoMeta {
    param(
        [string]$GithubLink,
        [switch]$Skip
    )

    if ($Skip) { return $null }

    if ($GithubLink -notmatch 'github\.com/([^/]+)/([^/]+)$') { return $null }

    $owner = $Matches[1]
    $repo = $Matches[2]
    $headers = @{ 'User-Agent' = 'rEFIndConfigEditor-theme-scraper' }
    if (-not [string]::IsNullOrWhiteSpace($GitHubToken)) {
        $headers['Authorization'] = "Bearer $GitHubToken"
    }

    try {
        $response = Invoke-RestMethod -Uri "https://api.github.com/repos/$owner/$repo" -Headers $headers -Method Get
        if ([string]::IsNullOrWhiteSpace($GitHubToken)) {
            Start-Sleep -Milliseconds 1100
        }
        return [ordered]@{
            stars   = [int]$response.stargazers_count
            created = [string]$response.created_at
        }
    }
    catch {
        Write-Warning "GitHub metadata failed for $GithubLink : $($_.Exception.Message)"
        return $null
    }
}

function Get-ThemeTraits {
    param(
        [string]$Name,
        [string]$Description,
        [string]$Slug,
        [string]$Github
    )

    $repoName = if ($Github -match 'github\.com/[^/]+/([^/]+)$') { $Matches[1] } else { '' }
    $text = "$Name $Description $Slug $repoName".ToLowerInvariant()

    $isLight = $false
    if ($text -match '(?<![a-z])light(?![a-z])' -and $text -notmatch 'highlight|spotlight') {
        if ($text -notmatch '(?<![a-z])dark(?![a-z])') { $isLight = $true }
    }
    if ($Slug -match 'refindTTL|refindttl' -or $Name -match 'refindTTL') { $isLight = $true }

    $isDark = $text -match '(?<![a-z])dark(?![a-z])|(?<![a-z])black(?![a-z])|oled|no.?blind|ronbm|darkmini|tokyo.?night|\bnord\b|synthwave|cyberpunk|matrix|shadow|midnight|(?<![a-z])night(?![a-z])|catppuccin|gruvbox|neon|fallout|demon.?slayer|batman|retro.?game|wildside|chalkboard|terminal|minimal.?dark|regular.?dark|custom.?dark|hi-dark|bsd.?black|grey-apple|(?<![a-z])snow(?![a-z])|sublime|fluent.?dark|nox|indulgence|glow|killign|metal.?frame|icon.?set|shadow-r'

    $isMinimal = $text -match 'minimal|minimalist|(?<![a-z])clean(?![a-z])|(?<![a-z])simple(?![a-z])|(?<![a-z])flat(?![a-z])|theme-regular|ambience|glassy|efifetch|monochrome|ultra|no labels|stunningly clean|simplistic|regular-minimalism|minimalistic|metro'

    if ($text -match 'star.?wars|light side.*dark side') {
        $isDark = $true
        $isLight = $true
    }
    if ($text -match 'elementary|(?<![a-z])dawn(?![a-z])|grey-apple|refindttl|tricky transparencies light') {
        $isLight = $true
    }
    if ($Slug -match 'refind-black|killign-rEFInd|RSWilli-refind-custom') {
        $isLight = $false
    }

    $tags = [System.Collections.Generic.List[string]]::new()
    if ($text -match 'oled|no.?blind|ronbm|theme-oled') { [void]$tags.Add('oled') }
    if ($text -match 'anime|demon.?slayer|genshin|star.?wars|batman|fallout|matrix|pachirisu|retro.?game|synthwave|phigros|retro game|pixel|alpaca|black.?cat|phi|pokemon|celeste|rog') {
        [void]$tags.Add('gaming')
    }
    if ($text -match 'gruvbox|nord|catppuccin|tokyo.?night|material|elementary|fluent|maia|color.?scheme|colourful|colorful|pastel|sublime|next-theme|celestial|details') {
        [void]$tags.Add('color-scheme')
    }
    if ($text -match 'icon set|icon-set|chris1111|shadow-ios|metal.?frame|darkgrey|shadow-refind') {
        [void]$tags.Add('icon-set')
    }
    if ($text -match 'mountain|deer|fireflies|planets|ursa|ambience|sunset|(?<![a-z])snow(?![a-z])|splash|dawn|harbour|harbor|pool|nature|scenic|digital.?void') {
        [void]$tags.Add('scenic')
    }
    if ($text -match 'synthwave|retro|neon|pixel|retro.?game|terminal|matrix|efifetch|batman.?neon') {
        [void]$tags.Add('retro')
    }
    if ($text -match 'multi-theme|all-themes|hi-themes|minimal-themes|refind-themes|linux-boot-efi|zeeshan933') {
        [void]$tags.Add('multi-pack')
    }

    return [ordered]@{
        isDark    = [bool]$isDark
        isLight   = [bool]$isLight
        isMinimal = [bool]$isMinimal
        tags      = @($tags | Select-Object -Unique)
    }
}

function New-ManifestEntry {
    param(
        $Theme,
        [string]$Github,
        [string]$Slug,
        [string]$DisplayName,
        [string]$PreviewRel,
        $UpstreamTheme,
        [int]$ExistingStars = -1,
        [string]$ExistingCreated = ''
    )

    $traits = Get-ThemeTraits -Name $DisplayName -Description ([string]$Theme.description) -Slug $Slug -Github $Github
    $skipGithub = $ExistingStars -gt 0
    $githubMeta = Get-GithubRepoMeta -GithubLink $Github -Skip:$skipGithub

    $created = Format-Iso8601 $ExistingCreated
    if ($UpstreamTheme -and $UpstreamTheme.creation_date) {
        $created = Format-Iso8601 $UpstreamTheme.creation_date
    }
    if ($githubMeta -and $githubMeta.created) {
        $created = Format-Iso8601 $githubMeta.created
    }

    $recentlyAdded = $false
    if ($UpstreamTheme -and $null -ne $UpstreamTheme.recently_added) {
        $recentlyAdded = [bool]$UpstreamTheme.recently_added
    }

    $stars = if ($ExistingStars -gt 0) { $ExistingStars }
        elseif ($githubMeta) { [int]$githubMeta.stars }
        else { 0 }

    return [ordered]@{
        id            = [string]$Theme.id
        slug          = $Slug
        name          = $DisplayName
        author        = [string]$Theme.user
        description   = [string]$Theme.description
        github        = $Github
        preview       = $PreviewRel
        githubStars   = $stars
        created       = $created
        recentlyAdded = $recentlyAdded
        isDark        = [bool]$traits.isDark
        isLight       = [bool]$traits.isLight
        isMinimal     = [bool]$traits.isMinimal
        tags          = @($traits.tags)
    }
}

function Convert-WebpToPng {
    param([string]$WebpPath, [string]$PngPath)

    Add-Type -AssemblyName PresentationCore
    Add-Type -AssemblyName WindowsBase

    $stream = $null
    $frame = $null
    $fileStream = $null
    try {
        $fileStream = [IO.File]::OpenRead($WebpPath)
        $frame = [System.Windows.Media.Imaging.BitmapFrame]::Create(
            $fileStream,
            [System.Windows.Media.Imaging.BitmapCreateOptions]::PreservePixelFormat,
            [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
        $frame.Freeze()

        $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
        $encoder.Frames.Add($frame)
        $outStream = New-Object IO.FileStream($PngPath, [IO.FileMode]::Create)
        try {
            $encoder.Save($outStream)
        }
        finally {
            $outStream.Dispose()
        }
    }
    finally {
        if ($fileStream) { $fileStream.Dispose() }
    }
}

$supportedLinks = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$supportedListLines = [System.Collections.Generic.List[string]]::new()

Write-Host "Fetching upstream themes.json..."
$upstream = Invoke-RestMethod -Uri $upstreamUrl -Method Get

if ($MetadataOnly) {
    if (-not (Test-Path -LiteralPath $OutputJson)) {
        throw "Catalog JSON not found: $OutputJson"
    }

    $existing = Get-Content -LiteralPath $OutputJson -Raw | ConvertFrom-Json
    $enriched = [System.Collections.Generic.List[object]]::new()

    foreach ($entry in $existing) {
        $github = Normalize-GithubLink ([string]$entry.github)
        $upstreamMatch = $upstream | Where-Object {
            (Normalize-GithubLink ([string]$_.link)) -eq $github
        } | Select-Object -First 1

        $themeShape = [ordered]@{
            id          = [string]$entry.id
            user        = [string]$entry.author
            description = [string]$entry.description
        }
        if ($upstreamMatch) {
            $themeShape['user'] = [string]$upstreamMatch.user
            $themeShape['description'] = [string]$upstreamMatch.description
        }

        $enriched.Add((New-ManifestEntry `
            -Theme $themeShape `
            -Github $github `
            -Slug ([string]$entry.slug) `
            -DisplayName ([string]$entry.name) `
            -PreviewRel ([string]$entry.preview) `
            -UpstreamTheme $upstreamMatch `
            -ExistingStars ([int]$entry.githubStars) `
            -ExistingCreated ([string]$entry.created))) | Out-Null
    }

    $enrichedJson = $enriched | ConvertTo-Json -Depth 6
    [IO.File]::WriteAllText($OutputJson, $enrichedJson)
    Write-Host "Updated metadata for $($enriched.Count) themes in $OutputJson"
    return
}

if ($AllWithImages) {
    foreach ($theme in $upstream) {
        if (-not ($theme.images -and $theme.images.Count -gt 0)) { continue }
        $link = Normalize-GithubLink ([string]$theme.link)
        if ($supportedLinks.Add($link)) {
            $supportedListLines.Add($link) | Out-Null
        }
    }

    if ($supportedListLines.Count -eq 0) {
        throw 'No upstream themes with preview images found.'
    }

    $supportedHeader = @(
        '# One GitHub repo URL per line. Regenerate with: pwsh .scripts/scrapeThemes.ps1 -AllWithImages'
        ''
    )
    [IO.File]::WriteAllLines($SupportedList, $supportedHeader + $supportedListLines)
    Write-Host "Wrote $($supportedListLines.Count) URLs to $SupportedList"
}
else {
    if (-not (Test-Path -LiteralPath $SupportedList)) {
        throw "Supported list not found: $SupportedList"
    }

    foreach ($line in Get-Content -LiteralPath $SupportedList) {
        $trimmed = $line.Trim()
        if ($trimmed.Length -eq 0 -or $trimmed.StartsWith('#')) { continue }
        $link = Normalize-GithubLink $trimmed
        if ($supportedLinks.Add($link)) {
            $supportedListLines.Add($link) | Out-Null
        }
    }
}

if ($supportedLinks.Count -eq 0) {
    throw 'No supported theme URLs found in list.'
}

$ordered = [System.Collections.Generic.List[object]]::new()
foreach ($url in $supportedListLines) {
    $norm = Normalize-GithubLink $url
    $match = $upstream | Where-Object {
        (Normalize-GithubLink ([string]$_.link)) -eq $norm -and $_.images -and $_.images.Count -gt 0
    } | Select-Object -First 1
    if ($match) { $ordered.Add($match) | Out-Null }
}

if ($ordered.Count -ne $supportedLinks.Count) {
    $found = $ordered | ForEach-Object { Normalize-GithubLink ([string]$_.link) }
    $missing = @($supportedLinks | Where-Object { $_ -notin $found })
    if ($missing.Count -gt 0) {
        throw "Upstream JSON missing supported theme(s) with previews: $($missing -join ', ')"
    }
}

$null = New-Item -ItemType Directory -Force -Path (Split-Path $OutputMd -Parent)
$null = New-Item -ItemType Directory -Force -Path (Split-Path $OutputJson -Parent)
$null = New-Item -ItemType Directory -Force -Path $PreviewDir

$manifest = [System.Collections.Generic.List[object]]::new()
$previewCount = 0

foreach ($theme in $ordered) {
    $github = Normalize-GithubLink ([string]$theme.link)
    $slug = Get-RepoSlug $github
    $displayName = [string]$theme.name
    if ($github -match 'github\.com/catppuccin/refind$') {
        $displayName = 'catppuccin/refind'
    }

    $previewRel = "previews/$slug.png"
    $previewPath = Join-Path $PreviewDir "$slug.png"
    $downloaded = $false

    if ($theme.images -and $theme.images.Count -gt 0) {
        foreach ($image in $theme.images) {
            $assetPath = ([string]$image).TrimStart('/')
            $webpUrl = "$collectionRawBase/$assetPath"
            $tempWebp = Join-Path $env:TEMP "refind-theme-$slug.webp"

            try {
                Write-Host "Downloading preview for $slug..."
                Invoke-WebRequest -Uri $webpUrl -OutFile $tempWebp -UseBasicParsing
                Convert-WebpToPng -WebpPath $tempWebp -PngPath $previewPath
                Remove-Item -LiteralPath $tempWebp -Force -ErrorAction SilentlyContinue
                $downloaded = $true
                $previewCount++
                break
            }
            catch {
                Write-Warning "Preview download failed for $slug ($webpUrl): $($_.Exception.Message)"
                Remove-Item -LiteralPath $tempWebp -Force -ErrorAction SilentlyContinue
            }
        }
    }

    if (-not $downloaded) {
        Write-Warning "Skipping $slug — no preview could be downloaded."
        continue
    }

    $manifest.Add((New-ManifestEntry `
        -Theme $theme `
        -Github $github `
        -Slug $slug `
        -DisplayName $displayName `
        -PreviewRel $previewRel `
        -UpstreamTheme $theme)) | Out-Null
}

$manifestJson = $manifest | ConvertTo-Json -Depth 6
[IO.File]::WriteAllText($OutputJson, $manifestJson)

$supportedHeader = @(
    '# One GitHub repo URL per line. Regenerate all with previews: pwsh .scripts/scrapeThemes.ps1 -AllWithImages'
    ''
)
$manifestUrls = $manifest | ForEach-Object { [string]$_.github }
[IO.File]::WriteAllLines($SupportedList, $supportedHeader + $manifestUrls)

$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine('# rEFInd supported themes')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('> Source: [rEFInd Theme Collection](https://refind-themes-collection.netlify.app/) ([themes.json](https://github.com/martinmilani/rEFInd-theme-collection/blob/main/src/data/themes.json))')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('| Theme | Author | Description | Repository |')
[void]$sb.AppendLine('| --- | --- | --- | --- |')

foreach ($entry in $manifest) {
    $themeCell = "``$($entry.name)``"
    $desc = Escape-MdCell $entry.description
    $repoCell = "[GitHub]($($entry.github))"
    [void]$sb.AppendLine("| $themeCell | $($entry.author) | $desc | $repoCell |")
}
[void]$sb.AppendLine('')

[IO.File]::WriteAllText($OutputMd, $sb.ToString())

$activeSlugs = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($entry in $manifest) { [void]$activeSlugs.Add([string]$entry.slug) }
foreach ($stale in [IO.Directory]::GetFiles($PreviewDir, '*.png')) {
    $base = [IO.Path]::GetFileNameWithoutExtension($stale)
    if (-not $activeSlugs.Contains($base)) {
        Remove-Item -LiteralPath $stale -Force
        Write-Host "Removed stale preview: $base.png"
    }
}

Write-Host "Wrote $OutputMd ($($manifest.Count) rows), $OutputJson, $previewCount previews"
