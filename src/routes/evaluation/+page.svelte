<script lang="ts">
	import { onMount } from 'svelte';
	import {
		answeredCount,
		createEvaluationModel,
		formatDate,
		type EvaluationModel
	} from '$lib/evaluation';
	import {
		readCompletionPayload,
		readSavedQuestionnaire,
		type QuestionnaireDraft,
		type QuestionnaireSubmission,
		type StageId
	} from '$lib/questionnaire';

	type Tab = 'profile' | 'report';

	const stageOrder: StageId[] = ['D', 'E', 'S', 'M', 'A', 'P'];
	let activeTab = $state<Tab>('profile');
	let payload = $state<QuestionnaireSubmission | null>(null);
	let draft = $state<QuestionnaireDraft | null>(null);
	let copied = $state(false);
	let downloadNotice = $state('');
	let showSample = $state(false);

	const model = $derived(createEvaluationModel(payload));
	const completed = $derived(Boolean(payload));
	const visibleModel = $derived(model);

	onMount(() => {
		payload = readCompletionPayload();
		draft = readSavedQuestionnaire();
	});

	function pointFor(index: number, value: number, radius = 108) {
		const angle = -Math.PI / 2 + (Math.PI * 2 * index) / stageOrder.length;
		return {
			x: 160 + Math.cos(angle) * radius * (value / 100),
			y: 145 + Math.sin(angle) * radius * (value / 100)
		};
	}

	function radarPoints(values: EvaluationModel['stageScores'], radius = 108): string {
		return stageOrder.map((stage, index) => {
			const point = pointFor(index, values[stage], radius);
			return `${point.x},${point.y}`;
		}).join(' ');
	}

	function framePoints(level: number): string {
		return radarPoints({ D: level, E: level, S: level, M: level, A: level, P: level }, 108);
	}

	function saveJson() {
		const exportModel = createEvaluationModel(payload);
		const data = {
			format: 'DESMAP frontend results preview',
			label: completed ? 'Self-assessment summary; career matches are illustrative until AI is connected.' : 'Sample report preview; no questionnaire has been completed.',
			questionnaire: payload,
			evaluation: exportModel
		};
		const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
		const url = URL.createObjectURL(blob);
		const link = document.createElement('a');
		link.href = url;
		link.download = `desmap-${completed ? 'results' : 'sample-preview'}.json`;
		link.click();
		URL.revokeObjectURL(url);
		downloadNotice = 'JSON saved locally.';
		window.setTimeout(() => (downloadNotice = ''), 2400);
	}

	function printReport() {
		window.print();
	}

	async function copySummary() {
		const text = `${visibleModel.targetCareer} · ${visibleModel.careerMatches[0]?.percent ?? 0}% illustrative match\n${visibleModel.strengths.join(' · ')}`;
		try {
			await navigator.clipboard.writeText(text);
			copied = true;
			window.setTimeout(() => (copied = false), 2400);
		} catch {
			downloadNotice = 'Copy is unavailable in this browser.';
			window.setTimeout(() => (downloadNotice = ''), 2400);
		}
	}

	function exploreTarget() {
		window.location.href = `/experiences?career=${visibleModel.targetExperienceSlug}`;
	}

	function profileTabLabel() {
		return completed ? 'Initial profile' : showSample ? 'Sample profile' : 'Your profile';
	}
</script>

<svelte:head>
	<title>My profile | DESMAP</title>
	<meta name="description" content="Review your DESMAP self-assessment and explore a clearly labelled sample career report." />
</svelte:head>

