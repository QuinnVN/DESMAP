import questionnaireData from './questions.json';

/** The six sections in the order used by the DESMAP assessment. */
export type StageId = 'D' | 'E' | 'S' | 'M' | 'A' | 'P';
export type OptionLetter = 'A' | 'B' | 'C';

export type QuestionOption = {
	letter: OptionLetter;
	text: string;
	score: number;
	scoreKey: string;
};

export type DesmapQuestion = {
	id: string;
	stage: StageId;
	dimension: string;
	/** Stage dimension such as S1; S questions additionally keep their sub-role in `dimension`. */
	dimensionGroup: string;
	prompt: string;
	options: QuestionOption[];
};

export type QuestionnaireStage = {
	id: StageId;
	label: string;
	shortLabel: string;
	subtitle: string;
	dimensionIds: string[];
	questions: DesmapQuestion[];
};

export type QuestionnairePresentationOrder = {
	questionIds: string[];
	optionLettersByQuestion: Record<string, OptionLetter[]>;
};

export type CareerInterest = {
	id: string;
	label: string;
	description: string;
};

export const careerInterestOptions: CareerInterest[] = [
	{
		id: 'technology-engineering',
		label: 'Công nghệ & kỹ thuật',
		description: 'Xây dựng hệ thống, sản phẩm và giải pháp kỹ thuật.'
	},
	{
		id: 'science-research',
		label: 'Khoa học & nghiên cứu',
		description: 'Khám phá câu hỏi, bằng chứng và cách thế giới vận hành.'
	},
	{
		id: 'design-creative',
		label: 'Thiết kế & sáng tạo',
		description: 'Định hình ý tưởng, trải nghiệm, hình ảnh và tác phẩm nguyên bản.'
	},
	{
		id: 'business-entrepreneurship',
		label: 'Kinh doanh & khởi nghiệp',
		description: 'Phát triển tổ chức, sản phẩm và dự án mới.'
	},
	{
		id: 'people-education',
		label: 'Con người & giáo dục',
		description: 'Giảng dạy, cố vấn và giúp mọi người phát triển.'
	},
	{
		id: 'health-wellbeing',
		label: 'Sức khỏe & hạnh phúc',
		description: 'Hỗ trợ sức khỏe, chăm sóc và chất lượng cuộc sống.'
	},
	{
		id: 'law-public-service',
		label: 'Luật & dịch vụ công',
		description: 'Làm việc với chính sách, công lý và lợi ích cộng đồng.'
	},
	{
		id: 'media-communication',
		label: 'Truyền thông & giao tiếp',
		description: 'Kết nối mọi người qua ngôn ngữ, câu chuyện và thông tin.'
	},
	{
		id: 'environment-sustainability',
		label: 'Môi trường & bền vững',
		description: 'Bảo vệ không gian, tài nguyên và tương lai dài hạn.'
	},
	{
		id: 'operations-trades',
		label: 'Vận hành & nghề kỹ thuật',
		description: 'Thực hiện công việc thực tế, dịch vụ và hệ thống đáng tin cậy.'
	},
	{
		id: 'exploring',
		label: 'Tôi vẫn đang khám phá',
		description: 'Giữ lựa chọn rộng mở khi tôi hiểu thêm về bản thân.'
	}
];

const stageMeta: Record<StageId, Omit<QuestionnaireStage, 'questions'>> = {
	D: {
		id: 'D',
		shortLabel: 'MONG MUỐN',
		label: 'Desire - Mong muốn',
		subtitle: 'Điều bạn mong công việc mang lại',
		dimensionIds: ['D1', 'D2', 'D3', 'D4', 'D5', 'D6']
	},
	E: {
		id: 'E',
		shortLabel: 'CHUYÊN MÔN',
		label: 'Expertise -Chuyên môn',
		subtitle: 'Những điểm mạnh bạn muốn phát huy',
		dimensionIds: ['E1', 'E2', 'E3', 'E4', 'E5', 'E6']
	},
	S: {
		id: 'S',
		shortLabel: 'VAI TRÒ XÃ HỘI',
		label: 'Social Role - Vai trò xã hội',
		subtitle: 'Cách bạn thường đóng góp cùng người khác',
		dimensionIds: ['S1', 'S2', 'S3']
	},
	M: {
		id: 'M',
		shortLabel: 'TƯ DUY',
		label: 'Mindset - Tư duy',
		subtitle: 'Cách bạn xử lý thông tin và quyết định',
		dimensionIds: ['M1', 'M2', 'M3']
	},
	A: {
		id: 'A',
		shortLabel: 'THÍCH ỨNG',
		label: 'Adaptability - Khả năng thích ứng',
		subtitle: 'Cách bạn đón nhận thay đổi',
		dimensionIds: ['A1', 'A2', 'A3', 'A4']
	},
	P: {
		id: 'P',
		shortLabel: 'ÁP LỰC',
		label: 'Pressure - Phản ứng với áp lực',
		subtitle: 'Cách bạn phản ứng khi công việc trở nên căng thẳng',
		dimensionIds: ['P1', 'P2', 'P3', 'P4', 'P5', 'P6']
	}
};

