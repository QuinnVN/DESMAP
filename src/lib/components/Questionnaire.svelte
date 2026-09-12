<script lang="ts">
	import { onMount, tick } from 'svelte';
	import { goto } from '$app/navigation';
	import type {
		OptionLetter,
		QuestionnaireAnswers,
		QuestionnaireDraft,
		QuestionnairePresentationOrder,
		StageId
	} from '$lib/questionnaire';
	import {
		buildCompletionPayload,
		careerInterestOptions,
		clearSavedQuestionnaire,
		createQuestionnairePresentationOrder,
		getStageById,
		optionsInPresentationOrder,
		questionnaireStages,
		questionsInPresentationOrder,
		readSavedQuestionnaire,
		totalQuestionCount,
		writeCompletionPayload,
		writeSavedQuestionnaire
	} from '$lib/questionnaire';

	type Mode = 'career' | 'questions' | 'review';

	let hydrated = $state(false);
	let mode = $state<Mode>('career');
	let currentIndex = $state(0);
	let selectedCareerInterests = $state<string[]>([]);
	let answers = $state<QuestionnaireAnswers>({});
	let startedAt = $state('');
	let statusMessage = $state('');
	let errorMessage = $state('');
	let presentationOrder = $state<QuestionnairePresentationOrder | null>(null);

	let orderedQuestions = $derived(
		presentationOrder ? questionsInPresentationOrder(presentationOrder) : []
	);
	let currentQuestion = $derived.by(() => orderedQuestions[currentIndex] ?? null);
	let currentOptions = $derived(
		currentQuestion && presentationOrder
			? optionsInPresentationOrder(currentQuestion, presentationOrder)
			: []
	);
	let currentStage = $derived.by(() =>
		currentQuestion ? getStageById(currentQuestion.stage) : null
	);
	let answeredCount = $derived(Object.keys(answers).length);
	let unansweredQuestions = $derived(orderedQuestions.filter((question) => !answers[question.id]));
	const optionLabels: OptionLetter[] = ['A', 'B', 'C'];

	onMount(() => {
		const saved = readSavedQuestionnaire();
		presentationOrder = saved?.presentationOrder ?? createQuestionnairePresentationOrder();
		startedAt = saved?.startedAt ?? new Date().toISOString();
		if (saved) {
			writeSavedQuestionnaire(saved);
			selectedCareerInterests = [...saved.careerInterests];
			answers = { ...saved.answers };
			const firstUnanswered = orderedQuestions.findIndex((question) => !saved.answers[question.id]);
			const complete = firstUnanswered === -1;
			mode = saved.step === 'review' && !complete ? 'questions' : saved.step;
			currentIndex =
				mode === 'questions' && firstUnanswered >= 0
					? firstUnanswered
					: Math.min(Math.max(saved.currentIndex, 0), totalQuestionCount - 1);
			statusMessage = 'Tiến trình đã lưu của bạn đã sẵn sàng.';
		}
		hydrated = true;
	});

	function persistDraft(nextMode: Mode = mode, nextIndex = currentIndex) {
		if (!hydrated || !startedAt || !presentationOrder) return;
		const draft: QuestionnaireDraft = {
			version: 1,
			completed: false,
			step: nextMode,
			currentIndex: nextIndex,
			startedAt,
			updatedAt: new Date().toISOString(),
			careerInterests: [...selectedCareerInterests],
			answers: { ...answers },
			presentationOrder
		};
		if (writeSavedQuestionnaire(draft)) statusMessage = 'Đã lưu tiến trình trên thiết bị này';
	}

	function toggleCareerInterest(id: string) {
		errorMessage = '';
		if (selectedCareerInterests.includes(id)) {
			selectedCareerInterests = selectedCareerInterests.filter((interest) => interest !== id);
		} else if (selectedCareerInterests.length < 3) {
			selectedCareerInterests = [...selectedCareerInterests, id];
		} else {
			errorMessage = 'Chọn tối đa ba lĩnh vực. Hãy bỏ một lĩnh vực trước khi thêm lựa chọn khác.';
		}
		persistDraft('career', 0);
	}

	function startAssessment() {
		if (selectedCareerInterests.length === 0) {
			errorMessage = 'Hãy chọn ít nhất một lĩnh vực để chúng tôi xây dựng hồ sơ DESMAP của bạn.';
			return;
		}
		errorMessage = '';
		mode = 'questions';
		currentIndex = Math.min(currentIndex, totalQuestionCount - 1);
		persistDraft('questions', currentIndex);
	}

	function selectOption(questionId: string, letter: OptionLetter) {
		answers = { ...answers, [questionId]: letter };
		errorMessage = '';
		persistDraft();
	}

	async function focusQuestion() {
		await tick();
		(document.getElementById('question-title') as HTMLElement | null)?.focus();
	}

	async function nextQuestion() {
		if (!currentQuestion) return;
		if (!answers[currentQuestion.id]) {
			errorMessage = 'Hãy chọn một phương án để tiếp tục.';
			return;
		}
		errorMessage = '';
		if (currentIndex >= totalQuestionCount - 1) {
			mode = 'review';
			persistDraft('review', currentIndex);
			return;
		}
		currentIndex += 1;
		persistDraft('questions', currentIndex);
		await focusQuestion();
	}

	async function previousQuestion() {
		errorMessage = '';
		if (currentIndex === 0) {
			mode = 'career';
			persistDraft('career', 0);
			return;
		}
		currentIndex -= 1;
		mode = 'questions';
		persistDraft('questions', currentIndex);
		await focusQuestion();
	}

	async function jumpToStage(stageId: StageId) {
		const stage = getStageById(stageId);
		const first = orderedQuestions.findIndex((question) => question.stage === stage.id);
		if (first < 0 || !canVisitStage(stage.id)) return;
		mode = 'questions';
		currentIndex = first;
		errorMessage = '';
		persistDraft('questions', first);
		await focusQuestion();
	}

	function stageProgress(stage: (typeof questionnaireStages)[number]): {
		answered: number;
		total: number;
	} {
		const total = stage.questions.length;
		const answered = stage.questions.filter((question) => Boolean(answers[question.id])).length;
		return { answered, total };
	}

	function stageStartIndex(stageId: StageId): number {
		return orderedQuestions.findIndex((question) => question.stage === stageId);
	}

	/** A stage is reachable only after every earlier question has an explicit answer. */
	function canVisitStage(stageId: StageId): boolean {
		if (mode === 'career') return false;
		const first = stageStartIndex(stageId);
		return (
			first >= 0 &&
			orderedQuestions.slice(0, first).every((question) => Boolean(answers[question.id]))
		);
	}

	function submitAssessment() {
		if (unansweredQuestions.length > 0) {
			const firstUnanswered = orderedQuestions.findIndex((question) => !answers[question.id]);
			if (firstUnanswered >= 0) {
				mode = 'questions';
				currentIndex = firstUnanswered;
				persistDraft('questions', firstUnanswered);
			}
			errorMessage = `Hãy trả lời ${unansweredQuestions.length} câu hỏi còn lại trước khi gửi.`;
			return;
		}

		const payload = buildCompletionPayload({
			answers,
			careerInterests: selectedCareerInterests,
			startedAt
		});
		writeCompletionPayload(payload);
		clearSavedQuestionnaire();
		void goto('/evaluation');
	}
