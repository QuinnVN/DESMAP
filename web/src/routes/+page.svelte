<script lang="ts">
	import { ArrowDown, CircleDot, Diamond, Eye, Headset } from '@lucide/svelte';
	import Header from '$lib/components/Header.svelte';
	import ProcessSteps from '$lib/components/ProcessSteps.svelte';
	import DimensionExplorer from '$lib/components/DimensionExplorer.svelte';
	import CareerPreview from '$lib/components/CareerPreview.svelte';
	import Footer from '$lib/components/Footer.svelte';
</script>

<svelte:head>
	<title>DESMAP — Trải nghiệm nghề nghiệp tiếp theo của bạn</title>
	<meta
		name="description"
		content="DESMAP giúp bạn hiểu điểm mạnh, khám phá nghề nghiệp thực tế và tự tin lựa chọn tương lai."
	/>
	<meta property="og:title" content="DESMAP — Trải nghiệm nghề nghiệp tiếp theo của bạn" />
	<meta
		property="og:description"
		content="Định hướng nghề nghiệp bằng AI và VR cho một tương lai mang dấu ấn của bạn."
	/>
</svelte:head>

<Header />

<main>
	<section class="relative overflow-hidden border-b border-line" aria-labelledby="hero-heading">
		<div
			class="pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_50%_46%,rgb(5_82_255_/.13),transparent_32%),radial-gradient(circle_at_50%_52%,rgb(159_64_255_/.08),transparent_46%)]"
			aria-hidden="true"
		></div>
		<div
			class="relative mx-auto flex min-h-[38rem] w-[min(100%_-_2rem,90rem)] items-center justify-center pt-[2rem] pb-16 min-[761px]:min-h-[45rem] min-[761px]:w-[min(100%_-_4rem,90rem)] min-[761px]:pt-[0rem] min-[761px]:pb-20"
		>
			<div class="relative z-2 flex w-full max-w-[58rem] flex-col items-center text-center">
				<p
					class="mb-[1.7rem] font-mono text-xs font-[740] tracking-[.13em] text-lime uppercase min-[761px]:mb-[2.4rem]"
				>
					ĐỊNH HƯỚNG NGHỀ NGHIỆP AI VÀ VR
				</p>
				<h1
					class="m-0 max-w-[58rem] text-[clamp(3rem,14vw,5.6rem)] leading-[1.2] font-[390] tracking-[-.085em] text-text uppercase min-[761px]:text-[clamp(3rem,7vw,6.7rem)]"
					id="hero-heading"
				>
					Đừng chỉ chọn.<br /><span class="text-lime">Hãy trải nghiệm</span>
				</h1>
				<p
					class="mt-8 mb-0 max-w-[38rem] text-[.96rem] leading-[1.55] text-muted min-[761px]:text-[1.06rem]"
				>
					DESMAP giúp bạn hiểu điểm mạnh, khám phá nghề nghiệp thực tế và tự tin lựa chọn tương lai.
				</p>
				<div class="mt-[2.4rem] flex justify-center">
					<a
						class="inline-flex min-h-[3.15rem] items-center justify-center gap-[.85rem] rounded-lg border border-transparent bg-lime px-[1.35rem] py-3 font-mono text-[.7rem] leading-none font-bold tracking-[.04em] text-[#050505] uppercase no-underline shadow-[0_0_1.2rem_rgb(188_255_99_/.15)] transition hover:-translate-y-0.5 hover:bg-[#d2ff96] hover:shadow-[0_0_1.6rem_rgb(188_255_99_/.32)]"
						href="#about"
					>
						Tìm hiểu thêm <span aria-hidden="true"><ArrowDown class="size-[1em]" /></span></a
					>
				</div>
			</div>
		</div>
		<div
			class="relative z-3 mx-auto grid w-[min(100%_-_2rem,90rem)] grid-cols-2 border-t border-line min-[761px]:w-[min(100%_-_4rem,90rem)] min-[761px]:grid-cols-4 [&_b]:font-sans [&_b]:text-[1.35rem] [&_b]:font-light [&_b]:text-lime min-[471px]:[&_b]:text-[1.8rem] [&>span]:flex [&>span]:min-h-[4.7rem] [&>span]:items-center [&>span]:gap-3 [&>span]:border-r [&>span]:border-line [&>span]:px-[.55rem] [&>span]:py-[.8rem] [&>span]:font-mono [&>span]:text-[.53rem] [&>span]:leading-[1.25] [&>span]:tracking-[.06em] [&>span]:text-muted [&>span]:uppercase min-[471px]:[&>span]:px-4 min-[471px]:[&>span]:text-[.64rem] min-[761px]:[&>span]:min-h-[5.2rem] [&>span:first-child]:border-l [&>span:nth-child(2)]:border-r-0 min-[761px]:[&>span:nth-child(2)]:border-r [&>span:nth-child(3)]:border-t min-[761px]:[&>span:nth-child(3)]:border-t-0 [&>span:nth-child(4)]:border-t min-[761px]:[&>span:nth-child(4)]:border-t-0"
			aria-label="Lợi ích DESMAP"
		>
			<span><b><CircleDot class="size-[1em]" aria-hidden="true" /></b> đánh giá<br />bằng AI</span
			><span
				><b><Headset class="size-[1em]" aria-hidden="true" /></b> mô phỏng VR<br />sống động</span
			><span
				><b><Eye class="size-[1em]" aria-hidden="true" /></b> góc nhìn nghề nghiệp<br />cá nhân hóa</span
			><span
				><b><Diamond class="size-[1em]" aria-hidden="true" /></b> quyết định sẵn sàng<br />cho tương
				lai</span
			>
		</div>
	</section>

	<DimensionExplorer />
	<ProcessSteps />
	<CareerPreview />
</main>

<Footer />
