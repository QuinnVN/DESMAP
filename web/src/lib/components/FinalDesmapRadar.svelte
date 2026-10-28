<script lang="ts">
	import { ChevronDown } from '@lucide/svelte';
	import { prefersReducedMotion } from 'svelte/motion';
	import { slide } from 'svelte/transition';
	import type { QuestionnaireScores, StageId } from '$lib/questionnaire';

	let {
		scores,
		assessments = {},
		expanded = true,
		ontoggle = () => undefined
	}: {
		scores: QuestionnaireScores;
		assessments?: Partial<Record<StageId, string>>;
		expanded?: boolean;
		ontoggle?: () => void;
	} = $props();

	type RadarStage = {
		id: StageId;
		name: string;
		assessment: string;
		strength: string;
		weakness: string;
		labelPosition: string;
	};

	const stages: RadarStage[] = [
		{
			id: 'D',
			name: 'Mong muốn',
			labelPosition: 'top-[8.889%] left-1/2',
			assessment:
				'Nội dung đánh giá của AI về mong muốn nghề nghiệp của bạn sẽ được hiển thị tại đây.',
			strength: 'Bạn nhận biết khá rõ điều mình thật sự coi trọng trong công việc.',
			weakness: 'Bạn cần làm rõ thứ tự ưu tiên khi nhiều mục tiêu xuất hiện cùng lúc.'
		},
		{
			id: 'E',
			name: 'Chuyên môn',
			labelPosition: 'top-[29.444%] left-[85.603%]',
			assessment:
				'Nội dung đánh giá của AI về chuyên môn và các điểm mạnh của bạn sẽ được hiển thị tại đây.',
			strength: 'Bạn có khả năng học và vận dụng kiến thức vào nhiệm vụ thực tế.',
			weakness:
				'Bạn cần phát triển chiều sâu chuyên môn ở các nhiệm vụ phức tạp hoặc chưa quen thuộc.'
		},
		{
			id: 'S',
			name: 'Vai trò xã hội',
			labelPosition: 'top-[70.556%] left-[85.603%]',
			assessment:
				'Nội dung đánh giá của AI về cách bạn phối hợp với người khác sẽ được hiển thị tại đây.',
			strength: 'Bạn biết lắng nghe và giữ nhịp phối hợp với những người xung quanh.',
			weakness:
				'Bạn cần chủ động nói rõ quan điểm khi nhóm có bất đồng hoặc thiếu người ra quyết định.'
		},
		{
			id: 'M',
			name: 'Tư duy',
			labelPosition: 'top-[91.111%] left-1/2',
			assessment:
				'Nội dung đánh giá của AI về cách bạn phân tích và ra quyết định sẽ được hiển thị tại đây.',
			strength: 'Bạn phân tích có trình tự và thường dựa trên dữ kiện.',
			weakness: 'Bạn cần luyện ra quyết định sớm hơn khi không thể thu thập đầy đủ thông tin.'
		},
		{
			id: 'A',
			name: 'Khả năng thích ứng',
			labelPosition: 'top-[70.556%] left-[14.397%]',
			assessment:
				'Nội dung đánh giá của AI về cách bạn thích ứng với thay đổi sẽ được hiển thị tại đây.',
			strength: 'Bạn có thể điều chỉnh cách làm khi bối cảnh thay đổi.',
			weakness: 'Bạn cần thêm thời gian làm quen khi nhiều yếu tố mới xuất hiện cùng lúc.'
		},
		{
			id: 'P',
			name: 'Phản ứng với áp lực',
			labelPosition: 'top-[29.444%] left-[14.397%]',
			assessment:
				'Nội dung đánh giá của AI về phản ứng của bạn khi chịu áp lực sẽ được hiển thị tại đây.',
			strength: 'Bạn duy trì được sự tập trung trong điều kiện áp lực vừa phải.',
			weakness: 'Bạn cần chuẩn bị cách ưu tiên và xin hỗ trợ khi nhiều vấn đề xảy ra cùng lúc.'
		}
	];

	const center = 180;
	const chartRadius = 108;
	const gridLevels = [0.25, 0.5, 0.75, 1];

	let selectedId = $state<StageId>('D');
	let selectedStage = $derived.by(() => {
		const stage = stages.find((item) => item.id === selectedId);
		if (!stage) return null;
		return {
			...stage,
			assessment: assessments[stage.id]?.trim() || stage.assessment
		};
	});
	let scorePoints = $derived(
		stages.map((stage, index) => point(index, scores.stages[stage.id].percent / 100)).join(' ')
	);

	function coordinates(index: number, radius: number) {
		const angle = -Math.PI / 2 + (index * Math.PI * 2) / stages.length;
		return {
			x: center + Math.cos(angle) * radius,
			y: center + Math.sin(angle) * radius
		};
	}

	function point(index: number, scale: number): string {
		const position = coordinates(index, chartRadius * scale);
		return `${position.x},${position.y}`;
	}

	function polygon(scale: number): string {
		return stages.map((_, index) => point(index, scale)).join(' ');
	}

	function selectStage(id: StageId) {
		selectedId = id;
	}
