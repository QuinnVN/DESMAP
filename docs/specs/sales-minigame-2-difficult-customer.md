# Sales minigame 2 returning customer

Status: design confirmed. Implementation has not started.

## Purpose

Part 2 tests customer-service skills in the sales career-orientation experience. The player must listen to a dissatisfied returning customer, discover the cause of her complaint, offer a suitable remedy, and rebuild trust.

The conversation uses player voice and AI-generated customer text. It is turn-based. The player and customer cannot interrupt each other.

## Scope

This specification covers:

- The transition from Sales Part 1 to Part 2.
- The returning customer, complaint, and store policy.
- Four ordered conversation objectives.
- Voice-turn capture, transcription, and AI customer replies.
- Final transcript analysis and the visible trust state.
- Session data, diagnostic audio, failure recovery, remote control, and testing.

It does not change the confirmed gameplay rules for Part 1.

## Experience flow

1. Create a pseudonymous Sales session when Part 1 starts.
2. Attach the Sales session ID to the Part 1 recording and assessment.
3. After the Part 1 recording uploads successfully, fade to black.
4. Move the XR Origin to an authored cashier anchor and display "Ba ngày sau".
5. Show the store-policy HUD and fade back in.
6. Move Lan from the entrance to an authored stopping point across the counter. Use a humanoid walking animation and scripted waypoints. A baked NavMesh is not required.
7. Show Lan's opening complaint and start Objective 1.
8. Alternate complete player voice turns with complete AI-generated customer text replies.
9. End after the customer responder closes the conversation, after five active minutes, after eight accepted player turns, or after a deterministic ending rule.
10. Analyze the completed phase-tagged transcript when no deterministic trust state applies.
11. Show the authored customer closing line that matches the trust state.
12. Keep Lan standing at the counter, fade to the final HUD, and show only the Vietnamese trust state.
13. Publish completion telemetry, expose `RemoteGamePhase.Finished`, and wait for a remote restart, reset, or scene-load command.

Natural completion must not load Standby automatically.

## Customer profile

Lan is a 22-year-old university student. She addresses the player as "em" and refers to herself as "chị". She begins annoyed but controlled. She becomes more cooperative when the player acknowledges the problem and asks useful questions. She does not insult the player.

Lan bought the returned shoes three days earlier after advice from the player's salesperson role. She is a different customer from Part 1. Her fixed facts are:

- The shoes are the correct size.
- The shoes are undamaged and eligible for exchange.
- She wore them for a full day at university.
- She walked from a bus stop to campus and between classes.
- The discomfort began later in the day rather than immediately.
- Her previous shoes were lighter.
- She often wears thin socks.
- She still likes the returned shoes' color and appearance.
- The actual cause is a needs mismatch. The heavy shoes do not suit her long daily walking routine and lightweight-shoe preference.
- The earlier salesperson failed to ask enough about her daily use before recommending the shoes.

The customer responder may phrase these facts naturally. It must not change the cause, invent product properties, claim a defect, invent a medical condition, or offer a remedy outside store policy. If the player asks for an unknown fact, Lan says she does not know or returns to what she experienced.

## Opening complaint

Lan opens with:

> Em ơi, chị muốn đổi đôi giày này. Chị mới mua ở đây ba ngày trước, nhưng mang vào thì bị đau chân. Lần trước em tư vấn đôi này phù hợp với chị nên chị khá thất vọng.

The opening does not reveal her daily walking routine, previous lightweight shoes, or the actual cause.

## Store policy

The following policy remains visible throughout Part 2, including the final trust-state screen:

> CHÍNH SÁCH ĐỔI HÀNG
>
> Trong 7 ngày • Giày còn nguyên vẹn
>
> Không hoàn tiền hoặc giảm giá
>
> Tự xử lý, không chuyển quản lý

The player may exchange the undamaged shoes for a lighter model suited to extended walking. The player may check fit and let Lan walk in the replacement inside the store. The player may not offer a refund, cash compensation, a discount, an absolute comfort guarantee, or manager escalation.

