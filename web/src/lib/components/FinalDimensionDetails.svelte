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

<section class="mt-4 border border-blue bg-[#071020]" aria-labelledby="dimension-details-heading">
	<button
		class="flex w-full cursor-pointer items-start justify-between gap-8 border-0 bg-transparent p-[clamp(1.4rem,3vw,2rem)] text-left font-[inherit] text-inherit focus-visible:outline-2 focus-visible:-outline-offset-[.45rem] focus-visible:outline-lime"
		type="button"
		aria-expanded={expanded}
		aria-controls="dimension-details-content"
		onclick={ontoggle}
	>
		<div>
			<h2 class="m-0 text-[clamp(1.35rem,3vw,1.8rem)]" id="dimension-details-heading">
				Chi tiết từng khía cạnh
			</h2>
			<p class="mt-2 mb-0 text-[.85rem] leading-6 text-[#91a0b4]">
				Mỗi thanh thể hiện mức đánh giá của một khía cạnh và phần diễn giải tương ứng.
			</p>
		</div>
		<span class="flex flex-none items-center gap-[.8rem]">
			<span class="m-0 flex-none border border-blue/60 px-[.65rem] py-[.45rem] max-[520px]:hidden"
				>28 khía cạnh</span
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
			id="dimension-details-content"
			transition:slide={{ duration: prefersReducedMotion.current ? 0 : 220 }}
		>
			<nav
				class="grid grid-cols-6 border-y border-white/14 max-[760px]:grid-cols-3"
				aria-label="Chọn nhóm DESMAP"
			>
				{#each stages as stage (stage)}
					<button
						type="button"
						class={`flex min-w-0 cursor-pointer items-baseline gap-[.55rem] border-0 border-r border-white/14 bg-transparent px-4 py-[.9rem] text-left text-[#91a0b4] last:border-r-0 hover:bg-lime/8 hover:text-lime focus-visible:relative focus-visible:bg-lime/8 focus-visible:text-lime focus-visible:outline-2 focus-visible:-outline-offset-3 focus-visible:outline-lime max-[520px]:px-[.7rem] max-[760px]:[&:nth-child(-n+3)]:border-b max-[760px]:[&:nth-child(3)]:border-r-0 ${selectedStage === stage ? 'bg-lime/8 text-lime' : ''}`}
						aria-pressed={selectedStage === stage}
						onclick={() => (selectedStage = stage)}
					>
						<span class="text-[1.1rem] font-extrabold">{stage}</span>
						<small
							class="overflow-hidden text-[.68rem] text-ellipsis whitespace-nowrap max-[520px]:hidden"
							>{stageNames[stage]}</small
						>
					</button>
				{/each}
			</nav>

			<div
				class="grid grid-cols-[minmax(0,1fr)_minmax(18rem,.8fr)] items-end gap-8 p-[clamp(1.4rem,3vw,2rem)] max-[760px]:grid-cols-1"
			>
				<div>
					<p class="mt-0 mb-[.6rem] text-[2.6rem] leading-none font-[350] text-lime">
						{selectedStage}
					</p>
					<h3 class="m-0 text-[1.1rem]">{stageNames[selectedStage]}</h3>
					<p class="mt-[.45rem] mb-0 text-[.82rem] text-[#91a0b4]">
						{getStageById(selectedStage).subtitle}
					</p>
				</div>
				<p class="m-0 border-l border-lime/55 pl-4 text-[.78rem] leading-[1.55] text-[#b8c4d4]">
					{stageNotes[selectedStage]}
				</p>
			</div>

			<div class="border-t border-white/14">
				{#each dimensions as dimension (dimension.id)}
					{@const level = levelFor(dimension.id)}
					<article class="border-b border-white/14 p-[clamp(1.4rem,3vw,2rem)] last:border-b-0">
						<div
							class="grid grid-cols-[3rem_minmax(0,1fr)_auto] items-start gap-4 max-[760px]:grid-cols-[2.5rem_minmax(0,1fr)]"
						>
							<p class="m-0 text-[1.15rem] font-extrabold text-lime">{dimension.id}</p>
							<div>
								<h4 class="m-0 text-base">{dimension.name}</h4>
								{#if dimension.description}
									<p class="mt-[.35rem] mb-0 text-[.78rem] text-[#91a0b4]">
										{dimension.description}
									</p>
								{/if}
							</div>
							<strong
								class="text-right text-[.82rem] text-lime max-[760px]:col-start-2 max-[760px]:text-left"
								>{dimensionLevelLabels[level]}</strong
							>
						</div>

						<div
							class="mt-5 grid grid-cols-5 gap-[.3rem]"
							role="img"
							aria-label={`${dimension.id}: ${dimensionLevelLabels[level]}`}
						>
							{#each dimensionLevelIds as scaleLevel (scaleLevel)}
								<div
									class={`relative min-h-[2.35rem] border-t-[.42rem] pt-[.55rem] text-[.62rem] leading-tight max-[520px]:min-h-[.6rem] max-[520px]:pt-0 ${scaleLevel === level ? "border-lime text-[#f7f9fb] before:absolute before:top-[-.69rem] before:left-1/2 before:size-[.55rem] before:-translate-x-1/2 before:rounded-full before:bg-lime before:shadow-[0_0_.75rem_rgb(188_255_99_/.45)] before:content-['']" : 'border-blue/35 text-[#66758a]'}`}
								>
									<span class="max-[520px]:hidden">{dimensionLevelLabels[scaleLevel]}</span>
								</div>
							{/each}
						</div>

						<div
							class="mt-5 w-full text-[.88rem] leading-[1.7] text-[#c5cfdd] [&>p]:m-0 [&>p+p]:mt-[.9rem] [&>p+ul]:mt-[.3rem] [&>ul]:m-0 [&>ul]:list-outside [&>ul]:list-disc [&>ul]:pl-6 [&>ul+p]:mt-[.9rem] [&>ul>li]:pl-[.15rem] [&>ul>li+li]:mt-[.12rem]"
						>
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
