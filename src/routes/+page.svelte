<script lang="ts">
	import Header from '$lib/components/Header.svelte';
	import ProcessSteps from '$lib/components/ProcessSteps.svelte';
	import DimensionExplorer from '$lib/components/DimensionExplorer.svelte';
	import CareerPreview from '$lib/components/CareerPreview.svelte';
	import Footer from '$lib/components/Footer.svelte';
</script>

<svelte:head>
	<title>DESMAP — Experience your next career</title>
	<meta name="description" content="DESMAP helps you understand your strengths, explore real careers, and choose your future with confidence." />
	<meta property="og:title" content="DESMAP — Experience your next career" />
	<meta property="og:description" content="AI × VR career orientation for a future that feels like yours." />
</svelte:head>

<Header />

<main>
	<section class="hero" aria-labelledby="hero-heading">
		<div class="hero-ambient" aria-hidden="true"></div>
		<div class="container hero-grid">
			<div class="hero-copy">
				<p class="eyebrow hero-eyebrow">AI × VR CAREER ORIENTATION</p>
				<h1 id="hero-heading">Don’t just<br />choose a career.<br /><span>Experience</span> it.</h1>
				<p class="hero-description">DESMAP helps you understand your strengths, explore real careers, choose your future with confidence.</p>
				<div class="hero-actions"><a class="button button-lime" href="/questionnaire">Build my profile <span aria-hidden="true">↗</span></a><a class="process-link" href="#how-it-works">Explore the process <span aria-hidden="true">→</span></a></div>
			</div>
			<div class="hero-art"><img class="hero-raster" src="/desmap/landing-01.png" alt="Blue virtual reality headset opening into a neon career portal" /></div>
		</div>
		<div class="container promise-strip" aria-label="DESMAP benefits"><span><b>◎</b> AI-powered<br />assessment</span><span><b>⌁</b> immersive VR<br />simulations</span><span><b>◈</b> personalized<br />career insights</span><span><b>◇</b> future-ready<br />decisions</span></div>
	</section>

	<ProcessSteps />
	<DimensionExplorer />
	<CareerPreview />
</main>

<Footer />

<style>
	.hero { position: relative; overflow: hidden; border-bottom: 1px solid var(--line); }.hero-ambient { position: absolute; inset: 0; background: radial-gradient(circle at 72% 47%, rgba(5,82,255,.16), transparent 25%), radial-gradient(circle at 83% 50%, rgba(159,64,255,.11), transparent 24%); pointer-events: none; }.hero-grid { position: relative; display: grid; min-height: 45rem; grid-template-columns: minmax(19rem, .92fr) minmax(30rem, 1.08fr); align-items: center; gap: 1rem; padding-block: 4.5rem 1rem; }.hero-copy { position: relative; z-index: 2; padding-top: 2rem; }.eyebrow { color: var(--lime); font-family: var(--mono); font-size: .75rem; font-weight: 740; letter-spacing: .13em; text-transform: uppercase; }.hero-eyebrow { margin: 0 0 2.4rem; }.hero h1 { max-width: 44rem; margin: 0; color: var(--text); font-size: clamp(3rem, 6.35vw, 6.7rem); font-weight: 390; letter-spacing: -.085em; line-height: .87; text-transform: uppercase; }.hero h1 span { color: var(--lime); }.hero-description { max-width: 33rem; margin: 2rem 0 0; color: var(--muted); font-size: 1.06rem; line-height: 1.45; }.hero-actions { display: flex; align-items: center; flex-wrap: wrap; gap: 1.5rem; margin-top: 2.4rem; }.process-link { display: inline-flex; align-items: center; gap: .7rem; padding-bottom: .5rem; border-bottom: 1px solid var(--text); color: var(--text); font-family: var(--mono); font-size: .72rem; letter-spacing: .05em; text-decoration: none; text-transform: uppercase; transition: color .25s ease, border-color .25s ease; }.process-link span { color: var(--lime); font-size: 1rem; transition: transform .25s ease; }.process-link:hover { border-color: var(--lime); color: var(--lime); }.process-link:hover span { transform: translateX(.25rem); }.hero-art { position: relative; display: grid; min-width: 0; place-items: center; min-height: 35rem; }.hero-raster { position: absolute; z-index: 1; width: 124%; max-width: none; aspect-ratio: 800 / 533; object-fit: contain; object-position: center; filter: drop-shadow(0 0 1.8rem rgba(17,95,255,.24)); }.promise-strip { position: relative; z-index: 3; display: grid; grid-template-columns: repeat(4, 1fr); border-top: 1px solid var(--line); }.promise-strip span { display: flex; align-items: center; gap: .75rem; min-height: 5.2rem; padding: .8rem 1rem; border-right: 1px solid var(--line); color: var(--muted); font-family: var(--mono); font-size: .64rem; letter-spacing: .06em; line-height: 1.25; text-transform: uppercase; }.promise-strip span:first-child { border-left: 1px solid var(--line); }.promise-strip b { color: var(--lime); font-family: var(--sans); font-size: 1.8rem; font-weight: 300; }
	@media (max-width: 1000px) { .hero-grid { min-height: 40rem; grid-template-columns: minmax(18rem, .8fr) minmax(23rem, 1.2fr); }.hero-raster { width: 140%; }.hero h1 { font-size: clamp(3.1rem, 6.8vw, 5.4rem); } }
	@media (max-width: 760px) { .hero-grid { display: flex; min-height: auto; flex-direction: column; align-items: stretch; padding-block: 4.2rem 0; }.hero-copy { padding-top: 0; }.hero-eyebrow { margin-bottom: 1.7rem; }.hero h1 { max-width: 24rem; font-size: clamp(3rem, 14vw, 5.6rem); }.hero-description { font-size: .96rem; }.hero-art { min-height: 22rem; margin-top: -1rem; }.hero-raster { width: 130%; }.promise-strip { grid-template-columns: repeat(2, 1fr); }.promise-strip span { min-height: 4.7rem; }.promise-strip span:nth-child(2) { border-right: 0; }.promise-strip span:nth-child(3), .promise-strip span:nth-child(4) { border-top: 1px solid var(--line); } }
	@media (max-width: 470px) { .hero-actions { align-items: flex-start; flex-direction: column; gap: 1.3rem; }.hero-art { min-height: 18rem; }.promise-strip span { padding-inline: .55rem; font-size: .53rem; }.promise-strip b { font-size: 1.35rem; } }
</style>
