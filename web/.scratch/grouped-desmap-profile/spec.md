# Detailed grouped DESMAP profile

Status: ready-for-agent

## Problem Statement

After completing the questionnaire, a participant can see six DESMAP stage percentages but cannot inspect the 28 grouped DESMAP scores that produced their profile. The evaluation page therefore hides the most specific questionnaire results available for the initial assessment.

The 28 grouped DESMAP scores have different raw maximums, so raw points cannot share one direct 0 through 4 interpretation. Participants need a consistent, readable assessment label for every grouped score without introducing unsupported claims about strengths, gaps, career recommendations, or per-dimension career fit.

The grouped scores already exist before the initial career match finishes. A slow or failed career-matching request should not prevent a participant from reading valid questionnaire results stored on their device.

## Solution

Replace the compact six-row DESMAP profile on the evaluation page with a detailed profile organized into the six DESMAP stages. Each stage is an expandable section containing its grouped DESMAP scores in canonical order. The first stage starts expanded and the other five start collapsed.

Each stage header shows the Vietnamese stage name and its existing stage percentage. Each grouped-score row shows only the Vietnamese grouped-dimension name and one textual `Mức đánh giá` label. It does not show raw points, percentages, a numeric level, descriptions, progress bars, or segmented indicators.

Convert each normalized grouped DESMAP percentage to an internal level by rounding `percentage × 4 ÷ 100` to the nearest integer. Map the result to the agreed Vietnamese labels: `Không tương thích`, `Kém tương thích`, `Bình thường`, `Khá tương thích`, and `Tương thích tốt` for levels 0 through 4 respectively.

Render the detailed profile as soon as a valid completed questionnaire record is available. Keep it visible while the initial career match is loading, after a career-matching error, after a successful match, and when AI evaluation is disabled. The profile reports questionnaire results and remains separate from the career-level initial match.

## User Stories

1. As a participant, I want to inspect all 28 grouped DESMAP scores, so that I can understand my questionnaire profile beyond the six stage totals.
2. As a participant, I want grouped DESMAP scores organized under the six DESMAP stages, so that I can understand how each score belongs to the overall framework.
3. As a participant, I want the stages ordered D, E, S, M, A, and P, so that the detailed profile follows the questionnaire's established order.
4. As a participant, I want grouped scores ordered by their canonical identifiers within each stage, so that the presentation remains stable between visits.
5. As a participant, I want each stage header written in Vietnamese, so that the profile matches the rest of the website.
6. As a participant, I want each stage header to show its stage percentage, so that I can scan the six broad results before opening the details.
7. As a participant, I want the first stage expanded when the profile appears, so that I immediately see how the detailed presentation works.
8. As a participant, I want the remaining stages collapsed initially, so that the page remains manageable on a small screen.
9. As a participant, I want to expand and collapse each stage independently, so that I can focus on one part of the profile at a time.
10. As a keyboard user, I want every stage control to work without a pointer, so that I can inspect the full profile using a keyboard.
11. As a screen-reader user, I want each stage control to expose whether it is expanded, so that I know which grouped scores are currently available.
12. As a screen-reader user, I want the grouped-dimension name and its assessment label read together, so that each result has an unambiguous meaning.
13. As a participant, I want every grouped-score row to show its Vietnamese name, so that internal identifiers such as D1 and P6 do not become the primary explanation.
14. As a participant, I want every grouped score summarized with a `Mức đánh giá` label, so that I can understand its normalized level without comparing different raw maximums.
15. As a participant with a level of 0, I want to see `Không tương thích`, so that the lowest normalized level uses the agreed wording.
16. As a participant with a level of 1, I want to see `Kém tương thích`, so that the second normalized level uses the agreed wording.
17. As a participant with a level of 2, I want to see `Bình thường`, so that the middle normalized level uses the agreed wording.
18. As a participant with a level of 3, I want to see `Khá tương thích`, so that the fourth normalized level uses the agreed wording.
19. As a participant with a level of 4, I want to see `Tương thích tốt`, so that the highest normalized level uses the agreed wording.
20. As a participant, I do not want raw points shown in a grouped-score row, so that the differing maximums do not make rows appear directly comparable.
21. As a participant, I do not want grouped-score percentages or numeric levels shown in a row, so that each row stays focused on the agreed text label.
22. As a participant, I do not want descriptive paragraphs repeated for every grouped score, so that the 28-item profile remains easy to scan.
23. As a participant, I do not want progress bars or five-segment indicators, so that the label remains the only visual expression of the grouped level.
24. As a participant, I want my detailed profile to appear while career matching is loading, so that I can read my questionnaire results without waiting for the external evaluator.
25. As a participant, I want my detailed profile to remain visible if career matching fails, so that an unrelated service error does not hide valid local results.
26. As a participant, I want my detailed profile to remain visible after career matching succeeds, so that I can view grouped questionnaire results alongside the career-level result.
27. As a participant using deterministic mode, I want the detailed profile to remain visible, so that disabling AI does not hide questionnaire-derived information.
28. As a participant without a completed questionnaire record, I want the existing empty-state guidance instead of an empty detailed profile, so that I know how to obtain results.
29. As a participant, I want the detailed profile to remain readable on mobile and desktop, so that stage names and labels do not overlap or become truncated beyond recognition.
30. As a participant using high zoom, I want grouped-score rows to reflow rather than require horizontal scrolling, so that the results remain usable.
31. As a participant, I want career-level initial matches kept separate from grouped DESMAP labels, so that I do not mistake a questionnaire score for per-dimension career compatibility.
32. As a researcher, I want the feature to use the existing saved questionnaire scores without recalculating or mutating them, so that presentation changes do not alter assessment data.
33. As a product maintainer, I want grouped names and stage metadata read from their canonical typed configuration, so that the evaluation page does not create another conflicting copy.
34. As a product maintainer, I want all new user-facing copy written in Vietnamese, so that the feature follows the website language requirement.
35. As a developer, I want the detailed profile isolated from the career evaluator contract, so that this presentation change does not require a backend or AI response change.

