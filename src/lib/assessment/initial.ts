import type { QuestionnaireSubmission } from '$lib/questionnaire';
import {
	assessmentConfig,
	careerCandidatesForInterests,
	type AssessmentCareer,
	type GroupedDimensionId
} from './config';

export type InitialAssessmentDimension = {
	id: GroupedDimensionId;
	name: string;
	description: string;
	score: number;
};

export type InitialAssessmentCriterion = { dimension_id: GroupedDimensionId; importance: number };
export type InitialAssessmentCareer = {
	id: string;
	name: string;
	description: string;
	criteria: InitialAssessmentCriterion[];
};
export type InitialAssessmentRequest = {
	assessment_id: string;
	dimensions: InitialAssessmentDimension[];
	careers: InitialAssessmentCareer[];
};
export type InitialCareerMatch = {
	career_id: string;
	career_name: string;
	match_percentage: number;
};
export type InitialAssessmentResponse = { assessment_id: string; results: InitialCareerMatch[] };
export type InitialAssessmentMode = 'ai' | 'weighted';

export type AssessmentStorage = {
	getItem(key: string): string | null;
	setItem(key: string, value: string): void;
};

export type AssessmentFetch = (input: RequestInfo | URL, init?: RequestInit) => Promise<Response>;

export type InitialAssessmentRun = {
	request: InitialAssessmentRequest;
	response: InitialAssessmentResponse;
	rankedResults: InitialCareerMatch[];
	source: 'cache' | 'network';
};

export type AssessmentErrorKind = 'recoverable' | 'configuration' | 'data' | 'invalid-response';
export class InitialAssessmentError extends Error {
	readonly kind: AssessmentErrorKind;
	readonly status?: number;
	constructor(kind: AssessmentErrorKind, message: string, status?: number) {
		super(message);
		this.name = 'InitialAssessmentError';
		this.kind = kind;
		this.status = status;
	}
}

export const INITIAL_ASSESSMENT_CACHE_KEY = 'desmap:assessment:initial:v2';

export function initialAssessmentCacheKey(mode: InitialAssessmentMode): string {
	return `${INITIAL_ASSESSMENT_CACHE_KEY}:${mode}`;
}

function stable(value: unknown): string {
	if (Array.isArray(value)) return `[${value.map(stable).join(',')}]`;
	if (value && typeof value === 'object') {
		return `{${Object.keys(value as Record<string, unknown>)
			.sort()
			.map((key) => `${JSON.stringify(key)}:${stable((value as Record<string, unknown>)[key])}`)
			.join(',')}}`;
	}
	return JSON.stringify(value);
}

export function requestFingerprint(
	request: InitialAssessmentRequest,
	mode: InitialAssessmentMode = 'ai'
): string {
	return stable({ mode, request });
}

export function buildInitialAssessmentRequest(
	payload: QuestionnaireSubmission
): InitialAssessmentRequest {
	const dimensions = assessmentConfig.dimensions.map((dimension) => {
		const score = payload.scores.groups[dimension.id]?.percent;
		if (typeof score !== 'number' || !Number.isInteger(score) || score < 0 || score > 100) {
			throw new InitialAssessmentError('data', `Điểm nhóm ${dimension.id} không hợp lệ.`);
		}
		return { ...dimension, score };
	});
	const careers = careerCandidatesForInterests(payload.careerInterests).map((career) =>
		careerRequest(career)
	);
	if (careers.length === 0)
		throw new InitialAssessmentError('data', 'Chưa có nghề nghiệp nào để đánh giá.');
	const request = { assessment_id: payload.assessmentId, dimensions, careers };
	validateInitialAssessmentRequest(request);
	return request;
}

function careerRequest(career: AssessmentCareer): InitialAssessmentCareer {
	return {
		id: career.id,
		name: career.name,
		description: career.description,
		criteria: assessmentConfig.dimensions.map((dimension) => ({
			dimension_id: dimension.id,
			importance: career.weights[dimension.id]
		}))
	};
}

