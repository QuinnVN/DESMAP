<script lang="ts">
	import { onMount } from 'svelte';
	import { fade, fly } from 'svelte/transition';
	import { totalQuestionCount } from '$lib/questionnaire';

	type Dimension = { id: string; letter: string; label: string; prompt: string; title: string; accent: string; copy: string };
	const dimensions: Dimension[] = [
		{ id: 'desire', letter: 'D', label: 'Desire', prompt: 'Điều gì thúc đẩy tôi?', title: 'DESIRE', accent: 'MONG MUỐN CỦA BẠN', copy: 'Khía cạnh này khám phá các giá trị nghề nghiệp: điều bạn mong muốn và ưu tiên ở công việc tương lai, như an toàn tài chính, phát triển, tự chủ, tác động ý nghĩa, sự ghi nhận hoặc cân bằng cuộc sống.' },
		{ id: 'expertise', letter: 'E', label: 'Expertise', prompt: 'Tôi giỏi điều gì?', title: 'EXPERTISE', accent: 'ĐIỂM MẠNH CỦA BẠN', copy: 'Khía cạnh này khám phá kỹ năng bạn mang đến cho thử thách: cách bạn học hỏi, giải quyết vấn đề, truyền đạt ý tưởng và biến điểm mạnh thành đóng góp.' },
		{ id: 'social', letter: 'S', label: 'Social Role', prompt: 'Tôi làm việc với người khác thế nào?', title: 'SOCIAL ROLE', accent: 'VAI TRÒ XÃ HỘI', copy: 'Khía cạnh này khám phá cách bạn thường đóng góp khi làm việc cùng người khác: dẫn dắt, điều phối, hỗ trợ, chia sẻ ý tưởng hay thích làm việc độc lập.' },
		{ id: 'mind', letter: 'M', label: 'Mind', prompt: 'Tôi suy nghĩ như thế nào?', title: 'MIND', accent: 'CÁCH BẠN SUY NGHĨ', copy: 'Khía cạnh này khám phá cách bạn xử lý thông tin và giải quyết vấn đề: phân tích tình huống, đưa ra quyết định, tạo ý tưởng và tiếp cận thử thách.' },
		{ id: 'adaptability', letter: 'A', label: 'Adaptability', prompt: 'Tôi phản ứng thế nào với thay đổi?', title: 'ADAPTABILITY', accent: 'PHẢN ỨNG VỚI THAY ĐỔI', copy: 'Khía cạnh này khám phá cách bạn phản ứng khi mọi việc thay đổi: thích nghi với môi trường mới, tình huống bất ngờ, phản hồi và nhiệm vụ xa lạ.' },
		{ id: 'pressure', letter: 'P', label: 'Pressure', prompt: 'Tôi hành động thế nào khi chịu áp lực?', title: 'PRESSURE', accent: 'KHẢ NĂNG CHỊU ÁP LỰC', copy: 'Khía cạnh này khám phá cách bạn hành động trong tình huống đòi hỏi cao: quản lý căng thẳng, duy trì tập trung, ra quyết định và tiếp tục làm việc khi đối mặt áp lực.' }
	];
	let activeId = $state('desire');
	let reduceMotion = $state(false);
	let active = $derived(dimensions.find((item) => item.id === activeId) ?? dimensions[0]);

	onMount(() => {
		const media = window.matchMedia('(prefers-reduced-motion: reduce)');
		const update = () => (reduceMotion = media.matches);
		update();
		media.addEventListener('change', update);
		return () => media.removeEventListener('change', update);
	});

	function choose(id: string, moveFocus = false) {
		activeId = id;
		if (moveFocus) queueMicrotask(() => (document.querySelector(`[data-dimension-id="${id}"]`) as HTMLButtonElement | null)?.focus({ preventScroll: true }));
	}
	function onCardKeydown(event: KeyboardEvent, index: number) {
		if (event.key === 'ArrowRight' || event.key === 'ArrowDown') { event.preventDefault(); choose(dimensions[(index + 1) % dimensions.length].id, true); }
		if (event.key === 'ArrowLeft' || event.key === 'ArrowUp') { event.preventDefault(); choose(dimensions[(index + dimensions.length - 1) % dimensions.length].id, true); }
	}
</script>

