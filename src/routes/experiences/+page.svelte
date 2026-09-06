<script lang="ts">
	import { onMount } from 'svelte';
	import { Dialog } from 'bits-ui';
	import { experiences, type CareerExperience, type ExperienceCategory } from '$lib/evaluation';

	type Filter = 'All' | ExperienceCategory;
	const filters: Filter[] = ['All', 'Healthcare', 'Law', 'Education', 'Business', 'Engineering'];
	let query = $state('');
	let filter = $state<Filter>('All');
	let dialogOpen = $state(false);
	let selected = $state<CareerExperience | null>(null);
	let targetSlug = $state('doctor');

	const recommended = $derived(experiences.find((experience) => experience.slug === targetSlug) ?? experiences[0]);
	const visibleExperiences = $derived(experiences.filter((experience) => {
		const search = query.trim().toLowerCase();
		const matchesSearch = !search || [experience.title, experience.category, experience.description, ...experience.observedFactors].join(' ').toLowerCase().includes(search);
		const matchesFilter = filter === 'All' || experience.category === filter;
		return matchesSearch && matchesFilter;
	}));
	const supportingExperiences = $derived(visibleExperiences.filter((experience) => experience.slug !== recommended.slug));

	onMount(() => {
		const slug = new URLSearchParams(window.location.search).get('career');
		if (slug && experiences.some((experience) => experience.slug === slug)) targetSlug = slug;
	});

	function openDetails(experience: CareerExperience) {
		selected = experience;
		dialogOpen = true;
	}

	function clearFilters() {
		query = '';
		filter = 'All';
	}

	function statusLabel(status: CareerExperience['status']): string {
		return status === 'recommended' ? 'Your target' : status === 'locked' ? 'Coming soon' : 'Ready to review';
	}
</script>

<svelte:head>
	<title>Career library | DESMAP</title>
	<meta name="description" content="Explore DESMAP career scenarios and review the factors each scenario is designed to observe." />
</svelte:head>

