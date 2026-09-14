import { env } from '$env/dynamic/private';
import { attachDatabasePool } from '@vercel/functions/db-connections';
import { MongoClient, MongoServerError, type Collection } from 'mongodb';
import type { QuestionnaireSubmission } from '$lib/questionnaire';

type StoredQuestionnaireSubmission = QuestionnaireSubmission & {
	normalizedEmail: string;
	syncStatus: 'synced';
	createdAt: Date;
	updatedAt: Date;
};

let collectionPromise: Promise<Collection<StoredQuestionnaireSubmission>> | undefined;

async function getCollection(): Promise<Collection<StoredQuestionnaireSubmission>> {
	if (!env.MONGODB_URI) throw new Error('MONGODB_URI is not configured.');
	collectionPromise ??= (async () => {
		const client = new MongoClient(env.MONGODB_URI as string, {
			maxIdleTimeMS: 5_000,
			serverSelectionTimeoutMS: 5_000
		});
		attachDatabasePool(client);
		await client.connect();
		const collection = client
			.db(env.MONGODB_DATABASE || 'desmap')
			.collection<StoredQuestionnaireSubmission>('questionnaire_submissions');
		await Promise.all([
			collection.createIndex({ normalizedEmail: 1 }, { unique: true }),
			collection.createIndex({ assessmentId: 1 }, { unique: true })
		]);
		return collection;
	})();
	try {
		return await collectionPromise;
	} catch (error) {
		collectionPromise = undefined;
		throw error;
	}
}

export async function emailHasAssessment(normalizedEmail: string): Promise<boolean> {
	const collection = await getCollection();
	return Boolean(await collection.findOne({ normalizedEmail }, { projection: { _id: 1 } }));
}

export async function saveQuestionnaireSubmission(
	payload: QuestionnaireSubmission
): Promise<'created' | 'updated' | 'conflict'> {
	const collection = await getCollection();
	const normalizedEmail = payload.participant.email;
	const existing = await collection.findOne({ normalizedEmail });
	if (existing) {
		if (existing.assessmentId !== payload.assessmentId) return 'conflict';
		await collection.updateOne(
			{ _id: existing._id },
			{ $set: { ...payload, normalizedEmail, syncStatus: 'synced', updatedAt: new Date() } }
		);
		return 'updated';
	}

	const now = new Date();
	try {
		await collection.insertOne({
			...payload,
			normalizedEmail,
			syncStatus: 'synced',
			createdAt: now,
			updatedAt: now
		});
		return 'created';
	} catch (error) {
		if (!(error instanceof MongoServerError) || error.code !== 11000) throw error;
		const raced = await collection.findOne({ normalizedEmail });
		if (raced?.assessmentId === payload.assessmentId) return 'updated';
		return 'conflict';
	}
}
