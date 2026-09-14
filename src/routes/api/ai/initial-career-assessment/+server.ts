import { env } from '$env/dynamic/private';
import { json, type RequestHandler } from '@sveltejs/kit';
import {
	calculateInitialAssessment,
	InitialAssessmentError,
	validateInitialAssessmentRequest,
	validateInitialAssessmentResponse
} from '$lib/assessment';
import { getInitialAssessmentMode } from '$lib/server/initial-assessment-mode';

export const POST: RequestHandler = async ({ request, fetch }) => {
	let body: unknown;
	try {
		body = await request.json();
		validateInitialAssessmentRequest(body);
	} catch (error) {
		const message =
			error instanceof InitialAssessmentError ? error.message : 'Dữ liệu yêu cầu không hợp lệ.';
		return json({ error: message }, { status: 422 });
	}
	if (getInitialAssessmentMode() === 'weighted') {
		return json(calculateInitialAssessment(body));
	}
	if (!env.AI_BACKEND_URL) {
		return json({ error: 'Dịch vụ đánh giá chưa được cấu hình.' }, { status: 503 });
	}

	try {
		const upstream = await fetch(env.AI_BACKEND_URL, {
			method: 'POST',
			headers: { 'content-type': 'application/json' },
			body: JSON.stringify(body)
		});
		const contentType = upstream.headers.get('content-type') ?? '';
		if (!contentType.includes('application/json')) {
			return json(
				{ error: 'Dịch vụ đánh giá trả về dữ liệu không hợp lệ.' },
				{ status: upstream.ok ? 502 : upstream.status }
			);
		}
		let responseBody: unknown;
		try {
			responseBody = await upstream.json();
		} catch {
			return json(
				{ error: 'Dịch vụ đánh giá trả về dữ liệu không hợp lệ.' },
				{ status: upstream.ok ? 502 : upstream.status }
			);
		}
		if (!upstream.ok) {
			const message =
				upstream.status === 401
					? 'Dịch vụ đánh giá chưa được cấu hình.'
					: upstream.status === 422
						? 'Dữ liệu đánh giá không hợp lệ.'
						: 'Dịch vụ đánh giá tạm thời chưa sẵn sàng.';
			return json({ error: message }, { status: upstream.status });
		}
		try {
			return json(validateInitialAssessmentResponse(responseBody, body));
		} catch {
			return json({ error: 'Dịch vụ đánh giá trả về dữ liệu không hợp lệ.' }, { status: 502 });
		}
	} catch {
		return json({ error: 'Không thể kết nối tới dịch vụ đánh giá.' }, { status: 503 });
	}
};
