<script lang="ts">
	import { onMount } from 'svelte';
	import { Dialog } from 'bits-ui';
	import { ArrowLeft, ArrowRight, ArrowUpRight, Clock3, Info, Search, X } from '@lucide/svelte';
	import { experiences, type CareerExperience, type ExperienceCategory } from '$lib/evaluation';

	type Filter = 'Tất cả' | ExperienceCategory;
	const filters: Filter[] = ['Tất cả', 'Y tế', 'Luật', 'Giáo dục', 'Kinh doanh', 'Kỹ thuật'];
	let query = $state('');
	let filter = $state<Filter>('Tất cả');
	let dialogOpen = $state(false);
	let selected = $state<CareerExperience | null>(null);
	let targetSlug = $state('doctor');

	const recommended = $derived(
		experiences.find((experience) => experience.slug === targetSlug) ?? experiences[0]
	);
	const visibleExperiences = $derived(
		experiences.filter((experience) => {
			const search = query.trim().toLowerCase();
			const matchesSearch =
				!search ||
				[
					experience.title,
					experience.category,
					experience.description,
					...experience.observedFactors
				]
					.join(' ')
					.toLowerCase()
					.includes(search);
			const matchesFilter = filter === 'Tất cả' || experience.category === filter;
			return matchesSearch && matchesFilter;
		})
	);
	const supportingExperiences = $derived(
		visibleExperiences.filter((experience) => experience.slug !== recommended.slug)
	);

	onMount(() => {
		const slug = new URLSearchParams(window.location.search).get('career');
		if (slug && experiences.some((experience) => experience.slug === slug)) targetSlug = slug;
	});

	function openDetails(experience: CareerExperience) {
		selected = experience;
		dialogOpen = true;
	}

	function clearFilters() {
		query = '';
		filter = 'Tất cả';
	}

	function statusLabel(status: CareerExperience['status']): string {
		return status === 'recommended'
			? 'Mục tiêu của bạn'
			: status === 'locked'
				? 'Sắp ra mắt'
				: 'Sẵn sàng xem';
	}
</script>

<svelte:head>
	<title>Thư viện nghề nghiệp | DESMAP</title>
	<meta
		name="description"
		content="Khám phá các tình huống nghề nghiệp DESMAP và xem những yếu tố mỗi tình huống được thiết kế để quan sát."
	/>
</svelte:head>

