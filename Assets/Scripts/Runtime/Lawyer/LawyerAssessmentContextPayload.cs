using System;
using System.Collections.Generic;
using UnityEngine;

namespace NCKH.VR.Lawyer
{
    [Serializable]
    public sealed class LawyerAssessmentEvidencePayload
    {
        public string evidenceId;
        public string title;
        public string description;
        public string strength;
    }

    [Serializable]
    public sealed class LawyerAssessmentReasoningPayload
    {
        public string reasoningId;
        public string title;
        public string description;
    }

    [Serializable]
    public sealed class LawyerAssessmentContextPayload
    {
        public string caseSummary;
        public string investigationObjective;
        public string defenseConclusion;
        public LawyerAssessmentEvidencePayload[] orderedEvidence;
        public LawyerAssessmentReasoningPayload[] reasoningCards;
        public string[] sampleAnswers;

        public static string BuildJson(LawyerCaseDefinition definition, LawyerGameSession session)
        {
            List<LawyerAssessmentEvidencePayload> evidence = new List<LawyerAssessmentEvidencePayload>();
            IReadOnlyList<string> orderedIds = session.GetOrderedEvidenceClueIds();
            for (int index = 0; index < orderedIds.Count; index++)
            {
                LawyerClueDefinition clue = definition.GetClue(orderedIds[index]);
                if (clue == null) continue;
                evidence.Add(new LawyerAssessmentEvidencePayload
                {
                    evidenceId = clue.Id,
                    title = clue.Title,
                    description = clue.Description,
                    strength = session.GetSelectedClueStrength(clue.Id).ToString().ToLowerInvariant()
                });
            }

            List<LawyerAssessmentReasoningPayload> reasoning = new List<LawyerAssessmentReasoningPayload>();
            IReadOnlyList<LawyerReasoningCardDefinition> cards = definition.Resolution?.ReasoningCards;
            if (cards != null)
            {
                for (int index = 0; index < cards.Count; index++)
                {
                    LawyerReasoningCardDefinition card = cards[index];
                    if (card == null) continue;
                    reasoning.Add(new LawyerAssessmentReasoningPayload
                    {
                        reasoningId = card.Id,
                        title = card.Title,
                        description = card.Description
                    });
                }
            }

            LawyerAssessmentContextPayload payload = new LawyerAssessmentContextPayload
            {
                caseSummary = definition.CaseSummary,
                investigationObjective = definition.InvestigationObjective,
                defenseConclusion = definition.Resolution?.TruthConclusion ?? string.Empty,
                orderedEvidence = evidence.ToArray(),
                reasoningCards = reasoning.ToArray(),
                sampleAnswers = Copy(definition.SampleAnswers)
            };
            return JsonUtility.ToJson(payload);
        }

        private static string[] Copy(IReadOnlyList<string> values)
        {
            if (values == null) return Array.Empty<string>();
            string[] result = new string[values.Count];
            for (int index = 0; index < values.Count; index++) result[index] = values[index];
            return result;
        }
    }
}
