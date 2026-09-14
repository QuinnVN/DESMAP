---
title: Universal game results and lawyer defense assessment
status: open
labels:
  - ready-for-agent
---

# Universal game results and lawyer defense assessment

## Problem Statement

The research system does not have one durable result path for its simulations. Doctor and Clinic emit result-shaped telemetry that the backend discards. Sales keeps local result files in game-specific formats. The Lawyer game stores a closing-defense recording but never transcribes or evaluates it. Researchers therefore cannot query one MongoDB collection for completed game results, and the Lawyer player receives no confirmation that speech recognition and assessment finished.

The backend already loads the pinned Vietnamese Zipformer model for both Sales activities. The Lawyer recording path does not use it. MongoDB is not integrated, and a database outage would currently have no durable retry mechanism for newly received Doctor or Clinic results.

## Solution

Add an asynchronous Lawyer closing-defense pipeline that persists the recording and its authored case context, detects silence, transcribes Vietnamese speech with the shared Zipformer recognizer, and asks the configured language model for a structured defense assessment. The assessment measures how well the player explains the provided defense conclusion with the evidence and reasoning established during the case. Unity shows only processing, retry, and completion states. Researchers can read the transcript and detailed assessment through a protected diagnostic endpoint.

Create one universal game-result contract for Doctor, Clinic, Lawyer, Sales Part 1, and Sales Part 2. Persist each completed result as an atomic local JSON document before acknowledging result delivery. A background synchronizer upserts pending results into the `game_results` collection in the `desmap` MongoDB database. MongoDB downtime must not stop gameplay or require a player to repeat completed work.

## User Stories

