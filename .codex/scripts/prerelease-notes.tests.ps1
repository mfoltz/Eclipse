Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$PrereleaseNotesPath = Join-Path $ScriptRoot "prerelease-notes.sh"
$BashPath = "C:\Program Files\Git\bin\bash.exe"

if (-not (Test-Path -LiteralPath $BashPath)) {
    throw "Git Bash was not found at $BashPath."
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
        throw $Message
    }
}

function New-Fixture {
    $FixtureRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("eclipse-prerelease-notes-" + [guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $FixtureRoot | Out-Null
    return $FixtureRoot
}

function Invoke-PrereleaseNotes {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & $BashPath $PrereleaseNotesPath @Arguments 2>&1 | Out-String
}

function Test-PrereleaseNotesIncludesChangelogAndDetailsCard {
    $FixtureRoot = New-Fixture
    try {
        $ChangelogPath = Join-Path $FixtureRoot "CHANGELOG.md"
        $OutputPath = Join-Path $FixtureRoot "prerelease-notes.md"
        Set-Content -Path $ChangelogPath -Value @'
## Unreleased

`1.2.3`
- fixed the client widget timing
- added a runtime receipt

`1.2.2`
- previous release
'@

        $Output = Invoke-PrereleaseNotes -Arguments @(
            "--changelog", $ChangelogPath,
            "--version", "1.2.3",
            "--tag", "v1.2.3-pre",
            "--branch", "main",
            "--commit", "1234567890abcdef",
            "--run-id", "42",
            "--output", $OutputPath)
        if ($LASTEXITCODE -ne 0) {
            throw "prerelease-notes.sh exited with $LASTEXITCODE`n$Output"
        }

        $Notes = Get-Content -Raw -Path $OutputPath
        Assert-Match -Text $Notes -Pattern '<details open>' -Message "Release notes did not include the details card."
        Assert-Match -Text $Notes -Pattern 'Good to know before Thunderstore' -Message "Release notes did not include the card summary."
        Assert-Match -Text $Notes -Pattern '## Unreleased.*empty' -Message "Release notes did not describe changelog turnover."
        Assert-Match -Text $Notes -Pattern 'Thunderstore version.*1\.2\.3' -Message "Release notes did not include the Thunderstore package version."
        Assert-Match -Text $Notes -Pattern 'fixed the client widget timing' -Message "Release notes did not include current version changelog notes."
        Assert-Match -Text $Notes -Pattern '1234567890ab' -Message "Release notes did not include the short commit."
    }
    finally {
        Remove-Item -Recurse -Force -LiteralPath $FixtureRoot
    }
}

function Test-PrereleaseNotesRejectsUnreleasedContent {
    $FixtureRoot = New-Fixture
    try {
        $ChangelogPath = Join-Path $FixtureRoot "CHANGELOG.md"
        Set-Content -Path $ChangelogPath -Value @'
## Unreleased
- still parked for the next release

`1.2.3`
- fixed the client widget timing
'@

        $Output = Invoke-PrereleaseNotes -Arguments @(
            "--changelog", $ChangelogPath,
            "--version", "1.2.3",
            "--check-only")
        if ($LASTEXITCODE -eq 0) {
            throw "prerelease-notes.sh unexpectedly accepted non-empty Unreleased notes."
        }

        Assert-Match -Text $Output -Pattern 'CHANGELOG\.md ## Unreleased must be empty' -Message "Unreleased rejection message was not specific."
    }
    finally {
        Remove-Item -Recurse -Force -LiteralPath $FixtureRoot
    }
}

function Test-PrereleaseNotesRejectsMissingVersionEntry {
    $FixtureRoot = New-Fixture
    try {
        $ChangelogPath = Join-Path $FixtureRoot "CHANGELOG.md"
        Set-Content -Path $ChangelogPath -Value @'
## Unreleased

`1.2.2`
- previous release
'@

        $Output = Invoke-PrereleaseNotes -Arguments @(
            "--changelog", $ChangelogPath,
            "--version", "1.2.3",
            "--check-only")
        if ($LASTEXITCODE -eq 0) {
            throw "prerelease-notes.sh unexpectedly accepted a missing version entry."
        }

        Assert-Match -Text $Output -Pattern "does not contain notes for '1\.2\.3'" -Message "Missing version rejection message was not specific."
    }
    finally {
        Remove-Item -Recurse -Force -LiteralPath $FixtureRoot
    }
}

function Test-ReleaseWorkflowChecksOnlyDownloadedReleaseChangelog {
    $WorkflowPath = Join-Path (Split-Path -Parent (Split-Path -Parent $ScriptRoot)) ".github/workflows/release.yml"
    $WorkflowText = Get-Content -Raw -Path $WorkflowPath

    $DownloadMarker = "      - name: Download Release"
    $DownloadedChangelogMarker = "      - name: Validate downloaded release changelog"

    $DownloadIndex = $WorkflowText.IndexOf($DownloadMarker, [StringComparison]::Ordinal)
    $DownloadedChangelogIndex = $WorkflowText.IndexOf($DownloadedChangelogMarker, [StringComparison]::Ordinal)

    if ($DownloadIndex -lt 0) {
        throw "release.yml is missing the Download Release step."
    }

    if ($DownloadedChangelogIndex -lt 0) {
        throw "release.yml is missing downloaded release changelog validation."
    }

    if ($DownloadedChangelogIndex -lt $DownloadIndex) {
        throw "Downloaded release changelog validation must run after the release download."
    }

    $BeforeDownload = $WorkflowText.Substring(0, $DownloadIndex)
    if ($BeforeDownload -match 'prerelease-notes\.sh[\s\S]*--check-only') {
        throw "release.yml must not run prerelease-notes.sh --check-only against the checkout before downloading the selected release tag."
    }
}

Test-PrereleaseNotesIncludesChangelogAndDetailsCard
Test-PrereleaseNotesRejectsUnreleasedContent
Test-PrereleaseNotesRejectsMissingVersionEntry
Test-ReleaseWorkflowChecksOnlyDownloadedReleaseChangelog

Write-Host "prerelease-notes tests passed"
