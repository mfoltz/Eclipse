# Eclipse Agent Guide

This file is for Codex and other coding agents working in this repository. Keep changes narrow, evidence-led, and specific to Eclipse's V Rising client UI and Bloodcraft bridge runtime.

## Start Here

- Before acting, restate the exact problem, main non-goals, and the condition that should make you stop instead of pushing forward.
- Check the current branch and worktree before editing. Do not rebase, push, merge, delete, retarget, or rewrite branches unless explicitly asked.
- Prefer small, local changes that follow the existing Eclipse patterns. Avoid broad rewrites, style-only churn, or generic architecture cleanups.
- Treat adjacent repos such as Bloodcraft and Emberglass as evidence or integration partners. Do not move their ownership boundaries into Eclipse unless the task explicitly asks for that consumer-side change.

## Build And Verification

- Use this verification ladder unless the task is explicitly docs-only:
  - `git diff --check`
  - `dotnet restore Eclipse.csproj`
  - `dotnet build Eclipse.csproj --configuration Release -p:DeployToClient=false --no-restore`
- `Eclipse.csproj` can copy build output to a local V Rising client plugin folder when deployment is enabled. Use `-p:DeployToClient=false` for compile checks that should not stage DLLs.
- Keep Codex tooling, probes, temporary receipts, and agent-only proof artifacts under `.codex/`.
- Runtime proof helpers and receipts live under `.codex/runtime-proofs/` and `.codex/runs/`; treat those receipts and logs as evidence, not as source files to polish during unrelated tasks.

## V Rising Modding Constraints

- Preserve BepInEx, Harmony, VampireReferenceAssemblies, IL2CPP, and V Rising client lifecycle assumptions unless the task is specifically to change them.
- Be careful around static initialization, world access, UI canvas/bootstrap timing, client readiness, and hotload/runtime-load entry points. Do not move runtime lookups earlier without proving the startup path still works.
- Eclipse initializes in stages: plugin load/config/patching first, then game/world readiness before client services and UI behavior are safe. Keep fallback paths explicit when Emberglass is unavailable or not ready.
- For Bloodcraft or Emberglass bridge work, separate registration, send, fallback, and runtime receipt evidence. Do not treat a queued or suppressed send as proof that the bridge actually delivered.
- Avoid importing framework behavior from Emberglass or server-side Bloodcraft code wholesale. Eclipse should stay a consumer UI mod unless the task explicitly changes that boundary.

## Release And Metadata Boundaries

- Keep the canonical version plain `X.Y.Z` in `Eclipse.csproj`, `thunderstore.toml`, and the top `CHANGELOG.md` entry.
- Do not commit branch-derived `-pre` or `-ft.*` versions. Those are CI outputs only.
- Defer final README, changelog, and Thunderstore wording until after build and focused verification when a feature branch is still being stabilized.
- For release workflow changes, keep GitHub prerelease and Thunderstore package-version handling distinct.

## Workflow And Review Guidance

- For GitHub Actions changes, prefer minimal reliability fixes and use YAML-aware validation. Do not run `bash -n` against workflow YAML.
- For PowerShell proof or release scripts, keep edits small and verify the exact script or test harness touched by the change.
- For UI/runtime changes, prefer source-backed or log-backed evidence over assumptions from history alone.

## Stop Conditions

- Stop if remote or GitHub state cannot be verified for a branch/merge-train task.
- Stop if the available evidence cannot distinguish Eclipse-owned behavior from Bloodcraft, Emberglass, stale installs, dependency mismatch, or local environment failure.
- Stop if verification fails in a way that would require broadening beyond the requested scope.
- Stop before advising public release, Thunderstore publication, or bridge/runtime success when build evidence, logs, or proof receipts do not support the claim.
