<script lang="ts">
	import { ChevronDown, Compass, Sparkles } from '@lucide/svelte';
	import { prefersReducedMotion } from 'svelte/motion';
	import { slide } from 'svelte/transition';
	import type { FinalCareerSuggestion } from '$lib/evaluation';

	let {
		suggestions,
		expanded = true,
		ontoggle = () => undefined
	}: {
		suggestions: FinalCareerSuggestion[];
		expanded?: boolean;
		ontoggle?: () => void;
	} = $props();
	let visibleSuggestions = $derived(suggestions.slice(0, 3));
	let primarySuggestion = $derived(visibleSuggestions[0]);
	let otherSuggestions = $derived(visibleSuggestions.slice(1));
</script>

<section
	class="mt-4 overflow-hidden border border-blue/75 bg-[#050b18] shadow-[0_1.5rem_5rem_rgb(4_10_24_/.7)]"
	aria-labelledby="career-suggestions-heading"
>
	<button
		class="relative isolate flex w-full cursor-pointer items-start justify-between gap-8 overflow-hidden border-0 bg-[radial-gradient(circle_at_88%_0%,rgb(188_255_99_/.16),transparent_28%),linear-gradient(105deg,#071020_0%,#071020_58%,#0c1830_100%)] p-[clamp(1.4rem,3vw,2rem)] text-left font-[inherit] text-inherit before:absolute before:top-0 before:left-0 before:h-full before:w-1 before:bg-lime focus-visible:outline-2 focus-visible:-outline-offset-[.45rem] focus-visible:outline-lime"
		type="button"
		aria-expanded={expanded}
		aria-controls="career-suggestions-content"
		onclick={ontoggle}
	>
		<div>
			<div class="flex items-center gap-3">
				<span
					class="grid size-8 place-items-center border border-blue/60 bg-blue/15 text-[#78a0ff]"
					aria-hidden="true"
				>
					<Sparkles size={16} strokeWidth={2.2} />
				</span>
				<h2 class="m-0 text-[clamp(1.35rem,3vw,1.8rem)]" id="career-suggestions-heading">
					Gợi ý nghề nghiệp
				</h2>
			</div>
			<p class="mt-2 mb-0 text-[.85rem] leading-6 text-[#91a0b4]">
				AI đối chiếu toàn bộ kết quả DESMAP với hành vi quan sát được trong trải nghiệm VR.
			</p>
		</div>
		<span class="flex flex-none items-center gap-[.8rem]">
			<span
				class="flex-none border border-lime/45 bg-lime/8 px-[.75rem] py-[.5rem] text-[.68rem] font-[760] tracking-[.04em] text-lime uppercase max-[520px]:hidden"
				>1 ưu tiên · 2 tham khảo</span
			>
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
			id="career-suggestions-content"
			transition:slide={{ duration: prefersReducedMotion.current ? 0 : 220 }}
		>
			<div class="border-t border-white/14">
				{#if primarySuggestion}
					<article
						class="grid min-[820px]:grid-cols-[minmax(17rem,.72fr)_minmax(25rem,1.28fr)]"
						aria-labelledby={`primary-career-${primarySuggestion.id}`}
					>
						<div
							class="relative isolate min-h-[20rem] overflow-hidden border-b border-blue/35 bg-[radial-gradient(circle_at_18%_8%,rgb(37_99_235_/.2),transparent_32%),linear-gradient(145deg,#0d1930_0%,#091323_58%,#071020_100%)] p-[clamp(1.5rem,4vw,3rem)] text-white min-[820px]:border-r min-[820px]:border-b-0"
						>
							<span
								class="pointer-events-none absolute -right-3 -bottom-10 -z-10 font-mono text-[clamp(9rem,18vw,15rem)] leading-none font-[800] tracking-[-.12em] text-blue/18"
								aria-hidden="true">01</span
							>
							<p
								class="m-0 inline-flex items-center gap-2 border border-lime/35 bg-lime/6 px-3 py-2 text-[.65rem] font-[760] tracking-[.13em] text-lime uppercase"
							>
								<Sparkles size={13} strokeWidth={2.4} aria-hidden="true" />
								Phù hợp nhất với hồ sơ của bạn
							</p>
							<h3
								class="mt-[clamp(3.5rem,8vw,6rem)] mb-0 max-w-[8ch] text-[clamp(2.8rem,7vw,5.5rem)] leading-[.9] font-[780] tracking-[-.065em]"
								id={`primary-career-${primarySuggestion.id}`}
							>
								{primarySuggestion.name}
							</h3>
						</div>
						<div
							class="relative bg-[radial-gradient(circle_at_100%_0%,rgb(188_255_99_/.09),transparent_30%),#0d1625] p-[clamp(1.6rem,4vw,3.25rem)] text-[#dce5f0] before:absolute before:top-[2.2rem] before:-left-3 before:hidden before:size-6 before:rotate-45 before:border-b before:border-l before:border-blue/35 before:bg-[#0d1625] min-[820px]:before:block"
						>
							<p
								class="m-0 font-mono text-[.68rem] font-[700] tracking-[.12em] text-lime uppercase"
							>
								Vì sao đây là lựa chọn hàng đầu
							</p>
							<div class="mt-5 h-px w-16 bg-lime/35" aria-hidden="true"></div>
							<p
								class="mt-5 mb-0 text-[clamp(.92rem,1.4vw,1.04rem)] leading-[1.8] font-[560] whitespace-pre-line"
							>
								{primarySuggestion.description}
							</p>
						</div>
					</article>
				{/if}

				{#if otherSuggestions.length > 0}
					<section
						class="border-t border-white/14 bg-[radial-gradient(circle_at_50%_120%,rgb(37_99_235_/.1),transparent_48%),#050b18] p-[clamp(1.5rem,4vw,3rem)]"
						aria-labelledby="other-careers-heading"
					>
						<div class="mb-7 flex items-end justify-between gap-4">
							<div class="flex items-start gap-4">
								<span
									class="mt-1 grid size-9 flex-none place-items-center border border-blue/70 bg-blue/15 text-[#6f9bff]"
									aria-hidden="true"
								>
									<Compass size={18} strokeWidth={2.1} />
								</span>
								<div>
									<p class="m-0 text-[.65rem] font-[760] tracking-[.14em] text-[#6f9bff] uppercase">
										Hướng tham khảo
									</p>
									<h3 class="mt-2 mb-0 text-[clamp(1.15rem,2vw,1.5rem)]" id="other-careers-heading">
										Hai nghề cũng phù hợp
									</h3>
								</div>
							</div>
						</div>
						<ol class="m-0 grid list-none gap-4 p-0 min-[700px]:grid-cols-2">
							{#each otherSuggestions as suggestion, index (suggestion.id)}
								<li
									class={[
										'relative isolate min-h-44 overflow-hidden border p-[clamp(1.2rem,3vw,2rem)] before:absolute before:top-0 before:left-0 before:h-1 before:w-full',
										index === 0
											? 'border-blue/45 bg-[linear-gradient(135deg,rgb(37_99_235_/.1),rgb(7_16_32_/.98)_58%)] before:bg-blue'
											: 'border-lime/35 bg-[linear-gradient(135deg,rgb(188_255_99_/.06),rgb(7_16_32_/.98)_58%)] before:bg-lime'
									]}
								>
									<p class="m-0 font-mono text-[.72rem] font-[700] text-[#6f9bff]">
										{String(index + 2).padStart(2, '0')}
									</p>
									<h4 class="mt-5 mb-0 text-[clamp(1.35rem,2.5vw,1.8rem)] tracking-[-.025em]">
										{suggestion.name}
									</h4>
									<p class="mt-3 mb-0 max-w-[48rem] text-[.84rem] leading-[1.65] text-[#b9c6d8]">
										{suggestion.description}
									</p>
								</li>
							{/each}
						</ol>
					</section>
				{/if}
			</div>
		</div>
	{/if}
</section>
