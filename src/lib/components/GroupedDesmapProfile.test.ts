import { render } from 'svelte/server';
import { describe, expect, it } from 'vitest';
import { groupedDimensions } from '$lib/assessment';
import {
	questionnaireStages,
	type DimensionScore,
	type QuestionnaireScores
} from '$lib/questionnaire';
import GroupedDesmapProfile from './GroupedDesmapProfile.svelte';

const boundaryPercentages = [0, 12, 13, 37, 38, 62, 63, 87, 88, 100];

function score(percent: number): DimensionScore {
	return { raw: 0, max: 2, percent, answered: 1, questionCount: 1 };
}

function completeScores(): QuestionnaireScores {
	const groups = Object.fromEntries(
		groupedDimensions.map((dimension, index) => [
			dimension.id,
			score(boundaryPercentages[index] ?? 50)
		])
	);
	const stages = Object.fromEntries(
		questionnaireStages.map((stage, index) => [stage.id, score((index + 1) * 11)])
	) as QuestionnaireScores['stages'];

	return {
		overall: score(50),
		dimensions: {},
		groups,
		stages
	};
}

describe('detailed grouped DESMAP profile', () => {
	it('renders the canonical stages and grouped dimensions as accessible disclosures', () => {
		const { body } = render(GroupedDesmapProfile, { props: { scores: completeScores() } });
		const stageControls = [
			...body.matchAll(/<button[^>]*aria-expanded="(true|false)"[^>]*>([\s\S]*?)<\/button>/g)
		];

		expect(stageControls).toHaveLength(6);
		expect(stageControls.map((match) => match[1])).toEqual([
			'true',
			'false',
			'false',
			'false',
			'false',
			'false'
		]);
		expect(
			stageControls.map((match) =>
				match[2]
					.replace(/<[^>]+>/g, '')
					.replace(/\s+/g, ' ')
					.trim()
			)
		).toEqual([
			'MONG MUỐN 11%',
			'CHUYÊN MÔN 22%',
			'VAI TRÒ XÃ HỘI 33%',
			'TƯ DUY 44%',
			'THÍCH ỨNG 55%',
			'ÁP LỰC 66%'
		]);

		for (const dimension of groupedDimensions) {
			expect(body.split(`>${dimension.name}<`)).toHaveLength(2);
		}
		expect(body.match(/role="group"/g)).toHaveLength(28);
		expect(body.match(/aria-labelledby="[^"]+ [^"]+"/g)).toHaveLength(28);
	});

	it('maps normalized boundary percentages to the five agreed labels', () => {
		const { body } = render(GroupedDesmapProfile, { props: { scores: completeScores() } });
		const firstTenRows = [...body.matchAll(/<li[^>]*role="group"[^>]*>([\s\S]*?)<\/li>/g)].slice(
			0,
			10
		);
		const labels = firstTenRows.map((match) =>
			match[1]
				.replace(/<[^>]+>/g, '')
				.replace(/\s+/g, ' ')
				.trim()
		);

		expect(labels).toEqual([
			'Thu nhập, phúc lợi, ổn định Mức đánh giá: Không tương thích',
			'Học hỏi, phát triển, thử thách Mức đánh giá: Không tương thích',
			'Tự chủ Mức đánh giá: Kém tương thích',
			'Ý nghĩa và đóng góp Mức đánh giá: Kém tương thích',
			'Công nhận và ảnh hưởng Mức đánh giá: Bình thường',
			'Điều kiện và cân bằng công việc Mức đánh giá: Bình thường',
			'Kỹ năng nền tảng Mức đánh giá: Khá tương thích',
			'Giải quyết vấn đề phức tạp Mức đánh giá: Khá tương thích',
			'Kỹ năng tương tác xã hội Mức đánh giá: Tương thích tốt',
			'Kỹ năng kỹ thuật Mức đánh giá: Tương thích tốt'
		]);
	});

	it('does not expose raw grouped-score details or visual scales', () => {
		const { body } = render(GroupedDesmapProfile, { props: { scores: completeScores() } });

		for (const dimension of groupedDimensions) {
			expect(body).not.toContain(`>${dimension.id}<`);
			expect(body).not.toContain(dimension.description);
		}
		expect(body).not.toContain('role="progressbar"');
		expect(body).not.toContain('aria-valuenow');
		expect(body).not.toContain('Điểm thô');
		for (const percent of boundaryPercentages) {
			expect(body).not.toContain(`>${percent}%<`);
		}
	});
});
