<script lang="ts">
	import { onMount, tick } from 'svelte';
	import { goto } from '$app/navigation';
	import {
		buildCompletionPayload,
		careerInterestOptions,
		clearSavedQuestionnaire,
		desmapQuestions,
		getStageById,
		questionnaireStages,
		readSavedQuestionnaire,
		totalQuestionCount,
		writeCompletionPayload,
		writeSavedQuestionnaire
	} from '$lib/questionnaire';
	import type { OptionLetter, QuestionnaireAnswers, QuestionnaireDraft, StageId } from '$lib/questionnaire';

	type Mode = 'career' | 'questions' | 'review';

	let hydrated = $state(false);
	let mode = $state<Mode>('career');
	let currentIndex = $state(0);
	let selectedCareerInterests = $state<string[]>([]);
	let answers = $state<QuestionnaireAnswers>({});
	let startedAt = $state('');
	let statusMessage = $state('');
	let errorMessage = $state('');

	let currentQuestion = $derived.by(() => desmapQuestions[currentIndex] ?? null);
	let currentStage = $derived.by(() => (currentQuestion ? getStageById(currentQuestion.stage) : null));
	let answeredCount = $derived(Object.keys(answers).length);
	let progress = $derived(mode === 'career' ? 0 : mode === 'review' ? 100 : Math.round(((currentIndex + 1) / totalQuestionCount) * 100));
	let unansweredQuestions = $derived(desmapQuestions.filter((question) => !answers[question.id]));

	onMount(() => {
		const saved = readSavedQuestionnaire();
		startedAt = saved?.startedAt ?? new Date().toISOString();
		if (saved) {
			selectedCareerInterests = [...saved.careerInterests];
			answers = { ...saved.answers };
			const firstUnanswered = desmapQuestions.findIndex((question) => !saved.answers[question.id]);
			const complete = firstUnanswered === -1;
			mode = saved.step === 'review' && !complete ? 'questions' : saved.step;
			currentIndex = mode === 'questions' && firstUnanswered >= 0
				? firstUnanswered
				: Math.min(Math.max(saved.currentIndex, 0), totalQuestionCount - 1);
			statusMessage = 'Your saved progress is ready.';
		}
		hydrated = true;
	});

	function persistDraft(nextMode: Mode = mode, nextIndex = currentIndex) {
		if (!hydrated || !startedAt) return;
		const draft: QuestionnaireDraft = {
			version: 1,
			completed: false,
			step: nextMode,
			currentIndex: nextIndex,
			startedAt,
			updatedAt: new Date().toISOString(),
			careerInterests: [...selectedCareerInterests],
			answers: { ...answers }
		};
		if (writeSavedQuestionnaire(draft)) statusMessage = 'Progress saved locally';
	}

	function toggleCareerInterest(id: string) {
		errorMessage = '';
		if (selectedCareerInterests.includes(id)) {
			selectedCareerInterests = selectedCareerInterests.filter((interest) => interest !== id);
		} else if (selectedCareerInterests.length < 3) {
			selectedCareerInterests = [...selectedCareerInterests, id];
		} else {
			errorMessage = 'Choose up to three fields. Remove one before adding another.';
		}
		persistDraft('career', 0);
	}

	function startAssessment() {
		if (selectedCareerInterests.length === 0) {
			errorMessage = 'Choose at least one field so we can shape your DESMAP profile.';
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
			errorMessage = 'Choose one option to continue.';
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
		const first = desmapQuestions.findIndex((question) => question.stage === stage.id);
		if (first < 0 || !canVisitStage(stage.id)) return;
		mode = 'questions';
		currentIndex = first;
		errorMessage = '';
		persistDraft('questions', first);
		await focusQuestion();
	}

	function stageProgress(stage: (typeof questionnaireStages)[number]): { answered: number; total: number } {
		const total = stage.questions.length;
		const answered = stage.questions.filter((question) => Boolean(answers[question.id])).length;
		return { answered, total };
	}

	function stageStartIndex(stageId: StageId): number {
		return desmapQuestions.findIndex((question) => question.stage === stageId);
	}

	/** A stage is reachable only after every earlier question has an explicit answer. */
	function canVisitStage(stageId: StageId): boolean {
		if (mode === 'career') return false;
		const first = stageStartIndex(stageId);
		return first >= 0 && desmapQuestions.slice(0, first).every((question) => Boolean(answers[question.id]));
	}

	function submitAssessment() {
		if (unansweredQuestions.length > 0) {
			const firstUnanswered = desmapQuestions.findIndex((question) => !answers[question.id]);
			if (firstUnanswered >= 0) {
				mode = 'questions';
				currentIndex = firstUnanswered;
				persistDraft('questions', firstUnanswered);
			}
			errorMessage = `Answer the remaining ${unansweredQuestions.length} question${unansweredQuestions.length === 1 ? '' : 's'} before submitting.`;
			return;
		}

		const payload = buildCompletionPayload({ answers, careerInterests: selectedCareerInterests, startedAt });
		writeCompletionPayload(payload);
		clearSavedQuestionnaire();
		void goto('/evaluation');
	}
</script>

{#if !hydrated}
	<div class="questionnaire-shell loading-shell" aria-live="polite">Loading your assessment…</div>
{:else}
	<div class="questionnaire-shell">
		<div class="assessment-layout">
			<aside class="stage-rail" aria-label="DESMAP assessment stages">
				<div class="rail-line" aria-hidden="true"></div>
				{#each questionnaireStages as stage (stage.id)}
					{@const stageStatus = stageProgress(stage)}
					{@const stageReachable = canVisitStage(stage.id)}
					{@const stageFirst = stageStartIndex(stage.id)}
					<button
						class="stage-marker"
						class:stage-current={currentStage?.id === stage.id && mode === 'questions'}
						class:stage-complete={stageStatus.answered === stageStatus.total && stageStatus.total > 0}
						class:stage-started={stageStatus.answered > 0 && stageStatus.answered < stageStatus.total}
						class:stage-future={!stageReachable && stageStatus.answered === 0}
						type="button"
						disabled={mode !== 'questions' || !stageReachable}
						aria-current={currentStage?.id === stage.id && mode === 'questions' ? 'step' : undefined}
						aria-label={`${stage.label}, ${stageStatus.answered} of ${stageStatus.total} answered${stageFirst > currentIndex ? ', not yet available' : ''}`}
						onclick={() => jumpToStage(stage.id)}
					>
						<span>{stage.id}</span>
					</button>
				{/each}
			</aside>

			<main class="assessment-main">
				<header class="assessment-header">
					<div>
						<p class="eyebrow">DESMAP / SELF DISCOVERY</p>
						<p class="header-caption">A considered pause before your next direction.</p>
					</div>
					<div class="progress-copy"><span>{progress}%</span><small>{mode === 'career' ? 'CAREER INTEREST' : mode === 'review' ? 'READY TO SUBMIT' : `${currentIndex + 1} / ${totalQuestionCount} QUESTIONS`}</small></div>
				</header>

				<div class="progress-track" aria-label={`${progress}% complete`} role="progressbar" aria-valuemin="0" aria-valuemax="100" aria-valuenow={progress}>
					<span style={`width: ${progress}%`}></span>
				</div>

				{#if mode === 'career'}
					<section class="panel career-panel" aria-labelledby="career-title">
						<div class="panel-kicker">STEP 01 / 04</div>
						<p class="section-label">CAREER INTEREST</p>
						<h1 id="career-title">What kind of work<br /><span>are you curious about?</span></h1>
						<p class="intro-copy">Choose up to three broad fields. Your selection gives the assessment a little context while you explore the questions.</p>

						<div class="interest-grid" role="group" aria-label="Career interest fields">
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
							<span class="selection-count" aria-live="polite"><span aria-hidden="true">☆</span> {selectedCareerInterests.length} <em>of 3 selected</em></span>
							<button class="primary-action" type="button" onclick={startAssessment}>Continue to DESMAP <span aria-hidden="true">→</span></button>
						</div>
					</section>
				{:else if mode === 'questions' && currentQuestion}
					<section class="panel question-panel" aria-labelledby="question-title">
						<div class="question-topline">
							<div class="stage-heading"><span class="stage-chip">{currentQuestion.stage}</span><span>{currentStage?.label}</span></div>
							<span class="dimension-tag">{currentQuestion.dimension}</span>
						</div>
						<h1 id="question-title" tabindex="-1">{currentQuestion.prompt}</h1>
						<p class="instruction" id="question-instruction">Choose the option that best describes you.</p>

						<fieldset class="options-fieldset" aria-describedby="question-instruction question-error">
							<legend class="sr-only">Answer options for question {currentQuestion.id}</legend>
							<div class="option-grid">
								{#each currentQuestion.options as option (option.letter)}
									{@const optionId = `${currentQuestion.id}-${option.letter}`}
									<label class="option-card" class:option-selected={answers[currentQuestion.id] === option.letter} for={optionId}>
										<input id={optionId} type="radio" name={currentQuestion.id} value={option.letter} checked={answers[currentQuestion.id] === option.letter} onchange={() => selectOption(currentQuestion.id, option.letter)} />
										<span class="option-letter" aria-hidden="true">{option.letter}</span>
										<span class="option-text">{option.text}</span>
										<span class="option-dot" aria-hidden="true"></span>
									</label>
								{/each}
							</div>
						</fieldset>

						{#if errorMessage}<p class="error-message" id="question-error" role="alert">{errorMessage}</p>{/if}
						<div class="question-footer">
							<button class="quiet-action" type="button" onclick={previousQuestion}><span aria-hidden="true">←</span> Back</button>
							<span class="question-count">{currentIndex + 1} <span>/</span> {totalQuestionCount}</span>
							<button class="primary-action" type="button" onclick={nextQuestion} disabled={!answers[currentQuestion.id]}>{currentIndex === totalQuestionCount - 1 ? 'Review answers' : 'Next question'} <span aria-hidden="true">→</span></button>
						</div>
					</section>
				{:else}
					<section class="panel review-panel" aria-labelledby="review-title">
						<div class="panel-kicker">STEP 03 / 04</div>
						<p class="section-label">REVIEW YOUR SIGNALS</p>
						<h1 id="review-title">You made space<br /><span>for a clearer direction.</span></h1>
						<p class="intro-copy">You answered {answeredCount} of {totalQuestionCount} questions. Review any stage before creating your frontend profile.</p>

						<div class="review-list">
							{#each questionnaireStages as stage (stage.id)}
								{@const stageStatus = stageProgress(stage)}
								<div class="review-row">
									<div><span class="stage-chip small">{stage.id}</span><strong>{stage.label}</strong></div>
									<div class="review-row-meta"><span>{stageStatus.answered}/{stageStatus.total}</span><button type="button" class="text-action" onclick={() => jumpToStage(stage.id)}>Review</button></div>
								</div>
							{/each}
						</div>

						{#if errorMessage}<p class="error-message" role="alert">{errorMessage}</p>{/if}
						<div class="question-footer review-footer">
							<button class="quiet-action" type="button" onclick={previousQuestion}><span aria-hidden="true">←</span> Back</button>
							<span class="saved-note" aria-live="polite">{unansweredQuestions.length === 0 ? 'All questions answered' : `${unansweredQuestions.length} left to answer`}</span>
							<button class="primary-action" type="button" onclick={submitAssessment} disabled={unansweredQuestions.length > 0}>Complete assessment <span aria-hidden="true">→</span></button>
						</div>
					</section>
				{/if}

				{#if statusMessage}<p class="status-message" aria-live="polite">{statusMessage}</p>{/if}
			</main>
		</div>
	</div>
{/if}

<style>
	:global(body) {
		margin: 0;
		background: var(--bg, #030303);
		color: var(--text, #f6f7fb);
	}

	:global(*) { box-sizing: border-box; }

	.questionnaire-shell {
		min-height: 100dvh;
		padding: clamp(1rem, 2.8vw, 3rem);
		background:
			radial-gradient(circle at 78% 16%, rgb(37 99 235 / 0.1), transparent 32rem),
			var(--bg, #030303);
		font-family: 'Avenir Next', 'Helvetica Neue', sans-serif;
	}

	.loading-shell { display: grid; place-items: center; color: var(--muted, #a7acb9); }

	.assessment-layout { width: min(92rem, 100%); margin: 0 auto; display: grid; grid-template-columns: 5.5rem minmax(0, 1fr); gap: clamp(1rem, 2.5vw, 3.5rem); }
	.assessment-main { min-width: 0; }
	.assessment-header { min-height: 4.4rem; display: flex; align-items: center; justify-content: space-between; gap: 1rem; border-bottom: 1px solid var(--line, #27303e); }
	.eyebrow, .section-label, .panel-kicker { margin: 0; color: var(--lime, #bcff63); letter-spacing: 0.18em; font-size: 0.7rem; font-weight: 700; }
	.header-caption { margin: 0.45rem 0 0; color: var(--muted, #a7acb9); font-size: 0.83rem; }
	.progress-copy { display: grid; justify-items: end; gap: 0.15rem; color: var(--lime, #bcff63); }
	.progress-copy span { font-size: clamp(1.55rem, 2.5vw, 2.3rem); font-weight: 500; line-height: 1; }
	.progress-copy small { color: var(--muted, #a7acb9); font-size: 0.65rem; letter-spacing: 0.1em; }
	.progress-track { height: 0.6rem; margin: 1.8rem 0 2rem; background: rgb(45 75 123 / 0.65); overflow: hidden; }
	.progress-track span { display: block; height: 100%; background: var(--lime, #bcff63); transition: width 360ms ease; }

	.stage-rail { position: relative; display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 1.15rem; padding: 6rem 0 2rem; }
	.rail-line { position: absolute; top: 7rem; bottom: 3rem; width: 1px; background: linear-gradient(var(--blue, #2563eb), rgb(37 99 235 / 0.15)); }
	.stage-marker { position: relative; z-index: 1; width: 3.05rem; height: 3.05rem; display: grid; place-items: center; border: 2px solid var(--blue, #2563eb); border-radius: 50%; color: var(--text, #f6f7fb); background: var(--bg, #030303); font: inherit; font-weight: 600; cursor: pointer; transition: background 180ms ease, border-color 180ms ease, color 180ms ease, transform 180ms ease; }
	.stage-marker:hover:not(:disabled), .stage-marker:focus-visible { border-color: var(--lime, #bcff63); color: var(--lime, #bcff63); transform: scale(1.06); outline: none; }
	.stage-marker:disabled { opacity: 0.5; cursor: default; }
	.stage-marker.stage-complete { background: var(--lime, #bcff63); border-color: var(--lime, #bcff63); color: #080b0f; }
	.stage-marker.stage-started { border-color: var(--blue, #2563eb); color: var(--blue, #4d8cff); background: rgb(37 99 235 / 0.08); }
	.stage-marker.stage-current { background: var(--bg, #030303); border-color: var(--blue, #2563eb); color: var(--blue, #4d8cff); box-shadow: 0 0 0 0.25rem rgb(37 99 235 / 0.2); }
	.stage-marker.stage-future { opacity: 0.32; border-color: rgb(37 99 235 / 0.5); color: var(--muted, #a7acb9); }

	.panel { min-height: min(69vh, 50rem); padding: clamp(1.4rem, 4vw, 4.3rem) clamp(1.2rem, 6vw, 7rem); border: 2px solid var(--blue, #2563eb); border-radius: 1.55rem; background: linear-gradient(145deg, rgb(11 19 34 / 0.98), rgb(5 10 20 / 0.98)); box-shadow: 0 1.5rem 5rem rgb(0 0 0 / 0.28); }
	.panel-kicker { text-align: right; color: var(--muted, #a7acb9); font-weight: 500; }
	.career-panel { display: flex; flex-direction: column; }
	.section-label { margin-top: 1.8rem; }
	h1 { margin: 1rem 0 0; color: var(--text, #f6f7fb); font-size: clamp(2.1rem, 5.2vw, 5rem); line-height: 0.98; letter-spacing: -0.055em; font-weight: 750; }
	h1 span { color: var(--lime, #bcff63); }
	.intro-copy { max-width: 37rem; margin: 1.2rem 0 2rem; color: var(--muted, #a7acb9); font-size: clamp(0.94rem, 1.2vw, 1.15rem); line-height: 1.55; }

	.interest-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 0.8rem; margin-top: auto; }
	.interest-card { position: relative; min-height: 8.4rem; display: flex; flex-direction: column; align-items: flex-start; gap: 0.5rem; padding: 1.2rem; border: 1px solid var(--blue, #2563eb); border-radius: 0.8rem; background: rgb(3 3 3 / 0.55); color: var(--text, #f6f7fb); text-align: left; font: inherit; cursor: pointer; transition: border-color 180ms ease, background 180ms ease, transform 180ms ease; }
	.interest-card:hover, .interest-card:focus-visible { border-color: var(--lime, #bcff63); transform: translateY(-2px); outline: none; }
	.interest-card.selected { border: 2px solid var(--lime, #bcff63); background: rgb(188 255 99 / 0.08); }
	.interest-card strong { max-width: 14rem; font-size: 0.95rem; line-height: 1.12; }
	.interest-card > span:last-child { color: var(--muted, #a7acb9); font-size: 0.75rem; line-height: 1.35; }
	.interest-check { align-self: flex-end; width: 1.55rem; height: 1.55rem; display: grid; place-items: center; border: 1px solid var(--blue, #2563eb); border-radius: 50%; color: var(--lime, #bcff63); font-size: 1rem; }
	.interest-card.selected .interest-check { border-color: var(--lime, #bcff63); background: var(--lime, #bcff63); color: #070a0b; }
	.selection-row, .question-footer { display: flex; align-items: center; justify-content: space-between; gap: 1rem; margin-top: 2.6rem; }
	.selection-count { display: flex; align-items: center; gap: 0.55rem; color: var(--lime, #bcff63); font-size: 1.45rem; }
	.selection-count span { font-size: 2rem; }
	.selection-count em { color: var(--muted, #a7acb9); font-size: 0.88rem; font-style: normal; }
	.primary-action { min-height: 3.35rem; padding: 0.8rem 1.55rem; border: 0; border-radius: 0.75rem; background: var(--lime, #bcff63); color: #090d11; font: inherit; font-size: 0.9rem; font-weight: 800; letter-spacing: 0.01em; cursor: pointer; transition: transform 180ms ease, filter 180ms ease; }
	.primary-action:hover:not(:disabled), .primary-action:focus-visible { filter: brightness(1.07); transform: translateY(-2px); outline: none; }
	.primary-action:disabled { cursor: not-allowed; filter: grayscale(0.65); opacity: 0.45; }

	.question-panel { display: flex; flex-direction: column; }
	.question-topline { display: flex; align-items: center; justify-content: space-between; gap: 1rem; color: var(--muted, #a7acb9); font-size: clamp(0.9rem, 1.5vw, 1.2rem); }
	.stage-heading { display: flex; align-items: center; gap: 0.75rem; }
	.stage-chip { min-width: 2.15rem; height: 2.15rem; display: inline-grid; place-items: center; border: 2px solid var(--blue, #2563eb); border-radius: 50%; color: var(--blue, #4d8cff); font-weight: 700; }
	.stage-chip.small { min-width: 1.7rem; width: 1.7rem; height: 1.7rem; border-width: 1px; font-size: 0.72rem; }
	.dimension-tag { color: var(--lime, #bcff63); font-size: 0.72rem; letter-spacing: 0.13em; }
	.question-panel h1 { max-width: 60rem; margin: clamp(3rem, 8vh, 6rem) auto 0; text-align: center; font-size: clamp(2rem, 4.3vw, 4.5rem); }
	.instruction { margin: 1.5rem 0 2.5rem; color: var(--muted, #a7acb9); text-align: center; font-size: 1rem; }
	.options-fieldset { border: 0; padding: 0; margin: 0; }
	.option-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 0.9rem; }
	.option-card { position: relative; min-height: 12rem; display: flex; flex-direction: column; align-items: flex-start; justify-content: space-between; gap: 1rem; padding: 1.25rem; border: 1px solid var(--blue, #2563eb); border-radius: 0.9rem; background: rgb(3 3 3 / 0.48); cursor: pointer; transition: border-color 180ms ease, background 180ms ease, transform 180ms ease; }
	.option-card:hover, .option-card:focus-within { border-color: var(--lime, #bcff63); transform: translateY(-2px); }
	.option-card.option-selected { border: 2px solid var(--lime, #bcff63); background: rgb(188 255 99 / 0.1); }
	.option-card input { position: absolute; width: 1px; height: 1px; opacity: 0; }
	.option-card input:focus-visible + .option-letter { outline: 2px solid var(--lime, #bcff63); outline-offset: 3px; }
	.option-letter { width: 2.4rem; height: 2.4rem; display: grid; place-items: center; border: 1px solid var(--blue, #2563eb); border-radius: 50%; color: var(--text, #f6f7fb); font-weight: 700; }
	.option-selected .option-letter { border-color: var(--lime, #bcff63); background: var(--lime, #bcff63); color: #070a0b; }
	.option-text { color: var(--text, #f6f7fb); font-size: 0.93rem; line-height: 1.45; }
	.option-dot { align-self: flex-end; width: 0.65rem; height: 0.65rem; border: 1px solid var(--blue, #2563eb); border-radius: 50%; }
	.option-selected .option-dot { border-color: var(--lime, #bcff63); background: var(--lime, #bcff63); }
	.quiet-action, .text-action { border: 0; background: none; color: var(--text, #f6f7fb); font: inherit; cursor: pointer; }
	.quiet-action { font-size: 1.05rem; }
	.quiet-action:hover, .quiet-action:focus-visible, .text-action:hover, .text-action:focus-visible { color: var(--lime, #bcff63); outline: none; }
	.quiet-action span { font-size: 1.8rem; vertical-align: -0.1em; }
	.question-count { color: var(--muted, #a7acb9); font-size: 1.1rem; }
	.question-count span { margin: 0 0.4rem; color: var(--blue, #4d8cff); }
	.error-message { margin: 1.2rem 0 0; color: #ff9f9f; font-size: 0.88rem; text-align: center; }
	.status-message { margin: 0.8rem; color: var(--muted, #a7acb9); text-align: center; font-size: 0.75rem; }

	.review-list { margin: 2.4rem 0 0; border-top: 1px solid var(--line, #27303e); }
	.review-row { display: flex; align-items: center; justify-content: space-between; gap: 1rem; padding: 1rem 0; border-bottom: 1px solid var(--line, #27303e); }
	.review-row > div:first-child { display: flex; align-items: center; gap: 0.7rem; }
	.review-row strong { font-size: 0.94rem; font-weight: 500; }
	.review-row-meta { display: flex; align-items: center; gap: 1.1rem; color: var(--muted, #a7acb9); font-size: 0.82rem; }
	.text-action { color: var(--lime, #bcff63); font-size: 0.8rem; }
	.saved-note { color: var(--muted, #a7acb9); font-size: 0.82rem; }

	.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0, 0, 0, 0); white-space: nowrap; border: 0; }

	@media (max-width: 850px) {
		.assessment-layout { grid-template-columns: 1fr; }
		.stage-rail { flex-direction: row; justify-content: flex-start; gap: 0.7rem; padding: 0.3rem 0 0; overflow-x: auto; }
		.rail-line { top: 50%; right: 0; bottom: auto; left: 0; width: auto; height: 1px; }
		.stage-marker { flex: 0 0 auto; width: 2.5rem; height: 2.5rem; }
		.interest-grid, .option-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
		.panel { min-height: auto; }
	}

	@media (max-width: 560px) {
		.questionnaire-shell { padding: 0.75rem; }
		.assessment-header { align-items: flex-start; padding-top: 0.65rem; }
		.header-caption { max-width: 12rem; }
		.progress-track { margin: 1.1rem 0; }
		.panel { padding: 1.15rem; border-radius: 1rem; }
		h1 { font-size: 2.5rem; }
		.interest-grid, .option-grid { grid-template-columns: 1fr; }
		.option-card { min-height: 8rem; }
		.selection-row, .question-footer { align-items: stretch; flex-wrap: wrap; }
		.selection-row .primary-action, .question-footer .primary-action { width: 100%; order: 3; }
		.selection-count, .question-count, .saved-note { order: 1; }
		.quiet-action { order: 2; }
		.question-panel h1 { margin-top: 3rem; font-size: 2.15rem; }
		.question-topline { align-items: flex-start; }
	}

	@media (prefers-reduced-motion: reduce) {
		*, *::before, *::after { scroll-behavior: auto !important; transition-duration: 0.01ms !important; animation-duration: 0.01ms !important; }
	}
</style>
