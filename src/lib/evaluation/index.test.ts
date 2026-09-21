import { describe, expect, it } from 'vitest';
import { evaluationPageState, placeholderBehaviourComparison, type FinalAssessment } from './index';

const finalAssessment: FinalAssessment = {
	version: 1,
	assessmentId: 'assessment-one',
	completedAt: '2026-09-20T08:00:00.000Z'
};

describe('evaluation page state', () => {
	it('shows the initial assessment when no final assessment exists', () => {
		expect(evaluationPageState({ assessmentId: 'assessment-one' }, null)).toBe('initial');
	});

	it('shows the final assessment when both records have the same assessment identifier', () => {
		expect(evaluationPageState({ assessmentId: 'assessment-one' }, finalAssessment)).toBe('final');
	});

	it('does not attach a final assessment to another questionnaire submission', () => {
		expect(evaluationPageState({ assessmentId: 'assessment-two' }, finalAssessment)).toBe(
			'initial'
		);
	});
});

describe('placeholder behaviour comparison', () => {
	it('covers every Test vs Behaviour outcome with both evidence sources', () => {
		expect(placeholderBehaviourComparison.isPlaceholder).toBe(true);
		expect(placeholderBehaviourComparison.findings.map((finding) => finding.kind)).toEqual([
			'confirmed',
			'emerging',
			'development'
		]);
		for (const finding of placeholderBehaviourComparison.findings) {
			expect(finding.questionnaireResult).toBeTruthy();
			expect(finding.vrEvidence).toBeTruthy();
			expect(finding.summary).toBeTruthy();
		}
	});
});