const stageIds: StageId[] = ['D', 'E', 'S', 'M', 'A', 'P'];
export const desmapQuestions = questionnaireData as DesmapQuestion[];
export const totalQuestionCount = desmapQuestions.length;

export const questionnaireStages: QuestionnaireStage[] = stageIds.map((id) => ({
	...stageMeta[id],
	questions: desmapQuestions.filter((question) => question.stage === id)
}));

function shuffled<T>(values: readonly T[], random: () => number): T[] {
	const result = [...values];
	for (let index = result.length - 1; index > 0; index -= 1) {
		const swapIndex = Math.floor(random() * (index + 1));
		[result[index], result[swapIndex]] = [result[swapIndex], result[index]];
	}
	return result;
}

/** Create one display order for a questionnaire attempt. */
export function createQuestionnairePresentationOrder(
	random: () => number = Math.random
): QuestionnairePresentationOrder {
	return {
		questionIds: stageIds.flatMap((stageId) =>
			shuffled(
				desmapQuestions
					.filter((question) => question.stage === stageId)
					.map((question) => question.id),
				random
			)
		),
		optionLettersByQuestion: Object.fromEntries(
			desmapQuestions.map((question) => [
				question.id,
				shuffled(
					question.options.map((option) => option.letter),
					random
				)
			])
		)
	};
}

export function questionsInPresentationOrder(
	order: QuestionnairePresentationOrder
): DesmapQuestion[] {
	return order.questionIds.map((id) => getQuestionById(id) as DesmapQuestion);
}

export function optionsInPresentationOrder(
	question: DesmapQuestion,
	order: QuestionnairePresentationOrder
): QuestionOption[] {
	return order.optionLettersByQuestion[question.id].map(
		(letter) => question.options.find((option) => option.letter === letter) as QuestionOption
	);
}

export function getQuestionById(id: string): DesmapQuestion | undefined {
	return desmapQuestions.find((question) => question.id === id);
}

export function getStageById(id: StageId): QuestionnaireStage {
	return questionnaireStages.find((stage) => stage.id === id) as QuestionnaireStage;
}

export type QuestionnaireAnswers = Record<string, OptionLetter>;

export type ParticipantDetails = {
	name: string;
	email: string;
};

export type DimensionScore = {
	raw: number;
	max: number;
	percent: number;
	answered: number;
	questionCount: number;
};

export type QuestionnaireScores = {
	overall: DimensionScore;
	dimensions: Record<string, DimensionScore>;
	groups: Record<string, DimensionScore>;
	stages: Record<StageId, DimensionScore>;
};

function percentage(raw: number, max: number): number {
	return max ? Math.round((raw / max) * 100) : 0;
}

