<script lang="ts">
	import { ArrowUpRight, BadgeCheck, Copyright } from '@lucide/svelte';
</script>

<footer class="border-t border-line bg-[#020202] pt-20">
	<div
		class="mx-auto block w-[min(100%_-_2rem,90rem)] gap-8 pb-14 min-[641px]:flex min-[641px]:w-[min(100%_-_4rem,90rem)] min-[641px]:items-end min-[641px]:justify-between min-[641px]:pb-20"
	>
		<div>
			<a
				class="text-[clamp(3rem,8vw,7.4rem)] leading-[.9] font-[760] tracking-[-.1em] text-text no-underline"
				href="/"
				>DESMAP<BadgeCheck
					class="mt-2 ml-1 inline size-[.2em] align-top text-lime"
					aria-label="Thương hiệu đã đăng ký"
				/></a
			>
			<p class="mt-[1.4rem] mb-0 text-muted">Tìm hướng đi mang đậm dấu ấn của bạn.</p>
		</div>
		<a
			class="mt-[2.2rem] inline-flex min-h-[3.15rem] items-center justify-center gap-[.85rem] rounded-lg border border-transparent bg-lime px-[1.35rem] py-3 font-mono text-[.7rem] leading-none font-medium tracking-[.04em] text-[#050505] uppercase no-underline shadow-[0_0_1.2rem_rgb(188_255_99_/.15)] transition hover:-translate-y-0.5 hover:bg-[#d2ff96] hover:shadow-[0_0_1.6rem_rgb(188_255_99_/.32)] min-[641px]:mt-0"
			href="/questionnaire"
			>Bắt đầu ngay <span aria-hidden="true"><ArrowUpRight class="size-[1em]" /></span></a
		>
	</div>
	<div
		class="mx-auto flex w-[min(100%_-_2rem,90rem)] flex-wrap justify-between gap-6 border-t border-line py-[1.1rem] font-mono text-[.6rem] tracking-[.06em] text-muted uppercase min-[641px]:w-[min(100%_-_4rem,90rem)]"
	>
		<span class="inline-flex items-center gap-1"
			><Copyright class="size-3" aria-hidden="true" /> {new Date().getFullYear()} DESMAP</span
		>
		<span>Đồng hành cùng chặng đường tiếp theo.</span>
	</div>
</footer>