An explicit attempt to transfer or consult a manager ends Part 2 immediately with lost trust. Mentioning the policy or stating that the player will handle the complaint personally is not escalation.

## Ordered objectives

The conversation has four objectives. They must be completed in order. The current objective is visible during every player turn. One accepted player turn can complete at most one objective.

The customer responder returns the current phase, whether the current objective was completed, and the facts disclosed in its reply. The backend validates that phase progression is monotonic and advances by no more than one phase.

### Objective 1

Displayed text:

> LẮNG NGHE VÀ LÀM RÕ PHÀN NÀN

The player must both:

- Acknowledge Lan's frustration or apologize for her experience without blaming her.
- Ask a relevant open question that invites her to describe the problem.

A generic apology alone does not complete the objective. If the player asks a later-phase diagnostic question without first handling the emotion, Lan responds to the unresolved concern and does not disclose later facts.

### Objective 2

Displayed text:

> TÌM NGUYÊN NHÂN THỰC SỰ

The player must gather evidence before stating the cause:

- Ask about how Lan used the shoes and how long she wore them.
- Ask about fit, condition, discomfort, or her previous footwear.
- State that the shoe's weight conflicts with her long daily walking routine and preference for lightweight shoes.

Lan discloses facts in response to relevant questions:

- Questions about use or duration may reveal the bus-stop walk, movement between classes, full-day use, and late-day discomfort.
- Questions about fit, damage, or onset may reveal correct sizing, no damage, and gradual discomfort.
- Questions about previous footwear or preferences may reveal lighter previous shoes and thin socks.
- Questions about appearance may confirm that she still likes the color, but appearance is not the cause.

A correct guess made before useful investigation does not complete the objective.

### Objective 3

Displayed text:

> ĐỀ XUẤT GIẢI PHÁP PHÙ HỢP

The player must offer a policy-compliant exchange to a lighter shoe suited to extended walking. The player must also offer a concrete fit check or an in-store walking trial.

An unspecified exchange to "another shoe" is insufficient. An unauthorized offer is rejected. Manager escalation triggers the immediate ending instead of remaining in this objective.

### Objective 4

Displayed text:

> KHÔI PHỤC NIỀM TIN

When Objective 3 completes, Lan always raises this challenge once:

> Nhưng lần trước em cũng tư vấn đôi này phù hợp với chị. Làm sao chị biết lần này sẽ không gặp vấn đề tương tự?

The player must:

- Take responsibility for not asking enough about Lan's daily walking during the original sale.
- Explain why the lighter recommendation matches her routine better.
- Offer a concrete verification step such as checking fit and walking in the store.

An absolute promise that the replacement can never cause discomfort does not complete the objective.

Statements intended for later objectives remain in the transcript but do not satisfy those objectives when spoken early. The customer responder redirects the player to the active objective without disclosing later-phase information.

## Turn handling

### Player input

- Accept Vietnamese speech only for this version.
- Start local recording automatically after Lan's text appears.
- Show the current objective, recording state, and remaining turn time using the existing Sales HUD style.
- Let the player press the right-controller A button to submit early.
- Submit automatically when the 45-second turn limit expires.
- Do not show the transcript or ask the player to confirm it.
- Do not provide answer choices, highlighted keywords, correctness marks, or coaching hints.

The status text must be in Vietnamese. Required states include:

- `ĐANG GHI ÂM`
- `ĐANG NHẬN DẠNG LỜI NÓI`
- `KHÁCH HÀNG ĐANG TRẢ LỜI`
- `KHÔNG THỂ HOÀN THÀNH TÌNH HUỐNG`

### Turn acceptance

Only a turn that the backend transcribes, accepts, and persists counts toward the eight-turn limit. A retry uses the same turn ID and does not create another turn.

A valid silent recording does not count toward the eight-turn limit, but it increments a silence counter. Lan prompts once after the first silent recording. A second silent recording ends with lost trust.

