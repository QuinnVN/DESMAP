# NCKH VR development guide

## Project baseline

- Unity Editor: `6000.5.4f1`.
- Render pipeline: Universal Render Pipeline `17.5.0`.
- XR stack: XR Interaction Toolkit `3.5.1`, XR Plug-in Management `4.7.0`, and OpenXR `1.17.1`.
- Input stack: Input System `1.19.0`. Locomotion and interaction are action-based; do not use the legacy Input Manager APIs.
- The first enabled build scene is `Assets/Scenes/Standby.unity`.
- Treat the Unity versions recorded in `ProjectSettings/ProjectVersion.txt` and `Packages/manifest.json` as authoritative.

## Language

- All player-facing text must be written in Vietnamese, including UI, HUDs, instructions, dialogue, labels, status messages, and displayed errors.
- Keep code symbols, API field names, telemetry event names, command names, and scene IDs in their established protocol form; translate only the text shown to players.
- When adding Vietnamese TextMesh Pro content, use a font asset that supports the required glyphs and verify every diacritic in the Unity Editor and on the target headset.

## Repository map

- Project-owned scripts currently live directly under `Assets/`.
- Project scenes live in `Assets/Scenes/`.
- Project XR assets live in `Assets/XR/` and `Assets/XRI/`.
- Imported or sample content lives in `Assets/Animated Hands/`, `Assets/Ishikawa1116/`, `Assets/Samples/`, `Assets/TextMesh Pro/`, and `Assets/TutorialInfo/`.
- Editor-only automation belongs in `Assets/Editor/`.
- New runtime code should normally go under `Assets/Scripts/Runtime/`; new editor code should go under `Assets/Scripts/Editor/` or `Assets/Editor/`.
- New tests should go under `Assets/Tests/EditMode/` or `Assets/Tests/PlayMode/`, with appropriate assembly definitions.

## Unity asset safety

- Preserve every `.meta` file and its GUID. Move an asset together with its `.meta` file.
- Never edit generated `*.csproj`, `*.sln`, or `*.slnx` files.
- Do not edit `Library/`, `Temp/`, `Logs/`, `Obj/`, or other generated directories.
- Avoid editing imported/sample packages unless the task explicitly targets them. Prefer project-owned prefabs, scripts, and overrides so package upgrades remain possible.
- Unity scenes and prefabs are serialized YAML. Keep changes narrow and inspect diffs for lost components, changed GUIDs, or unrelated serialized churn.
- Do not delete or recreate assets just to repair a reference. Diagnose the missing GUID/reference first.
- Let Unity create `.meta` files for new folders and assets, then include those files in Git.

## XR and locomotion conventions

- Use action-based XRI locomotion and interaction through the new Input System.
- Preserve existing Input Action assets and serialized `InputActionReference`/`InputActionProperty` assignments unless a task requires changing the control scheme.
- Subscribe to input actions in `OnEnable` and unsubscribe in `OnDisable`; avoid duplicate subscriptions and leaked callbacks.
- Do not move the tracked camera transform directly. Route player translation, turning, teleportation, and body transformations through the project's XRI locomotion system and XR Origin.
- Keep left- and right-hand bindings explicit. Verify controller bindings separately when changing locomotion or interaction.
- Account for tracking loss, recentering, controller disconnects, and headset pause/resume where the feature is affected.
- Do not mix legacy `Input.GetAxis`, old XR input APIs, and Input System actions.
- Any movement outside XRI providers must be frame-rate independent and must document why an XRI provider is unsuitable.

## C# conventions

- Prefer small `MonoBehaviour` components with serialized dependencies over global lookups.
- Avoid `GameObject.Find`, `FindObjectOfType`, reflection, LINQ allocations, and repeated component searches in hot paths.
- Cache component references and shader property IDs.
- Avoid per-frame garbage allocation in `Update`, physics callbacks, and interaction callbacks.
- Pair event subscriptions and temporary resource creation with deterministic cleanup.
- Keep editor-only APIs behind an `Editor` assembly/folder or `#if UNITY_EDITOR`.
- Add focused Edit Mode tests for pure logic and Play Mode tests for frame-, scene-, physics-, or interaction-dependent behavior when practical.

## VR performance and comfort

- A stable headset frame rate takes priority over visual embellishment.
- Avoid unbounded spawning, unnecessary transparent overdraw, excessive real-time lights, and runtime material instantiation.
- Pool frequently spawned objects and profile before claiming a performance improvement.
- Treat unexpected GC allocations during locomotion or interaction as a defect.
- Changes to acceleration, turning, camera motion, vignette behavior, player scale, or collision require physical-headset comfort verification.

## Build and verification

Use the repository build wrapper from PowerShell. It resolves the exact Unity version from `ProjectVersion.txt`.

```powershell
powershell -ExecutionPolicy Bypass -File .\Build.ps1 -Target Validate
powershell -ExecutionPolicy Bypass -File .\Build.ps1 -Target EditModeTests
powershell -ExecutionPolicy Bypass -File .\Build.ps1 -Target PlayModeTests
powershell -ExecutionPolicy Bypass -File .\Build.ps1 -Target Windows
powershell -ExecutionPolicy Bypass -File .\Build.ps1 -Target Android
```

Optional build flags include `-Development`, `-ConnectProfiler`, `-AutoRun`, and `-Output <path>`. Set `UNITY_EDITOR_PATH` or pass `-UnityPath <path>` if Unity Hub is installed somewhere nonstandard.

Before handing off a change:

1. Inspect `git diff` and confirm Unity serialized files contain only intended changes.
2. Run relevant Edit Mode tests.
3. Run Play Mode tests when behavior depends on scenes, frames, physics, or XRI interactions.
4. Build the affected target for build-system or platform changes.
5. Report compilation, tests, and build results separately.
6. Explicitly list anything that still requires headset verification. Never claim headset behavior or comfort was verified from editor tests alone.
