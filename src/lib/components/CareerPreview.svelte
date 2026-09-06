<script lang="ts">
	import { Dialog } from 'bits-ui';

	type Career = { title: string; category: string; description: string; skills: string[]; color: string };
	const careers: Career[] = [
		{ title: 'Product designer', category: 'Create', description: 'Shape digital products by turning curious questions into clear, useful experiences.', skills: ['Empathy', 'Systems thinking', 'Storytelling'], color: '#135cff' },
		{ title: 'Data scientist', category: 'Analyze', description: 'Find the signal in complex data and translate it into decisions people can use.', skills: ['Pattern finding', 'Logic', 'Experimentation'], color: '#8f62ff' },
		{ title: 'Sustainability strategist', category: 'Impact', description: 'Help organizations make measurable progress for people and the planet.', skills: ['Big picture thinking', 'Collaboration', 'Resilience'], color: '#baff60' }
	];
	let selected = $state<Career | null>(null);
	let filter = $state('All');
	const filters = ['All', 'Create', 'Analyze', 'Impact'];
	let visibleCareers = $derived(filter === 'All' ? careers : careers.filter((career) => career.category === filter));
</script>

<section class="careers section-pad" id="careers" aria-labelledby="careers-heading">
	<div class="container">
		<div class="section-kicker"><span>Career library</span><span>Explore a direction, then test how it feels.</span></div>
		<div class="career-heading"><div><p class="eyebrow">A FIELD GUIDE TO WHAT'S NEXT</p><h2 id="careers-heading">Make the<br /><span>unknown</span> tangible.</h2></div><a class="text-link" href="/experiences">Browse full library <span aria-hidden="true">↗</span></a></div>
		<div class="filters" aria-label="Career categories">
			{#each filters as item (item)}<button class:active={filter === item} onclick={() => filter = item}>{item}</button>{/each}
		</div>
		<div class="career-grid">
			{#each visibleCareers as career (career.title)}
				<button class="career-card" style={`--career-color:${career.color}`} onclick={() => selected = career}>
					<div class="career-card-art"><span>{career.title.split(' ').map((word) => word[0]).join('')}</span></div><small>{career.category}</small><h3>{career.title}</h3><p>{career.description}</p><span class="card-link">View role <b aria-hidden="true">↗</b></span>
				</button>
			{/each}
		</div>
	</div>
	<Dialog.Root open={selected !== null} onOpenChange={(open) => { if (!open) selected = null; }}>
		<Dialog.Portal>
			<Dialog.Overlay class="dialog-overlay" />
			<Dialog.Content class="career-dialog">
				{#if selected}
					<div class="dialog-visual" style={`--career-color:${selected.color}`}><span>{selected.title.split(' ').map((word) => word[0]).join('')}</span></div>
					<div class="dialog-copy"><p class="eyebrow">{selected.category} / CAREER SNAPSHOT</p><Dialog.Title>{selected.title}</Dialog.Title><p>{selected.description}</p><h4>Skills that show up here</h4><div class="skill-list">{#each selected.skills as skill (skill)}<span>{skill}</span>{/each}</div><a class="button button-lime" href="/questionnaire">See if it fits <span aria-hidden="true">↗</span></a></div>
					<Dialog.Close class="dialog-close" aria-label="Close role details">×</Dialog.Close>
				{/if}
			</Dialog.Content>
		</Dialog.Portal>
	</Dialog.Root>
</section>

<style>
	.careers { background: #050505; }.career-heading { display: flex; align-items: end; justify-content: space-between; gap: 2rem; padding-block: 3.6rem 2.5rem; }.eyebrow { color: var(--lime); font-family: var(--mono); font-size: .7rem; letter-spacing: .12em; text-transform: uppercase; }.career-heading h2 { max-width: 40rem; margin: 1.3rem 0 0; color: var(--text); font-size: clamp(3rem, 7vw, 6rem); font-weight: 430; line-height: .84; letter-spacing: -.09em; text-transform: uppercase; }.career-heading h2 span { color: var(--lime); }.text-link { color: var(--text); font-family: var(--mono); font-size: .72rem; letter-spacing: .04em; text-decoration: none; text-transform: uppercase; white-space: nowrap; }.text-link span { color: var(--lime); font-size: 1.2rem; }.filters { display: flex; flex-wrap: wrap; gap: .6rem; margin-bottom: 1.3rem; }.filters button { padding: .5rem .85rem; border: 1px solid var(--line-strong); border-radius: 100px; background: transparent; color: var(--muted); cursor: pointer; font-family: var(--mono); font-size: .65rem; letter-spacing: .05em; text-transform: uppercase; transition: .2s ease; }.filters button:hover, .filters button:focus-visible, .filters button.active { border-color: var(--lime); color: var(--lime); outline: none; }.career-grid { display: grid; grid-template-columns: repeat(3,1fr); gap: 1rem; }.career-card { display: flex; min-height: 26rem; flex-direction: column; align-items: flex-start; padding: 1rem; border: 1px solid var(--line); border-radius: .85rem; background: #080b12; color: var(--text); text-align: left; cursor: pointer; transition: border-color .25s ease, transform .25s ease, box-shadow .25s ease; }.career-card:hover, .career-card:focus-visible { border-color: var(--career-color); box-shadow: 0 0 1.3rem color-mix(in srgb, var(--career-color) 25%, transparent); outline: none; transform: translateY(-.35rem); }.career-card-art { display: grid; width: 100%; min-height: 12rem; place-items: center; margin-bottom: 1.4rem; border-radius: .5rem; background: radial-gradient(circle at 50% 45%, color-mix(in srgb, var(--career-color) 45%, transparent), transparent 46%), linear-gradient(145deg, #0d1a37, #05070d); }.career-card-art span, .dialog-visual span { color: var(--career-color); font-size: 4.7rem; font-weight: 300; letter-spacing: -.14em; text-shadow: 0 0 1.2rem var(--career-color); }.career-card small { color: var(--career-color); font-family: var(--mono); font-size: .63rem; letter-spacing: .1em; text-transform: uppercase; }.career-card h3 { margin: .65rem 0 0; font-size: 1.4rem; font-weight: 520; letter-spacing: -.05em; }.career-card p { margin: .7rem 0 1.2rem; color: var(--muted); font-size: .86rem; line-height: 1.45; }.card-link { display: inline-flex; gap: .55rem; margin-top: auto; font-family: var(--mono); font-size: .67rem; letter-spacing: .08em; text-transform: uppercase; }.card-link b { color: var(--lime); font-size: 1.1rem; }
	:global(.dialog-overlay) { position: fixed; inset: 0; z-index: 50; background: rgba(0,0,0,.76); animation: overlay-in .2s ease-out; }:global(.career-dialog) { position: fixed; z-index: 51; left: 50%; top: 50%; display: grid; grid-template-columns: minmax(13rem,.7fr) minmax(18rem,1.3fr); width: min(90vw, 54rem); transform: translate(-50%,-50%); padding: 1rem; border: 1px solid var(--line-strong); border-radius: 1rem; background: #080e1d; box-shadow: 0 2rem 7rem rgba(0,0,0,.6); }.dialog-visual { display: grid; min-height: 25rem; place-items: center; border-radius: .65rem; background: radial-gradient(circle at 50% 45%, color-mix(in srgb, var(--career-color) 48%, transparent), transparent 45%), #050912; }.dialog-copy { padding: 2.7rem 2.3rem; }.dialog-copy .eyebrow { margin: 0; }.dialog-copy :global(h2) { margin: 1.1rem 0; font-size: clamp(2rem, 4vw, 3.4rem); font-weight: 460; letter-spacing: -.08em; line-height: .9; text-transform: uppercase; }.dialog-copy > p:not(.eyebrow) { color: var(--muted); line-height: 1.5; }.dialog-copy h4 { margin: 2rem 0 .8rem; font-family: var(--mono); font-size: .68rem; letter-spacing: .08em; text-transform: uppercase; }.skill-list { display: flex; flex-wrap: wrap; gap: .45rem; margin-bottom: 2rem; }.skill-list span { padding: .38rem .55rem; border: 1px solid var(--line-strong); border-radius: 100px; color: var(--muted); font-size: .7rem; }:global(.career-dialog > .dialog-close) { position: absolute; right: 1rem; top: 1rem; }
	@keyframes overlay-in { from { opacity: 0; } to { opacity: 1; } } @media (max-width: 700px) { .career-heading { display: block; padding-block: 2.5rem 1.8rem; }.career-heading .text-link { display: inline-block; margin-top: 1.7rem; }.career-grid { grid-template-columns: 1fr; }.career-card { min-height: 23rem; }.career-card-art { min-height: 10rem; }:global(.career-dialog) { grid-template-columns: 1fr; max-height: 90vh; overflow: auto; }.dialog-visual { min-height: 10rem; }.dialog-copy { padding: 1.8rem 1rem 1rem; } }
</style>