A missing recording, unreadable audio, microphone failure, request failure, or backend processing failure is technical. It does not increment the silence counter or the accepted-turn count.

If the backend identifies a non-Vietnamese transcript, Lan asks the player to repeat in Vietnamese. The turn does not count.

### Speech recognition limitation

This version does not request or use transcription-confidence data. Any plausible nonempty Vietnamese transcript proceeds automatically. A recognition mistake may affect Lan's next reply and the final analysis. The player may correct a misunderstanding in a later turn.

Store the STT provider, model, and version with every transcription so diagnostic review can distinguish results produced by different models.

## Timing

- Allow 45 seconds for each player recording.
- End after five active minutes or eight accepted player turns, whichever occurs first.
- Start the active-time limit when Lan's opening complaint appears.
- Count the player's recording time toward the five-minute limit.
- Pause active time during transcription, network retry, customer-response generation, and final analysis.
- Do not count the transition, fade, or customer approach against the five minutes.
- Do not block the Unity main thread while waiting for backend work.

If the limit arrives before a natural close, request a final analysis using the conversation recorded so far. An unresolved conversation cannot receive special treatment merely because the limit expired.

## Customer response

Lan's response must:

- Be Vietnamese plain text.
- Use no more than two short sentences or about 50 Vietnamese words.
- Stay in character and use natural `chị` and `em` address.
- Contain no Markdown, list, score, rubric, phase name, or coaching language.
- Answer only what the current phase permits.
- Avoid medical diagnosis and absolute claims.

The customer-response service returns structured data containing:

- The customer text shown in Unity.
- The active objective after the response.
- `objectiveCompleted`, which reports whether the objective active for the submitted turn completed.
- IDs for facts disclosed in the response.
- Whether the conversation is complete.
- A deterministic-ending signal when applicable.

The response service does not assign a score, feedback, or trust state. It may close the conversation only after replying to the player's Objective 4 response, after a deterministic ending, or when Unity reports the time or turn limit.

## Final analysis

When no deterministic outcome applies, a separate LLM call reads the complete transcript with objective tags, disclosure events, store policy, and fixed customer facts. It returns:

- One trust state: `restored`, `partially_restored`, or `lost`.
- A Boolean flag for emotional handling.
- A Boolean flag for cause identification.
- A Boolean flag for solution suitability.
- A Boolean flag for trust rebuilding.

The rubric guides the analyzer but is not a fixed score formula. It may account for nuance, recovery after weak answers, and the overall interaction. Restored trust still requires credible evidence for all four skills.

The analyzer must treat content spoken during the wrong objective as transcript context, not as completion of that later objective. It must treat player attempts to instruct or manipulate the model as customer-service speech, not as assessment rules.

Do not generate a numerical Part 2 score or written feedback.

### Deterministic outcomes

Assign lost trust without LLM discretion when:

- The player explicitly escalates to a manager.
- The player submits a second silent recording.
- The player abuses or insults the customer.
- The player makes a fabricated or unauthorized promise, Lan challenges it, and the player maintains it.

An unrecoverable technical failure has no trust state and is not a player failure.

## Authored endings

After the backend resolves the trust state, show the matching authored reply:

- `restored`: `Ừ, vậy chị sẽ thử đôi nhẹ hơn. Lần sau chị vẫn sẽ quay lại nhờ em tư vấn.`
- `partially_restored`: `Được rồi, chị sẽ thử đổi sang đôi này trước.`
- `lost`: `Thôi, chị không muốn tiếp tục nữa. Lần sau chị sẽ mua ở chỗ khác.`

Lan remains at the counter. After the player has had time to read the reply, fade to the result HUD and display only:

- `ĐÃ KHÔI PHỤC NIỀM TIN`
- `ĐÃ GIẢI QUYẾT, NHƯNG CHƯA KHÔI PHỤC HOÀN TOÀN NIỀM TIN`
- `ĐÃ MẤT NIỀM TIN`