<main class="library-page">
	<div class="library-shell">
		<div class="eyebrow-row"><p class="eyebrow">03 / VR CAREER EXPERIENCE</p><span class="step-pill">{experiences.length} CAREERS AVAILABLE</span></div>
		<section class="library-intro">
			<div><p class="kicker">Career library</p><h1>Choose a career to experience.</h1><p class="intro-copy">Step into a simulated workplace and notice how you respond in real situations. Read the mission details before you choose what to compare.</p></div>
			<a class="back-link" href="/evaluation">← Back to profile</a>
		</section>

		<section class="library-controls" aria-label="Career library filters">
			<label class="search-field"><span aria-hidden="true">⌕</span><input bind:value={query} type="search" placeholder="Search for a career..." aria-label="Search for a career" /></label>
			<div class="category-filter" role="group" aria-label="Filter by category">
				{#each filters as item (item)}<button class:active={filter === item} type="button" aria-pressed={filter === item} onclick={() => (filter = item)}>{item}</button>{/each}
			</div>
		</section>

		<section class="recommended-grid" aria-label="Recommended career">
			<button class="recommended-card" type="button" onclick={() => openDetails(recommended)}>
				<div class="recommended-image"><img src={recommended.image} alt="" /></div>
				<div class="recommended-content">
					<div class="card-topline"><span class="status status-recommended">{statusLabel(recommended.status)} <i></i></span><span class="category-label">{recommended.category}</span></div>
					<p class="kicker">Recommended lens</p><h2>{recommended.title}</h2>
					<div class="duration"><span aria-hidden="true">◷</span>{recommended.missionCount} missions · {recommended.duration}</div>
					<p class="card-description">{recommended.description}</p>
						<div class="observed"><span>Observed factors</span><div>{#each recommended.observedFactors as factor (factor)}<b>{factor}</b>{/each}</div></div>
					<span class="card-action">View mission details <span aria-hidden="true">→</span></span>
				</div>
			</button>
			<aside class="target-note"><span class="note-icon">i</span><div><p class="kicker">Why this lens?</p><strong>{recommended.title} is your current target.</strong><p>Recommendation follows the career interest you selected. The comparison percentages on the profile remain demo values until AI is connected.</p></div></aside>
		</section>

		<div class="results-row"><p><strong>{visibleExperiences.length}</strong> {visibleExperiences.length === 1 ? 'career' : 'careers'} shown</p>{#if query || filter !== 'All'}<button class="text-button" type="button" onclick={clearFilters}>Clear filters ↗</button>{/if}</div>
		{#if supportingExperiences.length}
			<section class="experience-grid" aria-label="Other career experiences">
					{#each supportingExperiences as experience (experience.slug)}
					<button class="experience-card" type="button" onclick={() => openDetails(experience)}>
						<div class="experience-image"><img src={experience.image} alt="" /><span class:locked={experience.status === 'locked'} class="status overlay-status">{statusLabel(experience.status)}</span></div>
						<div class="experience-body"><div class="card-topline"><span class="category-label">{experience.category}</span><span class="duration-small">{experience.missionCount} missions · {experience.duration}</span></div><h2>{experience.title}</h2><p>{experience.description}</p><span class="card-action">View details <span aria-hidden="true">→</span></span></div>
					</button>
				{/each}
			</section>
		{:else}
			<section class="empty-results"><span class="empty-symbol">⌕</span><div><p class="kicker">No exact match</p><h2>Try another search or category.</h2><p>The target card stays visible above so you can keep your place.</p></div><button class="button button-outline" type="button" onclick={clearFilters}>Show all careers</button></section>
		{/if}

		<footer class="library-footer"><a href="/evaluation">← Back to results</a><span>Career scenarios are designed for reflection. No experience launches from this preview.</span></footer>
	</div>
</main>

<Dialog.Root bind:open={dialogOpen}>
	<Dialog.Portal>
		<Dialog.Overlay class="dialog-overlay" />
		<Dialog.Content class="experience-dialog" aria-label={selected ? `${selected.title} experience details` : 'Experience details'}>
			{#if selected}
				<div class="dialog-head"><div><p class="kicker">{selected.category} / {statusLabel(selected.status)}</p><Dialog.Title>{selected.title}</Dialog.Title><Dialog.Description>{selected.description}</Dialog.Description></div><Dialog.Close class="dialog-close" aria-label="Close details">×</Dialog.Close></div>
				<div class="dialog-image"><img src={selected.image} alt="" /></div>
				<div class="dialog-meta"><span>{selected.missionCount} missions</span><span>{selected.duration}</span><span>{selected.status === 'locked' ? 'Module in development' : 'Scenario overview'}</span></div>
				<div class="dialog-columns"><div><h3>Mission focus</h3><ul>{#each selected.missions as mission (mission)}<li>{mission}</li>{/each}</ul></div><div><h3>Observed factors</h3><ul>{#each selected.observedFactors as factor (factor)}<li>{factor}</li>{/each}</ul></div></div>
				<div class="dialog-fit"><p class="kicker">Why compare this?</p><p>{selected.whyItFits}</p></div>
				<div class="dialog-actions"><Dialog.Close class="button button-outline">Back to results</Dialog.Close><a class="button button-lime" href="/evaluation">Review profile <span aria-hidden="true">↗</span></a></div>
			{/if}
		</Dialog.Content>
	</Dialog.Portal>
</Dialog.Root>

<style>
	:global(:root) { --bg: #030303; --surface: #071020; --lime: #bcff63; --blue: #2563eb; --text: #f7f9fb; --muted: #91a0b4; --line: rgba(37,99,235,.62); --line-soft: rgba(255,255,255,.14); }
	.library-page { min-height: 100vh; padding: clamp(2.2rem, 5vw, 4.8rem) 0 3.5rem; background: var(--bg); color: var(--text); }
	.library-shell { width: min(1380px, calc(100% - 3rem)); margin: 0 auto; }
	.eyebrow-row, .library-intro, .library-controls, .card-topline, .results-row, .library-footer, .dialog-head, .dialog-actions { display: flex; align-items: center; justify-content: space-between; gap: 1.2rem; }
	.eyebrow-row { border-bottom: 1px solid var(--line-soft); padding-bottom: 1.2rem; }
	.eyebrow, .kicker { margin: 0; color: var(--lime); font-size: .68rem; font-weight: 760; letter-spacing: .16em; text-transform: uppercase; }
	.step-pill { border: 1px solid var(--blue); border-radius: .2rem; color: var(--lime); font-size: .72rem; font-weight: 760; letter-spacing: .1em; padding: .85rem 1.3rem; text-transform: uppercase; }
	.library-intro { align-items: end; padding: clamp(2rem, 5vw, 4.5rem) 0 2.4rem; }
	h1, h2, h3, p { margin-top: 0; }
	h1 { max-width: 780px; margin-bottom: 1rem; font-size: clamp(2.8rem, 7vw, 6.1rem); font-weight: 760; letter-spacing: -.07em; line-height: .9; text-transform: uppercase; }
	h2 { margin: .35rem 0 0; font-size: clamp(1.25rem, 2vw, 2rem); letter-spacing: -.05em; text-transform: uppercase; }
	h3 { color: var(--blue); font-size: .76rem; letter-spacing: .1em; text-transform: uppercase; }
	.intro-copy { max-width: 720px; margin: 0; color: var(--muted); font-size: clamp(1rem, 1.6vw, 1.28rem); line-height: 1.5; }
	.back-link, .library-footer a { color: var(--blue); font-size: .76rem; font-weight: 760; letter-spacing: .08em; text-decoration: none; text-transform: uppercase; }
	.library-controls { align-items: stretch; gap: .85rem; margin-bottom: 1rem; }
	.search-field { display: flex; align-items: center; flex: 1; min-height: 3.3rem; border: 1px solid var(--blue); background: transparent; }
	.search-field span { width: 2.5rem; color: var(--text); font-size: 1.45rem; text-align: center; }
	.search-field input { width: 100%; border: 0; outline: 0; background: transparent; color: var(--text); font: inherit; font-size: 1.08rem; }
	.search-field input::placeholder { color: rgba(247,249,251,.58); }
	.category-filter { display: flex; flex-wrap: wrap; justify-content: end; gap: .4rem; }
	.category-filter button { min-height: 3.3rem; padding: .55rem 1rem; border: 1px solid var(--line); background: transparent; color: var(--text); cursor: pointer; font-size: .7rem; font-weight: 760; letter-spacing: .06em; text-transform: uppercase; transition: border-color .2s ease, background .2s ease, color .2s ease; }
	.category-filter button:hover, .category-filter button.active { border-color: var(--lime); background: var(--lime); color: #061006; }
	.recommended-grid { display: grid; grid-template-columns: minmax(0, 1.45fr) minmax(15rem, .55fr); gap: .75rem; }
	.recommended-card, .experience-card { overflow: hidden; padding: 0; border: 1px solid var(--line); background: var(--surface); color: var(--text); text-align: left; cursor: pointer; transition: border-color .25s ease, transform .25s ease, box-shadow .25s ease; }
	.recommended-card { display: grid; grid-template-columns: minmax(0, 1.12fr) minmax(0, .88fr); min-height: 26rem; border: 2px solid var(--lime); }
	.recommended-card:hover, .experience-card:hover, .recommended-card:focus-visible, .experience-card:focus-visible { border-color: var(--lime); box-shadow: 0 1rem 3rem rgba(0,0,0,.26); transform: translateY(-3px); }
	.recommended-image, .experience-image { position: relative; overflow: hidden; min-height: 100%; background: #091526; }
	.recommended-image img, .experience-image img, .dialog-image img { display: block; width: 100%; height: 100%; object-fit: cover; object-position: center; filter: saturate(.9); transition: transform .45s ease; }
	.recommended-card:hover img, .experience-card:hover img { transform: scale(1.04); }
	.recommended-image::after, .experience-image::after { content: ''; position: absolute; inset: 0; background: linear-gradient(90deg, transparent 40%, rgba(3,3,3,.28)); pointer-events: none; }
	.recommended-content { display: flex; flex-direction: column; padding: clamp(1.2rem, 3vw, 2.2rem); background: linear-gradient(140deg, rgba(7,16,32,.88), rgba(3,3,3,.72)); }
	.card-topline { align-items: start; }
	.status, .category-label, .duration-small { font-size: .66rem; font-weight: 780; letter-spacing: .08em; text-transform: uppercase; }
	.status { color: var(--lime); }
	.status i { display: inline-block; width: .45rem; height: .45rem; margin-left: .3rem; border-radius: 50%; background: currentColor; vertical-align: .05rem; }
	.status.locked { color: var(--blue); }
	.category-label, .duration-small { color: var(--muted); }
	.recommended-content .kicker { margin-top: auto; }
	.recommended-content h2 { margin: .55rem 0 1rem; font-size: clamp(2rem, 5vw, 4rem); color: var(--text); }
	.duration { display: flex; align-items: center; gap: .55rem; margin-bottom: 1rem; font-size: .76rem; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; }
	.duration span { color: var(--lime); font-size: 1.05rem; }
	.card-description { max-width: 25rem; margin-bottom: 1.4rem; color: rgba(247,249,251,.86); font-size: .91rem; line-height: 1.5; }
	.observed { padding-top: 1rem; border-top: 1px solid var(--line-soft); }
	.observed > span { color: var(--lime); font-size: .65rem; font-weight: 800; letter-spacing: .12em; text-transform: uppercase; }
	.observed div { display: flex; flex-wrap: wrap; gap: .4rem; margin-top: .65rem; }
	.observed b { padding: .4rem .55rem; border: 1px solid rgba(188,255,99,.35); color: var(--text); font-size: .63rem; font-weight: 600; }
	.card-action { display: inline-flex; align-items: center; gap: .45rem; margin-top: auto; color: var(--blue); font-size: .72rem; font-weight: 800; letter-spacing: .1em; text-transform: uppercase; }
	.card-action span { font-size: 1.05rem; }
	.target-note { display: flex; align-items: start; gap: .9rem; padding: 1.4rem; border: 1px solid var(--line); background: radial-gradient(circle at 0 0, rgba(37,99,235,.14), transparent 50%), var(--surface); }
	.note-icon { display: grid; place-items: center; width: 2rem; height: 2rem; flex: none; border: 1px solid var(--blue); border-radius: 50%; color: var(--blue); font-weight: 800; }
	.target-note strong, .target-note p { display: block; }
	.target-note strong { margin: .6rem 0; font-size: .9rem; text-transform: uppercase; }
	.target-note p:not(.kicker) { color: var(--muted); font-size: .78rem; line-height: 1.5; }
	.results-row { margin: 1.7rem 0 .75rem; }
	.results-row p { margin: 0; color: var(--muted); font-size: .75rem; letter-spacing: .06em; text-transform: uppercase; }
	.results-row strong { color: var(--lime); }
	.text-button { border: 0; background: transparent; color: var(--blue); cursor: pointer; font-size: .7rem; font-weight: 780; letter-spacing: .08em; text-transform: uppercase; }
	.experience-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: .75rem; }
	.experience-card { display: grid; grid-template-columns: minmax(10rem, .8fr) minmax(0, 1.2fr); min-height: 14rem; }
	.experience-image { min-height: 14rem; }
	.overlay-status { position: absolute; z-index: 1; top: .85rem; right: .85rem; }
	.experience-body { display: flex; flex-direction: column; padding: 1.2rem 1.25rem; }
	.experience-body h2 { margin: .8rem 0 .6rem; font-size: clamp(1.05rem, 2vw, 1.45rem); }
	.experience-body p { margin-bottom: 1rem; color: var(--muted); font-size: .8rem; line-height: 1.45; }
	.experience-body .card-action { margin-top: auto; }
	.empty-results { display: flex; align-items: center; gap: 1.2rem; padding: 2rem; border: 1px dashed var(--line); background: var(--surface); }
	.empty-symbol { display: grid; place-items: center; width: 3rem; height: 3rem; border: 1px solid var(--lime); border-radius: 50%; color: var(--lime); font-size: 1.4rem; }
	.empty-results h2 { font-size: 1.2rem; }
	.empty-results p:not(.kicker) { margin-bottom: 0; color: var(--muted); font-size: .8rem; }
	.empty-results .button { margin-left: auto; }
	.button { display: inline-flex; align-items: center; justify-content: center; gap: .7rem; min-height: 3.2rem; padding: .85rem 1.1rem; border: 1px solid var(--line); border-radius: .2rem; color: var(--text); font-size: .72rem; font-weight: 760; letter-spacing: .07em; text-decoration: none; text-transform: uppercase; cursor: pointer; }
	.button-lime { border-color: var(--lime); background: var(--lime); color: #061006; }
	.button-outline { background: transparent; }
	.library-footer { justify-content: space-between; margin-top: 2rem; padding-top: 1.3rem; border-top: 1px solid var(--line-soft); }
	.library-footer span { color: var(--muted); font-size: .7rem; }
	:global(.dialog-overlay) { position: fixed; inset: 0; z-index: 50; background: rgba(0,0,0,.8); }
	:global(.experience-dialog) { position: fixed; z-index: 51; top: 50%; left: 50%; width: min(760px, calc(100% - 2rem)); max-height: min(90vh, 860px); overflow: auto; transform: translate(-50%, -50%); padding: clamp(1.15rem, 3vw, 2rem); border: 1px solid var(--lime); background: #071020; box-shadow: 0 2rem 6rem rgba(0,0,0,.58); }
	.dialog-head { align-items: start; }
	:global(.dialog-head h2) { margin: .45rem 0 .6rem; font-size: clamp(2rem, 5vw, 3.7rem); }
	.dialog-head p:not(.kicker) { max-width: 35rem; margin-bottom: 0; color: var(--muted); font-size: .88rem; line-height: 1.5; }
	:global(.dialog-close) { width: 2.4rem; height: 2.4rem; flex: none; border: 1px solid var(--line); background: transparent; color: var(--text); cursor: pointer; font-size: 1.4rem; }
	.dialog-image { height: 15rem; margin: 1.3rem 0 .85rem; overflow: hidden; border: 1px solid var(--line-soft); }
	.dialog-meta { display: flex; flex-wrap: wrap; gap: .45rem; }
	.dialog-meta span { padding: .45rem .6rem; border: 1px solid var(--line-soft); color: var(--muted); font-size: .66rem; font-weight: 700; letter-spacing: .06em; text-transform: uppercase; }
	.dialog-columns { display: grid; grid-template-columns: 1fr 1fr; gap: .75rem; margin-top: 1.3rem; }
	.dialog-columns > div { padding: 1rem; border: 1px solid var(--line-soft); }
	.dialog-columns h3 { margin-bottom: .65rem; }
	.dialog-columns ul { margin: 0; padding-left: 1.1rem; color: var(--text); }
	.dialog-columns li { margin: .6rem 0; font-size: .78rem; line-height: 1.4; }
	.dialog-fit { margin-top: .75rem; padding: 1rem; border-left: 2px solid var(--lime); background: rgba(188,255,99,.06); }
	.dialog-fit p:not(.kicker) { margin: .45rem 0 0; color: var(--muted); font-size: .78rem; line-height: 1.5; }
	.dialog-actions { justify-content: end; margin-top: 1.2rem; }
	@media (max-width: 1000px) { .library-controls { align-items: stretch; flex-direction: column; } .category-filter { justify-content: start; } }
	@media (max-width: 780px) { .library-intro { align-items: start; flex-direction: column; } .recommended-grid { grid-template-columns: 1fr; } .recommended-card { min-height: 22rem; } .experience-card { grid-template-columns: minmax(8rem, .7fr) minmax(0, 1.3fr); } }
	@media (max-width: 580px) { .library-shell { width: min(100% - 1.4rem, 1380px); } .eyebrow-row, .library-footer { align-items: start; flex-direction: column; } h1 { font-size: clamp(2.7rem, 14vw, 4.4rem); } .recommended-card, .experience-card { grid-template-columns: 1fr; } .recommended-image { min-height: 13rem; } .experience-image { min-height: 10rem; } .experience-grid { grid-template-columns: 1fr; } .empty-results { align-items: start; flex-direction: column; } .empty-results .button { margin-left: 0; width: 100%; } .dialog-columns { grid-template-columns: 1fr; } .dialog-actions { align-items: stretch; flex-direction: column; } .dialog-actions .button { width: 100%; } }
	@media (prefers-reduced-motion: reduce) { .recommended-card, .experience-card, .recommended-image img, .experience-image img, .category-filter button { transition: none; } }
</style>
