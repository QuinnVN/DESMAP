export {
	QUESTIONNAIRE_STORAGE_KEY,
	QUESTIONNAIRE_COMPLETION_STORAGE_KEY,
	buildCompletionPayload,
	careerInterestOptions,
	clearSavedQuestionnaire,
	desmapQuestions,
	getQuestionById,
	getStageById,
	parseQuestionSource,
	questionnaireStages,
	readSavedQuestionnaire,
	readCompletionPayload,
	scoreAnswers,
	totalQuestionCount,
	writeSavedQuestionnaire,
	writeCompletionPayload
} from './data';

export type {
	CareerInterest,
	DesmapQuestion,
	DimensionScore,
	OptionLetter,
	QuestionnaireAnswers,
	QuestionnaireDraft,
	QuestionnaireScores,
	QuestionnaireStage,
	QuestionnaireSubmission,
	StageId,
	QuestionOption
} from './data';