</script>

{#if !hydrated}
	<div class="questionnaire-shell loading-shell" aria-live="polite">Đang tải bài đánh giá…</div>
{:else}
	<div class="questionnaire-shell">
		<div class="assessment-layout" class:career-layout={mode === 'career'}>
			<aside class="stage-rail" aria-label="Các giai đoạn đánh giá DESMAP">
				<div class="rail-line" aria-hidden="true"></div>
				{#each questionnaireStages as stage (stage.id)}
					{@const stageStatus = stageProgress(stage)}
					{@const stageReachable = canVisitStage(stage.id)}
					{@const stageFirst = stageStartIndex(stage.id)}
					<button
						class="stage-marker"
						class:stage-current={currentStage?.id === stage.id && mode === 'questions'}
						class:stage-complete={stageStatus.answered === stageStatus.total &&
							stageStatus.total > 0}
						class:stage-started={stageStatus.answered > 0 &&
							stageStatus.answered < stageStatus.total}
						class:stage-future={!stageReachable && stageStatus.answered === 0}
						type="button"
						disabled={mode !== 'questions' || !stageReachable}
						aria-current={currentStage?.id === stage.id && mode === 'questions'
							? 'step'
							: undefined}
						aria-label={`${stage.label}, đã trả lời ${stageStatus.answered} trên ${stageStatus.total}${stageFirst > currentIndex ? ', chưa khả dụng' : ''}`}
						onclick={() => jumpToStage(stage.id)}
					>
						<span>{stage.id}</span>
					</button>
				{/each}
			</aside>

			<main class="assessment-main">
				<!-- <header class="assessment-header">
					<div>
						<p class="eyebrow">DESMAP / KHÁM PHÁ BẢN THÂN</p>
					</div>
				</header> -->

				{#if mode === 'career'}
					<section class="panel career-panel" aria-labelledby="career-title">
						<div class="panel-kicker">BƯỚC 01 / 04</div>
						<p class="section-label">SỞ THÍCH NGHỀ NGHIỆP</p>
						<h1 id="career-title">
							Bạn tò mò về<br /><span>loại công việc nào?</span>
						</h1>
						<p class="intro-copy">
							Chọn tối đa ba lĩnh vực rộng. Lựa chọn của bạn giúp bài đánh giá có thêm ngữ cảnh khi
							bạn khám phá các câu hỏi.
						</p>

						<div class="interest-grid" role="group" aria-label="Các lĩnh vực sở thích nghề nghiệp">
							{#each careerInterestOptions as interest (interest.id)}
								{@const selected = selectedCareerInterests.includes(interest.id)}
								<button
									class="interest-card"
									class:selected
									type="button"
									aria-pressed={selected}
									onclick={() => toggleCareerInterest(interest.id)}
								>
									<span class="interest-check" aria-hidden="true">{selected ? '✓' : '+'}</span>
									<strong>{interest.label}</strong>
									<span>{interest.description}</span>
								</button>
							{/each}
						</div>

						<div class="selection-row">
							<span class="selection-count" aria-live="polite"
								><span aria-hidden="true">☆</span>
								{selectedCareerInterests.length} <em>trên 3 đã chọn</em></span
							>
							<button class="primary-action" type="button" onclick={startAssessment}>
								Tiếp tục với DESMAP <span aria-hidden="true">→</span>
							</button>
						</div>
					</section>
				{:else if mode === 'questions' && currentQuestion}
					<section class="panel question-panel" aria-labelledby="question-title">
						<div class="question-copy">
							<h1 id="question-title" tabindex="-1">{currentQuestion.prompt}</h1>
							<p class="instruction" id="question-instruction">
								Chọn phương án mô tả đúng nhất về bạn.
							</p>
						</div>

						<fieldset
							class="options-fieldset"
							aria-describedby="question-instruction question-error"
						>
							<legend class="sr-only">
								Các phương án trả lời cho câu hỏi {currentQuestion.id}
							</legend>
							<div class="option-grid">
								{#each currentOptions as option, optionIndex (option.letter)}
									{@const optionId = `${currentQuestion.id}-${option.letter}`}
									<label
										class="option-card"
										class:option-selected={answers[currentQuestion.id] === option.letter}
										for={optionId}
									>
										<input
											id={optionId}
											type="radio"
											name={currentQuestion.id}
											value={option.letter}
											checked={answers[currentQuestion.id] === option.letter}
											onchange={() => selectOption(currentQuestion.id, option.letter)}
										/>
										<span class="option-letter" aria-hidden="true">{optionLabels[optionIndex]}</span
										>
										<span class="option-text">{option.text}</span>
										<span class="option-dot" aria-hidden="true"></span>
									</label>
								{/each}
							</div>
						</fieldset>

						{#if errorMessage}
							<p class="error-message" id="question-error" role="alert">
								{errorMessage}
							</p>
						{/if}
						<div class="question-footer">
							<button class="quiet-action" type="button" onclick={previousQuestion}>
								<span aria-hidden="true">←</span>
								Quay lại
							</button>
							<span class="question-count"
								>{currentIndex + 1} <span>/</span> {totalQuestionCount}</span
							>
							<button
								class="primary-action"
								type="button"
								onclick={nextQuestion}
								disabled={!answers[currentQuestion.id]}
							>
								{currentIndex === totalQuestionCount - 1 ? 'Xem lại câu trả lời' : 'Câu tiếp theo'}
								<span aria-hidden="true">→</span>
							</button>
						</div>
					</section>
				{:else}
					<section class="panel review-panel" aria-labelledby="review-title">
						<div class="panel-kicker">BƯỚC 03 / 04</div>
						<p class="section-label">XEM LẠI CÁC TÍN HIỆU</p>
						<h1 id="review-title">
							Bạn đã dành không gian<br />
							<span>cho một hướng đi rõ ràng hơn.</span>
						</h1>
						<p class="intro-copy">
							Bạn đã trả lời {answeredCount} trên {totalQuestionCount} câu hỏi. Hãy xem lại bất kỳ giai
							đoạn nào trước khi tạo hồ sơ.
						</p>

						<div class="review-list">
							{#each questionnaireStages as stage (stage.id)}
								{@const stageStatus = stageProgress(stage)}
								<div class="review-row">
									<div>
										<span class="stage-chip small">{stage.id}</span><strong>{stage.label}</strong>
									</div>
									<div class="review-row-meta">
										<span>{stageStatus.answered}/{stageStatus.total}</span><button
											type="button"
											class="text-action"
											onclick={() => jumpToStage(stage.id)}
										>
											Xem lại
										</button>
									</div>
								</div>
							{/each}
						</div>

						{#if errorMessage}
							<p class="error-message" role="alert">{errorMessage}</p>
						{/if}
						<div class="question-footer review-footer">
							<button class="quiet-action" type="button" onclick={previousQuestion}>
								<span aria-hidden="true">←</span>
								Quay lại
							</button>
							<span class="saved-note" aria-live="polite"
								>{unansweredQuestions.length === 0
									? 'Đã trả lời tất cả câu hỏi'
									: `Còn ${unansweredQuestions.length} câu cần trả lời`}</span
							>
							<button
								class="primary-action"
								type="button"
								onclick={submitAssessment}
								disabled={unansweredQuestions.length > 0}
							>
								Hoàn tất đánh giá <span aria-hidden="true">→</span>
							</button>
						</div>
					</section>
				{/if}

				{#if statusMessage}
					<p class="status-message" aria-live="polite">{statusMessage}</p>
				{/if}
			</main>
		</div>
	</div>
{/if}
