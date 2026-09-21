import doctorImage from './assets/doctor.png';
import lawyerImage from './assets/lawyer.png';
import teacherImage from './assets/teacher.png';
import salesImage from './assets/sales.png';
import engineeringImage from './assets/engineering.png';

import type {
	QuestionnaireDraft,
	QuestionnaireScores,
	QuestionnaireSubmission,
	StageId
} from '$lib/questionnaire';
import type { DimensionLevelId } from './final-dimension-content';

export type EvaluationStatus = 'completed' | 'sample';

export type FinalAssessment = {
	version: 1;
	assessmentId: string;
	completedAt: string;
	stageAssessments?: Partial<Record<StageId, string>>;
	dimensionLevels?: Partial<Record<string, DimensionLevelId>>;
	behaviourComparison?: BehaviourComparisonResult;
	careerSuggestions?: FinalCareerSuggestion[];
};

export type BehaviourComparisonKind = 'confirmed' | 'emerging' | 'development';

export type BehaviourComparisonFinding = {
	id: string;
	kind: BehaviourComparisonKind;
	title: string;
	questionnaireResult: string;
	vrEvidence: string;
	summary: string;
};

export type BehaviourComparisonResult = {
	experienceName: string;
	isPlaceholder?: boolean;
	findings: BehaviourComparisonFinding[];
};

export const placeholderBehaviourComparison: BehaviourComparisonResult = {
	experienceName: 'Bác sĩ cấp cứu',
	isPlaceholder: true,
	findings: [
		{
			id: 'analytical-thinking',
			kind: 'confirmed',
			title: 'Tư duy phân tích',
			questionnaireResult: 'Bạn tự đánh giá cao khả năng phân tích thông tin trước khi hành động.',
			vrEvidence: 'Trong VR, bạn kiểm tra dữ kiện chính trước khi chọn thứ tự ưu tiên.',
			summary: 'Kết quả VR củng cố điểm mạnh đã thể hiện trong bảng câu hỏi.'
		},
		{
			id: 'adaptability',
			kind: 'emerging',
			title: 'Khả năng thích ứng',
			questionnaireResult: 'Bạn chưa xem đây là một điểm mạnh nổi bật của mình.',
			vrEvidence: 'Bạn đổi hướng xử lý phù hợp khi tình huống xuất hiện thông tin mới.',
			summary: 'Đây có thể là năng lực bạn chưa nhận ra đầy đủ qua tự đánh giá.'
		},
		{
			id: 'pressure-prioritisation',
			kind: 'development',
			title: 'Ưu tiên khi chịu áp lực',
			questionnaireResult:
				'Bạn cho rằng mình duy trì quyết định ổn định khi thời gian bị giới hạn.',
			vrEvidence: 'Trong nhiệm vụ dồn dập, bạn mất thêm thời gian để chốt việc cần làm trước.',
			summary: 'Bạn nên luyện cách xác định ưu tiên trước khi xử lý các chi tiết còn lại.'
		}
	]
};

export type FinalCareerSuggestion = {
	id: string;
	name: string;
	compatibilityPercent: number;
	description: string;
};

export type EvaluationPageState = 'initial' | 'final';

export function evaluationPageState(
	submission: Pick<QuestionnaireSubmission, 'assessmentId'> | null,
	finalAssessment: Pick<FinalAssessment, 'assessmentId'> | null
): EvaluationPageState {
	return submission && finalAssessment?.assessmentId === submission.assessmentId
		? 'final'
		: 'initial';
}

export type ProfileRow = {
	code: StageId;
	label: string;
	value: string;
	detail: string;
};

export type CareerMatch = {
	label: string;
	percent: number;
	accent: 'lime' | 'blue';
};

export type EvaluationModel = {
	status: EvaluationStatus;
	completedAt?: string;
	targetCareer: string;
	targetExperienceSlug: string;
	profileRows: ProfileRow[];
	stageScores: Record<StageId, number>;
	careerMatches: CareerMatch[];
	strengths: string[];
	gaps: string[];
	roadmap: string[];
	observations: Array<{ self: string; observed: string }>;
};

export type ExperienceCategory = 'Y tế' | 'Luật' | 'Giáo dục' | 'Kinh doanh' | 'Kỹ thuật';

export type CareerExperience = {
	slug: string;
	title: string;
	category: ExperienceCategory;
	image: string;
	status: 'recommended' | 'ready' | 'locked';
	duration: string;
	missionCount: number;
	description: string;
	observedFactors: string[];
	missions: string[];
	whyItFits: string;
};

