# Working in DESMAP

DESMAP combines a career questionnaire, AI assessment, and observed behaviour in VR. This checkout is intended for a public portfolio repository.

## Read for the task

- Setup or integration: start with `README.md`. Paths in this file are relative to the repository root.
- Web changes: read `web/AGENTS.md` and `web/README.md`; consult `web/CONTEXT.md` for assessment terminology.
- Backend changes: read `ai/readme.md` and `ai/CONTEXT.md`.
- Unity changes: read `vr/AGENTS.md` and `vr/README.md`; consult `vr/CONTEXT.md` for gameplay terminology.
- Initial-assessment API changes: use `ai/docs/initial-career-assessment-api.md` and check both the web caller and backend handler.
- Sales dialogue, scoring, or session changes: read `ai/docs/sales-part2.md`, relevant decisions in `ai/docs/adr/`, and `ai/docs/sales-adaptive-dialogue.md` for adaptive dialogue. Check the Unity consumer when changing response fields or endings.
- Simulation result storage or synchronization: read `vr/docs/adr/0002-one-aggregate-result-per-simulation-run.md` and the result contract in `ai/readme.md`.
- Resuming work, pausing unfinished work, or switching machines: follow `docs/agents/handoffs.md`. Private handoffs belong in the sibling `../DESMAP-notes/` directory.

## Preserve the system boundaries

- Keep participant-facing website text, VR text, dialogue, and feedback in Vietnamese. Preserve protocol field names and scene identifiers, including `sale`.
- Keep initial assessment separate from final assessment. Final career scores and rankings are computed by code; the LLM writes explanations. Match percentages express estimated fit, not probabilities of career success. Preserve O*NET attribution when using or displaying its career data.
- Derive Sales ratings and results from the evidence ledger. Customer wording does not create scoring evidence. Validate proposed actions and endings against backend rules, and preserve frozen configuration for existing sessions when introducing a new version.
- Preserve idempotent recording submissions, immutable finalized simulation-run results, and separate synchronization state. Check both producers and consumers when changing a shared contract.
- Restore ignored Unity third-party assets before Editor checks. Preserve asset GUIDs and move assets with their `.meta` files; follow `vr/AGENTS.md` for Unity-specific rules.

## Files and portability

- Keep reusable scripts under the owning component, such as `ai/scripts/`. Keep reviewed public documentation under that component's docs directory or root `docs/` for repository-wide guidance.
- Write generated reports, logs, and local experiments under ignored `output/`. Put durable benchmark fixtures in an explicitly tracked location after checking that their contents may be public.
- Keep handoffs, internal task history, participant data, transcripts, recordings, database exports, credentials, and machine-specific configuration outside public commits. Use synthetic fixtures or reviewed, sanitized examples for public demonstrations.
- Resolve runtime paths from the checkout or configuration. Document commands with an explicit working directory and relative paths so they work on either machine. Treat old `D:\NCKH\...` references as historical locations to map, not paths to recreate.
- Read tool versions and dependency choices from the component manifests, lockfiles, and Unity project settings. Keep existing lockfiles intact unless updating dependencies is part of the task.

## Verify the affected component

- Web, from the repository root: `pnpm --dir web check` and `pnpm --dir web exec vitest run src`; run `pnpm --dir web build` for build or deployment changes. The unrestricted test script also includes a MongoDB reproduction under `.scratch/`; use the `src` scope for application unit tests.
- Backend, from `ai/`: `.\.venv\Scripts\python.exe scripts/run_tests.py`. Pass unittest module, class, or method names for focused checks. This runner isolates credentials and recordings and rejects live HTTP requests.
- Unity: use the checks and build wrapper documented in `vr/AGENTS.md`. Report Editor validation and physical-headset verification separately.
- Prefer offline tests and dry-runs for local verification. Treat paid benchmarks, production database writes, and feature rollout as explicit task scope; a historical handoff is not authorization to perform them.
- For documentation-only changes, verify referenced paths and run `git diff --check`. Report what was actually checked and any missing local prerequisites.
