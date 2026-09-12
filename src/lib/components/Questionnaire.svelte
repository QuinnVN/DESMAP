<script lang="ts">
	import { onMount, tick } from 'svelte';
	import { goto } from '$app/navigation';
	import { ArrowLeft, ArrowRight, Check, Plus, Star } from '@lucide/svelte';
	import { resolve } from '$app/paths';
	import { prefersReducedMotion } from 'svelte/motion';
	import { fly } from 'svelte/transition';
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
	let transitionsReady = $state(false);
	let animateCareerHandoff = $state(false);

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
	let hasAnsweredQuestions = $derived(answeredCount > 0);
	let unansweredQuestions = $derived(orderedQuestions.filter((question) => !answers[question.id]));
	const optionLabels: OptionLetter[] = ['A', 'B', 'C'];
	const careerHandoffDuration = 250;

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
			mode =
				saved.step === 'career' && Object.keys(saved.answers).length > 0
					? 'questions'
					: saved.step === 'review' && !complete
						? 'questions'
						: saved.step;
			currentIndex =
				mode === 'questions' && firstUnanswered >= 0
					? firstUnanswered
					: Math.min(Math.max(saved.currentIndex, 0), totalQuestionCount - 1);
			statusMessage = 'Tiến trình đã lưu của bạn đã sẵn sàng.';
		}
		hydrated = true;
		void enableTransitions();
	});

	async function enableTransitions() {
		await tick();
		transitionsReady = true;
	}

	function handoffDuration(): number {
		return transitionsReady && animateCareerHandoff && !prefersReducedMotion.current
			? careerHandoffDuration
			: 0;
	}

	function finishCareerHandoff() {
		animateCareerHandoff = false;
	}

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
		animateCareerHandoff = true;
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
			if (hasAnsweredQuestions) return;
			animateCareerHandoff = true;
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
		void goto(resolve('/evaluation'));
	}
</script>

