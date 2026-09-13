<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import type { PageProps } from './$types';
	import { ArrowRight, Download, RefreshCw } from '@lucide/svelte';
	import GroupedDesmapProfile from '$lib/components/GroupedDesmapProfile.svelte';
	import Header from '$lib/components/Header.svelte';
	import InitialMatchPanel from '$lib/components/InitialMatchPanel.svelte';
	import { experiences } from '$lib/evaluation';
	import {
		browserAssessmentStorage,
		InitialAssessmentError,
		runInitialAssessment,
		type AssessmentErrorKind,
		type InitialAssessmentResponse
	} from '$lib/assessment';
	import { readCompletionPayload, type QuestionnaireSubmission } from '$lib/questionnaire';

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
	const target = $derived(
		rankedResults[0]
			? experiences.find((experience) => experience.slug === rankedResults[0].career_id)
			: null
	);

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

	onMount(() => {
		payload = readCompletionPayload();
		if (!payload) viewState = 'empty';
		else void loadAssessment();
	});

	function showNotice(message: string) {
		notice = message;
		window.setTimeout(() => (notice = ''), 2400);
	}

	function saveJson() {
		if (!payload || !response) return;
		const data = {
			format: 'Đối chiếu nghề nghiệp ban đầu DESMAP',
			assessment_id: payload.assessmentId,
			questionnaire: payload,
			initial_assessment: response
		};
		const url = URL.createObjectURL(
			new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' })
		);
		const link = document.createElement('a');
		link.href = url;
		link.download = `desmap-initial-${payload.assessmentId}.json`;
		link.click();
		URL.revokeObjectURL(url);
		showNotice('Đã tải kết quả đối chiếu ban đầu.');
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
	<title>Đối chiếu nghề nghiệp ban đầu | DESMAP</title>
	<meta name="description" content="Đối chiếu ban đầu giữa điểm DESMAP và các nghề bạn chọn." />
</svelte:head>

<Header showBack />
<main
	class="min-h-dvh bg-[radial-gradient(circle_at_82%_8%,rgb(37_99_235_/.15),transparent_27%),var(--color-bg)] py-[clamp(2.2rem,5vw,5rem)] pb-16 text-[#f7f9fb]"
>
	<div
		class="mx-auto w-[min(1100px,calc(100%_-_3rem))] max-[640px]:w-[min(1100px,calc(100%_-_1.4rem))]"
	>
		<section class="border-b border-white/14 py-12">
			<p class="m-0 text-[.68rem] font-[760] tracking-[.16em] text-lime uppercase">
				02 / ĐỐI CHIẾU NGHỀ NGHIỆP BAN ĐẦU
			</p>
			<h1
				class="mt-3 mb-4 max-w-[820px] text-[clamp(2.5rem,7vw,5.8rem)] leading-[.92] font-[760] tracking-[-.065em]"
			>
				Hồ sơ tự báo cáo
			</h1>
			<p class="m-0 max-w-[760px] text-[1.05rem] leading-[1.55] text-[#91a0b4]">
				Các phần trăm là kết quả đối chiếu tạm thời từ câu trả lời của bạn. Đây không phải khuyến
				nghị nghề nghiệp hay kết luận cuối cùng.
			</p>
		</section>

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
							<span class="skeleton block h-7 w-52 max-w-full"></span>
							<span class="skeleton mt-3 block h-3 w-[78%]"></span>
							{#each { length: 5 }, index}
								<div
									class="grid grid-cols-[1.6rem_minmax(7rem,auto)_1fr_3rem] items-center gap-2 border-b border-white/14 py-4 max-[560px]:grid-cols-[1.5rem_1fr_3rem]"
								>
									<span class="skeleton block size-5 rounded-full"></span>
									<span class="skeleton block h-4 w-28 max-w-full"></span>
									<span class="h-2 bg-blue/20 max-[560px]:col-span-3">
										<span class="skeleton block h-full" style:width={`${82 - index * 9}%`}></span>
									</span>
									<span class="skeleton block h-5 w-10 justify-self-end"></span>
								</div>
							{/each}
						</div>

						{#if payload}
							<GroupedDesmapProfile scores={payload.scores} />
						{:else}
							<div class="border border-blue bg-[#071020] p-6" aria-hidden="true">
								<span class="skeleton block h-7 w-40"></span>
								<span class="skeleton mt-3 block h-3 w-[68%]"></span>
								{#each { length: 6 }, index}
									<div class="flex items-center justify-between border-b border-white/14 py-3">
										<span class="skeleton block h-4" style:width={`${42 + (index % 3) * 8}%`}
										></span>
										<span class="skeleton block h-5 w-10"></span>
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
							<span class="skeleton block h-7 w-40"></span>
							<span class="skeleton mt-3 block h-3 w-[min(100%,30rem)]"></span>
						</div>
						<div class="flex items-center gap-4">
							<span class="skeleton block h-11 w-36"></span>
							<span class="skeleton block h-4 w-20"></span>
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
		{:else if response && payload}
			<p class="sr-only" aria-live="polite" role="status">
				Đã có kết quả đối chiếu cho {rankedResults.length} nghề.
			</p>
			<section
				class="mt-3 grid gap-4 min-[850px]:grid-cols-[1.15fr_.85fr]"
				aria-label="Kết quả đối chiếu nghề nghiệp ban đầu"
			>
				<InitialMatchPanel mode={data.initialAssessmentMode} matches={rankedResults} />
				<GroupedDesmapProfile scores={payload.scores} />
			</section>
			<section
				class="mt-4 flex flex-wrap items-center justify-between gap-5 border border-blue bg-[#071020] p-6"
			>
				<div>
					{#if data.initialAssessmentMode === 'ai'}
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
					{#if target?.status === 'locked'}
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
					<button
						class="cursor-pointer border-0 bg-transparent text-blue focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-lime"
						type="button"
						onclick={copySummary}
					>
						{copied ? 'Đã sao chép' : 'Sao chép tóm tắt'}
					</button>
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

<style>
	.skeleton {
		position: relative;
		overflow: hidden;
		background: rgb(37 99 235 / 18%);
	}

	.skeleton::after {
		position: absolute;
		inset: 0;
		content: '';
		background: linear-gradient(
			100deg,
			transparent 20%,
			rgb(188 255 99 / 18%) 48%,
			transparent 76%
		);
		transform: translateX(-110%);
		animation: skeleton-scan 1.55s ease-in-out infinite;
	}

	@keyframes skeleton-scan {
		to {
			transform: translateX(110%);
		}
	}

	@media (prefers-reduced-motion: reduce) {
		.skeleton::after {
			animation: none;
			transform: none;
			opacity: 0.35;
		}
	}
</style>
