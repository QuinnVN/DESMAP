import { json } from '@sveltejs/kit';
import {
	isValidParticipantDetails,
	normalizeEmail,
	parseCompletionPayload
} from '$lib/questionnaire';
import {
	emailHasAssessment,
	saveQuestionnaireSubmission
} from '$lib/server/questionnaire-submissions';
import type { RequestHandler } from './$types';

const maximumBodyBytes = 100_000;

export const GET: RequestHandler = async ({ url }) => {
	const email = normalizeEmail(url.searchParams.get('email') ?? '');
	if (!isValidParticipantDetails({ name: 'Participant', email })) {
		return json({ error: 'Địa chỉ email không hợp lệ.' }, { status: 422 });
	}
	try {
		return json({ available: !(await emailHasAssessment(email)) });
	} catch {
		return json({ error: 'Máy chủ lưu kết quả tạm thời chưa sẵn sàng.' }, { status: 503 });
	}
};

export const POST: RequestHandler = async ({ request }) => {
	const contentLength = Number(request.headers.get('content-length') ?? 0);
	if (contentLength > maximumBodyBytes)
		return json({ error: 'Dữ liệu bảng câu hỏi quá lớn.' }, { status: 413 });

	let value: unknown;
	try {
		const body = await request.text();
		if (new TextEncoder().encode(body).byteLength > maximumBodyBytes)
			return json({ error: 'Dữ liệu bảng câu hỏi quá lớn.' }, { status: 413 });
		value = JSON.parse(body);
	} catch {
		return json({ error: 'Dữ liệu bảng câu hỏi không hợp lệ.' }, { status: 422 });
	}
	const payload = parseCompletionPayload(value);
	if (!payload) return json({ error: 'Dữ liệu bảng câu hỏi không hợp lệ.' }, { status: 422 });

	try {
		const result = await saveQuestionnaireSubmission(payload);
		if (result === 'conflict') {
			return json({ error: 'Email này đã hoàn thành một bài đánh giá.' }, { status: 409 });
		}
		return json(
			{ assessmentId: payload.assessmentId, status: 'synced' },
			{ status: result === 'created' ? 201 : 200 }
		);
	} catch {
		return json({ error: 'Máy chủ lưu kết quả tạm thời chưa sẵn sàng.' }, { status: 503 });
	}
};
