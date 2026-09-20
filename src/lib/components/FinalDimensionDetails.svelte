<script lang="ts">
	import { ChevronDown } from '@lucide/svelte';
	import { prefersReducedMotion } from 'svelte/motion';
	import { slide } from 'svelte/transition';
	import {
		dimensionLevelFromPercent,
		dimensionLevelIds,
		dimensionLevelLabels,
		finalDimensionDefinitions,
		type DimensionLevelId
	} from '$lib/evaluation/final-dimension-content';
	import { getStageById, type QuestionnaireScores, type StageId } from '$lib/questionnaire';

	let {
		scores,
		levels = {},
		expanded = true,
		ontoggle = () => undefined
	}: {
		scores: QuestionnaireScores;
		levels?: Partial<Record<string, DimensionLevelId>>;
		expanded?: boolean;
		ontoggle?: () => void;
	} = $props();

	const stages: StageId[] = ['D', 'E', 'S', 'M', 'A', 'P'];
	type ExplanationBlock = { type: 'paragraph'; text: string } | { type: 'list'; items: string[] };
	const stageNames: Record<StageId, string> = {
		D: 'Mong muốn',
		E: 'Chuyên môn',
		S: 'Vai trò xã hội',
		M: 'Tư duy',
		A: 'Khả năng thích ứng',
		P: 'Phản ứng với áp lực'
	};
	const stageNotes: Record<StageId, string> = {
		D: 'Với nhóm D, mức đánh giá cho biết bạn coi trọng yếu tố này đến đâu khi làm việc. Mức thấp không có nghĩa là một kết quả xấu.',
		E: 'Với nhóm E, mức đánh giá cho biết bạn đang thể hiện kỹ năng này rõ đến đâu. Mức thấp cho thấy kỹ năng cần thêm thời gian, hướng dẫn hoặc thực hành để phát triển.',
		S: 'Với nhóm S, mức đánh giá cho biết bạn có xu hướng đảm nhận vai trò này đến đâu khi làm việc cùng người khác. Mức thấp không có nghĩa là bạn không thể đảm nhận vai trò đó.',
		M: 'Với nhóm M, mức đánh giá cho biết bạn có xu hướng sử dụng cách tư duy này đến đâu. Mức thấp cho thấy bạn có thể cần tiêu chí rõ ràng hoặc thêm thời gian khi công việc đòi hỏi cách tư duy đó.',
		A: 'Với nhóm A, mức đánh giá cho biết bạn đang thích ứng với thay đổi nghề nghiệp đến đâu. Mức thấp cho thấy bạn có thể cần thêm thông tin, trải nghiệm hoặc hỗ trợ để thích nghi.',
		P: 'Với nhóm P, mức đánh giá cho biết bạn duy trì cách làm việc hiệu quả đến đâu khi gặp loại áp lực này. Mức thấp cho thấy bạn có thể cần chuẩn bị trước hoặc tăng dần mức độ thử thách.'
	};
	let selectedStage = $state<StageId>('D');
	let dimensions = $derived(
		finalDimensionDefinitions.filter((dimension) => dimension.stage === selectedStage)
	);

	function levelFor(dimensionId: string): DimensionLevelId {
		return (
			levels[dimensionId] ?? dimensionLevelFromPercent(scores.groups[dimensionId]?.percent ?? 0)
		);
	}

	function explanationBlocks(content: string): ExplanationBlock[] {
		const blocks: ExplanationBlock[] = [];
		let paragraphLines: string[] = [];
		let listItems: string[] = [];

		const commitParagraph = () => {
			if (paragraphLines.length) blocks.push({ type: 'paragraph', text: paragraphLines.join(' ') });
			paragraphLines = [];
		};
		const commitList = () => {
			if (listItems.length) blocks.push({ type: 'list', items: listItems });
			listItems = [];
		};

		for (const line of content.split('\n')) {
			const text = line.trim();
			if (!text) {
				commitParagraph();
				commitList();
			} else if (text.startsWith('• ')) {
				commitParagraph();
				listItems.push(text.slice(2));
			} else {
				commitList();
				paragraphLines.push(text);
			}
		}

		commitParagraph();
		commitList();
		return blocks;
	}
</script>

