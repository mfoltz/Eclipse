# Mod Pair Compatibility Proof

This proof shape is for checking whether two client-side mods can load together without a targeted startup or UI bring-up failure. It is intentionally generic: the current Eclipse/BloodCraftHub check is one profile of this workflow, not the name or owner of the workflow.

## Labels

- `pairLabel`: stable lowercase label for the two-mod pairing, such as `eclipse-bloodcrafthub`.
- `subjectMod`: the mod being changed or evaluated in this repository.
- `peerMod`: the other mod in the compatibility pair.
- `supportMod`: an additional staged mod needed to make the scenario realistic without redefining the pair under test.
- `proofMode`: one of `subject-only`, `peer-only`, `combined-control`, or `combined-candidate`.
- `runId`: timestamped identifier for one staging and evidence collection attempt.

Use these names in receipts, folders, and summaries so future community troubleshooting can reuse the same packet shape.

## Stop Rules

Stop and mark the run inconclusive when:

- the staged plugin inventory contains unrelated mods;
- two staged artifacts would land with the same plugin filename;
- the proof requires mutating the live game profile without a restorable backup;
- the client/server build, BepInEx pack, or mod versions cannot be recorded;
- the observed result is only a manual impression with no retained log or receipt;
- the failure moves outside the named compatibility lane.

## Recommended Matrix

| proofMode | Staged mods | Purpose |
| --- | --- | --- |
| `subject-only` | subject mod only | Prove the changed mod still loads by itself. |
| `peer-only` | peer mod only | Prove the peer mod is not already failing alone. |
| `combined-control` | subject + peer with suspected risky setting enabled | Optional negative control when safe and quick. |
| `combined-candidate` | subject + peer with candidate mitigation enabled | Main compatibility proof. |

For the Eclipse/BloodCraftHub case, the useful candidate profile is `combined-candidate` with Eclipse `UIOptions.AttributeBuffs=false`. A useful optional control is `combined-control` with `UIOptions.AttributeBuffs=true`.

## Receipt Requirements

Each run should retain:

- `receipt.json` with `pairLabel`, `proofMode`, mod names, artifact hashes, git commit, config overrides, and timestamp;
- `plugin-inventory.txt` listing the staged plugins;
- copied config overrides or a plain text description of them;
- `LogOutput.log` or the closest available BepInEx log from the run;
- `summary.md` naming the pass/fail/inconclusive result and the observed markers.

Expected success markers should be named before the run. For the current compatibility lane, useful markers are:

- subject mod loaded;
- peer mod loaded;
- no fatal or repeated exception around `CanvasService`;
- no fatal or repeated exception around the targeted DOTS attribute-buffer path;
- client reaches the agreed checkpoint, ideally world entry or a timed post-UI-bring-up survival window.

## Dry-Run Staging

Use the generic helper to create a proof packet before launching the game:

```powershell
.\.codex\scripts\New-ModPairCompatibilityProof.ps1 `
    -PairLabel eclipse-bloodcrafthub `
    -ProofMode combined-candidate `
    -SubjectModName Eclipse `
    -SubjectModArtifact .\bin\Release\net6.0\Eclipse.dll `
    -PeerModName BloodCraftHub `
    -PeerModArtifact C:\Path\To\BloodCraftHub.dll `
    -ConfigOverride "Eclipse:io.zfolmt.Eclipse.cfg:UIOptions.AttributeBuffs=false"
```

The helper creates a run folder under `.codex/runs/mod-pair-compatibility/`, stages only the named mod artifacts, and writes the initial receipt. Runtime launch and log collection remain explicit follow-up steps.

When the pair needs a realistic companion mod, keep the pair labels stable and add support artifacts explicitly:

```powershell
.\.codex\scripts\New-ModPairCompatibilityProof.ps1 `
    -PairLabel eclipse-bloodcrafthub `
    -ProofMode combined-candidate `
    -SubjectModName Eclipse `
    -SubjectModArtifact .\bin\Release\net6.0\Eclipse.dll `
    -PeerModName BloodCraftHub `
    -PeerModArtifact C:\Path\To\BloodCraftHub.dll `
    -SupportModArtifact C:\Path\To\Bloodcraft.dll `
    -ConfigOverride "Eclipse:io.zfolmt.Eclipse.cfg:UIOptions.AttributeBuffs=false"
```

## GitHub Release Assets

When the peer mod is distributed through GitHub Releases, fetch the exact release asset into the local artifact cache first:

```powershell
.\.codex\scripts\Save-GitHubReleaseAsset.ps1 `
    -ReleaseUrl "https://github.com/KDavidP1987/BloodCraftHub/releases/latest" `
    -AssetPattern "*.dll"
```

For ZIP assets:

```powershell
.\.codex\scripts\Save-GitHubReleaseAsset.ps1 `
    -Repository KDavidP1987/BloodCraftHub `
    -Tag latest `
    -AssetPattern "*.zip" `
    -ExtractZip
```

The downloader requires the asset pattern to match exactly one GitHub release asset. It writes a receipt beside the downloaded file under `.codex\artifacts\mod-releases\`; use the downloaded DLL, or the extracted DLL if the release asset was a ZIP, as `-PeerModArtifact` for the proof packet.

## Client Sandbox Setup

Use `VRisingCodex` as the client proof sandbox when available:

```powershell
$run = ".\.codex\runs\mod-pair-compatibility\eclipse-bloodcrafthub\combined-candidate\<runId>"

.\.codex\scripts\Use-ModPairClientProofProfile.ps1 `
    -Action Install `
    -ClientRoot "C:\Program Files (x86)\Steam\steamapps\common\VRisingCodex" `
    -ProofRunDirectory $run
```

The install action backs up the sandbox's current `BepInEx\plugins` and `BepInEx\config`, clears the plugin directory, copies the staged proof plugins, and applies config overrides recorded in the proof packet.

After the client run:

```powershell
.\.codex\scripts\Use-ModPairClientProofProfile.ps1 `
    -Action Collect `
    -ClientRoot "C:\Program Files (x86)\Steam\steamapps\common\VRisingCodex"

.\.codex\scripts\Use-ModPairClientProofProfile.ps1 `
    -Action Restore `
    -ClientRoot "C:\Program Files (x86)\Steam\steamapps\common\VRisingCodex"
```

`Collect` copies the current client logs and config into the proof run. `Restore` puts the previous sandbox plugin/config state back from the generated backup.

## Server Support Profiles

When the client pair needs a server mod loaded to produce meaningful replies, create a separate BepInEx plugin-set proof for the server sandbox instead of adding server mods to the client pair:

```powershell
.\.codex\scripts\New-BepInExPluginSetProof.ps1 `
    -ProfileLabel bloodcraft-server-support `
    -TargetRole server `
    -PluginArtifact C:\Path\To\Bloodcraft.dll, C:\Path\To\VampireCommandFramework.dll
```

Install it into the dedicated-server Codex sandbox with the same profile installer, using the server executable name:

```powershell
.\.codex\scripts\Use-ModPairClientProofProfile.ps1 `
    -Action Install `
    -ClientRoot "C:\Program Files (x86)\Steam\steamapps\common\VRisingDedicatedServerCodex" `
    -ExpectedExecutableName VRisingServer.exe `
    -ProofRunDirectory ".\.codex\runs\bepinex-plugin-set\bloodcraft-server-support\server\<runId>"
```
