import { render } from 'svelte/server';
import { describe, expect, it } from 'vitest';
import FinalCareerSuggestions from './FinalCareerSuggestions.svelte';

const suggestions = [
	{
		id: 'doctor',
		name: 'Bác sĩ',
		compatibilityPercent: 91,
		description: 'Giải thích chi tiết dựa trên kết quả DESMAP và hành vi trong VR.'
	},
	{
		id: 'teacher',
		name: 'Giáo viên',
		compatibilityPercent: 82,
		description: 'Khả năng truyền đạt phù hợp với công việc giảng dạy.'
	},
	{
		id: 'lawyer',
		name: 'Luật sư',
		compatibilityPercent: 76,
		description: 'Tư duy phân tích phù hợp với ngành luật.'
	}
];

describe('final career suggestions', () => {
	it('emphasises one recommendation and keeps two alternatives concise', () => {
		const { body } = render(FinalCareerSuggestions, { props: { suggestions } });

		expect(body).toContain('Phù hợp nhất với hồ sơ của bạn');
		expect(body).toContain('Giải thích chi tiết dựa trên kết quả DESMAP và hành vi trong VR.');
		expect(body).toContain('Hai nghề cũng phù hợp');
		expect(body).toContain('Khả năng truyền đạt phù hợp với công việc giảng dạy.');
		expect(body).toContain('Tư duy phân tích phù hợp với ngành luật.');
	});

	it('does not render compatibility percentages or score bars', () => {
		const { body } = render(FinalCareerSuggestions, { props: { suggestions } });

		expect(body).not.toMatch(/91%|82%|76%/);
		expect(body).not.toContain('role="img"');
		expect(body).not.toContain('tương thích');
	});
});
