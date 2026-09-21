<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import type { PageProps } from './$types';
	import { ArrowRight, Download, RefreshCw } from '@lucide/svelte';
	import FinalBehaviourComparison from '$lib/components/FinalBehaviourComparison.svelte';
	import GroupedDesmapProfile from '$lib/components/GroupedDesmapProfile.svelte';
	import FinalCareerSuggestions from '$lib/components/FinalCareerSuggestions.svelte';
	import FinalDimensionDetails from '$lib/components/FinalDimensionDetails.svelte';
	import FinalDesmapRadar from '$lib/components/FinalDesmapRadar.svelte';
	import Header from '$lib/components/Header.svelte';
	import InitialMatchPanel from '$lib/components/InitialMatchPanel.svelte';
	import {
		evaluationPageState,
		experiences,
		placeholderBehaviourComparison,
		type FinalAssessment
	} from '$lib/evaluation';
	import {
		browserAssessmentStorage,
		InitialAssessmentError,
		runInitialAssessment,
		type AssessmentErrorKind,
		type InitialAssessmentResponse
	} from '$lib/assessment';
	import {
		readCompletionPayload,
		readQuestionnaireSyncStatus,
		uploadQuestionnaireSubmission,
		type QuestionnaireSubmission,
		type QuestionnaireSyncStatus
	} from '$lib/questionnaire';

	let { data }: PageProps = $props();

	let payload = $state<QuestionnaireSubmission | null>(null);
	let response = $state<InitialAssessmentResponse | null>(null);
	let rankedResults = $state<InitialAssessmentResponse['results']>([]);
	let viewState = $state<'loading' | 'success' | 'error' | 'empty'>('loading');
	let errorKind = $state<AssessmentErrorKind | null>(null);
	let errorMessage = $state('');
	let copied = $state(false);
	let notice = $state('');
	let requestInFlight = $state(false);
	let syncStatus = $state<QuestionnaireSyncStatus | null>(null);
	let syncInFlight = $state(false);
	type FinalSection = 'overview' | 'comparison' | 'dimensions' | 'careers';
	let expandedFinalSections = $state<Record<FinalSection, boolean>>({
		overview: false,
		comparison: true,
		dimensions: false,
		careers: true
	});
	let finalAssessment = $derived(
		data.finalAssessment ??
			(data.previewFinal && payload ? createPreviewFinalAssessment(payload) : null)
	);
	let pageState = $derived(evaluationPageState(payload, finalAssessment));
	const target = $derived(
		rankedResults[0]
			? experiences.find((experience) => experience.slug === rankedResults[0].career_id)
			: null
	);

	function createPreviewFinalAssessment(submission: QuestionnaireSubmission): FinalAssessment {
		return {
			version: 1,
			assessmentId: submission.assessmentId,
			completedAt: submission.completedAt,
			stageAssessments: {
				D: 'Dữ liệu phân tích cho thấy bạn quan tâm đến công việc có mục tiêu rõ ràng, tạo ra kết quả có thể nhìn thấy và cho phép bạn hiểu ý nghĩa của phần việc mình đảm nhận. Bạn có xu hướng gắn bó tốt hơn khi biết vì sao nhiệm vụ quan trọng và nó đóng góp thế nào vào kết quả chung.\n\nKhi phải chọn giữa nhiều hướng đi, bạn nên so sánh từng lựa chọn với các giá trị mình coi trọng, thay vì chỉ dựa vào cảm hứng nhất thời. Cách này sẽ giúp bạn nhận ra môi trường phù hợp và tránh theo đuổi một vai trò hấp dẫn bề ngoài nhưng không đáp ứng nhu cầu lâu dài.',
				E: 'Nhận định mẫu của AI về chuyên môn và các điểm mạnh của bạn sẽ được hiển thị tại đây.',
				S: 'Nhận định mẫu của AI về cách bạn phối hợp với người khác sẽ được hiển thị tại đây.',
				M: 'Nhận định mẫu của AI về cách bạn phân tích và ra quyết định sẽ được hiển thị tại đây.',
				A: 'Nhận định mẫu của AI về cách bạn thích ứng với thay đổi sẽ được hiển thị tại đây.',
				P: 'Nhận định mẫu của AI về phản ứng của bạn khi chịu áp lực sẽ được hiển thị tại đây.'
			},
			careerSuggestions: [
				{
					id: 'doctor',
					name: 'Bác sĩ',
					compatibilityPercent: 86,
					description:
						'Đây là hướng phù hợp nhất khi đối chiếu kết quả tự đánh giá với hành vi của bạn trong VR. Hồ sơ DESMAP cho thấy bạn có xu hướng phân tích thông tin trước khi hành động, còn trong trải nghiệm bạn đã kiểm tra các dữ kiện chính trước khi chọn thứ tự ưu tiên. Cách làm này hỗ trợ việc đánh giá tình huống và đưa ra quyết định có căn cứ trong công việc y khoa.\n\nBạn vẫn cần rèn khả năng chốt ưu tiên khi thời gian bị giới hạn. Hãy bắt đầu bằng các bài tập tình huống ngắn, xác định việc quan trọng nhất trước, sau đó xin phản hồi về quyết định của mình.'
				},
				{
					id: 'teacher',
					name: 'Giáo viên',
					compatibilityPercent: 79,
					description:
						'Khả năng giải thích vấn đề và điều chỉnh cách xử lý theo thông tin mới cũng phù hợp với công việc giảng dạy.'
				},
				{
					id: 'lawyer',
					name: 'Luật sư',
					compatibilityPercent: 73,
					description:
						'Tư duy phân tích và thói quen kiểm tra dữ kiện là nền tảng phù hợp để bạn tiếp tục khám phá ngành luật.'
				}
			]
		};
	}

	async function loadAssessment(retry = false) {
		if (requestInFlight || !payload) return;
		requestInFlight = true;
		viewState = 'loading';
		errorKind = null;
		errorMessage = '';
		try {
			const result = await runInitialAssessment(payload, {
				storage: browserAssessmentStorage(),
				skipCache: retry,
				mode: data.initialAssessmentMode
			});
			response = result.response;
			rankedResults = result.rankedResults;
			viewState = 'success';
		} catch (error) {
			viewState = 'error';
			errorKind = error instanceof InitialAssessmentError ? error.kind : 'invalid-response';
			errorMessage =
				error instanceof InitialAssessmentError
					? error.message
					: 'Đã xảy ra lỗi khi chuẩn bị đánh giá.';
		} finally {
			requestInFlight = false;
		}
	}

	async function syncSubmission() {
		if (!payload || syncInFlight) return;
		syncInFlight = true;
		syncStatus = { assessmentId: payload.assessmentId, status: 'pending' };
		syncStatus = await uploadQuestionnaireSubmission(payload);
		syncInFlight = false;
	}

	onMount(() => {
		payload = readCompletionPayload();
		if (!payload) viewState = 'empty';
		else {
			syncStatus = readQuestionnaireSyncStatus(payload.assessmentId);
			if (evaluationPageState(payload, finalAssessment) === 'final') viewState = 'success';
			else void loadAssessment();
			if (!syncStatus || syncStatus.status === 'pending') void syncSubmission();
		}
	});

	function showNotice(message: string) {
		notice = message;
		window.setTimeout(() => (notice = ''), 2400);
	}

	function toggleFinalSection(section: FinalSection) {
		expandedFinalSections[section] = !expandedFinalSections[section];
	}

	function saveJson() {
		if (!payload || (!response && !finalAssessment)) return;
		const isFinal = evaluationPageState(payload, finalAssessment) === 'final';
		const exportData = {
			format: isFinal ? 'Đánh giá cuối cùng DESMAP' : 'Đối chiếu nghề nghiệp ban đầu DESMAP',
			assessment_id: payload.assessmentId,
			questionnaire: payload,
			initial_assessment: response,
			final_assessment: isFinal ? finalAssessment : undefined
		};
		const url = URL.createObjectURL(
			new Blob([JSON.stringify(exportData, null, 2)], { type: 'application/json' })
		);
		const link = document.createElement('a');
		link.href = url;
		link.download = `desmap-${isFinal ? 'final' : 'initial'}-${payload.assessmentId}.json`;
		link.click();
		URL.revokeObjectURL(url);
		showNotice(isFinal ? 'Đã tải đánh giá cuối cùng.' : 'Đã tải kết quả đối chiếu ban đầu.');
	}

	async function copySummary() {
		if (!rankedResults.length) return;
		const text = `Kết quả đối chiếu nghề nghiệp ban đầu DESMAP\n${rankedResults
			.map((item) => `${item.career_name}: ${item.match_percentage}%`)
			.join('\n')}`;
		try {
			await navigator.clipboard.writeText(text);
			copied = true;
			window.setTimeout(() => (copied = false), 2400);
		} catch {
			showNotice('Không thể sao chép trong trình duyệt này.');
		}
	}
