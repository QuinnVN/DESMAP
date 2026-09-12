# Evaluation UI contract

The experience route remains frontend-only. `/evaluation` reads the questionnaire contract from `$lib/questionnaire` in `onMount`:

- `readCompletionPayload()` supplies a completed submission and score dimensions.

The initial assessment adapter in `$lib/assessment` sends grouped DESMAP scores and career candidates through the same-origin proxy. It validates percentage-only responses, caches successful responses by assessment ID and request fingerprint, and maps the highest result to the experience slug used by `/experiences?career=...`. Fine scores remain in the browser completion record for a future final assessment and are never sent to this endpoint.

The scene crops in `assets/` are extracted from the supplied design reference for card imagery. Experience cards open accessible Bits UI detail dialogs and intentionally do not launch VR modules.
