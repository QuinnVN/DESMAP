<script lang="ts">
	import { Dialog } from 'bits-ui';
	import BrandMark from './BrandMark.svelte';

	let menuOpen = $state(false);
	const links = [
		{ label: 'About us', href: '/#about' },
		{ label: 'How it works', href: '/#how-it-works' },
		{ label: 'Career library', href: '/experiences' },
		{ label: 'My profile', href: '/evaluation' }
	];

	function closeMenu() { menuOpen = false; }
</script>

<header class="site-header">
	<div class="container header-inner">
		<BrandMark />
		<nav class="desktop-nav" aria-label="Main navigation">
			{#each links as link (link.href)}
				<a href={link.href}>{link.label}</a>
			{/each}
		</nav>
		<a class="button button-outline header-cta" href="/questionnaire">Start now <span aria-hidden="true">↗</span></a>
		<Dialog.Root bind:open={menuOpen}>
			<Dialog.Trigger class="menu-trigger" aria-label="Open menu" aria-expanded={menuOpen}>
				<span></span><span></span>
			</Dialog.Trigger>
			<Dialog.Portal>
				<Dialog.Overlay class="dialog-overlay" />
				<Dialog.Content class="mobile-menu" aria-label="Mobile navigation">
					<div class="mobile-menu-top"><BrandMark compact /><Dialog.Close class="dialog-close" aria-label="Close menu">×</Dialog.Close></div>
					<nav aria-label="Mobile navigation links">
						{#each links as link, index (link.href)}
							<a href={link.href} onclick={closeMenu}><span>0{index + 1}</span>{link.label}</a>
						{/each}
					</nav>
					<a class="button button-lime mobile-menu-cta" href="/questionnaire" onclick={closeMenu}>Start now <span aria-hidden="true">↗</span></a>
				</Dialog.Content>
			</Dialog.Portal>
		</Dialog.Root>
	</div>
</header>

<style>
	.site-header { position: relative; z-index: 30; border-bottom: 1px solid var(--line); background: rgba(3, 3, 3, .91); backdrop-filter: blur(18px); }
	.header-inner { display: flex; align-items: center; min-height: 6.5rem; gap: 2rem; }
	.desktop-nav { display: flex; align-items: center; justify-content: center; gap: clamp(1.3rem, 3vw, 4.1rem); margin: 0 auto; }
	.desktop-nav a { position: relative; color: var(--text); text-decoration: none; font-size: .81rem; font-weight: 540; letter-spacing: .035em; text-transform: uppercase; white-space: nowrap; opacity: .84; transition: color .25s ease, opacity .25s ease; }
	.desktop-nav a::after { content: ''; position: absolute; left: 0; bottom: -.45rem; width: 100%; height: 1px; background: var(--lime); transform: scaleX(0); transform-origin: left; transition: transform .25s ease; }
	.desktop-nav a:hover, .desktop-nav a:focus-visible { color: var(--lime); opacity: 1; }
	.desktop-nav a:hover::after, .desktop-nav a:focus-visible::after { transform: scaleX(1); }
	.header-cta { padding: .85rem 1.6rem; min-width: 10rem; }
	.header-cta span, .mobile-menu-cta span { font-size: 1.1em; }
	:global(.menu-trigger) { display: none; margin-left: auto; width: 2.9rem; height: 2.9rem; border: 1px solid var(--line-strong); border-radius: 50%; background: transparent; color: var(--text); cursor: pointer; }
	:global(.menu-trigger span) { display: block; width: 1rem; height: 1px; margin: .28rem auto; background: currentColor; }
	:global(.dialog-overlay) { position: fixed; inset: 0; z-index: 50; background: rgba(0,0,0,.76); animation: overlay-in .2s ease-out; }
	:global(.mobile-menu) { position: fixed; z-index: 51; inset: .8rem; display: flex; flex-direction: column; padding: 1.25rem; border: 1px solid var(--line-strong); border-radius: 1.2rem; background: #071020; box-shadow: 0 1.5rem 5rem rgba(0,0,0,.55); animation: menu-in .3s cubic-bezier(.16,1,.3,1); }
	:global(.mobile-menu-top) { display: flex; align-items: center; justify-content: space-between; }
	:global(.dialog-close) { width: 2.4rem; height: 2.4rem; border: 1px solid var(--line-strong); border-radius: 50%; background: transparent; color: var(--text); font-size: 1.5rem; cursor: pointer; }
	:global(.mobile-menu nav) { display: grid; margin: 4rem 0 auto; }
	:global(.mobile-menu nav a) { display: flex; align-items: baseline; gap: 1rem; padding: 1.2rem 0; border-bottom: 1px solid var(--line); color: var(--text); font-size: clamp(1.65rem, 7vw, 2.8rem); font-weight: 520; letter-spacing: -.055em; text-decoration: none; text-transform: uppercase; }
	:global(.mobile-menu nav a span) { color: var(--lime); font-family: var(--mono); font-size: .72rem; letter-spacing: 0; }
	.mobile-menu-cta { width: 100%; justify-content: space-between; margin-top: 2rem; }
	@keyframes overlay-in { from { opacity: 0; } to { opacity: 1; } }
	@keyframes menu-in { from { opacity: 0; transform: translateY(-1.5rem); } to { opacity: 1; transform: translateY(0); } }
	@media (max-width: 960px) { .desktop-nav { gap: 1.1rem; } .desktop-nav a { font-size: .7rem; } .header-cta { min-width: auto; padding-inline: 1.1rem; font-size: .71rem; } }
	@media (max-width: 760px) { .header-inner { min-height: 5.15rem; } .desktop-nav, .header-cta { display: none; } :global(.menu-trigger) { display: block; } }
</style>
