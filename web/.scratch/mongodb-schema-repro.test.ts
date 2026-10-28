import { afterAll, describe, expect, it } from 'vitest';
import { loadEnv } from 'vite';
import { MongoClient } from 'mongodb';
import {
	careerInterestOptions,
	desmapQuestions,
	isValidParticipantDetails,
	parseCompletionPayload
} from '../src/lib/questionnaire/data';

const runtimeEnv = loadEnv('', process.cwd(), 'MONGODB_');
const client = new MongoClient(runtimeEnv.MONGODB_URI, { serverSelectionTimeoutMS: 5_000 });

function describeValue(value: unknown): string {
	if (Array.isArray(value)) return `array(${value.length})`;
	if (value === null) return 'null';
	if (typeof value === 'object') return `object(${Object.keys(value).length})`;
	return typeof value;
}

describe('MongoDB questionnaire submission compatibility', () => {
	afterAll(async () => client.close());

	it('accepts every stored submission through the evaluation parser', async () => {
		const documents = await client
			.db(runtimeEnv.MONGODB_DATABASE || 'desmap')
			.collection('questionnaire_submissions')
			.find({})
			.sort({ updatedAt: -1, _id: -1 })
			.toArray();

		expect(documents.length, 'questionnaire_submissions is empty').toBeGreaterThan(0);

		const rejected = documents.flatMap((document, index) => {
			const submission = {
				version: document.version,
				completed: document.completed,
				assessmentId: document.assessmentId,
				startedAt: document.startedAt,
				completedAt: document.completedAt,
				participant: document.participant,
				careerInterests: document.careerInterests,
				answers: document.answers,
				scores: document.scores
			};
			if (parseCompletionPayload(submission)) return [];
			const answerIds = Object.keys(document.answers ?? {});
			const currentIds = new Set(desmapQuestions.map((question) => question.id));
			const startedAt = Date.parse(document.startedAt);
			const completedAt = Date.parse(document.completedAt);
			return [
				{
					index,
					diagnostics: {
						currentQuestionCount: currentIds.size,
						knownAnswerCount: answerIds.filter((id) => currentIds.has(id)).length,
						unknownAnswerCount: answerIds.filter((id) => !currentIds.has(id)).length,
						unknownAnswerIds: answerIds.filter((id) => !currentIds.has(id)).sort(),
						missingAnswerCount: desmapQuestions.filter(
							(question) => !(question.id in (document.answers ?? {}))
						).length,
						missingAnswerIds: desmapQuestions
							.filter((question) => !(question.id in (document.answers ?? {})))
							.map((question) => question.id)
							.sort(),
						invalidOptionCount: answerIds.filter(
							(id) => !['A', 'B', 'C'].includes(document.answers[id])
						).length,
						participantValid: isValidParticipantDetails(document.participant),
						careerInterestsValid:
							Array.isArray(document.careerInterests) &&
							document.careerInterests.every((id: unknown) =>
								careerInterestOptions.some((interest) => interest.id === id)
							),
						assessmentIdValid:
							typeof document.assessmentId === 'string' &&
							/^assessment-[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(
								document.assessmentId
							),
						timestampsValid:
							Number.isFinite(startedAt) &&
							Number.isFinite(completedAt) &&
							completedAt >= startedAt
					},
					shape: Object.fromEntries(
						Object.entries(submission).map(([key, value]) => [key, describeValue(value)])
					)
				}
			];
		});

		expect(
			rejected,
			`stored submissions were rejected; redacted shapes: ${JSON.stringify(rejected)}`
		).toEqual([]);
	});
});