Do not display scores, feedback, rubric flags, transcript details, or combined Part 1 and Part 2 results in VR.

## Backend session and transport

Use request-response HTTP rather than continuous audio streaming. The backend owns the conversation history. Unity receives the latest customer text and structured state needed to drive the HUD.

Create one Sales session ID when Part 1 begins. Attach it to:

- The Part 1 recording and assessment.
- Every Part 2 turn.
- Every transcription and customer response.
- The final analysis and trust state.
- Retry, timing, completion, and error telemetry.

Each Part 2 turn also has a unique turn ID. Retrying the same upload or customer-response request with the same ID must return or continue the same logical turn. It must not duplicate the transcript, consume another turn, or create a second AI response.

The backend must persist an accepted turn before reporting it as successful. A completion request must also be idempotent so a reconnect cannot produce conflicting trust states.

### HTTP API contract

All gameplay routes use JSON request and response bodies. Route names, nesting, and field names in this section are stable integration contracts.

#### Start or resume a session

`POST /api/sales/sessions`

```json
{
  "sessionId": "optional-client-generated-id",
  "part1AttemptId": "optional-part-1-attempt-id"
}
```

Unity should generate and send `sessionId` before Part 1 starts. If it is omitted, the backend creates one. `part1AttemptId` associates an existing Part 1 attempt during session creation or resume.

#### Read session state

`GET /api/sales/sessions/{sessionId}`

Use this route after reconnect or an uncertain request result. The response is the persisted session state and must not contain raw audio.

#### Submit or retry a turn

`POST /api/sales/sessions/{sessionId}/turns`

```json
{
  "turnId": "client-generated-turn-id",
  "audio": {
    "mimeType": "audio/wav",
    "encoding": "pcm_s16le",
    "sampleRateHz": 16000,
    "channels": 1,
    "dataBase64": "base64-encoded-wav"
  },
  "clientVersion": "optional-unity-client-version",
  "retryCount": 0
}
```

Every turn uses 16 kHz mono PCM WAV. Retrying with the same `turnId` returns the same persisted turn record. It must not retranscribe the audio, append another conversation turn, consume another turn, or create another customer response.

The synchronous response has this shape:

```json
{
  "sessionId": "sales-session-id",
  "turnId": "client-generated-turn-id",
  "status": "completed",
  "accepted": true,
  "transcript": "optional-transcript",
  "customerText": "customer-reply-or-null",
  "activeObjective": 2,
  "objectiveCompleted": true,
  "disclosedFactIds": ["full_day_at_university"],
  "conversationComplete": false,
  "deterministicEnding": null,
  "error": null
}
```

Unity may ignore `transcript` because it does not display or confirm recognized speech. It uses the remaining fields to update the HUD and conversation state.

#### Complete the conversation

`POST /api/sales/sessions/{sessionId}/complete`

```json
{
  "completionId": "client-generated-completion-id",
  "reason": "optional-completion-reason"
}
```

The completion request is idempotent by `completionId`. It returns the persisted session state, including `trustState` and the four rubric Booleans for emotional handling, cause identification, solution suitability, and trust rebuilding. Repeating the request must not rerun analysis or change a completed trust state.

#### Delete diagnostic data

`DELETE /api/sales/sessions/{sessionId}/diagnostics`

This operation requires `SALES_DIAGNOSTIC_TOKEN`. An ordinary gameplay bearer token is insufficient. The route deletes retained raw audio and transcripts for the session while preserving any minimal audit record required to prove that deletion occurred. No gameplay route returns raw audio.

#### Part 1 telemetry association

Part 1 telemetry may carry a Sales session ID in the outer telemetry envelope or as `payload.salesSessionId`. If both are present, `payload.salesSessionId` takes precedence. The backend persists the resolved association with the Part 1 attempt.

## Data retention and privacy

Retain each original Part 2 audio turn for diagnostics. Store it under the pseudonymous Sales session ID and turn ID, not a player name.

