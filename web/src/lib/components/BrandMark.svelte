<script lang="ts">
	let { compact = false, condensed = false } = $props<{
		compact?: boolean;
		condensed?: boolean;
	}>();
</script>

<a class="inline-flex shrink-0 items-center no-underline" href="/" aria-label="Trang chủ DESMAP">
	<img
		class={[
			'w-auto transition-[height] duration-300 ease-out motion-reduce:transition-none',
			condensed ? 'h-[1.35rem] sm:h-[1.5rem]' : compact ? 'h-[1.65rem]' : 'h-[1.55rem] sm:h-[1.8rem]'
		]}
		src="/desmap/logo.svg"
		alt=""
	/>
</a>
