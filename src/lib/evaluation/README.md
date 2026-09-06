# Evaluation UI contract

The evaluation and experience routes are frontend-only. `/evaluation` reads the questionnaire contract from `$lib/questionnaire` in `onMount`:

- `readCompletionPayload()` supplies a completed submission and score dimensions.
- `readSavedQuestionnaire()` supplies a partial draft for the resume state.

`createEvaluationModel` keeps the self-assessment dimensions tied to the submitted scores. Career match percentages and the AI report are deliberately marked illustrative/demo until a backend evaluation endpoint exists. The selected career-interest id maps to the target experience slug used by `/experiences?career=...`.

The scene crops in `assets/` are extracted from the supplied design reference for card imagery. Experience cards open accessible Bits UI detail dialogs and intentionally do not launch VR modules.
