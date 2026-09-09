<script lang="ts">
	import { onMount } from 'svelte';
	import { Dialog } from 'bits-ui';
	import { experiences, type CareerExperience, type ExperienceCategory } from '$lib/evaluation';

	type Filter = 'Tất cả' | ExperienceCategory;
	const filters: Filter[] = ['Tất cả', 'Y tế', 'Luật', 'Giáo dục', 'Kinh doanh', 'Kỹ thuật'];
	let query = $state('');
	let filter = $state<Filter>('Tất cả');
	let dialogOpen = $state(false);
	let selected = $state<CareerExperience | null>(null);
	let targetSlug = $state('doctor');

	const recommended = $derived(experiences.find((experience) => experience.slug === targetSlug) ?? experiences[0]);
	const visibleExperiences = $derived(experiences.filter((experience) => {
		const search = query.trim().toLowerCase();
		const matchesSearch = !search || [experience.title, experience.category, experience.description, ...experience.observedFactors].join(' ').toLowerCase().includes(search);
		const matchesFilter = filter === 'Tất cả' || experience.category === filter;
		return matchesSearch && matchesFilter;
	}));
	const supportingExperiences = $derived(visibleExperiences.filter((experience) => experience.slug !== recommended.slug));

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
		return status === 'recommended' ? 'Mục tiêu của bạn' : status === 'locked' ? 'Sắp ra mắt' : 'Sẵn sàng xem';
	}
</script>

<svelte:head>
	<title>Thư viện nghề nghiệp | DESMAP</title>
	<meta name="description" content="Khám phá các tình huống nghề nghiệp DESMAP và xem những yếu tố mỗi tình huống được thiết kế để quan sát." />
</svelte:head>