## Implementation Decisions

- Replace the existing compact six-stage profile with one detailed grouped DESMAP profile in the evaluation results area.
- Treat a grouped DESMAP score as one of the 28 normalized questionnaire scores. Do not describe the six stage scores as grouped dimensions.
- Organize the 28 grouped scores under the six stages in D, E, S, M, A, and P order. Preserve canonical grouped-dimension order within each stage.
- Use the existing typed grouped-dimension metadata as the source of the 28 Vietnamese names and identifiers.
- Use the existing questionnaire stage metadata as the source of stage order and Vietnamese stage names. Avoid adding another page-local copy of these values.
- Continue using the saved stage percentages for stage headers. Do not derive a stage percentage by averaging its grouped percentages because the groups contain different numbers of questions.
- Use accessible disclosure controls for the six stages. The D stage starts open. E, S, M, A, and P start closed.
- Allow participants to open or close each stage independently. Do not force an accordion rule that closes one stage when another opens.
- Calculate an internal display level as the rounded value of the grouped score percentage multiplied by four and divided by 100.
- The resulting integer thresholds are 0 through 12 percent for level 0, 13 through 37 for level 1, 38 through 62 for level 2, 63 through 87 for level 3, and 88 through 100 for level 4.
- Map levels to exact visible labels: `Không tương thích`, `Kém tương thích`, `Bình thường`, `Khá tương thích`, and `Tương thích tốt`.
- Present each grouped-score row with only the grouped-dimension name and its `Mức đánh giá` text. Do not display the grouped identifier, raw score, raw maximum, grouped percentage, internal 0 through 4 level, description, progress bar, or segmented scale.
- Keep the assessment label textual and programmatically associated with its grouped-dimension name. Color may support hierarchy but cannot be the only carrier of meaning.
- Render the profile whenever the page has a valid completed questionnaire payload. Its visibility must not depend on the initial career-match request state or response mode.
- Keep the profile visible during career-match loading, recoverable and non-recoverable career-match errors, successful career matches, cached career matches, and deterministic mode.
- Preserve the existing empty state when no valid completed questionnaire payload exists.
- Keep the initial career match as a separate career-level result. Do not present grouped-score labels as AI output, career recommendations, personal strengths, gaps, or dimension-level career matches.
- Make no changes to the initial-assessment request, response, cache, ranking, career weight matrix, or backend endpoint.
- Keep the current dark DESMAP palette, blue structural borders, lime emphasis, and established typography. Treat the profile as one continuous disclosure list rather than a collection of detached cards.
- Reflow each grouped-score row on narrow screens so the name and `Mức đánh giá` remain readable without horizontal scrolling.
- Retain visible keyboard focus and reduced-motion behavior. The disclosure interaction does not require decorative animation.

