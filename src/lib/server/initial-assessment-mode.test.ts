import { describe, expect, it } from 'vitest';
import { parseInitialAssessmentMode } from './initial-assessment-mode';

describe('initial assessment mode', () => {
	it.each(['true', 'TRUE', 'True'])('uses weighted scoring for %s', (value) => {
		expect(parseInitialAssessmentMode(value)).toBe('weighted');
	});

	it.each([undefined, 'false', 'FALSE', '', '1', 'yes', 'invalid'])('uses AI for %s', (value) => {
		expect(parseInitialAssessmentMode(value)).toBe('ai');
	});
});
