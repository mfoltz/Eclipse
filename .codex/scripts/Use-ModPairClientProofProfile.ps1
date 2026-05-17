[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Install', 'Collect', 'Restore')]
    [string]$Action,

    [string]$ClientRoot = "C:\Program Files (x86)\Steam\steamapps\common\VRisingCodex",

    [string]$ExpectedExecutableName = "VRising.exe",

    [string]$ProofRunDirectory = "",

    [string]$BackupDirectory = "",

    [string]$EvidenceLabel = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-Directory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $resolved = @(Resolve-Path -LiteralPath $Path -ErrorAction Stop)
    if ($resolved.Count -ne 1) {
        throw "Expected one directory for '$Path', found $($resolved.Count)."
    }

    $item = Get-Item -LiteralPath $resolved[0].Path
    if ($item.PSIsContainer) {
        return $item
    }

    throw "Expected directory '$Path'."
}

function Assert-ChildPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Root,

        [Parameter(Mandatory = $true)]
        [string]$Child
    )

    $rootPath = [System.IO.Path]::GetFullPath($Root).TrimEnd('\')
    $childPath = [System.IO.Path]::GetFullPath($Child)
    if (-not $childPath.StartsWith($rootPath + "\", [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing path outside root '$rootPath': $childPath"
    }
}

function Copy-DirectoryContents {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Source,

        [Parameter(Mandatory = $true)]
        [string]$Destination
    )

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    if (Test-Path -LiteralPath $Source) {
        Get-ChildItem -LiteralPath $Source -Force | ForEach-Object {
            Copy-Item -LiteralPath $_.FullName -Destination $Destination -Recurse -Force
        }
    }
}

function Clear-DirectoryContents {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    New-Item -ItemType Directory -Path $Path -Force | Out-Null
    Get-ChildItem -LiteralPath $Path -Force | ForEach-Object {
        Remove-Item -LiteralPath $_.FullName -Recurse -Force
    }
}

function Set-IniValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Section,

        [Parameter(Mandatory = $true)]
        [string]$Key,

        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $lines = @()
    if (Test-Path -LiteralPath $Path) {
        $lines = @(Get-Content -LiteralPath $Path)
    }

    $sectionHeader = "[$Section]"
    $sectionIndex = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i].Trim() -eq $sectionHeader) {
            $sectionIndex = $i
            break
        }
    }

    if ($sectionIndex -lt 0) {
        if ($lines.Count -gt 0 -and $lines[$lines.Count - 1] -ne "") {
            $lines += ""
        }

        $lines += $sectionHeader
        $lines += "$Key = $Value"
        $lines | Set-Content -LiteralPath $Path -Encoding utf8
        return
    }

    $insertIndex = $lines.Count
    for ($i = $sectionIndex + 1; $i -lt $lines.Count; $i++) {
        $trimmed = $lines[$i].Trim()
        if ($trimmed.StartsWith("[") -and $trimmed.EndsWith("]")) {
            $insertIndex = $i
            break
        }

        $escapedKey = [regex]::Escape($Key)
        if ($trimmed -match "^$escapedKey\s*=") {
            $lines[$i] = "$Key = $Value"
            $lines | Set-Content -LiteralPath $Path -Encoding utf8
            return
        }
    }

    $before = @()
    $after = @()
    if ($insertIndex -gt 0) {
        $before = $lines[0..($insertIndex - 1)]
    }

    if ($insertIndex -lt $lines.Count) {
        $after = $lines[$insertIndex..($lines.Count - 1)]
    }

    @($before + "$Key = $Value" + $after) | Set-Content -LiteralPath $Path -Encoding utf8
}

