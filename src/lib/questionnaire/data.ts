import source from '../../../DESMAP-QUESTION.md?raw';

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

export type CareerInterest = {
	id: string;
	label: string;
	description: string;
};

export const careerInterestOptions: CareerInterest[] = [
	{ id: 'technology-engineering', label: 'Công nghệ & kỹ thuật', description: 'Xây dựng hệ thống, sản phẩm và giải pháp kỹ thuật.' },
	{ id: 'science-research', label: 'Khoa học & nghiên cứu', description: 'Khám phá câu hỏi, bằng chứng và cách thế giới vận hành.' },
	{ id: 'design-creative', label: 'Thiết kế & sáng tạo', description: 'Định hình ý tưởng, trải nghiệm, hình ảnh và tác phẩm nguyên bản.' },
	{ id: 'business-entrepreneurship', label: 'Kinh doanh & khởi nghiệp', description: 'Phát triển tổ chức, sản phẩm và dự án mới.' },
	{ id: 'people-education', label: 'Con người & giáo dục', description: 'Giảng dạy, cố vấn và giúp mọi người phát triển.' },
	{ id: 'health-wellbeing', label: 'Sức khỏe & hạnh phúc', description: 'Hỗ trợ sức khỏe, chăm sóc và chất lượng cuộc sống.' },
	{ id: 'law-public-service', label: 'Luật & dịch vụ công', description: 'Làm việc với chính sách, công lý và lợi ích cộng đồng.' },
	{ id: 'media-communication', label: 'Truyền thông & giao tiếp', description: 'Kết nối mọi người qua ngôn ngữ, câu chuyện và thông tin.' },
	{ id: 'environment-sustainability', label: 'Môi trường & bền vững', description: 'Bảo vệ không gian, tài nguyên và tương lai dài hạn.' },
	{ id: 'operations-trades', label: 'Vận hành & nghề kỹ thuật', description: 'Thực hiện công việc thực tế, dịch vụ và hệ thống đáng tin cậy.' },
	{ id: 'exploring', label: 'Tôi vẫn đang khám phá', description: 'Giữ lựa chọn rộng mở khi tôi hiểu thêm về bản thân.' }
];

const stageMeta: Record<StageId, Omit<QuestionnaireStage, 'questions'>> = {
	D: {
		id: 'D',
		shortLabel: 'MONG MUỐN',
		label: 'Mong muốn & định hướng nghề nghiệp',
		subtitle: 'Điều bạn mong công việc mang lại',
		dimensionIds: ['D1', 'D2', 'D3', 'D4', 'D5', 'D6']
	},
	E: {
		id: 'E',
		shortLabel: 'CHUYÊN MÔN',
		label: 'Chuyên môn',
		subtitle: 'Những điểm mạnh bạn muốn phát huy',
		dimensionIds: ['E1', 'E2', 'E3', 'E4', 'E5', 'E6']
	},
	S: {
		id: 'S',
		shortLabel: 'VAI TRÒ XÃ HỘI',
		label: 'Vai trò xã hội',
		subtitle: 'Cách bạn thường đóng góp cùng người khác',
		dimensionIds: ['S1', 'S2', 'S3']
	},
	M: {
		id: 'M',
		shortLabel: 'TƯ DUY',
		label: 'Tư duy',
		subtitle: 'Cách bạn xử lý thông tin và quyết định',
		dimensionIds: ['M1', 'M2', 'M3']
	},
	A: {
		id: 'A',
		shortLabel: 'THÍCH ỨNG',
		label: 'Khả năng thích ứng',
		subtitle: 'Cách bạn đón nhận thay đổi',
		dimensionIds: ['A1', 'A2', 'A3', 'A4']
	},
	P: {
		id: 'P',
		shortLabel: 'ÁP LỰC',
		label: 'Phản ứng với áp lực',
		subtitle: 'Cách bạn phản ứng khi công việc trở nên căng thẳng',
		dimensionIds: ['P1', 'P2', 'P3', 'P4', 'P5', 'P6']
	}
};

