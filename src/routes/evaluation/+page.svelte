<script lang="ts">
	import { onMount } from 'svelte';
	import { ArrowRight, ArrowUpRight, CircleDot, Download, Headset, Plus } from '@lucide/svelte';
	import Header from '$lib/components/Header.svelte';
	import {
		answeredCount,
		createEvaluationModel,
		formatDate,
		type EvaluationModel
	} from '$lib/evaluation';
	import {
		readCompletionPayload,
		readSavedQuestionnaire,
		totalQuestionCount,
		type QuestionnaireDraft,
		type QuestionnaireSubmission,
		type StageId
	} from '$lib/questionnaire';

	type Tab = 'profile' | 'report';

	const stageOrder: StageId[] = ['D', 'E', 'S', 'M', 'A', 'P'];
	let activeTab = $state<Tab>('profile');
	let payload = $state<QuestionnaireSubmission | null>(null);
	let draft = $state<QuestionnaireDraft | null>(null);
	let copied = $state(false);
	let downloadNotice = $state('');
	let showSample = $state(false);

	const model = $derived(createEvaluationModel(payload));
	const completed = $derived(Boolean(payload));
	const visibleModel = $derived(model);

	onMount(() => {
		payload = readCompletionPayload();
		draft = readSavedQuestionnaire();
	});

	function pointFor(index: number, value: number, radius = 108) {
		const angle = -Math.PI / 2 + (Math.PI * 2 * index) / stageOrder.length;
		return {
			x: 160 + Math.cos(angle) * radius * (value / 100),
			y: 145 + Math.sin(angle) * radius * (value / 100)
		};
	}

	function radarPoints(values: EvaluationModel['stageScores'], radius = 108): string {
		return stageOrder
			.map((stage, index) => {
				const point = pointFor(index, values[stage], radius);
				return `${point.x},${point.y}`;
			})
			.join(' ');
	}

	function framePoints(level: number): string {
		return radarPoints({ D: level, E: level, S: level, M: level, A: level, P: level }, 108);
	}

	function saveJson() {
		const exportModel = createEvaluationModel(payload);
		const data = {
			format: 'Bản xem trước kết quả DESMAP trên giao diện',
			label: completed
				? 'Tóm tắt tự đánh giá; các nghề nghiệp phù hợp chỉ có tính minh họa cho đến khi AI được kết nối.'
				: 'Bản xem trước báo cáo mẫu; chưa hoàn tất bảng câu hỏi.',
			questionnaire: payload,
			evaluation: exportModel
		};
		const blob = new Blob([JSON.stringify(data, null, 2)], {
			type: 'application/json'
		});
		const url = URL.createObjectURL(blob);
		const link = document.createElement('a');
		link.href = url;
		link.download = `desmap-${completed ? 'results' : 'sample-preview'}.json`;
		link.click();
		URL.revokeObjectURL(url);
		downloadNotice = 'Đã lưu JSON trên thiết bị này.';
		window.setTimeout(() => (downloadNotice = ''), 2400);
	}

	function printReport() {
		window.print();
	}

	async function copySummary() {
		const text = `${visibleModel.targetCareer} · ${visibleModel.careerMatches[0]?.percent ?? 0}% mức độ phù hợp minh họa\n${visibleModel.strengths.join(' · ')}`;
		try {
			await navigator.clipboard.writeText(text);
			copied = true;
			window.setTimeout(() => (copied = false), 2400);
		} catch {
			downloadNotice = 'Không thể sao chép trong trình duyệt này.';
			window.setTimeout(() => (downloadNotice = ''), 2400);
		}
	}

	function exploreTarget() {
		window.location.href = `/experiences?career=${visibleModel.targetExperienceSlug}`;
	}

	function profileTabLabel() {
		return completed ? 'Hồ sơ ban đầu' : showSample ? 'Hồ sơ mẫu' : 'Hồ sơ của bạn';
	}
</script>

