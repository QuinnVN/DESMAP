import type { RequestEvent } from '@sveltejs/kit';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { buildInitialAssessmentRequest, calculateInitialAssessment } from '$lib/assessment';
import { buildCompletionPayload, desmapQuestions } from '$lib/questionnaire';

const mode = vi.hoisted(() => vi.fn(() => 'ai' as 'ai' | 'weighted'));
vi.mock('$lib/server/initial-assessment-mode', () => ({ getInitialAssessmentMode: mode }));

import { POST } from './+server';

const payload = buildCompletionPayload({
	assessmentId: 'assessment-123e4567-e89b-42d3-a456-426614174000',
	answers: Object.fromEntries(
		desmapQuestions.map((question) => [question.id, question.options[0].letter])
	),
	careerInterests: ['science-research'],
	startedAt: '2026-09-12T08:00:00.000Z'
});
const requestBody = buildInitialAssessmentRequest(payload);
const validResponse = {
	assessment_id: requestBody.assessment_id,
	results: requestBody.careers.map((career) => ({
		career_id: career.id,
		career_name: career.name,
		match_percentage: 80
	}))
};

function event(body: string, fetcher: typeof fetch): RequestEvent {
	return {
		request: new Request('http://localhost/api/ai/initial-career-assessment', {
			method: 'POST',
			headers: { 'content-type': 'application/json' },
			body
		}),
		fetch: fetcher
	} as unknown as RequestEvent;
}

describe('initial assessment proxy', () => {
	afterEach(() => {
		mode.mockReturnValue('ai');
	});

	it('rejects malformed browser input before contacting the backend', async () => {
		const fetcher = vi.fn<typeof fetch>();
		const response = await POST(event('{}', fetcher));
		expect(response.status).toBe(422);
		expect(fetcher).not.toHaveBeenCalled();
	});

	it('forwards a valid request to the fixed loopback endpoint', async () => {
		const fetcher = vi.fn<typeof fetch>().mockResolvedValue(
			new Response(JSON.stringify(validResponse), {
				status: 200,
				headers: { 'content-type': 'application/json' }
			})
		);
		const response = await POST(event(JSON.stringify(requestBody), fetcher));
		expect(response.status).toBe(200);
		expect(await response.json()).toEqual(validResponse);
		expect(fetcher).toHaveBeenCalledWith(
			'http://127.0.0.1:8000/api/ai/initial-career-assessment',
			expect.objectContaining({ method: 'POST' })
		);
	});

	it.each(['true', 'TRUE'])('calculates a valid request without contacting AI for %s', async () => {
		mode.mockReturnValue('weighted');
		const fetcher = vi.fn<typeof fetch>();
		const response = await POST(event(JSON.stringify(requestBody), fetcher));

		expect(response.status).toBe(200);
		expect(await response.json()).toEqual(calculateInitialAssessment(requestBody));
		expect(fetcher).not.toHaveBeenCalled();
	});

	it('continues to AI when the flag value is invalid', async () => {
		const fetcher = vi.fn<typeof fetch>().mockResolvedValue(
			new Response(JSON.stringify(validResponse), {
				status: 200,
				headers: { 'content-type': 'application/json' }
			})
		);
		const response = await POST(event(JSON.stringify(requestBody), fetcher));

		expect(response.status).toBe(200);
		expect(fetcher).toHaveBeenCalledOnce();
	});

	it('turns an invalid successful backend response into a safe 502', async () => {
		const fetcher = vi.fn<typeof fetch>().mockResolvedValue(
			new Response(JSON.stringify({ ...validResponse, extra: true }), {
				status: 200,
				headers: { 'content-type': 'application/json' }
			})
		);
		const response = await POST(event(JSON.stringify(requestBody), fetcher));
		expect(response.status).toBe(502);
		expect(await response.json()).toEqual({
			error: 'Dịch vụ đánh giá trả về dữ liệu không hợp lệ.'
		});
	});

	it('preserves backend status without exposing its response body', async () => {
		const fetcher = vi.fn<typeof fetch>().mockResolvedValue(
			new Response(JSON.stringify({ detail: 'internal backend detail' }), {
				status: 503,
				headers: { 'content-type': 'application/json' }
			})
		);
		const response = await POST(event(JSON.stringify(requestBody), fetcher));
		expect(response.status).toBe(503);
		expect(JSON.stringify(await response.json())).not.toContain('internal backend detail');
	});
});