## Testing Decisions

- Test externally visible behavior rather than internal helper names, CSS class strings, private state, or exact component structure.
- Use one primary automated seam: render the detailed grouped DESMAP profile with controlled, complete score fixtures and inspect its accessible output.
- Verify that the rendered profile contains six stage controls in canonical order and all 28 grouped-dimension names exactly once.
- Verify that the D stage starts expanded and the other five stages start collapsed.
- Verify that stage headers expose their existing stage percentages and that controls expose expanded state to assistive technology.
- Verify the five visible labels through rendered fixtures at the conversion boundaries: 0, 12, 13, 37, 38, 62, 63, 87, 88, and 100 percent.
- Verify that grouped rows do not render raw points, grouped percentages, internal numeric levels, grouped identifiers, descriptions, progress bars, or segmented indicators.
- Verify that a grouped-dimension name and its assessment label have an accessible association in the rendered output.
- Verify keyboard operation and visible focus for every stage disclosure during manual browser review.
- Verify mobile reflow, long Vietnamese names, browser zoom, and all six expanded sections during manual responsive review.
- Verify on the evaluation page that the detailed profile appears with a valid questionnaire payload during career-match loading, after a career-match error, after success, and in deterministic mode.
- Verify that the existing empty state remains unchanged when no valid completed questionnaire payload is available.
- Use the existing server-rendered component tests for the career-match panel as prior art for fixtures and output assertions.
- Keep the existing public initial-assessment adapter tests unchanged unless implementation reveals a regression. This feature does not alter assessment request or response behavior.
- Run the unit test suite, Svelte type checking, linting, and a production build after implementation.
- Run the Svelte autofixer on every new or modified Svelte component until it returns no issues or suggestions.

## Out of Scope

- Changing the initial career match calculation or its career-level percentage.
- Adding per-dimension career compatibility, AI explanations, score attribution, or career-weight contribution details.
- Showing raw grouped points, grouped percentages, numeric 0 through 4 levels, fine DESMAP scores, or question-level answers.
- Showing descriptive or interpretive paragraphs for grouped scores.
- Adding progress bars, radar charts, five-segment indicators, or other score visualizations.
- Changing the questionnaire scoring algorithm, answer values, grouped-score maximums, or six-stage aggregation.
- Changing the initial-assessment API, backend evaluator, deterministic evaluator, cache format, or browser completion-record schema.
- Changing career candidates, career weights, VR experiences, or the final assessment.
- A broad redesign of the evaluation page outside the detailed DESMAP profile and the layout changes needed to keep it visible across request states.

## Further Notes

- The profile describes the participant's normalized questionnaire results. It does not explain compatibility with a particular career.
- Grouped-score raw maximums vary because groups contain different numbers of questions. The feature uses the stored normalized percentage only to select a text label.
- The internal 0 through 4 level is a presentation rule. It is not persisted, sent to the initial-assessment API, or shown to the participant.
- The stage percentage remains a separate aggregate based on all questions in that stage. In particular, the Social Role stage is not a simple average of S1, S2, and S3.
- This specification supersedes the earlier request to display how many raw points the participant earned for each grouped score.