<svelte:head>
	<title>Hồ sơ của tôi | DESMAP</title>
	<meta
		name="description"
		content="Xem lại bản tự đánh giá DESMAP và khám phá báo cáo nghề nghiệp mẫu được gắn nhãn rõ ràng."
	/>
</svelte:head>

<Header showBack />

<main class="min-h-dvh bg-bg py-[clamp(2.2rem,5vw,5rem)] pb-16 text-[#f7f9fb]">
	<div
		class="mx-auto w-[min(1380px,calc(100%_-_3rem))] max-[640px]:w-[min(1380px,calc(100%_-_1.4rem))]"
	>
		<section
			class="flex items-end justify-between gap-6 py-[clamp(2rem,5vw,4.7rem)] pb-10 max-[950px]:flex-col max-[950px]:items-start"
		>
			<div>
				<p class="m-0 text-[.68rem] font-[760] tracking-[.16em] text-lime uppercase">
					{completed ? 'Kết quả DESMAP ban đầu' : 'Hồ sơ của bạn đang chờ'}
				</p>
				<h1
					class="mt-[.35rem] mb-4 max-w-[760px] text-[clamp(2.6rem,6vw,5.5rem)] leading-[.95] font-[760] tracking-[-.065em] max-[640px]:text-[clamp(2.5rem,14vw,4.2rem)]"
				>
					{completed
						? 'Nhìn thấy mô thức trong câu trả lời của bạn.'
						: 'Hoàn tất bảng câu hỏi để mở khóa hồ sơ.'}
				</h1>
				<p class="mb-0 max-w-[760px] text-[clamp(1rem,1.6vw,1.3rem)] leading-[1.55] text-[#91a0b4]">
					{completed
						? 'Đây là ảnh chụp nhanh về bản tự đánh giá của bạn. Các nghề nghiệp phù hợp bên dưới chỉ là bản xem trước minh họa cho đến khi dịch vụ đánh giá AI được kết nối.'
						: 'Trang này vẫn hữu ích khi bạn tạm dừng. Hãy tiếp tục các câu trả lời đã lưu, hoặc mở báo cáo mẫu để xem trải nghiệm đã thiết kế.'}
				</p>
			</div>
			<div class="flex min-w-44 items-center gap-[.85rem] pb-[.3rem]">
				<span
					class="inline-block h-[.72rem] w-[.72rem] rounded-full bg-lime shadow-[0_0_0_.3rem_rgb(188_255_99_/.12)]"
				></span>
				<div>
					<strong class="block text-[.78rem]"
						>{completed
							? 'Đã lưu trên thiết bị này'
							: draft
								? `Đã lưu ${answeredCount(draft)} câu trả lời`
								: 'Chưa có câu trả lời được lưu'}</strong
					>
					<small class="mt-[.3rem] block text-[.68rem] text-[#91a0b4]"
						>{completed
							? formatDate(payload?.completedAt)
							: draft
								? 'Tiếp tục khi bạn sẵn sàng'
								: 'Kết quả của bạn sẽ ở lại trên thiết bị này'}</small
					>
				</div>
			</div>
		</section>

		{#if !completed}
			<section
				class="flex items-center justify-between gap-6 border border-blue bg-[linear-gradient(100deg,rgb(37_99_235_/.14),rgb(7_16_32_/.9))] p-[1.35rem_1.6rem] max-[640px]:flex-col max-[640px]:items-start"
				aria-label="Tiến trình bảng câu hỏi"
			>
				<div class="flex items-center gap-4">
					<span class="font-mono text-[.8rem] text-blue">01</span>
					<div>
						<p class="m-0 text-[.68rem] font-[760] tracking-[.16em] text-lime uppercase">
							{draft ? 'Tiếp tục bảng câu hỏi' : 'Bắt đầu tự đánh giá'}
						</p>
						<h2 class="mt-[.35rem] mb-0 text-[1.15rem] font-bold tracking-[-.04em] uppercase">
							{draft
								? `Đã lưu ${answeredCount(draft)} trên ${totalQuestionCount} câu hỏi`
								: 'Câu trả lời của bạn tạo nên bản đồ đầu tiên.'}
						</h2>
						<p class="mt-[.4rem] mb-0 text-[.82rem] text-[#91a0b4]">
							{draft
								? 'Các câu trả lời mới nhất chỉ được lưu trong trình duyệt này.'
								: 'Hãy cho DESMAP biết điều quan trọng với bạn trước khi chúng tôi so sánh hồ sơ với các tình huống nghề nghiệp.'}
						</p>
					</div>
				</div>
				<a
					class="inline-flex min-h-[3.35rem] items-center justify-center gap-3 border border-lime bg-lime px-5 py-3 text-[.75rem] font-[760] tracking-[.07em] text-[#061006] uppercase no-underline max-[640px]:w-full"
					href="/questionnaire"
					>{draft ? 'Tiếp tục bảng câu hỏi' : 'Bắt đầu bảng câu hỏi'}
					<span aria-hidden="true"><ArrowUpRight class="size-[1em]" /></span></a
				>
			</section>
			<div
				class="mt-4 flex items-center justify-between gap-6 border border-dashed border-lime/45 bg-lime/[.045] p-[1rem_1.2rem] max-[640px]:flex-col max-[640px]:items-start"
			>
				<div>
					<span class="text-[.68rem] font-[760] tracking-[.16em] text-lime uppercase"
						>MẪU TÙY CHỌN</span
					><strong class="mt-[.45rem] block text-[.85rem]"
						>Bạn muốn xem trước bố cục hoàn chỉnh?</strong
					>
					<p class="mt-1 mb-0 text-[.78rem] text-[#91a0b4]">
						Mở hồ sơ minh họa được gắn nhãn rõ ràng. Đây không phải dự đoán về bạn.
					</p>
				</div>
				<button
					class="inline-flex cursor-pointer items-center gap-2 border-0 bg-transparent text-[.74rem] font-[760] tracking-[.04em] whitespace-nowrap text-blue uppercase transition hover:-translate-y-0.5 hover:text-lime"
					type="button"
					onclick={() => (showSample = !showSample)}
				>
					{showSample ? 'Ẩn mẫu' : 'Xem trước mẫu'}
					<span aria-hidden="true"><ArrowRight class="size-[1em]" /></span>
				</button>
			</div>
		{/if}

		<div
			class="mt-[2.1rem] mb-5 flex gap-[.35rem] border-b border-white/14"
			role="tablist"
			aria-label="Các chế độ xem hồ sơ"
		>
			<button
				role="tab"
				class={activeTab === 'profile'
					? 'relative cursor-pointer border-0 bg-transparent px-[1.05rem] pt-[.95rem] pb-[1.1rem] text-[.78rem] font-[740] tracking-[.09em] text-[#f7f9fb] uppercase after:absolute after:right-4 after:bottom-[-1px] after:left-4 after:h-0.5 after:scale-x-100 after:bg-lime'
					: 'relative cursor-pointer border-0 bg-transparent px-[1.05rem] pt-[.95rem] pb-[1.1rem] text-[.78rem] font-[740] tracking-[.09em] text-[#91a0b4] uppercase after:absolute after:right-4 after:bottom-[-1px] after:left-4 after:h-0.5 after:scale-x-0 after:bg-lime'}
				type="button"
				onclick={() => (activeTab = 'profile')}
				aria-selected={activeTab === 'profile'}
			>
				{profileTabLabel()}
			</button>
			<button
				role="tab"
				class={activeTab === 'report'
					? 'relative cursor-pointer border-0 bg-transparent px-[1.05rem] pt-[.95rem] pb-[1.1rem] text-[.78rem] font-[740] tracking-[.09em] text-[#f7f9fb] uppercase after:absolute after:right-4 after:bottom-[-1px] after:left-4 after:h-0.5 after:scale-x-100 after:bg-lime'
					: 'relative cursor-pointer border-0 bg-transparent px-[1.05rem] pt-[.95rem] pb-[1.1rem] text-[.78rem] font-[740] tracking-[.09em] text-[#91a0b4] uppercase after:absolute after:right-4 after:bottom-[-1px] after:left-4 after:h-0.5 after:scale-x-0 after:bg-lime'}
				type="button"
				onclick={() => (activeTab = 'report')}
				aria-selected={activeTab === 'report'}
			>
				Đánh giá AI <span class="ml-[.35rem] text-[.58rem] text-lime">MINH HỌA</span>
			</button>
		</div>

		{#if activeTab === 'profile'}
			{#if !completed && !showSample}
				<section
					class="grid grid-cols-[auto_1fr_auto] items-center gap-5 border border-blue bg-[radial-gradient(circle_at_15%_40%,rgb(37_99_235_/.18),transparent_32%),#071020] p-[clamp(1.6rem,4vw,3.2rem)] max-[640px]:flex max-[640px]:flex-col max-[640px]:items-start"
					aria-label="Chưa có hồ sơ"
				>
					<div
						class="grid h-16 w-16 place-items-center rounded-full border border-lime text-2xl text-lime"
					>
						<Plus class="size-[1em]" aria-hidden="true" />
					</div>
					<div>
						<p class="m-0 text-[.68rem] font-[760] tracking-[.16em] text-lime uppercase">
							Chưa tạo hồ sơ
						</p>
						<h2
							class="mt-2 mb-0 max-w-[35rem] text-[clamp(1.2rem,2.6vw,2rem)] font-bold tracking-[-.04em] uppercase"
						>
							Trả lời một vài câu hỏi và bản đồ của bạn sẽ hiện tại đây.
						</h2>
						<p class="mt-3 mb-0 max-w-[42rem] text-[.84rem] leading-[1.5] text-[#91a0b4]">
							Các câu trả lời đã lưu không rời khỏi trình duyệt này trong bản xem trước giao diện.
							Khi hoàn tất, hãy trở lại đây để xem các khía cạnh DESMAP của riêng bạn.
						</p>
					</div>
					<a
						class="inline-flex min-h-[3.35rem] items-center justify-center gap-3 border border-blue bg-transparent px-5 py-3 text-[.75rem] font-[760] tracking-[.07em] text-[#f7f9fb] uppercase no-underline max-[640px]:w-full"
						href="/questionnaire">Đến bảng câu hỏi <span aria-hidden="true"><ArrowUpRight class="size-[1em]" /></span></a
					>
				</section>
			{:else}
				<section
					class="grid grid-cols-[minmax(0,1.15fr)_minmax(0,1fr)] gap-3 max-[950px]:grid-cols-1"
					aria-label={completed ? 'Hồ sơ nghề nghiệp ban đầu' : 'Hồ sơ nghề nghiệp mẫu'}
				>
					<div
						class="border border-blue bg-[radial-gradient(circle_at_85%_12%,rgb(37_99_235_/.1),transparent_30%),#071020] p-6"
					>
						<div class="flex items-start justify-between gap-6">
							<div>
								<p class="m-0 text-[.68rem] font-[760] tracking-[.16em] text-lime uppercase">
									01 / Tôi là ai?
								</p>
								<h2
									class="mt-[.35rem] mb-0 text-[clamp(1.25rem,2vw,1.85rem)] font-bold tracking-[-.04em] uppercase"
								>
									Hồ sơ cá nhân
								</h2>
							</div>
							<span
								class="grid h-10 w-10 place-items-center rounded-full border border-current text-[1.35rem] text-blue"
								><CircleDot class="size-[1em]" aria-hidden="true" /></span
							>
						</div>
						<div class="grid place-items-center pt-4 pb-[.35rem]">
							<svg
								class="w-[min(100%,350px)] overflow-visible [&_text]:fill-blue [&_text]:font-mono [&_text]:text-xs [&_text]:font-extrabold"
								viewBox="0 0 320 290"
								role="img"
								aria-label="Biểu đồ radar khía cạnh DESMAP"
							>
								{#each [20, 40, 60, 80, 100] as level (level)}
									<polygon
										points={framePoints(level)}
										fill="none"
										stroke="rgba(255,255,255,.14)"
										stroke-width="1"
									/>
								{/each}
								{#each stageOrder as stage, index (stage)}
									{@const axis = pointFor(index, 100, 108)}
									<line
										x1="160"
										y1="145"
										x2={axis.x}
										y2={axis.y}
										stroke="rgba(255,255,255,.14)"
										stroke-width="1"
									/>
									{@const label = pointFor(index, 122, 108)}
									<text x={label.x} y={label.y} text-anchor="middle" dominant-baseline="middle">
										{stage}
									</text>
								{/each}
								<polygon
									points={radarPoints(visibleModel.stageScores)}
									fill="rgba(188,255,99,.21)"
									stroke="var(--lime)"
									stroke-width="2.5"
								/>
								{#each stageOrder as stage, index (stage)}
									{@const dot = pointFor(index, visibleModel.stageScores[stage], 108)}
									<circle cx={dot.x} cy={dot.y} r="4.5" fill="var(--lime)" />
								{/each}
							</svg>
							<div class="flex items-center gap-[.55rem] text-[.7rem] text-[#91a0b4]">
								<span
									class="inline-block h-[.72rem] w-[.72rem] rounded-full bg-lime shadow-[0_0_0_.3rem_rgb(188_255_99_/.12)]"
								></span><span>{completed ? 'Điểm tự đánh giá' : 'Điểm mẫu minh họa'}</span>
							</div>
						</div>
						<div class="mt-3 border-t border-white/14">
							{#each visibleModel.profileRows as row (row.code)}
								<div
									class="grid grid-cols-[2rem_1fr_auto] items-center gap-3 border-b border-white/14 py-3"
								>
									<span
										class="grid h-[1.8rem] w-[1.8rem] place-items-center rounded-full border border-blue text-[.7rem] font-extrabold text-blue"
										>{row.code}</span
									>
									<div>
										<strong class="block text-[.77rem] tracking-[.06em] uppercase"
											>{row.label}</strong
										><small class="mt-1 block text-[.72rem] text-[#91a0b4]">{row.detail}</small>
									</div>
									<b class="text-[1.05rem] text-lime">{row.value}</b>
								</div>
							{/each}
						</div>
					</div>

					<div class="grid gap-3">
						<div
							class="border border-blue bg-[radial-gradient(circle_at_85%_12%,rgb(37_99_235_/.1),transparent_30%),#071020] p-6"
						>
							<div class="flex items-start justify-between gap-6">
								<div>
									<p class="m-0 text-[.68rem] font-[760] tracking-[.16em] text-lime uppercase">
										02 / Điều gì phù hợp?
									</p>
									<h2
										class="mt-[.35rem] mb-0 text-[clamp(1.25rem,2vw,1.85rem)] font-bold tracking-[-.04em] uppercase"
									>
										Mức độ phù hợp nghề nghiệp
									</h2>
								</div>
								<span
									class="grid h-10 w-10 place-items-center rounded-full border border-current text-[1.35rem] text-blue"
									><Headset class="size-[1em]" aria-hidden="true" /></span
								>
							</div>
							<div
								class="mt-5 flex items-center justify-between gap-4 border border-lime/30 px-3 py-2 text-[.63rem] font-extrabold tracking-[.1em] text-lime max-[640px]:flex-col max-[640px]:items-start"
							>
								BẢN XEM TRƯỚC MINH HỌA
								<span class="text-right font-medium tracking-normal text-[#91a0b4]"
									>Đánh giá AI chưa được kết nối</span
								>
							</div>
							{#each visibleModel.careerMatches as match, index (match.label)}
								<div
									class="grid grid-cols-[2rem_minmax(6.5rem,auto)_1fr_3.2rem] items-center gap-2 border-b border-white/14 py-3 max-[640px]:grid-cols-[1.6rem_minmax(6rem,auto)_1fr_2.8rem]"
								>
									<span class="font-mono font-extrabold text-blue">0{index + 1}</span><strong
										class={match.accent === 'lime'
											? 'text-[.86rem] tracking-[.08em] text-lime uppercase'
											: 'text-[.86rem] tracking-[.08em] uppercase'}>{match.label}</strong
									>
									<div class="h-[.45rem] overflow-hidden bg-blue/30">
										<span class="block h-full bg-lime" style:width={`${match.percent}%`}></span>
									</div>
									<b class="text-right text-lime">{match.percent}%</b>
								</div>
							{/each}
							<p class="mt-4 mb-0 text-[.7rem] leading-[1.45] text-[#91a0b4]">
								Báo cáo AI thực tế sẽ thay thế các giá trị minh họa sau khi dịch vụ đánh giá được
								kết nối.
							</p>
						</div>

						<div
							class="border border-blue bg-[radial-gradient(circle_at_85%_12%,rgb(37_99_235_/.1),transparent_30%),#071020] p-6"
						>
							<div class="flex items-center justify-between gap-6">
								<div>
									<p class="m-0 text-[.68rem] font-[760] tracking-[.16em] text-lime uppercase">
										03 / Góc nhìn tiếp theo
									</p>
									<h2
										class="mt-[.35rem] mb-0 text-[clamp(1.25rem,2vw,1.85rem)] font-bold tracking-[-.04em] uppercase"
									>
										Khoảng cần phát triển
									</h2>
								</div>
								<span
									class="rounded-full border border-lime px-3 py-2 text-[.57rem] font-bold tracking-[.12em] text-lime uppercase"
									>MỤC TIÊU: {visibleModel.targetCareer.toUpperCase()}</span
								>
							</div>
							<div class="mt-4 border-t border-white/14">
								{#each visibleModel.gaps as gap, index (gap)}
									<div
										class="grid grid-cols-[1.6rem_minmax(0,1fr)_6rem_auto] items-center gap-3 border-b border-white/14 py-3 max-[640px]:grid-cols-[1.4rem_minmax(0,1fr)_auto]"
									>
										<span class="font-mono text-[.72rem] font-extrabold text-blue"
											>0{index + 1}</span
										>
										<div>
											<strong class="block text-[.7rem] tracking-[.03em] uppercase">{gap}</strong
											><small class="mt-1 block text-[.65rem] text-blue uppercase"
												>{index === 0
													? 'Hiện tại: đang phát triển'
													: index === 1
														? 'Hiện tại: trung bình'
														: 'Hiện tại: tốt'}</small
											>
										</div>
										<i
											class="block h-[.35rem] bg-[linear-gradient(to_right,#2563eb_var(--gap),rgb(37_99_235_/.18)_var(--gap))] max-[640px]:hidden"
											style:--gap={`${24 - index * 4}%`}
										></i><b class="text-[.7rem] whitespace-nowrap text-lime"
											>CHÊNH LỆCH {24 - index * 4}%</b
										>
									</div>
								{/each}
							</div>
						</div>
					</div>
				</section>

				<section
					class="mt-3 flex items-center justify-between gap-6 border border-blue bg-[#071020] p-[1.2rem_1.35rem] max-[950px]:flex-wrap max-[950px]:items-start max-[640px]:flex-col"
				>
					<div class="flex items-center gap-[.85rem]">
						<span class="text-[1.9rem] text-lime" aria-hidden="true"><CircleDot class="size-[1em]" /></span>
						<div>
							<strong class="block text-[.82rem] tracking-[.08em] uppercase"
								>{completed
									? 'Hồ sơ của bạn cho thấy một điểm khởi đầu.'
									: 'Hồ sơ mẫu cho thấy hành trình.'}</strong
							>
							<p class="mt-1 mb-0 text-[.75rem] text-lime">
								{completed
									? 'Các tình huống VR có thể bổ sung hành vi quan sát được sau khi kết nối.'
									: 'Hãy dùng bảng câu hỏi để thay thế bản xem trước bằng câu trả lời của riêng bạn.'}
							</p>
						</div>
					</div>
					<button
						class="inline-flex min-h-[3.35rem] cursor-pointer items-center justify-center gap-3 border border-lime bg-lime px-5 py-3 text-[.75rem] font-[760] tracking-[.07em] text-[#061006] uppercase"
						type="button"
						onclick={exploreTarget}
					>
						Khám phá nghề {visibleModel.targetCareer}
						<span aria-hidden="true"><ArrowRight class="size-[1em]" /></span>
					</button>
					<div class="flex gap-4 max-[950px]:ml-auto max-[640px]:ml-0 max-[640px]:flex-wrap">
						<button
							class="cursor-pointer border-0 bg-transparent text-[.74rem] font-[760] tracking-[.04em] text-blue uppercase transition hover:-translate-y-0.5 hover:text-lime"
							type="button"
							onclick={saveJson}
						>
							<span aria-hidden="true"><Download class="size-[1em]" /></span> Tải JSON
						</button><button
							class="cursor-pointer border-0 bg-transparent text-[.74rem] font-[760] tracking-[.04em] text-blue uppercase transition hover:-translate-y-0.5 hover:text-lime"
							type="button"
							onclick={copySummary}
						>
							{copied ? 'Đã sao chép trên thiết bị này' : 'Sao chép tóm tắt'}
						</button>
					</div>
				</section>
			{/if}
		{:else}
			<section
				class="border border-blue bg-[#071020] p-[clamp(1.3rem,3vw,2.2rem)]"
				aria-label="Báo cáo đánh giá AI mẫu"
			>
				<div
					class="flex items-center justify-between gap-6 border-b border-white/14 pb-7 max-[640px]:flex-col max-[640px]:items-start"
				>
					<div>
						<p class="m-0 text-[.68rem] font-[760] tracking-[.16em] text-lime uppercase">
							BÁO CÁO MINH HỌA / 04
						</p>
						<h2
							class="mt-[.35rem] mb-0 text-[clamp(1.8rem,4vw,3.2rem)] font-bold tracking-[-.04em] uppercase"
						>
							Hồ sơ nghề nghiệp cuối cùng
						</h2>
						<p class="mt-2 mb-0 max-w-[48rem] text-[.82rem] leading-[1.5] text-[#91a0b4]">
							Đây là bố cục báo cáo mẫu. Đây không phải kết luận từ AI và không bao gồm quan sát VR.
						</p>
					</div>
					<div
						class="grid h-32 w-32 shrink-0 place-items-center rounded-full border-[.7rem] border-lime border-r-lime/20"
					>
						<strong class="text-[1.5rem]">84%</strong><span
							class="-mt-[.35rem] text-[.55rem] font-extrabold tracking-[.1em] text-lime"
							>ĐỘ PHÙ HỢP MẪU</span
						>
					</div>
				</div>
				<div
					class="mt-3 grid grid-cols-[minmax(0,1.1fr)_minmax(0,.9fr)] gap-3 max-[950px]:grid-cols-1"
				>
					<div>
						<div class="border border-blue bg-[#071020] p-5">
							<div class="flex items-center justify-between gap-6">
								<div>
									<p class="m-0 text-[.68rem] font-[760] tracking-[.16em] text-lime uppercase">
										Tự đánh giá và hành vi quan sát
									</p>
									<h2
										class="mt-[.35rem] mb-0 text-[clamp(1.25rem,2vw,1.85rem)] font-bold tracking-[-.04em] uppercase"
									>
										Điều gì sẽ thay đổi sau VR?
									</h2>
								</div>
							</div>
							{#each visibleModel.observations as observation (observation.self)}
								<div class="grid grid-cols-[2rem_1fr] gap-3 border-b border-white/14 py-4">
									<span class="text-[1.5rem] text-blue" aria-hidden="true"><ArrowRight class="size-[1em]" /></span>
									<div>
										<strong class="block text-[.77rem] uppercase">{observation.self}</strong><small
											class="mt-1 block text-[.75rem] leading-[1.4] text-[#91a0b4]"
											>{observation.observed}</small
										>
									</div>
								</div>
							{/each}
						</div>
						<div
							class="mt-3 grid grid-cols-2 border border-blue bg-[#071020] max-[640px]:grid-cols-1"
						>
							<div class="p-5">
								<h3 class="mt-0 mb-4 text-[.83rem] tracking-[.12em] text-blue uppercase">
									Điểm mạnh
								</h3>
								{#each visibleModel.strengths as strength (strength)}
									<p
										class="mb-0 border-b border-white/14 py-3 text-[.72rem] leading-[1.4] uppercase"
									>
										{strength}
									</p>
								{/each}
							</div>
							<div class="border-l border-white/14 p-5 max-[640px]:border-t max-[640px]:border-l-0">
								<h3 class="mt-0 mb-4 text-[.83rem] tracking-[.12em] text-blue uppercase">
									Khoảng cần phát triển
								</h3>
								{#each visibleModel.gaps as gap (gap)}
									<p
										class="mb-0 border-b border-white/14 py-3 text-[.72rem] leading-[1.4] uppercase"
									>
										{gap}
									</p>
								{/each}
							</div>
						</div>
					</div>
					<div>
						<div class="border border-blue bg-[#071020] p-5">
							<div class="flex items-center justify-between gap-6">
								<div>
									<p class="m-0 text-[.68rem] font-[760] tracking-[.16em] text-lime uppercase">
										Phát triển cá nhân
									</p>
									<h2
										class="mt-[.35rem] mb-0 text-[clamp(1.25rem,2vw,1.85rem)] font-bold tracking-[-.04em] uppercase"
									>
										Lộ trình
									</h2>
								</div>
							</div>
							{#each visibleModel.roadmap as item, index (item)}
								<div class="grid grid-cols-[2rem_1fr] gap-3 border-b border-white/14 py-4">
									<span class="font-mono font-extrabold text-blue">0{index + 1}</span><strong
										class="text-[.77rem] uppercase">{item}</strong
									>
								</div>
							{/each}
							<h3 class="mt-7 mb-3 text-[.83rem] tracking-[.12em] text-blue uppercase">
								Nghề nghiệp tương tự
							</h3>
							<div class="flex flex-wrap gap-2">
								<span class="border border-white/14 px-3 py-2 text-[.67rem] text-[#91a0b4]"
									>Y học cấp cứu</span
								><span class="border border-white/14 px-3 py-2 text-[.67rem] text-[#91a0b4]"
									>Điều dưỡng</span
								><span class="border border-white/14 px-3 py-2 text-[.67rem] text-[#91a0b4]"
									>Điều phối lâm sàng</span
								>
							</div>
						</div>
					</div>
				</div>
				<div class="mt-6 flex justify-end gap-3 max-[640px]:flex-col">
					<button
						class="inline-flex min-h-[3.35rem] cursor-pointer items-center justify-center border border-blue bg-transparent px-5 py-3 text-[.75rem] font-[760] tracking-[.07em] text-[#f7f9fb] uppercase max-[640px]:w-full"
						type="button"
						onclick={saveJson}
					>
						<span aria-hidden="true"><Download class="size-[1em]" /></span> Tải JSON
					</button><button
						class="inline-flex min-h-[3.35rem] cursor-pointer items-center justify-center border border-blue bg-transparent px-5 py-3 text-[.75rem] font-[760] tracking-[.07em] text-[#f7f9fb] uppercase max-[640px]:w-full"
						type="button"
						onclick={printReport}
					>
						In / lưu PDF
					</button><button
						class="inline-flex min-h-[3.35rem] cursor-pointer items-center justify-center border border-lime bg-lime px-5 py-3 text-[.75rem] font-[760] tracking-[.07em] text-[#061006] uppercase max-[640px]:w-full"
						type="button"
						onclick={copySummary}
					>
						{copied ? 'Đã sao chép trên thiết bị này' : 'Sao chép tóm tắt báo cáo'}
					</button>
				</div>
			</section>
		{/if}

		{#if downloadNotice}
			<p
				class="fixed right-4 bottom-4 z-40 m-0 border border-lime bg-[#081306] px-4 py-3 text-[.77rem] text-lime"
				role="status"
			>
				{downloadNotice}
			</p>
		{/if}
	</div>
</main>
