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
	};

	const stages: RadarStage[] = [
		{
			id: 'D',
			name: 'Mong muốn',
			assessment:
				'Nội dung đánh giá của AI về mong muốn nghề nghiệp của bạn sẽ được hiển thị tại đây.'
		},
		{
			id: 'E',
			name: 'Chuyên môn',
			assessment:
				'Nội dung đánh giá của AI về chuyên môn và các điểm mạnh của bạn sẽ được hiển thị tại đây.'
		},
		{
			id: 'S',
			name: 'Vai trò xã hội',
			assessment:
				'Nội dung đánh giá của AI về cách bạn phối hợp với người khác sẽ được hiển thị tại đây.'
		},
		{
			id: 'M',
			name: 'Tư duy',
			assessment:
				'Nội dung đánh giá của AI về cách bạn phân tích và ra quyết định sẽ được hiển thị tại đây.'
		},
		{
			id: 'A',
			name: 'Khả năng thích ứng',
			assessment:
				'Nội dung đánh giá của AI về cách bạn thích ứng với thay đổi sẽ được hiển thị tại đây.'
		},
		{
			id: 'P',
			name: 'Phản ứng với áp lực',
			assessment:
				'Nội dung đánh giá của AI về phản ứng của bạn khi chịu áp lực sẽ được hiển thị tại đây.'
		}
	];

	const center = 180;
	const chartRadius = 108;
	const labelRadius = 148;
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

	function labelPosition(index: number): string {
		const position = coordinates(index, labelRadius);
		return `left: ${(position.x / 360) * 100}%; top: ${(position.y / 360) * 100}%;`;
	}

	function selectStage(id: StageId) {
		selectedId = id;
	}
</script>

