<script lang="ts">
	import { Dialog } from 'bits-ui';
	import BrandMark from './BrandMark.svelte';

	let menuOpen = $state(false);
	const links = [
		{ label: 'Về chúng tôi', href: '/#about' },
		{ label: 'Cách hoạt động', href: '/#how-it-works' },
		{ label: 'Thư viện nghề nghiệp', href: '/experiences' },
		{ label: 'Hồ sơ của tôi', href: '/evaluation' }
	];

	function closeMenu() { menuOpen = false; }
</script>

<header class="relative z-30 border-b border-line bg-[rgb(3_3_3_/.91)] backdrop-blur-[18px]">
	<div class="mx-auto flex min-h-[5.15rem] w-[min(100%_-_2rem,90rem)] items-center gap-8 min-[761px]:min-h-26 min-[761px]:w-[min(100%_-_4rem,90rem)]">
		<BrandMark />
		<nav class="mx-auto hidden items-center justify-center gap-[clamp(1.3rem,3vw,4.1rem)] min-[761px]:flex max-[960px]:gap-[1.1rem]" aria-label="Điều hướng chính">
			{#each links as link (link.href)}
				<a class="group relative whitespace-nowrap text-[.81rem] font-[540] tracking-[.035em] text-text uppercase no-underline opacity-[.84] transition-[color,opacity] duration-250 after:absolute after:bottom-[-.45rem] after:left-0 after:h-px after:w-full after:origin-left after:scale-x-0 after:bg-lime after:transition-transform after:duration-250 hover:text-lime hover:opacity-100 hover:after:scale-x-100 focus-visible:text-lime focus-visible:opacity-100 focus-visible:after:scale-x-100 max-[960px]:text-[.7rem]" href={link.href}>{link.label}</a>
			{/each}
		</nav>
		<a class="hidden min-h-[3.15rem] min-w-40 items-center justify-center gap-[.85rem] rounded-full border border-lime px-6 py-[.85rem] font-mono text-[.7rem] leading-none font-medium tracking-[.04em] text-lime uppercase no-underline transition hover:-translate-y-0.5 hover:bg-lime hover:text-[#050505] min-[761px]:inline-flex max-[960px]:min-w-0 max-[960px]:px-[1.1rem] max-[960px]:text-[.71rem]" href="/questionnaire">Bắt đầu ngay <span class="text-[1.1em]" aria-hidden="true">↗</span></a>
		<Dialog.Root bind:open={menuOpen}>
			<Dialog.Trigger class="ml-auto block h-[2.9rem] w-[2.9rem] cursor-pointer rounded-full border border-line-strong bg-transparent text-text min-[761px]:hidden" aria-label="Mở menu" aria-expanded={menuOpen}>
				<span class="mx-auto my-[.28rem] block h-px w-4 bg-current"></span><span class="mx-auto my-[.28rem] block h-px w-4 bg-current"></span>
			</Dialog.Trigger>
			<Dialog.Portal>
				<Dialog.Overlay class="fixed inset-0 z-50 animate-overlay-in bg-black/76" />
				<Dialog.Content class="fixed inset-[.8rem] z-51 flex animate-menu-in flex-col rounded-[1.2rem] border border-line-strong bg-[#071020] p-5 shadow-[0_1.5rem_5rem_rgb(0_0_0_/.55)]" aria-label="Điều hướng trên thiết bị di động">
					<div class="flex items-center justify-between"><BrandMark compact /><Dialog.Close class="h-[2.4rem] w-[2.4rem] cursor-pointer rounded-full border border-line-strong bg-transparent text-2xl text-text" aria-label="Đóng menu">×</Dialog.Close></div>
					<nav class="mt-16 mb-auto grid" aria-label="Các liên kết điều hướng trên thiết bị di động">
						{#each links as link, index (link.href)}
							<a class="flex items-baseline gap-4 border-b border-line py-[1.2rem] text-[clamp(1.65rem,7vw,2.8rem)] font-[520] tracking-[-.055em] text-text uppercase no-underline" href={link.href} onclick={closeMenu}><span class="font-mono text-[.72rem] tracking-normal text-lime">0{index + 1}</span>{link.label}</a>
						{/each}
					</nav>
					<a class="mt-8 inline-flex min-h-[3.15rem] w-full items-center justify-between gap-[.85rem] rounded-lg border border-transparent bg-lime px-[1.35rem] py-3 font-mono text-[.7rem] leading-none font-medium tracking-[.04em] text-[#050505] uppercase no-underline shadow-[0_0_1.2rem_rgb(188_255_99_/.15)] transition hover:-translate-y-0.5 hover:bg-[#d2ff96] hover:shadow-[0_0_1.6rem_rgb(188_255_99_/.32)]" href="/questionnaire" onclick={closeMenu}>Bắt đầu ngay <span class="text-[1.1em]" aria-hidden="true">↗</span></a>
				</Dialog.Content>
			</Dialog.Portal>
		</Dialog.Root>
	</div>
</header>
