<script lang="ts">
	import { ChevronDown } from '@lucide/svelte';
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

	function compatibility(percent: number): number {
		return Math.min(100, Math.max(0, Math.round(percent)));
	}
</script>

<section class="career-suggestions" aria-labelledby="career-suggestions-heading">
	<button
		class="career-suggestions__header"
		type="button"
		aria-expanded={expanded}
		aria-controls="career-suggestions-content"
		onclick={ontoggle}
	>
		<div>
			<h2 id="career-suggestions-heading">Nghề nghiệp phù hợp</h2>
			<p>Gợi ý của AI dựa trên kết quả DESMAP và bằng chứng quan sát trong trải nghiệm.</p>
		</div>
		<span class="career-suggestions__status">
			<span class="career-suggestions__limit">Tối đa 3 nghề</span>
			<ChevronDown
				class={`accordion-icon ${expanded ? 'accordion-icon--expanded' : ''}`}
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
			<ol class="career-suggestions__list">
				{#each visibleSuggestions as suggestion, index (suggestion.id)}
					{@const percent = compatibility(suggestion.compatibilityPercent)}
					<li class="career-suggestion">
						<p class="career-suggestion__index">{String(index + 1).padStart(2, '0')}</p>
						<div class="career-suggestion__identity">
							<h3>{suggestion.name}</h3>
							<div
								class="career-suggestion__track"
								role="img"
								aria-label={`${suggestion.name}: tương thích ${percent}%`}
							>
								<span style:width={`${percent}%`}></span>
							</div>
						</div>
						<p class="career-suggestion__percent">{percent}%</p>
						<p class="career-suggestion__description">{suggestion.description}</p>
					</li>
				{/each}
			</ol>

			<p class="career-suggestions__note">
				Các tỷ lệ này là gợi ý để bạn cân nhắc và trải nghiệm thêm, không phải kết luận cố định về
				nghề nghiệp.
			</p>
		</div>
	{/if}
</section>

<style>
	.career-suggestions {
		margin-top: 1rem;
		border: 1px solid var(--color-blue);
		background: radial-gradient(circle at 90% 8%, rgb(188 255 99 / 8%), transparent 30%), #071020;
	}

	.career-suggestions__header {
		display: flex;
		width: 100%;
		cursor: pointer;
		align-items: flex-start;
		justify-content: space-between;
		gap: 2rem;
		border: 0;
		background: transparent;
		padding: clamp(1.4rem, 3vw, 2rem);
		color: inherit;
		font: inherit;
		text-align: left;
	}

	.career-suggestions__header:focus-visible {
		outline: 2px solid var(--color-lime);
		outline-offset: -0.45rem;
	}

	.career-suggestions__header h2 {
		margin: 0;
		font-size: clamp(1.35rem, 3vw, 1.8rem);
	}

	.career-suggestions__header p {
		margin: 0.5rem 0 0;
		color: #91a0b4;
		font-size: 0.85rem;
		line-height: 1.5;
	}

	.career-suggestions__limit {
		flex: none;
		border: 1px solid rgb(188 255 99 / 45%);
		padding: 0.45rem 0.65rem;
		color: var(--color-lime);
		font-size: 0.7rem;
	}

	.career-suggestions__status {
		display: flex;
		flex: none;
		align-items: center;
		gap: 0.8rem;
	}

	:global(.accordion-icon) {
		color: var(--color-lime);
		transition: transform 160ms ease;
	}

	:global(.accordion-icon--expanded) {
		transform: rotate(180deg);
	}

	.career-suggestions__list {
		margin: 0;
		padding: 0;
		border-top: 1px solid rgb(255 255 255 / 14%);
		list-style: none;
	}

	.career-suggestion {
		display: grid;
		grid-template-columns: 2.5rem minmax(12rem, 0.7fr) 5rem minmax(18rem, 1.3fr);
		align-items: center;
		gap: clamp(1rem, 3vw, 2.5rem);
		padding: clamp(1.4rem, 3vw, 2rem);
		border-bottom: 1px solid rgb(255 255 255 / 14%);
	}

	.career-suggestion:last-child {
		border-bottom: 0;
	}

	.career-suggestion__index {
		margin: 0;
		color: #637188;
		font-size: 0.72rem;
	}

	.career-suggestion__identity h3 {
		margin: 0;
		font-size: clamp(1.1rem, 2vw, 1.45rem);
	}

	.career-suggestion__track {
		height: 0.3rem;
		margin-top: 0.85rem;
		background: rgb(37 99 235 / 28%);
	}

	.career-suggestion__track span {
		display: block;
		height: 100%;
		background: var(--color-lime);
	}

	.career-suggestion__percent {
		margin: 0;
		color: var(--color-lime);
		font-size: clamp(1.7rem, 4vw, 2.6rem);
		font-weight: 750;
		letter-spacing: -0.05em;
	}

	.career-suggestion__description {
		margin: 0;
		color: #c5cfdd;
		font-size: 0.86rem;
		line-height: 1.65;
	}

	.career-suggestions__note {
		margin: 0;
		border-top: 1px solid rgb(255 255 255 / 14%);
		padding: 1rem clamp(1.4rem, 3vw, 2rem);
		color: #77869a;
		font-size: 0.72rem;
		line-height: 1.5;
	}

	@media (max-width: 820px) {
		.career-suggestion {
			grid-template-columns: 2.5rem minmax(0, 1fr) auto;
		}

		.career-suggestion__description {
			grid-column: 2 / -1;
		}
	}

	@media (max-width: 520px) {
		.career-suggestions__limit {
			display: none;
		}

		.career-suggestion {
			grid-template-columns: 2rem minmax(0, 1fr) auto;
			gap: 0.8rem;
		}
	}

	@media (prefers-reduced-motion: reduce) {
		:global(.accordion-icon) {
			transition: none;
		}
	}
</style>