/** Score only the submitted answers; unanswered questions remain visible in `answered`. */
export function scoreAnswers(answers: QuestionnaireAnswers): QuestionnaireScores {
	const dimensionTotals = new Map<
		string,
		{ raw: number; answered: number; questionCount: number }
	>();
	const groupTotals = new Map<string, { raw: number; answered: number; questionCount: number }>();
	const stageTotals = new Map<StageId, { raw: number; answered: number; questionCount: number }>();
	let raw = 0;
	let answered = 0;

	for (const question of desmapQuestions) {
		const dimension = dimensionTotals.get(question.dimension) ?? {
			raw: 0,
			answered: 0,
			questionCount: 0
		};
		dimension.questionCount += 1;
		const group = groupTotals.get(question.dimensionGroup) ?? {
			raw: 0,
			answered: 0,
			questionCount: 0
		};
		group.questionCount += 1;
		const stage = stageTotals.get(question.stage) ?? {
			raw: 0,
			answered: 0,
			questionCount: 0
		};
		stage.questionCount += 1;
		const selected = question.options.find((option) => option.letter === answers[question.id]);
		if (selected) {
			raw += selected.score;
			answered += 1;
			dimension.raw += selected.score;
			dimension.answered += 1;
			group.raw += selected.score;
			group.answered += 1;
			stage.raw += selected.score;
			stage.answered += 1;
		}
		dimensionTotals.set(question.dimension, dimension);
		groupTotals.set(question.dimensionGroup, group);
		stageTotals.set(question.stage, stage);
	}

	const toScore = (item: {
		raw: number;
		answered: number;
		questionCount: number;
	}): DimensionScore => ({
		raw: item.raw,
		max: item.questionCount * 2,
		percent: percentage(item.raw, item.questionCount * 2),
		answered: item.answered,
		questionCount: item.questionCount
	});

	const dimensions: Record<string, DimensionScore> = {};
	for (const [key, value] of dimensionTotals) dimensions[key] = toScore(value);
	const groups: Record<string, DimensionScore> = {};
	for (const [key, value] of groupTotals) groups[key] = toScore(value);
	const stages = {} as Record<StageId, DimensionScore>;
	for (const stage of stageIds)
		stages[stage] = toScore(stageTotals.get(stage) ?? { raw: 0, answered: 0, questionCount: 0 });

	return {
		overall: {
			raw,
			max: totalQuestionCount * 2,
			percent: percentage(raw, totalQuestionCount * 2),
			answered,
			questionCount: totalQuestionCount
		},
		dimensions,
		groups,
		stages
	};
}

export type QuestionnaireSubmission = {
	version: 1;
	completed: true;
	assessmentId: string;
	completedAt: string;
	startedAt: string;
	participant: ParticipantDetails;
	careerInterests: string[];
	answers: QuestionnaireAnswers;
	scores: QuestionnaireScores;
};

export type QuestionnaireDraft = {
	version: 1;
	completed: false;
	step: 'participant' | 'career' | 'questions' | 'review';
	currentIndex: number;
	startedAt: string;
	updatedAt: string;
	participant: ParticipantDetails;
	careerInterests: string[];
	answers: QuestionnaireAnswers;
	presentationOrder: QuestionnairePresentationOrder;
};

export function buildCompletionPayload(input: {
	answers: QuestionnaireAnswers;
	careerInterests: string[];
	startedAt: string;
	participant: ParticipantDetails;
	completedAt?: string;
	assessmentId?: string;
}): QuestionnaireSubmission {
	if (desmapQuestions.some((question) => !isOptionLetter(input.answers[question.id]))) {
		throw new Error('Cannot create a completed DESMAP payload until every question has an answer.');
	}
	const completedAt = input.completedAt ?? new Date().toISOString();
	const participant = normalizeParticipantDetails(input.participant);
	if (!isValidParticipantDetails(participant)) {
		throw new Error('Cannot create a completed DESMAP payload without valid participant details.');
	}
	return {
		version: 1,
		completed: true,
		assessmentId: input.assessmentId ?? createAssessmentId(),
		completedAt,
		startedAt: input.startedAt,
		participant,
		careerInterests: [...input.careerInterests],
		answers: { ...input.answers },
		scores: scoreAnswers(input.answers)
	};
}

export function createAssessmentId(randomUUID: () => string = () => crypto.randomUUID()): string {
	return `assessment-${randomUUID()}`;
}

export const QUESTIONNAIRE_STORAGE_KEY = 'desmap:questionnaire:v1';
export const QUESTIONNAIRE_COMPLETION_STORAGE_KEY = 'desmap:questionnaire:completed:v1';
export const QUESTIONNAIRE_SYNC_STORAGE_KEY = 'desmap:questionnaire:sync:v1';

export type QuestionnaireSyncStatus = {
	assessmentId: string;
	status: 'pending' | 'synced' | 'error';
	message?: string;
	recoverable?: boolean;
};

