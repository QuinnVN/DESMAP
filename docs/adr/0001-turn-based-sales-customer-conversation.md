---
status: accepted
---

# Use turn-based HTTP for the sales customer conversation

Sales Part 2 sends complete player voice turns over idempotent HTTP requests and receives complete Vietnamese customer text replies. The backend owns conversation history under one pseudonymous Sales session, while a separate final LLM call analyzes the phase-tagged transcript and assigns the trust state. This design matches the current batch transcription and non-streaming model services, supports reliable retry and diagnostic audio retention, and avoids adding continuous audio infrastructure before customer speech or interruption is required.

## Considered options

- Continuous bidirectional audio streaming would support interruption and future speech output, but the current experience has neither a streaming STT path nor customer TTS. It would add a second conversation transport before those features are in scope.
- A single LLM call for both customer behavior and final judgment would reduce requests, but it could leak assessment language into the character response and make the visible conversation responsible for grading itself.

## Consequences

The customer cannot interrupt the player, and each turn includes transcription and generation latency. Unity pauses gameplay timers while backend work runs. Every turn needs a stable ID so a retry cannot duplicate conversation history. Future TTS can consume the returned customer text without changing the turn contract, but true interruption will require a new streaming protocol.
