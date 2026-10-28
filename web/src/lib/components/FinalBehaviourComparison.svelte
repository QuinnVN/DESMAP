<script lang="ts">
	import { ArrowDown, ChevronDown, CircleCheck, Eye, TrendingUp } from '@lucide/svelte';
	import { prefersReducedMotion } from 'svelte/motion';
	import { slide } from 'svelte/transition';
	import type {
		BehaviourComparisonFinding,
		BehaviourComparisonKind,
		BehaviourComparisonResult
	} from '$lib/evaluation';

	let {
		comparison,
		expanded = true,
		ontoggle = () => undefined
	}: {
		comparison: BehaviourComparisonResult;
		expanded?: boolean;
		ontoggle?: () => void;
	} = $props();

	type KindCopy = {
		label: string;
		description: string;
		accentClass: string;
		glowClass: string;
		panelClass: string;
		numberClass: string;
		conclusionClass: string;
		Icon: typeof CircleCheck;
	};

	const kindCopy: Record<BehaviourComparisonKind, KindCopy> = {
		confirmed: {
			label: 'Điểm mạnh được xác nhận',
			description: 'Kết quả test và hành vi trong VR cùng cho thấy điểm mạnh này.',
			accentClass: 'border-lime bg-lime text-[#071006]',
			glowClass: 'shadow-[0_0_1.5rem_rgb(188_255_99_/.25)]',
			panelClass:
				'border-lime/70 bg-[radial-gradient(circle_at_92%_0%,rgb(188_255_99_/.2),transparent_34%),linear-gradient(105deg,rgb(188_255_99_/.1),rgb(5_11_22)_34%)] shadow-[0_0_2.25rem_rgb(188_255_99_/.08)]',
			numberClass: 'text-lime/20',
			conclusionClass: 'border-lime/45 bg-lime/12 text-[#e8ffd2]',
			Icon: CircleCheck
		},
		emerging: {
			label: 'Điểm mạnh tiềm ẩn',
			description: 'Hành vi trong VR tốt hơn mức bạn tự đánh giá.',
			accentClass: 'border-[#65dfff] bg-[#65dfff] text-[#03131a]',
			glowClass: 'shadow-[0_0_1.5rem_rgb(101_223_255_/.25)]',
			panelClass:
				'border-[#65dfff]/70 bg-[radial-gradient(circle_at_92%_0%,rgb(101_223_255_/.2),transparent_34%),linear-gradient(105deg,rgb(37_99_235_/.16),rgb(5_11_22)_34%)] shadow-[0_0_2.25rem_rgb(101_223_255_/.08)]',
			numberClass: 'text-[#65dfff]/20',
			conclusionClass: 'border-[#65dfff]/45 bg-[#65dfff]/12 text-[#dffaff]',
			Icon: Eye
		},
		development: {
			label: 'Điểm cần phát triển',
			description: 'Hành vi quan sát được chưa ổn định như kết quả tự đánh giá.',
			accentClass: 'border-[#ffbd59] bg-[#ffbd59] text-[#1b1002]',
			glowClass: 'shadow-[0_0_1.5rem_rgb(255_189_89_/.25)]',
			panelClass:
				'border-[#ffbd59]/70 bg-[radial-gradient(circle_at_92%_0%,rgb(255_189_89_/.2),transparent_34%),linear-gradient(105deg,rgb(255_189_89_/.1),rgb(5_11_22)_34%)] shadow-[0_0_2.25rem_rgb(255_189_89_/.08)]',
			numberClass: 'text-[#ffbd59]/20',
			conclusionClass: 'border-[#ffbd59]/45 bg-[#ffbd59]/12 text-[#ffe7bf]',
			Icon: TrendingUp
		}
	};

	function copyFor(finding: BehaviourComparisonFinding): KindCopy {
		return kindCopy[finding.kind];
	}
</script>

<section
	class="mt-4 border border-blue bg-[#071020]"
	aria-labelledby="behaviour-comparison-heading"
