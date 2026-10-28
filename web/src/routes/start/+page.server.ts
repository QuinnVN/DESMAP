import { fail } from '@sveltejs/kit';
import { isValidParticipantDetails, normalizeParticipantDetails } from '$lib/questionnaire';
import { findQuestionnaireSubmission } from '$lib/server/questionnaire-submissions';
import type { Actions } from './$types';

export const actions = {
	login: async ({ request }) => {
		const formData = await request.formData();
		const participant = normalizeParticipantDetails({
			name: String(formData.get('name') ?? ''),
			email: String(formData.get('email') ?? '')
		});

		if (!isValidParticipantDetails(participant)) {
			return fail(422, {
				success: false as const,
				name: participant.name,
				email: participant.email,
				error: 'Hãy nhập họ tên và địa chỉ email hợp lệ.'
			});
		}

		try {
			const submission = await findQuestionnaireSubmission(participant.name, participant.email);
			if (!submission) {
				return fail(404, {
					success: false as const,
					name: participant.name,
					email: participant.email,
					error: 'Không tìm thấy kết quả khớp với họ tên và email này.'
				});
			}

			return { success: true as const, submission };
		} catch {
			return fail(503, {
				success: false as const,
				name: participant.name,
				email: participant.email,
				error: 'Máy chủ lưu kết quả tạm thời chưa sẵn sàng. Hãy thử lại sau.'
			});
		}
	}
} satisfies Actions;