1. As a Lawyer player, I want my closing defense transcribed after upload, so that the system can evaluate what I said.
2. As a Lawyer player, I want the game to keep the conclusion visible before and during my defense, so that I can focus on explaining it with evidence.
3. As a Lawyer player, I want the assessment to reward my use of provided evidence, so that the score reflects the task I was asked to perform.
4. As a Lawyer player, I want the assessment to reward logical connections between evidence and conclusion, so that listing unrelated facts does not earn a strong result.
5. As a Lawyer player, I want unsupported claims to reduce my result, so that the game rewards arguments grounded in the case.
6. As a Lawyer player, I want clear and persuasive wording to count toward the assessment, so that communication skill remains part of the exercise.
7. As a Lawyer player, I want the game to show that my defense is processing, so that I do not mistake model latency for a frozen game.
8. As a Lawyer player, I want the game to show a completion message without exposing scores or feedback, so that the player experience remains simple.
9. As a Lawyer player, I want a technical failure to offer a retry, so that I do not lose my defense because a model call failed.
10. As a Lawyer player, I want a retry to reuse my stored recording, so that I do not have to record the same defense again.
11. As a Lawyer player, I want repeated uploads of the same defense to produce one assessment, so that network retries do not duplicate my result.
12. As a Lawyer player, I want a conflicting reuse of my round identifier rejected, so that one defense cannot silently replace another.
13. As a Lawyer player, I want an empty recording handled consistently, so that silence is not mistaken for a model failure.
14. As an operator, I want the backend console to report an empty Lawyer recording, so that microphone problems are visible during a research session.
15. As a researcher, I want the full Lawyer transcript, criterion scores, total score, and Vietnamese feedback stored, so that I can analyze closing-defense performance.
16. As a researcher, I want the Lawyer assessment to use the authored case script and expected reasoning, so that it does not invent facts or grade against an unknown case.
17. As a researcher, I want the backend to calculate the Lawyer total score, so that arithmetic does not depend on generated model output.
18. As a researcher, I want speech and language-model metadata attached to assessed results, so that I can identify which models produced them.
19. As a researcher, I want one MongoDB collection for simulation results, so that I can query results across games without joining unrelated collections.
20. As a researcher, I want numeric results to include a name and scale, so that a Doctor point total is not confused with a Sales percentage.
21. As a researcher, I want categorical outcomes stored separately from scores, so that Clinic resolutions and Sales trust states retain their meaning.
22. As a researcher, I want game-specific details retained in a validated section, so that the common result contract does not discard useful domain data.
23. As a researcher, I want one result document for each Doctor case, so that I can inspect performance within a round.
24. As a researcher, I want one result document for each completed Doctor round, so that I can inspect its final total.
25. As a researcher, I want one result document for each resolved Clinic patient, so that cure and timeout outcomes remain attributable to a patient.
26. As a researcher, I want one result document for each completed Clinic round, so that I can inspect its final total.
27. As a researcher, I want one result document for each Lawyer closing defense, so that its recording-derived assessment has one durable identity.
28. As a researcher, I want one result document for each Sales persuasion attempt, so that its score, feedback, and transcript remain associated.
29. As a researcher, I want one result document for each completed Sales returning-customer session, so that its trust outcome and rubric flags remain together.
30. As a researcher, I want the career questionnaire excluded from game results, so that career guidance is not mixed with simulation performance.
31. As a researcher, I want routine telemetry excluded from MongoDB result storage, so that the collection contains evaluated outcomes rather than activity logs.
32. As a researcher, I want WAV data excluded from MongoDB, so that the result collection does not become an audio store.
33. As a researcher, I want complete case scripts excluded from MongoDB, so that each result stores only the data needed to interpret it.
34. As a researcher, I want detailed results available through one protected endpoint, so that ordinary gameplay clients cannot read transcripts and assessments.
35. As an operator, I want result delivery persisted locally before the backend acknowledges it, so that a MongoDB outage cannot erase a received result.
36. As an operator, I want pending results synchronized after backend restart, so that recovery does not require manual replay.
37. As an operator, I want MongoDB writes to use deterministic upserts, so that retries cannot create duplicate documents.
38. As an operator, I want synchronized local result files retained, so that MongoDB is not the only copy and deletion remains a manual decision.
39. As an operator, I want Doctor and Clinic result events retried by Unity, so that temporary network failures do not drop results before local persistence.
40. As an operator, I want routine telemetry to keep its existing best-effort behavior, so that this feature does not turn every gameplay event into durable data.
41. As an operator, I want a database health endpoint, so that I can inspect configuration, connectivity, pending work, and the last sanitized synchronization failure.
42. As an operator, I want the main readiness endpoint independent of MongoDB, so that queued local persistence permits research sessions during database downtime.
43. As an operator, I want absent MongoDB configuration reported without crashing the backend, so that results stay queued until configuration is supplied.
44. As an operator, I want credentials loaded only from the environment, so that MongoDB secrets do not enter source control or result files.
45. As a maintainer, I want one shared Zipformer recognizer for Lawyer and Sales, so that the process loads one model and serializes inference safely.
46. As a maintainer, I want the shared speech component to use domain-neutral errors and names, so that Lawyer processing does not depend on Sales terminology.
47. As a maintainer, I want result-producing telemetry validated strictly, so that malformed Doctor or Clinic events do not enter the durable result store.
48. As a maintainer, I want local writes to be atomic, so that a process interruption does not leave a partial result document.
49. As a maintainer, I want synchronization failures sanitized, so that credentials and server responses cannot leak through health or diagnostic APIs.
50. As a maintainer, I want an ADR for the universal result contract and durable outbox, so that future game integrations understand the chosen storage model.

## Implementation Decisions