The default retention period is 30 days. Make it configurable per deployment. Restrict raw audio access to the diagnostic deletion workflow guarded by `SALES_DIAGNOSTIC_TOKEN`. Do not return audio through ordinary gameplay or result APIs.

Store these records with the session:

- Original audio and its format metadata.
- Transcript and language result.
- STT provider, model, and version.
- Objective active during the turn.
- Objective-completion and disclosed-fact events.
- Customer text and model version.
- Accepted-turn count and silence count.
- Active and processing durations.
- Retry count and idempotency identifiers.
- Final rubric flags and trust state.
- Sanitized error telemetry.

Before Part 1 starts, show a Vietnamese notice that the game uses the microphone and temporarily stores recordings for diagnostics. Support deletion of all audio and transcripts for one Sales session.

Do not place credentials, authorization headers, raw exception secrets, machine-local paths, or model prompts in telemetry.

## Failure behavior

Pause gameplay timers while a request is in flight. If transcription or customer-response generation has not completed within 20 seconds, show retry controls. Preserve the current audio and reuse its turn ID.

After the configured retry policy is exhausted:

- Show `KHÔNG THỂ HOÀN THÀNH TÌNH HUỐNG`.
- Offer a retry that resumes the same logical operation.
- Publish an incomplete-attempt event.
- Do not assign or display a trust state.
- Do not record the attempt as poor player performance.

Error telemetry must contain the Sales session ID, turn ID when applicable, active objective, sanitized failure category, retry count, elapsed time, Unity client version, and STT or LLM provider, model, and version. Do not send a raw stack trace or secrets.

## Unity requirements

### Stable authoring contract

`SalesPart2Controller` must expose these serialized dependencies with the exact field names and types below. Editor setup, saved-scene validation, and automated tests may bind them by serialized name, so renaming a field requires a deliberate data migration.

| Field | Type | Purpose |
| --- | --- | --- |
| `scenario` | `SalesPart2ScenarioData` | Authored Part 2 customer, policy, objectives, and endings. |
| `xrOrigin` | `Transform` | Root moved during the checkout transition. The tracked camera is never moved directly. |
| `playerAnchor` | `Transform` | Authored cashier position and orientation. |
| `customer` | `Transform` | Lan's scene instance. |
| `customerStart` | `Transform` | Start of Lan's approach. |
| `customerWaypoints` | `Transform[]` | Ordered authored route from the entrance to the counter. |
| `customerStop` | `Transform` | Final position and orientation across the counter. |
| `customerAnimator` | `Animator` | Humanoid animator used for arrival and idle presentation. |
| `walkClip` | `AnimationClip` | Looping walk animation used while moving between route points. |
| `idleClip` | `AnimationClip` | Standing animation used at the counter. |
| `headTarget` | `Transform` | Player-relative target used for customer head tracking. |
| `fadeCanvas` | `CanvasGroup` | Fade used before and after moving the XR Origin. |
| `customerText` | `TMP_Text` | Latest AI-generated customer reply. |
| `objectiveText` | `TMP_Text` | Current ordered objective. |
| `policyText` | `TMP_Text` | Persistent Part 2 store policy. |
| `statusText` | `TMP_Text` | Recording, recognition, response, retry, and error state. |
| `timerText` | `TMP_Text` | Current player-turn time. |
| `trustStateText` | `TMP_Text` | Final Vietnamese trust-state label. |
| `locomotion` | `Behaviour[]` | XRI locomotion providers disabled during the conversation. |
| `microphoneProvider` | `ISalesMicrophone` | Replaceable microphone seam shared with the Sales recording workflow. |
| `requestService` | `ISalesPart2RequestService` | Replaceable request-response backend seam. |

`SalesPart2ScenarioData` must expose these authored fields with the exact names below:

