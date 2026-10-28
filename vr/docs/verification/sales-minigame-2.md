# Sales Part 2 implementation verification

Date: 2026-09-13. Unity 6000.5.4f1.

## Implemented flow

`Assets/Scenes/Sale/Store.unity` now connects the existing Part 1 recording upload to Part 2. A Sales session ID is created for Part 1 and reused for the returning-customer conversation. A microphone/diagnostic notice appears before the Part 1 activity.

After upload, the scene fades, moves the XR Origin to the cashier position, shows “Ba ngày sau”, and brings Lan to the counter using the existing female humanoid model and walk/idle clips. The Vietnamese HUD shows her reply, one current objective, store policy, recording status, and remaining time. Right-hand A submits early; recording also submits at 45 seconds. Five active minutes or eight accepted turns request final analysis. Network processing pauses the active timer.

The backend controls ordered objective progression, disclosure, mandatory trust challenge, and final trust. Technical failures retain audio and turn IDs for retry. Final analysis retries retain their completion ID. The three authored endings lead to a persistent trust-state HUD; remote restart/stop remains available through the `sale` adapter.

Scene authoring is reproducible through `SalesPart2ProjectSetup.SetupPart2InOpenSaleScene()`. It uses project-owned additions and preserves existing asset GUIDs. The existing dynamic Vietnamese TMP font retains its source font, so new Vietnamese characters can be populated at runtime.

## Automated results

- Compilation: no C# errors in the connected Unity Editor after the final runtime changes.
- Sales Edit Mode: all nine tests passed, including scenario data, font/resources, and Store scene references.
- Sales Play Mode: all 17 tests passed. Coverage includes early/automatic submission, ordered progression, eight accepted turns, active-time expiry, paused network timing, retries, cancellation, final completion ID, and persistent final HUD.
- Backend: 95 tests passed via `.venv\Scripts\python.exe -m unittest discover -s app/tests -q`. This includes the Unity-shaped HTTP contract and a scripted six-turn traversal of all four objectives.
- Full Unity Edit Mode: 124 tests ran, with three Lawyer failures: two recording-duration expectations (30 versus 60 seconds), and an Office canvas expectation.
- Full Unity Play Mode before the final three added Sales limit tests: 59 tests ran, with four Lawyer failures concerning evidence text overflow, continuation scene, board distance, and clue placement. The subsequent Sales-only run passed all 17 tests.
- The two Game Control tests that assumed Sales was absent from the scene catalog/build list were updated and passed in the final Edit Mode run.

The full project test suite is not green. The Lawyer scene failures are outside this implementation and remain unchanged.

## Build and manual verification

The PowerShell build wrapper was attempted, but its batch Unity invocation could not validate the already-open project. The Android development build through the connected Editor succeeded in 632 seconds with zero errors and 17 warnings. Output: `Builds/Android/SalesPart2/NCKH-VR.apk`. This builds all enabled project scenes; no headset installation was performed.

Editor visual inspection checked Lan's counter placement and the Vietnamese reply/objective/policy layout. `Artifacts/SalesQA/part2-hud-corrected.png` records the layout inspection. The physical counter monitor required reducing the HUD scale and moving the objective in front of its panel.

Still required on the target headset: microphone permission/denial and real speech, right-controller A, tracking loss/recenter/pause-resume, fade and cashier reposition comfort, Lan's walking/head pose, every Vietnamese diacritic and long partial-trust label, and frame-rate/allocation profiling. Live whisper.cpp accuracy and LLM dialogue quality also need a configured-service run. Deterministic tests do not establish those results.

## Review and preserved work

Backend review found and corrected retry duplication, terminal-turn retries, diagnostic authorization, deletion races, and insufficient phase/fact validation. Final scene inspection found no removed existing object IDs. Existing object changes consist of attached Part 2 components/children and camera HUD attachments, apart from Unity's negative-zero normalization.

Pre-existing font, CONTEXT, GAME SALES, ADR/spec, and scene activation edits were preserved outside the implementation commit. The backend's existing `whisper.cpp` submodule change was also excluded. A scene backup remains in `Artifacts/SalesQA/before-part2-unsaved.unity`.
