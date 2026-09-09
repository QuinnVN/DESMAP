<script lang="ts">
	let { compact = false } = $props<{ compact?: boolean }>();
</script>

<a class="inline-flex items-center gap-[.7rem] text-text no-underline" href="/" aria-label="Trang chủ DESMAP">
	<svg class="h-[2.15rem] w-[2.15rem] shrink-0 sm:h-[2.45rem] sm:w-[2.45rem]" viewBox="0 0 50 50" aria-hidden="true">
		<path d="M4 4h17.5C34 4 44 14 44 25S34 46 21.5 46H4V4Z" fill="none" stroke="currentColor" stroke-width="4" />
		<path d="M4 4v42M4 14h17c6.2 0 11 4.9 11 11s-4.8 11-11 11H4" fill="none" stroke="var(--blue)" stroke-width="5" />
		<circle cx="23" cy="25" r="5.2" fill="var(--blue)" />
	</svg>
	{#if !compact}<span class="text-[1.05rem] font-[760] tracking-[-.04em] sm:text-[1.2rem]">DESMAP</span>{/if}
</a>
