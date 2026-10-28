---
title: Participant-linked simulation-run telemetry
status: open
labels:
  - ready-for-agent
---

# Participant-linked simulation-run telemetry

## Problem Statement

The research system cannot produce one complete, participant-linked record for a finished career simulation. Clinic and Doctor emit gameplay telemetry that the backend accepts and discards. Doctor Game 1 does not retain patient resolution time. Doctor Game 2 does not retain enough information to evaluate final note categorization, and its authored questions have no quality labels or distractors.

Lawyer and Sales persist recordings, transcripts, and assessments in separate game-specific files. Lawyer already records an interview restart count and assesses the closing defense, but its stored context is not a complete final clue set. Sales Part 1 and Part 2 use different record shapes. Sales Part 2 may omit assessment flags for deterministic endings. Researchers cannot query one MongoDB document for a completed simulation run.

The backend has no operator command for assigning a real participant name. Unity uses an anonymous runtime session identifier that cannot tell researchers whose results they are reading. The current universal-results specification also describes multiple MongoDB documents per run, model provenance, schema versions, and audio retention behavior that no longer match the approved design.

## Solution

Add an operator command that assigns a real participant name before a simulation starts. The backend creates a participant-session identifier, sends the assignment to Unity, and blocks game start while no participant is active. The assignment survives Unity restarts and control-socket reconnects because the backend reapplies it. A backend restart clears it.

Build one simulation-run result for each completed Clinic, Doctor, Lawyer, or Sales run. Doctor Game 1 and Doctor Game 2 are separate runs. Sales Part 1 and Part 2 form one Sales run. The backend updates an atomic local draft as durable result fragments arrive. It finalizes one JSON document only when the run has all required results and transcripts, then upserts that complete document to MongoDB in one operation.

Keep WAV recordings and game-specific processing records on the backend filesystem indefinitely. The simulation-run result excludes WAV bytes, audio paths, machine-local details, model metadata, rubric metadata, and routine telemetry. The local finalized aggregate is equivalent in data content to the MongoDB document.

Try MongoDB synchronization three times automatically. Persist retry state outside the aggregate. After the third failure, retain the unsynchronized result locally until an operator runs `db sync`. Clear the active participant only after MongoDB confirms the completed aggregate. Log the cleared participant name and participant-session identifier.

## User Stories