| Field | Type | Purpose |
| --- | --- | --- |
| `customerProfile` | `SalesCustomerProfile` | Lan's fixed identity and form of address. |
| `openingComplaint` | `string` | First customer message shown after arrival. |
| `storePolicy` | `SalesStorePolicy` | Policy shown throughout Part 2. |
| `mandatoryTrustChallenge` | `string` | Required challenge after a suitable solution. |
| `objectives` | `SalesPart2ObjectiveData[4]` | Four ordered Vietnamese objectives. |
| `authoredEndings` | `SalesPart2AuthoredEndings` | Closing lines keyed by trust state. |

The scenario may also carry fixed fact IDs and other non-visual grounding data. Those additions must not duplicate or rename the stable authored fields above.

- Reuse the existing Sales HUD style, right-controller A submission, microphone abstraction, WAV pipeline, upload retry behavior, and Vietnamese font assets where suitable.
- Use separate Part 2 scenario data rather than forcing the three-shoe Part 1 data model to represent a returning-customer conversation.
- Add authored transforms for the cashier position, customer start, waypoints, and customer stop.
- Move the player through the XR Origin. Do not move the tracked camera directly.
- Fade before repositioning the XR Origin.
- Lock locomotion providers during Part 2 while retaining head and hand tracking.
- Use an existing compatible humanoid walk clip for Lan's approach. Drive translation through project-owned runtime code because the existing NPC motion component does not apply root motion.
- Return Lan to an idle standing pose and keep head tracking active after she reaches the counter.
- Stop microphone capture, cancel stale callbacks, and release temporary resources on retry, restart, remote stop, scene unload, pause, and headset interruption.
- Add a Sales implementation of `IGameSceneAdapter`. It must expose the current remote phase and support start, restart, and stop.
- A remote restart begins Part 1 with a new Sales session ID. A network retry reuses the current session and turn.
- Natural completion leaves the trust-state HUD visible and reports `RemoteGamePhase.Finished`.

All visible text must be Vietnamese and use a TextMesh Pro font asset with every required glyph.

## Backend requirements

- Add a Part 2 session and turn model rather than extending the Part 1 persuasion payload with unrelated fields.
- Keep Part 1 and Part 2 associated through the Sales session ID.
- Accept only 16 kHz mono PCM WAV for Part 2 turns. The JSON request nests the WAV metadata and base64 data under `audio`.
- Persist audio before starting transcription and persist accepted turn state before returning success.
- Separate customer-response generation from final transcript analysis.
- Validate every structured model response. Reject unknown trust states, backward phase movement, skipped phases, missing customer text, oversized customer replies, and responses that do not satisfy the stable HTTP response schema.
- Bound transcript and prompt size even though the current design permits at most eight accepted turns.
- Retry or fail safely when an LLM returns invalid structured data. Do not invent a customer reply or trust state in application code.
- Keep diagnostic audio and transcript deletion auditable. Require `SALES_DIAGNOSTIC_TOKEN`; ordinary gameplay authorization cannot delete or read diagnostic audio.

## Acceptance tests

### Unity Edit Mode

- Validate the fixed customer profile, opening line, policy, four ordered objectives, authored endings, and required Vietnamese glyphs.
- Validate scene references for the player anchor, customer route, stop position, customer model, walk clip, Sales HUD, policy HUD, locomotion providers, and remote adapter.
- Validate that Part 2 does not reuse the Part 1 shoe-comparison data contract.

### Unity Play Mode

- Complete Part 1 upload and verify the fade, XR Origin reposition, policy HUD, customer approach, and opening complaint.
- Verify Objective 1 starts first and the HUD shows the current objective on every player turn.
- Verify one turn cannot advance more than one objective.
- Verify later-phase content spoken early does not complete or skip an objective.
- Verify fact disclosure follows relevant Objective 2 questions.
- Verify the mandatory trust challenge appears once after a valid solution.
- Verify A submits early and 45 seconds submits automatically.
- Verify five active minutes and eight accepted turns end the conversation, and processing time does not consume active time.
- Verify silence handling, non-Vietnamese handling, technical retry, and duplicate-turn suppression.
- Verify manager escalation ends immediately with lost trust.
- Verify each authored closing line maps to the correct Vietnamese trust state.
- Verify technical failure shows the incomplete HUD without a trust state.
- Verify final completion publishes telemetry, reports `RemoteGamePhase.Finished`, locks locomotion, and leaves the HUD visible.
- Verify restart creates a new Sales session and returns to Part 1.
- Verify stop, reset, scene exit, and headset pause release microphone and request resources.