export function normalizeEmail(email: string): string {
	return email.trim().toLowerCase();
}

export function normalizeParticipantDetails(details: ParticipantDetails): ParticipantDetails {
	return { name: details.name.trim(), email: normalizeEmail(details.email) };
}

export function isValidParticipantDetails(details: ParticipantDetails): boolean {
	return (
		details.name.length >= 2 &&
		details.name.length <= 100 &&
		!/[\u0000-\u001f\u007f]/.test(details.name) &&
		details.email.length <= 254 &&
		/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(details.email)
	);
}

function isOptionLetter(value: unknown): value is OptionLetter {
	return value === 'A' || value === 'B' || value === 'C';
}

function isPresentationOrder(value: unknown): value is QuestionnairePresentationOrder {
	if (!value || typeof value !== 'object') return false;
	const order = value as Partial<QuestionnairePresentationOrder>;
	if (!Array.isArray(order.questionIds)) return false;
	if (
		order.questionIds.length !== totalQuestionCount ||
		new Set(order.questionIds).size !== totalQuestionCount ||
		order.questionIds.some((id, index) => {
			const question = typeof id === 'string' ? getQuestionById(id) : undefined;
			return !question || question.stage !== desmapQuestions[index]?.stage;
		})
	)
		return false;
	if (!order.optionLettersByQuestion || typeof order.optionLettersByQuestion !== 'object')
		return false;
	return desmapQuestions.every((question) => {
		const letters = order.optionLettersByQuestion?.[question.id];
		return (
			Array.isArray(letters) &&
			letters.length === question.options.length &&
			new Set(letters).size === question.options.length &&
			letters.every((letter) => question.options.some((option) => option.letter === letter))
		);
	});
}

function parseDraft(value: unknown): QuestionnaireDraft | null {
	if (!value || typeof value !== 'object') return null;
	const draft = value as Partial<QuestionnaireDraft>;
	if (
		draft.version !== 1 ||
		draft.completed !== false ||
		!draft.participant ||
		typeof draft.participant !== 'object' ||
		!Array.isArray(draft.careerInterests) ||
		typeof draft.answers !== 'object'
	)
		return null;
	if (!draft.answers || typeof draft.startedAt !== 'string' || typeof draft.updatedAt !== 'string')
		return null;
	if (
		draft.step !== 'participant' &&
		draft.step !== 'career' &&
		draft.step !== 'questions' &&
		draft.step !== 'review'
	)
		return null;
	const participant = normalizeParticipantDetails(draft.participant as ParticipantDetails);
	if (!isValidParticipantDetails(participant)) return null;
	const currentIndex = draft.currentIndex;
	if (
		typeof currentIndex !== 'number' ||
		!Number.isInteger(currentIndex) ||
		currentIndex < 0 ||
		currentIndex >= totalQuestionCount
	)
		return null;
	if (
		!Object.entries(draft.answers).every(
			([id, answer]) => Boolean(getQuestionById(id)) && isOptionLetter(answer)
		)
	)
		return null;

	return {
		...(draft as Omit<QuestionnaireDraft, 'presentationOrder'>),
		participant,
		presentationOrder: isPresentationOrder(draft.presentationOrder)
			? draft.presentationOrder
			: createQuestionnairePresentationOrder()
	};
}

/** Browser-only, failure-tolerant draft persistence. Safe to call during SSR. */
export function readSavedQuestionnaire(): QuestionnaireDraft | null {
	if (typeof window === 'undefined') return null;
	try {
		const raw = window.localStorage.getItem(QUESTIONNAIRE_STORAGE_KEY);
		if (!raw) return null;
		const parsed: unknown = JSON.parse(raw);
		return parseDraft(parsed);
	} catch {
		return null;
	}
}

export function writeSavedQuestionnaire(draft: QuestionnaireDraft): boolean {
	if (typeof window === 'undefined') return false;
	try {
		window.localStorage.setItem(QUESTIONNAIRE_STORAGE_KEY, JSON.stringify(draft));
		return true;
	} catch {
		return false;
	}
}