<main class="library-page">
	<div class="library-shell">
		<div class="eyebrow-row"><p class="eyebrow">03 / TRẢI NGHIỆM NGHỀ NGHIỆP VR</p><span class="step-pill">{experiences.length} NGHỀ NGHIỆP KHẢ DỤNG</span></div>
		<section class="library-intro">
			<div><p class="kicker">Thư viện nghề nghiệp</p><h1>Chọn một nghề để trải nghiệm.</h1><p class="intro-copy">Bước vào môi trường làm việc mô phỏng và quan sát cách bạn phản ứng trong tình huống thực tế. Đọc chi tiết nhiệm vụ trước khi chọn nghề để so sánh.</p></div>
			<a class="back-link" href="/evaluation">← Quay lại hồ sơ</a>
		</section>

		<section class="library-controls" aria-label="Bộ lọc thư viện nghề nghiệp">
			<label class="search-field"><span aria-hidden="true">⌕</span><input bind:value={query} type="search" placeholder="Tìm kiếm nghề nghiệp..." aria-label="Tìm kiếm nghề nghiệp" /></label>
			<div class="category-filter" role="group" aria-label="Lọc theo danh mục">
				{#each filters as item (item)}<button class:active={filter === item} type="button" aria-pressed={filter === item} onclick={() => (filter = item)}>{item}</button>{/each}
			</div>
		</section>

		<section class="recommended-grid" aria-label="Nghề nghiệp được đề xuất">
			<button class="recommended-card" type="button" onclick={() => openDetails(recommended)}>
				<div class="recommended-image"><img src={recommended.image} alt="" /></div>
				<div class="recommended-content">
					<div class="card-topline"><span class="status status-recommended">{statusLabel(recommended.status)} <i></i></span><span class="category-label">{recommended.category}</span></div>
					<p class="kicker">Góc nhìn được đề xuất</p><h2>{recommended.title}</h2>
					<div class="duration"><span aria-hidden="true">◷</span>{recommended.missionCount} nhiệm vụ · {recommended.duration}</div>
					<p class="card-description">{recommended.description}</p>
						<div class="observed"><span>Yếu tố được quan sát</span><div>{#each recommended.observedFactors as factor (factor)}<b>{factor}</b>{/each}</div></div>
					<span class="card-action">Xem chi tiết nhiệm vụ <span aria-hidden="true">→</span></span>
				</div>
			</button>
			<aside class="target-note"><span class="note-icon">i</span><div><p class="kicker">Vì sao là nghề này?</p><strong>{recommended.title} là mục tiêu hiện tại của bạn.</strong><p>Đề xuất dựa trên sở thích nghề nghiệp bạn đã chọn. Tỷ lệ so sánh trong hồ sơ vẫn là dữ liệu minh họa cho đến khi AI được kết nối.</p></div></aside>
		</section>

		<div class="results-row"><p>Hiển thị <strong>{visibleExperiences.length}</strong> {visibleExperiences.length === 1 ? 'nghề nghiệp' : 'nghề nghiệp'}</p>{#if query || filter !== 'Tất cả'}<button class="text-button" type="button" onclick={clearFilters}>Xóa bộ lọc ↗</button>{/if}</div>
		{#if supportingExperiences.length}
			<section class="experience-grid" aria-label="Các trải nghiệm nghề nghiệp khác">
					{#each supportingExperiences as experience (experience.slug)}
					<button class="experience-card" type="button" onclick={() => openDetails(experience)}>
						<div class="experience-image"><img src={experience.image} alt="" /><span class:locked={experience.status === 'locked'} class="status overlay-status">{statusLabel(experience.status)}</span></div>
						<div class="experience-body"><div class="card-topline"><span class="category-label">{experience.category}</span><span class="duration-small">{experience.missionCount} nhiệm vụ · {experience.duration}</span></div><h2>{experience.title}</h2><p>{experience.description}</p><span class="card-action">Xem chi tiết <span aria-hidden="true">→</span></span></div>
					</button>
				{/each}
			</section>
		{:else}
			<section class="empty-results"><span class="empty-symbol">⌕</span><div><p class="kicker">Không có kết quả khớp</p><h2>Hãy thử tìm kiếm hoặc danh mục khác.</h2><p>Thẻ mục tiêu vẫn hiển thị phía trên để bạn không mất vị trí.</p></div><button class="button button-outline" type="button" onclick={clearFilters}>Hiện tất cả nghề nghiệp</button></section>
		{/if}

		<footer class="library-footer"><a href="/evaluation">← Quay lại kết quả</a><span>Các tình huống nghề nghiệp được thiết kế để suy ngẫm. Không có trải nghiệm nào được khởi chạy từ bản xem trước này.</span></footer>
	</div>
</main>

<Dialog.Root bind:open={dialogOpen}>
	<Dialog.Portal>
		<Dialog.Overlay class="dialog-overlay" />
		<Dialog.Content class="experience-dialog" aria-label={selected ? `Chi tiết trải nghiệm ${selected.title}` : 'Chi tiết trải nghiệm'}>
			{#if selected}
				<div class="dialog-head"><div><p class="kicker">{selected.category} / {statusLabel(selected.status)}</p><Dialog.Title>{selected.title}</Dialog.Title><Dialog.Description>{selected.description}</Dialog.Description></div><Dialog.Close class="dialog-close" aria-label="Đóng chi tiết">×</Dialog.Close></div>
				<div class="dialog-image"><img src={selected.image} alt="" /></div>
				<div class="dialog-meta"><span>{selected.missionCount} nhiệm vụ</span><span>{selected.duration}</span><span>{selected.status === 'locked' ? 'Mô-đun đang phát triển' : 'Tổng quan tình huống'}</span></div>
				<div class="dialog-columns"><div><h3>Trọng tâm nhiệm vụ</h3><ul>{#each selected.missions as mission (mission)}<li>{mission}</li>{/each}</ul></div><div><h3>Yếu tố được quan sát</h3><ul>{#each selected.observedFactors as factor (factor)}<li>{factor}</li>{/each}</ul></div></div>
				<div class="dialog-fit"><p class="kicker">Vì sao nên so sánh?</p><p>{selected.whyItFits}</p></div>
				<div class="dialog-actions"><Dialog.Close class="button button-outline">Quay lại kết quả</Dialog.Close><a class="button button-lime" href="/evaluation">Xem hồ sơ <span aria-hidden="true">↗</span></a></div>
			{/if}
		</Dialog.Content>
	</Dialog.Portal>
</Dialog.Root>
