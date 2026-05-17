Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$ReleaseNudgePath = Join-Path $ScriptRoot "release-nudge.ps1"
$BumpVersionPath = Join-Path $ScriptRoot "bump-version.ps1"

function Assert-Equal {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Actual,
        [Parameter(Mandatory = $true)]
        [string]$Expected,
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    if ($Actual -ne $Expected) {
        throw "$Message Expected '$Expected', got '$Actual'."
    }
}

function Assert-Match {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text,
        [Parameter(Mandatory = $true)]
        [string]$Pattern,
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    if ($Text -notmatch $Pattern) {
        throw "$Message Pattern '$Pattern' was not found."
    }
}

function Invoke-Git {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory
    )

    Push-Location $WorkingDirectory
    try {
        & git -c core.autocrlf=false @Arguments | Out-String
        if ($LASTEXITCODE -ne 0) {
            throw "git $($Arguments -join ' ') failed in $WorkingDirectory"
        }
    }
    finally {
        Pop-Location
    }
}

function New-FixtureRepo {
    $FixtureRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("eclipse-release-hygiene-" + [guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $FixtureRoot | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $FixtureRoot ".codex/scripts") -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $FixtureRoot "Systems") -Force | Out-Null

    Copy-Item -Path $ReleaseNudgePath -Destination (Join-Path $FixtureRoot ".codex/scripts/release-nudge.ps1")
    Copy-Item -Path $BumpVersionPath -Destination (Join-Path $FixtureRoot ".codex/scripts/bump-version.ps1")

    Set-Content -Path (Join-Path $FixtureRoot "Eclipse.csproj") -Value @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <Version>1.2.3</Version>
  </PropertyGroup>
</Project>
"@ -NoNewline

    Set-Content -Path (Join-Path $FixtureRoot "thunderstore.toml") -Value @"
[package]
name = "Eclipse"
versionNumber = "1.2.3"
"@ -NoNewline

    Set-Content -Path (Join-Path $FixtureRoot "CHANGELOG.md") -Value @"
## Unreleased

- planned fix

`1.2.3`
- previous release
"@ -NoNewline

    Set-Content -Path (Join-Path $FixtureRoot "Systems/FamiliarHealthChangeSystem.cs") -Value "class FamiliarHealthChangeSystem {}" -NoNewline

    Invoke-Git -WorkingDirectory $FixtureRoot -Arguments @("init", "-b", "main") | Out-Null
    Invoke-Git -WorkingDirectory $FixtureRoot -Arguments @("config", "user.email", "codex@example.invalid") | Out-Null
    Invoke-Git -WorkingDirectory $FixtureRoot -Arguments @("config", "user.name", "Codex") | Out-Null
    Invoke-Git -WorkingDirectory $FixtureRoot -Arguments @("add", ".") | Out-Null
    Invoke-Git -WorkingDirectory $FixtureRoot -Arguments @("commit", "-m", "baseline") | Out-Null

    return $FixtureRoot
}

function Test-BumpVersionUpdatesEclipseMetadata {
    $FixtureRoot = New-FixtureRepo
    try {
        & pwsh -NoProfile -File (Join-Path $FixtureRoot ".codex/scripts/bump-version.ps1") -Version "1.2.4"
        if ($LASTEXITCODE -ne 0) {
            throw "bump-version.ps1 exited with $LASTEXITCODE"
        }

        $ProjectText = Get-Content -Raw -Path (Join-Path $FixtureRoot "Eclipse.csproj")
        $ThunderstoreText = Get-Content -Raw -Path (Join-Path $FixtureRoot "thunderstore.toml")
        $ChangelogText = Get-Content -Raw -Path (Join-Path $FixtureRoot "CHANGELOG.md")

        Assert-Match -Text $ProjectText -Pattern '<Version>1\.2\.4</Version>' -Message "Project version was not updated."
        Assert-Match -Text $ThunderstoreText -Pattern 'versionNumber = "1\.2\.4"' -Message "Thunderstore version was not updated."
        Assert-Match -Text $ChangelogText -Pattern '(?m)^## Unreleased\s+`1\.2\.4`\s+- planned fix' -Message "Changelog release entry was not created."
    }
    finally {
        Remove-Item -LiteralPath $FixtureRoot -Recurse -Force
    }
}

