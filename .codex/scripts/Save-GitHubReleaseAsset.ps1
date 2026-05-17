[CmdletBinding()]
param(
    [string]$Repository = "",

    [string]$ReleaseUrl = "",

    [string]$Tag = "latest",

    [Parameter(Mandatory = $true)]
    [string]$AssetPattern,

    [string]$OutputDirectory = ".codex\artifacts\mod-releases",

    [switch]$ExtractZip
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-GitHubHeaders {
    $headers = @{
        "Accept" = "application/vnd.github+json"
        "User-Agent" = "Eclipse-ModPairCompatibilityProof"
        "X-GitHub-Api-Version" = "2022-11-28"
    }

    if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_TOKEN)) {
        $headers["Authorization"] = "Bearer $env:GITHUB_TOKEN"
    }

    $headers
}

function Read-ReleaseUrl {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Url
    )

    $match = [regex]::Match($Url, '^https://github\.com/([^/]+)/([^/]+)/releases(?:/tag/([^/?#]+)|/latest)?')
    if (-not $match.Success) {
        throw "ReleaseUrl must look like https://github.com/owner/repo/releases/tag/v1.2.3 or /releases/latest."
    }

    $parsedTag = "latest"
    if ($match.Groups[3].Success -and -not [string]::IsNullOrWhiteSpace($match.Groups[3].Value)) {
        $parsedTag = [System.Uri]::UnescapeDataString($match.Groups[3].Value)
    }

    [ordered]@{
        repository = "$($match.Groups[1].Value)/$($match.Groups[2].Value)"
        tag = $parsedTag
    }
}

function Get-ReleaseApiUri {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepositoryName,

        [Parameter(Mandatory = $true)]
        [string]$ReleaseTag
    )

    if ($ReleaseTag -eq "latest") {
        return "https://api.github.com/repos/$RepositoryName/releases/latest"
    }

    $escapedTag = [System.Uri]::EscapeDataString($ReleaseTag)
    "https://api.github.com/repos/$RepositoryName/releases/tags/$escapedTag"
}

function Resolve-ArtifactDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Root,

        [Parameter(Mandatory = $true)]
        [string]$RepositoryName,

        [Parameter(Mandatory = $true)]
        [string]$ReleaseTag
    )

    $safeRepository = $RepositoryName -replace '[\\/]', '__'
    $safeTag = $ReleaseTag -replace '[^\w\.-]', '_'
    Join-Path $Root (Join-Path $safeRepository $safeTag)
}

if ([string]::IsNullOrWhiteSpace($Repository) -and [string]::IsNullOrWhiteSpace($ReleaseUrl)) {
    throw "Provide either -Repository owner/name or -ReleaseUrl."
}

if (-not [string]::IsNullOrWhiteSpace($ReleaseUrl)) {
    $parsed = Read-ReleaseUrl -Url $ReleaseUrl
    if ([string]::IsNullOrWhiteSpace($Repository)) {
        $Repository = $parsed.repository
    }

    if ($Tag -eq "latest") {
        $Tag = $parsed.tag
    }
}

if ($Repository -notmatch '^[^/\s]+/[^/\s]+$') {
    throw "Repository must be in owner/name form."
}

$releaseApiUri = Get-ReleaseApiUri -RepositoryName $Repository -ReleaseTag $Tag
$headers = Get-GitHubHeaders
$release = Invoke-RestMethod -Method Get -Uri $releaseApiUri -Headers $headers
$assets = @($release.assets | Where-Object { $_.name -like $AssetPattern })

if ($assets.Count -eq 0) {
    $available = @($release.assets | ForEach-Object { $_.name }) -join ", "
    throw "No assets matched '$AssetPattern'. Available assets: $available"
}

if ($assets.Count -gt 1) {
    $matches = @($assets | ForEach-Object { $_.name }) -join ", "
    throw "AssetPattern '$AssetPattern' matched multiple assets: $matches"
}

$asset = $assets[0]
$artifactDirectory = Resolve-ArtifactDirectory -Root $OutputDirectory -RepositoryName $Repository -ReleaseTag $release.tag_name
New-Item -ItemType Directory -Path $artifactDirectory -Force | Out-Null

$artifactPath = Join-Path $artifactDirectory $asset.name
$downloadHeaders = Get-GitHubHeaders
$downloadHeaders["Accept"] = "application/octet-stream"
Invoke-WebRequest -Method Get -Uri $asset.browser_download_url -Headers $downloadHeaders -OutFile $artifactPath

$artifactItem = Get-Item -LiteralPath $artifactPath
$artifactHash = Get-FileHash -LiteralPath $artifactPath -Algorithm SHA256
$extractedFiles = @()

if ($ExtractZip -and ($artifactItem.Extension -ieq ".zip")) {
    $extractDirectory = Join-Path $artifactDirectory ([System.IO.Path]::GetFileNameWithoutExtension($artifactItem.Name))
    if (Test-Path -LiteralPath $extractDirectory) {
        Remove-Item -LiteralPath $extractDirectory -Recurse -Force
    }

    Expand-Archive -LiteralPath $artifactPath -DestinationPath $extractDirectory -Force
    $extractedFiles = @(Get-ChildItem -LiteralPath $extractDirectory -File -Recurse | ForEach-Object {
        $hash = Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256
        [ordered]@{
            path = $_.FullName
            sha256 = $hash.Hash
            length = $_.Length
        }
    })
}

$receipt = [ordered]@{
    schema = "github-release-asset.v1"
    downloadedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    repository = $Repository
    releaseTag = $release.tag_name
    releaseName = $release.name
    releaseUrl = $release.html_url
    assetPattern = $AssetPattern
    assetName = $asset.name
    assetUrl = $asset.browser_download_url
    artifactPath = (Resolve-Path -LiteralPath $artifactPath).Path
    sha256 = $artifactHash.Hash
    length = $artifactItem.Length
    extractedFiles = $extractedFiles
}

$receiptPath = Join-Path $artifactDirectory "github-release-asset.receipt.json"
$receipt | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $receiptPath -Encoding utf8

Write-Host "Downloaded GitHub release asset:"
Write-Host "  $artifactPath"
Write-Host "SHA256:"
Write-Host "  $($artifactHash.Hash)"
Write-Host "Receipt:"
Write-Host "  $receiptPath"

if ($extractedFiles.Count -gt 0) {
    Write-Host "Extracted files:"
    $extractedFiles | ForEach-Object {
        Write-Host "  $($_.path)"
    }
}
