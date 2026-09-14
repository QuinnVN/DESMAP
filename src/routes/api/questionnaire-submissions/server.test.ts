import { beforeEach, describe, expect, it, vi } from 'vitest';
import { buildCompletionPayload, desmapQuestions } from '$lib/questionnaire';

const repository = vi.hoisted(() => ({
	emailHasAssessment: vi.fn(),
	saveQuestionnaireSubmission: vi.fn()
}));
vi.mock('$lib/server/questionnaire-submissions', () => repository);

import { GET, POST } from './+server';
type SubmissionEvent = Parameters<typeof GET>[0];

const payload = buildCompletionPayload({
	assessmentId: 'assessment-123e4567-e89b-42d3-a456-426614174000',
	answers: Object.fromEntries(
		desmapQuestions.map((question) => [question.id, question.options[0].letter])
	),
	careerInterests: ['science-research'],
	participant: { name: 'Nguyen Van A', email: 'Student@Example.com' },
	startedAt: '2026-09-12T08:00:00.000Z'
});

function getEvent(email: string): SubmissionEvent {
	return {
		url: new URL(
			`http://localhost/api/questionnaire-submissions?email=${encodeURIComponent(email)}`
		)
	} as SubmissionEvent;
}

function postEvent(value: unknown): SubmissionEvent {
	return {
		request: new Request('http://localhost/api/questionnaire-submissions', {
			method: 'POST',
			headers: { 'content-type': 'application/json' },
			body: JSON.stringify(value)
		})
	} as SubmissionEvent;
}

describe('questionnaire submission endpoint', () => {
	beforeEach(() => vi.clearAllMocks());

	it('checks a normalized email before the questionnaire starts', async () => {
		repository.emailHasAssessment.mockResolvedValue(false);
		const response = await GET(getEvent(' Student@Example.com '));
		expect(response.status).toBe(200);
		expect(await response.json()).toEqual({ available: true });
		expect(repository.emailHasAssessment).toHaveBeenCalledWith('student@example.com');
	});

	it('recalculates and saves a valid completed questionnaire', async () => {
		repository.saveQuestionnaireSubmission.mockResolvedValue('created');
		const response = await POST(postEvent({ ...payload, scores: {} }));
		expect(response.status).toBe(201);
		expect(repository.saveQuestionnaireSubmission).toHaveBeenCalledWith(
			expect.objectContaining({
				assessmentId: payload.assessmentId,
				participant: { name: 'Nguyen Van A', email: 'student@example.com' },
				scores: payload.scores
			})
		);
	});

	it('returns a non-retryable conflict for another assessment using the same email', async () => {
		repository.saveQuestionnaireSubmission.mockResolvedValue('conflict');
		const response = await POST(postEvent(payload));
		expect(response.status).toBe(409);
		expect(await response.json()).toEqual({
			error: 'Email này đã hoàn thành một bài đánh giá.'
		});
	});

	it('rejects malformed data without contacting MongoDB', async () => {
		const response = await POST(postEvent({ ...payload, answers: {} }));
		expect(response.status).toBe(422);
		expect(repository.saveQuestionnaireSubmission).not.toHaveBeenCalled();
	});
});