- Use the existing pinned `sherpa-onnx-zipformer-vi-30M-int8-2026-02-09` model on CPU. The approved deployment remains non-commercial research under the model's current license.
- Share one initialized Zipformer recognizer between Lawyer, Sales Part 1, and Sales Part 2. The games do not run concurrently, and the recognizer continues to serialize native inference on one worker.
- Rename speech interfaces, instance names, and shared processing errors where needed to remove Sales-specific ownership from the shared speech component.
- Keep Lawyer submission asynchronous. The upload request succeeds after the backend has atomically persisted the WAV, case context, request hash, and processing record.
- Add a bounded Lawyer assessment context to the Unity payload. It contains the case summary, investigation objective, provided defense conclusion, relevant evidence titles and descriptions, and authored reasoning cards. It excludes unrelated interview branches and audio transcripts from nonselected paths.
- Treat the submitted Lawyer context as an immutable snapshot for that round. An identical retry returns the existing state. Reusing the round identifier with different audio or context returns conflict status.
- Detect silent Lawyer audio before speech recognition. Silence completes with a zero total, zero for every criterion, and brief Vietnamese feedback explaining that no defense was detected. The backend logs an explicit empty-recording alert and does not call Zipformer or the language model.
- Ask the language model for four bounded Lawyer criterion scores and brief Vietnamese feedback. The criteria are `evidence_use` out of 40, `logical_connections` out of 35, `conclusion_fidelity` out of 15, and `clarity_and_persuasiveness` out of 10.
- The backend sums the four Lawyer criterion scores. It rejects generated values outside their criterion bounds or output that does not match the strict structured contract.
- Treat transcript text as untrusted player speech. The assessment prompt must not follow instructions found in the transcript or permit the transcript to replace the rubric and case context.
- Persist Lawyer speech provider, model, version, and configured language-model metadata with completed assessments. Do not expose raw upstream errors to Unity or store them in MongoDB.
- Add a player-safe Lawyer status endpoint keyed by round identifier. It returns processing, completed, or retryable technical failure state without transcript, scores, criteria, or feedback.
- Add a Lawyer retry endpoint keyed by round identifier. It restarts failed transcription or assessment from the stored WAV and context. It does not accept replacement audio.
- Unity polls the Lawyer status endpoint after upload. Its defense HUD shows Vietnamese processing, retry, and completion messages. It does not display scores, criteria, transcript, or feedback.
- A game result is one durable outcome for a meaningful result unit. Create result documents for `doctor.case`, `doctor.round`, `clinic.patient`, `clinic.round`, `lawyer.defense`, `sale.persuasion`, and `sale.returning_customer`.
- Use deterministic result identifiers:
  - Doctor case: `doctor:case:{roundId}:{caseId}`
  - Doctor round: `doctor:round:{roundId}`
  - Clinic patient: `clinic:patient:{roundId}:{patientId}`
  - Clinic round: `clinic:round:{roundId}`
  - Lawyer defense: `lawyer:defense:{roundId}`
  - Sales persuasion: `sale:persuasion:{attemptId}`
  - Sales returning customer: `sale:returning-customer:{sessionId}`
