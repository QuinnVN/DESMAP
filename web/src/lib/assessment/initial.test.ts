import { describe, expect, it, vi } from 'vitest';
import {
	assessmentCareers,
	assessmentConfig,
	careerCandidatesForInterests,
	validateAssessmentConfig,
	type AssessmentConfig
} from './config';
import {
	buildInitialAssessmentRequest,
	calculateInitialAssessment,
	initialAssessmentCacheKey,
	InitialAssessmentError,
	rankInitialMatches,
	readCachedInitialAssessment,
	requestFingerprint,
	runInitialAssessment,
	validateInitialAssessmentResponse,
	writeCachedInitialAssessment,
	type AssessmentStorage,
	type InitialAssessmentRequest,
	type InitialAssessmentResponse
} from './initial';
import {
	buildCompletionPayload,
	desmapQuestions,
	type QuestionnaireSubmission
} from '$lib/questionnaire';

const assessmentId = 'assessment-123e4567-e89b-42d3-a456-426614174000';

function questionnaire(interests: string[] = ['science-research']): QuestionnaireSubmission {
	return buildCompletionPayload({
		assessmentId,
		answers: Object.fromEntries(
			desmapQuestions.map((question) => [question.id, question.options[0].letter])
		),
		careerInterests: interests,
		participant: { name: 'Nguyen Van A', email: 'student@example.com' },
		startedAt: '2026-09-12T08:00:00.000Z',
		completedAt: '2026-09-12T08:30:00.000Z'
	});
}

function responseFor(
	request: InitialAssessmentRequest,
	percentages?: number[]
): InitialAssessmentResponse {
	return {
		assessment_id: request.assessment_id,
		results: request.careers.map((career, index) => ({
			career_id: career.id,
			career_name: career.name,
			match_percentage: percentages?.[index] ?? 80 - index
		}))
	};
}

function memoryStorage(initial?: Record<string, string>) {
	const values = new Map(Object.entries(initial ?? {}));
	const setItem = vi.fn((key: string, value: string) => values.set(key, value));
	const storage: AssessmentStorage = {
		getItem: (key) => values.get(key) ?? null,
		setItem
	};
	return { storage, values, setItem };
}

function jsonResponse(body: unknown, status = 200): Response {
	return new Response(JSON.stringify(body), {
		status,
		headers: { 'content-type': 'application/json' }
	});
}

describe('initial assessment configuration', () => {
	it('contains the approved 28-by-5 weight matrix', () => {
		validateAssessmentConfig();
		expect(assessmentConfig.dimensions).toHaveLength(28);
		expect(assessmentConfig.careers).toHaveLength(5);
		const expected = [
			[3, 5, 2, 5, 2, 2, 5, 5, 5, 3, 4, 4, 5, 5, 2, 5, 3, 5, 4, 5, 4, 5, 5, 5, 5, 5, 4, 5],
			[3, 4, 4, 4, 4, 2, 5, 5, 5, 1, 4, 4, 5, 3, 4, 5, 4, 4, 4, 5, 4, 5, 5, 5, 5, 4, 5, 5],
			[2, 5, 3, 5, 3, 3, 5, 4, 5, 2, 4, 4, 4, 5, 2, 4, 5, 5, 4, 4, 5, 4, 4, 4, 4, 5, 5, 4],
			[5, 4, 4, 3, 5, 2, 4, 4, 5, 2, 3, 5, 4, 5, 4, 4, 4, 5, 3, 5, 4, 5, 5, 5, 4, 5, 5, 4],
			[4, 5, 4, 3, 3, 3, 4, 5, 3, 5, 5, 4, 5, 3, 3, 5, 4, 5, 4, 5, 5, 5, 4, 4, 5, 3, 3, 5]
		];
		expect(
			assessmentCareers.map((career) =>
				assessmentConfig.dimensions.map((dimension) => career.weights[dimension.id])
			)
		).toEqual(expected);
	});

	it('rejects incomplete weights and unknown interest mappings', () => {
		const firstCareer = assessmentConfig.careers[0];
		const brokenWeights = { ...firstCareer.weights } as Record<string, number>;
		delete brokenWeights.D1;
		const broken: AssessmentConfig = {
			...assessmentConfig,
			careers: [
				{ ...firstCareer, weights: brokenWeights as typeof firstCareer.weights },
				...assessmentConfig.careers.slice(1)
			],
			interestToCareers: { ...assessmentConfig.interestToCareers, exploring: ['missing' as never] }
		};
		expect(() => validateAssessmentConfig(broken)).toThrow();
	});

	it.each([
		['technology-engineering', 'automotive-engineer'],
		['environment-sustainability', 'automotive-engineer'],
		['operations-trades', 'automotive-engineer'],
		['science-research', 'doctor'],
		['health-wellbeing', 'doctor'],
		['design-creative', 'teacher'],
		['people-education', 'teacher'],
		['business-entrepreneurship', 'sales-representative'],
		['media-communication', 'sales-representative'],
		['law-public-service', 'lawyer']
	])('maps %s to %s', (interest, career) => {
		expect(careerCandidatesForInterests([interest]).map((item) => item.id)).toEqual([career]);
	});

	it('expands in saved order, removes duplicates, and expands exploring in catalog order', () => {
		expect(
			careerCandidatesForInterests([
				'law-public-service',
				'science-research',
				'law-public-service'
			]).map((career) => career.id)
		).toEqual(['lawyer', 'doctor']);
		expect(careerCandidatesForInterests(['exploring']).map((career) => career.id)).toEqual(
			assessmentConfig.careers.map((career) => career.id)
		);
	});
});

