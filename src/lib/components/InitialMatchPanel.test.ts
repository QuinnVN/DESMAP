import { render } from 'svelte/server';
import { describe, expect, it } from 'vitest';
import InitialMatchPanel from './InitialMatchPanel.svelte';

const matches = [
	{
		career_id: 'doctor',
		career_name: 'Bác sĩ',
		match_percentage: 91
	}
];

describe('initial match panel', () => {
	it('hides calculated scores in weighted mode', () => {
		const { body } = render(InitialMatchPanel, { props: { mode: 'weighted', matches } });

		expect(body).toContain('Chưa đủ dữ liệu để đưa ra gợi ý');
		expect(body).not.toContain('91%');
		expect(body).not.toContain('Bác sĩ');
	});

	it('shows scores in AI mode', () => {
		const { body } = render(InitialMatchPanel, { props: { mode: 'ai', matches } });

		expect(body).toContain('Bác sĩ');
		expect(body).toContain('91%');
		expect(body).not.toContain('Chưa đủ dữ liệu để đưa ra gợi ý');
	});
});