</script>

<section
	class="border border-blue p-[clamp(1.25rem,3vw,1.75rem)] final-radar-gradient"
	aria-labelledby="final-radar-heading"
>
	<button
		class="flex w-full cursor-pointer items-start justify-between gap-4 border-0 bg-transparent p-0 text-left font-[inherit] text-inherit focus-visible:outline-2 focus-visible:outline-offset-[.45rem] focus-visible:outline-lime max-[420px]:flex-col"
		type="button"
		aria-expanded={expanded}
		aria-controls="final-radar-content"
		onclick={ontoggle}
	>
		<div>
			<h2 class="m-0 text-[clamp(1.35rem,3vw,1.8rem)]" id="final-radar-heading">
				Đánh giá cuối cùng
			</h2>
			<p class="mt-[.45rem] mb-0 text-[.83rem] leading-6 text-[#91a0b4]">
				Chọn một chữ trên biểu đồ để xem nhận định tương ứng.
			</p>
		</div>
		<span class="flex flex-none items-center gap-[.8rem]">
			<span
				class="flex-none border border-lime/45 px-[.55rem] py-[.35rem] text-[.68rem] font-bold text-lime"
				>Bản xem trước</span
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
			class="grid min-h-[27rem] grid-cols-[minmax(18rem,24rem)_minmax(0,1fr)] items-start gap-[clamp(2rem,4vw,4rem)] py-7 max-[760px]:grid-cols-1 max-[760px]:gap-4 max-[420px]:min-h-[23rem]"
			id="final-radar-content"
			transition:slide={{ duration: prefersReducedMotion.current ? 0 : 220 }}
		>
			<div
				class="relative ml-[clamp(0rem,2vw,1.5rem)] aspect-square w-[min(100%,22rem)] justify-self-start max-[760px]:ml-0 max-[760px]:justify-self-center"
			>
				<svg
					class="block size-full overflow-visible"
					viewBox="0 0 360 360"
					role="img"
					aria-labelledby="radar-title radar-description"
				>
					<title id="radar-title">Biểu đồ sáu điểm DESMAP</title>
					<desc id="radar-description">
						Biểu đồ sử dụng điểm từ đánh giá ban đầu. Chọn một chữ để đọc nhận định của AI.
					</desc>

					{#each gridLevels as level (level)}
						<polygon
							class="fill-none stroke-[rgb(89_137_203_/.3)] stroke-1 [vector-effect:non-scaling-stroke]"
							points={polygon(level)}
						/>
					{/each}
					{#each stages as stage, index (stage.id)}
						{@const axisEnd = coordinates(index, chartRadius)}
						<line
							class="stroke-[rgb(89_137_203_/.2)] stroke-1 [vector-effect:non-scaling-stroke]"
							x1={center}
							y1={center}
							x2={axisEnd.x}
							y2={axisEnd.y}
						/>
					{/each}

					<polygon
						class="fill-lime/14 stroke-lime stroke-2 [stroke-linejoin:round] [vector-effect:non-scaling-stroke]"
						points={scorePoints}
					/>
					{#each stages as stage, index (stage.id)}
						{@const scorePoint = coordinates(
							index,
							chartRadius * (scores.stages[stage.id].percent / 100)
						)}
						<circle
							class="fill-[#071020] stroke-lime stroke-2 [vector-effect:non-scaling-stroke]"
							cx={scorePoint.x}
							cy={scorePoint.y}
							r="4.5"
						/>
					{/each}
				</svg>

				<div class="absolute inset-0" aria-label="Các khía cạnh DESMAP">
					{#each stages as stage (stage.id)}
						<button
							class={`absolute grid min-h-11 min-w-11 -translate-1/2 cursor-pointer place-content-center rounded-full border bg-[#071020] leading-none transition-[border-color,color,box-shadow] duration-[160ms] motion-reduce:transition-none max-[420px]:min-h-10 max-[420px]:min-w-10 ${stage.labelPosition} ${selectedId === stage.id ? 'border-lime text-lime shadow-[0_0_1rem_rgb(188_255_99_/.24)]' : 'border-blue text-[#f7f9fb] shadow-[0_0_0_0_rgb(188_255_99_/.0)] hover:border-lime hover:text-lime hover:shadow-[0_0_1rem_rgb(188_255_99_/.24)]'} focus-visible:border-lime focus-visible:text-lime focus-visible:shadow-[0_0_1rem_rgb(188_255_99_/.24)] focus-visible:outline-2 focus-visible:outline-offset-3 focus-visible:outline-lime`}
							type="button"
							aria-pressed={selectedId === stage.id}
							aria-label={`${stage.id}, ${stage.name}. ${selectedId === stage.id ? 'Đang xem nhận định' : 'Xem nhận định'}`}
							onclick={() => selectStage(stage.id)}
						>
							<span class="text-base font-extrabold">{stage.id}</span>
						</button>
					{/each}
				</div>
			</div>

			{#if selectedStage}
				<aside
					class="relative w-full max-w-[54rem] justify-self-stretch border-l border-lime/55 py-1 pr-0 pl-[clamp(1.15rem,3vw,2rem)] max-[760px]:justify-self-stretch max-[760px]:border-t max-[760px]:border-l-0 max-[760px]:pt-6 max-[760px]:pr-0 max-[760px]:pb-0 max-[760px]:pl-0"
					aria-live="polite"
					aria-labelledby="assessment-heading"
				>
					<div class="mb-7 flex items-center gap-4">
						<p
							class="m-0 grid size-12 flex-none place-items-center border border-lime/55 text-[2rem] leading-none font-semibold text-lime"
							aria-hidden="true"
						>
							{selectedStage.id}
						</p>
						<div>
							<p class="m-0 text-[.66rem] font-bold tracking-[.14em] text-[#77869a] uppercase">
								Khía cạnh DESMAP
							</p>
							<h3 class="mt-1 mb-0 text-[clamp(1.15rem,2vw,1.45rem)]" id="assessment-heading">
								{selectedStage.name}
							</h3>
						</div>
					</div>
					<p class="mb-2 text-[.7rem] font-bold tracking-[.12em] text-[#91a0b4] uppercase">
						Nhận định tổng hợp
					</p>
					<p
						class="m-0 max-w-[76ch] text-[.94rem] leading-[1.75] whitespace-pre-line text-[#d4dce7]"
					>
						{selectedStage.assessment}
					</p>
					<div class="mt-7 grid grid-cols-2 border-t border-white/14 pt-5 max-[520px]:grid-cols-1">
						<section
							class="pr-6 max-[520px]:pr-0 max-[520px]:pb-5"
							aria-labelledby="strength-heading"
						>
							<h4
								class="m-0 text-[.7rem] font-bold tracking-[.12em] text-lime uppercase"
								id="strength-heading"
							>
								Điểm mạnh
							</h4>
							<p class="mt-2 mb-0 text-[.82rem] leading-[1.65] text-[#aeb9c8]">
								{selectedStage.strength}
							</p>
						</section>
						<section
							class="border-l border-white/14 pl-6 max-[520px]:border-t max-[520px]:border-l-0 max-[520px]:pt-5 max-[520px]:pl-0"
							aria-labelledby="weakness-heading"
						>
							<h4
								class="m-0 text-[.7rem] font-bold tracking-[.12em] text-[#f5ba66] uppercase"
								id="weakness-heading"
							>
								Điểm cần phát triển
							</h4>
							<p class="mt-2 mb-0 text-[.82rem] leading-[1.65] text-[#aeb9c8]">
								{selectedStage.weakness}
							</p>
						</section>
					</div>
				</aside>
			{/if}
		</div>

		<p class="m-0 border-t border-white/14 pt-4 text-[.72rem] leading-6 text-[#77869a]">
			Biểu đồ hiện dùng điểm của đánh giá ban đầu. Nhận định của AI đang là nội dung tạm thời.
		</p>
	{/if}
</section>
