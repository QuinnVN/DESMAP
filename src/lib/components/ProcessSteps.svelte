<script lang="ts">
	const steps = [
		{ number: '01', title: 'Interest', copy: 'Choose the career you want to explore.', icon: '/desmap/landing-02.png' },
		{ number: '02', title: 'DESMAP test', copy: 'Discover your traits and strengths.', icon: '/desmap/landing-03.png' },
		{ number: '03', title: 'VR experience', copy: 'Experience real career simulations.', icon: '/desmap/landing-04.png' },
		{ number: '04', title: 'Final profile', copy: 'Receive your results and development roadmap.', icon: '/desmap/landing-05.png' }
	];
</script>

<section class="process section-pad" id="how-it-works" aria-labelledby="process-heading">
	<div class="container">
		<div class="section-kicker"><span>How it works</span><span>From self-discovery to real experience</span></div>
		<div class="process-heading-row"><h2 id="process-heading">A clearer route<br /><span>to your next move.</span></h2><p>Four focused steps turn questions about your future into a profile you can act on.</p></div>
		<div class="steps-grid">
			{#each steps as step, index (step.number)}
				<article class="step-card" style={`--step-delay: ${index * 90}ms`}>
					<div class="step-top"><span class="step-number">{step.number}</span><span class="step-arrow" aria-hidden="true">↗</span></div>
					<div class="step-image"><img src={step.icon} alt="" loading="lazy" /></div>
					<h3>{step.title}</h3><p>{step.copy}</p>
				</article>
			{/each}
		</div>
	</div>
</section>

<style>
	.process { position: relative; overflow: hidden; }.process::before { content: ''; position: absolute; inset: 20% 0 auto; height: 18rem; background: radial-gradient(ellipse at 48% 40%, rgba(0,95,255,.11), transparent 65%); pointer-events: none; }.section-kicker { display: flex; align-items: center; justify-content: space-between; padding-bottom: 1.4rem; border-bottom: 1px solid var(--line); color: var(--lime); font-family: var(--mono); font-size: .69rem; letter-spacing: .13em; text-transform: uppercase; }.section-kicker span:last-child { color: var(--muted); }.process-heading-row { display: flex; align-items: end; justify-content: space-between; gap: 2rem; padding-block: 3.1rem 3.7rem; }.process-heading-row h2 { max-width: 31rem; margin: 0; color: var(--text); font-size: clamp(2.5rem, 6vw, 5.2rem); font-weight: 430; line-height: .9; letter-spacing: -.08em; text-transform: uppercase; }.process-heading-row h2 span { color: var(--lime); }.process-heading-row p { max-width: 18rem; margin: 0; color: var(--muted); line-height: 1.5; }.steps-grid { display: grid; grid-template-columns: repeat(4, 1fr); border-top: 1px solid var(--line); }.step-card { min-height: 25rem; padding: 1.4rem 1.2rem 0; border-right: 1px solid var(--line); animation: rise-in .8s both; animation-delay: var(--step-delay); }.step-card:first-child { border-left: 1px solid var(--line); }.step-top { display: flex; align-items: center; justify-content: space-between; }.step-number { color: var(--lime); font-size: 3.1rem; font-weight: 360; line-height: 1; letter-spacing: -.09em; }.step-arrow { color: var(--blue); font-size: 1.3rem; }.step-image { width: 5rem; height: 5rem; margin: 3rem auto 2.3rem; padding: .75rem; border: 1px solid var(--blue); border-radius: 50%; background: #050c1d; box-shadow: 0 0 1.1rem rgba(7,97,255,.56), inset 0 0 .8rem rgba(32,97,255,.4); }.step-image img { width: 100%; height: 100%; object-fit: contain; filter: drop-shadow(0 0 .4rem #3a87ff); }.step-card h3 { margin: 0; font-family: var(--mono); font-size: .9rem; letter-spacing: .07em; text-transform: uppercase; }.step-card p { max-width: 12rem; margin: .8rem 0 0; color: var(--muted); font-size: .9rem; line-height: 1.45; }.step-card:hover .step-image { border-color: var(--lime); box-shadow: 0 0 1.6rem rgba(188,255,99,.4), inset 0 0 .8rem rgba(188,255,99,.2); }.step-card:hover .step-arrow { color: var(--lime); transform: translate(.2rem,-.2rem); }
	@keyframes rise-in { from { opacity: 0; transform: translateY(1.3rem); } to { opacity: 1; transform: translateY(0); } } @media (prefers-reduced-motion: reduce) { .step-card { animation: none; } }
	@media (max-width: 760px) { .section-kicker { flex-direction: column; align-items: flex-start; gap: .5rem; }.process-heading-row { display: block; padding-block: 2.3rem; }.process-heading-row p { margin-top: 1.3rem; }.steps-grid { grid-template-columns: repeat(2, 1fr); }.step-card { min-height: 21rem; }.step-card:nth-child(2) { border-right: 0; }.step-card:nth-child(3) { border-top: 1px solid var(--line); }.step-card:nth-child(4) { border-top: 1px solid var(--line); border-right: 0; } }
	@media (max-width: 470px) { .step-card { padding-inline: .85rem; }.step-number { font-size: 2.4rem; }.step-image { margin-block: 2rem 1.8rem; width: 4.3rem; height: 4.3rem; }.step-card p { font-size: .8rem; } }
</style>
