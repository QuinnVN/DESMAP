<script lang="ts">
	import { Dialog } from 'bits-ui';

	type Career = {
		title: string;
		category: string;
		description: string;
		skills: string[];
		color: string;
	};
	const careers: Career[] = [
		{
			title: 'Nhà thiết kế sản phẩm',
			category: 'Sáng tạo',
			description:
				'Định hình sản phẩm số bằng cách biến những câu hỏi đầy tò mò thành trải nghiệm rõ ràng, hữu ích.',
			skills: ['Thấu cảm', 'Tư duy hệ thống', 'Kể chuyện'],
			color: '#135cff'
		},
		{
			title: 'Nhà khoa học dữ liệu',
			category: 'Phân tích',
			description: 'Tìm tín hiệu trong dữ liệu phức tạp và chuyển thành những quyết định hữu ích.',
			skills: ['Nhận diện mẫu', 'Logic', 'Thử nghiệm'],
			color: '#8f62ff'
		},
		{
			title: 'Chuyên gia chiến lược bền vững',
			category: 'Tác động',
			description: 'Giúp tổ chức tạo ra tiến bộ có thể đo lường cho con người và hành tinh.',
			skills: ['Tư duy toàn cảnh', 'Hợp tác', 'Kiên cường'],
			color: '#baff60'
		}
	];
	let selected = $state<Career | null>(null);
	let filter = $state('Tất cả');
	const filters = ['Tất cả', 'Sáng tạo', 'Phân tích', 'Tác động'];
	let visibleCareers = $derived(
		filter === 'Tất cả' ? careers : careers.filter((career) => career.category === filter)
	);
</script>

<section
	class="bg-[#050505] py-[clamp(4.5rem,9vw,9rem)]"
	id="careers"
	aria-labelledby="careers-heading"