<section class="radar-card" aria-labelledby="final-radar-heading">
	<button
		class="radar-card__heading"
		type="button"
		aria-expanded={expanded}
		aria-controls="final-radar-content"
		onclick={ontoggle}
	>
		<div>
			<h2 id="final-radar-heading">Đánh giá cuối cùng</h2>
			<p>Chọn một chữ trên biểu đồ để xem nhận định tương ứng.</p>
		</div>
		<span class="radar-card__status">
			<span class="preview-label">Bản xem trước</span>
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
			class="radar-card__content"
			id="final-radar-content"
			transition:slide={{ duration: prefersReducedMotion.current ? 0 : 220 }}
		>
			<div class="chart-wrap">
				<svg
					class="radar"
					viewBox="0 0 360 360"
					role="img"
					aria-labelledby="radar-title radar-description"
				>
					<title id="radar-title">Biểu đồ sáu điểm DESMAP</title>
					<desc id="radar-description">
						Biểu đồ sử dụng điểm từ đánh giá ban đầu. Chọn một chữ để đọc nhận định của AI.
					</desc>

					{#each gridLevels as level (level)}
						<polygon class="radar__grid" points={polygon(level)} />
					{/each}
					{#each stages as stage, index (stage.id)}
						{@const axisEnd = coordinates(index, chartRadius)}
						<line class="radar__axis" x1={center} y1={center} x2={axisEnd.x} y2={axisEnd.y} />
					{/each}

					<polygon class="radar__score" points={scorePoints} />
					{#each stages as stage, index (stage.id)}
						{@const scorePoint = coordinates(
							index,
							chartRadius * (scores.stages[stage.id].percent / 100)
						)}
						<circle class="radar__point" cx={scorePoint.x} cy={scorePoint.y} r="4.5" />
					{/each}
				</svg>

				<div class="radar-labels" aria-label="Các khía cạnh DESMAP">
					{#each stages as stage, index (stage.id)}
						<button
							class:radar-label--active={selectedId === stage.id}
							class="radar-label"
							style={labelPosition(index)}
							type="button"
							aria-pressed={selectedId === stage.id}
							aria-label={`${stage.id}, ${stage.name}. ${selectedId === stage.id ? 'Đang xem nhận định' : 'Xem nhận định'}`}
							onclick={() => selectStage(stage.id)}
						>
							<span>{stage.id}</span>
						</button>
					{/each}
				</div>
			</div>

			{#if selectedStage}
				<aside class="assessment" aria-live="polite" aria-labelledby="assessment-heading">
					<p class="assessment__letter" aria-hidden="true">{selectedStage.id}</p>
					<p class="assessment__name">{selectedStage.name}</p>
					<h3 id="assessment-heading">Nhận định của AI</h3>
					<p class="assessment__copy">{selectedStage.assessment}</p>
					<div class="assessment__score">
						<span>Điểm từ đánh giá ban đầu</span>
						<strong>{scores.stages[selectedStage.id].percent}%</strong>
					</div>
				</aside>
			{/if}
		</div>

		<p class="radar-card__note">
			Biểu đồ hiện dùng điểm của đánh giá ban đầu. Nhận định của AI đang là nội dung tạm thời.
		</p>
	{/if}
</section>

<style>
	.radar-card {
		border: 1px solid var(--color-blue);
		background: radial-gradient(circle at 32% 48%, rgb(37 99 235 / 12%), transparent 42%), #071020;
		padding: clamp(1.25rem, 3vw, 1.75rem);
	}

	.radar-card__heading {
		display: flex;
		width: 100%;
		cursor: pointer;
		align-items: flex-start;
		justify-content: space-between;
		gap: 1rem;
		border: 0;
		background: transparent;
		padding: 0;
		color: inherit;
		font: inherit;
		text-align: left;
	}

	.radar-card__heading:focus-visible {
		outline: 2px solid var(--color-lime);
		outline-offset: 0.45rem;
	}

	.radar-card__heading h2 {
		margin: 0;
		font-size: clamp(1.35rem, 3vw, 1.8rem);
	}

	.radar-card__heading p {
		margin: 0.45rem 0 0;
		color: #91a0b4;
		font-size: 0.83rem;
		line-height: 1.5;
	}

	.preview-label {
		flex: none;
		border: 1px solid rgb(188 255 99 / 45%);
		padding: 0.35rem 0.55rem;
		color: var(--color-lime);
		font-size: 0.68rem;
		font-weight: 700;
	}

	.radar-card__status {
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

	.radar-card__content {
		display: grid;
		grid-template-columns: minmax(18rem, 24rem) minmax(0, 1fr);
		align-items: center;
		gap: clamp(2rem, 4vw, 4rem);
		min-height: 27rem;
		padding-block: 1.25rem;
	}

	.chart-wrap {
		position: relative;
		width: min(100%, 22rem);
		aspect-ratio: 1;
		justify-self: start;
		margin-left: clamp(0rem, 2vw, 1.5rem);
	}

	.radar {
		display: block;
		width: 100%;
		height: 100%;
		overflow: visible;
	}

	.radar__grid,
	.radar__axis {
		fill: none;
		stroke: rgb(89 137 203 / 30%);
		stroke-width: 1;
		vector-effect: non-scaling-stroke;
	}

	.radar__axis {
		stroke: rgb(89 137 203 / 20%);
	}

	.radar__score {
		fill: rgb(188 255 99 / 14%);
		stroke: var(--color-lime);
		stroke-width: 2;
		stroke-linejoin: round;
		vector-effect: non-scaling-stroke;
	}

	.radar__point {
		fill: #071020;
		stroke: var(--color-lime);
		stroke-width: 2;
		vector-effect: non-scaling-stroke;
	}

	.radar-labels {
		position: absolute;
		inset: 0;
	}

	.radar-label {
		position: absolute;
		display: grid;
		min-width: 2.8rem;
		min-height: 2.8rem;
		cursor: pointer;
		place-content: center;
		transform: translate(-50%, -50%);
		border: 1px solid var(--color-blue);
		border-radius: 50%;
		background: #071020;
		color: #f7f9fb;
		line-height: 1;
		box-shadow: 0 0 0 0 rgb(188 255 99 / 0%);
		transition:
			border-color 160ms ease,
			color 160ms ease,
			box-shadow 160ms ease;
	}

	.radar-label:hover,
	.radar-label:focus-visible,
	.radar-label--active {
		border-color: var(--color-lime);
		color: var(--color-lime);
		box-shadow: 0 0 1rem rgb(188 255 99 / 24%);
	}

	.radar-label:focus-visible {
		outline: 2px solid var(--color-lime);
		outline-offset: 3px;
	}

	.radar-label span {
		font-size: 1rem;
		font-weight: 800;
	}

	.assessment {
		position: relative;
		align-self: center;
		justify-self: stretch;
		width: 100%;
		border-left: 1px solid rgb(188 255 99 / 55%);
		padding: 1rem 0 1rem clamp(1.15rem, 3vw, 2rem);
	}

	.assessment__letter {
		margin: 0;
		color: var(--color-lime);
		font-size: clamp(3.5rem, 7vw, 5.5rem);
		font-weight: 350;
		line-height: 0.9;
		letter-spacing: -0.08em;
	}

	.assessment__name {
		margin: 0.65rem 2.5rem 1.75rem 0;
		color: #91a0b4;
		font-size: 0.82rem;
	}

	.assessment h3 {
		margin: 0 0 0.7rem;
		font-size: 1rem;
	}

	.assessment__copy {
		margin: 0;
		color: #c5cfdd;
		font-size: 0.9rem;
		line-height: 1.65;
	}

	.assessment__score {
		display: flex;
		align-items: flex-end;
		justify-content: space-between;
		gap: 1rem;
		margin-top: 1.5rem;
		border-top: 1px solid rgb(255 255 255 / 14%);
		padding-top: 1rem;
	}

	.assessment__score span {
		max-width: 10rem;
		color: #91a0b4;
		font-size: 0.7rem;
		line-height: 1.4;
	}

	.assessment__score strong {
		color: var(--color-lime);
		font-size: 1.5rem;
	}

	.radar-card__note {
		margin: 0;
		border-top: 1px solid rgb(255 255 255 / 14%);
		padding-top: 1rem;
		color: #77869a;
		font-size: 0.72rem;
		line-height: 1.5;
	}

	@media (max-width: 760px) {
		.radar-card__content {
			grid-template-columns: 1fr;
			gap: 1rem;
		}

		.chart-wrap {
			justify-self: center;
			margin-left: 0;
		}

		.assessment {
			justify-self: stretch;
			width: 100%;
			border-top: 1px solid rgb(188 255 99 / 55%);
			border-left: 0;
			padding: 1.5rem 0 0;
		}
	}

	@media (max-width: 420px) {
		.radar-card__heading {
			align-items: flex-start;
			flex-direction: column;
		}

		.radar-card__content {
			min-height: 23rem;
		}

		.radar-label {
			min-width: 2.5rem;
			min-height: 2.5rem;
		}
	}

	@media (prefers-reduced-motion: reduce) {
		.radar-label,
		:global(.accordion-icon) {
			transition: none;
		}
	}
</style>
