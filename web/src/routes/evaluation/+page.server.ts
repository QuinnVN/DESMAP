import { dev } from '$app/environment';
import { getInitialAssessmentMode } from '$lib/server/initial-assessment-mode';
import type { FinalAssessment } from '$lib/evaluation';
import type { PageServerLoad } from './$types';

async function loadFinalAssessment(): Promise<FinalAssessment | null> {
	// Replace this stub with the MongoDB lookup when final assessments are persisted.
	return null;
}

export const load: PageServerLoad = async ({ url }) => {
	return {
		initialAssessmentMode: getInitialAssessmentMode(),
		finalAssessment: await loadFinalAssessment(),
		previewFinal: dev && url.searchParams.get('preview') === 'final'
	};
};
