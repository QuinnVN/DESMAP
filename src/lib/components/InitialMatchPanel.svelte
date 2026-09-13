<script lang="ts">
	import type { InitialAssessmentMode, InitialCareerMatch } from '$lib/assessment';

	let { mode, matches }: { mode: InitialAssessmentMode; matches: InitialCareerMatch[] } = $props();
</script>

<div class="border border-blue bg-[#071020] p-6">
	<h2 class="mt-0">Các nghề nghiệp phù hợp</h2>
	<p class="text-[.83rem] text-[#91a0b4]">Dựa trên đánh giá của AI</p>
	{#if mode === 'weighted'}
		<p class="m-0 py-8 text-center font-semibold text-[#dbeafe]">Chưa đủ dữ liệu để đưa ra gợi ý</p>
	{:else}
		<p class="text-[.83rem] text-[#91a0b4]">
			Các phần trăm độc lập được xếp từ cao xuống thấp. Đây không phải khuyến nghị.
		</p>
		{#each matches as match, index (match.career_id)}
			<div
				class="grid grid-cols-[1.6rem_minmax(7rem,auto)_1fr_3rem] items-center gap-2 border-b border-white/14 py-4 max-[560px]:grid-cols-[1.5rem_1fr_3rem]"
			>
				<span class="font-mono text-blue">0{index + 1}</span>
				<strong>{match.career_name}</strong>
				<div
					class="h-2 bg-blue/30 max-[560px]:col-span-3"
					role="img"
					aria-label={`${match.career_name}: ${match.match_percentage}%`}
				>
					<span class="block h-full bg-lime" style:width={`${match.match_percentage}%`}></span>
				</div>
				<b class="text-right text-lime">{match.match_percentage}%</b>
			</div>
		{/each}
	{/if}
</div>