function Test-ReleaseNudgeWarnOnlyFlagsUnreleasedNotes {
    $FixtureRoot = New-FixtureRepo
    try {
        Set-Content -Path (Join-Path $FixtureRoot "Systems/FamiliarHealthChangeSystem.cs") -Value "class FamiliarHealthChangeSystem { void Changed() {} }" -NoNewline

        $Output = & pwsh -NoProfile -File (Join-Path $FixtureRoot ".codex/scripts/release-nudge.ps1") -BaseRef "main" -WarnOnly 2>&1 | Out-String
        if ($LASTEXITCODE -ne 0) {
            throw "release-nudge.ps1 -WarnOnly exited with $LASTEXITCODE"
        }

        Assert-Match -Text $Output -Pattern 'CHANGELOG\.md has Unreleased notes' -Message "Release nudge did not flag unreleased notes."
    }
    finally {
        Remove-Item -LiteralPath $FixtureRoot -Recurse -Force
    }
}

function Test-ReleaseNudgeBlocksWithoutWarnOnly {
    $FixtureRoot = New-FixtureRepo
    try {
        Set-Content -Path (Join-Path $FixtureRoot "Systems/FamiliarHealthChangeSystem.cs") -Value "class FamiliarHealthChangeSystem { void Changed() {} }" -NoNewline

        $Output = & pwsh -NoProfile -File (Join-Path $FixtureRoot ".codex/scripts/release-nudge.ps1") -BaseRef "main" 2>&1 | Out-String
        Assert-Equal -Actual "$LASTEXITCODE" -Expected "1" -Message "Release nudge should block when nudges exist."
        Assert-Match -Text $Output -Pattern 'Eclipse' -Message "Release nudge output should name Eclipse."
    }
    finally {
        Remove-Item -LiteralPath $FixtureRoot -Recurse -Force
    }
}

function Test-ReleaseNudgeUsesGitHubEventBeforeWhenBaseRefOmitted {
    $FixtureRoot = New-FixtureRepo
    $OriginalEventBefore = $env:GITHUB_EVENT_BEFORE
    try {
        $BeforeSha = (Invoke-Git -WorkingDirectory $FixtureRoot -Arguments @("rev-parse", "HEAD")).Trim()
        Set-Content -Path (Join-Path $FixtureRoot "Systems/FamiliarHealthChangeSystem.cs") -Value "class FamiliarHealthChangeSystem { void Changed() {} }" -NoNewline
        Invoke-Git -WorkingDirectory $FixtureRoot -Arguments @("add", ".") | Out-Null
        Invoke-Git -WorkingDirectory $FixtureRoot -Arguments @("commit", "-m", "change system") | Out-Null

        $env:GITHUB_EVENT_BEFORE = $BeforeSha
        $Output = & pwsh -NoProfile -File (Join-Path $FixtureRoot ".codex/scripts/release-nudge.ps1") -WarnOnly 2>&1 | Out-String
        if ($LASTEXITCODE -ne 0) {
            throw "release-nudge.ps1 -WarnOnly exited with $LASTEXITCODE"
        }

        Assert-Match -Text $Output -Pattern 'Eclipse CHANGELOG\.md has Unreleased notes' -Message "Release nudge did not use GITHUB_EVENT_BEFORE when BaseRef was omitted."
    }
    finally {
        $env:GITHUB_EVENT_BEFORE = $OriginalEventBefore
        Remove-Item -LiteralPath $FixtureRoot -Recurse -Force
    }
}

Test-BumpVersionUpdatesEclipseMetadata
Test-ReleaseNudgeWarnOnlyFlagsUnreleasedNotes
Test-ReleaseNudgeBlocksWithoutWarnOnly
Test-ReleaseNudgeUsesGitHubEventBeforeWhenBaseRefOmitted

Write-Host "release-hygiene tests passed"
