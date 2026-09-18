import { afterEach, describe, expect, it, vi } from 'vitest';
import {
	buildCompletionPayload,
	createAssessmentId,
	desmapQuestions,
	parseCompletionPayload,
	readCompletionPayload,
	writeCompletionPayload
} from './data';

const uuid = '123e4567-e89b-42d3-a456-426614174000';

function completedRecord() {
	return buildCompletionPayload({
		assessmentId: `assessment-${uuid}`,
		answers: Object.fromEntries(
			desmapQuestions.map((question) => [question.id, question.options[0].letter])
		),
		careerInterests: ['science-research'],
		participant: { name: 'Nguyen Van A', email: 'Student@Example.com ' },
		startedAt: '2026-09-12T08:00:00.000Z',
		completedAt: '2026-09-12T08:30:00.000Z'
	});
}

describe('completed questionnaire identity', () => {
	afterEach(() => vi.unstubAllGlobals());

	it('creates a schema-safe assessment identifier', () => {
		expect(createAssessmentId(() => uuid)).toBe(`assessment-${uuid}`);
	});

	it('keeps an existing identifier and all score levels', () => {
		const parsed = parseCompletionPayload(completedRecord());
		expect(parsed?.assessmentId).toBe(`assessment-${uuid}`);
		expect(parsed?.participant).toEqual({ name: 'Nguyen Van A', email: 'student@example.com' });
		expect(Object.keys(parsed?.scores.dimensions ?? {})).toHaveLength(35);
		expect(Object.keys(parsed?.scores.groups ?? {})).toHaveLength(28);
		expect(Object.keys(parsed?.scores.stages ?? {})).toHaveLength(6);
	});

	it('upgrades a valid legacy record once without losing questionnaire data', () => {
		const legacy = { ...completedRecord(), assessmentId: undefined };
		const parsed = parseCompletionPayload(legacy, () => `assessment-${uuid}`);
		expect(parsed?.assessmentId).toBe(`assessment-${uuid}`);
		expect(parsed?.answers).toEqual(legacy.answers);
		expect(parsed?.careerInterests).toEqual(legacy.careerInterests);
		expect(parsed?.completedAt).toBe(legacy.completedAt);
	});

	it('rejects corrupted records and malformed identifiers', () => {
		const record = completedRecord();
		expect(parseCompletionPayload({ ...record, answers: {} })).toBeNull();
		expect(parseCompletionPayload({ ...record, careerInterests: ['missing'] })).toBeNull();
		expect(
			parseCompletionPayload({ ...record, participant: { name: '', email: 'bad' } })
		).toBeNull();
		expect(
			parseCompletionPayload(
				{ ...record, assessmentId: 'assessment-not-a-uuid' },
				() => `assessment-${uuid}`
			)
		).toBeNull();
	});

	it('preserves a restored MongoDB submission for the evaluation page', () => {
		const values = new Map<string, string>();
		vi.stubGlobal('window', {
			sessionStorage: {
				getItem: (key: string) => values.get(key) ?? null,
				setItem: (key: string, value: string) => values.set(key, value)
			}
		});

		const submission = completedRecord();
		expect(writeCompletionPayload(submission)).toBe(true);
		expect(readCompletionPayload()).toEqual(submission);
	});
});
