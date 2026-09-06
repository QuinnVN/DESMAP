<script lang="ts">
	import { onMount } from 'svelte';
	import { fade, fly } from 'svelte/transition';
	import { totalQuestionCount } from '$lib/questionnaire';

	type Dimension = { id: string; letter: string; label: string; prompt: string; title: string; accent: string; copy: string };
	const dimensions: Dimension[] = [
		{ id: 'desire', letter: 'D', label: 'Desire', prompt: 'What motivates me?', title: 'DESIRE — YOUR VALUE', accent: 'YOUR VALUE', copy: 'This dimension explores your career values: what you want and prioritize in a future job, such as financial security, growth, independence, meaningful impact, recognition, or work-life balance.' },
		{ id: 'expertise', letter: 'E', label: 'Expertise', prompt: 'What am I good at?', title: 'EXPERTISE — YOUR STRENGTHS', accent: 'YOUR STRENGTHS', copy: 'This dimension explores the skills you bring to a challenge: how you learn, solve problems, communicate ideas, and turn strengths into contribution.' },
		{ id: 'social', letter: 'S', label: 'Social role', prompt: 'How do I work with others?', title: 'SOCIAL ROLE — ROLE WITH OTHERS', accent: 'ROLE WITH OTHERS', copy: 'This dimension explores how you tend to contribute when working with others: whether you lead, coordinate, support, share ideas, or prefer working independently.' },
		{ id: 'mind', letter: 'M', label: 'Mind', prompt: 'How do I think?', title: 'MIND — YOUR WAY OF THINKING', accent: 'YOUR WAY OF THINKING', copy: 'This dimension explores how you process information and solve problems: how you analyze situations, make decisions, generate ideas, and approach challenges.' },
		{ id: 'adaptability', letter: 'A', label: 'Adaptability', prompt: 'How do I respond to change?', title: 'ADAPTABILITY — RESPONSE TO CHANGE', accent: 'RESPONSE TO CHANGE', copy: 'This dimension explores how you respond when things change: how you adjust to new environments, unexpected situations, feedback, and unfamiliar tasks.' },
		{ id: 'pressure', letter: 'P', label: 'Pressure', prompt: 'How do I act under pressure?', title: 'PRESSURE — YOUR RESPONSE UNDER PRESSURE', accent: 'YOUR RESPONSE UNDER PRESSURE', copy: 'This dimension explores how you act in demanding situations: how you manage stress, stay focused, make decisions, and continue working when facing pressure.' }
	];
	let activeId = $state('desire');
	let reduceMotion = $state(false);
	let active = $derived(dimensions.find((item) => item.id === activeId) ?? dimensions[0]);

	onMount(() => {
		const media = window.matchMedia('(prefers-reduced-motion: reduce)');
		const update = () => (reduceMotion = media.matches);
		update();
		media.addEventListener('change', update);
		return () => media.removeEventListener('change', update);
	});

	function choose(id: string, moveFocus = false) {
		activeId = id;
		if (moveFocus) queueMicrotask(() => (document.querySelector(`[data-dimension-id="${id}"]`) as HTMLButtonElement | null)?.focus({ preventScroll: true }));
	}
	function onCardKeydown(event: KeyboardEvent, index: number) {
		if (event.key === 'ArrowRight' || event.key === 'ArrowDown') { event.preventDefault(); choose(dimensions[(index + 1) % dimensions.length].id, true); }
		if (event.key === 'ArrowLeft' || event.key === 'ArrowUp') { event.preventDefault(); choose(dimensions[(index + dimensions.length - 1) % dimensions.length].id, true); }
	}
</script>

