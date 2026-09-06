<script lang="ts">
	let { compact = false } = $props<{ compact?: boolean }>();
</script>

<a class="brand" href="/" aria-label="DESMAP home">
	<svg class="brand-mark" viewBox="0 0 50 50" aria-hidden="true">
		<path d="M4 4h17.5C34 4 44 14 44 25S34 46 21.5 46H4V4Z" fill="none" stroke="currentColor" stroke-width="4" />
		<path d="M4 4v42M4 14h17c6.2 0 11 4.9 11 11s-4.8 11-11 11H4" fill="none" stroke="var(--blue)" stroke-width="5" />
		<circle cx="23" cy="25" r="5.2" fill="var(--blue)" />
	</svg>
	{#if !compact}<span class="brand-name">DESMAP</span>{/if}
</a>

<style>
	.brand { display: inline-flex; align-items: center; gap: .7rem; color: var(--text); text-decoration: none; }
	.brand-mark { width: 2.45rem; height: 2.45rem; flex: none; }
	.brand-name { font-size: 1.2rem; font-weight: 760; letter-spacing: -.04em; }
	@media (max-width: 640px) { .brand-name { font-size: 1.05rem; } .brand-mark { width: 2.15rem; height: 2.15rem; } }
</style>