<main class="evaluation-page">
	<div class="page-shell">
		<div class="eyebrow-row">
			<p class="eyebrow">02 / DESMAP TEST</p>
			<span class="step-pill">PROFILE REVIEW</span>
		</div>

		<section class="page-intro">
			<div>
				<p class="kicker">{completed ? 'Initial DESMAP results' : 'Your profile is waiting'}</p>
				<h1>{completed ? 'See the pattern in your answers.' : 'Complete the questionnaire to unlock your profile.'}</h1>
				<p class="intro-copy">
					{completed
						? 'This is your self-assessment snapshot. Career matches below are an illustrative frontend preview until an AI evaluation service is connected.'
						: 'We keep the page useful while you are away. Resume your saved answers, or open the sample report to review the designed experience.'}
				</p>
			</div>
			<div class="intro-aside">
				<span class="signal-dot"></span>
				<div>
					<strong>{completed ? 'Saved locally' : draft ? `${answeredCount(draft)} answers saved` : 'No saved answers'}</strong>
					<small>{completed ? formatDate(payload?.completedAt) : draft ? 'Resume when you are ready' : 'Your results will stay on this device'}</small>
				</div>
			</div>
		</section>

			{#if !completed}
			<section class="resume-panel" aria-label="Questionnaire progress">
			<div class="resume-copy">
				<span class="resume-index">01</span>
				<div>
					<p class="kicker">{draft ? 'Resume your questionnaire' : 'Start your self-assessment'}</p>
					<h2>{draft ? `${answeredCount(draft)} of 98 questions saved` : 'Your answers shape the first map.'}</h2>
					<p>{draft ? 'Your latest answers are stored only in this browser.' : 'Tell DESMAP what matters to you before we compare your profile with career scenarios.'}</p>
				</div>
			</div>
			<a class="button button-lime" href="/questionnaire">{draft ? 'Resume questionnaire' : 'Begin questionnaire'} <span aria-hidden="true">↗</span></a>
			</section>
			<div class="sample-callout">
				<div><span class="sample-tag">OPTIONAL SAMPLE</span><strong>Want to review the finished layout first?</strong><p>Open a clearly labelled demo profile. It is not a prediction about you.</p></div>
				<button class="text-button" type="button" onclick={() => (showSample = !showSample)}>{showSample ? 'Hide sample' : 'Preview sample'} <span aria-hidden="true">→</span></button>
			</div>
		{/if}

		<div class="report-tabs" role="tablist" aria-label="Profile views">
				<button role="tab" class:active={activeTab === 'profile'} type="button" onclick={() => (activeTab = 'profile')} aria-selected={activeTab === 'profile'}>{profileTabLabel()}</button>
				<button role="tab" class:active={activeTab === 'report'} type="button" onclick={() => (activeTab = 'report')} aria-selected={activeTab === 'report'}>AI evaluation <span>DEMO</span></button>
		</div>

		{#if activeTab === 'profile'}
			{#if !completed && !showSample}
				<section class="empty-profile" aria-label="No profile available yet">
					<div class="empty-symbol">+</div>
					<div><p class="kicker">No profile generated yet</p><h2>Answer a few questions and your map will appear here.</h2><p>Your saved answers never leave this browser in the frontend preview. When you finish, return here to see your own DESMAP dimensions.</p></div>
					<a class="button button-outline" href="/questionnaire">Go to questionnaire <span aria-hidden="true">↗</span></a>
				</section>
			{:else}
			<section class="profile-grid" aria-label={completed ? 'Initial career profile' : 'Sample career profile'}>
				<div class="panel profile-panel">
					<div class="panel-heading"><div><p class="kicker">01 / Who am I?</p><h2>Personal profile</h2></div><span class="panel-icon blue">◎</span></div>
					<div class="radar-wrap">
						<svg class="radar" viewBox="0 0 320 290" role="img" aria-label="DESMAP dimension radar">
							{#each [20, 40, 60, 80, 100] as level (level)}
								<polygon points={framePoints(level)} fill="none" stroke="rgba(255,255,255,.14)" stroke-width="1" />
							{/each}
							{#each stageOrder as stage, index (stage)}
								{@const axis = pointFor(index, 100, 108)}
								<line x1="160" y1="145" x2={axis.x} y2={axis.y} stroke="rgba(255,255,255,.14)" stroke-width="1" />
								{@const label = pointFor(index, 122, 108)}
								<text x={label.x} y={label.y} text-anchor="middle" dominant-baseline="middle">{stage}</text>
							{/each}
							<polygon points={radarPoints(visibleModel.stageScores)} fill="rgba(188,255,99,.21)" stroke="var(--lime)" stroke-width="2.5" />
							{#each stageOrder as stage, index (stage)}
								{@const dot = pointFor(index, visibleModel.stageScores[stage], 108)}
								<circle cx={dot.x} cy={dot.y} r="4.5" fill="var(--lime)" />
							{/each}
						</svg>
						<div class="radar-note"><span class="legend-dot"></span><span>{completed ? 'Self-assessment score' : 'Illustrative sample score'}</span></div>
					</div>
					<div class="profile-list">
							{#each visibleModel.profileRows as row (row.code)}
							<div class="profile-row"><span class="profile-code">{row.code}</span><div><strong>{row.label}</strong><small>{row.detail}</small></div><b>{row.value}</b></div>
						{/each}
					</div>
				</div>

				<div class="profile-side">
					<div class="panel match-panel">
						<div class="panel-heading"><div><p class="kicker">02 / What fits me?</p><h2>Career match</h2></div><span class="panel-icon blue">⌁</span></div>
						<div class="demo-banner">ILLUSTRATIVE PREVIEW <span>AI evaluation is not connected</span></div>
							{#each visibleModel.careerMatches as match, index (match.label)}
							<div class="match-row"><span class="match-index">0{index + 1}</span><strong class:lime={match.accent === 'lime'}>{match.label}</strong><div class="match-track"><span style={`width: ${match.percent}%`}></span></div><b>{match.percent}%</b></div>
						{/each}
						<p class="panel-footnote">A real AI report will replace these demo values after the evaluation service is connected.</p>
					</div>

					<div class="panel target-panel">
						<div class="panel-heading compact"><div><p class="kicker">03 / Your next lens</p><h2>Development gaps</h2></div><span class="target-pill">TARGET: {visibleModel.targetCareer.toUpperCase()}</span></div>
						<div class="gap-list">
								{#each visibleModel.gaps as gap, index (gap)}
								<div class="gap-row"><span>0{index + 1}</span><div><strong>{gap}</strong><small>{index === 0 ? 'Current: developing' : index === 1 ? 'Current: moderate' : 'Current: good'}</small></div><i style={`--gap: ${24 - index * 4}%`}></i><b>GAP {24 - index * 4}%</b></div>
							{/each}
						</div>
					</div>
				</div>
			</section>

			<section class="bottom-rail">
			<div><span class="rail-icon">◎</span><div><strong>{completed ? 'Your profile shows a starting point.' : 'A sample profile shows the journey.'}</strong><p>{completed ? 'VR scenarios can add observed behaviour once they are connected.' : 'Use the questionnaire to replace this preview with your own answers.'}</p></div></div>
			<button class="button button-lime" type="button" onclick={exploreTarget}>Explore {visibleModel.targetCareer} <span aria-hidden="true">→</span></button>
			<div class="rail-actions"><button class="text-button" type="button" onclick={saveJson}>↓ Download JSON</button><button class="text-button" type="button" onclick={copySummary}>{copied ? 'Copied locally' : 'Copy summary'}</button></div>
			</section>
			{/if}
		{:else}
			<section class="final-report" aria-label="Sample AI evaluation report">
				<div class="report-topline"><div><p class="kicker">DEMO REPORT / 04</p><h2>Final career profile</h2><p class="report-disclaimer">This is a sample report layout. It is not an AI conclusion and does not include VR observations.</p></div><div class="fit-ring"><strong>84%</strong><span>DEMO FIT</span></div></div>
				<div class="report-columns">
					<div>
			<div class="panel observation-panel"><div class="panel-heading compact"><div><p class="kicker">Self-assessment vs. observed behaviour</p><h2>What would change after VR?</h2></div></div>{#each visibleModel.observations as observation (observation.self)}<div class="observation-row"><span>→</span><div><strong>{observation.self}</strong><small>{observation.observed}</small></div></div>{/each}</div>
			<div class="split-panel"><div><h3>Strengths</h3>{#each visibleModel.strengths as strength (strength)}<p>{strength}</p>{/each}</div><div><h3>Development gaps</h3>{#each visibleModel.gaps as gap (gap)}<p>{gap}</p>{/each}</div></div>
					</div>
					<div>
							<div class="panel roadmap-panel"><div class="panel-heading compact"><div><p class="kicker">Personal development</p><h2>Roadmap</h2></div></div>{#each visibleModel.roadmap as item, index (item)}<div class="roadmap-row"><span>0{index + 1}</span><strong>{item}</strong></div>{/each}<h3>Similar careers</h3><div class="similar-list"><span>Emergency medicine</span><span>Nursing</span><span>Clinical coordination</span></div></div>
					</div>
				</div>
				<div class="report-actions"><button class="button button-outline" type="button" onclick={saveJson}>↓ Download JSON</button><button class="button button-outline" type="button" onclick={printReport}>Print / save PDF</button><button class="button button-lime" type="button" onclick={copySummary}>{copied ? 'Copied locally' : 'Copy report summary'}</button></div>
			</section>
		{/if}

		{#if downloadNotice}<p class="notice" role="status">{downloadNotice}</p>{/if}
	</div>
</main>

<style>
	:global(:root) { --bg: #030303; --surface: #071020; --surface-raised: #0a1424; --lime: #bcff63; --blue: #2563eb; --text: #f7f9fb; --muted: #91a0b4; --line: rgba(37,99,235,.62); --line-soft: rgba(255,255,255,.14); }
	.evaluation-page { min-height: 100vh; background: var(--bg); color: var(--text); padding: clamp(2.2rem, 5vw, 5rem) 0 4rem; }
	.page-shell { width: min(1380px, calc(100% - 3rem)); margin: 0 auto; }
	.eyebrow-row, .page-intro, .panel-heading, .resume-panel, .sample-callout, .bottom-rail, .report-topline, .report-actions { display: flex; align-items: center; justify-content: space-between; gap: 1.5rem; }
	.eyebrow-row { border-bottom: 1px solid var(--line-soft); padding-bottom: 1.2rem; }
	.eyebrow, .kicker, .sample-tag { margin: 0; color: var(--lime); font-size: .68rem; font-weight: 760; letter-spacing: .16em; text-transform: uppercase; }
	.step-pill, .target-pill { border: 1px solid var(--lime); border-radius: 999px; color: var(--lime); font-size: .7rem; font-weight: 700; letter-spacing: .12em; padding: .65rem 1rem; text-transform: uppercase; }
	.page-intro { align-items: end; padding: clamp(2rem, 5vw, 4.7rem) 0 2.6rem; }
	h1, h2, h3, p { margin-top: 0; }
	h1 { max-width: 760px; margin-bottom: 1rem; font-size: clamp(2.6rem, 6vw, 5.5rem); font-weight: 760; letter-spacing: -.065em; line-height: .95; }
	h2 { margin: .35rem 0 0; font-size: clamp(1.25rem, 2vw, 1.85rem); letter-spacing: -.04em; text-transform: uppercase; }
	h3 { color: var(--blue); font-size: .83rem; letter-spacing: .12em; text-transform: uppercase; }
	.intro-copy { max-width: 760px; margin-bottom: 0; color: var(--muted); font-size: clamp(1rem, 1.6vw, 1.3rem); line-height: 1.55; }
	.intro-aside { display: flex; align-items: center; gap: .85rem; min-width: 11rem; padding-bottom: .3rem; }
	.intro-aside strong, .intro-aside small { display: block; }
	.intro-aside strong { font-size: .78rem; }
	.intro-aside small { color: var(--muted); font-size: .68rem; margin-top: .3rem; }
	.signal-dot, .legend-dot { width: .72rem; height: .72rem; display: inline-block; border-radius: 50%; background: var(--lime); box-shadow: 0 0 0 .3rem rgba(188,255,99,.12); }
	.resume-panel { align-items: center; padding: 1.35rem 1.6rem; border: 1px solid var(--line); background: linear-gradient(100deg, rgba(37,99,235,.14), rgba(7,16,32,.9)); }
	.resume-copy { display: flex; align-items: center; gap: 1rem; }
	.resume-index { color: var(--blue); font-family: var(--mono, monospace); font-size: .8rem; }
	.resume-copy h2 { font-size: 1.15rem; }
	.resume-copy p:not(.kicker) { margin: .4rem 0 0; color: var(--muted); font-size: .82rem; }
	.button { display: inline-flex; align-items: center; justify-content: center; gap: .7rem; min-height: 3.35rem; padding: .9rem 1.35rem; border: 1px solid var(--line); border-radius: .25rem; color: var(--text); font-size: .75rem; font-weight: 760; letter-spacing: .07em; text-decoration: none; text-transform: uppercase; cursor: pointer; transition: transform .2s ease, border-color .2s ease, background .2s ease; }
	.button:hover, .text-button:hover { transform: translateY(-2px); }
	.button-lime { border-color: var(--lime); background: var(--lime); color: #061006; }
	.button-outline { background: transparent; }
	.sample-callout { margin-top: 1rem; padding: 1rem 1.2rem; border: 1px dashed rgba(188,255,99,.45); background: rgba(188,255,99,.045); }
	.sample-callout strong, .sample-callout p { display: block; }
	.sample-callout strong { margin-top: .45rem; font-size: .85rem; }
	.sample-callout p { margin: .25rem 0 0; color: var(--muted); font-size: .78rem; }
	.text-button { border: 0; background: transparent; color: var(--blue); cursor: pointer; font-size: .74rem; font-weight: 760; letter-spacing: .04em; text-transform: uppercase; transition: transform .2s ease, color .2s ease; }
	.report-tabs { display: flex; gap: .35rem; margin: 2.1rem 0 1.2rem; border-bottom: 1px solid var(--line-soft); }
	.report-tabs button { position: relative; padding: .95rem 1.05rem 1.1rem; border: 0; background: transparent; color: var(--muted); cursor: pointer; font-size: .78rem; font-weight: 740; letter-spacing: .09em; text-transform: uppercase; }
	.report-tabs button::after { content: ''; position: absolute; right: 1rem; bottom: -1px; left: 1rem; height: 2px; transform: scaleX(0); background: var(--lime); transition: transform .2s ease; }
	.report-tabs button.active { color: var(--text); }
	.report-tabs button.active::after { transform: scaleX(1); }
	.report-tabs button span { margin-left: .35rem; color: var(--lime); font-size: .58rem; }
	.profile-grid { display: grid; grid-template-columns: minmax(0, 1.15fr) minmax(0, 1fr); gap: .75rem; }
	.empty-profile { display: grid; grid-template-columns: auto 1fr auto; align-items: center; gap: 1.25rem; padding: clamp(1.6rem, 4vw, 3.2rem); border: 1px solid var(--line); background: radial-gradient(circle at 15% 40%, rgba(37,99,235,.18), transparent 32%), var(--surface); }
	.empty-symbol { display: grid; place-items: center; width: 4rem; height: 4rem; border: 1px solid var(--lime); border-radius: 50%; color: var(--lime); font-size: 2rem; }
	.empty-profile h2 { max-width: 35rem; font-size: clamp(1.2rem, 2.6vw, 2rem); }
	.empty-profile p:not(.kicker) { max-width: 42rem; margin: .6rem 0 0; color: var(--muted); font-size: .84rem; line-height: 1.5; }
	.profile-side { display: grid; gap: .75rem; }
	.panel { border: 1px solid var(--line); background: radial-gradient(circle at 85% 12%, rgba(37,99,235,.1), transparent 30%), var(--surface); }
	.profile-panel { padding: 1.55rem; }
	.panel-heading { align-items: start; }
	.panel-heading.compact { align-items: center; }
	.panel-icon { display: grid; place-items: center; width: 2.55rem; height: 2.55rem; border: 1px solid currentColor; border-radius: 50%; font-size: 1.35rem; }
	.panel-icon.blue { color: var(--blue); }
	.radar-wrap { display: grid; place-items: center; padding: 1rem .5rem .35rem; }
	.radar { width: min(100%, 350px); overflow: visible; }
	.radar text { fill: var(--blue); font-family: var(--mono, monospace); font-size: 12px; font-weight: 800; }
	.radar-note { display: flex; align-items: center; gap: .55rem; color: var(--muted); font-size: .7rem; }
	.profile-list { margin-top: .8rem; border-top: 1px solid var(--line-soft); }
	.profile-row { display: grid; grid-template-columns: 2rem 1fr auto; align-items: center; gap: .8rem; padding: .77rem 0; border-bottom: 1px solid var(--line-soft); }
	.profile-code { display: grid; place-items: center; width: 1.8rem; height: 1.8rem; border: 1px solid var(--blue); border-radius: 50%; color: var(--blue); font-size: .7rem; font-weight: 800; }
	.profile-row strong, .profile-row small { display: block; }
	.profile-row strong { font-size: .77rem; letter-spacing: .06em; text-transform: uppercase; }
	.profile-row small { margin-top: .2rem; color: var(--muted); font-size: .72rem; }
	.profile-row b { color: var(--lime); font-size: 1.05rem; }
	.match-panel, .target-panel { padding: 1.45rem; }
	.demo-banner { display: flex; align-items: center; justify-content: space-between; gap: 1rem; margin: 1.2rem 0 .55rem; padding: .55rem .7rem; border: 1px solid rgba(188,255,99,.32); color: var(--lime); font-size: .63rem; font-weight: 800; letter-spacing: .1em; }
	.demo-banner span { color: var(--muted); font-weight: 520; letter-spacing: 0; text-align: right; }
	.match-row { display: grid; grid-template-columns: 2rem minmax(6.5rem, auto) 1fr 3.2rem; align-items: center; gap: .55rem; padding: .8rem 0; border-bottom: 1px solid var(--line-soft); }
	.match-index { color: var(--blue); font-family: var(--mono, monospace); font-weight: 800; }
	.match-row strong { font-size: .86rem; letter-spacing: .08em; text-transform: uppercase; }
	.match-row strong.lime { color: var(--lime); }
	.match-track { height: .45rem; background: rgba(37,99,235,.28); overflow: hidden; }
	.match-track span { display: block; height: 100%; background: var(--lime); }
	.match-row b { color: var(--lime); text-align: right; }
	.panel-footnote { margin: 1.1rem 0 0; color: var(--muted); font-size: .7rem; line-height: 1.45; }
	.target-pill { padding: .5rem .75rem; font-size: .57rem; }
	.gap-list { margin-top: 1rem; border-top: 1px solid var(--line-soft); }
	.gap-row { display: grid; grid-template-columns: 1.6rem minmax(0, 1fr) 6rem auto; align-items: center; gap: .7rem; padding: .72rem 0; border-bottom: 1px solid var(--line-soft); }
	.gap-row > span { color: var(--blue); font-family: var(--mono, monospace); font-size: .72rem; font-weight: 800; }
	.gap-row strong, .gap-row small { display: block; }
	.gap-row strong { font-size: .7rem; letter-spacing: .03em; text-transform: uppercase; }
	.gap-row small { margin-top: .22rem; color: var(--blue); font-size: .65rem; text-transform: uppercase; }
	.gap-row i { display: block; height: .35rem; background: linear-gradient(to right, var(--blue) var(--gap), rgba(37,99,235,.18) var(--gap)); }
	.gap-row b { color: var(--lime); font-size: .7rem; white-space: nowrap; }
	.bottom-rail { align-items: center; margin-top: .75rem; padding: 1.2rem 1.35rem; border: 1px solid var(--line); background: var(--surface); }
	.bottom-rail > div:first-child { display: flex; align-items: center; gap: .85rem; }
	.rail-icon { color: var(--lime); font-size: 1.9rem; }
	.bottom-rail strong, .bottom-rail p { display: block; }
	.bottom-rail strong { font-size: .82rem; letter-spacing: .08em; text-transform: uppercase; }
	.bottom-rail p { margin: .25rem 0 0; color: var(--lime); font-size: .75rem; }
	.rail-actions { display: flex; gap: 1rem; }
	.notice { position: fixed; right: 1rem; bottom: 1rem; z-index: 40; margin: 0; padding: .75rem 1rem; border: 1px solid var(--lime); background: #081306; color: var(--lime); font-size: .77rem; }
	.final-report { border: 1px solid var(--line); background: var(--surface); padding: clamp(1.3rem, 3vw, 2.2rem); }
	.report-topline { align-items: center; padding-bottom: 1.65rem; border-bottom: 1px solid var(--line-soft); }
	.report-topline h2 { font-size: clamp(1.8rem, 4vw, 3.2rem); }
	.report-disclaimer { max-width: 48rem; margin: .55rem 0 0; color: var(--muted); font-size: .82rem; line-height: 1.5; }
	.fit-ring { display: grid; place-items: center; width: 8rem; height: 8rem; flex: none; border: .7rem solid var(--lime); border-right-color: rgba(188,255,99,.18); border-radius: 50%; }
	.fit-ring strong { font-size: 1.5rem; }
	.fit-ring span { margin-top: -.35rem; color: var(--lime); font-size: .55rem; font-weight: 800; letter-spacing: .1em; }
	.report-columns { display: grid; grid-template-columns: minmax(0, 1.1fr) minmax(0, .9fr); gap: .75rem; margin-top: .75rem; }
	.observation-panel, .roadmap-panel { padding: 1.3rem; }
	.observation-row { display: grid; grid-template-columns: 2rem 1fr; gap: .75rem; padding: 1rem 0; border-bottom: 1px solid var(--line-soft); }
	.observation-row > span { color: var(--blue); font-size: 1.5rem; }
	.observation-row strong, .observation-row small { display: block; }
	.observation-row strong { font-size: .77rem; text-transform: uppercase; }
	.observation-row small { margin-top: .35rem; color: var(--muted); font-size: .75rem; line-height: 1.4; }
	.split-panel { display: grid; grid-template-columns: 1fr 1fr; margin-top: .75rem; border: 1px solid var(--line); background: var(--surface); }
	.split-panel > div { padding: 1.2rem; }
	.split-panel > div + div { border-left: 1px solid var(--line-soft); }
	.split-panel h3 { margin-bottom: 1rem; }
	.split-panel p { padding: .75rem 0; margin-bottom: 0; border-bottom: 1px solid var(--line-soft); font-size: .72rem; line-height: 1.4; text-transform: uppercase; }
	.roadmap-row { display: grid; grid-template-columns: 2rem 1fr; gap: .7rem; padding: 1rem 0; border-bottom: 1px solid var(--line-soft); }
	.roadmap-row span { color: var(--blue); font-family: var(--mono, monospace); font-weight: 800; }
	.roadmap-row strong { font-size: .77rem; text-transform: uppercase; }
	.roadmap-panel h3 { margin: 1.7rem 0 .8rem; }
	.similar-list { display: flex; flex-wrap: wrap; gap: .45rem; }
	.similar-list span { padding: .48rem .65rem; border: 1px solid var(--line-soft); color: var(--muted); font-size: .67rem; }
	.report-actions { justify-content: flex-end; margin-top: 1.4rem; }
	@media (max-width: 950px) { .page-intro { align-items: start; flex-direction: column; } .intro-aside { align-self: start; } .profile-grid, .report-columns { grid-template-columns: 1fr; } .bottom-rail { align-items: start; flex-wrap: wrap; } .rail-actions { margin-left: auto; } }
	@media (max-width: 640px) { .page-shell { width: min(100% - 1.4rem, 1380px); } .eyebrow-row, .resume-panel, .sample-callout, .bottom-rail, .report-topline, .empty-profile { align-items: start; flex-direction: column; } .empty-profile { display: flex; } h1 { font-size: clamp(2.5rem, 14vw, 4.2rem); } .step-pill { align-self: start; } .resume-panel .button, .bottom-rail > .button, .empty-profile .button { width: 100%; } .match-row { grid-template-columns: 1.6rem minmax(6rem, auto) 1fr 2.8rem; gap: .35rem; } .match-row strong { font-size: .72rem; } .gap-row { grid-template-columns: 1.4rem minmax(0, 1fr) auto; } .gap-row i { display: none; } .bottom-rail .rail-actions { margin-left: 0; flex-wrap: wrap; } .split-panel { grid-template-columns: 1fr; } .split-panel > div + div { border-top: 1px solid var(--line-soft); border-left: 0; } .report-actions { align-items: stretch; flex-direction: column; } .report-actions .button { width: 100%; } .demo-banner { align-items: start; flex-direction: column; gap: .4rem; } }
	@media (prefers-reduced-motion: reduce) { .button, .text-button, .report-tabs button::after { transition: none; } }
	@media print { :global(body) { background: white; } .evaluation-page { color: #101010; background: white; } .page-shell { width: 100%; } .eyebrow-row, .page-intro, .report-tabs, .report-actions, .notice { display: none; } .final-report, .panel, .split-panel { border-color: #777; background: white; } .report-disclaimer, .observation-row small, .similar-list span { color: #444; } }
</style>
