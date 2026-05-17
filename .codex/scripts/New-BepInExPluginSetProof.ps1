[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[a-z0-9][a-z0-9-]*$')]
    [string]$ProfileLabel,

    [Parameter(Mandatory = $true)]
    [ValidateSet('client', 'server')]
    [string]$TargetRole,

    [Parameter(Mandatory = $true)]
    [string[]]$PluginArtifact,

    [string[]]$ConfigOverride = @(),

    [string]$RunRoot = ".codex\runs\bepinex-plugin-set",

    [string]$RunId = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-ExistingFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $resolved = @(Resolve-Path -LiteralPath $Path -ErrorAction Stop)
    if ($resolved.Count -ne 1) {
        throw "Expected one file for '$Path', found $($resolved.Count)."
    }

    $item = Get-Item -LiteralPath $resolved[0].Path
    if (-not $item.PSIsContainer) {
        return $item
    }

    throw "Expected a file path, got directory '$Path'."
}

function Assert-UniqueArtifactFileNames {
    param(
        [Parameter(Mandatory = $true)]
        [System.IO.FileInfo[]]$Artifacts
    )

    $duplicates = @($Artifacts | Group-Object -Property { $_.Name.ToLowerInvariant() } | Where-Object { $_.Count -gt 1 })
    if ($duplicates.Count -eq 0) {
        return
    }

    $messages = @()
    foreach ($duplicate in $duplicates) {
        $messages += "{0}: {1}" -f $duplicate.Group[0].Name, (($duplicate.Group | ForEach-Object { $_.FullName }) -join "; ")
    }

    throw "Duplicate staged plugin filenames are not supported because BepInEx plugin staging would overwrite files. Rename or wrap one artifact, or stage this scenario manually. Duplicates: $($messages -join " | ")"
}

if ([string]::IsNullOrWhiteSpace($RunId)) {
    $RunId = Get-Date -Format "yyyyMMdd-HHmmss"
}

$artifacts = @($PluginArtifact | ForEach-Object { Resolve-ExistingFile -Path $_ })
Assert-UniqueArtifactFileNames -Artifacts $artifacts

$runDirectory = Join-Path $RunRoot (Join-Path $ProfileLabel (Join-Path $TargetRole $RunId))
$stagePluginDirectory = Join-Path $runDirectory "stage\BepInEx\plugins"
New-Item -ItemType Directory -Path $stagePluginDirectory -Force | Out-Null

$stagedArtifacts = @()
foreach ($artifact in $artifacts) {
    $hash = Get-FileHash -LiteralPath $artifact.FullName -Algorithm SHA256
    $target = Join-Path $stagePluginDirectory $artifact.Name
    Copy-Item -LiteralPath $artifact.FullName -Destination $target -Force

    $stagedArtifacts += [ordered]@{
        name = [System.IO.Path]::GetFileNameWithoutExtension($artifact.Name)
        sourcePath = $artifact.FullName
        stagedPath = (Resolve-Path -LiteralPath $target).Path
        sha256 = $hash.Hash
        length = $artifact.Length
        lastWriteTimeUtc = $artifact.LastWriteTimeUtc.ToString("o")
    }
}

$inventoryPath = Join-Path $runDirectory "plugin-inventory.txt"
$stagedArtifacts | ForEach-Object {
    "{0}`t{1}`t{2}" -f $_.name, $_.sha256, $_.stagedPath
} | Set-Content -LiteralPath $inventoryPath -Encoding utf8

$configPath = Join-Path $runDirectory "config-overrides.txt"
if ($ConfigOverride.Count -gt 0) {
    $ConfigOverride | Set-Content -LiteralPath $configPath -Encoding utf8
}
else {
    "No config overrides recorded." | Set-Content -LiteralPath $configPath -Encoding utf8
}

$gitCommit = ""
try {
    $gitCommit = (& git rev-parse HEAD).Trim()
}
catch {
    $gitCommit = "unavailable"
}

$receipt = [ordered]@{
    schema = "bepinex-plugin-set-proof.v1"
    runId = $RunId
    profileLabel = $ProfileLabel
    targetRole = $TargetRole
    createdAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    repository = (Resolve-Path -LiteralPath ".").Path
    gitCommit = $gitCommit
    stagedPluginDirectory = (Resolve-Path -LiteralPath $stagePluginDirectory).Path
    configOverrides = $ConfigOverride
    artifacts = $stagedArtifacts
    result = "not-run"
    resultNotes = "Staging receipt only. Install this plugin set into an isolated BepInEx profile, then collect logs into this run directory."
}

$receiptPath = Join-Path $runDirectory "receipt.json"
$receipt | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $receiptPath -Encoding utf8

$summaryPath = Join-Path $runDirectory "summary.md"
@(
    "# BepInEx Plugin Set Proof"
    ""
    "- Profile: ``$ProfileLabel``"
    "- Target role: ``$TargetRole``"
    "- Result: ``not-run``"
    ""
    "## Next Steps"
    ""
    "1. Install this staged plugin set into the isolated BepInEx target."
    "2. Run the target to the agreed checkpoint."
    "3. Collect logs into this run directory."
    "4. Update ``result`` and ``resultNotes`` in ``receipt.json``."
) | Set-Content -LiteralPath $summaryPath -Encoding utf8

Write-Host "Created BepInEx plugin set proof packet:"
Write-Host "  $runDirectory"
Write-Host "Receipt:"
Write-Host "  $receiptPath"