1. As an operator, I want to assign a real participant name before starting a game, so that every result belongs to the correct person.
2. As an operator, I want names with Vietnamese characters and spaces to work, so that I can enter participants' actual names.
3. As an operator, I want to view the active participant, so that I can verify the assignment before starting a game.
4. As an operator, I want to clear the active participant manually, so that I can recover from an incorrect assignment.
5. As an operator, I want repeated assignment of the active name to be a no-op, so that an accidental repeated command does not split one participant session.
6. As an operator, I want each new assignment after a clear to receive a new participant-session identifier, so that separate study visits remain distinguishable.
7. As an operator, I want game start blocked when no participant is active, so that the system does not create anonymous research results.
8. As an operator, I want participant changes rejected during active gameplay or audio assessment, so that one run cannot contain two names.
9. As an operator, I want the participant assignment to survive a Unity restart, so that restarting the headset application does not require re-entering the name.
10. As an operator, I want a backend restart to clear the participant, so that backend memory does not silently restore an old assignment.
11. As an operator, I want ordinary WebSocket reconnects to preserve the participant, so that a short network interruption does not change ownership.
12. As an operator, I want the active participant to remain selected after a stopped or restarted game, so that I can repeat the run for the same person.
13. As an operator, I want the active participant cleared after the completed run reaches MongoDB, so that the next run cannot inherit the previous participant.
14. As an operator, I want automatic and manual participant clears logged, so that I can see whose assignment ended.
15. As an operator, I want clear logs limited to the participant name and participant-session identifier, so that the log does not collect extra result details.
16. As an operator, I want synchronization attempted three times, so that short MongoDB failures recover without unlimited background retries.
17. As an operator, I want failed attempt counts to survive backend restarts, so that restarts do not reset the automatic retry limit.
18. As an operator, I want `db sync` to retry all unsynchronized results, so that I can recover after restoring MongoDB.
19. As an operator, I want `db sync` to report synchronized, failed, and remaining counts, so that I know whether recovery finished.
20. As an operator, I want synchronization commands serialized, so that automatic and manual passes cannot write the same result concurrently.
21. As an operator, I want a manual participant clear available after synchronization fails, so that data collection is not permanently blocked.
22. As an operator, I want an unsynchronized result to keep its snapshotted participant identity, so that a later clear cannot reassign it.
23. As a researcher, I want one MongoDB document per completed simulation run, so that I can read the complete outcome without joining result documents.
24. As a researcher, I want Clinic and Doctor Game 2 stored as separate runs, so that their different tasks are not combined into one score.
25. As a researcher, I want Sales Part 1 and Part 2 stored together, so that recommendation and complaint-handling performance remain associated.
26. As a researcher, I want the real participant name and participant-session identifier on every aggregate, so that I can group results and distinguish study visits.
27. As a researcher, I want stable run identifiers, so that retries cannot create duplicate MongoDB documents.
28. As a researcher, I want simulation start and completion timestamps, so that I can place completed runs in time.
29. As a researcher, I want Doctor Game 1 timing to begin when a patient is fully visible and actionable, so that fade-in time is not counted as treatment time.
30. As a researcher, I want Doctor Game 1 timing to end when cure or timeout resolution succeeds, so that each patient has one defined endpoint.
31. As a researcher, I want active patient resolution duration, so that pauses and wall-clock adjustments do not distort the measure.
32. As a researcher, I want cured and timed-out patients retained, so that missing cures are not mistaken for missing telemetry.
33. As a researcher, I want patient identifiers, sickness, severity, bed, outcome, score changes, and timing together, so that I can interpret each Clinic result.
34. As a researcher, I want the Clinic round score stored with its patient results, so that I can inspect both individual and aggregate performance.
35. As a researcher, I want every selected Doctor question stored in selection order, so that I can analyze the player's questioning choices later.
36. As a researcher, I want selected Doctor question text stored with its identifier, so that asset edits do not erase the meaning of an old selection.
37. As a researcher, I want no generated question-quality label, so that the data does not claim the current content measures question correctness.
38. As a researcher, I want every revealed Doctor note and its submitted category, so that I can reconstruct final categorization.
39. As a researcher, I want each revealed note's correct category and essential status, so that I can verify the calculated accuracy.
40. As a researcher, I want overall and essential-note accuracy stored separately, so that one measure does not hide mistakes on essential information.
41. As a researcher, I want correct, incorrect, and unassigned note counts, so that percentages retain their denominator context.
42. As a researcher, I want case score changes and the round total, so that the new metrics do not replace the existing game score.
43. As a researcher, I want the Lawyer closing defense transcript, so that I can inspect the speech used for assessment.
44. As a researcher, I want all four Lawyer criterion scores, so that I can distinguish evidence use, logical connections, conclusion fidelity, and persuasive clarity.
45. As a researcher, I want the raw Lawyer score and final penalized score, so that I can separate defense quality from the restart rule.
46. As a researcher, I want the 50 percent penalty retained after more than three interview restarts, so that current scoring behavior does not change.
47. As a researcher, I want only the interview restart count, so that whole-game restarts and technical retries do not enter the performance result.
48. As a researcher, I want the final Lawyer clue set and completed evidence links, so that I can see the case material available for the closing defense.
49. As a researcher, I want restarted interview attempts excluded, so that discarded strong, weak, and noise clues do not enter the completed result.
50. As a researcher, I want successful final clue sets limited to their actual strong and weak clues, so that the system does not invent retained noise clues.
51. As a researcher, I want Sales Part 1 audio transcribed and assessed, so that I can analyze the player's persuasion response.
52. As a researcher, I want the selected and best-fit shoes stored with the Part 1 result, so that I can interpret the persuasion task.
53. As a researcher, I want the Part 1 score and Vietnamese feedback, so that I can inspect the assessment outcome.
54. As a researcher, I want each Sales Part 2 player transcript and AI customer reply in order, so that I can reconstruct the conversation.
55. As a researcher, I want each Sales Part 2 turn tagged with its objective and timestamp, so that I can inspect progress through the conversation.
56. As a researcher, I want every completed Part 2 result to contain a trust state and four assessment flags, so that deterministic endings have the same result shape as analyzed endings.
57. As a researcher, I want a categorical Sales completion reason, so that silence, abuse, escalation, unauthorized promises, and normal completion remain distinguishable.
58. As a researcher, I want no numeric Sales Part 2 score, so that a categorical trust result is not converted into an invented scale.
59. As a researcher, I want silent Lawyer and Sales Part 1 recordings stored as completed zero outcomes, so that silence is not confused with a technical failure.
60. As a researcher, I want transcripts in MongoDB but WAV recordings excluded, so that the result document stays bounded.
61. As a researcher, I want no model, provider, or rubric metadata in MongoDB, so that the aggregate contains only the approved result data and timestamps.
62. As a researcher, I want completed aggregates immutable, so that later processing cannot silently change historical results.
63. As a researcher, I want aborted runs excluded from MongoDB, so that the collection contains completed simulation runs only.
64. As a researcher, I accept that Sales Part 1 remains local when Part 2 is aborted, so that the one-document Sales rule remains consistent.
65. As a maintainer, I want atomic local draft updates, so that a process interruption cannot expose partial JSON.
66. As a maintainer, I want synchronization state outside the aggregate, so that the local finalized document matches MongoDB data.
67. As a maintainer, I want result fragments delivered idempotently, so that network retries cannot duplicate patients, cases, turns, or assessments.
68. As a maintainer, I want strict finalization validation, so that a missing transcript or required result keeps the draft pending.
69. As a maintainer, I want one MongoDB upsert per run, so that researchers never observe a partially assembled remote result.
70. As a maintainer, I want routine telemetry excluded, so that command and lifecycle noise does not enter research results.
71. As a maintainer, I want real names excluded from filenames, so that Unicode and path characters cannot create unsafe paths.
72. As a maintainer, I want local WAV write failures treated as retryable technical failures, so that assessments do not complete without the requested recording.
73. As a maintainer, I want database credentials loaded from environment variables, so that credentials do not enter source control.
74. As a maintainer, I want MongoDB failures isolated from active gameplay, so that the completed screen can appear before synchronization finishes.
75. As a maintainer, I want new runs blocked while the current completed result awaits synchronization, so that participant ownership remains explicit.
76. As a maintainer, I want completed, unsynchronized, and aborted local data retained, so that automatic cleanup cannot remove research material.

