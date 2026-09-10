# Sales Game Part 1 — paused work

Stopped at the user's request on 2026-09-10. Do not resume without a new request.

## Current state

- Unity project: `D:/NCKH-VR`.
- Scene: `Assets/Scenes/Sale/Shoes store - showroom with footwear.unity`.
- Unity was taken out of Play Mode. Latest scene changes were saved. A script refresh was requested and returned successfully, but the latest compilation result has NOT been checked.
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

- [ ] Check Unity console for compilation errors from the most recent refresh. The newest saved-scene test has not compiled/been run yet; verify its TMP `HasCharacters` and XRI API usage against installed versions if compilation fails.
- [ ] Re-run `NCKH.Sales.EditModeTests` and `NCKH.Sales.PlayModeTests` through Unity MCP. Earlier run passed 4 Edit Mode and 3 Play Mode tests, BEFORE the final lifecycle, scene-layout and controller-path regression additions. Expected latest Edit Mode count is 5; Play Mode count remains 3 with added assertions.
- [ ] Confirm the saved-scene regression test verifies nonzero dialogue text positions, two ray interactors with hovered activation, three selectable shoes, and Vietnamese glyph availability.
- [ ] Re-enter Play Mode and verify the entire saved scene after reload: dialogue, next/replay, transition, all three choices, hover speed, single committed choice, result, retry, and mission disable/re-enable.
- [ ] Verify actual XRI hover/trigger activation in-editor with simulated input where practical, then on a headset. Independent review found the old select-filter/hovered-activation mismatch; it is fixed in setup and scene but has not been exercised with real controllers.
- [ ] Capture final 16:9 dialogue, choice and completion screenshots. Inspect text bounds, card alignment, all three shoes, and Vietnamese diacritics. Latest caption explicitly says product information is illustrative.
- [ ] Inspect the standing customer after the final forearm pose update; that asset edit has not been visually checked. The NPC uses the existing randomized appearance prefab.
- [ ] Recheck dimming and occlusion with the final layout. A temporary wall test showed that the shoes remain visible behind geometry. That temporary wall was runtime-only and removed by exiting Play Mode.
- [ ] Ensure dimming, occlusion-culling state, locomotion and UI restore appropriately on retry, disable/re-enable and leaving the scene.
- [ ] Inspect final git/serialized diffs for unintended changes. Do not overwrite unrelated pre-existing edits listed below. Include all new Unity `.meta` files.
- [ ] Add short usage/data-editing documentation if requested when resuming, and give final implementation/test/headset-limitations summary. No target build has been run; no platform/build-system changes were requested.

## Verification and review notes

- Last fully verified test run: **4/4 Edit Mode, 3/3 Play Mode passed**.
- Independent reviewer identified blocked XRI activation and focus remaining disabled after re-enable. Both fixes are implemented; latest tests still need rerunning.
- Re-enable fix: `SalesMissionController.OnEnable` re-enables focus and resets the mission after initialization.
- Saved TMP positions initially reset to zero on reload. Setup now uses `RectTransform.anchoredPosition3D`; subsequent dialogue capture verified correct positions.
- Shoe data is explicitly placeholder information; actual 1A model looks like a dress shoe. Keep requested model IDs and illustrative caption rather than treating the invented properties as real product facts.
- No headset comfort, tracking, stereo readability, left/right trigger, or audio-device verification has been performed.
- Existing editor warnings included XR audio output fallback and excessive showroom shadow maps. Avoid claiming a clean headset/performance result.

## QA artifacts and caveats

- `Artifacts/SalesQA/before-sales-scene.unity`: pre-integration scene backup.
- `Artifacts/SalesQA/dialogue-final.png`: latest verified readable dialogue capture, BEFORE the final forearm pose adjustment.
- `Artifacts/SalesQA/choices-final.png`: **currently mislabeled/outdated**; a script reload reset the mission and this file contains dialogue. Replace it with an actual final choices capture.
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
