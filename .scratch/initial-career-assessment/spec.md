# Initial career assessment

Status: ready-for-agent

## Problem Statement

A participant can complete the DESMAP questionnaire, but the website does not request an AI assessment. It stores the questionnaire in the browser and shows fixed demonstration percentages and report content. Those values are not derived from the participant's answers.

The existing AI contract does not fit the approved initial assessment. It accepts only 20 dimensions even though the questionnaire produces 28 grouped DESMAP scores. It also requires a written evaluation that the product must not show at this stage. The initial assessment has only self-reported questionnaire data, so it cannot support claims about personal strengths, gaps, VR observations, or a final career recommendation.

## Solution

After questionnaire completion, the website will preserve the participant's full questionnaire record, navigate to the evaluation page, and request an initial assessment through a local SvelteKit proxy. The request will contain the 28 grouped DESMAP scores and the career candidates derived from the participant's selected career interests. Each career candidate will include all 28 criteria from an editable, typed weight matrix.

The renamed AI endpoint will use Qwen to return one provisional match percentage for each career candidate. It will return no written evaluation. The website will cache the successful response, rank the percentages for display, retain the six-stage DESMAP profile, and remove unsupported claims from completed initial results.

The website will retain all 35 fine DESMAP scores under the same stable assessment identifier for a future final assessment. This feature will not store questionnaire data on the backend or combine it with telemetry.

## User Stories

1. As a participant, I want to select up to three career interests before the questionnaire, so that the initial assessment considers careers related to what I want to explore.
2. As a participant, I want each selected career interest to map to the available career candidates, so that the results refer to experiences the product can offer.
3. As a participant, I want duplicate career candidates removed, so that selecting related interests does not show the same career more than once.
4. As a participant who selects "Tôi vẫn đang khám phá", I want all five available careers assessed, so that I can compare them without choosing a field first.
5. As a participant, I want the website to accept my completed questionnaire before contacting the AI service, so that a model failure cannot erase my work.
6. As a participant, I want one stable assessment identifier assigned to my completed questionnaire, so that its initial result and future analysis can refer to the same attempt.
7. As a participant, I want to reach the evaluation page immediately after completion, so that I can see that the assessment is being prepared.
8. As a participant, I want a visible Vietnamese loading message while Qwen is processing the request, so that I know the website has accepted my questionnaire.
9. As a screen-reader user, I want loading and result state changes announced without stealing focus, so that I can follow the assessment progress.
10. As a participant, I want duplicate submissions disabled while a request is pending, so that one click does not create multiple model requests.
11. As a participant, I want the six-stage DESMAP profile to remain visible, so that I can review the questionnaire scores separately from the AI result.
12. As a participant, I want each initial career match displayed as a percentage, so that I can compare the selected career candidates.
13. As a participant, I want matches sorted from highest to lowest, so that the strongest provisional match is easy to find.
14. As a participant, I want tied percentages to keep a stable order, so that the result does not move between renders or reloads.
15. As a participant, I want the website to label the percentages as provisional initial matches, so that I do not mistake them for career recommendations.
16. As a participant, I do not want the initial assessment to claim that it identified my strengths, so that self-reported questionnaire data is not overstated.
17. As a participant, I do not want the initial assessment to claim that it identified my gaps, so that the product does not present unsupported conclusions.
18. As a participant, I do not want the initial assessment to show VR observations, so that it does not imply that I completed an experience that has not occurred.
19. As a participant, I do not want a written AI evaluation at this stage, so that the result remains limited to the approved percentage.
20. As a participant, I want model and network failures explained in Vietnamese, so that I understand why no percentage is available.
21. As a participant, I want to retry a recoverable failure from the evaluation page, so that I do not need to repeat the questionnaire.
22. As a participant, I do not want demonstration percentages substituted after a real request fails, so that every displayed result has a clear source.
23. As a participant, I want a successful result reused after a refresh, so that the percentage remains stable and the model is not called again unnecessarily.
24. As a participant, I want a changed questionnaire or career-weight configuration to invalidate an old cached result, so that stale percentages are not displayed.
25. As a participant, I want the highest initial match to link to its available VR experience, so that I can continue exploring that career.
26. As a participant, I want an unavailable highest-match experience labeled "Sắp ra mắt", so that I am not sent to locked content.
27. As a participant, I want all visible controls to work with a keyboard and show focus, so that I can use the evaluation page without a pointer.
28. As a participant, I want all visible assessment text in Vietnamese, so that the flow uses the website's established language.
29. As a researcher, I want the 35 fine DESMAP scores retained with the assessment identifier, so that a future final assessment can use the detailed questionnaire record.
30. As a researcher, I want the 28 grouped scores distinguished from the 35 fine scores, so that later analysis does not confuse initial input with retained detail.
31. As a product maintainer, I want the five-career weight matrix in one typed configuration, so that I can adjust weights without editing components or backend logic.
32. As a product maintainer, I want invalid weights and incomplete matrices rejected by automated tests, so that a manual edit cannot silently corrupt requests.
33. As a backend maintainer, I want the initial endpoint named separately from a future final endpoint, so that the two assessment stages cannot be confused.
34. As a backend maintainer, I want every request and model response checked against strict schemas, so that malformed data never reaches the result page.
35. As a developer, I want the website to reach the loopback backend through a same-origin server proxy, so that local browser requests do not depend on backend CORS.
36. As a developer, I want request construction and result ordering behind one public frontend adapter, so that contract behavior can be tested without mounting the full page.
37. As a future final-assessment developer, I want the initial feature to preserve detailed data without interpreting telemetry, so that the later feature begins with a clear boundary.