## Implementation Decisions

### Participant assignment

- Add `user <name>` to the backend operator console. Everything after the first separating whitespace is the name.
- Normalize the entered name to Unicode NFC, trim leading and trailing whitespace, collapse internal whitespace runs, preserve display capitalization, and require 1 through 100 characters.
- Reserve `user clear` for clearing the active assignment. A bare `user` command prints the current participant or reports that none is active.
- Generate a new opaque participant-session identifier when an assignment begins. Repeating the currently active normalized name is a no-op. Assigning the same name after a clear creates a new identifier.
- Keep the active participant only in backend memory. A backend process restart clears it.
- Keep the assignment across Unity restarts and control WebSocket reconnects. On every Unity connection, the backend sends the current assignment or an explicit clear command and waits for acknowledgement before applying game-start commands.
- Extend the existing idempotent control-command contract with scene-independent participant assignment and clear commands. Do not overload the scene identifier.
- Store the participant name and participant-session identifier centrally on Unity's persistent game controller. Stamp them onto result-producing telemetry and recording submissions rather than relying on every scene adapter to look up mutable backend state.
- Reject assignment changes and manual clears during active gameplay or audio assessment. Allow manual clear after a run stops, aborts, finishes, or waits for database synchronization.
- Block scene start when the backend or Unity has no acknowledged participant assignment.
- Snapshot participant identity when a run begins and copy it into the draft. Later commands cannot mutate existing drafts or finalized results.
- Preserve the participant after an operator stop, whole-game restart, or Unity restart. A restarted simulation receives a new run identifier but keeps the participant session.
- Clear the participant after MongoDB confirms the finalized run aggregate. Clear both backend and Unity state.
- Write a console entry and structured local operational entry for every automatic or manual clear. The entry contains only the participant name and participant-session identifier.

