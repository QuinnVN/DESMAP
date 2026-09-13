import { getInitialAssessmentMode } from '$lib/server/initial-assessment-mode';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = () => ({
	initialAssessmentMode: getInitialAssessmentMode()
});
