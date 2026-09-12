<script lang="ts">
let { compact = false } = $props<{ compact?: boolean }>();
</script>

<a
	class="inline-flex shrink-0 items-center no-underline"
	href="/"
	aria-label="Trang chủ DESMAP"
>
	<img
		class={compact
			? "h-[1.65rem] w-auto"
			: "h-[1.55rem] w-auto sm:h-[1.8rem]"}
		src="/desmap/logo.svg"
		alt=""
	>
</a>