export const experiences: CareerExperience[] = [
	{
		slug: 'doctor',
		title: 'Bác sĩ',
		category: 'Y tế',
		image: doctorImage,
		status: 'recommended',
		duration: '10 phút',
		missionCount: 2,
		description: 'Phân loại bệnh nhân và xác định thông tin quan trọng trong áp lực.',
		observedFactors: ['Tốc độ quyết định', 'Ưu tiên', 'Phản ứng với áp lực'],
		missions: [
			'Phân loại ba bệnh nhân khi có thông tin mới',
			'Trao đổi ưu tiên điều trị với nhóm của bạn'
		],
		whyItFits:
			'Tình huống này giúp người tham gia tìm hiểu công việc phân loại và trao đổi ưu tiên điều trị.'
	},
	{
		slug: 'lawyer',
		title: 'Luật sư',
		category: 'Luật',
		image: lawyerImage,
		status: 'ready',
		duration: '12 phút',
		missionCount: 2,
		description: 'Phân tích vụ việc và đưa ra khuyến nghị hợp lý trong thời hạn gấp.',
		observedFactors: ['Tổng hợp bằng chứng', 'Tự tin quyết định', 'Giao tiếp khi căng thẳng'],
		missions: [
			'Tách bằng chứng liên quan khỏi hồ sơ vụ việc phức tạp',
			'Đưa ra khuyến nghị với lập luận rõ ràng'
		],
		whyItFits:
			'Tình huống này giúp người tham gia tìm hiểu cách phân tích bằng chứng và trình bày lập luận.'
	},
	{
		slug: 'teacher',
		title: 'Giáo viên',
		category: 'Giáo dục',
		image: teacherImage,
		status: 'ready',
		duration: '10 phút',
		missionCount: 2,
		description: 'Thu hút học sinh và điều chỉnh cách dạy trong thời gian thực.',
		observedFactors: ['Thấu cảm', 'Khả năng thích ứng', 'Điều phối nhóm'],
		missions: [
			'Đáp ứng các nhu cầu học tập khác nhau trong một buổi học',
			'Diễn giải lại khái niệm khi sự chú ý giảm'
		],
		whyItFits:
			'Tình huống này giúp người tham gia tìm hiểu cách điều chỉnh hướng dẫn theo nhu cầu của người học.'
	},
	{
		slug: 'sales-representative',
		title: 'Nhân viên kinh doanh',
		category: 'Kinh doanh',
		image: salesImage,
		status: 'ready',
		duration: '8 phút',
		missionCount: 2,
		description: 'Xây dựng kết nối và chốt giao dịch trong các tình huống thực tế.',
		observedFactors: ['Xây dựng kết nối', 'Lắng nghe', 'Kiên cường'],
		missions: ['Khám phá nhu cầu khách hàng qua hội thoại', 'Phản hồi băn khoăn mà không mất đà'],
		whyItFits:
			'Tình huống này giúp người tham gia tìm hiểu cách khám phá nhu cầu và phản hồi băn khoăn của khách hàng.'
	},
	{
		slug: 'automotive-engineer',
		title: 'Kỹ sư ô tô',
		category: 'Kỹ thuật',
		image: engineeringImage,
		status: 'locked',
		duration: 'Sắp ra mắt',
		missionCount: 2,
		description: 'Chẩn đoán và giải quyết thử thách kỹ thuật trong phòng thí nghiệm ảo.',
		observedFactors: ['Tư duy hệ thống', 'Chính xác', 'Khả năng thích ứng'],
		missions: ['Lần theo lỗi trong một hệ thống xa lạ', 'Chọn trình tự kiểm tra an toàn'],
		whyItFits:
			'Mô-đun tương lai này sẽ giới thiệu công việc chẩn đoán lỗi và chọn trình tự kiểm tra an toàn.'
	}
];

const sampleStageScores: Record<StageId, number> = {
	D: 58,
	E: 76,
	S: 67,
	M: 70,
	A: 62,
	P: 48
};

