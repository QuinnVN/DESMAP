# Adaptive Lan dialogue (opt-in, `lan-adaptive-v1` and `lan-adaptive-v2`)

Status: implemented behind a flag, default off. Legacy sessions and the legacy writer request (220 tokens, reasoning off, 4 s) are unchanged. New adaptive sessions use `lan-adaptive-v2` unless `SALES_ADAPTIVE_VERSION=v1`. The previous personality and language-verifier revision had a small live replay check with thinking off and targeted verifier probes. These checks exposed false rejections of colloquial negation by the local policy guard and a missed incoherent STT fragment by the model verifier. Their reliability remains unverified. The current clarification revision has offline backend coverage and a focused Unity Play Mode test for its turn counter; no live model or headset check has been performed for that revision.

## Versions

A session freezes `adaptiveDialogue` (including `version`) at creation. `app/sales_adaptive_versions.py` dispatches every turn on that frozen version:

| `adaptiveDialogue` | Module | Behaviour |
|---|---|---|
| absent | none | legacy plan + writer; no adaptive request |
| `version: lan-adaptive-v1` | `app/sales_adaptive.py` | recognized acts unlock actions (below) |
| `version: lan-adaptive-v2` | `app/sales_adaptive_v2.py` | the model picks the action from the utterance (see [v2](#lan-adaptive-v2)) |
| version missing or unknown, or another model | none | the turn fails with `pipeline_version_unsupported` |

A missing version is never read as the current default, and no session is upgraded. `app/sales_adaptive.py` is the frozen v1 module. It is byte-identical to the benchmark baseline `scripts/benchmarks/sales_adaptive_v1.py`.

New v2 sessions freeze `dialogueRevision=karen-clarification-v1`, the generation prompt hash and `languageGuard` settings/hash. Existing `karen-language-v1` sessions retain their previous prompt, verifier and turn counting. Existing v2 sessions without a revision retain the original prompt and generation-only validation, with no language-verifier requests. The original prompt is retained in `app/sales_adaptive_v2_original_prompt.py`. An unknown revision or mismatched frozen prompt/verifier configuration is unsupported rather than silently upgraded. The benchmark uses the current revision directly.

## What changes (v1)

In the legacy flow, `plan_reply` maps each classified utterance to a fixed intent (for example every `refusesRemedy` turn becomes `refusal_challenge`). Qwen then only paraphrases that intent. In an adaptive session, one `qwen/qwen3.7-flash` request per turn chooses **how Lan reacts**: the action, whether to continue or close, and the cited fact IDs. The same request **drafts her words**. Code still owns everything that affects facts, policy, limits and scoring.

| Owner | Responsibilities |
|---|---|
| Jev/Luna + `sales_rubric`/`sales_concerns` (unchanged) | labels, evidence ledger, disclosures, rubric credit, score, hard endings, turn cap |
| `app/sales_adaptive.py` (code) | eligible actions, which facts may/must be stated, whether an early close is permitted, guards, fallback, action memory |
| Qwen (model) | one action from `allowedActions`, `continue`/`close`, `customerText`, `factIds` |

The model never sees or writes scores, ratings, labels' probabilities or ledger fields. The action it chooses never touches the evidence ledger.

## Enabling

| Variable | Default | Meaning |
|---|---|---|
| `SALES_ADAPTIVE_DIALOGUE_ENABLED` | `false` | New `openrouter` sessions freeze `adaptiveDialogue` at creation |
| `SALES_ADAPTIVE_VERSION` | `v2` | `v1` or `v2` (or the full name); anything else is `v2`. Applies to new sessions only |
| `SALES_ADAPTIVE_REASONING` | `off` | `off` → `reasoning.enabled=false`; `on` → `enabled=true` |
| `SALES_ADAPTIVE_MAX_TOKENS` | `4096` | shared completion cap (reasoning + visible output) |
| `SALES_ADAPTIVE_REASONING_MAX_TOKENS` | `1024` | `reasoning.max_tokens` when reasoning is on; `0` omits it |
| `SALES_ADAPTIVE_TIMEOUT_SECONDS` | `45` | deadline for all attempts of one turn |

The frozen session config (`version`, `model`, `reasoning`, `maxTokens`, `reasoningMaxTokens`, `timeoutSeconds`, `temperature=0.4`) is authoritative. Changing the environment later never converts an existing session in either direction. A missing or unknown `adaptiveDialogue.version` fails the turn with `pipeline_version_unsupported`. Provider routing uses the existing `SALES_OPENROUTER_DATA_COLLECTION`/`SALES_OPENROUTER_ZDR` preferences. Requests use `response_format={"type":"json_object"}` because the endpoint advertises `response_format` but not strict structured outputs. The schema is validated locally.

## Turn flow (v1)

1. STT, Jev, Luna and `evaluate()` run exactly as before. They produce the candidate state, the decision and the ledger entry.
2. `plan_reply()` and `select_customer_line()` still run. Their guarded line is the **fallback**; the legacy intent is kept only as diagnostics (`adaptiveAction.legacyIntent`).
3. `turn_policy()` derives the code limits:
   - `allowedActions` from recognized acts:
     - `challenge_refusal` only after a refusal or withdrawn exchange;
     - `reject_promise` only with a promise or violation;
     - `request_respect` only for bad conduct;
     - `defer_physical` only for physical requests;
     - `voice_trust_doubt` only once the challenge exists;
     - `close_conversation` only when an early close is permitted.
   - A challenge-type action already used twice in a row is withdrawn. Lan must change approach or leave instead of looping.
   - Fact limits: `requiredFactIds` (what Jev says was asked) must be stated and cited. Cited IDs must be shared or released now.
4. `model_context()` sends Lan the following:
   - store policy, purchased shoe, shared facts and facts released this turn;
   - the IDs of facts Lan does not know;
   - every committed prior turn, the current utterance, recognized acts and the open concern/hint (advisory);
   - limits, allowed actions, recent action memory and recent Lan replies.

   Private facts that are not yet released are withheld.
5. `AdaptiveLanResponder.respond()` sends one request. On schema or guard rejection it makes at most one repair request, with codes only: the invalid output is never echoed back. A transport error (5xx/429/network) gets one same-payload retry instead. Everything shares one deadline.
6. Validation (`output_rejections`) combines the legacy text guard, re-aimed at the chosen action and decision, with adaptive checks:
   - action allowed; decision matches action; `must_close` / `close_not_allowed`;
   - `uncitable_fact` / `missing_cited_fact`;
   - `fabricated_store_action` (a store, manager or salesperson "already did/approved/will" do something);
   - for soft closes, `missing_closure` and `unsupported_satisfaction`.

   Existing guards still reject the following:
   - refunds, discounts, compensation and guarantees;
   - invented periods or extra exchange conditions;
   - unallowed or contradicted facts, and unsupported details;
   - completed inspections or trials, and physical actions;
   - repetition, wrong address, length, and unplanned goodbyes.
7. Invalid output, timeout or provider failure falls back to the deterministic line and the deterministic continue/close. It never retries the turn or creates a bad rating. `adaptiveAction.source` is `model` or `fallback`, with `fallbackReason`.
8. The adaptive record and reply are checkpointed together (`turnCheckpoints[turnId].adaptive/written`), so a resubmitted turn never repeats the paid request. The compact `adaptiveMemory` (last 8 `{turn, action, decision, factIds, source}`) is written **only** inside the turn commit. It is hidden from public session responses.

## lan-adaptive-v2

v2 keeps v1's ownership split, generation request settings, two-generation limit, deadline, fallback and checkpoint. It changes the following.

**The model reacts to what was said.** `allowedActions` no longer depends on recognized acts. The context shows the raw utterance and the committed history. Jev's recognized acts appear only as `hints.classifierActs`, and the prompt says they may be wrong or incomplete. A question Jev missed can still get `answer_question`; a refusal Jev missed can still get `challenge_refusal`. Only code-owned state restricts actions:

- a deterministic ending allows only `close_conversation`, except the current revision can request unclear speech again before a provisional `turn_limit` or `stalemate` ending;
- a trust challenge opened this turn allows only `voice_trust_doubt`;
- otherwise every action is allowed, except `close_conversation` without `closeAllowed` and `voice_trust_doubt` before the challenge exists.

`code_limits()` computes facts, ending, ending tone, trust, early-close eligibility and the provisional turn cap with v1's code. The current revision also permits `request_repeat` under the conditions below. The model still cannot release a fact: `requiredFactIds`/`citableFactIds` come from the history and this turn's ledger entry. Labels determine scoring evidence; ordinary actions keep identical ledger and score behaviour (`test_same_labels_give_same_ledger_and_score_across_legacy_v1_v2`).

### Reliability revision for new sessions

New adaptive v2 sessions freeze `karen-reliability-v1`. The original v2, `karen-language-v1` and `karen-clarification-v1` keep their saved prompts, verifier contracts and behaviour. Start a new session to use this revision.

The existing language verifier returns exactly `{verdict, codes, salespersonClarity}`. `salespersonClarity` is `clear`, `mixed` or `unclear`, evaluated from the current player words independently of Lan's chosen reaction. Off-topic speech, jokes, metaphors and unhelpful answers are still meaningful. Natural requests to repeat unclear words are coherent; awkward but understandable Vietnamese is not enough to reject a reply. The earlier revisions retain the two-field verdict.

If the verifier reports wholly unclear speech and the classifier recognized no clear act, code replaces any frustrated or closing reaction with a safe repeat request. This receives the existing `clarificationRequested` exemption even when the original action was not `request_repeat`. The ledger retains the original labels and records `speechUnintelligible`; their uncertainty does not create a review item for this exchange. Five consecutive repeats end as `speech_unintelligible` without a score, trust verdict or completion review. A meaningful act or violation in partly unclear speech keeps the ordinary evidence rules. An unavailable verifier cannot grant this exemption.

Luna verifies monetary promises even when Jev is confident, distinguishing a return of the purchase payment from a separate compensation payment. Distinct commitments can still create distinct violations. Unverified disputed labels stay uncertain rather than creating penalties. Luna also checks positively recognized exchange conditions against the actual policy: seven days and intact shoes, without mandatory receipts, boxes, tags or unworn status.

A first receipt question holds the remedy concern open so the player can respond to Lan's answer. A later correctly stated exchange policy clears this obstacle. A missing receipt is not itself a new store condition. The ledger fold applies this rule only to entries carrying the new reliability revision, including completion review.

Recognized recall of shared needs remains evidence but no longer forces Lan to recite those needs unless the player asks for confirmation. The local guard accepts “đúng size” and “đúng kích cỡ” for the authored fit fact while retaining intactness and contradiction checks. Intactness already present in that fact does not require an additional disclosure of wear history. An implicit self-reference is a style warning; an explicitly wrong form of address is still rejected. Facts, physical actions, policy approval and Chinese-character checks remain mandatory.

`salespersonClarity` stays in private diagnostics. The public fields and Unity's existing handling of `clarificationRequested` and `terminalError` are unchanged. Generation/guard accounting, shared deadlines, retry checkpoints and request limits are unchanged; additional Luna verification may increase arbitration frequency. Offline regressions cover these branches but cannot establish the live models' semantic accuracy.

### Asking the player to repeat unclear speech

In `karen-clarification-v1`, Lan may choose `request_repeat` when she cannot hear or understand part of the current utterance, including a garbled STT phrase. She asks briefly in Vietnamese, in her usual impatient voice, without guessing a corrected transcript. `ask_clarification` still means a normal question about a meaningful proposal; `request_next_step` asks for a solution. Neither ordinary action receives a free turn.

Only a locally valid, model-verified `request_repeat` receives the exemption. Its verifier input adds `clarificationRequest=true`; the current verifier prompt checks that Lan actually asks for unclear speech to be repeated or explained, rather than asking an ordinary product, policy or solution question. A rejected or unavailable verifier triggers the existing regeneration/fallback flow. Fallbacks consume the usual turn. This remains a model judgement and can be wrong.

An accepted repeat returns `clarificationRequested=true` while retaining `status=accepted`. The transcript, reply, labels and evidence ledger entry are committed normally, with `clarificationRequested=true` on the ledger entry. `dialogueTurnCount`, `evaluableTurnCount` and `acceptedTurnCount` do not increase, and `remainingTurns` is unchanged. It also adds no no-progress hint or stalemate penalty. Previous hints are retained, so the next ordinary exchange can still reach stalemate or the turn limit. Clearly recognized evidence, violations and mandatory fact answers remain in effect; the action cannot erase a violation or disclose a private fact. Hard endings and a newly opened trust challenge take priority over repeating speech.

Code ends the dialogue on the fifth consecutive validated repeat, with `endingReason=clarification_limit`. `consecutiveRepeatRequests` is committed with the reply and reset by a verified ordinary model reply. Rejected output or an unavailable verifier neither earns a free turn nor resets the streak. The fifth repeat also costs no dialogue turn. Code replaces its question with a guarded, neutral closing line that says Lan still cannot understand and stops the talk; any required fact answers are retained. This authored ending is recorded as a fallback with `adaptive_clarification_limit`, without another model request. It creates no additional policy violation or scoring penalty and does not diagnose the microphone. Existing evidence still determines the result. Sessions containing only repeats can complete even though their dialogue budget is unused.

If a session in this revision ends with only repeat requests or uncertain turns, no evaluable turns and no clearly recognized act or violation, it ends as an error. The backend saves `status=finished`, `assessmentStatus=failed`, `completionStatus=failed` and `terminalError` with code `speech_unintelligible` and a Vietnamese message. It skips completion review and final scoring, with no score or trust verdict. The terminating turn, completion and session reads expose the same optional error. Unity ends the flow without requesting analysis or offering a retry of that session. Time-limit completion uses the same rule. A session with an ordinary evaluable turn or clear evidence in a partly unclear turn keeps the normal assessment rules. Failed sessions cannot finalize scored simulation-run results; completed historical results are unchanged. Thus the all-repeat completion above closes the session without assigning zero points.

The adjusted candidate, decision and reply share one checkpoint. Retrying a committed or checkpointed turn neither calls the model again nor adds another ledger entry. The Unity DTO reads the optional boolean, defaulting to false for older servers; the controller displays the reply and returns to recording without incrementing its local turn counter. Deploying this behaviour requires the updated client and a new adaptive session. The active-time deadline still applies. No change to `.env` or feature rollout is required by the implementation.

**Hard rejections and warnings are separate.** `app/sales_adaptive_guard.py` checks Lan's text clause by clause. It separates Lan's own assertions from questions, requests, negations and reported speech. It is a conservative regex heuristic, not a semantic verifier.

| Hard rejection (repair, then fallback) | Warning only (accepted, recorded privately) |
|---|---|
| invalid JSON or schema, action/decision mismatch, action not allowed, `must_close`, `close_not_allowed` | more than 2 sentences (`too_many_sentences`) |
| missing, uncited, undisclosed (`unallowed_fact`), contradicted or invented (`unsupported_detail`, wrong periods) facts | echo or paraphrase of the salesperson or of recent Lan replies (`transcript_echo`, `repeated_reply`) |
| a store, manager or salesperson approval, refund, discount, compensation, guarantee or completed action asserted by Lan; invented policy conditions | the same action three turns in a row (`repeated_action`) |
| a physical action or an inspection/try-on said to happen or have happened | goodbye keywords while continuing (`unplanned_ending`); a soft close without a closing keyword (`closure_not_explicit`) |
| instruction or schema-key leaks, wrong address (no `chị`, or `tôi`/`bạn`) | Lan asking for a refund, discount or guarantee (`customer_requests_unauthorized_remedy`) |
| over 600 characters or 80 words; ending tone contradicted; satisfaction claimed in a soft close; trust challenge not voiced | more than 55 words (`long_reply`); ending tone not verifiable; `defer_physical` text without a deferral |

Clause rules:

- A reported-speech marker ("em nói", "em bảo", "em hứa") exempts only what it governs in its clause, with no "thì/nhưng/mà" in between. Quotation marks alone exempt nothing.
- A negator exempts a match only when it is the single negator in scope, with no break ("vì", "mà", "nhưng", "thì") and no other predicate's tense or approval in between. So "chị không biết em đã hoàn tiền" is blocked, and so is "cửa hàng hoàn tiền không sai".
- A yes/no question exempts a match only when it asks about it ("đã … chưa?", "có … không?", "… phải không?"). "… rồi, em có biết không?" is blocked.
- A wh-question may not presuppose a completed claim.
- Colloquial objections such as "em có quyền bồi thường hay giảm giá đâu" and "chứ có cho hoàn tiền đâu" deny authorization. "Chị không nghĩ cửa hàng sẽ hoàn tiền" negates a future expectation, but "chị không biết cửa hàng đã hoàn tiền" remains blocked. A double-negative customer demand remains a demand with a warning. A balanced quote immediately followed by "em vừa nói vậy" is attributed unless endorsed; separate assertions remain checked. Missing mandatory facts still cause rejection.

Three natural sentences, keywordless closes and Lan's requests are accepted.

**Lan's personality.** The v2 prompt makes Lan impatient, demanding and skeptical (cọc cằn, khó tính, a little "Karen"). She speaks curtly, complains sharply and may be sarcastic, but uses no profanity, slurs or threats. She must still answer what the current utterance actually says. She acknowledges real progress grudgingly, and only `limits.endingTone` makes her satisfied. Distrust in her tone is allowed with any action. `voice_trust_doubt` remains the code-owned trust challenge. Harshness is intended and is not checked by any guard. The prompt changes no fact, policy, action, ending or scoring rule.

**Language verifier.** Lan's text must be natural, intelligible Vietnamese. Product and brand names and common loanwords such as `size`, `form` and `shop` are allowed. Speech recognition can produce meaningless words, and Lan must not repeat them as if she understood. She asks what they mean or leaves them out. Two checks enforce this, on `customerText` only:

1. **Local check.** Han, kana, Hangul, CJK punctuation, full-width forms and the supplementary ideograph planes are a hard rejection (`non_vietnamese_text`) before any model call.
2. **Model check.** Each candidate that passes every local check gets exactly one verifier request. It uses the session's frozen model and provider preferences, with reasoning off, `temperature=0`, `max_tokens=96` and at most 8 s. The verifier sees only the current utterance, Lan's previous line and the candidate. It sees no facts, limits, hints, history, identifiers or reasoning. It returns exactly `{"verdict": "pass"|"reject", "codes": [...]}`, with codes `non_vietnamese_text` or `incoherent_text`. No rationale is requested or stored. The prompt tells it not to judge politeness, tone, facts or policy, and it holds no phrase list.

A rejection triggers a **new** reply from the original context. The request adds only the reason codes and fixed instructions; the rejected text is never sent back. The new reply goes through every local check and the verifier again. If the verifier is unusable (timeout, HTTP or network error, or a malformed or extra-field verdict), the candidate is never accepted. A fresh draft is requested from the unchanged context if a generation is left. A missing key or a 4xx other than 429 falls back at once. Limits per turn:

- at most 2 generation requests (initial, then one repair, transport retry or regeneration);
- at most 1 verifier request per locally valid candidate, so at most 4 requests;
- all of them inside the single `timeoutSeconds` deadline.

The deterministic fallback follows. Its public `fallbackReason` is `adaptive_guard_rejected` or `adaptive_guard_unavailable`. The turn glue also refuses any output whose responder did not report a passed verdict. An accepted reply now costs at least two requests. The verifier adds one round trip whose latency has not yet been measured live; a regeneration adds a full generation plus another verifier call.

**Private diagnostics.** Each attempt's candidate is kept with its rejection and warning codes in `session.adaptiveDiagnostics[turnId]`: action, decision, close reason, cited fact IDs, and text truncated to 600 characters with keys, bearer tokens, e-mail addresses and phone-like numbers redacted. It is written at the turn commit (the checkpoint holds it until then), bounded to the last 32 turns, removed by diagnostics deletion and stripped from `public_session`. Each verifier attempt is kept as `{role: "guard", forAttempt, errorCode, verdict, codes}`. Reasoning text, rationales, headers and raw provider bodies are never stored. The turn response's `writerMetadata` keeps timing, usage and cost for every generation and verifier attempt (`role`, `kind`, `generationAttempts`, `guardAttempts`, `costUsdByRole`) but no codes, verdicts or candidates. A rejected candidate is never echoed into the repair or regeneration request.

## Endings: what stays authoritative

| Ending | Owner | Adaptive behaviour |
|---|---|---|
| `turn_limit`, `manager_escalation`, `maintained_unauthorized_promise`, `exchange_accepted`, `objectives_completed`, `stalemate` (ledger-derived), review endings | code | Lan **must** close (`allowedActions=["close_conversation"]`). The model only words it within the code-chosen tone (`ending_*` guard). It cannot extend the dialogue. |
| `second_silence` | code | handled before any model call (fixed text) |
| trust challenge opening (`challengeShownNow`) | code | not an ending; Lan must voice it (`voice_trust_doubt`) because scoring now expects a response |
| `customer_closed` (new) | model, within code limit | Lan may leave early when `closeAllowed`: no deterministic ending, the turn is not uncertain, no trust challenge opens now, turns remain, and the trailing turns without progress (bad conduct or no progress) are ≥3, or ≥2 when the current turn is bad. Close reason is `unresolved_frustration` or `needs_time`. |

Scoring is unchanged. `customer_closed` is a session-level ending like `stopped_early`/`turn_limit`: not in `FORCED_ENDINGS`, so trust follows the ledger. A legacy and an adaptive session with the same labels get identical scores, components, violations and trust; only `endingReason` differs (see `test_early_close_changes_ending_but_not_deterministic_score`). The deterministic concern/hint ladder still advances in the ledger and is shown to the model as advice. It no longer selects Lan's reaction in adaptive sessions.

## Benchmark (generation-only replay)

Both harnesses replay the 10 discovery snapshots and time only Lan's response generation. **They are not end-to-end gameplay measurements:** STT, Jev/Luna and TTS are excluded.

### Four arms: v1 vs v2, thinking off vs on (`scripts/benchmark_sales_adaptive_arms.py`)

Arms `v1_off`, `v1_on`, `v2_off` and `v2_on` run over 10 cases × 3 repeats = 120 samples. A v1 sample makes at most 2 HTTP requests. A v2 sample makes at most 4: 2 generations, each followed by at most one verifier request.

Use `--arms v1_off,v2_off --cases SA-02,SA-03,SA-06,SA-10 --repeats 1` for eight samples comparing versions with thinking off. Only selected arms are scheduled and charged. Reasoning-pair payload checks apply when both reasoning settings of a version are selected; shared request settings are checked across all selected arms. Arm configs and the schedule remain part of resume identity.

- **v1 is the real v1.** The v1 arms import `scripts/benchmarks/sales_adaptive_v1.py`, the immutable snapshot of the v1 module. Its line-ending-normalized SHA-256 is pinned, and its prompt and schema digests must equal those recorded by the original live run. They use v1's prompt, label-unlocked policy, validator, deadline, late re-check and fallback. v2's guard never judges v1 output.
- **Request settings.** All arms share model, provider pin, `temperature`, `max_tokens` and deadline. Within one version the payloads are identical except `reasoning`, and every run checks this by payload hash. v1 and v2 differ by design in prompt, context shape, allowed actions and validator, so v1 vs v2 is a descriptive comparison.
- **Schedule.** Serial and seeded. Each case runs its four arms back to back in a seeded order that rotates every repeat, and the case order is reshuffled per repeat.
- **Case source.** `--source cached` is the default, for use when the private source session files are unavailable. It replays the exact v1 contexts, v1 policies and legacy-plan fields recorded by an earlier v1 run under the ignored `output/` folder (`--cached-run`). Before any request, each case must pass these checks:
  - the cached context hashes to its recorded `contextSha256` and to every live sample of that case;
  - its prefix and current utterance equal the discovery manifest's `priorDialogue`;
  - its recognized acts equal the selected turn's own labels;
  - no selected or later reply, later utterance, private ID or scoring field appears in either version's messages.

  The v2 context is rendered from the same base by `sales_adaptive_v2.render_context`. A test shows this render equals the gameplay `model_context`. `--source rebuild` instead rebuilds from the session files with `scripts/sales_adaptive_replay.py`.
- **Recorded per sample.** Everything the v1 harness records, plus version, accepted warnings, per-attempt rejection and warning codes, and each attempt's redacted candidate (action, decision, fact IDs and text, so blocked outputs can be read). Cost sums `usage.cost` over every HTTP attempt, including repairs, regenerations, verifier requests and rejected or failed attempts. It is `null` when any attempt lacks it.
  - Every HTTP record carries its `role` (`generation` or `guard`), recognized from the request's system prompt. Generation records keep the redacted candidate. Verifier records keep only `verdict`/`verdictCodes`, so a verdict is never parsed as Lan's reply.
  - Samples and summaries split request counts, latency, tokens and cost by role (`generationAttempts`/`guardAttempts`, `costUsdByRole`, `tokensByRole`, `guardLatencySeconds`, `guardVerdicts`, `guardRejectionCodes`). They also count `regenerateAttempts`.
  - The dry-run simulator answers verifier requests and rejects a fixed simulated phrase, so the regeneration path runs offline.
- **Resume.** `--resume` refuses any change to source hashes, prompts, schema, the v2 verifier settings and prompt digest (`guardRequest`), arm configs, case inputs, validator policies, fallback plans, provenance, seed, repeats or schedule. It also refuses samples that carry another run fingerprint. Saved error samples retain their costs and are not automatically sent again. A non-empty new `--run-dir` is never overwritten.
- **Outputs.** Everything goes to a new `output/sales-adaptive-benchmark/runs/arms-<mode>-<UTC stamp>/`: `config.json`, `cases/`, `samples.jsonl`, `summary.json`, `summary.md` and `grader_reference.json`. Old artifacts are only read. Old absolute `D:\NCKH` paths are rebased in memory and never rewritten.

### Thinking off vs on, v1 only (`scripts/benchmark_sales_adaptive.py`)

This is the original v1-only harness. It needs the private source session files. The arms are:

- `adaptive_off` and `adaptive_on`: identical prompt, schema, context, `temperature=0.4`, `max_tokens=4096`, 45 s deadline and provider pin (`order=[alibaba]`, `allow_fallbacks=false`, `require_parameters=true`, plus the configured data-collection/ZDR preferences). Only `reasoning` differs: `{"enabled": false}` versus `{"enabled": true, "max_tokens": 1024}`. Each sample records payload SHA-256 values excluding `reasoning` to prove identity.
- Optional `--include-legacy` adds `legacy_220_off`, the production plan+writer path. Its prompt, architecture and budget differ, so it is not part of the causal comparison.

Ordering is serial, case-interleaved and seeded. Each case alternates AB/BA across its 3 repeats, and the case order is reshuffled per repeat. That gives 60 adaptive samples, or 90 with the legacy arm. There are at most 2 HTTP attempts per sample.

Each sample records:

- wall-clock ready-to-use seconds, which include attempts, guards and fallback;
- first-attempt latency and the latency of every request;
- per-attempt HTTP status, served model, provider, request ID and finish reason;
- prompt, completion, reasoning, cached and total tokens;
- `reasoningChars` (a length only — reasoning text is never stored);
- provider `usage.cost` (`null` when unknown, never 0);
- rejection codes, outcome (`model`/`fallback`/`harness_error`), action, decision and Lan's text.

### Replay reconstruction (`scripts/sales_adaptive_replay.py`)

The discovery manifest's `sessionMemorySnapshot` is the **final** session state and is never used. Each case is rebuilt from its source file:

- **Session:** a fresh current-version session (current rubric, scenario and memory rules).
- **Memory:** the source's immutable facts, with `shared` initially reset unless `sharedTurnId` is opening or part1. Disclosures from prior turns are added at their original turn during replay, so earlier ledger entries cannot know later prefix facts. Sessions saved without memory (SA-02, SA-06, SA-07, SA-10) are initialized by the current rules and Part 1-linked from their attempt file, with provenance recorded. SA-05 keeps its authentic unlinked memory.
- **Prior turns:** each one goes through `evaluate()` with its ledger labels (post-arbitration) or, failing that, its turn labels. Labels missing from an older question set are set to `false` and listed.
  - Pre-v2 boolean labels (SA-06, SA-10) are mapped by `LEGACY_BOOLEAN_MAP`.
  - If a prior turn would end the dialogue under current rules even though the saved conversation continued (SA-10 turn 1: generous legacy keyword labels), its credit-only labels are dropped. Conduct and safety labels are kept, and both are recorded in provenance.
- **Disclosures:** reconciled to what Lan actually said (`disclosedFactIds`/`volunteeredFactIds`).
- **Action memory:** the prefix's action memory is derived from the current planner's intents.
- **Selected turn:** evaluated with its own labels, since classification precedes Lan's reply. The historical reply and every later turn are excluded.
- **Checks:** each run verifies source SHA-256 before and after, 10 distinct sessions, and a leakage check covering the selected reply, new future text, private IDs and scoring fields.

The effective manifest records amendments when an earlier eligible turn replaces a forced terminal turn. Original discovery files remain unchanged. Participant identifiers and the selected conversation details stay in the private benchmark artifacts.

### Commands (PowerShell, from `ai/`)

Paid live runs are a separate, explicitly authorized step; offline tests and dry runs need no key.

```powershell
.\.venv\Scripts\python.exe scripts/run_tests.py app.tests.test_sales_adaptive app.tests.test_sales_adaptive_v2 app.tests.test_sales_adaptive_v2_language app.tests.test_sales_adaptive_arms app.tests.test_sales_adaptive_benchmark
# Four arms, no network/key/cost: case checks, payload identity, simulated transport end to end.
.\.venv\Scripts\python.exe -X utf8 scripts\benchmark_sales_adaptive_arms.py dry-run
# Paid: 120 samples (at most 360 requests: 60 v1 x 2, 60 v2 x 4). Reads OPENROUTER_API_KEY from env/.env.
.\.venv\Scripts\python.exe -X utf8 scripts\benchmark_sales_adaptive_arms.py live --confirm-paid
# Continue an interrupted run with the same flags; any input change is refused.
.\.venv\Scripts\python.exe -X utf8 scripts\benchmark_sales_adaptive_arms.py live --confirm-paid --resume ..\output\sales-adaptive-benchmark\runs\arms-live-<stamp>
# v1-only harness (needs the source session files under recordings/ and backup/).
.\.venv\Scripts\python.exe -X utf8 scripts\benchmark_sales_adaptive.py dry-run --include-legacy
```

The v1-only harness writes `runs/<mode>-<UTC stamp>/` under `output/sales-adaptive-benchmark/`, containing:

  - `effective_manifest.json` (the shared one in `output/sales-adaptive-benchmark/` is never rewritten);
  - `config.json`: arm configs, checks and schedule;
  - `cases/SA-xx.json`: provenance, policy, legacy plan and exact model context;
  - `samples.jsonl`;
  - `summary.json` and `summary.md`;
  - `grader_reference.json`: historical replies for blinded review only.

## Limitations

- Fixed-prefix, one-turn replay. It does not show how a player would answer the new reply. A free-running multi-turn check is still needed.
- Guards are conservative keyword/regex checks. False rejections fall back safely but reduce adaptivity; true semantic violations may slip past them.
- The v2 language verifier is the same model judging its own family's output. It can pass mixed-language or meaningless text, or reject valid colloquial Vietnamese, which costs a regeneration or a fallback. Text in the candidate could try to steer the verdict; the strict schema limits the effect to pass/reject. Its detection quality, false-rejection rate and added latency have not been measured live. The offline tests mock its verdicts and only cover request flow, limits, accounting and privacy.
- Lan cannot volunteer a private fact until a recognized question releases it. This keeps the ledger and memory consistent.
- Older sessions are replayed under current rules with mapped or absent labels. Their states are approximations, documented per case.
- A 45 s deadline is far longer than the legacy 4 s writer. Real-time suitability depends on the measured latency.
- Unity receives the new `endingReason` value `customer_closed` with `conversationComplete=true`. The flow matches other endings; the focused clarification counter test does not verify this adaptive close or headset behaviour.
