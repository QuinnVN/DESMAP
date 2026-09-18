import { beforeEach, describe, expect, it, vi } from 'vitest';
import { buildCompletionPayload, desmapQuestions } from '$lib/questionnaire';

const repository = vi.hoisted(() => ({
	findQuestionnaireSubmission: vi.fn()
}));
vi.mock('$lib/server/questionnaire-submissions', () => repository);

import { actions } from './+page.server';

const submission = buildCompletionPayload({
	assessmentId: 'assessment-123e4567-e89b-42d3-a456-426614174000',
	answers: Object.fromEntries(
		desmapQuestions.map((question) => [question.id, question.options[0].letter])
	),
	careerInterests: ['science-research'],
	participant: { name: 'Nguyen Van A', email: 'student@example.com' },
	startedAt: '2026-09-12T08:00:00.000Z'
});

function loginEvent(name: string, email: string) {
	const formData = new FormData();
	formData.set('name', name);
	formData.set('email', email);
	return {
		request: new Request('http://localhost/start?/login', { method: 'POST', body: formData })
	};
}

describe('start page login action', () => {
	beforeEach(() => vi.clearAllMocks());

	it('returns the saved submission when both participant fields match', async () => {
		repository.findQuestionnaireSubmission.mockResolvedValue(submission);
		const result = await actions.login(
			loginEvent(' Nguyen Van A ', ' Student@Example.com ') as never
		);
		expect(result).toEqual({ success: true, submission });
		expect(repository.findQuestionnaireSubmission).toHaveBeenCalledWith(
			'Nguyen Van A',
			'student@example.com'
		);
	});

	it('returns an error when the participant cannot be found', async () => {
		repository.findQuestionnaireSubmission.mockResolvedValue(null);
		const result = await actions.login(loginEvent('Nguyen Van A', 'student@example.com') as never);
		expect(result).toMatchObject({ status: 404, data: { success: false } });
	});

	it('rejects invalid participant details before querying MongoDB', async () => {
		const result = await actions.login(loginEvent('', 'not-an-email') as never);
		expect(result).toMatchObject({ status: 422, data: { success: false } });
		expect(repository.findQuestionnaireSubmission).not.toHaveBeenCalled();
	});
});
