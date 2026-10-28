import dimensionDefinitions from './final-dimension-content.json';
import type { StageId } from '$lib/questionnaire';

export const dimensionLevelIds = [
	'not-compatible',
	'low-compatible',
	'neutral',
	'fairly-compatible',
	'well-compatible'
] as const;

export type DimensionLevelId = (typeof dimensionLevelIds)[number];

export type DimensionDefinition = {
	id: string;
	stage: StageId;
	name: string;
	description: string;
	levelContent: Record<DimensionLevelId, string>;
};

export const dimensionLevelLabels: Record<DimensionLevelId, string> = {
	'not-compatible': 'Không tương thích',
	'low-compatible': 'Kém tương thích',
	neutral: 'Bình thường',
	'fairly-compatible': 'Khá tương thích',
	'well-compatible': 'Tương thích tốt'
};

export const finalDimensionDefinitions = dimensionDefinitions as DimensionDefinition[];

export function dimensionLevelFromPercent(percent: number): DimensionLevelId {
	if (percent <= 20) return 'not-compatible';
	if (percent <= 40) return 'low-compatible';
	if (percent <= 60) return 'neutral';
	if (percent <= 80) return 'fairly-compatible';
	return 'well-compatible';
}