function ConvertTo-ConfigOverride {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Override,

        [Parameter(Mandatory = $true)]
        [string]$ConfigDirectory
    )

    $parts = $Override.Split(':', 3)
    if ($parts.Count -ne 3) {
        throw "Invalid config override '$Override'. Expected ModName:ConfigFile:Section.Key=value."
    }

    $configFile = $parts[1]
    $setting = $parts[2]
    $settingParts = $setting.Split('=', 2)
    if ($settingParts.Count -ne 2) {
        throw "Invalid config setting '$setting'. Expected Section.Key=value."
    }

    $pathParts = $settingParts[0].Split('.', 2)
    if ($pathParts.Count -ne 2) {
        throw "Invalid config key '$($settingParts[0])'. Expected Section.Key."
    }

    $configPath = Join-Path $ConfigDirectory $configFile
    Assert-ChildPath -Root $ConfigDirectory -Child $configPath

    [pscustomobject]@{
        Raw = $Override
        ConfigPath = $configPath
        Section = $pathParts[0]
        Key = $pathParts[1]
        Value = $settingParts[1]
    }
}

function Apply-ConfigOverride {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Override
    )

    Set-IniValue -Path $Override.ConfigPath -Section $Override.Section -Key $Override.Key -Value $Override.Value
}

function Get-ActiveStatePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ClientRootPath
    )

    Join-Path $ClientRootPath ".codex-client-proof-active.json"
}

$clientRootItem = Resolve-Directory -Path $ClientRoot
$clientRootPath = $clientRootItem.FullName
$pluginsDirectory = Join-Path $clientRootPath "BepInEx\plugins"
$configDirectory = Join-Path $clientRootPath "BepInEx\config"
$logOutputPath = Join-Path $clientRootPath "BepInEx\LogOutput.log"
$errorLogPath = Join-Path $clientRootPath "BepInEx\ErrorLog.log"
$activeStatePath = Get-ActiveStatePath -ClientRootPath $clientRootPath

if (-not (Test-Path -LiteralPath (Join-Path $clientRootPath $ExpectedExecutableName))) {
    throw "BepInEx target root does not contain ${ExpectedExecutableName}: $clientRootPath"
}

New-Item -ItemType Directory -Path $pluginsDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $configDirectory -Force | Out-Null

if ($Action -eq "Install") {
    if ([string]::IsNullOrWhiteSpace($ProofRunDirectory)) {
        throw "-ProofRunDirectory is required for Install."
    }

    if (Test-Path -LiteralPath $activeStatePath) {
        throw "An active proof profile already exists. Run Restore first: $activeStatePath"
    }

    $proofRunItem = Resolve-Directory -Path $ProofRunDirectory
    $proofRunPath = $proofRunItem.FullName
    $stagePluginsDirectory = Join-Path $proofRunPath "stage\BepInEx\plugins"
    $configOverridesPath = Join-Path $proofRunPath "config-overrides.txt"
    $receiptPath = Join-Path $proofRunPath "receipt.json"

    if (-not (Test-Path -LiteralPath $stagePluginsDirectory)) {
        throw "Proof run does not contain staged plugins: $stagePluginsDirectory"
    }

    if (-not (Test-Path -LiteralPath $receiptPath)) {
        throw "Proof run does not contain receipt.json: $receiptPath"
    }

    $configOverrides = @()
    $configOverrideRecords = @()
    if (Test-Path -LiteralPath $configOverridesPath) {
        $configOverrides = @(Get-Content -LiteralPath $configOverridesPath | Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and $_ -ne "No config overrides recorded." })
        foreach ($override in $configOverrides) {
            $configOverrideRecords += ConvertTo-ConfigOverride -Override $override -ConfigDirectory $configDirectory
        }
    }

    $runId = Split-Path -Leaf $proofRunPath
    $backupRoot = Join-Path $clientRootPath ".codex-client-proof-backups"
    $backupPath = Join-Path $backupRoot $runId
    Assert-ChildPath -Root $clientRootPath -Child $backupPath

    $state = [ordered]@{
        schema = "mod-pair-client-proof-profile.v1"
        action = "Install"
        installedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
        clientRoot = $clientRootPath
        proofRunDirectory = $proofRunPath
        backupDirectory = $backupPath
        stagedPluginDirectory = $stagePluginsDirectory
        configOverrides = $configOverrides
    }

    New-Item -ItemType Directory -Path $backupPath -Force | Out-Null
    Copy-DirectoryContents -Source $pluginsDirectory -Destination (Join-Path $backupPath "plugins")
    Copy-DirectoryContents -Source $configDirectory -Destination (Join-Path $backupPath "config")

    $state | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $activeStatePath -Encoding utf8

    Clear-DirectoryContents -Path $pluginsDirectory
    Copy-DirectoryContents -Source $stagePluginsDirectory -Destination $pluginsDirectory

    foreach ($overrideRecord in $configOverrideRecords) {
        Apply-ConfigOverride -Override $overrideRecord
    }

    Write-Host "Installed proof profile into:"
    Write-Host "  $clientRootPath"
    Write-Host "Backup:"
    Write-Host "  $backupPath"
    Write-Host "Active state:"
    Write-Host "  $activeStatePath"
    return
}

