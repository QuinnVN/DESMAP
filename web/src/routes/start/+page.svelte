<script lang="ts">
	import { onMount, untrack } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { prefersReducedMotion } from 'svelte/motion';
	import { fly } from 'svelte/transition';
	import { ArrowLeft, ArrowRight, LogIn } from '@lucide/svelte';
	import BrandMark from '$lib/components/BrandMark.svelte';
	import {
		checkEmailAvailability,
		clearSavedQuestionnaire,
		createQuestionnairePresentationOrder,
		isValidParticipantDetails,
		normalizeParticipantDetails,
		parseCompletionPayload,
		readSavedQuestionnaire,
		readQuestionnaireSessionRoute,
		writeCompletionPayload,
		writeSavedQuestionnaire,
		writeQuestionnaireSyncStatus,
		type ParticipantDetails,
		type QuestionnaireDraft,
		type QuestionnaireSubmission
	} from '$lib/questionnaire';
	import type { PageProps } from './$types';

	let { form }: PageProps = $props();
	let activeTab = $state<'signup' | 'login'>(
		untrack(() => (form?.success === false ? 'login' : 'signup'))
	);
	let restoring = $state(false);
	let restoreError = $state('');
	let signupParticipant = $state<ParticipantDetails>({ name: '', email: '' });
	let signupError = $state('');
	let checkingEmail = $state(false);
	let restoredAssessmentId = '';
	let panelOffset = $derived(activeTab === 'login' ? 18 : -18);
	let transitionDuration = $derived(prefersReducedMotion.current ? 0 : 220);

	function restoreSubmission(submission: QuestionnaireSubmission) {
		if (restoring || restoredAssessmentId === submission.assessmentId) return;
		restoring = true;
		restoredAssessmentId = submission.assessmentId;
		const restoredSubmission = parseCompletionPayload(submission);
		if (!restoredSubmission) {
			restoreError =
				'Kết quả đã lưu không đầy đủ hoặc không còn tương thích. Hãy liên hệ quản trị viên.';
			restoring = false;
			return;
		}
		if (!writeCompletionPayload(restoredSubmission)) {
			restoreError =
				'Không thể lưu kết quả trên thiết bị này. Hãy kiểm tra quyền lưu trữ của trình duyệt.';
			restoring = false;
			return;
		}
		clearSavedQuestionnaire();
		writeQuestionnaireSyncStatus({
			assessmentId: restoredSubmission.assessmentId,
			status: 'synced'
		});
		void goto(resolve('/evaluation'));
	}

	async function startQuestionnaire() {
		if (checkingEmail) return;
		const participant = normalizeParticipantDetails(signupParticipant);
		if (!isValidParticipantDetails(participant)) {
			signupError = 'Hãy nhập họ tên và địa chỉ email hợp lệ.';
			return;
		}

		checkingEmail = true;
		signupError = '';
		try {
			if (!(await checkEmailAvailability(participant.email))) {
				signupError = 'Email này đã hoàn thành một bài đánh giá. Hãy dùng tab Đăng nhập.';
				return;
			}

			const saved = readSavedQuestionnaire();
			const resumable = saved?.participant.email === participant.email ? saved : null;
			const draft: QuestionnaireDraft = {
				version: 1,
				completed: false,
				step: resumable?.step && resumable.step !== 'participant' ? resumable.step : 'career',
				currentIndex: resumable?.currentIndex ?? 0,
				startedAt: resumable?.startedAt ?? new Date().toISOString(),
				updatedAt: new Date().toISOString(),
				participant,
				careerInterests: resumable?.careerInterests ?? [],
				answers: resumable?.answers ?? {},
				presentationOrder: resumable?.presentationOrder ?? createQuestionnairePresentationOrder()
			};
			if (!writeSavedQuestionnaire(draft)) {
				signupError =
					'Không thể lưu thông tin trên thiết bị này. Hãy kiểm tra quyền lưu trữ của trình duyệt.';
				return;
			}
			signupParticipant = participant;
			void goto(resolve('/questionnaire'));
		} catch (error) {
			signupError = error instanceof Error ? error.message : 'Không thể kiểm tra địa chỉ email.';
		} finally {
			checkingEmail = false;
		}
	}

	onMount(() => {
		if (form?.success) {
			restoreSubmission(form.submission);
			return;
		}
		const sessionRoute = readQuestionnaireSessionRoute();
		if (sessionRoute) {
			void goto(resolve(sessionRoute), { replaceState: true });
			return;
		}
		const saved = readSavedQuestionnaire();
		if (saved) signupParticipant = { ...saved.participant };
	});
</script>

<svelte:head>
	<title>Bắt đầu hoặc xem lại kết quả | DESMAP</title>
	<meta
		name="description"
		content="Bắt đầu bài đánh giá DESMAP hoặc đăng nhập để xem lại kết quả đã lưu."
	/>
