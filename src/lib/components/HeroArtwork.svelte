<div class="artwork" aria-label="A blue virtual reality headset opening into a neon career portal" role="img">
	<div class="portal portal-back"></div><div class="portal portal-mid"></div><div class="portal portal-front"></div>
	<div class="portal-core"><span></span><span></span><span></span></div>
	<div class="grid-floor"></div>
	<div class="headset"><div class="headset-band"></div><div class="headset-body"><div class="lens"></div><div class="lens lens-right"></div></div><div class="headset-side"></div></div>
	<div class="career-card card-top"><b>✦</b><span>DOCTOR</span><i></i><i></i></div>
	<div class="career-card card-right"><b>⌁</b><span>TEACHER</span><i></i><i></i></div>
	<div class="career-card card-bottom"><b>⌁</b><span>LAWYER</span><i></i><i></i></div>
</div>

<style>
	.artwork { position: relative; width: min(100%, 48rem); aspect-ratio: 1.16; isolation: isolate; overflow: hidden; filter: drop-shadow(0 0 2rem rgba(11,84,255,.18)); }
	.artwork::before { content: ''; position: absolute; inset: 4% 0 0; background: radial-gradient(ellipse at 58% 48%, rgba(6,78,250,.2), transparent 39%), radial-gradient(ellipse at 53% 58%, rgba(177,62,255,.16), transparent 38%); z-index: -2; }
	.portal { position: absolute; border: 2px solid #1282ff; border-radius: 50%; transform: rotateX(65deg) rotateZ(-17deg); box-shadow: 0 0 1rem #1568ff, 0 0 3rem rgba(0,70,255,.55), inset 0 0 1rem rgba(0,157,255,.95); }
	.portal-back { width: 56%; aspect-ratio: 1; top: 17%; left: 46%; opacity: .32; }
	.portal-mid { width: 44%; aspect-ratio: 1; top: 25%; left: 52%; border-color: #a949ff; box-shadow: 0 0 1.5rem #774dff, 0 0 2.3rem rgba(197,50,255,.5), inset 0 0 .8rem #276fff; animation: portal-float 4.5s ease-in-out infinite; }
	.portal-front { width: 35%; aspect-ratio: 1; top: 31%; left: 57%; border-color: #55d7ff; box-shadow: 0 0 1.5rem #2bd4ff, 0 0 3rem rgba(0,110,255,.8), inset 0 0 1.3rem #1f90ff; }
	.portal-core { position: absolute; width: 27%; aspect-ratio: 1; top: 36%; left: 61%; border-radius: 50%; background: radial-gradient(circle, #07136e 0 29%, #182cff 32% 38%, #00b3ff 42% 45%, #122085 52%, transparent 63%); box-shadow: 0 0 2rem #0869ff; animation: core-pulse 2.8s ease-in-out infinite; }
	.portal-core span { position: absolute; left: 48%; top: -1rem; width: 2px; height: 12rem; background: linear-gradient(transparent, #52d8ff, transparent); transform: rotate(18deg); transform-origin: 50% 100%; opacity: .72; }
	.portal-core span:nth-child(2) { transform: rotate(-33deg); opacity: .46; }.portal-core span:nth-child(3) { transform: rotate(82deg); opacity: .32; }
	.grid-floor { position: absolute; left: 37%; bottom: -14%; width: 69%; height: 43%; background: repeating-linear-gradient(90deg, transparent 0 2rem, rgba(11,128,255,.65) 2.1rem 2.2rem), repeating-linear-gradient(0deg, transparent 0 1.8rem, rgba(49,161,255,.72) 1.9rem 2rem); transform: perspective(16rem) rotateX(53deg) rotateZ(-11deg); mask-image: linear-gradient(transparent, #000 25%, transparent); opacity: .78; }
	.headset { position: absolute; left: 8%; top: 31%; width: 50%; height: 34%; transform: rotate(-5deg); filter: drop-shadow(0 0 1.3rem #0a55ff); z-index: 2; }
	.headset-band { position: absolute; left: 27%; top: -48%; width: 53%; height: 89%; border: 1.4rem solid #2379ee; border-bottom: 0; border-radius: 52% 52% 0 0; transform: rotate(10deg); background: linear-gradient(90deg,#b3efff,#2473ef 44%,#0624af); }
	.headset-body { position: absolute; inset: 21% 2% 0 0; transform: skewX(-8deg); border-radius: 25% 23% 32% 21%; border: 1px solid #2fbcff; background: linear-gradient(145deg, #22a8ff 0%, #1044d3 24%, #031264 77%, #021354); box-shadow: inset 0 .5rem 1rem rgba(173,242,255,.5), inset -.9rem -1rem 2rem rgba(0,0,0,.6), 0 .9rem 1.5rem rgba(0,0,0,.48); }
	.headset-body::after { content: ''; position: absolute; right: -16%; top: 19%; width: 25%; height: 50%; border-radius: 0 50% 50% 0; background: linear-gradient(90deg,#145fe7,#55bfff,#0954cc); transform: skewX(6deg); }
	.lens { position: absolute; left: 12%; top: 35%; width: 19%; height: 13%; border-radius: 50%; border: .18rem solid #001b83; background: #092693; box-shadow: inset 0 0 .4rem #43b7ff; }.lens-right { left: auto; right: 17%; }
	.headset-side { position: absolute; right: -16%; top: 18%; width: 20%; height: 61%; border-radius: 35%; background: linear-gradient(135deg,#48c1ff,#0c42cd 56%,#031f9b); box-shadow: inset 0 0 .9rem rgba(160,239,255,.48); }
	.career-card { position: absolute; z-index: 4; display: grid; grid-template-columns: auto 1fr; align-items: center; column-gap: .65rem; width: 9rem; padding: .8rem .9rem; border: 1px solid #2280ff; border-radius: .75rem; background: rgba(7,46,142,.66); box-shadow: 0 0 1rem rgba(24,121,255,.75), inset 0 0 .8rem rgba(76,174,255,.18); color: #eff6ff; font-family: var(--mono); font-size: .7rem; letter-spacing: .08em; transform: rotate(-8deg); backdrop-filter: blur(4px); }.career-card b { grid-row: span 2; color: #8de8ff; font-size: 1.6rem; }.career-card i { height: .15rem; width: 70%; background: #4c8cff; opacity: .8; }.card-top { top: 8%; right: 1%; transform: rotate(-8deg); }.card-right { top: 52%; right: -3%; transform: rotate(8deg); }.card-bottom { bottom: 11%; left: 40%; transform: rotate(-15deg); }
	@keyframes portal-float { 50% { transform: rotateX(65deg) rotateZ(-17deg) translateY(-.5rem); } } @keyframes core-pulse { 50% { transform: scale(1.04); filter: brightness(1.2); } }
	@media (max-width: 700px) { .artwork { width: 120%; margin-left: -10%; } .career-card { transform: scale(.72) rotate(-8deg); transform-origin: center; }.card-right { transform: scale(.72) rotate(8deg); }.card-bottom { transform: scale(.72) rotate(-15deg); } }
	@media (prefers-reduced-motion: reduce) { .portal-mid, .portal-core { animation: none; } }
</style>