<section class="border-y border-line bg-[radial-gradient(circle_at_74%_46%,rgb(6_38_98_/.18),transparent_36%)] py-[clamp(4.5rem,9vw,9rem)]" id="about" aria-labelledby="explorer-heading">
	<div class="mx-auto grid w-[min(100%_-_2rem,90rem)] grid-cols-1 items-center gap-8 min-[1001px]:w-[min(100%_-_4rem,90rem)] min-[1001px]:grid-cols-[minmax(18rem,.7fr)_minmax(35rem,1.3fr)] min-[1001px]:gap-[clamp(2rem,6vw,7.4rem)]">
		<div class="flex min-h-0 flex-col pt-12 pb-0 min-[1001px]:min-h-[40rem] min-[1001px]:pb-12">
			<p class="m-0 font-mono text-[.73rem] font-[760] tracking-[.14em] text-lime uppercase">ĐÁNH GIÁ DESMAP</p>
			<div class="grid min-h-72 pt-[3.3rem] min-[1001px]:min-h-80 [&>div]:[grid-area:1/1]" aria-live="polite">
				{#key active.id}
					<div in:fly={{ y: reduceMotion ? 0 : 12, duration: reduceMotion ? 0 : 360 }} out:fade={{ duration: reduceMotion ? 0 : 140 }}>
						<div id="dimension-description" role="tabpanel" tabindex="-1"><h2 class="m-0 max-w-96 text-[clamp(2rem,4.8vw,4.1rem)] leading-[.98] font-[450] tracking-[-.07em] uppercase" id="explorer-heading">{active.title.split(' — ')[0]} <span class="text-text">—</span><strong class="block font-[450] text-lime">{active.accent}</strong></h2>
						<p class="mt-8 mb-0 max-w-[29rem] text-[1.02rem] leading-[1.58] text-muted">{active.copy}</p>
						</div>
					</div>
				{/key}
			</div>
			<div class="mt-12 grid max-w-[38rem] grid-cols-4 border-y border-line min-[1001px]:mt-auto [&>div]:flex [&>div]:min-w-0 [&>div]:flex-col [&>div]:gap-[.18rem] [&>div]:border-r [&>div]:border-line [&>div]:px-[.45rem] [&>div]:py-4 min-[601px]:[&>div]:px-[.7rem] [&>div:last-child]:border-r-0 [&_b]:text-[.95rem] [&_b]:font-[580] [&_b]:tracking-[-.04em] min-[601px]:[&_b]:text-[1.15rem] [&_small]:font-mono [&_small]:text-[.45rem] [&_small]:leading-[1.2] [&_small]:tracking-[.06em] [&_small]:text-muted min-[601px]:[&_small]:text-[.53rem]" aria-label="Chi tiết bài đánh giá">
				<div><span class="text-[1.3rem] leading-none text-blue">▤</span><b>{totalQuestionCount}</b><small>CÂU HỎI</small></div>
				<div><span class="text-[1.3rem] leading-none text-blue">◷</span><b>NHỊP ĐỘ</b><small>THEO CÁCH CỦA BẠN</small></div>
				<div><span class="text-[1.3rem] leading-none text-blue">⌁</span><b>VR</b><small>TRẢI NGHIỆM</small></div>
				<div><span class="text-[1.3rem] leading-none text-blue">⌂</span><b>∞</b><small>ĐÃ LƯU TIẾN TRÌNH</small></div>
			</div>
		</div>
		<div class="flex flex-col items-center pb-8 min-[1001px]:pb-0">
			<div class="grid w-full grid-cols-2 gap-3 min-[601px]:grid-cols-3 min-[601px]:gap-[1.3rem]" role="tablist" aria-label="Các khía cạnh DESMAP">
				{#each dimensions as dimension, index (dimension.id)}
					<button data-dimension-id={dimension.id} class={`flex min-h-40 cursor-pointer flex-col items-center justify-center rounded-[.65rem] border bg-[linear-gradient(145deg,rgb(7_18_40_/.58),rgb(2_5_17_/.72))] p-4 text-text transition-[border-color,box-shadow,color,transform] duration-300 hover:-translate-y-1 hover:border-[#72aaff] hover:shadow-[0_0_1rem_rgb(12_102_255_/.4)] focus-visible:-translate-y-1 focus-visible:border-[#72aaff] focus-visible:shadow-[0_0_1rem_rgb(12_102_255_/.4)] focus-visible:outline-none min-[601px]:min-h-[13.3rem] min-[601px]:rounded-[.9rem] ${activeId === dimension.id ? 'border-lime text-lime shadow-[0_0_1.3rem_rgb(188_255_99_/.35),inset_0_0_1.5rem_rgb(188_255_99_/.05)]' : 'border-blue'}`} role="tab" aria-selected={activeId === dimension.id} aria-controls="dimension-description" tabindex={activeId === dimension.id ? 0 : -1} onclick={() => choose(dimension.id)} onkeydown={(event) => onCardKeydown(event, index)}>
						<span class="text-[4rem] leading-[.9] font-[350] tracking-[-.1em] min-[601px]:text-[clamp(4rem,6vw,6.9rem)]">{dimension.letter}</span><span class="mt-[.7rem] font-mono text-[.67rem] font-bold tracking-[.03em] uppercase min-[601px]:mt-[1.1rem] min-[601px]:text-[.83rem]">{dimension.label}</span><span class="mt-[.7rem] text-[.68rem] text-current opacity-90 min-[601px]:text-[.82rem]">{dimension.prompt}</span>
					</button>
				{/each}
			</div>
			<p class="mt-6 mb-0 text-center font-mono text-[.68rem] leading-normal tracking-[.04em] text-muted">Câu trả lời của bạn chỉ được dùng<br class="hidden min-[601px]:block" /> để xây dựng hồ sơ nghề nghiệp.</p>
		</div>
	</div>
</section>