### Simulation-run identity and lifecycle

- Create one run identifier when the simulation begins. Use it as the local draft identity, the finalized result identifier, and MongoDB `_id`.
- Use `clinic`, `doctor`, `lawyer`, and `sale` as game identifiers. Clinic and Doctor Game 2 create separate run documents. Sales Part 1 and Part 2 share one Sales run identifier.
- A simulation run may contain several game results but produces one simulation-run result.
- Keep one atomic local draft per run. Update it after each durable patient result, Doctor case submission, accepted recording, transcript, assessment, or Sales turn.
- Result-fragment retries reuse their event, attempt, round, session, or turn identifier. The backend replaces or ignores an already accepted fragment instead of appending a duplicate.
- Mark an operator-stopped or superseded draft `aborted`. Retain it locally and never send it to MongoDB.
- When Unity restarts during a run, mark the unfinished run draft aborted. Reapply the participant assignment and use a new run identifier for the next start.
- When the backend restarts, clear the active participant and mark unfinished drafts aborted. Finalized and unsynchronized aggregates keep their snapshotted identity.
- Finalize only when all required game results and backend processing outputs exist. Technical failures retain the draft for retry.
- The finalized aggregate has common fields for `_id`, `runId`, `participantName`, `participantSessionId`, `gameId`, `status`, `startedAtUtc`, `completedAtUtc`, and `data`.
- Set finalized `status` to `completed`. Do not add a schema-version field.
- Keep the finalized local aggregate equivalent to the MongoDB document. Store automatic attempt count, synchronization state, and sanitized synchronization errors in a separate local sidecar record.
- Do not put WAV bytes, WAV paths, machine-local paths, request hashes, credentials, raw exceptions, routine telemetry, model metadata, provider metadata, or rubric metadata in the aggregate.
- Treat finalized aggregates as immutable. This feature does not support reanalysis.

### Clinic data

- Start patient timing only when fade-in finishes, the patient is fully visible, and cure interaction becomes actionable. Do not use the existing activation-request timestamp as the measurement start.
- End timing when the patient accepts a cure or timeout resolution, before fade-out completes.
- Calculate `resolutionDurationSeconds` from monotonic active gameplay time. Accumulate the same frame delta used by the game timer so pause and time-scale behavior match gameplay.
- Store `actionableAtUtc` and `resolvedAtUtc` for placement in time. Treat the monotonic duration as the performance measurement.
- Store one patient entry for every cured or timed-out patient. Each entry contains `patientId`, `sicknessName`, `severity`, `bedIndex`, `resolution`, `actionableAtUtc`, `resolvedAtUtc`, `resolutionDurationSeconds`, `scoreDelta`, and `scoreTotal`.
- Store the round identifier, round start and completion timestamps, and final score in the Clinic `data` object.
- Deliver each resolved patient summary reliably with a stable event identifier. The round completion fragment finalizes only after the backend has accepted every resolved patient summary.
- Do not create a patient result when appearance setup fails before the patient becomes actionable. Report that condition as an operational error outside the aggregate.

### Doctor data

- Record only questions the player selected. Do not assign `essential`, `relevant`, or `unnecessary` labels. Do not calculate question correctness, quality, or coverage.
- Preserve selected questions in selection order. Each entry contains the authored question identifier, the Vietnamese question text snapshot, and its one-based selection order.
- Do not copy unselected questions into the aggregate.
- At case submission, store every revealed note. Each note contains `cardId`, the Vietnamese text snapshot, `isEssential`, `selectedCategory` or null, `correctCategory`, and `isCorrect`.
- Store `correctCount`, `incorrectCount`, and `unassignedCount` across all revealed notes.
- Store the same three counts for essential notes.
- Store `categorizationAccuracyPercent` across all revealed notes and `essentialCategorizationAccuracyPercent` across revealed essential notes. Percentages range from 0 through 100. Store null when the relevant denominator is zero.
- Store the case identifier, patient name, selected questions, submitted notes, score delta, cumulative round score, remaining round time, and case submission timestamp.
- Store the round identifier, start and completion timestamps, and final score in the Doctor `data` object.
- Send one reliable, idempotent case summary at submission. Do not depend on best-effort question or card-movement events to build the aggregate.
- Keep the existing scoring rule. Correctly categorized revealed essential notes add the configured points. Incorrect, unassigned, and nonessential notes do not add points.

