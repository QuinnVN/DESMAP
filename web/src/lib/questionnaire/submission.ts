import type { QuestionnaireSubmission, QuestionnaireSyncStatus } from './data';
import { writeQuestionnaireSyncStatus } from './data';

export type SubmissionFetch = (input: RequestInfo | URL, init?: RequestInit) => Promise<Response>;

export class QuestionnaireSubmissionError extends Error {
	readonly recoverable: boolean;
	readonly status?: number;

	constructor(message: string, recoverable: boolean, status?: number) {
		super(message);
		this.name = 'QuestionnaireSubmissionError';
		this.recoverable = recoverable;
		this.status = status;
	}
}

export async function checkEmailAvailability(
	email: string,
	fetcher: SubmissionFetch = fetch
): Promise<boolean> {
	let response: Response;
	try {
		response = await fetcher(`/api/questionnaire-submissions?email=${encodeURIComponent(email)}`);
	} catch {
		throw new QuestionnaireSubmissionError('Không thể kết nối tới máy chủ lưu kết quả.', true);
	}
	if (response.status === 422)
		throw new QuestionnaireSubmissionError('Địa chỉ email không hợp lệ.', false, response.status);
	if (!response.ok)
		throw new QuestionnaireSubmissionError(
			'Máy chủ lưu kết quả tạm thời chưa sẵn sàng.',
			true,
			response.status
		);
	const body = (await response.json()) as { available?: unknown };
	if (typeof body.available !== 'boolean')
		throw new QuestionnaireSubmissionError('Máy chủ trả về dữ liệu không hợp lệ.', true);
	return body.available;
}

export async function uploadQuestionnaireSubmission(
	payload: QuestionnaireSubmission,
	fetcher: SubmissionFetch = fetch
): Promise<QuestionnaireSyncStatus> {
	writeQuestionnaireSyncStatus({ assessmentId: payload.assessmentId, status: 'pending' });
	try {
		const response = await fetcher('/api/questionnaire-submissions', {
			method: 'POST',
			headers: { 'content-type': 'application/json' },
			body: JSON.stringify(payload)
		});
		if (response.ok) {
			const status = { assessmentId: payload.assessmentId, status: 'synced' } as const;
			writeQuestionnaireSyncStatus(status);
			return status;
		}
		const body = (await response.json().catch(() => null)) as { error?: unknown } | null;
		const recoverable = response.status !== 409 && response.status !== 422;
		const message =
			typeof body?.error === 'string'
				? body.error
				: recoverable
					? 'Máy chủ lưu kết quả tạm thời chưa sẵn sàng.'
					: 'Không thể lưu kết quả này.';
		throw new QuestionnaireSubmissionError(message, recoverable, response.status);
	} catch (error) {
		const submissionError =
			error instanceof QuestionnaireSubmissionError
				? error
				: new QuestionnaireSubmissionError('Không thể kết nối tới máy chủ lưu kết quả.', true);
		const status: QuestionnaireSyncStatus = {
			assessmentId: payload.assessmentId,
			status: 'error',
			message: submissionError.message,
			recoverable: submissionError.recoverable
		};
		writeQuestionnaireSyncStatus(status);
		return status;
	}
}