export function validateInitialAssessmentRequest(
	value: unknown
): asserts value is InitialAssessmentRequest {
	if (!value || typeof value !== 'object') {
		throw new InitialAssessmentError('data', 'Dữ liệu yêu cầu không hợp lệ.');
	}
	const body = value as Record<string, unknown>;
	if (
		Object.keys(body).sort().join(',') !== 'assessment_id,careers,dimensions' ||
		typeof body.assessment_id !== 'string' ||
		!/^assessment-[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(
			body.assessment_id
		) ||
		!Array.isArray(body.dimensions) ||
		body.dimensions.length !== 28 ||
		!Array.isArray(body.careers) ||
		body.careers.length < 1 ||
		body.careers.length > 5
	) {
		throw new InitialAssessmentError('data', 'Dữ liệu yêu cầu không hợp lệ.');
	}
	const dimensionIds = new Set<string>();
	for (const item of body.dimensions) {
		if (!item || typeof item !== 'object') {
			throw new InitialAssessmentError('data', 'Nhóm DESMAP không hợp lệ.');
		}
		const dimension = item as Record<string, unknown>;
		if (
			Object.keys(dimension).sort().join(',') !== 'description,id,name,score' ||
			typeof dimension.id !== 'string' ||
			!assessmentConfig.dimensions.some((known) => known.id === dimension.id) ||
			dimensionIds.has(dimension.id) ||
			typeof dimension.name !== 'string' ||
			!dimension.name.trim() ||
			typeof dimension.description !== 'string' ||
			!dimension.description.trim() ||
			!Number.isInteger(dimension.score) ||
			(dimension.score as number) < 0 ||
			(dimension.score as number) > 100
		) {
			throw new InitialAssessmentError('data', 'Nhóm DESMAP không hợp lệ.');
		}
		dimensionIds.add(dimension.id);
	}
	const careerIds = new Set<string>();
	for (const item of body.careers) {
		if (!item || typeof item !== 'object') {
			throw new InitialAssessmentError('data', 'Nghề ứng viên không hợp lệ.');
		}
		const career = item as Record<string, unknown>;
		if (
			Object.keys(career).sort().join(',') !== 'criteria,description,id,name' ||
			typeof career.id !== 'string' ||
			careerIds.has(career.id) ||
			typeof career.name !== 'string' ||
			!career.name.trim() ||
			typeof career.description !== 'string' ||
			!career.description.trim() ||
			!Array.isArray(career.criteria) ||
			career.criteria.length !== 28
		) {
			throw new InitialAssessmentError('data', 'Nghề ứng viên không hợp lệ.');
		}
		const criterionIds = new Set<string>();
		for (const itemCriterion of career.criteria) {
			if (!itemCriterion || typeof itemCriterion !== 'object') {
				throw new InitialAssessmentError('data', 'Tiêu chí nghề nghiệp không hợp lệ.');
			}
			const criterion = itemCriterion as Record<string, unknown>;
			if (
				Object.keys(criterion).sort().join(',') !== 'dimension_id,importance' ||
				typeof criterion.dimension_id !== 'string' ||
				!dimensionIds.has(criterion.dimension_id) ||
				criterionIds.has(criterion.dimension_id) ||
				!Number.isInteger(criterion.importance) ||
				(criterion.importance as number) < 1 ||
				(criterion.importance as number) > 5
			) {
				throw new InitialAssessmentError('data', 'Tiêu chí nghề nghiệp không hợp lệ.');
			}
			criterionIds.add(criterion.dimension_id);
		}
		careerIds.add(career.id);
	}
}

export function validateInitialAssessmentResponse(
	value: unknown,
	request: InitialAssessmentRequest
): InitialAssessmentResponse {
	if (!value || typeof value !== 'object')
		throw new InitialAssessmentError('invalid-response', 'Phản hồi đánh giá không hợp lệ.');
	const body = value as Record<string, unknown>;
	if (Object.keys(body).sort().join(',') !== 'assessment_id,results')
		throw new InitialAssessmentError(
			'invalid-response',
			'Phản hồi đánh giá có dữ liệu không được hỗ trợ.'
		);
	if (
		body.assessment_id !== request.assessment_id ||
		!Array.isArray(body.results) ||
		body.results.length !== request.careers.length
	)
		throw new InitialAssessmentError(
			'invalid-response',
			'Phản hồi đánh giá không khớp với hồ sơ này.'
		);
	const results = body.results.map((item, index) => {
		if (!item || typeof item !== 'object')
			throw new InitialAssessmentError('invalid-response', 'Kết quả nghề nghiệp không hợp lệ.');
		const result = item as Record<string, unknown>;
		if (Object.keys(result).sort().join(',') !== 'career_id,career_name,match_percentage')
			throw new InitialAssessmentError(
				'invalid-response',
				'Kết quả nghề nghiệp có dữ liệu không được hỗ trợ.'
			);
		const career = request.careers[index];
		if (result.career_id !== career.id || result.career_name !== career.name)
			throw new InitialAssessmentError(
				'invalid-response',
				'Thứ tự hoặc danh tính nghề nghiệp đã thay đổi.'
			);
		if (
			!Number.isInteger(result.match_percentage) ||
			(result.match_percentage as number) < 0 ||
			(result.match_percentage as number) > 100
		)
			throw new InitialAssessmentError('invalid-response', 'Phần trăm kết quả không hợp lệ.');
		return result as unknown as InitialCareerMatch;
	});
	if (new Set(results.map((result) => result.career_id)).size !== results.length)
		throw new InitialAssessmentError('invalid-response', 'Kết quả bị trùng nghề nghiệp.');
	return { assessment_id: body.assessment_id as string, results };
}

export function calculateInitialAssessment(
	request: InitialAssessmentRequest
): InitialAssessmentResponse {
	validateInitialAssessmentRequest(request);
	const scores = new Map(request.dimensions.map((dimension) => [dimension.id, dimension.score]));
	const response: InitialAssessmentResponse = {
		assessment_id: request.assessment_id,
		results: request.careers.map((career) => {
			let weightedTotal = 0;
			let totalImportance = 0;
			for (const criterion of career.criteria) {
				weightedTotal += (scores.get(criterion.dimension_id) as number) * criterion.importance;
				totalImportance += criterion.importance;
			}
			return {
				career_id: career.id,
				career_name: career.name,
				match_percentage: Math.round(weightedTotal / totalImportance)
			};
		})
	};
	return validateInitialAssessmentResponse(response, request);
}

export function rankInitialMatches(response: InitialAssessmentResponse): InitialCareerMatch[] {
	return response.results
		.map((result, index) => ({ result, index }))
		.sort((a, b) => b.result.match_percentage - a.result.match_percentage || a.index - b.index)
		.map(({ result }) => result);
}

export function readCachedInitialAssessment(
	storage: AssessmentStorage,
	request: InitialAssessmentRequest,
	mode: InitialAssessmentMode = 'ai'
): InitialAssessmentResponse | null {
	try {
		const parsed = JSON.parse(storage.getItem(initialAssessmentCacheKey(mode)) ?? 'null') as {
			assessment_id?: string;
			fingerprint?: string;
			mode?: InitialAssessmentMode;
			response?: unknown;
		} | null;
		if (
			!parsed ||
			parsed.assessment_id !== request.assessment_id ||
			parsed.mode !== mode ||
			parsed.fingerprint !== requestFingerprint(request, mode)
		)
			return null;
		return validateInitialAssessmentResponse(parsed.response, request);
	} catch {
		return null;
	}
}

export function writeCachedInitialAssessment(
	storage: AssessmentStorage,
	request: InitialAssessmentRequest,
	response: InitialAssessmentResponse,
	mode: InitialAssessmentMode = 'ai'
): void {
	try {
		storage.setItem(
			initialAssessmentCacheKey(mode),
			JSON.stringify({
				assessment_id: request.assessment_id,
				fingerprint: requestFingerprint(request, mode),
				mode,
				response
			})
		);
	} catch {
		// A valid result remains usable when browser storage is unavailable or full.
	}
}

export async function fetchInitialAssessment(
	request: InitialAssessmentRequest,
	fetcher: AssessmentFetch = fetch
): Promise<InitialAssessmentResponse> {
	validateInitialAssessmentRequest(request);
	let response: Response;
	try {
		response = await fetcher('/api/ai/initial-career-assessment', {
			method: 'POST',
			headers: { 'content-type': 'application/json' },
			body: JSON.stringify(request)
		});
	} catch {
		throw new InitialAssessmentError('recoverable', 'Không thể kết nối tới dịch vụ đánh giá.');
	}
	if (response.status === 502 || response.status === 503)
		throw new InitialAssessmentError(
			'recoverable',
			'Dịch vụ đánh giá tạm thời chưa sẵn sàng.',
			response.status
		);
	if (response.status === 401)
		throw new InitialAssessmentError(
			'configuration',
			'Dịch vụ đánh giá chưa được cấu hình.',
			response.status
		);
	if (response.status === 422)
		throw new InitialAssessmentError('data', 'Dữ liệu đánh giá không hợp lệ.', response.status);
	if (!response.ok)
		throw new InitialAssessmentError(
			'invalid-response',
			'Dịch vụ đánh giá không phản hồi thành công.',
			response.status
		);
	try {
		return validateInitialAssessmentResponse(await response.json(), request);
	} catch (error) {
		if (error instanceof InitialAssessmentError) throw error;
		throw new InitialAssessmentError('invalid-response', 'Dịch vụ trả về kết quả không hợp lệ.');
	}
}

export async function runInitialAssessment(
	payload: QuestionnaireSubmission,
	options: {
		storage?: AssessmentStorage | null;
		fetcher?: AssessmentFetch;
		skipCache?: boolean;
		mode?: InitialAssessmentMode;
	} = {}
): Promise<InitialAssessmentRun> {
	const request = buildInitialAssessmentRequest(payload);
	const mode = options.mode ?? 'ai';
	if (!options.skipCache && options.storage) {
		const cached = readCachedInitialAssessment(options.storage, request, mode);
		if (cached) {
			return {
				request,
				response: cached,
				rankedResults: rankInitialMatches(cached),
				source: 'cache'
			};
		}
	}
	const response = await fetchInitialAssessment(request, options.fetcher);
	if (options.storage) writeCachedInitialAssessment(options.storage, request, response, mode);
	return {
		request,
		response,
		rankedResults: rankInitialMatches(response),
		source: 'network'
	};
}

export function browserAssessmentStorage(): AssessmentStorage | null {
	return typeof window === 'undefined' ? null : window.localStorage;
}