### Lawyer data

- Keep the existing asynchronous recording, transcription, and structured defense assessment pipeline.
- Persist the WAV locally before transcription. A write failure is a retryable technical failure and cannot produce a completed result.
- Store the Lawyer round and case identifiers, interview restart count, final clue set, completed evidence links, transcript, criterion scores, raw score, restart penalty, final score, Vietnamese feedback, recording timestamp, assessment completion timestamp, and completion status.
- Keep only `interviewRestartCount`. Do not add whole-game restart or technical retry counts to the aggregate.
- Preserve the existing criterion scales: `evidenceUse` out of 40, `logicalConnections` out of 35, `conclusionFidelity` out of 15, and `clarityAndPersuasiveness` out of 10.
- Preserve the existing 50 percent penalty when the player uses more than three interview restarts. Store `rawScore`, `restartPenaltyPercent`, and `finalScore` separately.
- Build the final clue set from the final active interview attempt. Each clue contains `clueId`, title, strength, source, and source question identifier when applicable.
- Store completed evidence links separately from the final clue set.
- Exclude every discarded interview attempt and its clues. A successful final clue set can contain strong and weak clues but cannot contain a noise clue under the current progression rules.
- Treat a silent recording as a completed result with an empty transcript, four zero criteria, zero raw and final scores, no restart penalty, and fixed Vietnamese feedback.
- Keep WAV data and processing metadata in the existing local processing record. Copy only the approved fields into the aggregate.

### Sales data

- Use one Sales run and one aggregate for Parts 1 and 2.
- Persist every player WAV locally before transcription. A write failure remains retryable and prevents completion of the affected operation.
- Store Part 1 fields for `attemptId`, selected shoe identifier, best-fit shoe identifier, transcript, score from 0 through 100, Vietnamese feedback, recording timestamp, and assessment completion timestamp.
- Treat a silent Part 1 recording as a completed zero score with an empty transcript and fixed Vietnamese feedback.
- Store the ordered Part 2 conversation. Each turn contains `turnId`, player transcript, AI customer reply, active objective, and turn timestamp.
- Store Part 2 `trustState`, `emotionalHandling`, `causeIdentification`, `solutionSuitability`, `trustRebuilding`, `completionReason`, accepted turn count, silence count, and completion timestamp.
- Populate all four Part 2 assessment flags for analyzed and deterministic endings. Derive deterministic values from persisted objective progress and ending state rather than calling the language model again.
- Use categorical completion reasons for normal analysis completion, silence limit, abuse, manager escalation, and maintained unauthorized promise.
- Do not add a numeric Part 2 score or coaching feedback.
- Finalize the Sales aggregate only after Part 2 reaches a terminal result and all Part 1 and Part 2 fields exist.
- If Part 2 is stopped or restarted before completion, mark the combined draft aborted and keep it local. Do not upload the otherwise completed Part 1 assessment separately.
- Keep per-turn audio files and game-specific processing records local. Exclude audio references, audio format diagnostics, model metadata, and request hashes from the aggregate.

### Local storage and audio retention

- Use opaque participant-session, run, round, attempt, and turn identifiers in filenames. Do not use participant names in directory names or filenames.
- Write drafts, finalized aggregates, and synchronization sidecars atomically through a temporary file and replacement operation.
- Retain completed aggregates, unsynchronized aggregates, aborted drafts, game-specific processing records, and WAV files indefinitely.
- Remove or disable automatic Sales diagnostic cleanup for data created under this design.
- Do not add application-level audio browsing, download, deletion, deletion audit, or access-control features. Operators manage local files through the host filesystem.
- Do not backfill existing Lawyer or Sales files. Only runs started after this feature is enabled create simulation-run aggregates.