>
	<div class="mx-auto w-[min(100%_-_2rem,90rem)] min-[701px]:w-[min(100%_-_4rem,90rem)]">
		<div
			class="flex items-center justify-between border-b border-line pb-[1.4rem] font-mono text-[.69rem] tracking-[.13em] text-lime uppercase [&>span:last-child]:text-muted"
		>
			<span>Thư viện nghề nghiệp</span><span>Khám phá một hướng đi, rồi cảm nhận nghề đó.</span>
		</div>
		<div
			class="block gap-8 py-[2.5rem_1.8rem] min-[701px]:flex min-[701px]:items-end min-[701px]:justify-between min-[701px]:py-[3.6rem_2.5rem]"
		>
			<div>
				<p class="font-mono text-[.7rem] tracking-[.12em] text-lime uppercase">
					CẨM NANG CHO CHẶNG ĐƯỜNG TIẾP THEO
				</p>
				<h2
					class="mt-[1.3rem] mb-0 max-w-[40rem] text-[clamp(3rem,7vw,6rem)] leading-[.84] font-[430] tracking-[-.09em] text-text uppercase"
					id="careers-heading"
				>
					Biến điều<br /><span class="text-lime">chưa biết</span>
					thành cụ thể.
				</h2>
			</div>
			<a
				class="mt-[1.7rem] inline-block font-mono text-[.72rem] tracking-[.04em] whitespace-nowrap text-text uppercase no-underline min-[701px]:mt-0 [&>span]:text-[1.2rem] [&>span]:text-lime"
				href="/experiences">Xem toàn bộ thư viện <span aria-hidden="true">↗</span></a
			>
		</div>
		<div class="mb-[1.3rem] flex flex-wrap gap-[.6rem]" aria-label="Danh mục nghề nghiệp">
			{#each filters as item (item)}
				<button
					class={`cursor-pointer rounded-full border bg-transparent px-[.85rem] py-2 font-mono text-[.65rem] tracking-[.05em] uppercase transition duration-200 hover:border-lime hover:text-lime focus-visible:border-lime focus-visible:text-lime focus-visible:outline-none ${filter === item ? 'border-lime text-lime' : 'border-line-strong text-muted'}`}
					onclick={() => (filter = item)}
				>
					{item}
				</button>
			{/each}
		</div>
		<div class="grid grid-cols-1 gap-4 min-[701px]:grid-cols-3">
			{#each visibleCareers as career (career.title)}
				<button
					class="group flex min-h-92 cursor-pointer flex-col items-start rounded-[.85rem] border border-line bg-surface p-4 text-left text-text transition-[border-color,transform,box-shadow] duration-250 hover:-translate-y-[.35rem] hover:border-(--career-color) hover:shadow-[0_0_1.3rem_color-mix(in_srgb,var(--career-color)_25%,transparent)] focus-visible:-translate-y-[.35rem] focus-visible:border-(--career-color) focus-visible:shadow-[0_0_1.3rem_color-mix(in_srgb,var(--career-color)_25%,transparent)] focus-visible:outline-none min-[701px]:min-h-[26rem]"
					style:--career-color={career.color}
					onclick={() => (selected = career)}
				>
					<div
						class="mb-[1.4rem] grid min-h-40 w-full place-items-center rounded-lg bg-[radial-gradient(circle_at_50%_45%,color-mix(in_srgb,var(--career-color)_45%,transparent),transparent_46%),linear-gradient(145deg,#0d1a37,#05070d)] min-[701px]:min-h-48"
					>
						<span
							class="text-[4.7rem] font-light tracking-[-.14em] text-(--career-color) [text-shadow:0_0_1.2rem_var(--career-color)]"
							>{career.title
								.split(' ')
								.map((word) => word[0])
								.join('')}</span
						>
					</div>
					<small class="font-mono text-[.63rem] tracking-[.1em] text-(--career-color) uppercase"
						>{career.category}</small
					>
					<h3 class="mt-[.65rem] mb-0 text-[1.4rem] font-[520] tracking-[-.05em]">
						{career.title}
					</h3>
					<p class="mt-[.7rem] mb-[1.2rem] text-[.86rem] leading-[1.45] text-muted">
						{career.description}
					</p>
					<span
						class="mt-auto inline-flex gap-[.55rem] font-mono text-[.67rem] tracking-[.08em] uppercase"
						>Xem vai trò
						<b class="text-[1.1rem] text-lime" aria-hidden="true">↗</b></span
					>
				</button>
			{/each}
		</div>
	</div>
	<Dialog.Root
		open={selected !== null}
		onOpenChange={(open) => {
			if (!open) selected = null;
		}}
	>
		<Dialog.Portal>
			<Dialog.Overlay class="fixed inset-0 z-50 animate-overlay-in bg-black/76" />
			<Dialog.Content
				class="fixed top-1/2 left-1/2 z-51 grid max-h-[90vh] w-[min(90vw,54rem)] -translate-x-1/2 -translate-y-1/2 grid-cols-1 overflow-auto rounded-2xl border border-line-strong bg-[#080e1d] p-4 shadow-[0_2rem_7rem_rgb(0_0_0_/.6)] min-[701px]:grid-cols-[minmax(13rem,.7fr)_minmax(18rem,1.3fr)] min-[701px]:overflow-visible"
			>
				{#if selected}
					<div
						class="grid min-h-40 place-items-center rounded-[.65rem] bg-[radial-gradient(circle_at_50%_45%,color-mix(in_srgb,var(--career-color)_48%,transparent),transparent_45%),#050912] min-[701px]:min-h-[25rem]"
						style:--career-color={selected.color}
					>
						<span
							class="text-[4.7rem] font-light tracking-[-.14em] text-(--career-color) [text-shadow:0_0_1.2rem_var(--career-color)]"
							>{selected.title
								.split(' ')
								.map((word) => word[0])
								.join('')}</span
						>
					</div>
					<div class="p-[1.8rem_1rem_1rem] min-[701px]:p-[2.7rem_2.3rem]">
						<p class="m-0 font-mono text-[.7rem] tracking-[.12em] text-lime uppercase">
							{selected.category}
							/ TỔNG QUAN NGHỀ NGHIỆP
						</p>
						<Dialog.Title
							class="my-[1.1rem] text-[clamp(2rem,4vw,3.4rem)] leading-[.9] font-[460] tracking-[-.08em] uppercase"
							>{selected.title}</Dialog.Title
						>
						<p class="leading-normal text-muted">{selected.description}</p>
						<h4 class="mt-8 mb-[.8rem] font-mono text-[.68rem] tracking-[.08em] uppercase">
							Kỹ năng thể hiện ở đây
						</h4>
						<div class="mb-8 flex flex-wrap gap-[.45rem]">
							{#each selected.skills as skill (skill)}
								<span
									class="rounded-full border border-line-strong px-[.55rem] py-[.38rem] text-[.7rem] text-muted"
									>{skill}</span
								>
							{/each}
						</div>
						<a
							class="inline-flex min-h-[3.15rem] items-center justify-center gap-[.85rem] rounded-lg border border-transparent bg-lime px-[1.35rem] py-3 font-mono text-[.7rem] leading-none font-medium tracking-[.04em] text-[#050505] uppercase no-underline shadow-[0_0_1.2rem_rgb(188_255_99_/.15)] transition hover:-translate-y-0.5 hover:bg-[#d2ff96] hover:shadow-[0_0_1.6rem_rgb(188_255_99_/.32)]"
							href="/questionnaire">Xem mức độ phù hợp <span aria-hidden="true">↗</span></a
						>
					</div>
					<Dialog.Close
						class="absolute top-4 right-4 h-[2.4rem] w-[2.4rem] cursor-pointer rounded-full border border-line-strong bg-transparent text-2xl text-text"
						aria-label="Đóng chi tiết vai trò">×</Dialog.Close
					>
				{/if}
			</Dialog.Content>
		</Dialog.Portal>
	</Dialog.Root>
</section>