const interestToCareer: Record<string, { label: string; slug: string }> = {
	'technology-engineering': {
		label: 'Kỹ sư ô tô',
		slug: 'automotive-engineer'
	},
	'science-research': { label: 'Bác sĩ', slug: 'doctor' },
	'design-creative': { label: 'Giáo viên', slug: 'teacher' },
	'business-entrepreneurship': {
		label: 'Nhân viên kinh doanh',
		slug: 'sales-representative'
	},
	'people-education': { label: 'Giáo viên', slug: 'teacher' },
	'health-wellbeing': { label: 'Bác sĩ', slug: 'doctor' },
	'law-public-service': { label: 'Luật sư', slug: 'lawyer' },
	'media-communication': {
		label: 'Nhân viên kinh doanh',
		slug: 'sales-representative'
	},
	'environment-sustainability': {
		label: 'Kỹ sư ô tô',
		slug: 'automotive-engineer'
	},
	'operations-trades': { label: 'Kỹ sư ô tô', slug: 'automotive-engineer' },
	exploring: { label: 'Bác sĩ', slug: 'doctor' }
};

const stageCopy: Record<StageId, { label: string; detail: string }> = {
	D: { label: 'Mong muốn', detail: 'Ý nghĩa & đóng góp' },
	E: { label: 'Chuyên môn', detail: 'Giao tiếp' },
	S: { label: 'Vai trò xã hội', detail: 'Điều phối' },
	M: { label: 'Tư duy', detail: 'Phân tích' },
	A: { label: 'Khả năng thích ứng', detail: 'Linh hoạt' },
	P: { label: 'Áp lực', detail: 'Trung bình' }
};

function scoreFor(scores: QuestionnaireScores | undefined, stage: StageId): number {
	return scores?.stages[stage]?.percent ?? sampleStageScores[stage];
}

export function createEvaluationModel(payload?: QuestionnaireSubmission | null): EvaluationModel {
	const stageScores = {} as Record<StageId, number>;
	for (const stage of Object.keys(stageCopy) as StageId[])
		stageScores[stage] = scoreFor(payload?.scores, stage);
	const target =
		payload?.careerInterests.map((id) => interestToCareer[id]).find(Boolean) ??
		interestToCareer.exploring;
	const comparisonLabels = [
		target.label,
		'Bác sĩ',
		'Luật sư',
		'Giáo viên',
		'Nhân viên kinh doanh',
		'Kỹ sư ô tô'
	]
		.filter((label, index, labels) => labels.indexOf(label) === index)
		.slice(0, 3);

	const profileRows = (Object.keys(stageCopy) as StageId[]).map((code) => ({
		code,
		label: stageCopy[code].label,
		value: `${stageScores[code]}%`,
		detail: stageCopy[code].detail
	}));

	return {
		status: payload ? 'completed' : 'sample',
		completedAt: payload?.completedAt,
		targetCareer: target.label,
		targetExperienceSlug: target.slug,
		profileRows,
		stageScores,
		careerMatches: comparisonLabels.map((label, index) => ({
			label,
			percent: index === 0 ? 87 : label === 'Luật sư' ? 81 : 78,
			accent: index === 0 ? ('lime' as const) : ('blue' as const)
		})),
		strengths: ['Phân tích thông tin tốt', 'Ưu tiên rõ ràng', 'Duy trì tập trung'],
		gaps: [
			'Tốc độ quyết định khi chịu áp lực',
			'Quản lý các yêu cầu đồng thời',
			'Phản ứng với thay đổi bất ngờ'
		],
		roadmap: [
			'Luyện tập phân loại có giới hạn thời gian',
			'Đóng vai giao tiếp áp lực cao',
			'Xem lại tiến độ sau 4 tuần'
		],
		observations: [
			{
				self: 'Tôi xử lý áp lực tốt',
				observed: 'Tốc độ quyết định giảm khi nhiệm vụ chồng chéo'
			},
			{
				self: 'Tôi giao tiếp tự tin',
				observed: 'Xác định chính xác thông tin quan trọng nhất'
			},
			{
				self: 'Tôi thích làm việc độc lập',
				observed: 'Có xu hướng điều phối và sắp xếp ưu tiên'
			}
		]
	};
}

export function answeredCount(draft: QuestionnaireDraft | null): number {
	return draft ? Object.keys(draft.answers).length : 0;
}

export function formatDate(value?: string): string {
	if (!value) return 'Chế độ xem trước';
	const date = new Date(value);
	if (Number.isNaN(date.getTime())) return 'Vừa hoàn tất';
	return new Intl.DateTimeFormat('vi-VN', {
		day: '2-digit',
		month: 'short',
		year: 'numeric'
	}).format(date);
}