### Backend

- Create and resume a Sales session through `POST /api/sales/sessions`, using a client ID when supplied and creating one when omitted.
- Associate one Part 1 attempt during session creation and verify that `payload.salesSessionId` takes precedence over the outer telemetry session ID.
- Read the persisted state through `GET /api/sales/sessions/{sessionId}` without returning raw audio.
- Persist valid audio before transcription and retain it under the correct session and turn IDs.
- Reject a turn whose nested audio object is not 16 kHz mono PCM WAV.
- Retry an identical turn idempotently without duplicate transcripts, customer replies, or turn counts.
- Return the same synchronous turn record for a duplicate `turnId`, including status and error state.
- Reject corrupt audio as a technical failure rather than silence.
- Prompt once for a valid silent turn and assign deterministic lost trust after the second.
- Reject or redirect non-Vietnamese speech without consuming an accepted turn.
- Enforce ordered, one-step phase progression and permitted fact disclosure.
- Validate customer replies for language, length, format, character facts, and structured state.
- Complete through `POST /api/sales/sessions/{sessionId}/complete`; run final analysis only when no deterministic outcome applies.
- Retry the same `completionId` without rerunning analysis or changing the stored trust state.
- Verify the analyzer returns only an allowed trust state and the four Boolean rubric flags, with no score or feedback.
- Verify every deterministic lost-trust rule and the technical-incomplete path.
- Verify 20-second timeout recovery and idempotent final-analysis retry.
- Record sanitized error fields and model-version metadata.
- Enforce default and configured diagnostic-audio retention.
- Delete all retained audio and transcripts only when `DELETE /api/sales/sessions/{sessionId}/diagnostics` receives the valid `SALES_DIAGNOSTIC_TOKEN`.
- Reject deletion with an ordinary bearer token and deny raw-audio access through every gameplay and result API.

Use deterministic substitutes for STT and LLM calls in automated tests. Add a small Vietnamese transcript evaluation set for emotional handling, useful questioning, correct and incorrect causes, policy-compliant and unauthorized solutions, trust rebuilding, recovery after a weak response, prompt injection, and abuse. Do not assert exact generated customer wording except for the authored opening, mandatory challenge, status text, policy, and endings.

## Headset verification

Automated tests cannot verify these items:

- Fade comfort when repositioning the XR Origin.
- Player and customer placement at the checkout desk.
- Walk animation speed, foot sliding, route alignment, and stopping distance.
- Readability of customer replies, objective, policy, timers, errors, and the long partial-trust label.
- Controller A behavior and microphone permission on the target headset.
- Recording cleanup across headset pause, resume, disconnect, and recentering.
- Vietnamese diacritics on the target device.

Report headset checks separately. Do not claim them from Editor or backend tests.

## Out of scope

- Customer speech synthesis, lip sync, or player interruption of Lan.
- Continuous or bidirectional audio streaming.
- Transcript confirmation or transcription-confidence handling.
- Multiple Part 2 customer cases or AI-generated case facts.
- A numerical Part 2 score or written feedback.
- Showing assessment metrics, transcripts, or combined results in VR.
- An assessor dashboard or ordinary web access to diagnostic audio.
- Automatic scene return after completion.
- Changes to the confirmed Part 1 gameplay design beyond Sales session association and transition into Part 2.

## Known limitation

Because this version neither collects STT confidence nor asks the player to confirm transcripts, a plausible but incorrect transcript can affect the conversation and trust state. Model-version telemetry and retained diagnostic audio support later investigation, but they do not correct the live attempt.
