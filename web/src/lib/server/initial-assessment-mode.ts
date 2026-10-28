import { env } from '$env/dynamic/private';
import type { InitialAssessmentMode } from '$lib/assessment';

export function parseInitialAssessmentMode(flagValue: string | undefined): InitialAssessmentMode {
	return flagValue?.toLowerCase() === 'true' ? 'weighted' : 'ai';
}

export function getInitialAssessmentMode(): InitialAssessmentMode {
	return parseInitialAssessmentMode(env.DISABLE_AI_INIT_ASSESSMENT);
}
