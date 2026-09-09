<script lang="ts">
	import { onMount } from 'svelte';
	import {
		answeredCount,
		createEvaluationModel,
		formatDate,
		type EvaluationModel
	} from '$lib/evaluation';
	import {
		readCompletionPayload,
		readSavedQuestionnaire,
		type QuestionnaireDraft,
		type QuestionnaireSubmission,
		type StageId
	} from '$lib/questionnaire';

	type Tab = 'profile' | 'report';

	const stageOrder: StageId[] = ['D', 'E', 'S', 'M', 'A', 'P'];
	let activeTab = $state<Tab>('profile');
	let payload = $state<QuestionnaireSubmission | null>(null);
	let draft = $state<QuestionnaireDraft | null>(null);
	let copied = $state(false);
	let downloadNotice = $state('');
	let showSample = $state(false);

	const model = $derived(createEvaluationModel(payload));
	const completed = $derived(Boolean(payload));
	const visibleModel = $derived(model);

	onMount(() => {
		payload = readCompletionPayload();
		draft = readSavedQuestionnaire();
	});

	function pointFor(index: number, value: number, radius = 108) {
		const angle = -Math.PI / 2 + (Math.PI * 2 * index) / stageOrder.length;
		return {
			x: 160 + Math.cos(angle) * radius * (value / 100),
			y: 145 + Math.sin(angle) * radius * (value / 100)
		};
	}

	function radarPoints(values: EvaluationModel['stageScores'], radius = 108): string {
		return stageOrder.map((stage, index) => {
			const point = pointFor(index, values[stage], radius);
			return `${point.x},${point.y}`;
		}).join(' ');
	}

	function framePoints(level: number): string {
		return radarPoints({ D: level, E: level, S: level, M: level, A: level, P: level }, 108);
	}

	function saveJson() {
		const exportModel = createEvaluationModel(payload);
		const data = {
			format: 'Bản xem trước kết quả DESMAP trên giao diện',
			label: completed ? 'Tóm tắt tự đánh giá; các nghề nghiệp phù hợp chỉ có tính minh họa cho đến khi AI được kết nối.' : 'Bản xem trước báo cáo mẫu; chưa hoàn tất bảng câu hỏi.',
			questionnaire: payload,
			evaluation: exportModel
		};
		const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
		const url = URL.createObjectURL(blob);
		const link = document.createElement('a');
		link.href = url;
		link.download = `desmap-${completed ? 'results' : 'sample-preview'}.json`;
		link.click();
		URL.revokeObjectURL(url);
		downloadNotice = 'Đã lưu JSON trên thiết bị này.';
		window.setTimeout(() => (downloadNotice = ''), 2400);
	}

	function printReport() {
		window.print();
	}

	async function copySummary() {
		const text = `${visibleModel.targetCareer} · ${visibleModel.careerMatches[0]?.percent ?? 0}% mức độ phù hợp minh họa\n${visibleModel.strengths.join(' · ')}`;
		try {
			await navigator.clipboard.writeText(text);
			copied = true;
			window.setTimeout(() => (copied = false), 2400);
		} catch {
			downloadNotice = 'Không thể sao chép trong trình duyệt này.';
			window.setTimeout(() => (downloadNotice = ''), 2400);
		}
	}

	function exploreTarget() {
		window.location.href = `/experiences?career=${visibleModel.targetExperienceSlug}`;
	}

	function profileTabLabel() {
		return completed ? 'Hồ sơ ban đầu' : showSample ? 'Hồ sơ mẫu' : 'Hồ sơ của bạn';
	}
</script>

<svelte:head>
	<title>Hồ sơ của tôi | DESMAP</title>
	<meta name="description" content="Xem lại bản tự đánh giá DESMAP và khám phá báo cáo nghề nghiệp mẫu được gắn nhãn rõ ràng." />
</svelte:head>