### MongoDB synchronization

- Configure MongoDB through `MONGODB_URI`, `MONGODB_DATABASE`, and `MONGODB_RESULTS_COLLECTION` environment variables.
- Default the database to `desmap` and the collection to `game_results`. Do not commit connection credentials.
- Upsert the entire finalized aggregate under its run identifier. Never insert fragments or partial drafts into MongoDB.
- Create indexes for `participantName`, `participantSessionId`, `gameId`, and `completedAtUtc`. MongoDB's `_id` index covers the run identifier.
- Run at most three automatic attempts per finalized aggregate: immediately, five seconds after the first failure, and thirty seconds after the second failure.
- Persist the automatic attempt count in the local sidecar. A backend restart resumes only attempts that remain and never resets an exhausted count.
- Keep failed aggregates unsynchronized after the third failure. Do not retry them in an unbounded background loop.
- Add the operator command `db sync`. It makes one idempotent upsert attempt for every unsynchronized aggregate, oldest first.
- Manual synchronization has no lifetime invocation limit. A failed manual attempt leaves the aggregate unsynchronized for a later command.
- Use one synchronization lock for automatic and manual work. Do not run overlapping passes.
- Print synchronized, failed, and remaining counts after `db sync`.
- Keep local aggregates and sidecars after successful synchronization. Do not delete or expire them.
- Clear the active participant when the active run's aggregate synchronizes. If a historical aggregate synchronizes, do not change an unrelated active participant.
- After the third automatic failure, keep the participant active and block another game start. Permit `user clear` after a warning because the finalized aggregate already owns an immutable participant snapshot.
- Let gameplay display its completed state before MongoDB synchronization finishes. Database latency must not keep the player inside an active task.
- Do not add a results HTTP endpoint or researcher dashboard. Researchers access MongoDB through database administration tools.

## Testing Decisions

- Test observable behavior at the highest stable boundary. Prefer operator-console, HTTP, WebSocket-control, and complete store workflows over tests of private helpers.
- Use the central Unity game controller and scene adapters as the primary Unity seam. Use the backend application entry points with fake speech, language-model, filesystem, clock, and MongoDB boundaries as the primary backend seam.
- Test `user <name>` with Vietnamese diacritics, multiple spaces, leading and trailing whitespace, empty input, overlong input, `clear`, repeated active names, and reserved command text.
- Test that a participant assignment creates one identifier, reaches Unity through an acknowledged command, stamps result submissions, and blocks game start until acknowledged.
- Test that assignment changes fail during active gameplay and processing.
- Test that control-socket reconnect and Unity restart reapply the backend assignment. Test that backend restart clears it and sends an explicit clear to Unity.
- Test automatic and manual clears and verify that each log entry contains only the participant name and participant-session identifier.
- Test that stopped and restarted runs retain the participant but create new run identifiers and aborted local drafts.
- Add Clinic Play Mode tests for fade-in completion, actionability, cure, timeout, pause behavior, monotonic duration, and emitted patient summaries.
- Add Clinic tests that appearance failure creates no patient result.
- Add Doctor Edit Mode tests for selected-question snapshots, final note snapshots, all-note counts, essential-note counts, percentage calculations, null denominators, score preservation, and idempotent case summaries.
- Add Doctor Play Mode tests for multiple cases, submission order, round finalization, restart, and timeout.
- Add Lawyer endpoint tests with deterministic speech and assessment substitutes. Verify transcript, four criteria, raw score, restart penalty, final score, feedback, timestamps, silence, retry, and final clue-set projection.
- Add Lawyer tests proving that discarded interview clues and noise clues do not enter a completed aggregate.
- Add Sales Part 1 tests for normal speech, silence, score, feedback, selected shoe, best-fit shoe, and timestamps.
- Add Sales Part 2 tests for ordered transcript turns, objectives, AI replies, normal completion, silence limit, abuse, manager escalation, maintained unauthorized promise, trust state, four flags, completion reason, and counts.
- Test that an aborted Sales Part 2 run leaves Part 1 local and creates no MongoDB aggregate.
- Test every aggregate shape through the backend's public gameplay endpoints or result-fragment endpoint. Verify one finalized local JSON and one MongoDB document per completed run.
- Test strict finalization. Missing patients, cases, transcripts, assessment fields, or Sales parts must keep the draft pending or failed instead of creating a partial MongoDB document.
- Test idempotent replay of patient, case, audio, turn, completion, and synchronization operations.
- Test atomic draft, aggregate, and sidecar writes by injecting failures before replacement and verifying that partial JSON never becomes visible.
- Test synchronization with a fake MongoDB boundary. Cover immediate success, success on attempts two and three, exhausted retries, persisted retry state, backend restart, manual recovery, duplicate commands, and concurrent sync requests.
- Use a fake clock to verify the immediate, five-second, and thirty-second automatic attempt schedule without slow tests.
- Test that MongoDB receives the exact finalized aggregate and no audio path, machine-local detail, request hash, raw error, model metadata, rubric metadata, routine telemetry, or schema-version field.
- Test `db sync` ordering and its synchronized, failed, and remaining summary.
- Test that successful synchronization clears only the participant whose active run was synchronized.
- Test that a manual clear after exhausted synchronization leaves the aggregate's participant snapshot unchanged.
- Test database configuration defaults and indexes without connecting automated tests to a production database.
- Run a live verification against a test MongoDB database and confirm one document for each game identifier.
- Run the repository validation wrapper, relevant Edit Mode tests, and relevant Play Mode tests after Unity changes.
- Run the backend test suite after backend changes and report backend tests separately from Unity compilation and tests.
- Verify microphone permission, Vietnamese speech capture, pause and resume, controller interactions, and readable Vietnamese status text on a physical headset. Automated Editor tests cannot verify headset behavior or comfort.