const stageIds: StageId[] = ['D', 'E', 'S', 'M', 'A', 'P'];
const optionPattern = /^\s*(?:\*\*)?([ABC])\.(?:\*\*)?\s*(.*?)\s*→\s*(?:`([^`]+)`|(.+?))\s*$/u;
const questionHeadingPattern = /^\s*#{1,6}\s+\*\*([A-Z]\d+\.\d+)(?:\s+—[^*]*)?\*\*\s*$/u;
const inlineQuestionPattern = /^\s*\*\*([A-Z]\d+\.\d+)\*\*\s+(.+?)\s*$/u;
const bareQuestionPattern = /^\s*\*\*([A-Z]\d+\.\d+)\*\*\s*$/u;

function cleanMarkdown(value: string): string {
	return value
		.replace(/\*\*/g, '')
		.replace(/\\([.*_#-])/g, '$1')
		.replace(/\s+/g, ' ')
		.trim();
}

function startQuestion(id: string, inlinePrompt = ''): { id: string; prompt: string; options: QuestionOption[] } {
	return { id, prompt: cleanMarkdown(inlinePrompt), options: [] };
}

function parseScoreToken(token: string): { scoreKey: string; score: number } | undefined {
	const match = token.trim().replace(/\\\+/g, '+').match(/^(.+?)\s*\+\s*([012])$/u);
	if (!match) return undefined;
	return { scoreKey: cleanMarkdown(match[1]), score: Number(match[2]) };
}

/**
 * Parse the canonical DESMAP-QUESTION.md source into renderable question data.
 * Keeping this parser beside the source means the UI cannot silently drift from
 * the documented questions when wording or option order changes.
 */
export function parseQuestionSource(markdown: string): DesmapQuestion[] {
	const parsed: Array<{ id: string; prompt: string; options: QuestionOption[] }> = [];
	let current: { id: string; prompt: string; options: QuestionOption[] } | undefined;

	const flush = () => {
		if (current) parsed.push(current);
		current = undefined;
	};

	for (const rawLine of markdown.split(/\r?\n/u)) {
		const heading = rawLine.match(questionHeadingPattern);
		const inline = rawLine.match(inlineQuestionPattern);
		const bare = rawLine.match(bareQuestionPattern);
		if (heading || inline || bare) {
			flush();
			current = startQuestion(heading?.[1] ?? inline?.[1] ?? bare?.[1] ?? '', inline?.[2] ?? '');
			continue;
		}
		// A few explanatory examples sit between sections in the source. A
		// top-level heading closes the active question before those examples.
		if (current && /^\s*#\s/u.test(rawLine)) {
			flush();
		}

		const option = rawLine.match(optionPattern);
		if (current && option) {
			const scoring = parseScoreToken(option[3] ?? option[4]);
			if (scoring) {
				current.options.push({
					letter: option[1] as OptionLetter,
					text: cleanMarkdown(option[2]),
					score: scoring.score,
					scoreKey: scoring.scoreKey
				});
			}
			continue;
		}

		if (current && !current.prompt && rawLine.trim() && !rawLine.trim().startsWith('#')) {
			current.prompt = cleanMarkdown(rawLine);
		}
	}
	flush();

	return parsed.map((question) => {
		const stage = question.id.slice(0, 1) as StageId;
		const dimension = question.options[0]?.scoreKey ?? question.id.split('.')[0];
		return { ...question, stage, dimension, dimensionGroup: question.id.split('.')[0] };
	});
}

const parsedQuestions = parseQuestionSource(source);
const expectedCounts: Record<StageId, number> = { D: 18, E: 18, S: 20, M: 15, A: 12, P: 18 };

for (const stage of stageIds) {
	const count = parsedQuestions.filter((question) => question.stage === stage).length;
	if (count !== expectedCounts[stage]) {
		throw new Error(`DESMAP source is incomplete for ${stage}: expected ${expectedCounts[stage]}, received ${count}`);
	}
}

if (parsedQuestions.some((question) => question.options.length !== 3 || !question.prompt)) {
	throw new Error('Every DESMAP question must have a prompt and exactly three scored options.');
}

export const desmapQuestions: DesmapQuestion[] = parsedQuestions;
export const totalQuestionCount = desmapQuestions.length;

export const questionnaireStages: QuestionnaireStage[] = stageIds.map((id) => ({
	...stageMeta[id],
	questions: desmapQuestions.filter((question) => question.stage === id)
}));

export function getQuestionById(id: string): DesmapQuestion | undefined {
	return desmapQuestions.find((question) => question.id === id);
}

export function getStageById(id: StageId): QuestionnaireStage {
	return questionnaireStages.find((stage) => stage.id === id) as QuestionnaireStage;
}

export type QuestionnaireAnswers = Record<string, OptionLetter>;

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
	const dimensionTotals = new Map<string, { raw: number; answered: number; questionCount: number }>();
	const groupTotals = new Map<string, { raw: number; answered: number; questionCount: number }>();
	const stageTotals = new Map<StageId, { raw: number; answered: number; questionCount: number }>();
	let raw = 0;
	let answered = 0;

	for (const question of desmapQuestions) {
		const dimension = dimensionTotals.get(question.dimension) ?? { raw: 0, answered: 0, questionCount: 0 };
		dimension.questionCount += 1;
		const group = groupTotals.get(question.dimensionGroup) ?? { raw: 0, answered: 0, questionCount: 0 };
		group.questionCount += 1;
		const stage = stageTotals.get(question.stage) ?? { raw: 0, answered: 0, questionCount: 0 };
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

	const toScore = (item: { raw: number; answered: number; questionCount: number }): DimensionScore => ({
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
	for (const stage of stageIds) stages[stage] = toScore(stageTotals.get(stage) ?? { raw: 0, answered: 0, questionCount: 0 });

	return {
		overall: { raw, max: totalQuestionCount * 2, percent: percentage(raw, totalQuestionCount * 2), answered, questionCount: totalQuestionCount },
		dimensions,
		groups,
		stages
	};
}

export type QuestionnaireSubmission = {
	version: 1;
	completed: true;
	completedAt: string;
	startedAt: string;
	careerInterests: string[];
	answers: QuestionnaireAnswers;
	scores: QuestionnaireScores;
};

export type QuestionnaireDraft = {
	version: 1;
	completed: false;
	step: 'career' | 'questions' | 'review';
	currentIndex: number;
	startedAt: string;
	updatedAt: string;
	careerInterests: string[];
	answers: QuestionnaireAnswers;
};

export function buildCompletionPayload(input: {
	answers: QuestionnaireAnswers;
	careerInterests: string[];
	startedAt: string;
	completedAt?: string;
}): QuestionnaireSubmission {
	if (desmapQuestions.some((question) => !isOptionLetter(input.answers[question.id]))) {
		throw new Error('Cannot create a completed DESMAP payload until every question has an answer.');
	}
	const completedAt = input.completedAt ?? new Date().toISOString();
	return {
		version: 1,
		completed: true,
		completedAt,
		startedAt: input.startedAt,
		careerInterests: [...input.careerInterests],
		answers: { ...input.answers },
		scores: scoreAnswers(input.answers)
	};
}

export const QUESTIONNAIRE_STORAGE_KEY = 'desmap:questionnaire:v1';
export const QUESTIONNAIRE_COMPLETION_STORAGE_KEY = 'desmap:questionnaire:completed:v1';

function isOptionLetter(value: unknown): value is OptionLetter {
	return value === 'A' || value === 'B' || value === 'C';
}

function isDraft(value: unknown): value is QuestionnaireDraft {
	if (!value || typeof value !== 'object') return false;
	const draft = value as Partial<QuestionnaireDraft>;
	if (draft.version !== 1 || draft.completed !== false || !Array.isArray(draft.careerInterests) || typeof draft.answers !== 'object') return false;
	if (!draft.answers || typeof draft.startedAt !== 'string' || typeof draft.updatedAt !== 'string') return false;
	if (draft.step !== 'career' && draft.step !== 'questions' && draft.step !== 'review') return false;
	const currentIndex = draft.currentIndex;
	if (typeof currentIndex !== 'number' || !Number.isInteger(currentIndex) || currentIndex < 0 || currentIndex >= totalQuestionCount) return false;
	return Object.entries(draft.answers).every(([id, answer]) => Boolean(getQuestionById(id)) && isOptionLetter(answer));
}

/** Browser-only, failure-tolerant draft persistence. Safe to call during SSR. */
export function readSavedQuestionnaire(): QuestionnaireDraft | null {
	if (typeof window === 'undefined') return null;
	try {
		const raw = window.localStorage.getItem(QUESTIONNAIRE_STORAGE_KEY);
		if (!raw) return null;
		const parsed: unknown = JSON.parse(raw);
		return isDraft(parsed) ? parsed : null;
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

export function readCompletionPayload(): QuestionnaireSubmission | null {
	if (typeof window === 'undefined') return null;
	try {
		const raw = window.localStorage.getItem(QUESTIONNAIRE_COMPLETION_STORAGE_KEY);
		if (!raw) return null;
		const parsed = JSON.parse(raw) as Partial<QuestionnaireSubmission>;
		if (parsed.version !== 1 || parsed.completed !== true || !parsed.scores || !parsed.answers || !Array.isArray(parsed.careerInterests)) return null;
		if (typeof parsed.completedAt !== 'string' || typeof parsed.startedAt !== 'string') return null;
		const answers = parsed.answers;
		if (desmapQuestions.some((question) => !isOptionLetter(answers[question.id])) || Object.keys(answers).length !== totalQuestionCount) return null;
		return parsed as QuestionnaireSubmission;
	} catch {
		return null;
	}
}