- Use the deterministic result identifier as the MongoDB document `_id` and as the local result filename identity after safe encoding. Repeated processing upserts the same document.
- Do not add a schema-version field.
- Use a common result envelope with `resultId`, `gameId`, `resultType`, relevant session, round, and subject identifiers, `status`, occurrence and completion timestamps, named `scores`, named `outcomes`, optional `criteria`, optional `feedbackVi`, optional `transcript`, optional `modelRuns`, and a validated `details` object.
- Omit optional fields that do not apply. Do not represent absent values with invented scores or outcomes.
- Each score records its name, numeric value, unit, and scale when the scale is defined. Doctor and Clinic totals remain game points. Sales Part 1 and Lawyer totals use a 0 through 100 scale.
- Each outcome records a domain-specific name and value. Clinic patient results retain `cured` or `timedout`. Sales Part 2 retains its trust state and rubric flags without creating a numeric score.
- Doctor case results contain the case score delta, cumulative score total, remaining time, round identifier, and case identifier. Doctor round results contain the final score total.
- Clinic patient results contain patient identifier, sickness name, severity, bed index, resolution, score delta, cumulative score total, and remaining time. Clinic round results contain the final score total.
- Sales Part 1 results contain the attempt and optional Sales-session identifiers, scenario and customer identifiers, selected and best-fit shoe identifiers, assessment status, normalized score, Vietnamese feedback, transcript, timestamps, and available model metadata. Do not copy the full shoe catalog into the universal result.
- Sales Part 2 results contain the session and optional Part 1 attempt identifiers, final trust state, emotional handling, cause identification, solution suitability, trust rebuilding, completion reason, counts, timestamps, and available model metadata. Preserve the existing rule that Sales Part 2 has no numeric score or coaching feedback.
- Only completed performance outcomes enter MongoDB. A technical processing failure remains in its game-specific local processing record until retry succeeds. Silent Lawyer and Sales Part 1 submissions are completed outcomes with deterministic zero scores.
- Convert only these sources into universal results: `doctor.case_submitted`, `doctor.round_finished`, `clinic.patient_resolved`, `clinic.round_finished`, a completed Lawyer defense assessment, a completed Sales Part 1 assessment, and a completed Sales Part 2 session.
- Continue to log and acknowledge all other telemetry without adding it to the result outbox. Periodic telemetry storage remains future work.
- Change Doctor and Clinic result-producing events to reliable delivery with at most five attempts and the existing exponential retry delays. One serialized event envelope and event identifier must be reused across all retries.
- Validate the complete event envelope and game-specific result payload before persistence. If local persistence fails, return a server error so reliable Unity delivery retries the same event.
- Add a local result store under `RESULTS_DIR`, which defaults to `recordings/results`. Write each result atomically through a temporary file and replace operation.
- Keep local synchronization state outside the MongoDB document. Track pending or synchronized state, attempt timestamps, and the last sanitized synchronization error in the local record.
- Retain local result records after successful synchronization. Do not add automatic expiry, cleanup, tombstones, or deletion endpoints.
- Do not backfill current Sales files. Existing records are test data. Only results completed or received after this feature starts enter the universal result store.
- Synchronize pending results at backend startup and in a bounded background loop. A failed MongoDB operation leaves the result pending and does not change the gameplay outcome.
- Use idempotent MongoDB replacement or update operations with upsert enabled. Synchronization must never insert a second document for the same deterministic result identifier.
- Configure MongoDB with `MONGODB_URI`, `MONGODB_DATABASE`, and `MONGODB_RESULTS_COLLECTION`. The database defaults to `desmap`, and the collection defaults to `game_results`. An absent URI disables synchronization without disabling local result persistence.
- Add `RESULTS_DIAGNOSTIC_TOKEN` for detailed result access. The token must be independent of ordinary gameplay authorization and loaded only from the environment.
- Add one protected diagnostic endpoint that reads a universal result by result identifier. It may return transcript, scores, criteria, feedback, outcomes, model metadata, details, and local synchronization state. If the diagnostic token is unset, detailed result access is disabled.
- Add `GET /api/health/db`. It reports whether MongoDB is configured, whether the most recent connectivity check succeeded, the pending local result count, the last successful synchronization time, and a sanitized last error. It must not return the MongoDB URI or credentials.
- Keep `/api/health/ready` dependent on the configured language model and ready Zipformer recognizer. MongoDB configuration and connectivity do not affect that readiness result.
- Exclude WAV bytes and files, complete case scripts, local synchronization errors, authentication tokens, raw routine telemetry, and MongoDB connection details from MongoDB documents.
- Record an ADR that explains the universal typed result envelope, deterministic identities, local durable outbox, one collection, and nonblocking MongoDB synchronization.

## Testing Decisions