</svelte:head>

<main class="relative isolate min-h-dvh bg-bg p-[clamp(1rem,3vw,2.2rem)] text-text">
	<div
		class="pointer-events-none absolute inset-0 -z-1 bg-[radial-gradient(circle_at_31%_46%,rgb(5_82_255_/.26),transparent_34%),radial-gradient(circle_at_52%_52%,rgb(159_64_255_/.15),transparent_52%)]"
		aria-hidden="true"
	></div>
	<div
		class="relative mx-auto flex min-h-[calc(100dvh_-_clamp(2rem,6vw,4.4rem))] max-w-[76rem] flex-col"
	>
		<header class="flex items-center justify-between gap-6">
			<BrandMark />
			<a
				class="inline-flex min-h-11 items-center gap-2 rounded-full border border-line-strong px-5 font-mono text-[.68rem] tracking-[.04em] text-muted uppercase no-underline transition hover:border-lime hover:text-lime"
				href={resolve('/')}
			>
				<ArrowLeft class="size-4" aria-hidden="true" /> Trang chủ
			</a>
		</header>

		<section
			class="my-auto grid animate-start-page-enter items-center gap-10 py-14 motion-reduce:animate-none min-[900px]:grid-cols-[1fr_30rem]"
		>
			<div class="max-w-[38rem]">
				<p class="m-0 font-mono text-[.7rem] font-bold tracking-[.15em] text-lime uppercase">
					HỒ SƠ DESMAP CỦA BẠN
				</p>
				<h1
					class="mt-5 mb-0 text-[clamp(3rem,7vw,6.4rem)] leading-[.92] font-[720] tracking-[-.07em]"
				>
					Bắt đầu mới.<br />Hoặc tiếp tục.
				</h1>
				<p class="mt-7 mb-0 max-w-[34rem] text-[1rem] leading-[1.65] text-muted">
					Tạo hồ sơ DESMAP lần đầu, hoặc dùng thông tin đã đăng ký để xem lại kết quả bảng câu hỏi
					của bạn.
				</p>
			</div>

			<div
				class="overflow-hidden rounded-[1.4rem] border-2 border-blue bg-[linear-gradient(145deg,rgb(11_19_34_/.98),rgb(5_10_20_/.98))] shadow-[0_1.5rem_5rem_rgb(0_0_0_/.34)]"
			>
				<div
					class="relative m-3 mb-0 grid grid-cols-2 rounded-xl border border-line-strong bg-[#050914] p-1 shadow-[0_.8rem_2rem_rgb(0_0_0_/.18)]"
					role="tablist"
					aria-label="Chọn cách bắt đầu"
				>
					<span
						class={`pointer-events-none absolute top-1 bottom-1 left-1 w-[calc(50%_-_.25rem)] rounded-lg bg-lime shadow-[0_0_1.2rem_rgb(188_255_99_/.18)] transition-transform duration-300 ease-[cubic-bezier(.22,1,.36,1)] motion-reduce:transition-none ${activeTab === 'login' ? 'translate-x-full' : ''}`}
						aria-hidden="true"
					></span>
					<button
						class={`relative z-1 min-h-12 cursor-pointer rounded-lg border-0 bg-transparent px-5 font-mono text-[.72rem] font-bold tracking-[.07em] uppercase transition-colors duration-200 ${activeTab === 'signup' ? 'text-[#080b0f]' : 'text-muted hover:text-lime'}`}
						type="button"
						role="tab"
						aria-selected={activeTab === 'signup'}
						aria-controls="signup-panel"
						onclick={() => (activeTab = 'signup')}
					>
						Đăng ký
					</button>
					<button
						class={`relative z-1 min-h-12 cursor-pointer rounded-lg border-0 bg-transparent px-5 font-mono text-[.72rem] font-bold tracking-[.07em] uppercase transition-colors duration-200 ${activeTab === 'login' ? 'text-[#080b0f]' : 'text-muted hover:text-lime'}`}
						type="button"
						role="tab"
						aria-selected={activeTab === 'login'}
						aria-controls="login-panel"
						onclick={() => (activeTab = 'login')}
					>
						Đăng nhập
					</button>
				</div>

				<div class="overflow-hidden">
					{#key activeTab}
						{#if activeTab === 'signup'}
							<div
								class="p-[clamp(1.4rem,5vw,2.5rem)]"
								id="signup-panel"
								role="tabpanel"
								in:fly={{ x: panelOffset, duration: transitionDuration }}
							>
								<p
									class="m-0 font-mono text-[.65rem] font-bold tracking-[.13em] text-blue uppercase"
								>
									DÀNH CHO NGƯỜI THAM GIA MỚI
								</p>
								<h2 class="mt-4 mb-0 text-[2rem] leading-[1.05] font-[720] tracking-[-.04em]">
									Nhập thông tin để bắt đầu
								</h2>
								<form
									onsubmit={(event) => {
										event.preventDefault();
										void startQuestionnaire();
									}}
								>
									<div class="mt-7 grid gap-5">
										<label
											class="grid gap-2 text-[.78rem] font-semibold text-text"
											for="signup-name"
										>
											Họ và tên
											<input
												class="min-h-[3.35rem] rounded-xl border border-blue bg-[#030303]/65 px-4 text-base text-text transition outline-none focus:border-lime focus:ring-2 focus:ring-lime/20"
												id="signup-name"
												name="name"
												type="text"
												autocomplete="name"
												maxlength="100"
												required
												bind:value={signupParticipant.name}
											/>
										</label>
										<label
											class="grid gap-2 text-[.78rem] font-semibold text-text"
											for="signup-email"
										>
											Địa chỉ email
											<input
												class="min-h-[3.35rem] rounded-xl border border-blue bg-[#030303]/65 px-4 text-base text-text transition outline-none focus:border-lime focus:ring-2 focus:ring-lime/20"
												id="signup-email"
												name="email"
												type="email"
												autocomplete="email"
												maxlength="254"
												required
												bind:value={signupParticipant.email}
											/>
										</label>
									</div>
									{#if signupError}
										<p
											class="mt-5 border-l-2 border-[#ff7777] bg-[#2a0d12] px-4 py-3 text-[.84rem] leading-[1.5] text-[#ffb4b4]"
											role="alert"
										>
											{signupError}
										</p>
									{/if}
									<button
										class="mt-7 inline-flex min-h-[3.35rem] w-full cursor-pointer items-center justify-center gap-3 rounded-xl border-0 bg-lime px-6 py-3 text-[.88rem] font-extrabold text-[#090d11] transition hover:-translate-y-0.5 hover:brightness-[1.07] disabled:cursor-wait disabled:opacity-60"
										type="submit"
										disabled={checkingEmail}
									>
										{checkingEmail ? 'Đang kiểm tra email…' : 'Tiếp tục'}
										<ArrowRight class="size-5" aria-hidden="true" />
									</button>
								</form>
							</div>
						{:else}
							<div
								id="login-panel"
								role="tabpanel"
								in:fly={{ x: panelOffset, duration: transitionDuration }}
							>
								<form class="p-[clamp(1.4rem,5vw,2.5rem)]" method="POST" action="?/login">
									<p
										class="m-0 font-mono text-[.65rem] font-bold tracking-[.13em] text-blue uppercase"
									>
										XEM LẠI KẾT QUẢ
									</p>
									<h2 class="mt-4 mb-0 text-[2rem] leading-[1.05] font-[720] tracking-[-.04em]">
										Nhập thông tin đã đăng ký
									</h2>
									<div class="mt-7 grid gap-5">
										<label class="grid gap-2 text-[.78rem] font-semibold text-text" for="name">
											Họ và tên
											<input
												class="min-h-[3.35rem] rounded-xl border border-blue bg-[#030303]/65 px-4 text-base text-text transition outline-none placeholder:text-muted/55 focus:border-lime focus:ring-2 focus:ring-lime/20"
												id="name"
												name="name"
												type="text"
												autocomplete="name"
												value={form?.success === false ? form.name : ''}
												required
											/>
										</label>
										<label class="grid gap-2 text-[.78rem] font-semibold text-text" for="email">
											Địa chỉ email
											<input
												class="min-h-[3.35rem] rounded-xl border border-blue bg-[#030303]/65 px-4 text-base text-text transition outline-none placeholder:text-muted/55 focus:border-lime focus:ring-2 focus:ring-lime/20"
												id="email"
												name="email"
												type="email"
												autocomplete="email"
												value={form?.success === false ? form.email : ''}
												required
											/>
										</label>
									</div>

									{#if form?.success === false || restoreError}
										<p
											class="mt-5 border-l-2 border-[#ff7777] bg-[#2a0d12] px-4 py-3 text-[.84rem] leading-[1.5] text-[#ffb4b4]"
											role="alert"
										>
											{restoreError || form?.error}
										</p>
									{/if}

									<button
										class="mt-7 inline-flex min-h-[3.35rem] w-full cursor-pointer items-center justify-center gap-3 rounded-xl border-0 bg-lime px-6 py-3 text-[.88rem] font-extrabold text-[#090d11] transition hover:-translate-y-0.5 hover:brightness-[1.07] disabled:cursor-wait disabled:opacity-60"
										type="submit"
										disabled={restoring}
									>
										<LogIn class="size-4" aria-hidden="true" />
										{restoring ? 'Đang mở kết quả…' : 'Xem kết quả của tôi'}
									</button>
								</form>
							</div>
						{/if}
					{/key}
				</div>
			</div>
		</section>
	</div>
</main>