<section class="explorer section-pad" id="about" aria-labelledby="explorer-heading">
	<div class="container explorer-grid">
		<div class="explorer-copy">
			<p class="eyebrow">DESMAP ASSESSMENT</p>
			<div class="copy-stage" aria-live="polite">
				{#key active.id}
					<div in:fly={{ y: reduceMotion ? 0 : 12, duration: reduceMotion ? 0 : 360 }} out:fade={{ duration: reduceMotion ? 0 : 140 }}>
						<div id="dimension-description" role="tabpanel" tabindex="-1"><h2 id="explorer-heading">{active.title.split(' — ')[0]} <span>—</span><strong>{active.accent}</strong></h2>
						<p class="explorer-description">{active.copy}</p>
						</div>
					</div>
				{/key}
			</div>
			<div class="assessment-meta" aria-label="Assessment details">
				<div><span class="meta-icon">▤</span><b>{totalQuestionCount}</b><small>QUESTIONS</small></div>
				<div><span class="meta-icon">◷</span><b>PACE</b><small>AT YOUR OWN</small></div>
				<div><span class="meta-icon">⌁</span><b>VR</b><small>TEST</small></div>
				<div><span class="meta-icon">⌂</span><b>∞</b><small>PROGRESS SAVED</small></div>
			</div>
		</div>
		<div class="dimension-panel">
			<div class="dimension-grid" role="tablist" aria-label="DESMAP dimensions">
				{#each dimensions as dimension, index (dimension.id)}
					<button data-dimension-id={dimension.id} class:active={activeId === dimension.id} class="dimension-card" role="tab" aria-selected={activeId === dimension.id} aria-controls="dimension-description" tabindex={activeId === dimension.id ? 0 : -1} onclick={() => choose(dimension.id)} onkeydown={(event) => onCardKeydown(event, index)}>
						<span class="dimension-letter">{dimension.letter}</span><span class="dimension-label">{dimension.label}</span><span class="dimension-prompt">{dimension.prompt}</span>
					</button>
				{/each}
			</div>
			<p class="privacy-note">Your responses are used only<br class="desktop-only" /> to build your career profile.</p>
		</div>
	</div>
</section>

<style>
	.explorer { border-top: 1px solid var(--line); border-bottom: 1px solid var(--line); background: radial-gradient(circle at 74% 46%, rgba(6,38,98,.18), transparent 36%); }
	.explorer-grid { display: grid; grid-template-columns: minmax(18rem, .7fr) minmax(35rem, 1.3fr); align-items: center; gap: clamp(2rem, 6vw, 7.4rem); }
	.explorer-copy { display: flex; min-height: 40rem; flex-direction: column; padding-block: 3rem; }
	.eyebrow { color: var(--lime); font-family: var(--mono); font-size: .73rem; font-weight: 760; letter-spacing: .14em; margin: 0; text-transform: uppercase; }
	.copy-stage { display: grid; min-height: 20rem; padding-top: 3.3rem; }
	.copy-stage > div { grid-area: 1 / 1; }
	.copy-stage h2 { margin: 0; max-width: 24rem; font-size: clamp(2rem, 4.8vw, 4.1rem); font-weight: 450; line-height: .98; letter-spacing: -.07em; text-transform: uppercase; }.copy-stage h2 span { color: var(--text); }.copy-stage h2 strong { display: block; color: var(--lime); font-weight: 450; }
	.explorer-description { max-width: 29rem; margin: 2rem 0 0; color: var(--muted); font-size: 1.02rem; line-height: 1.58; }
	.assessment-meta { display: grid; grid-template-columns: repeat(4,1fr); margin-top: auto; border-top: 1px solid var(--line); border-bottom: 1px solid var(--line); }.assessment-meta > div { display: flex; min-width: 0; flex-direction: column; gap: .18rem; padding: 1rem .7rem; border-right: 1px solid var(--line); }.assessment-meta > div:last-child { border-right: 0; }.meta-icon { color: var(--blue); font-size: 1.3rem; line-height: 1; }.assessment-meta b { font-size: 1.15rem; font-weight: 580; letter-spacing: -.04em; }.assessment-meta small { color: var(--muted); font-family: var(--mono); font-size: .53rem; line-height: 1.2; letter-spacing: .06em; }
	.dimension-panel { display: flex; flex-direction: column; align-items: center; }.dimension-grid { display: grid; grid-template-columns: repeat(3, minmax(8rem, 1fr)); gap: 1.3rem; width: 100%; }.dimension-card { display: flex; min-height: 13.3rem; flex-direction: column; align-items: center; justify-content: center; padding: 1rem; border: 1px solid var(--blue); border-radius: .9rem; background: linear-gradient(145deg, rgba(7,18,40,.58), rgba(2,5,17,.72)); color: var(--text); cursor: pointer; transition: border-color .3s ease, box-shadow .3s ease, color .3s ease, transform .3s ease; }.dimension-card:hover, .dimension-card:focus-visible { transform: translateY(-.25rem); border-color: #72aaff; box-shadow: 0 0 1rem rgba(12,102,255,.4); outline: none; }.dimension-card.active { border-color: var(--lime); color: var(--lime); box-shadow: 0 0 1.3rem rgba(188,255,99,.35), inset 0 0 1.5rem rgba(188,255,99,.05); }.dimension-letter { font-size: clamp(4rem, 6vw, 6.9rem); font-weight: 350; line-height: .9; letter-spacing: -.1em; }.dimension-label { margin-top: 1.1rem; font-family: var(--mono); font-size: .83rem; font-weight: 700; letter-spacing: .03em; text-transform: uppercase; }.dimension-prompt { margin-top: .7rem; color: currentColor; font-size: .82rem; opacity: .9; }.privacy-note { margin: 1.5rem 0 0; color: var(--muted); font-family: var(--mono); font-size: .68rem; letter-spacing: .04em; line-height: 1.5; text-align: center; }
	@media (max-width: 1000px) { .explorer-grid { grid-template-columns: 1fr; gap: 2rem; }.explorer-copy { min-height: auto; padding-bottom: 0; }.copy-stage { min-height: 18rem; }.assessment-meta { max-width: 38rem; margin-top: 3rem; }.dimension-panel { padding-bottom: 2rem; } }
	@media (max-width: 600px) { .dimension-grid { grid-template-columns: repeat(2,1fr); gap: .75rem; }.dimension-card { min-height: 10rem; border-radius: .65rem; }.dimension-letter { font-size: 4rem; }.dimension-label { margin-top: .7rem; font-size: .67rem; }.dimension-prompt { font-size: .68rem; }.assessment-meta > div { padding-inline: .45rem; }.assessment-meta b { font-size: .95rem; }.assessment-meta small { font-size: .45rem; }.desktop-only { display: none; } }
</style>