## Implementation Decisions

- Treat the initial assessment and final assessment as separate domain operations. This work implements only the questionnaire-only initial assessment.
- Rename the AI route to `POST /api/ai/initial-career-assessment`. A future questionnaire-plus-telemetry operation must use a separate contract and endpoint.
- Rename the API contract documentation to identify the initial assessment and update every internal reference to the old route.
- Keep the request object strict. It contains `assessment_id`, `dimensions`, and `careers`; unknown fields remain invalid.
- Allow 1 through 28 dimensions. The website always sends all 28 grouped DESMAP scores for an initial assessment.
- Allow 1 through 28 criteria per career. The website sends exactly 28 criteria for every career candidate, with one unique criterion referencing each supplied grouped dimension.
- Keep importance values as integers from 1 through 5. Every approved matrix cell affects the corresponding career candidate.
- Keep career limits compatible with the current contract. The initial website catalog supplies at most five unique career candidates.
- Remove `evaluation` from the initial response contract, model schema, prompt, parser, repair prompt, examples, and tests. Each result contains only `career_id`, `career_name`, and `match_percentage`.
- Preserve the request assessment identifier, career identifiers, career names, career count, and request order in the backend response. The backend rejects a model response that changes any of them.
- Keep percentages independent. They are integers from 0 through 100 and do not need to total 100.
- Keep Qwen as the evaluator. The model interprets grouped DESMAP scores, career descriptions, and importance weights rather than applying a fixed arithmetic formula.
- Reduce the initial generation temperature from 0.6 to 0.2. Keep strict JSON output, hidden reasoning, the existing bounded request behavior, and one formatting-repair attempt.
- Change the system prompt so it requests percentages only. It must prohibit written evaluations, strengths, gaps, recommendations, diagnoses, guarantees, telemetry claims, VR observations, hidden reasoning, and additional output.
- Extend the versioned browser completion record with one schema-safe stable identifier in the form `assessment-<UUID>`. Reuse that identifier for requests, cached results, export, retry, and future handoff.
- Retain the full questionnaire answers, all 35 fine DESMAP scores, all 28 grouped scores, six stage scores, selected career interests, and timestamps in the browser completion record.
- Upgrade a valid older completion record that lacks an assessment identifier by assigning one identifier and rewriting the record. Do not make a participant repeat a valid completed questionnaire only because the local record predates this feature.
- Do not send fine DESMAP scores to the initial endpoint. Do not add fine scores as unknown request fields.
- Use this career-interest mapping:
  - "Công nghệ & kỹ thuật", "Môi trường & bền vững", and "Vận hành & nghề kỹ thuật" map to Kỹ sư ô tô.
  - "Khoa học & nghiên cứu" and "Sức khỏe & hạnh phúc" map to Bác sĩ.
  - "Thiết kế & sáng tạo" and "Con người & giáo dục" map to Giáo viên.
  - "Kinh doanh & khởi nghiệp" and "Truyền thông & giao tiếp" map to Nhân viên kinh doanh.
  - "Luật & dịch vụ công" maps to Luật sư.
  - "Tôi vẫn đang khám phá" maps to all five career candidates.
- Expand interests in their saved selection order and discard a career identifier after its first occurrence. Expand "Tôi vẫn đang khám phá" using the catalog order.
- Keep the career catalog, interest mapping, grouped-dimension metadata, and weight matrix in one typed assessment configuration. Components consume the configuration through exported functions and types rather than duplicating domain data.
- Validate the configuration in tests. It must contain 28 unique schema-safe grouped dimensions, five unique schema-safe careers, 28 integer weights per career, valid descriptions, and no unknown identifiers.
- Use this approved provisional matrix:

| Grouped DESMAP dimension | Bác sĩ | Luật sư | Giáo viên | Nhân viên kinh doanh | Kỹ sư ô tô |
| --- | ---: | ---: | ---: | ---: | ---: |
| D1 Thu nhập, phúc lợi, ổn định | 3 | 3 | 2 | 5 | 4 |
| D2 Học hỏi, phát triển, thử thách | 5 | 4 | 5 | 4 | 5 |
| D3 Tự chủ | 2 | 4 | 3 | 4 | 4 |
| D4 Ý nghĩa và đóng góp | 5 | 4 | 5 | 3 | 3 |
| D5 Công nhận và ảnh hưởng | 2 | 4 | 3 | 5 | 3 |
| D6 Điều kiện và cân bằng công việc | 2 | 2 | 3 | 2 | 3 |
| E1 Kỹ năng nền tảng | 5 | 5 | 5 | 4 | 4 |
| E2 Giải quyết vấn đề phức tạp | 5 | 5 | 4 | 4 | 5 |
| E3 Kỹ năng tương tác xã hội | 5 | 5 | 5 | 5 | 3 |
| E4 Kỹ năng kỹ thuật | 3 | 1 | 2 | 2 | 5 |
| E5 Kỹ năng hệ thống | 4 | 4 | 4 | 3 | 5 |
| E6 Quản lý nguồn lực | 4 | 4 | 4 | 5 | 4 |
| S1 Vai trò hướng nhiệm vụ | 5 | 5 | 4 | 4 | 5 |
| S2 Vai trò duy trì quan hệ | 5 | 3 | 5 | 5 | 3 |
| S3 Vai trò định hướng cá nhân | 2 | 4 | 2 | 4 | 3 |
| M1 Tư duy phân tích | 5 | 5 | 4 | 4 | 5 |
| M2 Tư duy sáng tạo | 3 | 4 | 5 | 4 | 4 |
| M3 Tư duy thực tiễn | 5 | 4 | 5 | 5 | 5 |
| A1 Chuẩn bị cho tương lai | 4 | 4 | 4 | 3 | 4 |
| A2 Chủ động và chịu trách nhiệm | 5 | 5 | 4 | 5 | 5 |
| A3 Khám phá khả năng mới | 4 | 4 | 5 | 4 | 5 |
| A4 Tự tin vượt qua khó khăn | 5 | 5 | 4 | 5 | 5 |
| P1 Áp lực thời gian và tốc độ | 5 | 5 | 4 | 5 | 4 |
| P2 Áp lực khối lượng công việc | 5 | 5 | 4 | 5 | 4 |
| P3 Áp lực tư duy và quyết định | 5 | 5 | 4 | 4 | 5 |
| P4 Áp lực cảm xúc | 5 | 4 | 5 | 5 | 3 |
| P5 Áp lực tương tác và xung đột | 4 | 5 | 5 | 5 | 3 |
| P6 Áp lực trách nhiệm và hậu quả | 5 | 5 | 4 | 4 | 5 |

- Add one same-origin SvelteKit POST endpoint for the browser. It forwards the validated body to `http://127.0.0.1:8000/api/ai/initial-career-assessment` and returns the backend status and safe response.
- Keep the loopback backend address fixed for this local-only implementation. Assume backend token authentication is disabled. Public deployment, configurable remote origins, and browser-direct CORS are outside this work.
- Put request construction, career expansion, strict response validation, stable ranking, and cache fingerprinting behind one public frontend assessment adapter. Keep browser storage and network calls at its boundary so tests can supply controlled implementations.
- Build a canonical request fingerprint from every request field, including the current weights. A cached success is valid only when its assessment identifier and request fingerprint match the current request.
- Cache only successful, validated responses. Preserve the completion record after all failures. Never cache error responses or demonstration values.
- On questionnaire completion, persist the completion record, clear only the draft, and navigate to the evaluation page without waiting for Qwen.
- The evaluation page owns `loading`, `success`, and `error` states. It first reads the completion record, checks for a matching cached result, and requests an assessment only when no valid cache exists.
- Prevent concurrent requests for the same page state. A retry starts one new request only after the previous request has failed.
- Treat 502, 503, and network failures as recoverable. Show a Vietnamese error and a retry action. Treat 401 and 422 as configuration or data errors, show a specific Vietnamese message, and retain the completion record.
- Do not impose a browser timeout shorter than the backend's initial request plus its possible formatting-repair request.
- Keep the locally calculated six-stage profile and its text values separate from the initial career matches.
- Replace fixed career percentages with validated API results for a completed questionnaire. Do not fall back to fixed percentages when an API request fails.
- Sort a copy of the validated results by descending percentage for display. Break ties using the original backend response order. Keep the cached raw response in backend order.
- Treat the first displayed result as the initial target career. Resolve its experience by career identifier, not by translated display text.
- If the target experience is locked, disable its action and show "Sắp ra mắt". Do not silently select a lower-ranked available career.
- Remove or hide completed-result sections and labels that claim an AI report, personal strengths, gaps, a roadmap, final conclusions, or observed VR behavior. An explicitly labeled sample preview may remain available before questionnaire completion, but it cannot appear as a fallback for a completed initial assessment.
- Update JSON export and copied summaries so they identify the result as an initial career match and contain no demonstration or final-assessment claims.
- Keep every user-facing loading, success, error, retry, cache, and availability message in Vietnamese.
- Preserve semantic status and alert announcements, visible keyboard focus, disabled-state semantics, and textual score equivalents for nonvisual users.

