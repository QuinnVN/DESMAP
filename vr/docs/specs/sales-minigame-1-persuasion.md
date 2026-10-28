# Sales minigame 1 persuasion

Status: gameplay requirements confirmed. Test boundaries await confirmation. Issue publication awaits project tracker setup.

## Problem Statement

Sales minigame 1 currently ends when the player selects a shoe. It does not let the player respond to a customer's objection or provide a recorded response for assessment. Selecting the best-fit shoe alone does not demonstrate whether the player can explain its value or reconsider an unsuitable recommendation.

## Solution

After any shoe selection, the customer delivers a predetermined objection specific to that choice. The objection may question price, appearance, budget, or suitability. Any comparison alternative mentioned must have appeared in the earlier shoe comparison panel. It may be cheaper or more expensive than the recommended shoe.

Hide the comparison panel. Display "Thuyết phục khách hàng" for the best-fit shoe and "Thay đổi lựa chọn hoặc thuyết phục khách hàng" for any other choice. Give the player 10 seconds to prepare, then automatically record a response for up to 60 seconds. The player may defend the recommended shoe or name another shoe in their speech, without reopening the panel or restarting the timer.

The A button submits early; expiration submits automatically. After a successful upload, the customer delivers a predetermined closing reply and the stage finishes. The backend processes the recording and stores a persuasion score from 0 to 100 and brief Vietnamese feedback. The assessment does not generate or select a purchase outcome. A silent recording receives zero with an explanation. A failed upload can be retried using the same recording.

## User Stories

1. As a player, I want every shoe selection to lead to a customer objection, so that I can practice responding even when my initial choice is unsuitable.
2. As a player, I want the objection to address my selected shoe, so that the conversation follows my decision.
3. As a player, I want the customer to identify a concrete concern about price, appearance, budget, or needs, so that I know what to address.
4. As a player, I want every mentioned comparison alternative to come from the earlier panel, so that I can respond using information I have seen.
5. As a player, I want prices and product claims in dialogue to match the comparison information, so that the task is consistent.
6. As a player who selected the best-fit shoe, I want the objective "Thuyết phục khách hàng", so that I know to explain my recommendation.
7. As a player who selected another shoe, I want the objective "Thay đổi lựa chọn hoặc thuyết phục khách hàng", so that I know I can reconsider or defend my choice.
8. As a player, I want the comparison panel hidden during preparation and recording, so that the response exercises my recall of the products.
9. As a player, I want 10 seconds of preparation after the objection, so that I can organize my response before recording begins.
10. As a player, I want recording to start automatically after preparation, so that I do not need another action to begin speaking.
11. As a player, I want the full 60-second response allowance to begin with recording, so that preparation does not consume speaking time.
12. As a player, I want to name a different shoe in my response, so that I can change my recommendation verbally.
13. As a player, I want the recording to continue when I change my recommendation, so that my explanation remains one continuous response.
14. As a player, I want to press A when I finish, so that I can submit without waiting for the timer.
15. As a player, I want automatic submission at 60 seconds, so that the response has a clear end.
16. As a player, I want to retry a failed upload using my recorded response, so that I do not have to repeat my speech because of a connection failure.
17. As a player, I want a predetermined customer reply after a successful upload, so that the stage has a consistent ending.
18. As a player, I want the stage to finish without waiting for AI assessment, so that backend processing does not prolong the encounter.
19. As an assessor, I want the backend to store a score and brief Vietnamese feedback for each submitted attempt, so that the response can be reviewed later.
20. As an assessor, I want evaluation to consider customer needs, product facts, and the objection, so that persuasive but inaccurate claims do not receive undue credit.
21. As a player, I want a justified change of recommendation to be eligible for full credit, so that correcting my choice is assessed fairly.
22. As an assessor, I want silence recorded as a zero score with explanatory feedback, so that an unanswered task has an explicit result.
23. As a player, I want all objectives, dialogue, recording status, and displayed errors in Vietnamese, so that the experience uses one language.

## Implementation Decisions

### Confirmed behavior

- Extend the sales mission beyond shoe selection. Preserve the current selection interaction; any valid choice enters persuasion.
- Use the scenario's designated best-fit shoe to select the objective label. Do not require a corrective selection before the response.
- Author an objection for each selectable shoe. An alternative mentioned in it must reference an earlier comparison entry. The original requirement that every alternative be cheaper has been superseded by support for either price direction.
- Use the scenario's actual prices. The 1.8-million and 1.2-million examples illustrate dialogue and do not mandate repricing existing shoes or adding another shoe.
- Hide the comparison panel during preparation and recording. A spoken change of recommendation requires no further selection interaction and does not reset the timer.
- Sequence the encounter as selection, objection, 10-second preparation, recording for up to 60 seconds, upload, predetermined closing reply, and completion. Failed upload permits resubmission of the captured response.
- Start the response countdown with recording. A ends and submits the recording early; timeout submits it automatically.
- Process AI assessment in the backend after accepting the upload. The customer reply and stage completion do not depend on the score or generated feedback.
- Store a score from 0 to 100 and brief Vietnamese feedback. Evaluate customer needs, factual accuracy, and handling of the objection. A supported change of recommendation remains eligible for full credit.
- Assign zero and explanatory feedback to a silent recording. Technical failures are distinct from silence and must not be represented as an assessed zero.
- Use predetermined customer dialogue rather than an AI-generated purchase decision. The final reply wording is an authoring choice; no exact closing sentence was specified.

