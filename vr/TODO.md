# Sales Game Part 1 — resumed verification

Resumed at the user's request on 2026-09-10.

## Current state

- Unity project: `D:/NCKH-VR`.
- Scene: `Assets/Scenes/Sale/Shoes store - showroom with footwear.unity`.
- Unity is out of Play Mode. Latest scene changes are saved and compilation is clean.
- Added a scene-local left ray interactor and repaired both rays to use their matching left/right XRI Activate actions with hovered activation enabled. The shared `Assets/Prefabs/VR Player.prefab` was not changed.
- Implemented customer by the big double door, Vietnamese dialogue with optional AudioClip data, transition to three spinning shoe choices, faster hover rotation, selection feedback and retry.
- Uses the requested existing `Shoes_1A`, `Shoes_3B`, `Shoes_8A` prefabs from `Assets/Shoes store - showroom with footwear/Models/Prefabs/`.
- New runtime scripts: `Assets/Scripts/Runtime/Sales/`.
- New editor setup: `Assets/Scripts/Editor/Sales/SalesProjectSetup.cs`.
- Editable dialogue/shoe data: `Assets/Data/Sales/Placeholder Sales Mission.asset`. Audio slots intentionally empty; audio support is implemented.
- New materials/font/standing pose: `Assets/Materials/Sales/` and `Assets/Data/Sales/`.
- Tests: `Assets/Tests/Sales/EditMode/` and `Assets/Tests/Sales/PlayMode/`.
- Existing lawyer focus feature is reused through layer 9, stencil mask, depth reset and dimming. PC and Mobile renderers already had the feature; no shared lawyer code was changed.
- Scene-specific VR interactor overrides enable hovered activation for activation-only buttons. Original input bindings are preserved. Demo camera is disabled and XR Origin starts near the customer.
- No AI or persuasion work implemented; scope stops after shoe choice.

## Remaining tasks

- [x] Check Unity console for compilation errors from the most recent refresh. TMP `HasCharacters` and current XRI API compile successfully.
- [x] Re-run `NCKH.Sales.EditModeTests` and `NCKH.Sales.PlayModeTests` through Unity MCP: 5/5 Edit Mode and 3/3 Play Mode passed.
- [x] Confirm the saved-scene regression test verifies nonzero dialogue text positions, two ray interactors with hovered activation and matching left/right Activate action maps, three selectable shoes, and Vietnamese glyph availability.
- [x] Re-enter Play Mode and verify the entire saved scene after reload: both dialogue pages, replay path/data, transition, all three choices, 3x hover speed, single committed choice, result, retry, and mission disable/re-enable.
- [ ] Verify physical XRI hover/trigger activation on a headset. Live editor inspection verifies both rays use `XRI Left Interaction/Activate` and `XRI Right Interaction/Activate`; no XR controller simulator was available for a trustworthy end-to-end trigger event.
- [x] Capture and inspect final 1280x720 dialogue, choice and completion screenshots under `Artifacts/SalesQA/Final16x9/`. Text bounds, card alignment, three shoes and Vietnamese diacritics are readable; the illustrative-data caption is present.
- [x] Inspect the standing customer after the final forearm pose update in the dialogue and choice captures. The randomized appearance changes between play sessions as designed; the arms remain behind the torso rather than in an obviously broken forearm pose.
- [x] Recheck dimming and occlusion with the final layout. The three focused shoes remain visible over store geometry while the rest of the scene dims.
- [x] Ensure dimming, occlusion-culling state, locomotion and UI restore appropriately on retry, disable/re-enable and leaving Play Mode. The dim value eases back to zero over its normal transition.
- [x] Inspect final Sales code/scene changes and Unity `.meta` coverage. Full binary diff/check is blocked by Git LFS temp-file access, and unrelated lighting/probe/skybox/editor changes remain untouched.
- [ ] Add short usage/data-editing documentation if requested when resuming, and give final implementation/test/headset-limitations summary. No target build has been run; no platform/build-system changes were requested.

## Verification and review notes

- Latest fully verified test run: **5/5 Edit Mode, 3/3 Play Mode passed**.
- Independent reviewer identified blocked XRI activation and focus remaining disabled after re-enable. Both fixes are implemented; latest tests still need rerunning.
- Re-enable fix: `SalesMissionController.OnEnable` re-enables focus and resets the mission after initialization.
- Saved TMP positions initially reset to zero on reload. Setup now uses `RectTransform.anchoredPosition3D`; subsequent dialogue capture verified correct positions.
- Shoe data is explicitly placeholder information; actual 1A model looks like a dress shoe. Keep requested model IDs and illustrative caption rather than treating the invented properties as real product facts.
- No headset comfort, tracking, stereo readability, physical left/right trigger, or audio-device verification has been performed.
- Existing editor warnings included XR audio output fallback and excessive showroom shadow maps. Avoid claiming a clean headset/performance result.

## QA artifacts and caveats

- `Artifacts/SalesQA/before-sales-scene.unity`: pre-integration scene backup.
- `Artifacts/SalesQA/dialogue-final.png`: latest verified readable dialogue capture, BEFORE the final forearm pose adjustment.
- `Artifacts/SalesQA/Final16x9/dialogue.png`, `choices.png`, and `completion.png`: latest verified 1280x720 desktop QA captures. Hand renderers were hidden only in Play Mode for these captures and no rig change was saved.
- `Artifacts/SalesQA/choices-final.png`: retained outdated intermediate artifact; do not present it as final.
- Other files under `Artifacts/SalesQA/` include intermediate captures with known layout problems; do not present them as final.
- Desktop QA captures temporarily hid untracked hand renderers only in Play Mode. This was not saved to the VR rig. Hand meshes can obscure the view without tracking.

## Preserve existing user work

Baseline had modified:

- `Assets/Shoes store - showroom with footwear/Scenes/UniversalRenderPipelineAsset.asset`
- `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset`
- `ProjectSettings/ProjectSettings.asset`

Baseline also had untracked `Assets/Scenes/Sale.meta`, the Sale scene/assets directory, and `ProjectSettings/TimelineSettings.asset`. The Sale scene was dirty in the editor with the user's VR Player setup; integration preserved that open scene rather than reloading it.

`Assets/XR/Settings/OpenXRPackageSettings.asset` briefly appeared modified during editor activity but was no longer modified at the last git-status check.

## Agent workflow

Requested orchestrator discovery completed. Its implementation agent became unresponsive and was interrupted; root completed implementation directly. Independent review completed with no further code/wiring blocker after the reported fixes. Do not leave implementation work running after this pause.
