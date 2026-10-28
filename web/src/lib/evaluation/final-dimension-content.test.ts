import { describe, expect, it } from 'vitest';
import {
	dimensionLevelFromPercent,
	dimensionLevelIds,
	finalDimensionDefinitions
} from './final-dimension-content';

describe('final dimension content', () => {
	it('loads every grouped DESMAP dimension and all five explanations', () => {
		expect(finalDimensionDefinitions).toHaveLength(28);
		expect(finalDimensionDefinitions.map((dimension) => dimension.id)).toEqual([
			'D1',
			'D2',
			'D3',
			'D4',
			'D5',
			'D6',
			'E1',
			'E2',
			'E3',
			'E4',
			'E5',
			'E6',
			'S1',
			'S2',
			'S3',
			'M1',
			'M2',
			'M3',
			'A1',
			'A2',
			'A3',
			'A4',
			'P1',
			'P2',
			'P3',
			'P4',
			'P5',
			'P6'
		]);
		for (const dimension of finalDimensionDefinitions) {
			for (const level of dimensionLevelIds) {
				expect(dimension.levelContent[level].length, `${dimension.id} ${level}`).toBeGreaterThan(
					20
				);
			}
		}
	});

	it('maps preview percentages to the five ordered states', () => {
		expect([0, 21, 41, 61, 81, 100].map(dimensionLevelFromPercent)).toEqual([
			'not-compatible',
			'low-compatible',
			'neutral',
			'fairly-compatible',
			'well-compatible',
			'well-compatible'
		]);
	});
});