>
	<button
		class="flex w-full cursor-pointer items-start justify-between gap-8 border-0 bg-transparent p-[clamp(1.4rem,3vw,2rem)] text-left font-[inherit] text-inherit focus-visible:outline-2 focus-visible:-outline-offset-[.45rem] focus-visible:outline-lime"
		type="button"
		aria-expanded={expanded}
		aria-controls="behaviour-comparison-content"
		onclick={ontoggle}
	>
		<div>
			<p class="m-0 text-[.66rem] font-bold tracking-[.14em] text-lime uppercase">
				Test và hành vi
			</p>
			<h2 class="mt-2 mb-0 text-[clamp(1.35rem,3vw,1.8rem)]" id="behaviour-comparison-heading">
				Bạn nghĩ gì, bạn đã thể hiện thế nào?
			</h2>
			<p class="mt-2 mb-0 max-w-[46rem] text-[.85rem] leading-6 text-[#91a0b4]">
				Đối chiếu hồ sơ tự đánh giá với hành vi quan sát được trong trải nghiệm
				{comparison.experienceName}.
			</p>
		</div>
		<span class="flex flex-none items-center gap-3">
			{#if comparison.isPlaceholder}
				<span
					class="border border-white/20 px-2.5 py-1.5 text-[.65rem] text-[#aeb9c8] max-[560px]:hidden"
				>
					Dữ liệu minh họa
				</span>
			{/if}
			<ChevronDown
				class={`text-lime transition-transform duration-[160ms] motion-reduce:transition-none ${expanded ? 'rotate-180' : ''}`}
				size={20}
				strokeWidth={2.25}
				aria-hidden="true"
			/>
		</span>
	</button>

	{#if expanded}
		<div
			id="behaviour-comparison-content"
			class="border-t border-white/14"
			transition:slide={{ duration: prefersReducedMotion.current ? 0 : 220 }}
		>
			<ol
				class="m-0 grid list-none items-stretch gap-5 p-[clamp(1rem,2vw,1.5rem)] min-[700px]:grid-cols-2 min-[1050px]:grid-cols-3"
			>
				{#each comparison.findings as finding, index (finding.id)}
					{@const copy = copyFor(finding)}
					<li class={`relative flex min-w-0 flex-col overflow-hidden border ${copy.panelClass}`}>
						<span
							class={`pointer-events-none absolute top-0 right-4 text-[clamp(5rem,8vw,6.75rem)] leading-none font-black tracking-[-.1em] select-none ${copy.numberClass}`}
							aria-hidden="true"
						>
							{String(index + 1).padStart(2, '0')}
						</span>

						<div
							class="relative border-b border-white/14 px-[clamp(1rem,2vw,1.5rem)] py-5 min-[1050px]:min-h-[9.25rem]"
						>
							<div class="flex min-w-0 items-center gap-4">
								<span
									class={`grid size-12 flex-none place-items-center border ${copy.accentClass} ${copy.glowClass}`}
								>
									<copy.Icon size={22} strokeWidth={2.4} aria-hidden="true" />
								</span>
								<div class="min-w-0 flex-1">
									<div class="relative z-10 flex items-center">
										<p
											class="m-0 text-[.66rem] font-extrabold tracking-[.13em] text-white/65 uppercase"
										>
											{copy.label}
										</p>
									</div>
									<h3 class="mt-1 mb-0 text-[clamp(1.2rem,2.4vw,1.65rem)] tracking-[-.02em]">
										{finding.title}
									</h3>
								</div>
							</div>
							<p class="relative z-10 mt-4 mb-0 text-[.72rem] leading-5 text-white/65">
								{copy.description}
							</p>
						</div>

						<section
							class={`relative mx-[clamp(1rem,2vw,1.5rem)] mt-5 border p-[clamp(1rem,2vw,1.35rem)] min-[1050px]:min-h-[9.25rem] ${copy.conclusionClass}`}
							aria-label={`Kết luận cho ${finding.title}`}
						>
							<p class="m-0 text-[.63rem] font-black tracking-[.14em] uppercase">Kết luận chính</p>
							<p class="mt-3 mb-0 text-[clamp(1rem,1.5vw,1.15rem)] leading-[1.55] font-semibold">
								{finding.summary}
							</p>
						</section>

						<div class="relative flex flex-1 flex-col px-[clamp(1rem,2vw,1.5rem)] py-5">
							<p
								class="mt-0 mb-3 text-[.62rem] font-black tracking-[.14em] text-white/45 uppercase"
							>
								Căn cứ đối chiếu
							</p>
							<div class="border border-white/12 bg-[#020712]/65 p-4 min-[1050px]:min-h-[6.6rem]">
								<p
									class="m-0 text-[.62rem] font-extrabold tracking-[.12em] text-[#91a0b4] uppercase"
								>
									Kết quả tự đánh giá
								</p>
								<p class="mt-2 mb-0 text-[.86rem] leading-[1.65] text-[#dce4ef]">
									{finding.questionnaireResult}
								</p>
							</div>
							<div class="grid h-12 place-items-center" aria-hidden="true">
								<span class={`grid size-8 place-items-center border ${copy.accentClass}`}>
									<ArrowDown size={17} strokeWidth={2.5} />
								</span>
							</div>
							<div class="border border-white/12 bg-[#020712]/65 p-4 min-[1050px]:min-h-[6.6rem]">
								<p
									class="m-0 text-[.62rem] font-extrabold tracking-[.12em] text-[#91a0b4] uppercase"
								>
									Bằng chứng trong VR
								</p>
								<p class="mt-2 mb-0 text-[.86rem] leading-[1.65] text-[#dce4ef]">
									{finding.vrEvidence}
								</p>
							</div>
						</div>
					</li>
				{/each}
			</ol>

			<p
				class="m-0 border-t border-white/14 px-[clamp(1.4rem,3vw,2rem)] py-4 text-[.72rem] leading-6 text-[#77869a]"
			>
				Kết quả mô tả hành vi đã quan sát trong tình huống này. Đây không phải kết luận cố định về
				năng lực của bạn.
			</p>
		</div>
	{/if}
</section>
