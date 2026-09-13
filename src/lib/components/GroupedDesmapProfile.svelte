<script lang="ts">
	import { ChevronDown } from '@lucide/svelte';
	import { groupedDimensions } from '$lib/assessment';
	import { questionnaireStages, type QuestionnaireScores, type StageId } from '$lib/questionnaire';

	let { scores }: { scores: QuestionnaireScores } = $props();

	const profileId = $props.id();
	const profileStages = questionnaireStages.map((stage) => ({
		stage,
		dimensions: groupedDimensions.filter((dimension) => stage.dimensionIds.includes(dimension.id))
	}));
	const ratingLabels = [
		'Không tương thích',
		'Kém tương thích',
		'Bình thường',
		'Khá tương thích',
		'Tương thích tốt'
	] as const;
	let expandedStages = $state<Record<StageId, boolean>>({
		D: true,
		E: false,
		S: false,
		M: false,
		A: false,
		P: false
	});

	function ratingLabel(percent: number): (typeof ratingLabels)[number] {
		const level = Math.max(0, Math.min(4, Math.round((percent * 4) / 100)));
		return ratingLabels[level];
	}
</script>

<section class="border border-blue bg-[#071020] p-6" aria-labelledby={`${profileId}-heading`}>
	<h2 id={`${profileId}-heading`} class="mt-0">Hồ sơ DESMAP</h2>
	<p class="text-[.83rem] text-[#91a0b4]">
		Kết quả tự báo cáo theo từng nhóm trong sáu giai đoạn DESMAP.
	</p>

	<div class="border-t border-blue">
		{#each profileStages as { stage, dimensions }, stageIndex (stage.id)}
			{@const controlId = `${profileId}-stage-control-${stageIndex}`}
			{@const panelId = `${profileId}-stage-panel-${stageIndex}`}
			<div class="border-b border-blue">
				<button
					id={controlId}
					class="flex w-full cursor-pointer items-center gap-3 bg-transparent py-4 text-left text-[#f7f9fb] focus-visible:outline-2 focus-visible:outline-offset-[-2px] focus-visible:outline-lime"
					type="button"
					aria-expanded={expandedStages[stage.id]}
					aria-controls={panelId}
					onclick={() => (expandedStages[stage.id] = !expandedStages[stage.id])}
				>
					<span class="min-w-0 flex-1 font-mono text-[.75rem] font-bold tracking-[.08em] text-lime">
						{stage.shortLabel}
					</span>
					<strong class="shrink-0 text-lime">{scores.stages[stage.id].percent}%</strong>
					<ChevronDown
						class={`size-4 shrink-0 ${expandedStages[stage.id] ? 'rotate-180' : ''}`}
						aria-hidden="true"
					/>
				</button>

				<div
					id={panelId}
					role="region"
					aria-labelledby={controlId}
					hidden={!expandedStages[stage.id]}
				>
					<ul class="m-0 list-none border-t border-white/14 p-0">
						{#each dimensions as dimension, dimensionIndex (dimension.id)}
							{@const nameId = `${profileId}-name-${stageIndex}-${dimensionIndex}`}
							{@const ratingId = `${profileId}-rating-${stageIndex}-${dimensionIndex}`}
							<li
								class="grid min-w-0 gap-2 border-b border-white/14 py-4 last:border-b-0 min-[520px]:grid-cols-[minmax(0,1fr)_minmax(10rem,auto)] min-[520px]:items-center min-[520px]:gap-5"
								role="group"
								aria-labelledby={`${nameId} ${ratingId}`}
							>
								<span id={nameId} class="min-w-0 leading-snug break-words">{dimension.name}</span>
								<span
									id={ratingId}
									class="min-w-0 text-[.8rem] leading-snug break-words text-[#91a0b4] min-[520px]:text-right"
								>
									Mức đánh giá:
									<strong class="font-semibold text-[#f7f9fb]">
										{ratingLabel(scores.groups[dimension.id].percent)}
									</strong>
								</span>
							</li>
						{/each}
					</ul>
				</div>
			</div>
		{/each}
	</div>
</section>