## Out of Scope

- Doctor question correctness, quality labels, coverage scores, or new distractor questions.
- Uploading unselected Doctor questions or complete Doctor case scripts.
- Retaining Lawyer clues from discarded interview attempts.
- Reporting noise-clue selection in a completed Lawyer result.
- Removing or changing the existing Lawyer restart penalty.
- Counting Lawyer whole-game restarts or technical retries as performance data.
- Uploading a completed Sales Part 1 result when Part 2 aborts.
- Uploading aborted or partial simulation runs to MongoDB.
- Storing WAV bytes or local audio paths in MongoDB.
- Adding application-level audio access, download, deletion, deletion auditing, encryption, or retention controls.
- Automatically deleting local aggregates, drafts, processing records, or WAV files.
- Storing schema versions, speech-model metadata, language-model metadata, provider metadata, prompt versions, or rubric versions in the aggregate.
- Reanalyzing or overwriting completed assessments.
- Storing routine command, lifecycle, interaction, network-retry, or raw-error telemetry in MongoDB.
- Backfilling current Lawyer or Sales local records.
- Adding a MongoDB results API, researcher dashboard, or player-facing detailed results display.
- Connecting participant identity to the web questionnaire or implementing the future final assessment.
- Changing the questionnaire, web application, or questionnaire storage model.
- Creating a lifetime participant document that accumulates several simulation runs.
- Combining Clinic and Doctor Game 2 into one run or aggregate.

## Further Notes

- The current Doctor Game 2 content contains 64 selectable questions. Every question reveals at least one unique essential note. The implementation can record question selection but cannot measure discrimination between good and bad questions without new authored content.
- Under the current Lawyer progression rules, a noise clue prevents successful interview completion and forces a restart. Because discarded attempts are excluded, completed Lawyer aggregates contain strong and weak clues only.
- The design deliberately stores real names in local aggregate JSON and MongoDB while retaining raw audio locally without automatic cleanup. Deployment operators are responsible for host filesystem and database access.
- The design deliberately omits schema and model provenance. Research datasets must keep speech models, language models, prompts, and rubrics fixed because completed results cannot identify or replay a prior analysis configuration.
- MongoDB is the research copy of a completed run. The local finalized aggregate and separate synchronization sidecar provide retry and crash recovery.
- The local `docs/specs` directory is this project's tracked issue store. The `ready-for-agent` label marks this specification for implementation.