{#if !hydrated}
	<div
		class="grid min-h-dvh place-items-center bg-[radial-gradient(circle_at_78%_16%,rgb(37_99_235_/.1),transparent_32rem),#030303] p-[clamp(.9rem,1.8vw,2rem)] font-sans text-muted"
		aria-live="polite"
	>
		Đang tải bài đánh giá…
	</div>
{:else}
	<div
		class="min-h-dvh bg-[radial-gradient(circle_at_78%_16%,rgb(37_99_235_/.1),transparent_32rem),#030303] p-[clamp(.9rem,1.8vw,2rem)] font-sans text-[#f6f7fb] max-[560px]:p-3"
	>
		<div
			class={`mx-auto grid min-h-[calc(100dvh_-_clamp(1.8rem,3.6vw,4rem))] w-full max-w-[108rem] transition-[grid-template-columns,gap] duration-250 ease-out max-[850px]:grid-cols-1 ${mode === 'career' ? 'grid-cols-[0_minmax(0,1fr)] gap-0 max-[850px]:grid-cols-1' : 'grid-cols-[4.5rem_minmax(0,1fr)] gap-[clamp(.75rem,1.6vw,1.75rem)]'}`}
		>
			{#if mode !== 'career'}
				<aside
					class="relative flex flex-col items-center justify-center gap-[1.15rem] pt-[4.5rem] pb-4 max-[850px]:flex-row max-[850px]:justify-start max-[850px]:gap-[.7rem] max-[850px]:overflow-x-auto max-[850px]:pt-[.3rem]"
					aria-label="Các giai đoạn đánh giá DESMAP"
					in:fly={{ x: -20, duration: handoffDuration() }}
					out:fly={{ x: -20, duration: handoffDuration() }}
				>
					<div
						class="absolute top-[5.5rem] bottom-8 w-px bg-[linear-gradient(#2563eb,rgb(37_99_235_/.15))] max-[850px]:top-1/2 max-[850px]:right-0 max-[850px]:bottom-auto max-[850px]:left-0 max-[850px]:h-px max-[850px]:w-auto"
						aria-hidden="true"
					></div>
					{#each questionnaireStages as stage (stage.id)}
						{@const stageStatus = stageProgress(stage)}
						{@const stageReachable = canVisitStage(stage.id)}
						{@const stageFirst = stageStartIndex(stage.id)}
						<button
							class={`relative z-1 grid h-[3.05rem] w-[3.05rem] place-items-center rounded-full border-2 bg-[#030303] font-semibold transition-[background,border-color,color,transform] duration-180 hover:scale-[1.06] hover:border-lime hover:text-lime focus-visible:scale-[1.06] focus-visible:border-lime focus-visible:text-lime focus-visible:outline-none disabled:cursor-default disabled:opacity-50 max-[850px]:h-10 max-[850px]:w-10 max-[850px]:shrink-0 ${currentStage?.id === stage.id && mode === 'questions' ? 'border-blue text-[#4d8cff] shadow-[0_0_0_.25rem_rgb(37_99_235_/.2)]' : stageStatus.answered === stageStatus.total && stageStatus.total > 0 ? 'border-lime bg-lime text-[#080b0f]' : stageStatus.answered > 0 ? 'border-blue bg-blue/8 text-[#4d8cff]' : !stageReachable ? 'border-blue/50 text-muted opacity-32' : 'border-blue text-[#f6f7fb]'}`}
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
			{/if}

			<main class="col-start-2 grid min-w-0 grid-rows-[minmax(0,1fr)_auto] max-[850px]:col-start-1">
				{#if mode === 'career'}
					<section
						class="col-start-1 row-start-1 flex min-h-[min(69vh,50rem)] flex-col rounded-[1.55rem] border-2 border-blue bg-[linear-gradient(145deg,rgb(11_19_34_/.98),rgb(5_10_20_/.98))] p-[clamp(1.4rem,4vw,4.3rem)] shadow-[0_1.5rem_5rem_rgb(0_0_0_/.28)] max-[850px]:min-h-0 max-[560px]:rounded-2xl max-[560px]:p-[1.15rem] min-[1100px]:grid min-[1100px]:min-h-0 min-[1100px]:grid-cols-[minmax(0,1.08fr)_minmax(22rem,.92fr)] min-[1100px]:grid-rows-[auto_auto_minmax(0,1fr)_auto] min-[1100px]:gap-x-[clamp(2rem,4vw,5rem)] min-[1100px]:gap-y-[clamp(.75rem,1.5vh,1.15rem)] min-[1100px]:[grid-template-areas:'label_kicker'_'title_intro'_'interests_interests'_'selection_selection']"
						aria-labelledby="career-title"
						in:fly={{ x: -32, duration: handoffDuration() }}
						out:fly={{ x: -32, duration: handoffDuration() }}
						onintroend={finishCareerHandoff}
					>
						<div
							class="text-right text-[.7rem] font-medium tracking-[.18em] text-muted min-[1100px]:[grid-area:kicker]"
						>
							BƯỚC 01 / 04
						</div>
						<p
							class="m-0 mt-[1.8rem] text-[.7rem] font-bold tracking-[.18em] text-lime min-[1100px]:m-0 min-[1100px]:[grid-area:label]"
						>
							SỞ THÍCH NGHỀ NGHIỆP
						</p>
						<h1
							class="m-0 mt-4 text-[clamp(2.1rem,5.2vw,5rem)] leading-[.98] font-[750] tracking-[-.055em] min-[1100px]:mt-0 min-[1100px]:text-[clamp(2.75rem,3.6vw,4.4rem)] min-[1100px]:leading-[.94] min-[1100px]:[grid-area:title]"
							id="career-title"
						>
							Bạn tò mò về<br /><span class="text-lime">loại công việc nào?</span>
						</h1>
						<p
							class="mt-[1.2rem] mb-8 max-w-[37rem] text-right text-[clamp(.94rem,1.2vw,1.15rem)] leading-[1.55] min-[1100px]:m-[0_0_.25rem] min-[1100px]:max-w-[32rem] min-[1100px]:justify-self-end min-[1100px]:self-end min-[1100px]:[grid-area:intro]"
						>
							Chọn tối đa ba lĩnh vực rộng. Lựa chọn của bạn giúp bài đánh giá có thêm ngữ cảnh khi
							bạn khám phá các câu hỏi.
						</p>

						<div
							class="mt-auto grid grid-cols-3 gap-[.8rem] max-[850px]:grid-cols-2 max-[560px]:grid-cols-1 min-[1100px]:m-0 min-[1100px]:grid-cols-4 min-[1100px]:gap-[.65rem] min-[1100px]:self-center min-[1100px]:[grid-area:interests]"
							role="group"
							aria-label="Các lĩnh vực sở thích nghề nghiệp"
						>
							{#each careerInterestOptions as interest (interest.id)}
								{@const selected = selectedCareerInterests.includes(interest.id)}
								<button
									class={`relative flex min-h-[10rem] cursor-pointer flex-col items-start gap-3 rounded-[.8rem] border bg-[#030303]/55 p-[1.4rem] text-left text-[#f6f7fb] transition-[border-color,background,transform] duration-180 hover:-translate-y-0.5 hover:border-lime focus-visible:-translate-y-0.5 focus-visible:border-lime focus-visible:outline-none min-[1100px]:min-h-[7.75rem] min-[1100px]:justify-center min-[1100px]:gap-[.5rem] min-[1100px]:p-[1.15rem_3.2rem_1.15rem_1.25rem] ${selected ? 'border-2 border-lime bg-lime/8' : 'border-blue'}`}
									type="button"
									aria-pressed={selected}
									onclick={() => toggleCareerInterest(interest.id)}
								>
									<span
										class={`grid h-[1.55rem] w-[1.55rem] place-items-center self-end rounded-full border text-base min-[1100px]:absolute min-[1100px]:top-[.85rem] min-[1100px]:right-[.85rem] ${selected ? 'border-lime bg-lime text-[#070a0b]' : 'border-blue text-lime'}`}
										aria-hidden="true"
										>{#if selected}<Check class="size-4" />{:else}<Plus class="size-4" />{/if}</span
									>
									<strong
										class="max-w-56 text-[1.05rem] leading-[1.12] min-[1100px]:max-w-none min-[1100px]:text-[.98rem]"
										>{interest.label}</strong
									>
									<span
										class="text-[.82rem] leading-[1.35] text-muted min-[1100px]:text-[.78rem] min-[1100px]:leading-[1.3]"
										>{interest.description}</span
									>
								</button>
							{/each}
						</div>

						<div
							class="mt-[2.6rem] flex items-center justify-between gap-4 max-[560px]:flex-wrap max-[560px]:items-stretch min-[1100px]:m-0 min-[1100px]:border-t min-[1100px]:border-line min-[1100px]:pt-4 min-[1100px]:[grid-area:selection]"
						>
							<span
								class="flex items-center gap-[.55rem] text-[1.45rem] text-lime max-[560px]:order-1"
								aria-live="polite"
								><span class="text-[2rem]" aria-hidden="true"><Star class="size-[1em]" /></span>
								{selectedCareerInterests.length}
								<em class="text-[.88rem] text-muted not-italic">trên 3 đã chọn</em></span
							>
							<button
								class="inline-flex min-h-[3.35rem] cursor-pointer items-center justify-center gap-3 rounded-xl border-0 bg-lime px-[1.55rem] py-[.8rem] text-[.9rem] font-extrabold tracking-[.01em] text-[#090d11] transition-[transform,filter] duration-180 hover:-translate-y-0.5 hover:brightness-[1.07] focus-visible:-translate-y-0.5 focus-visible:brightness-[1.07] focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-45 disabled:grayscale-[.65] max-[560px]:order-3 max-[560px]:w-full"
								type="button"
								onclick={startAssessment}
							>
								Tiếp tục với DESMAP <span aria-hidden="true"><ArrowRight class="size-[1em]" /></span
								>
							</button>
						</div>
					</section>
				{:else if mode === 'questions' && currentQuestion}
					<section
						class="col-start-1 row-start-1 flex min-h-[min(69vh,50rem)] flex-col rounded-[1.55rem] border-2 border-blue bg-[linear-gradient(145deg,rgb(11_19_34_/.98),rgb(5_10_20_/.98))] p-[clamp(1.4rem,4vw,4.3rem)] shadow-[0_1.5rem_5rem_rgb(0_0_0_/.28)] max-[850px]:min-h-0 max-[560px]:rounded-2xl max-[560px]:p-[1.15rem] min-[1100px]:grid min-[1100px]:min-h-0 min-[1100px]:grid-cols-[minmax(0,.92fr)_minmax(0,1.08fr)] min-[1100px]:grid-rows-[minmax(0,1fr)_auto_auto] min-[1100px]:gap-x-[clamp(2.5rem,4vw,5.5rem)] min-[1100px]:gap-y-4 min-[1100px]:[grid-template-areas:'copy_answers'_'error_error'_'footer_footer']"
						aria-labelledby="question-title"
						in:fly={{ x: 32, duration: handoffDuration() }}
						out:fly={{ x: 32, duration: handoffDuration() }}
						onintroend={finishCareerHandoff}
					>
						<div class="min-[1100px]:self-center min-[1100px]:[grid-area:copy]">
							<h1
								class="mx-auto mt-[clamp(3rem,8vh,6rem)] max-w-[60rem] text-center text-[clamp(2rem,4.3vw,4.5rem)] leading-[.98] font-[750] tracking-[-.055em] min-[1100px]:m-0 min-[1100px]:max-w-[43rem] min-[1100px]:text-left min-[1100px]:text-[clamp(2.45rem,3.25vw,4rem)]"
								id="question-title"
								tabindex="-1"
							>
								{currentQuestion.prompt}
							</h1>
							<p
								class="mt-6 mb-10 text-center text-base text-muted min-[1100px]:mt-6 min-[1100px]:mb-0 min-[1100px]:text-left"
								id="question-instruction"
							>
								Chọn phương án mô tả đúng nhất về bạn.
							</p>
						</div>

						<fieldset
							class="m-0 border-0 p-0 min-[1100px]:self-center min-[1100px]:[grid-area:answers]"
							aria-describedby="question-instruction question-error"
						>
							<legend class="sr-only">
								Các phương án trả lời cho câu hỏi {currentQuestion.id}
							</legend>
							<div
								class="grid grid-cols-3 gap-[.9rem] max-[850px]:grid-cols-2 max-[560px]:grid-cols-1 min-[1100px]:grid-cols-1 min-[1100px]:gap-3"
							>
								{#each currentOptions as option, optionIndex (option.letter)}
									{@const optionId = `${currentQuestion.id}-${option.letter}`}
									<label
									class={`relative flex min-h-[14rem] cursor-pointer flex-col items-start justify-between gap-5 rounded-[.9rem] border bg-[#030303]/48 p-6 transition-[border-color,background,transform] duration-180 focus-within:-translate-y-0.5 focus-within:border-lime hover:-translate-y-0.5 hover:border-lime max-[560px]:min-h-40 min-[1100px]:grid min-[1100px]:min-h-[8.75rem] min-[1100px]:grid-cols-[auto_minmax(0,1fr)_auto] min-[1100px]:items-center min-[1100px]:gap-6 min-[1100px]:p-[1.45rem_1.65rem] ${answers[currentQuestion.id] === option.letter ? 'border-2 border-lime bg-lime/10' : 'border-blue'}`}
										for={optionId}
									>
										<input
											class="absolute h-px w-px opacity-0"
											id={optionId}
											type="radio"
											name={currentQuestion.id}
											value={option.letter}
											checked={answers[currentQuestion.id] === option.letter}
											onchange={() => selectOption(currentQuestion.id, option.letter)}
										/>
										<span
										class={`grid h-[2.8rem] w-[2.8rem] place-items-center rounded-full border text-[1.05rem] font-bold ${answers[currentQuestion.id] === option.letter ? 'border-lime bg-lime text-[#070a0b]' : 'border-blue text-[#f6f7fb]'}`}
											aria-hidden="true">{optionLabels[optionIndex]}</span
										>
										<span
										class="text-[1.08rem] leading-[1.5] text-[#f6f7fb] min-[1100px]:text-[clamp(1.05rem,1.15vw,1.2rem)]"
											>{option.text}</span
										>
										<span
											class={`h-[.65rem] w-[.65rem] self-end rounded-full border min-[1100px]:self-center ${answers[currentQuestion.id] === option.letter ? 'border-lime bg-lime' : 'border-blue'}`}
											aria-hidden="true"
										></span>
									</label>
								{/each}
							</div>
						</fieldset>

						{#if errorMessage}
							<p
								class="mt-5 text-center text-[.88rem] text-[#ff9f9f] min-[1100px]:m-0 min-[1100px]:[grid-area:error]"
								id="question-error"
								role="alert"
							>
								{errorMessage}
							</p>
						{/if}
						<div
							class="mt-[2.6rem] flex items-center justify-between gap-4 max-[560px]:flex-wrap max-[560px]:items-stretch min-[1100px]:m-0 min-[1100px]:border-t min-[1100px]:border-line min-[1100px]:pt-[1.15rem] min-[1100px]:[grid-area:footer]"
						>
							<button
								class="cursor-pointer border-0 bg-transparent text-[1.05rem] text-[#f6f7fb] hover:text-lime focus-visible:text-lime focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-45 max-[560px]:order-2"
								type="button"
								onclick={previousQuestion}
								disabled={currentIndex === 0 && hasAnsweredQuestions}
							>
								<span class="align-[-.1em] text-[1.8rem]" aria-hidden="true"
									><ArrowLeft class="size-[1em]" /></span
								>
								Quay lại
							</button>
							<span class="text-[1.1rem] text-muted max-[560px]:order-1"
								>{currentIndex + 1} <span class="mx-[.4rem] text-[#4d8cff]">/</span>
								{totalQuestionCount}</span
							>
							<button
								class="inline-flex min-h-[3.35rem] cursor-pointer items-center justify-center gap-3 rounded-xl border-0 bg-lime px-[1.55rem] py-[.8rem] text-[.9rem] font-extrabold tracking-[.01em] text-[#090d11] transition-[transform,filter] duration-180 hover:-translate-y-0.5 hover:brightness-[1.07] focus-visible:-translate-y-0.5 focus-visible:brightness-[1.07] focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-45 disabled:grayscale-[.65] max-[560px]:order-3 max-[560px]:w-full"
								type="button"
								onclick={nextQuestion}
								disabled={!answers[currentQuestion.id]}
							>
								{currentIndex === totalQuestionCount - 1 ? 'Xem lại câu trả lời' : 'Câu tiếp theo'}
								<span aria-hidden="true"><ArrowRight class="size-[1em]" /></span>
							</button>
						</div>
					</section>
				{:else}
					<section
						class="col-start-1 row-start-1 min-h-[min(69vh,50rem)] rounded-[1.55rem] border-2 border-blue bg-[linear-gradient(145deg,rgb(11_19_34_/.98),rgb(5_10_20_/.98))] p-[clamp(1.4rem,4vw,4.3rem)] shadow-[0_1.5rem_5rem_rgb(0_0_0_/.28)] max-[850px]:min-h-0 max-[560px]:rounded-2xl max-[560px]:p-[1.15rem]"
						aria-labelledby="review-title"
					>
						<div class="text-right text-[.7rem] font-medium tracking-[.18em] text-muted">
							BƯỚC 03 / 04
						</div>
						<p class="m-0 mt-[1.8rem] text-[.7rem] font-bold tracking-[.18em] text-lime">
							XEM LẠI CÁC TÍN HIỆU
						</p>
						<h1
							class="m-0 mt-4 text-[clamp(2.1rem,5.2vw,5rem)] leading-[.98] font-[750] tracking-[-.055em]"
							id="review-title"
						>
							Bạn đã dành không gian<br />
							<span class="text-lime">cho một hướng đi rõ ràng hơn.</span>
						</h1>
						<p
							class="mt-[1.2rem] mb-8 max-w-[37rem] text-[clamp(.94rem,1.2vw,1.15rem)] leading-[1.55] text-muted"
						>
							Bạn đã trả lời {answeredCount} trên {totalQuestionCount} câu hỏi. Hãy xem lại bất kỳ giai
							đoạn nào trước khi tạo hồ sơ.
						</p>

						<div class="mt-10 border-t border-line">
							{#each questionnaireStages as stage (stage.id)}
								{@const stageStatus = stageProgress(stage)}
								<div class="flex items-center justify-between gap-4 border-b border-line py-4">
									<div class="flex items-center gap-[.7rem]">
										<span
											class="inline-grid h-[1.7rem] w-[1.7rem] place-items-center rounded-full border border-blue text-[.72rem] font-bold text-[#4d8cff]"
											>{stage.id}</span
										><strong class="text-[.94rem] font-medium">{stage.label}</strong>
									</div>
									<div class="flex items-center gap-[1.1rem] text-[.82rem] text-muted">
										<span>{stageStatus.answered}/{stageStatus.total}</span><button
											type="button"
											class="cursor-pointer border-0 bg-transparent text-[.8rem] text-lime hover:text-lime focus-visible:text-lime focus-visible:outline-none"
											onclick={() => jumpToStage(stage.id)}
										>
											Xem lại
										</button>
									</div>
								</div>
							{/each}
						</div>

						{#if errorMessage}
							<p class="mt-5 text-center text-[.88rem] text-[#ff9f9f]" role="alert">
								{errorMessage}
							</p>
						{/if}
						<div
							class="mt-[2.6rem] flex items-center justify-between gap-4 max-[560px]:flex-wrap max-[560px]:items-stretch"
						>
							<button
								class="cursor-pointer border-0 bg-transparent text-[1.05rem] text-[#f6f7fb] hover:text-lime focus-visible:text-lime focus-visible:outline-none max-[560px]:order-2"
								type="button"
								onclick={previousQuestion}
							>
								<span class="align-[-.1em] text-[1.8rem]" aria-hidden="true"
									><ArrowLeft class="size-[1em]" /></span
								>
								Quay lại
							</button>
							<span class="text-[.82rem] text-muted max-[560px]:order-1" aria-live="polite"
								>{unansweredQuestions.length === 0
									? 'Đã trả lời tất cả câu hỏi'
									: `Còn ${unansweredQuestions.length} câu cần trả lời`}</span
							>
							<button
								class="inline-flex min-h-[3.35rem] cursor-pointer items-center justify-center gap-3 rounded-xl border-0 bg-lime px-[1.55rem] py-[.8rem] text-[.9rem] font-extrabold tracking-[.01em] text-[#090d11] transition-[transform,filter] duration-180 hover:-translate-y-0.5 hover:brightness-[1.07] focus-visible:-translate-y-0.5 focus-visible:brightness-[1.07] focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-45 disabled:grayscale-[.65] max-[560px]:order-3 max-[560px]:w-full"
								type="button"
								onclick={submitAssessment}
								disabled={unansweredQuestions.length > 0}
							>
								Hoàn tất đánh giá <span aria-hidden="true"><ArrowRight class="size-[1em]" /></span>
							</button>
						</div>
					</section>
				{/if}
			</main>
		</div>
	</div>
{/if}