export function clearSavedQuestionnaire(): void {
	if (typeof window === 'undefined') return;
	try {
		window.localStorage.removeItem(QUESTIONNAIRE_STORAGE_KEY);
	} catch {
		// Storage can be disabled by privacy settings; completion should still work in memory.
	}
}

/** Store the latest completed payload for the client-side evaluation route. */
export function writeCompletionPayload(payload: QuestionnaireSubmission): boolean {
	if (typeof window === 'undefined') return false;
	try {
		window.localStorage.setItem(QUESTIONNAIRE_COMPLETION_STORAGE_KEY, JSON.stringify(payload));
		return true;
	} catch {
		return false;
	}
}

export function readQuestionnaireSyncStatus(assessmentId: string): QuestionnaireSyncStatus | null {
	if (typeof window === 'undefined') return null;
	try {
		const parsed = JSON.parse(
			window.localStorage.getItem(QUESTIONNAIRE_SYNC_STORAGE_KEY) ?? 'null'
		) as Partial<QuestionnaireSyncStatus> | null;
		if (
			!parsed ||
			parsed.assessmentId !== assessmentId ||
			(parsed.status !== 'pending' && parsed.status !== 'synced' && parsed.status !== 'error')
		)
			return null;
		return parsed as QuestionnaireSyncStatus;
	} catch {
		return null;
	}
}

export function writeQuestionnaireSyncStatus(status: QuestionnaireSyncStatus): boolean {
	if (typeof window === 'undefined') return false;
	try {
		window.localStorage.setItem(QUESTIONNAIRE_SYNC_STORAGE_KEY, JSON.stringify(status));
		return true;
	} catch {
		return false;
	}
}

const assessmentIdPattern =
	/^assessment-[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

export function parseCompletionPayload(
	value: unknown,
	createId: () => string = createAssessmentId
): QuestionnaireSubmission | null {
	if (!value || typeof value !== 'object') return null;
	const parsed = value as Partial<QuestionnaireSubmission>;
	if (
		parsed.version !== 1 ||
		parsed.completed !== true ||
		!parsed.answers ||
		!parsed.participant ||
		!Array.isArray(parsed.careerInterests) ||
		typeof parsed.completedAt !== 'string' ||
		typeof parsed.startedAt !== 'string'
	)
		return null;
	const participant = normalizeParticipantDetails(parsed.participant as ParticipantDetails);
	if (!isValidParticipantDetails(participant)) return null;
	const startedAt = Date.parse(parsed.startedAt);
	const completedAt = Date.parse(parsed.completedAt);
	if (!Number.isFinite(startedAt) || !Number.isFinite(completedAt) || completedAt < startedAt)
		return null;
	if (
		parsed.careerInterests.length < 1 ||
		parsed.careerInterests.length > 3 ||
		new Set(parsed.careerInterests).size !== parsed.careerInterests.length ||
		parsed.careerInterests.some(
			(id) => !careerInterestOptions.some((interest) => interest.id === id)
		)
	)
		return null;
	const answers = parsed.answers;
	if (
		desmapQuestions.some((question) => !isOptionLetter(answers[question.id])) ||
		Object.keys(answers).length !== totalQuestionCount
	)
		return null;
	if (parsed.assessmentId !== undefined && !assessmentIdPattern.test(parsed.assessmentId))
		return null;
	const assessmentId = parsed.assessmentId ?? createId();
	if (!assessmentIdPattern.test(assessmentId)) return null;
	return {
		version: 1,
		completed: true,
		assessmentId,
		completedAt: parsed.completedAt,
		startedAt: parsed.startedAt,
		participant,
		careerInterests: [...parsed.careerInterests],
		answers: { ...answers },
		scores: scoreAnswers(answers)
	};
}

export function readCompletionPayload(): QuestionnaireSubmission | null {
	if (typeof window === 'undefined') return null;
	try {
		const raw = window.localStorage.getItem(QUESTIONNAIRE_COMPLETION_STORAGE_KEY);
		if (!raw) return null;
		const parsed: unknown = JSON.parse(raw);
		const payload = parseCompletionPayload(parsed);
		if (
			payload &&
			(parsed as Partial<QuestionnaireSubmission>).assessmentId !== payload.assessmentId
		) {
			writeCompletionPayload(payload);
		}
		return payload;
	} catch {
		return null;
	}
}
