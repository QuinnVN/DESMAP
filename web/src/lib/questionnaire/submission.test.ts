import { describe, expect, it, vi } from 'vitest';
import { buildCompletionPayload, desmapQuestions } from './data';
import { checkEmailAvailability, uploadQuestionnaireSubmission } from './submission';

const payload = buildCompletionPayload({
	assessmentId: 'assessment-123e4567-e89b-42d3-a456-426614174000',
	answers: Object.fromEntries(
		desmapQuestions.map((question) => [question.id, question.options[0].letter])
	),
	careerInterests: ['science-research'],
	participant: { name: 'Nguyen Van A', email: 'student@example.com' },
	startedAt: '2026-09-12T08:00:00.000Z'
});

describe('questionnaire submission client', () => {
	it('checks email availability', async () => {
		const fetcher = vi.fn<typeof fetch>().mockResolvedValue(
			new Response(JSON.stringify({ available: true }), {
				headers: { 'content-type': 'application/json' }
			})
		);
		await expect(checkEmailAvailability('student@example.com', fetcher)).resolves.toBe(true);
		expect(fetcher).toHaveBeenCalledWith(
			'/api/questionnaire-submissions?email=student%40example.com'
		);
	});

	it('marks an unavailable database as a recoverable sync error', async () => {
		const fetcher = vi.fn<typeof fetch>().mockResolvedValue(new Response(null, { status: 503 }));
		await expect(uploadQuestionnaireSubmission(payload, fetcher)).resolves.toMatchObject({
			assessmentId: payload.assessmentId,
			status: 'error',
			recoverable: true
		});
	});

	it('marks an email conflict as non-recoverable', async () => {
		const fetcher = vi.fn<typeof fetch>().mockResolvedValue(
			new Response(JSON.stringify({ error: 'Email này đã hoàn thành một bài đánh giá.' }), {
				status: 409,
				headers: { 'content-type': 'application/json' }
			})
		);
		await expect(uploadQuestionnaireSubmission(payload, fetcher)).resolves.toMatchObject({
			status: 'error',
			recoverable: false
		});
	});
});