<main class="min-h-dvh bg-bg py-[clamp(2.2rem,5vw,4.8rem)] pb-14 text-[#f7f9fb]">
	<div
		class="mx-auto w-[min(1380px,calc(100%_-_3rem))] max-[640px]:w-[min(1380px,calc(100%_-_1.4rem))]"
	>
		<div
			class="flex items-center justify-between gap-5 border-b border-white/14 pb-5 max-[640px]:flex-col max-[640px]:items-start"
		>
			<p class="m-0 text-[.68rem] font-[760] tracking-[.16em] text-lime uppercase">
				03 / TRẢI NGHIỆM NGHỀ NGHIỆP VR
			</p>
			<span
				class="rounded-[.2rem] border border-blue px-5 py-[.85rem] text-[.72rem] font-[760] tracking-[.1em] text-lime uppercase"
				>{experiences.length} NGHỀ NGHIỆP KHẢ DỤNG</span
			>
		</div>
		<section
			class="flex items-end justify-between gap-5 py-[clamp(2rem,5vw,4.5rem)] pb-10 max-[950px]:flex-col max-[950px]:items-start"
		>
			<div>
				<p class="m-0 text-[.68rem] font-[760] tracking-[.16em] text-lime uppercase">
					Thư viện nghề nghiệp
				</p>
				<h1
					class="mt-[.35rem] mb-4 max-w-[780px] text-[clamp(2.8rem,7vw,6.1rem)] leading-[.9] font-[760] tracking-[-.07em] uppercase"
				>
					Chọn một nghề để trải nghiệm.
				</h1>
				<p class="m-0 max-w-[720px] text-[clamp(1rem,1.6vw,1.28rem)] leading-[1.5] text-[#91a0b4]">
					Bước vào môi trường làm việc mô phỏng và quan sát cách bạn phản ứng trong tình huống thực
					tế. Đọc chi tiết nhiệm vụ trước khi chọn nghề để so sánh.
				</p>
			</div>
			<a
				class="text-[.76rem] font-[760] tracking-[.08em] text-blue uppercase no-underline"
				href="/evaluation"><span aria-hidden="true"><ArrowLeft class="size-[1em]" /></span> Quay lại hồ sơ</a
			>
		</section>

		<section
			class="mb-4 flex items-stretch gap-[.85rem] max-[760px]:flex-col"
			aria-label="Bộ lọc thư viện nghề nghiệp"
		>
			<label class="flex min-h-[3.3rem] flex-1 items-center border border-blue bg-transparent"
				><span class="w-10 text-center text-[1.45rem]" aria-hidden="true"><Search class="inline size-[1em]" /></span>
				<input
					class="w-full border-0 bg-transparent text-[1.08rem] text-[#f7f9fb] outline-none placeholder:text-[#f7f9fb]/45"
					bind:value={query}
					type="search"
					placeholder="Tìm kiếm nghề nghiệp..."
					aria-label="Tìm kiếm nghề nghiệp"
				/></label
			>
			<div class="flex flex-wrap gap-2" role="group" aria-label="Lọc theo danh mục">
				{#each filters as item (item)}
					<button
						class={`cursor-pointer border px-[.85rem] py-[.72rem] text-[.68rem] font-bold tracking-[.08em] uppercase transition hover:border-lime hover:text-lime focus-visible:border-lime focus-visible:text-lime focus-visible:outline-none ${filter === item ? 'border-lime bg-lime/8 text-lime' : 'border-blue bg-transparent text-[#91a0b4]'}`}
						type="button"
						aria-pressed={filter === item}
						onclick={() => (filter = item)}
					>
						{item}
					</button>
				{/each}
			</div>
		</section>

		<section
			class="grid grid-cols-[minmax(0,1.35fr)_minmax(16rem,.65fr)] gap-3 max-[900px]:grid-cols-1"
			aria-label="Nghề nghiệp được đề xuất"
		>
			<button
				class="group grid cursor-pointer grid-cols-[minmax(13rem,.9fr)_minmax(18rem,1.1fr)] overflow-hidden border border-blue bg-[#071020] text-left text-[#f7f9fb] transition hover:-translate-y-1 hover:border-lime max-[700px]:grid-cols-1"
				type="button"
				onclick={() => openDetails(recommended)}
			>
				<div class="min-h-[24rem] overflow-hidden bg-[#061020] max-[700px]:min-h-56">
					<img
						class="h-full w-full object-cover transition duration-500 group-hover:scale-105"
						src={recommended.image}
						alt=""
					/>
				</div>
				<div class="flex flex-col p-[clamp(1.3rem,3vw,2.7rem)]">
					<div class="flex items-center justify-between gap-4">
						<span
							class="inline-flex items-center gap-2 rounded-full border border-lime px-3 py-1 text-[.63rem] font-bold tracking-[.08em] text-lime uppercase"
							>{statusLabel(recommended.status)}
							<i class="h-1.5 w-1.5 rounded-full bg-lime"></i></span
						><span class="text-[.7rem] font-bold tracking-[.1em] text-blue uppercase"
							>{recommended.category}</span
						>
					</div>
					<p class="mt-8 mb-0 text-[.68rem] font-[760] tracking-[.16em] text-lime uppercase">
						Góc nhìn được đề xuất
					</p>
					<h2
						class="mt-[.35rem] mb-0 text-[clamp(1.55rem,3vw,3rem)] leading-[.95] font-bold tracking-[-.055em] uppercase"
					>
						{recommended.title}
					</h2>
					<div class="mt-5 flex items-center gap-2 text-[.76rem] text-[#91a0b4]">
						<span aria-hidden="true"><Clock3 class="size-[1em]" /></span>{recommended.missionCount}
						nhiệm vụ · {recommended.duration}
					</div>
					<p class="mt-5 mb-0 text-[.9rem] leading-[1.5] text-[#91a0b4]">
						{recommended.description}
					</p>
					<div class="mt-6 border-t border-white/14 pt-4">
						<span class="text-[.63rem] font-bold tracking-[.1em] text-blue uppercase"
							>Yếu tố được quan sát</span
						>
						<div class="mt-3 flex flex-wrap gap-2">
							{#each recommended.observedFactors as factor (factor)}
								<b class="border border-white/14 px-2 py-1 text-[.66rem] font-medium text-[#91a0b4]"
									>{factor}</b
								>
							{/each}
						</div>
					</div>
					<span class="mt-auto pt-8 text-[.73rem] font-bold tracking-[.08em] text-lime uppercase"
						>Xem chi tiết nhiệm vụ <span aria-hidden="true"><ArrowRight class="size-[1em]" /></span></span
					>
				</div>
			</button>
			<aside class="flex gap-4 border border-lime/45 bg-lime/[.045] p-5">
				<span
					class="grid h-8 w-8 shrink-0 place-items-center rounded-full border border-lime text-[.85rem] font-bold text-lime"
					><Info class="size-[1em]" aria-hidden="true" /></span
				>
				<div>
					<p class="m-0 text-[.68rem] font-[760] tracking-[.16em] text-lime uppercase">
						Vì sao là nghề này?
					</p>
					<strong class="mt-3 block text-[.92rem]"
						>{recommended.title} là mục tiêu hiện tại của bạn.</strong
					>
					<p class="mt-2 mb-0 text-[.78rem] leading-[1.5] text-[#91a0b4]">
						Đề xuất dựa trên sở thích nghề nghiệp bạn đã chọn. Tỷ lệ so sánh trong hồ sơ vẫn là dữ
						liệu minh họa cho đến khi AI được kết nối.
					</p>
				</div>
			</aside>
		</section>

		<div class="flex items-center justify-between gap-4 py-5 text-[.8rem] text-[#91a0b4]">
			<p class="m-0">
				Hiển thị <strong>{visibleExperiences.length}</strong>
				{visibleExperiences.length === 1 ? 'nghề nghiệp' : 'nghề nghiệp'}
			</p>
			{#if query || filter !== 'Tất cả'}
				<button
					class="inline-flex cursor-pointer items-center gap-2 border-0 bg-transparent text-[.74rem] font-[760] tracking-[.04em] whitespace-nowrap text-blue uppercase transition hover:-translate-y-0.5 hover:text-lime"
					type="button"
					onclick={clearFilters}
				>
					Xóa bộ lọc <span aria-hidden="true"><ArrowUpRight class="size-[1em]" /></span>
				</button>
			{/if}
		</div>
		{#if supportingExperiences.length}
			<section
				class="grid grid-cols-3 gap-3 max-[900px]:grid-cols-2 max-[620px]:grid-cols-1"
				aria-label="Các trải nghiệm nghề nghiệp khác"
			>
				{#each supportingExperiences as experience (experience.slug)}
					<button
						class="group cursor-pointer overflow-hidden border border-blue bg-[#071020] text-left text-[#f7f9fb] transition hover:-translate-y-1 hover:border-lime"
						type="button"
						onclick={() => openDetails(experience)}
					>
						<div class="relative h-48 overflow-hidden">
							<img
								class="h-full w-full object-cover transition duration-500 group-hover:scale-105"
								src={experience.image}
								alt=""
							/>
							<span
								class={`absolute top-3 left-3 border px-2 py-1 text-[.6rem] font-bold tracking-[.08em] uppercase ${experience.status === 'locked' ? 'border-[#f5ba66] bg-[#2c1e08] text-[#f5ba66]' : 'border-lime bg-[#081306] text-lime'}`}
								>{statusLabel(experience.status)}</span
							>
						</div>
						<div class="flex min-h-[15rem] flex-col p-5">
							<div class="flex items-center justify-between gap-3">
								<span class="text-[.7rem] font-bold tracking-[.1em] text-blue uppercase"
									>{experience.category}</span
								><span class="text-[.68rem] text-[#91a0b4]"
									>{experience.missionCount}
									nhiệm vụ · {experience.duration}</span
								>
							</div>
							<h2
								class="mt-5 mb-0 text-[1.35rem] leading-none font-bold tracking-[-.04em] uppercase"
							>
								{experience.title}
							</h2>
							<p class="mt-4 mb-0 text-[.82rem] leading-[1.5] text-[#91a0b4]">
								{experience.description}
							</p>
							<span class="mt-auto pt-6 text-[.7rem] font-bold tracking-[.08em] text-lime uppercase"
								>Xem chi tiết <span aria-hidden="true"><ArrowRight class="size-[1em]" /></span></span
							>
						</div>
					</button>
				{/each}
			</section>
		{:else}
			<section
				class="grid grid-cols-[auto_1fr_auto] items-center gap-5 border border-blue bg-[radial-gradient(circle_at_15%_40%,rgb(37_99_235_/.18),transparent_32%),#071020] p-[clamp(1.6rem,4vw,3.2rem)] max-[640px]:flex max-[640px]:flex-col max-[640px]:items-start"
			>
				<span
					class="grid h-16 w-16 place-items-center rounded-full border border-lime text-2xl text-lime"
					><Search class="size-[1em]" aria-hidden="true" /></span
				>
				<div>
					<p class="m-0 text-[.68rem] font-[760] tracking-[.16em] text-lime uppercase">
						Không có kết quả khớp
					</p>
					<h2
						class="mt-2 mb-0 text-[clamp(1.2rem,2.6vw,2rem)] font-bold tracking-[-.04em] uppercase"
					>
						Hãy thử tìm kiếm hoặc danh mục khác.
					</h2>
					<p class="mt-3 mb-0 text-[.84rem] leading-[1.5] text-[#91a0b4]">
						Thẻ mục tiêu vẫn hiển thị phía trên để bạn không mất vị trí.
					</p>
				</div>
				<button
					class="inline-flex min-h-[3.35rem] cursor-pointer items-center justify-center gap-3 border border-blue bg-transparent px-5 py-3 text-[.75rem] font-[760] tracking-[.07em] text-[#f7f9fb] uppercase transition hover:-translate-y-0.5 hover:border-lime max-[640px]:w-full"
					type="button"
					onclick={clearFilters}
				>
					Hiện tất cả nghề nghiệp
				</button>
			</section>
		{/if}

		<footer
			class="mt-10 flex items-center justify-between gap-5 border-t border-white/14 pt-5 text-[.72rem] leading-[1.5] text-[#91a0b4] max-[640px]:flex-col max-[640px]:items-start"
		>
			<a
				class="text-[.76rem] font-[760] tracking-[.08em] text-blue uppercase no-underline"
				href="/evaluation"><span aria-hidden="true"><ArrowLeft class="size-[1em]" /></span> Quay lại kết quả</a
			><span
				>Các tình huống nghề nghiệp được thiết kế để suy ngẫm. Không có trải nghiệm nào được khởi
				chạy từ bản xem trước này.</span
			>
		</footer>
	</div>
</main>

<Dialog.Root bind:open={dialogOpen}>
	<Dialog.Portal>
		<Dialog.Overlay class="fixed inset-0 z-50 bg-black/75 backdrop-blur-sm" />
		<Dialog.Content
			class="fixed top-1/2 left-1/2 z-51 grid max-h-[90vh] w-[min(92vw,62rem)] -translate-x-1/2 -translate-y-1/2 overflow-auto border border-blue bg-[#071020] p-[clamp(1.2rem,3vw,2.5rem)] text-[#f7f9fb] shadow-[0_2rem_7rem_rgb(0_0_0_/.6)]"
			aria-label={selected ? `Chi tiết trải nghiệm ${selected.title}` : 'Chi tiết trải nghiệm'}
		>
			{#if selected}
				<div class="flex items-start justify-between gap-5">
					<div>
						<p class="m-0 text-[.68rem] font-[760] tracking-[.16em] text-lime uppercase">
							{selected.category}
							/ {statusLabel(selected.status)}
						</p>
						<Dialog.Title
							class="mt-3 text-[clamp(2rem,5vw,4.2rem)] leading-[.9] font-bold tracking-[-.07em] uppercase"
							>{selected.title}</Dialog.Title
						><Dialog.Description
							class="mt-4 block max-w-[44rem] text-[.95rem] leading-[1.5] text-[#91a0b4]"
							>{selected.description}</Dialog.Description
						>
					</div>
					<Dialog.Close
						class="grid h-10 w-10 shrink-0 cursor-pointer place-items-center rounded-full border border-blue bg-transparent text-2xl text-[#f7f9fb]"
						aria-label="Đóng chi tiết"><X class="size-[1em]" aria-hidden="true" /></Dialog.Close
					>
				</div>
				<div class="mt-7 h-[min(30vw,17rem)] overflow-hidden border border-blue">
					<img class="h-full w-full object-cover" src={selected.image} alt="" />
				</div>
				<div
					class="flex flex-wrap gap-2 border-b border-white/14 py-4 text-[.7rem] font-bold tracking-[.08em] text-blue uppercase [&>span]:border [&>span]:border-blue [&>span]:px-3 [&>span]:py-2"
				>
					<span>{selected.missionCount} nhiệm vụ</span><span>{selected.duration}</span><span
						>{selected.status === 'locked'
							? 'Mô-đun đang phát triển'
							: 'Tổng quan tình huống'}</span
					>
				</div>
				<div class="mt-6 grid grid-cols-2 gap-8 max-[640px]:grid-cols-1">
					<div>
						<h3 class="m-0 text-[.76rem] font-bold tracking-[.1em] text-blue uppercase">
							Trọng tâm nhiệm vụ
						</h3>
						<ul class="mt-4 grid gap-2 pl-5 text-[.84rem] leading-[1.5] text-[#91a0b4]">
							{#each selected.missions as mission (mission)}
								<li>{mission}</li>
							{/each}
						</ul>
					</div>
					<div>
						<h3 class="m-0 text-[.76rem] font-bold tracking-[.1em] text-blue uppercase">
							Yếu tố được quan sát
						</h3>
						<ul class="mt-4 grid gap-2 pl-5 text-[.84rem] leading-[1.5] text-[#91a0b4]">
							{#each selected.observedFactors as factor (factor)}
								<li>{factor}</li>
							{/each}
						</ul>
					</div>
				</div>
				<div class="mt-7 border border-lime/45 bg-lime/[.045] p-4">
					<p class="m-0 text-[.68rem] font-[760] tracking-[.16em] text-lime uppercase">
						Vì sao nên so sánh?
					</p>
					<p class="mt-3 mb-0 text-[.84rem] leading-[1.5] text-[#91a0b4]">{selected.whyItFits}</p>
				</div>
				<div class="mt-7 flex justify-end gap-3 max-[640px]:flex-col">
					<Dialog.Close
						class="inline-flex min-h-[3.35rem] cursor-pointer items-center justify-center border border-blue bg-transparent px-5 py-3 text-[.75rem] font-[760] tracking-[.07em] text-[#f7f9fb] uppercase"
						>Quay lại kết quả</Dialog.Close
					><a
						class="inline-flex min-h-[3.35rem] items-center justify-center gap-3 border border-lime bg-lime px-5 py-3 text-[.75rem] font-[760] tracking-[.07em] text-[#061006] uppercase no-underline"
						href="/evaluation">Xem hồ sơ <span aria-hidden="true"><ArrowUpRight class="size-[1em]" /></span></a
					>
				</div>
			{/if}
		</Dialog.Content>
	</Dialog.Portal>
</Dialog.Root>