describe('request and response contract', () => {
	it('sends exactly 28 grouped scores and 28 approved criteria without fine scores', () => {
		const payload = questionnaire(['science-research', 'law-public-service']);
		const request = buildInitialAssessmentRequest(payload);
		expect(Object.keys(payload.scores.dimensions)).toHaveLength(35);
		expect(request.dimensions).toHaveLength(28);
		expect(request.dimensions.map((item) => item.id)).toEqual(
			assessmentConfig.dimensions.map((item) => item.id)
		);
		expect(request.dimensions.map((item) => item.score)).toEqual(
			assessmentConfig.dimensions.map((item) => payload.scores.groups[item.id].percent)
		);
		expect(request.careers.every((career) => career.criteria.length === 28)).toBe(true);
		expect(JSON.stringify(request)).not.toContain('scores');
		expect(JSON.stringify(request)).not.toContain('answers');
	});

	it('rejects malformed fields, identities, order, counts, percentages, and duplicates', () => {
		const request = buildInitialAssessmentRequest(
			questionnaire(['science-research', 'law-public-service'])
		);
		const valid = responseFor(request);
		expect(validateInitialAssessmentResponse(valid, request)).toEqual(valid);
		const cases: unknown[] = [
			{ results: valid.results },
			{ ...valid, extra: true },
			{ ...valid, assessment_id: 'assessment-00000000-0000-4000-8000-000000000000' },
			{ ...valid, results: valid.results.slice(0, 1) },
			{ ...valid, results: valid.results.map((item) => ({ ...item, evaluation: 'Không hợp lệ' })) },
			{ ...valid, results: [{ ...valid.results[0], career_id: 'lawyer' }, valid.results[1]] },
			{ ...valid, results: [...valid.results].reverse() },
			{ ...valid, results: [{ ...valid.results[0], match_percentage: -1 }, valid.results[1]] },
			{ ...valid, results: [{ ...valid.results[0], match_percentage: 101 }, valid.results[1]] },
			{ ...valid, results: [valid.results[0], valid.results[0]] }
		];
		for (const value of cases) {
			expect(() => validateInitialAssessmentResponse(value, request)).toThrow(
				InitialAssessmentError
			);
		}
	});

	it('sorts a copy descending and keeps request order for ties', () => {
		const request = buildInitialAssessmentRequest(
			questionnaire(['science-research', 'law-public-service', 'design-creative'])
		);
		const response = responseFor(request, [70, 90, 90]);
		const before = structuredClone(response);
		expect(rankInitialMatches(response).map((item) => item.career_id)).toEqual([
			'lawyer',
			'teacher',
			'doctor'
		]);
		expect(response).toEqual(before);
	});

	it('calculates each career from all 28 grouped scores and importance weights', () => {
		const request = buildInitialAssessmentRequest(questionnaire(['exploring']));
		request.dimensions.forEach((dimension, index) => {
			dimension.score = index % 3 === 0 ? 100 : index % 3 === 1 ? 50 : 0;
		});
		const scores = new Map(request.dimensions.map((dimension) => [dimension.id, dimension.score]));
		const expected = request.careers.map((career) => {
			const weightedTotal = career.criteria.reduce(
				(total, criterion) =>
					total + (scores.get(criterion.dimension_id) as number) * criterion.importance,
				0
			);
			const totalImportance = career.criteria.reduce(
				(total, criterion) => total + criterion.importance,
				0
			);
			return {
				career_id: career.id,
				career_name: career.name,
				match_percentage: Math.round(weightedTotal / totalImportance)
			};
		});

		expect(calculateInitialAssessment(request)).toEqual({
			assessment_id: request.assessment_id,
			results: expected
		});
	});
});