if ($Action -eq "Collect") {
    if ([string]::IsNullOrWhiteSpace($ProofRunDirectory)) {
        if (-not (Test-Path -LiteralPath $activeStatePath)) {
            throw "-ProofRunDirectory is required when no active profile state exists."
        }

        $state = Get-Content -LiteralPath $activeStatePath -Raw | ConvertFrom-Json
        $ProofRunDirectory = $state.proofRunDirectory
    }

    $proofRunItem = Resolve-Directory -Path $ProofRunDirectory
    $proofRunPath = $proofRunItem.FullName
    $evidenceDirectoryName = "client-logs"
    if (-not [string]::IsNullOrWhiteSpace($EvidenceLabel)) {
        if ($EvidenceLabel -notmatch '^[a-z0-9][a-z0-9-]*$') {
            throw "EvidenceLabel must be lowercase letters, numbers, or hyphens."
        }

        $evidenceDirectoryName = "client-logs-$EvidenceLabel"
    }

    $collectedDirectory = Join-Path $proofRunPath $evidenceDirectoryName
    New-Item -ItemType Directory -Path $collectedDirectory -Force | Out-Null

    if (Test-Path -LiteralPath $logOutputPath) {
        Copy-Item -LiteralPath $logOutputPath -Destination (Join-Path $collectedDirectory "LogOutput.log") -Force
    }

    if (Test-Path -LiteralPath $errorLogPath) {
        Copy-Item -LiteralPath $errorLogPath -Destination (Join-Path $collectedDirectory "ErrorLog.log") -Force
    }

    Copy-DirectoryContents -Source $configDirectory -Destination (Join-Path $collectedDirectory "config")
    Write-Host "Collected client proof evidence into:"
    Write-Host "  $collectedDirectory"
    return
}

if ($Action -eq "Restore") {
    if ([string]::IsNullOrWhiteSpace($BackupDirectory)) {
        if (-not (Test-Path -LiteralPath $activeStatePath)) {
            throw "-BackupDirectory is required when no active profile state exists."
        }

        $state = Get-Content -LiteralPath $activeStatePath -Raw | ConvertFrom-Json
        $BackupDirectory = $state.backupDirectory
    }

    $backupItem = Resolve-Directory -Path $BackupDirectory
    $backupPath = $backupItem.FullName
    Assert-ChildPath -Root $clientRootPath -Child $backupPath

    $backupPlugins = Join-Path $backupPath "plugins"
    $backupConfig = Join-Path $backupPath "config"

    Clear-DirectoryContents -Path $pluginsDirectory
    Clear-DirectoryContents -Path $configDirectory
    Copy-DirectoryContents -Source $backupPlugins -Destination $pluginsDirectory
    Copy-DirectoryContents -Source $backupConfig -Destination $configDirectory

    if (Test-Path -LiteralPath $activeStatePath) {
        Remove-Item -LiteralPath $activeStatePath -Force
    }

    Write-Host "Restored client profile from:"
    Write-Host "  $backupPath"
}
