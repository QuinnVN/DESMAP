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

export type EvaluationStatus = 'completed' | 'sample';

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

export type ExperienceCategory = 'Healthcare' | 'Law' | 'Education' | 'Business' | 'Engineering';

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
		title: 'Doctor',
		category: 'Healthcare',
		image: doctorImage,
		status: 'recommended',
		duration: '10 minutes',
		missionCount: 2,
		description: 'Triage patients and identify critical information under pressure.',
		observedFactors: ['Decision speed', 'Prioritization', 'Pressure response'],
		missions: ['Triage three patients as new information arrives', 'Communicate a treatment priority to your team'],
		whyItFits: 'Your sample DESMAP profile shows a strong pull toward meaning, interpersonal expertise and high responsibility.'
	},
	{
		slug: 'lawyer',
		title: 'Lawyer',
		category: 'Law',
		image: lawyerImage,
		status: 'ready',
		duration: '12 minutes',
		missionCount: 2,
		description: 'Analyse cases and make sound recommendations under deadlines.',
		observedFactors: ['Evidence synthesis', 'Decision confidence', 'Communication under stress'],
		missions: ['Separate relevant evidence from a noisy case file', 'Make a recommendation with a clear rationale'],
		whyItFits: 'A useful comparison career for people who enjoy structured reasoning and high-stakes communication.'
	},
	{
		slug: 'teacher',
		title: 'Teacher',
		category: 'Education',
		image: teacherImage,
		status: 'ready',
		duration: '10 minutes',
		missionCount: 2,
		description: 'Engage students and adapt your teaching in real time.',
		observedFactors: ['Empathy', 'Adaptability', 'Group coordination'],
		missions: ['Respond to different learning needs in one session', 'Reframe a concept when attention drops'],
		whyItFits: 'A people-centred scenario that tests how your interpersonal strengths translate into live facilitation.'
	},
	{
		slug: 'sales-representative',
		title: 'Sales representative',
		category: 'Business',
		image: salesImage,
		status: 'ready',
		duration: '8 minutes',
		missionCount: 2,
		description: 'Build rapport and close deals in realistic scenarios.',
		observedFactors: ['Rapport building', 'Listening', 'Resilience'],
		missions: ['Discover a client need through conversation', 'Respond to a concern without losing momentum'],
		whyItFits: 'A fast social scenario that reveals how your communication style performs with competing priorities.'
	},
	{
		slug: 'automotive-engineer',
		title: 'Automotive engineer',
		category: 'Engineering',
		image: engineeringImage,
		status: 'locked',
		duration: 'Coming soon',
		missionCount: 2,
		description: 'Diagnose and solve engineering challenges in a virtual lab.',
		observedFactors: ['Systems thinking', 'Precision', 'Adaptability'],
		missions: ['Trace a fault through an unfamiliar system', 'Choose a safe test sequence'],
		whyItFits: 'A future module for comparing analytical focus and practical problem solving in a technical environment.'
	}
];

const sampleStageScores: Record<StageId, number> = { D: 58, E: 76, S: 67, M: 70, A: 62, P: 48 };

const interestToCareer: Record<string, { label: string; slug: string }> = {
	'technology-engineering': { label: 'Automotive engineer', slug: 'automotive-engineer' },
	'science-research': { label: 'Doctor', slug: 'doctor' },
	'design-creative': { label: 'Teacher', slug: 'teacher' },
	'business-entrepreneurship': { label: 'Sales representative', slug: 'sales-representative' },
	'people-education': { label: 'Teacher', slug: 'teacher' },
	'health-wellbeing': { label: 'Doctor', slug: 'doctor' },
	'law-public-service': { label: 'Lawyer', slug: 'lawyer' },
	'media-communication': { label: 'Sales representative', slug: 'sales-representative' },
	'environment-sustainability': { label: 'Automotive engineer', slug: 'automotive-engineer' },
	'operations-trades': { label: 'Automotive engineer', slug: 'automotive-engineer' },
	exploring: { label: 'Doctor', slug: 'doctor' }
};

const stageCopy: Record<StageId, { label: string; detail: string }> = {
	D: { label: 'Desire', detail: 'Meaning & contribution' },
	E: { label: 'Expertise', detail: 'Interpersonal' },
	S: { label: 'Social role', detail: 'Coordinator' },
	M: { label: 'Mind', detail: 'Analytical' },
	A: { label: 'Adaptability', detail: 'Flexible' },
	P: { label: 'Pressure', detail: 'Moderate' }
};

function scoreFor(scores: QuestionnaireScores | undefined, stage: StageId): number {
	return scores?.stages[stage]?.percent ?? sampleStageScores[stage];
}

export function createEvaluationModel(payload?: QuestionnaireSubmission | null): EvaluationModel {
	const stageScores = {} as Record<StageId, number>;
	for (const stage of Object.keys(stageCopy) as StageId[]) stageScores[stage] = scoreFor(payload?.scores, stage);
	const target = payload?.careerInterests.map((id) => interestToCareer[id]).find(Boolean) ?? interestToCareer.exploring;
	const comparisonLabels = [target.label, 'Doctor', 'Lawyer', 'Teacher', 'Sales representative', 'Automotive engineer'].filter((label, index, labels) => labels.indexOf(label) === index).slice(0, 3);

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
			percent: index === 0 ? 87 : label === 'Lawyer' ? 81 : 78,
			accent: index === 0 ? 'lime' as const : 'blue' as const
		})),
		strengths: ['Strong information analysis', 'Clear prioritization', 'Sustained focus'],
		gaps: ['Decision speed under pressure', 'Managing concurrent requests', 'Responding to unexpected change'],
		roadmap: ['Timed triage practice', 'High-pressure communication role-play', 'Review progress in 4 weeks'],
		observations: [
			{ self: 'I handle pressure well', observed: 'Decision speed decreased when tasks overlapped' },
			{ self: 'I communicate confidently', observed: 'Identified the most important information accurately' },
			{ self: 'I prefer working independently', observed: 'Tended to coordinate and organize priorities' }
		]
	};
}

export function answeredCount(draft: QuestionnaireDraft | null): number {
	return draft ? Object.keys(draft.answers).length : 0;
}

export function formatDate(value?: string): string {
	if (!value) return 'Preview mode';
	const date = new Date(value);
	if (Number.isNaN(date.getTime())) return 'Completed recently';
	return new Intl.DateTimeFormat('en', { day: '2-digit', month: 'short', year: 'numeric' }).format(date);
}