describe('cache and network boundary', () => {
	it('reuses a matching success without calling the network', async () => {
		const payload = questionnaire();
		const request = buildInitialAssessmentRequest(payload);
		const response = responseFor(request);
		const cached = JSON.stringify({
			assessment_id: request.assessment_id,
			fingerprint: requestFingerprint(request, 'ai'),
			mode: 'ai',
			response
		});
		const { storage } = memoryStorage({ [initialAssessmentCacheKey('ai')]: cached });
		const fetcher = vi.fn();
		const result = await runInitialAssessment(payload, { storage, fetcher });
		expect(result.source).toBe('cache');
		expect(fetcher).not.toHaveBeenCalled();
	});

	it('misses corrupted, changed-score, changed-career, changed-weight, and mismatched-id caches', () => {
		const request = buildInitialAssessmentRequest(questionnaire());
		const response = responseFor(request);
		const changedScore = structuredClone(request);
		changedScore.dimensions[0].score += 1;
		const changedCareer = buildInitialAssessmentRequest(questionnaire(['law-public-service']));
		const changedWeight = structuredClone(request);
		changedWeight.careers[0].criteria[0].importance = 1;
		const mismatchedId = structuredClone(request);
		mismatchedId.assessment_id = 'assessment-00000000-0000-4000-8000-000000000000';
		const { storage, values } = memoryStorage();
		for (const current of [changedScore, changedCareer, changedWeight, mismatchedId]) {
			values.set(
				initialAssessmentCacheKey('ai'),
				JSON.stringify({
					assessment_id: request.assessment_id,
					fingerprint: requestFingerprint(request, 'ai'),
					mode: 'ai',
					response
				})
			);
			expect(readCachedInitialAssessment(storage, current)).toBeNull();
		}
		values.set(initialAssessmentCacheKey('ai'), '{broken');
		expect(readCachedInitialAssessment(storage, request)).toBeNull();
	});

	it('keeps AI and weighted results in separate caches', () => {
		const request = buildInitialAssessmentRequest(questionnaire());
		const aiResponse = responseFor(request, [81]);
		const weightedResponse = responseFor(request, [67]);
		const { storage, values } = memoryStorage();

		writeCachedInitialAssessment(storage, request, aiResponse, 'ai');
		expect(readCachedInitialAssessment(storage, request, 'weighted')).toBeNull();
		writeCachedInitialAssessment(storage, request, weightedResponse, 'weighted');

		expect(readCachedInitialAssessment(storage, request, 'ai')).toEqual(aiResponse);
		expect(readCachedInitialAssessment(storage, request, 'weighted')).toEqual(weightedResponse);
		expect(values.has(initialAssessmentCacheKey('ai'))).toBe(true);
		expect(values.has(initialAssessmentCacheKey('weighted'))).toBe(true);
	});

	it('caches only a successful validated network response', async () => {
		const payload = questionnaire();
		const request = buildInitialAssessmentRequest(payload);
		const valid = responseFor(request);
		const successStorage = memoryStorage();
		await runInitialAssessment(payload, {
			storage: successStorage.storage,
			fetcher: async () => jsonResponse(valid)
		});
		expect(successStorage.setItem).toHaveBeenCalledTimes(1);

		const failureStorage = memoryStorage();
		await expect(
			runInitialAssessment(payload, {
				storage: failureStorage.storage,
				fetcher: async () => jsonResponse({ error: 'unavailable' }, 503)
			})
		).rejects.toMatchObject({ kind: 'recoverable', status: 503 });
		expect(failureStorage.setItem).not.toHaveBeenCalled();
	});

	it('still returns a valid result when browser storage cannot write', async () => {
		const payload = questionnaire();
		const request = buildInitialAssessmentRequest(payload);
		const result = await runInitialAssessment(payload, {
			storage: {
				getItem: () => null,
				setItem: () => {
					throw new DOMException('quota');
				}
			},
			fetcher: async () => jsonResponse(responseFor(request))
		});
		expect(result.source).toBe('network');
		expect(result.response).toEqual(responseFor(request));
	});

	it.each([
		[502, 'recoverable'],
		[503, 'recoverable'],
		[401, 'configuration'],
		[422, 'data']
	] as const)('classifies HTTP %s as %s', async (status, kind) => {
		const payload = questionnaire();
		await expect(
			runInitialAssessment(payload, {
				fetcher: async () => jsonResponse({ error: 'failure' }, status)
			})
		).rejects.toMatchObject({ kind, status });
	});

	it('classifies network failures and invalid JSON', async () => {
		const payload = questionnaire();
		await expect(
			runInitialAssessment(payload, {
				fetcher: async () => {
					throw new TypeError('offline');
				}
			})
		).rejects.toMatchObject({ kind: 'recoverable' });
		await expect(
			runInitialAssessment(payload, {
				fetcher: async () =>
					new Response('not json', {
						status: 200,
						headers: { 'content-type': 'application/json' }
					})
			})
		).rejects.toMatchObject({ kind: 'invalid-response' });
	});
});