</script>

<svelte:head>
	<title
		>{pageState === 'final' ? 'Đánh giá cuối cùng' : 'Đối chiếu nghề nghiệp ban đầu'} | DESMAP</title
	>
	<meta
		name="description"
		content={pageState === 'final'
			? 'Đánh giá cuối cùng kết hợp hồ sơ DESMAP và bằng chứng quan sát.'
			: 'Đối chiếu ban đầu giữa điểm DESMAP và các nghề bạn chọn.'}
	/>
</svelte:head>

<Header showBack />
<main class="min-h-dvh py-[clamp(2.2rem,5vw,5rem)] pb-16 text-[#f7f9fb] evaluation-page-gradient">
	<div class="mx-auto w-[min(90rem,calc(100%_-_4rem))] max-[640px]:w-[calc(100%_-_1.4rem)]">
		<section class="border-b border-white/14 py-12">
			<p class="m-0 text-[.68rem] font-[760] tracking-[.16em] text-lime uppercase">
				{pageState === 'final' ? '03 / ĐÁNH GIÁ CUỐI CÙNG' : '02 / ĐỐI CHIẾU NGHỀ NGHIỆP BAN ĐẦU'}
			</p>
			<h1
				class="mt-3 mb-4 max-w-[820px] text-[clamp(2.5rem,7vw,5.8rem)] leading-[.92] font-[760] tracking-[-.065em]"
			>
				{pageState === 'final' ? 'Hồ sơ DESMAP cuối cùng' : 'Hồ sơ tự báo cáo'}
			</h1>
			<p class="m-0 max-w-[760px] text-[1.05rem] leading-[1.55] text-[#91a0b4]">
				{#if pageState === 'final'}
					Kết quả này kết hợp câu trả lời trong bảng câu hỏi với bằng chứng quan sát từ trải nghiệm
					VR.
				{:else}
					Các phần trăm là kết quả đối chiếu tạm thời từ câu trả lời của bạn. Đây không phải khuyến
					nghị nghề nghiệp hay kết luận cuối cùng.
				{/if}
			</p>
		</section>

		{#if syncStatus?.status === 'pending'}
			<p class="sr-only" aria-live="polite" role="status">Đang đồng bộ kết quả lên máy chủ.</p>
		{:else if syncStatus?.status === 'error'}
			<section
				class="mt-8 border border-[#f5ba66] bg-[#181106] p-8"
				aria-live="assertive"
				role="alert"
			>
				<h2 class="mt-0">Chưa thể đồng bộ kết quả</h2>
				<p class="text-[#d9c8ad]">
					{syncStatus.message ?? 'Không thể gửi kết quả lên máy chủ.'} Bảng câu hỏi của bạn vẫn được lưu
					trên thiết bị.
				</p>
				{#if syncStatus.recoverable}
					<button
						class="inline-flex cursor-pointer items-center gap-2 border border-lime bg-lime px-5 py-3 font-bold text-[#061006] focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-lime disabled:cursor-wait disabled:opacity-60"
						type="button"
						onclick={() => void syncSubmission()}
						disabled={syncInFlight}
					>
						<RefreshCw class="size-4" aria-hidden="true" /> Thử lại
					</button>
				{/if}
			</section>
		{/if}

		{#if viewState === 'empty'}
			<section class="mt-8 border border-blue bg-[#071020] p-8" aria-live="polite">
				<h2 class="mt-0">Chưa có bảng câu hỏi đã hoàn tất</h2>
				<p>Hãy hoàn tất bảng câu hỏi để tạo đối chiếu nghề nghiệp ban đầu.</p>
				<a
					class="inline-flex border border-lime bg-lime px-5 py-3 font-bold text-[#061006] no-underline focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-lime"
					href={resolve('/questionnaire')}>Đến bảng câu hỏi</a
				>
			</section>
		{:else if viewState === 'loading'}
			<section class="mt-8">
				<p class="sr-only" aria-live="polite" role="status">
					Đang chuẩn bị kết quả đối chiếu nghề nghiệp.
				</p>
				<div>
					<!-- <div class="flex justify-end">
						<span class="skeleton h-9 w-52 border border-blue/50"></span>
					</div> -->

					<div class="mt-3 grid gap-4 min-[850px]:grid-cols-[1.15fr_.85fr]">
						<div class="border border-blue bg-[#071020] p-6" aria-hidden="true">
							<span
								class="relative block h-7 w-52 max-w-full overflow-hidden bg-blue/18 after:absolute after:inset-0 after:translate-x-[-110%] after:animate-skeleton-scan after:bg-[linear-gradient(100deg,transparent_20%,rgb(188_255_99_/.18)_48%,transparent_76%)] after:content-[''] motion-reduce:after:translate-x-0 motion-reduce:after:animate-none motion-reduce:after:opacity-35"
							></span>
							<span
								class="relative mt-3 block h-3 w-[78%] overflow-hidden bg-blue/18 after:absolute after:inset-0 after:translate-x-[-110%] after:animate-skeleton-scan after:bg-[linear-gradient(100deg,transparent_20%,rgb(188_255_99_/.18)_48%,transparent_76%)] after:content-[''] motion-reduce:after:translate-x-0 motion-reduce:after:animate-none motion-reduce:after:opacity-35"
							></span>
							{#each { length: 5 }, index}
								<div
									class="grid grid-cols-[1.6rem_minmax(7rem,auto)_1fr_3rem] items-center gap-2 border-b border-white/14 py-4 max-[560px]:grid-cols-[1.5rem_1fr_3rem]"
								>
									<span
										class="relative block size-5 overflow-hidden rounded-full bg-blue/18 after:absolute after:inset-0 after:translate-x-[-110%] after:animate-skeleton-scan after:bg-[linear-gradient(100deg,transparent_20%,rgb(188_255_99_/.18)_48%,transparent_76%)] after:content-[''] motion-reduce:after:translate-x-0 motion-reduce:after:animate-none motion-reduce:after:opacity-35"
									></span>
									<span
										class="relative block h-4 w-28 max-w-full overflow-hidden bg-blue/18 after:absolute after:inset-0 after:translate-x-[-110%] after:animate-skeleton-scan after:bg-[linear-gradient(100deg,transparent_20%,rgb(188_255_99_/.18)_48%,transparent_76%)] after:content-[''] motion-reduce:after:translate-x-0 motion-reduce:after:animate-none motion-reduce:after:opacity-35"
									></span>
									<span class="h-2 bg-blue/20 max-[560px]:col-span-3">
										<span
											class="relative block h-full overflow-hidden bg-blue/18 after:absolute after:inset-0 after:translate-x-[-110%] after:animate-skeleton-scan after:bg-[linear-gradient(100deg,transparent_20%,rgb(188_255_99_/.18)_48%,transparent_76%)] after:content-[''] motion-reduce:after:translate-x-0 motion-reduce:after:animate-none motion-reduce:after:opacity-35"
											style:width={`${82 - index * 9}%`}
										></span>
									</span>
									<span
										class="relative block h-5 w-10 justify-self-end overflow-hidden bg-blue/18 after:absolute after:inset-0 after:translate-x-[-110%] after:animate-skeleton-scan after:bg-[linear-gradient(100deg,transparent_20%,rgb(188_255_99_/.18)_48%,transparent_76%)] after:content-[''] motion-reduce:after:translate-x-0 motion-reduce:after:animate-none motion-reduce:after:opacity-35"
									></span>
								</div>
							{/each}
						</div>

						{#if payload}
							<GroupedDesmapProfile scores={payload.scores} />
						{:else}
							<div class="border border-blue bg-[#071020] p-6" aria-hidden="true">
								<span
									class="relative block h-7 w-40 overflow-hidden bg-blue/18 after:absolute after:inset-0 after:translate-x-[-110%] after:animate-skeleton-scan after:bg-[linear-gradient(100deg,transparent_20%,rgb(188_255_99_/.18)_48%,transparent_76%)] after:content-[''] motion-reduce:after:translate-x-0 motion-reduce:after:animate-none motion-reduce:after:opacity-35"
								></span>
								<span
									class="relative mt-3 block h-3 w-[68%] overflow-hidden bg-blue/18 after:absolute after:inset-0 after:translate-x-[-110%] after:animate-skeleton-scan after:bg-[linear-gradient(100deg,transparent_20%,rgb(188_255_99_/.18)_48%,transparent_76%)] after:content-[''] motion-reduce:after:translate-x-0 motion-reduce:after:animate-none motion-reduce:after:opacity-35"
								></span>
								{#each { length: 6 }, index}
									<div class="flex items-center justify-between border-b border-white/14 py-3">
										<span
											class="relative block h-4 overflow-hidden bg-blue/18 after:absolute after:inset-0 after:translate-x-[-110%] after:animate-skeleton-scan after:bg-[linear-gradient(100deg,transparent_20%,rgb(188_255_99_/.18)_48%,transparent_76%)] after:content-[''] motion-reduce:after:translate-x-0 motion-reduce:after:animate-none motion-reduce:after:opacity-35"
											style:width={`${42 + (index % 3) * 8}%`}
										></span>
										<span
											class="relative block h-5 w-10 overflow-hidden bg-blue/18 after:absolute after:inset-0 after:translate-x-[-110%] after:animate-skeleton-scan after:bg-[linear-gradient(100deg,transparent_20%,rgb(188_255_99_/.18)_48%,transparent_76%)] after:content-[''] motion-reduce:after:translate-x-0 motion-reduce:after:animate-none motion-reduce:after:opacity-35"
										></span>
									</div>
								{/each}
							</div>
						{/if}
					</div>

					<div
						class="mt-4 flex flex-wrap items-center justify-between gap-5 border border-blue bg-[#071020] p-6"
						aria-hidden="true"
					>
						<div class="min-w-[min(100%,22rem)] flex-1">
							<span
								class="relative block h-7 w-40 overflow-hidden bg-blue/18 after:absolute after:inset-0 after:translate-x-[-110%] after:animate-skeleton-scan after:bg-[linear-gradient(100deg,transparent_20%,rgb(188_255_99_/.18)_48%,transparent_76%)] after:content-[''] motion-reduce:after:translate-x-0 motion-reduce:after:animate-none motion-reduce:after:opacity-35"
							></span>
							<span
								class="relative mt-3 block h-3 w-[min(100%,30rem)] overflow-hidden bg-blue/18 after:absolute after:inset-0 after:translate-x-[-110%] after:animate-skeleton-scan after:bg-[linear-gradient(100deg,transparent_20%,rgb(188_255_99_/.18)_48%,transparent_76%)] after:content-[''] motion-reduce:after:translate-x-0 motion-reduce:after:animate-none motion-reduce:after:opacity-35"
							></span>
						</div>
						<div class="flex items-center gap-4">
							<span
								class="relative block h-11 w-36 overflow-hidden bg-blue/18 after:absolute after:inset-0 after:translate-x-[-110%] after:animate-skeleton-scan after:bg-[linear-gradient(100deg,transparent_20%,rgb(188_255_99_/.18)_48%,transparent_76%)] after:content-[''] motion-reduce:after:translate-x-0 motion-reduce:after:animate-none motion-reduce:after:opacity-35"
							></span>
							<span
								class="relative block h-4 w-20 overflow-hidden bg-blue/18 after:absolute after:inset-0 after:translate-x-[-110%] after:animate-skeleton-scan after:bg-[linear-gradient(100deg,transparent_20%,rgb(188_255_99_/.18)_48%,transparent_76%)] after:content-[''] motion-reduce:after:translate-x-0 motion-reduce:after:animate-none motion-reduce:after:opacity-35"
							></span>
						</div>
					</div>
				</div>
			</section>
		{:else if viewState === 'error'}
			<section
				class="mt-8 border border-[#f5ba66] bg-[#181106] p-8"
				aria-live="assertive"
				role="alert"
			>
				<h2 class="mt-0">Chưa thể hoàn tất đối chiếu</h2>
				<p class="text-[#d9c8ad]">
					{errorMessage} Bảng câu hỏi của bạn vẫn được lưu trên thiết bị.
				</p>
				{#if errorKind === 'recoverable'}
					<button
						class="inline-flex cursor-pointer items-center gap-2 border border-lime bg-lime px-5 py-3 font-bold text-[#061006] focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-lime disabled:cursor-wait disabled:opacity-60"
						type="button"
						onclick={() => void loadAssessment(true)}
						disabled={requestInFlight}
					>
						<RefreshCw class="size-4" aria-hidden="true" /> Thử lại
					</button>
				{/if}
			</section>
			{#if payload}
				<div class="mt-4">
					<GroupedDesmapProfile scores={payload.scores} />
				</div>
			{/if}
		{:else if payload && (response || pageState === 'final')}
			<p class="sr-only" aria-live="polite" role="status">
				{pageState === 'final'
					? 'Đã có đánh giá cuối cùng.'
					: `Đã có kết quả đối chiếu cho ${rankedResults.length} nghề.`}
			</p>
			{#if pageState === 'final' && finalAssessment}
				<div class="mt-3" aria-label="Kết quả đánh giá cuối cùng">
					<FinalDesmapRadar
						scores={payload.scores}
						assessments={finalAssessment.stageAssessments}
						expanded={expandedFinalSections.overview}
						ontoggle={() => toggleFinalSection('overview')}
					/>
				</div>
				<FinalBehaviourComparison
					comparison={finalAssessment.behaviourComparison ?? placeholderBehaviourComparison}
					expanded={expandedFinalSections.comparison}
					ontoggle={() => toggleFinalSection('comparison')}
				/>
				<FinalDimensionDetails
					scores={payload.scores}
					levels={finalAssessment.dimensionLevels}
					expanded={expandedFinalSections.dimensions}
					ontoggle={() => toggleFinalSection('dimensions')}
				/>
				{#if finalAssessment.careerSuggestions?.length}
					<FinalCareerSuggestions
						suggestions={finalAssessment.careerSuggestions}
						expanded={expandedFinalSections.careers}
						ontoggle={() => toggleFinalSection('careers')}
					/>
				{/if}
			{:else}
				<section
					class="mt-3 grid gap-4 min-[850px]:grid-cols-[1.15fr_.85fr]"
					aria-label="Kết quả đối chiếu nghề nghiệp ban đầu"
				>
					<InitialMatchPanel mode={data.initialAssessmentMode} matches={rankedResults} />
					<GroupedDesmapProfile scores={payload.scores} />
				</section>
			{/if}
			<section
				class="mt-4 flex flex-wrap items-center justify-between gap-5 border border-blue bg-[#071020] p-6"
			>
				<div>
					{#if pageState === 'final'}
						<h2 class="mt-0 mb-1 text-lg font-bold">Đánh giá đã hoàn tất</h2>
						<p class="m-0 max-w-[34rem] text-[.83rem] text-[#91a0b4]">
							Bạn có thể xem từng khía cạnh DESMAP trên biểu đồ hoặc lưu toàn bộ kết quả.
						</p>
					{:else if data.initialAssessmentMode === 'ai'}
						<h2 class="mt-0 mb-1 text-lg font-bold">Bước tiếp theo</h2>
						<p class="m-0 max-w-[34rem] text-[.83rem] text-[#91a0b4]">
							Bạn có thể xem trải nghiệm của nghề đứng đầu hoặc lưu kết quả này.
						</p>
					{:else}
						<h2 class="mt-0 mb-1 text-lg font-bold">Cảm ơn bạn vì đã tham gia bài test này.</h2>
						<p class="m-0 max-w-[34rem] text-[.83rem] text-[#91a0b4]">
							Để nhận được kết quả chính xác nhất và gợi ý nghề nghiệp phù hợp, hãy tới buổi trải
							nghiệm bằng VR của tụi mình!
						</p>
					{/if}
				</div>
				<div class="flex flex-wrap items-center gap-4">
					{#if pageState === 'final'}
						<!-- The final assessment has no career-experience action. -->
					{:else if target?.status === 'locked'}
						<button
							class="border border-blue px-4 py-3 text-lime disabled:cursor-not-allowed disabled:opacity-70"
							type="button"
							disabled
						>
							{target.title}: Sắp ra mắt
						</button>
					{:else if target && data.initialAssessmentMode !== 'weighted'}
						<a
							class="inline-flex items-center gap-2 border border-lime bg-lime px-5 py-3 font-bold text-[#061006] no-underline focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-lime"
							href={resolve(`/experiences?career=${encodeURIComponent(target.slug)}`)}
						>
							Khám phá {target.title}<ArrowRight class="size-4" aria-hidden="true" />
						</a>
					{/if}
					<button
						class="cursor-pointer border-0 bg-transparent text-blue focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-lime"
						type="button"
						onclick={saveJson}
					>
						<Download class="inline size-4" aria-hidden="true" /> Tải JSON
					</button>
					{#if pageState === 'initial'}
						<button
							class="cursor-pointer border-0 bg-transparent text-blue focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-lime"
							type="button"
							onclick={copySummary}
						>
							{copied ? 'Đã sao chép' : 'Sao chép tóm tắt'}
						</button>
					{/if}
				</div>
			</section>
		{/if}

		{#if notice}
			<p
				class="fixed right-4 bottom-4 border border-lime bg-[#081306] px-4 py-3 text-lime"
				role="status"
			>
				{notice}
			</p>
		{/if}
	</div>
</main>