<main class="evaluation-page">
	<div class="page-shell">
		<div class="eyebrow-row">
			<p class="eyebrow">02 / ĐÁNH GIÁ DESMAP</p>
			<span class="step-pill">XEM LẠI HỒ SƠ</span>
		</div>

		<section class="page-intro">
			<div>
				<p class="kicker">{completed ? 'Kết quả DESMAP ban đầu' : 'Hồ sơ của bạn đang chờ'}</p>
				<h1>{completed ? 'Nhìn thấy mô thức trong câu trả lời của bạn.' : 'Hoàn tất bảng câu hỏi để mở khóa hồ sơ.'}</h1>
				<p class="intro-copy">
					{completed
						? 'Đây là ảnh chụp nhanh về bản tự đánh giá của bạn. Các nghề nghiệp phù hợp bên dưới chỉ là bản xem trước minh họa cho đến khi dịch vụ đánh giá AI được kết nối.'
						: 'Trang này vẫn hữu ích khi bạn tạm dừng. Hãy tiếp tục các câu trả lời đã lưu, hoặc mở báo cáo mẫu để xem trải nghiệm đã thiết kế.'}
				</p>
			</div>
			<div class="intro-aside">
				<span class="signal-dot"></span>
				<div>
					<strong>{completed ? 'Đã lưu trên thiết bị này' : draft ? `Đã lưu ${answeredCount(draft)} câu trả lời` : 'Chưa có câu trả lời được lưu'}</strong>
					<small>{completed ? formatDate(payload?.completedAt) : draft ? 'Tiếp tục khi bạn sẵn sàng' : 'Kết quả của bạn sẽ ở lại trên thiết bị này'}</small>
				</div>
			</div>
		</section>

			{#if !completed}
			<section class="resume-panel" aria-label="Tiến trình bảng câu hỏi">
			<div class="resume-copy">
				<span class="resume-index">01</span>
				<div>
					<p class="kicker">{draft ? 'Tiếp tục bảng câu hỏi' : 'Bắt đầu tự đánh giá'}</p>
					<h2>{draft ? `Đã lưu ${answeredCount(draft)} trên 98 câu hỏi` : 'Câu trả lời của bạn tạo nên bản đồ đầu tiên.'}</h2>
					<p>{draft ? 'Các câu trả lời mới nhất chỉ được lưu trong trình duyệt này.' : 'Hãy cho DESMAP biết điều quan trọng với bạn trước khi chúng tôi so sánh hồ sơ với các tình huống nghề nghiệp.'}</p>
				</div>
			</div>
			<a class="button button-lime" href="/questionnaire">{draft ? 'Tiếp tục bảng câu hỏi' : 'Bắt đầu bảng câu hỏi'} <span aria-hidden="true">↗</span></a>
			</section>
			<div class="sample-callout">
				<div><span class="sample-tag">MẪU TÙY CHỌN</span><strong>Bạn muốn xem trước bố cục hoàn chỉnh?</strong><p>Mở hồ sơ minh họa được gắn nhãn rõ ràng. Đây không phải dự đoán về bạn.</p></div>
				<button class="text-button" type="button" onclick={() => (showSample = !showSample)}>{showSample ? 'Ẩn mẫu' : 'Xem trước mẫu'} <span aria-hidden="true">→</span></button>
			</div>
		{/if}

		<div class="report-tabs" role="tablist" aria-label="Các chế độ xem hồ sơ">
				<button role="tab" class:active={activeTab === 'profile'} type="button" onclick={() => (activeTab = 'profile')} aria-selected={activeTab === 'profile'}>{profileTabLabel()}</button>
				<button role="tab" class:active={activeTab === 'report'} type="button" onclick={() => (activeTab = 'report')} aria-selected={activeTab === 'report'}>Đánh giá AI <span>MINH HỌA</span></button>
		</div>

		{#if activeTab === 'profile'}
			{#if !completed && !showSample}
				<section class="empty-profile" aria-label="Chưa có hồ sơ">
					<div class="empty-symbol">+</div>
					<div><p class="kicker">Chưa tạo hồ sơ</p><h2>Trả lời một vài câu hỏi và bản đồ của bạn sẽ hiện tại đây.</h2><p>Các câu trả lời đã lưu không rời khỏi trình duyệt này trong bản xem trước giao diện. Khi hoàn tất, hãy trở lại đây để xem các khía cạnh DESMAP của riêng bạn.</p></div>
					<a class="button button-outline" href="/questionnaire">Đến bảng câu hỏi <span aria-hidden="true">↗</span></a>
				</section>
			{:else}
			<section class="profile-grid" aria-label={completed ? 'Hồ sơ nghề nghiệp ban đầu' : 'Hồ sơ nghề nghiệp mẫu'}>
				<div class="panel profile-panel">
					<div class="panel-heading"><div><p class="kicker">01 / Tôi là ai?</p><h2>Hồ sơ cá nhân</h2></div><span class="panel-icon blue">◎</span></div>
					<div class="radar-wrap">
						<svg class="radar" viewBox="0 0 320 290" role="img" aria-label="Biểu đồ radar khía cạnh DESMAP">
							{#each [20, 40, 60, 80, 100] as level (level)}
								<polygon points={framePoints(level)} fill="none" stroke="rgba(255,255,255,.14)" stroke-width="1" />
							{/each}
							{#each stageOrder as stage, index (stage)}
								{@const axis = pointFor(index, 100, 108)}
								<line x1="160" y1="145" x2={axis.x} y2={axis.y} stroke="rgba(255,255,255,.14)" stroke-width="1" />
								{@const label = pointFor(index, 122, 108)}
								<text x={label.x} y={label.y} text-anchor="middle" dominant-baseline="middle">{stage}</text>
							{/each}
							<polygon points={radarPoints(visibleModel.stageScores)} fill="rgba(188,255,99,.21)" stroke="var(--lime)" stroke-width="2.5" />
							{#each stageOrder as stage, index (stage)}
								{@const dot = pointFor(index, visibleModel.stageScores[stage], 108)}
								<circle cx={dot.x} cy={dot.y} r="4.5" fill="var(--lime)" />
							{/each}
						</svg>
						<div class="radar-note"><span class="legend-dot"></span><span>{completed ? 'Điểm tự đánh giá' : 'Điểm mẫu minh họa'}</span></div>
					</div>
					<div class="profile-list">
							{#each visibleModel.profileRows as row (row.code)}
							<div class="profile-row"><span class="profile-code">{row.code}</span><div><strong>{row.label}</strong><small>{row.detail}</small></div><b>{row.value}</b></div>
						{/each}
					</div>
				</div>

				<div class="profile-side">
					<div class="panel match-panel">
						<div class="panel-heading"><div><p class="kicker">02 / Điều gì phù hợp?</p><h2>Mức độ phù hợp nghề nghiệp</h2></div><span class="panel-icon blue">⌁</span></div>
						<div class="demo-banner">BẢN XEM TRƯỚC MINH HỌA <span>Đánh giá AI chưa được kết nối</span></div>
							{#each visibleModel.careerMatches as match, index (match.label)}
							<div class="match-row"><span class="match-index">0{index + 1}</span><strong class:lime={match.accent === 'lime'}>{match.label}</strong><div class="match-track"><span style:width={`${match.percent}%`}></span></div><b>{match.percent}%</b></div>
						{/each}
						<p class="panel-footnote">Báo cáo AI thực tế sẽ thay thế các giá trị minh họa sau khi dịch vụ đánh giá được kết nối.</p>
					</div>

					<div class="panel target-panel">
						<div class="panel-heading compact"><div><p class="kicker">03 / Góc nhìn tiếp theo</p><h2>Khoảng cần phát triển</h2></div><span class="target-pill">MỤC TIÊU: {visibleModel.targetCareer.toUpperCase()}</span></div>
						<div class="gap-list">
								{#each visibleModel.gaps as gap, index (gap)}
								<div class="gap-row"><span>0{index + 1}</span><div><strong>{gap}</strong><small>{index === 0 ? 'Hiện tại: đang phát triển' : index === 1 ? 'Hiện tại: trung bình' : 'Hiện tại: tốt'}</small></div><i style:--gap={`${24 - index * 4}%`}></i><b>CHÊNH LỆCH {24 - index * 4}%</b></div>
							{/each}
						</div>
					</div>
				</div>
			</section>

			<section class="bottom-rail">
			<div><span class="rail-icon">◎</span><div><strong>{completed ? 'Hồ sơ của bạn cho thấy một điểm khởi đầu.' : 'Hồ sơ mẫu cho thấy hành trình.'}</strong><p>{completed ? 'Các tình huống VR có thể bổ sung hành vi quan sát được sau khi kết nối.' : 'Hãy dùng bảng câu hỏi để thay thế bản xem trước bằng câu trả lời của riêng bạn.'}</p></div></div>
			<button class="button button-lime" type="button" onclick={exploreTarget}>Khám phá nghề {visibleModel.targetCareer} <span aria-hidden="true">→</span></button>
			<div class="rail-actions"><button class="text-button" type="button" onclick={saveJson}>↓ Tải JSON</button><button class="text-button" type="button" onclick={copySummary}>{copied ? 'Đã sao chép trên thiết bị này' : 'Sao chép tóm tắt'}</button></div>
			</section>
			{/if}
		{:else}
			<section class="final-report" aria-label="Báo cáo đánh giá AI mẫu">
				<div class="report-topline"><div><p class="kicker">BÁO CÁO MINH HỌA / 04</p><h2>Hồ sơ nghề nghiệp cuối cùng</h2><p class="report-disclaimer">Đây là bố cục báo cáo mẫu. Đây không phải kết luận từ AI và không bao gồm quan sát VR.</p></div><div class="fit-ring"><strong>84%</strong><span>ĐỘ PHÙ HỢP MẪU</span></div></div>
				<div class="report-columns">
					<div>
			<div class="panel observation-panel"><div class="panel-heading compact"><div><p class="kicker">Tự đánh giá và hành vi quan sát</p><h2>Điều gì sẽ thay đổi sau VR?</h2></div></div>{#each visibleModel.observations as observation (observation.self)}<div class="observation-row"><span>→</span><div><strong>{observation.self}</strong><small>{observation.observed}</small></div></div>{/each}</div>
			<div class="split-panel"><div><h3>Điểm mạnh</h3>{#each visibleModel.strengths as strength (strength)}<p>{strength}</p>{/each}</div><div><h3>Khoảng cần phát triển</h3>{#each visibleModel.gaps as gap (gap)}<p>{gap}</p>{/each}</div></div>
					</div>
					<div>
							<div class="panel roadmap-panel"><div class="panel-heading compact"><div><p class="kicker">Phát triển cá nhân</p><h2>Lộ trình</h2></div></div>{#each visibleModel.roadmap as item, index (item)}<div class="roadmap-row"><span>0{index + 1}</span><strong>{item}</strong></div>{/each}<h3>Nghề nghiệp tương tự</h3><div class="similar-list"><span>Y học cấp cứu</span><span>Điều dưỡng</span><span>Điều phối lâm sàng</span></div></div>
					</div>
				</div>
				<div class="report-actions"><button class="button button-outline" type="button" onclick={saveJson}>↓ Tải JSON</button><button class="button button-outline" type="button" onclick={printReport}>In / lưu PDF</button><button class="button button-lime" type="button" onclick={copySummary}>{copied ? 'Đã sao chép trên thiết bị này' : 'Sao chép tóm tắt báo cáo'}</button></div>
			</section>
		{/if}

		{#if downloadNotice}<p class="notice" role="status">{downloadNotice}</p>{/if}
	</div>
</main>