## Testing Decisions

- Tests assert externally visible contract behavior. They must not depend on private helper names, component markup structure, internal state variable names, or exact model prose.
- Use one primary frontend test seam: the public initial-assessment adapter. Supply questionnaire records, career interests, configuration, storage, and network responses through its public inputs, then assert the produced request, cache decision, error classification, and ranked result.
- Add Vitest as the website's unit-test runner and add a project script that runs the suite noninteractively.
- Test that request construction sends exactly 28 grouped dimensions and never sends the 35 fine dimensions.
- Test that every career candidate receives exactly 28 criteria with the approved weights.
- Test every career-interest mapping, stable deduplication, and the all-career expansion for "Tôi vẫn đang khám phá".
- Test stable assessment-identifier creation and upgrade of valid older completion records.
- Test strict response validation, including missing fields, extra fields, changed identities, changed order, percentages outside 0 through 100, duplicate results, and an unexpected career count.
- Test descending display order and stable request-order tie handling without changing the cached raw response.
- Test cache hits, cache misses, invalid JSON, mismatched identifiers, changed questionnaire scores, changed career candidates, changed weights, and corrupted local storage.
- Test that only successful validated responses enter the cache.
- Test recoverable classification for 502, 503, and network errors. Test non-recoverable configuration or data messaging for 401 and 422.
- Keep backend contract tests at the existing assessment request, response, parser, prompt, and route boundaries. This is the highest existing backend seam and avoids testing Qwen internals.
- Update backend tests for the renamed route, 28-dimension and 28-criterion limits, removed evaluation field, strict percentage-only model schema, identity and order preservation, generation settings, and the single repair attempt.
- Add boundary cases that accept 28 items and reject 29 items for both dimensions and criteria.
- Keep existing backend transport tests for unavailable, rejected, malformed, and empty model responses. Do not require a live Qwen server in the automated suite.
- The website has no prior automated test suite. Its existing pure questionnaire scoring module is the closest design precedent for the new adapter. Backend unit tests provide prior art for contract-focused fixtures and fake model services.
- Run the website Vitest suite, Svelte type checking, linting, and production build. Report pre-existing lint or formatting failures separately from failures introduced by this work.
- Run the focused backend career-assessment and LLM transport tests. Run the full backend suite if the focused tests pass.
- Manually verify questionnaire completion, evaluation loading, successful percentages, refresh cache reuse, retry after a stopped backend, invalid local data, keyboard operation, responsive layout, and the locked-career action.

## Out of Scope

- The final assessment that combines questionnaire data with telemetry.
- Collection, interpretation, storage, or joining of VR telemetry.
- Backend persistence of questionnaire records, fine DESMAP scores, or initial results.
- User accounts, cross-device result synchronization, retention policy, or deletion policy.
- Scientific validation or calibration of the provisional career-weight matrix.
- Personal-strength analysis, gap analysis, written AI evaluations, development roadmaps, diagnoses, guarantees, or career recommendations.
- New career candidates, new VR experiences, or changes to locked experience availability.
- A browser-based or administrator weight editor.
- Public deployment, backend CORS, remote backend discovery, production secrets, or authenticated proxying.
- Playwright, end-to-end browser automation, or a broad redesign of the evaluation page.
- Changes to the future final-assessment API contract.

## Further Notes

- The matrix is approved as provisional product data. A maintainer may change it in the typed configuration and rebuild the website.
- Any questionnaire score, career mapping, description, or weight change changes the request fingerprint and invalidates the cached initial result.
- The model percentage is an AI judgment, not the direct result of an arithmetic weighting formula. The product must not imply that a weight change guarantees a specific numeric delta.
- The full 35-score record remains browser-local until the final-assessment identity, privacy, persistence, and telemetry contracts are designed.
- Existing unrelated website changes must remain untouched during implementation.