### Integration approach

- Extend sales mission data and controller behavior, plus the existing scene setup, to supply objections, objective text, preparation, recording, and closing dialogue. Keep scene labels consistent with mission data.
- Reuse the lawyer recording workflow where suitable, including microphone handling, WAV encoding, early submission, and retained audio for retries. Its current backend saves recordings but does not transcribe or assess them; persuasion needs that processing added.
- Add a sales recording submission contract carrying the audio and sufficient attempt and scenario context to associate the assessment with the customer, initial recommendation, available shoes, and objection. Choose exact field names and transport during implementation; none were agreed in the interview.
- Accept only a recording format supported by the backend. Resolve any difference between actual microphone sample rate and the backend's expected format rather than mislabeling audio.
- Add Vietnamese speech transcription and a constrained assessment result using the existing backend model infrastructure where suitable. Existing local model files are not evidence of an integrated transcription service.
- Persist the accepted recording and assessment association before reporting upload success. Retried delivery must not create duplicate attempts or overwrite another attempt's result.
- Keep assessment processing independent of the Unity encounter. Backend errors need a distinguishable processing status, so accepted audio does not silently disappear or become a fabricated score.
- Use the transcript and scenario facts as assessment input. Treat spoken instructions to the model as response content, not as evaluation rules.
- Keep microphone capture and input subscriptions scoped to the active attempt. Reset, scene exit, permission failure, and headset interruption must release recording resources and avoid submitting unrelated audio. Follow the existing lawyer recovery behavior where applicable.
- Use Vietnamese fonts and verify the new text's diacritics. Preserve the existing Input System and XR interaction conventions.

These integration details describe the proposed implementation approach. They do not add new player decisions or a new assessment interface for viewing stored results.

## Testing Decisions

Proposed test boundaries, pending user confirmation: extend the existing sales Play Mode tests for the player-visible encounter, and use the backend HTTP submission boundary for recording acceptance and stored assessment. Prefer these existing high-level boundaries over tests of private helpers. Use controlled audio and model responses to make acceptance tests repeatable.

- Extend the existing sales flow tests to select each shoe and verify the corresponding objection, objective, hidden panel, and progression into persuasion instead of immediate completion.
- Verify 10 seconds of preparation precede capture, the 60-second allowance starts with capture, A submits early, and timeout submits exactly once. Exercise actual controller input where the existing tests already do so.
- Verify speaking a revised recommendation requires no UI reselection and does not reopen the panel or restart the response window.
- Verify upload failure retains the same recording for retry. Successful upload leads to the fixed closing reply and stage completion independently of model completion.
- Verify reset and scene exit stop capture and prevent callbacks from an old attempt affecting a new one. Cover microphone failure and interruption behavior without requiring a physical microphone for automated tests.
- Extend existing sales Edit Mode data and scene tests to check objection coverage, valid comparison references, accurate product text, both Vietnamese objectives, and glyph support.
- Follow existing backend HTTP tests to submit representative recordings and verify acceptance, rejection of invalid audio, attempt association, duplicate-delivery behavior, and durable assessment storage.
- Substitute deterministic transcription and assessment responses at external service boundaries. Verify that stored results preserve the expected score and Vietnamese feedback, silence produces zero, and service failure remains distinguishable from silence.
- Add a small representative evaluation set for factual persuasion, unsupported product claims, a justified recommendation change, and silence. Use it to inspect model behavior rather than asserting an exact model-generated sentence or score.
- Perform a real recording-through-backend check with configured speech and model services. Report it separately from automated tests that substitute those services.
- Verify microphone permissions, audible capture, controller A, countdown legibility, Vietnamese text, and pause/resume on the target headset. Editor tests cannot establish headset verification.

A good test observes the player's state, submitted recording, or persisted assessment. Avoid assertions about internal method calls or a particular arrangement of helper classes.

## Out of Scope

- Changes to sales minigame 2 or other career games.
- Rebuilding the earlier customer dialogue or shoe selection exercise to match older design-document examples.
- Adding a fourth shoe or repricing products solely to reproduce the illustrative objection.
- An AI-generated customer conversation, purchase decision, or branching ending based on the assessment.
- Reopening the comparison panel or adding a second shoe-selection UI during persuasion.
- Showing the score or AI feedback in the Unity encounter, or building an assessor dashboard.
- Additional customer objection turns after the single recorded response.
- Converting the lawyer game to use the new assessment pipeline.

## Further Notes

The user confirmed the timing, hidden comparison panel, both objectives, variable objections, spoken recommendation changes, backend-only score and feedback storage, early and automatic submission, fixed closing reply after upload, full-credit eligibility for a justified change, silence scoring, and retrying the same recording.

The existing scene has three shoes and currently ends after any selection. Its best-fit shoe is the cheapest. An objection about a more expensive, more attractive alternative supports this scenario without changing the catalog. Older design notes describe four shoes and different earlier rounds; this feature extends the existing scene.

Exact rubric weights, transcription runtime, persistence representation, and closing dialogue wording remain implementation choices. No ADR was warranted during the discussion because no hard-to-reverse architectural trade-off was settled.

The local repositories have GitHub remotes, but no project issue-tracker configuration or triage vocabulary was found. Run `/setup-matt-pocock-skills` to provide the setup required by the to-spec skill. Once configured and the proposed test boundaries are confirmed, publish this specification to the project issue tracker with the `ready-for-agent` label. This local document has not been published as an issue.
