[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[a-z0-9][a-z0-9-]*$')]
    [string]$PairLabel,

    [Parameter(Mandatory = $true)]
    [ValidateSet('subject-only', 'peer-only', 'combined-control', 'combined-candidate')]
    [string]$ProofMode,

    [Parameter(Mandatory = $true)]
    [string]$SubjectModName,

    [Parameter(Mandatory = $true)]
    [string]$SubjectModArtifact,

    [Parameter(Mandatory = $true)]
    [string]$PeerModName,

    [Parameter(Mandatory = $true)]
    [string]$PeerModArtifact,

    [string[]]$SupportModArtifact = @(),

    [string[]]$ConfigOverride = @(),

    [string]$RunRoot = ".codex\runs\mod-pair-compatibility",

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

    $item = Get-Item -LiteralPath $resolved.Path
    if (-not $item.PSIsContainer) {
        return $item
    }

    throw "Expected a file path, got directory '$Path'."
}

function Add-Artifact {
    param(
        [Parameter(Mandatory = $true)]
        [System.IO.FileInfo]$Artifact,

        [Parameter(Mandatory = $true)]
        [string]$Role,

        [Parameter(Mandatory = $true)]
        [string]$ModName,

        [Parameter(Mandatory = $true)]
        [string]$PluginDirectory
    )

    $hash = Get-FileHash -LiteralPath $Artifact.FullName -Algorithm SHA256
    $target = Join-Path $PluginDirectory $Artifact.Name
    Copy-Item -LiteralPath $Artifact.FullName -Destination $target -Force

    [ordered]@{
        role = $Role
        name = $ModName
        sourcePath = $Artifact.FullName
        stagedPath = (Resolve-Path -LiteralPath $target).Path
        sha256 = $hash.Hash
        length = $Artifact.Length
        lastWriteTimeUtc = $Artifact.LastWriteTimeUtc.ToString("o")
    }
}

function Assert-UniqueStageFileNames {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$StagePlan
    )

    $duplicates = @($StagePlan | Group-Object -Property { $_.Artifact.Name.ToLowerInvariant() } | Where-Object { $_.Count -gt 1 })
    if ($duplicates.Count -eq 0) {
        return
    }

    $messages = @()
    foreach ($duplicate in $duplicates) {
        $entries = @($duplicate.Group | ForEach-Object {
            "{0}:{1}={2}" -f $_.Role, $_.ModName, $_.Artifact.FullName
        })
        $messages += "{0}: {1}" -f $duplicate.Group[0].Artifact.Name, ($entries -join "; ")
    }

    throw "Duplicate staged plugin filenames are not supported because BepInEx plugin staging would overwrite files. Rename or wrap one artifact, or stage this scenario manually. Duplicates: $($messages -join " | ")"
}

if ([string]::IsNullOrWhiteSpace($RunId)) {
    $RunId = Get-Date -Format "yyyyMMdd-HHmmss"
}

$subjectArtifact = Resolve-ExistingFile -Path $SubjectModArtifact
$peerArtifact = Resolve-ExistingFile -Path $PeerModArtifact
$supportArtifacts = @($SupportModArtifact | ForEach-Object { Resolve-ExistingFile -Path $_ })

$runDirectory = Join-Path $RunRoot (Join-Path $PairLabel (Join-Path $ProofMode $RunId))
$stagePluginDirectory = Join-Path $runDirectory "stage\BepInEx\plugins"
New-Item -ItemType Directory -Path $stagePluginDirectory -Force | Out-Null

$stagePlan = @()
if (($ProofMode -eq "subject-only") -or ($ProofMode -eq "combined-control") -or ($ProofMode -eq "combined-candidate")) {
    $stagePlan += [pscustomobject]@{
        Artifact = $subjectArtifact
        Role = "subjectMod"
        ModName = $SubjectModName
    }
}

if (($ProofMode -eq "peer-only") -or ($ProofMode -eq "combined-control") -or ($ProofMode -eq "combined-candidate")) {
    $stagePlan += [pscustomobject]@{
        Artifact = $peerArtifact
        Role = "peerMod"
        ModName = $PeerModName
    }
}

foreach ($supportArtifact in $supportArtifacts) {
    $supportName = [System.IO.Path]::GetFileNameWithoutExtension($supportArtifact.Name)
    $stagePlan += [pscustomobject]@{
        Artifact = $supportArtifact
        Role = "supportMod"
        ModName = $supportName
    }
}

Assert-UniqueStageFileNames -StagePlan $stagePlan

$artifacts = @()
foreach ($stageItem in $stagePlan) {
    $artifacts += Add-Artifact -Artifact $stageItem.Artifact -Role $stageItem.Role -ModName $stageItem.ModName -PluginDirectory $stagePluginDirectory
}

$inventoryPath = Join-Path $runDirectory "plugin-inventory.txt"
$artifacts | ForEach-Object {
    "{0}`t{1}`t{2}`t{3}" -f $_.role, $_.name, $_.sha256, $_.stagedPath
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
    schema = "mod-pair-compatibility-proof.v1"
    runId = $RunId
    pairLabel = $PairLabel
    proofMode = $ProofMode
    createdAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    repository = (Resolve-Path -LiteralPath ".").Path
    gitCommit = $gitCommit
    subjectMod = [ordered]@{
        name = $SubjectModName
        artifactPath = $subjectArtifact.FullName
    }
    peerMod = [ordered]@{
        name = $PeerModName
        artifactPath = $peerArtifact.FullName
    }
    supportMods = @($supportArtifacts | ForEach-Object {
        [ordered]@{
            name = [System.IO.Path]::GetFileNameWithoutExtension($_.Name)
            artifactPath = $_.FullName
        }
    })
    stagedPluginDirectory = (Resolve-Path -LiteralPath $stagePluginDirectory).Path
    configOverrides = $ConfigOverride
    artifacts = $artifacts
    result = "not-run"
    resultNotes = "Staging receipt only. Launch the client from an isolated profile, then copy logs into this run directory."
}

$receiptPath = Join-Path $runDirectory "receipt.json"
$receipt | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $receiptPath -Encoding utf8

$summaryPath = Join-Path $runDirectory "summary.md"
@(
    "# Mod Pair Compatibility Proof"
    ""
    "- Pair: ``$PairLabel``"
    "- Proof mode: ``$ProofMode``"
    "- Subject mod: ``$SubjectModName``"
    "- Peer mod: ``$PeerModName``"
    "- Result: ``not-run``"
    ""
    "## Next Steps"
    ""
    "1. Copy or point this staged profile at an isolated client test environment."
    "2. Launch the client and reach the agreed checkpoint."
    "3. Copy the BepInEx log into this run directory."
    "4. Update ``result`` and ``resultNotes`` in ``receipt.json``."
) | Set-Content -LiteralPath $summaryPath -Encoding utf8

Write-Host "Created mod pair compatibility proof packet:"
Write-Host "  $runDirectory"
Write-Host "Receipt:"
Write-Host "  $receiptPath"