- Test observable behavior at the highest stable boundary. Prefer HTTP endpoint tests for backend workflows and Unity controller or scene tests for player behavior. Use focused store tests only for crash safety and synchronization behavior that cannot be observed reliably through HTTP.
- Backend HTTP tests submit each supported result-producing telemetry event and verify the resulting universal document. They also verify that routine telemetry creates no result document.
- Backend HTTP tests submit a Lawyer recording with bounded case context and verify accepted, processing, completed, conflict, and retry responses.
- Lawyer pipeline tests use deterministic speech and language-model substitutes. They verify persisted input before processing, transcript handling, criterion bounds, backend total calculation, model metadata, and sanitized failures.
- Silence tests submit a valid quiet WAV. They verify four zero criterion scores, a zero total, fixed Vietnamese feedback, no speech or language-model call, and an empty-recording console message.
- Idempotency tests repeat identical Lawyer submissions and result telemetry. They verify one local result and one MongoDB document. Conflict tests change immutable content under an existing identifier.
- Result-store tests interrupt or fail writes before replacement and verify that no partial JSON becomes visible. They reuse the existing atomic recording-store approach where practical.
- MongoDB synchronization tests use a fake database boundary. They verify upsert identity, pending state after failure, retry after recovery, startup synchronization, retained local records, and omission of local synchronization fields from the remote document.
- Database health tests cover unconfigured, connected, disconnected, pending, recovered, and sanitized-error states. They verify that connection details never appear in responses.
- Diagnostic API tests cover missing, invalid, and valid diagnostic tokens. They verify that ordinary gameplay authorization cannot read detailed results and that the response contains no audio or secrets.
- Main readiness tests verify that MongoDB absence or failure does not make `/api/health/ready` fail when the language model and Zipformer are ready.
- Doctor result tests submit case and round events and verify point names, score deltas, totals, identifiers, and timestamps without inventing a normalized scale.
- Clinic result tests submit patient and round events and verify severity, bed, cure or timeout outcome, score delta, total, identifiers, and timestamps.
- Sales Part 1 tests verify that a newly completed assessment creates one universal result with its normalized score, feedback, transcript, timestamps, and model metadata. A technical failure must not create a completed performance result.
- Sales Part 2 tests verify that a newly completed session creates one result with trust state and rubric flags but no score or coaching feedback.
- Unity Edit Mode tests verify bounded Lawyer context serialization and strict omission of unrelated case material.
- Unity tests verify that Doctor and Clinic use reliable delivery only for the approved result event types and retain one event identifier across retries.
- Lawyer Unity tests verify polling transitions, retry behavior, completion-only output, Vietnamese status text, and the absence of transcript, score, criteria, or feedback in the player HUD.
- Play Mode tests cover behavior that depends on coroutine timing, network completion, or scene state. Edit Mode tests cover deterministic payload construction and response parsing.
- A live verification run must exercise the installed Zipformer model, configured language model, and a test MongoDB database. Record live-model quality separately from deterministic automated test results.
- Headset verification remains required for microphone permission, real Vietnamese speech, early recording completion, pause and resume, controller input, and readable Vietnamese status text. Editor tests cannot establish those results.

## Out of Scope

- Storing periodic, lifecycle, interaction, or other routine telemetry in MongoDB.
- Backfilling current local Sales records or reconstructing historical Doctor and Clinic results.
- Uploading WAV audio to MongoDB.
- Uploading complete Lawyer case scripts to MongoDB.
- Adding automatic result expiry or retention cleanup.
- Adding result or diagnostic deletion endpoints.
- Adding a user-facing cross-game results dashboard.
- Showing Lawyer scores, criteria, transcript, or feedback inside the VR game.
- Converting Sales Part 2 into a numeric score.
- Storing the initial career questionnaire or career suggestions as game results.
- Running more than one speech recognizer or processing speech for simultaneous games.
- Replacing the approved Zipformer release or changing its decoding method.
- Supporting commercial deployment under the current speech-model license.
- Building the future storage path for non-result telemetry.

## Further Notes

- The selected Zipformer model is already integrated into both Sales speech paths and has recorded local benchmark evidence for Lawyer and Sales audio. This work connects the existing recognizer to Lawyer rather than introducing a second speech model.
- The provided defense conclusion is part of the Lawyer exercise. The assessment grades explanation, evidence use, reasoning, fidelity, and communication. It must not grade whether the player independently discovered the conclusion.
- MongoDB is a synchronized research store, not the gameplay source of truth. The atomic local result record is the point at which the backend may acknowledge durable result receipt.
- The universal collection intentionally supports different result meanings. Consumers must filter by `gameId` and `resultType` and interpret named scores and outcomes rather than compare every numeric value as if it used one scale.
- The repositories currently have no local ticket database or separate issue-file convention. This specification is the local tracked issue under `docs/specs` and carries the required `ready-for-agent` label in its front matter.