<section class="dimension-details" aria-labelledby="dimension-details-heading">
	<button
		class="dimension-details__header"
		type="button"
		aria-expanded={expanded}
		aria-controls="dimension-details-content"
		onclick={ontoggle}
	>
		<div>
			<h2 id="dimension-details-heading">Chi tiết từng khía cạnh</h2>
			<p>Mỗi thanh thể hiện mức đánh giá của một khía cạnh và phần diễn giải tương ứng.</p>
		</div>
		<span class="dimension-details__status">
			<span class="dimension-details__count">28 khía cạnh</span>
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
			id="dimension-details-content"
			transition:slide={{ duration: prefersReducedMotion.current ? 0 : 220 }}
		>
			<nav class="stage-tabs" aria-label="Chọn nhóm DESMAP">
				{#each stages as stage (stage)}
					<button
						type="button"
						class:stage-tabs__button--active={selectedStage === stage}
						class="stage-tabs__button"
						aria-pressed={selectedStage === stage}
						onclick={() => (selectedStage = stage)}
					>
						<span>{stage}</span>
						<small>{stageNames[stage]}</small>
					</button>
				{/each}
			</nav>

			<div class="stage-introduction">
				<div>
					<p class="stage-introduction__letter">{selectedStage}</p>
					<h3>{stageNames[selectedStage]}</h3>
					<p>{getStageById(selectedStage).subtitle}</p>
				</div>
				<p class="stage-introduction__note">{stageNotes[selectedStage]}</p>
			</div>

			<div class="dimension-list">
				{#each dimensions as dimension (dimension.id)}
					{@const level = levelFor(dimension.id)}
					<article class="dimension-card">
						<div class="dimension-card__title">
							<p>{dimension.id}</p>
							<div>
								<h4>{dimension.name}</h4>
								{#if dimension.description}<p>{dimension.description}</p>{/if}
							</div>
							<strong>{dimensionLevelLabels[level]}</strong>
						</div>

						<div
							class="level-scale"
							role="img"
							aria-label={`${dimension.id}: ${dimensionLevelLabels[level]}`}
						>
							{#each dimensionLevelIds as scaleLevel (scaleLevel)}
								<div
									class:level-scale__segment--active={scaleLevel === level}
									class="level-scale__segment"
								>
									<span>{dimensionLevelLabels[scaleLevel]}</span>
								</div>
							{/each}
						</div>

						<div class="dimension-card__explanation">
							{#each explanationBlocks(dimension.levelContent[level]) as block, blockIndex (`${block.type}-${blockIndex}`)}
								{#if block.type === 'paragraph'}
									<p>{block.text}</p>
								{:else}
									<ul>
										{#each block.items as item, itemIndex (`${item}-${itemIndex}`)}
											<li>{item}</li>
										{/each}
									</ul>
								{/if}
							{/each}
						</div>
					</article>
				{/each}
			</div>
		</div>
	{/if}
</section>

<style>
	.dimension-details {
		margin-top: 1rem;
		border: 1px solid var(--color-blue);
		background: #071020;
	}

	.dimension-details__header {
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

	.dimension-details__header:focus-visible {
		outline: 2px solid var(--color-lime);
		outline-offset: -0.45rem;
	}

	.dimension-details__header h2 {
		margin: 0;
		font-size: clamp(1.35rem, 3vw, 1.8rem);
	}

	.dimension-details__header p {
		margin: 0.5rem 0 0;
		color: #91a0b4;
		font-size: 0.85rem;
		line-height: 1.5;
	}

	.dimension-details__count {
		flex: none;
		margin: 0;
		border: 1px solid rgb(37 99 235 / 60%);
		padding: 0.45rem 0.65rem;
	}

	.dimension-details__status {
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

	.stage-tabs {
		display: grid;
		grid-template-columns: repeat(6, 1fr);
		border-block: 1px solid rgb(255 255 255 / 14%);
	}

	.stage-tabs__button {
		display: flex;
		min-width: 0;
		cursor: pointer;
		align-items: baseline;
		gap: 0.55rem;
		border: 0;
		border-right: 1px solid rgb(255 255 255 / 14%);
		background: transparent;
		padding: 0.9rem 1rem;
		color: #91a0b4;
		text-align: left;
	}

	.stage-tabs__button:last-child {
		border-right: 0;
	}

	.stage-tabs__button span {
		font-size: 1.1rem;
		font-weight: 800;
	}

	.stage-tabs__button small {
		overflow: hidden;
		font-size: 0.68rem;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.stage-tabs__button:hover,
	.stage-tabs__button:focus-visible,
	.stage-tabs__button--active {
		background: rgb(188 255 99 / 8%);
		color: var(--color-lime);
	}

	.stage-tabs__button:focus-visible {
		position: relative;
		outline: 2px solid var(--color-lime);
		outline-offset: -3px;
	}

	.stage-introduction {
		display: grid;
		grid-template-columns: minmax(0, 1fr) minmax(18rem, 0.8fr);
		align-items: end;
		gap: 2rem;
		padding: clamp(1.4rem, 3vw, 2rem);
	}

	.stage-introduction__letter {
		margin: 0 0 0.6rem;
		color: var(--color-lime);
		font-size: 2.6rem;
		font-weight: 350;
		line-height: 1;
	}

	.stage-introduction h3 {
		margin: 0;
		font-size: 1.1rem;
	}

	.stage-introduction div > p:last-child {
		margin: 0.45rem 0 0;
		color: #91a0b4;
		font-size: 0.82rem;
	}

	.stage-introduction__note {
		margin: 0;
		border-left: 1px solid rgb(188 255 99 / 55%);
		padding-left: 1rem;
		color: #b8c4d4;
		font-size: 0.78rem;
		line-height: 1.55;
	}

	.dimension-list {
		border-top: 1px solid rgb(255 255 255 / 14%);
	}

	.dimension-card {
		padding: clamp(1.4rem, 3vw, 2rem);
		border-bottom: 1px solid rgb(255 255 255 / 14%);
	}

	.dimension-card:last-child {
		border-bottom: 0;
	}

	.dimension-card__title {
		display: grid;
		grid-template-columns: 3rem minmax(0, 1fr) auto;
		align-items: start;
		gap: 1rem;
	}

	.dimension-card__title > p {
		margin: 0;
		color: var(--color-lime);
		font-size: 1.15rem;
		font-weight: 800;
	}

	.dimension-card__title h4 {
		margin: 0;
		font-size: 1rem;
	}

	.dimension-card__title div p {
		margin: 0.35rem 0 0;
		color: #91a0b4;
		font-size: 0.78rem;
	}

	.dimension-card__title strong {
		color: var(--color-lime);
		font-size: 0.82rem;
		text-align: right;
	}

	.level-scale {
		display: grid;
		grid-template-columns: repeat(5, 1fr);
		gap: 0.3rem;
		margin-top: 1.25rem;
	}

	.level-scale__segment {
		position: relative;
		min-height: 2.35rem;
		border-top: 0.42rem solid rgb(37 99 235 / 35%);
		padding-top: 0.55rem;
		color: #66758a;
		font-size: 0.62rem;
		line-height: 1.25;
	}

	.level-scale__segment--active {
		border-color: var(--color-lime);
		color: #f7f9fb;
	}

	.level-scale__segment--active::before {
		position: absolute;
		top: -0.69rem;
		left: 50%;
		width: 0.55rem;
		height: 0.55rem;
		content: '';
		transform: translateX(-50%);
		border-radius: 50%;
		background: var(--color-lime);
		box-shadow: 0 0 0.75rem rgb(188 255 99 / 45%);
	}

	.dimension-card__explanation {
		width: 100%;
		margin: 1.25rem 0 0;
		color: #c5cfdd;
		font-size: 0.88rem;
		line-height: 1.7;
	}

	.dimension-card__explanation p {
		margin: 0;
	}

	.dimension-card__explanation p + p,
	.dimension-card__explanation ul + p {
		margin-top: 0.9rem;
	}

	.dimension-card__explanation p + ul {
		margin-top: 0.3rem;
	}

	.dimension-card__explanation ul {
		margin: 0;
		padding-left: 1.5rem;
		list-style: disc outside;
	}

	.dimension-card__explanation li {
		padding-left: 0.15rem;
	}

	.dimension-card__explanation li + li {
		margin-top: 0.12rem;
	}

	@media (max-width: 760px) {
		.stage-tabs {
			grid-template-columns: repeat(3, 1fr);
		}

		.stage-tabs__button:nth-child(3) {
			border-right: 0;
		}

		.stage-tabs__button:nth-child(-n + 3) {
			border-bottom: 1px solid rgb(255 255 255 / 14%);
		}

		.stage-introduction {
			grid-template-columns: 1fr;
		}

		.dimension-card__title {
			grid-template-columns: 2.5rem minmax(0, 1fr);
		}

		.dimension-card__title strong {
			grid-column: 2;
			text-align: left;
		}
	}

	@media (max-width: 520px) {
		.dimension-details__count {
			display: none;
		}

		.level-scale__segment span {
			display: none;
		}

		.level-scale__segment {
			min-height: 0.6rem;
			padding-top: 0;
		}

		.stage-tabs__button {
			padding-inline: 0.7rem;
		}

		.stage-tabs__button small {
			display: none;
		}
	}

	@media (prefers-reduced-motion: reduce) {
		:global(.accordion-icon) {
			transition: none;
		}
	}
</style>
