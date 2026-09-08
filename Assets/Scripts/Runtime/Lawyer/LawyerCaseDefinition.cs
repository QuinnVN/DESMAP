using System;
using System.Collections.Generic;
using UnityEngine;

namespace NCKH.VR.Lawyer
{
    [CreateAssetMenu(fileName = "Lawyer Case", menuName = "NCKH VR/Lawyer/Case")]
    public sealed class LawyerCaseDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string caseId = "lawyer-placeholder";
        [SerializeField] private string caseTitle = "[NỘI DUNG MẪU] Hồ sơ vụ án";

        [Header("Case Brief")]
        [SerializeField, TextArea(3, 8)] private string caseSummary = string.Empty;
        [SerializeField, TextArea(2, 6)] private string investigationObjective = string.Empty;
        [SerializeField] private List<string> timeline = new List<string>();
        [SerializeField] private List<string> witnesses = new List<string>();
        [SerializeField] private List<string> caseDetails = new List<string>();

        [Header("Clues")]
        [SerializeField] private List<LawyerClueDefinition> clues = new List<LawyerClueDefinition>();
        [SerializeField] private List<string> startingClueIds = new List<string>();

        [Header("Defendant Interview")]
        [SerializeField] private string defendantName = "Bị cáo mẫu";
        [SerializeField] private List<LawyerInterviewQuestion> questions = new List<LawyerInterviewQuestion>();

        [Header("Evidence Resolution")]
        [SerializeField] private LawyerCaseResolution resolution = new LawyerCaseResolution(
            null, string.Empty, string.Empty);

        public string CaseId => caseId;
        public string CaseTitle => caseTitle;
        public string CaseSummary => caseSummary;
        public string InvestigationObjective => investigationObjective;
        public IReadOnlyList<string> Timeline => timeline;
        public IReadOnlyList<string> Witnesses => witnesses;
        public IReadOnlyList<string> CaseDetails => caseDetails;
        public IReadOnlyList<LawyerClueDefinition> Clues => clues;
        public IReadOnlyList<string> StartingClueIds => startingClueIds;
        public string DefendantName => defendantName;
        public IReadOnlyList<LawyerInterviewQuestion> Questions => questions;
        public LawyerCaseResolution Resolution => resolution;

        public LawyerClueDefinition GetClue(string clueId)
        {
            for (int index = 0; index < clues.Count; index++)
            {
                LawyerClueDefinition clue = clues[index];
                if (clue != null && clue.Id == clueId)
                    return clue;
            }

            return null;
        }

        public LawyerInterviewQuestion GetQuestion(string questionId)
        {
            for (int index = 0; index < questions.Count; index++)
            {
                LawyerInterviewQuestion question = questions[index];
                if (question != null && question.Id == questionId)
                    return question;
            }

            return null;
        }

        public void GetValidationErrors(List<string> errors)
        {
            if (errors == null)
                throw new ArgumentNullException(nameof(errors));
            errors.Clear();

            if (questions.Count != 5)
                errors.Add("Hồ sơ luật sư phải có đúng 5 câu hỏi chính.");

            int keyCount = 0;
            HashSet<string> clueIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> questionIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < clues.Count; index++)
            {
                LawyerClueDefinition clue = clues[index];
                if (clue == null || string.IsNullOrWhiteSpace(clue.Id) || !clueIds.Add(clue.Id))
                    errors.Add($"Manh mối ở vị trí {index} có mã trống hoặc trùng.");
            }

            for (int index = 0; index < questions.Count; index++)
            {
                LawyerInterviewQuestion question = questions[index];
                if (question == null || string.IsNullOrWhiteSpace(question.Id)
                    || !questionIds.Add(question.Id))
                {
                    errors.Add($"Câu hỏi ở vị trí {index} có mã trống hoặc trùng.");
                    continue;
                }

                if (question.IsKeyQuestion)
                {
                    keyCount++;
                    if (question.FollowUps.Count != 3)
                        errors.Add($"Câu hỏi chính '{question.Id}' phải có đúng 3 câu hỏi tiếp theo.");
                }
                else if (question.FollowUps.Count != 0)
                {
                    errors.Add($"Câu hỏi nhiễu '{question.Id}' không được có câu hỏi tiếp theo.");
                }

                ValidateTestimony(question.Response, clueIds, question.Id, errors);
                HashSet<string> followUpIds = new HashSet<string>(StringComparer.Ordinal);
                for (int followIndex = 0; followIndex < question.FollowUps.Count; followIndex++)
                {
                    LawyerFollowUpQuestion followUp = question.FollowUps[followIndex];
                    if (followUp == null || string.IsNullOrWhiteSpace(followUp.Id)
                        || !followUpIds.Add(followUp.Id))
                    {
                        errors.Add($"Câu hỏi tiếp theo của '{question.Id}' có mã trống hoặc trùng.");
                        continue;
                    }
                    ValidateTestimony(followUp.Response, clueIds, followUp.Id, errors);
                }
            }

            if (keyCount != 2)
                errors.Add("Hồ sơ luật sư phải có đúng 2 câu hỏi chính xác.");

            for (int index = 0; index < startingClueIds.Count; index++)
                if (!clueIds.Contains(startingClueIds[index]))
                    errors.Add($"Manh mối ban đầu '{startingClueIds[index]}' không tồn tại.");

            ValidateResolution(clueIds, errors);
        }

        private void ValidateResolution(HashSet<string> clueIds, List<string> errors)
        {
            if (resolution == null)
            {
                errors.Add("Hồ sơ luật sư chưa có kết luận vụ án.");
                return;
            }
            if (resolution.RequiredLinks.Count == 0)
                errors.Add("Kết luận vụ án phải có ít nhất một liên kết bắt buộc.");
            if (string.IsNullOrWhiteSpace(resolution.TruthConclusion))
                errors.Add("Kết luận vụ án không được để trống.");
            if (string.IsNullOrWhiteSpace(resolution.InsufficientEvidenceMessage))
                errors.Add("Thông báo thiếu chứng cứ không được để trống.");

            HashSet<LawyerClueLink> links = new HashSet<LawyerClueLink>();
            for (int index = 0; index < resolution.RequiredLinks.Count; index++)
            {
                LawyerRequiredClueLink required = resolution.RequiredLinks[index];
                if (required == null || !required.Link.IsValid)
                {
                    errors.Add($"Liên kết bắt buộc ở vị trí {index} không hợp lệ.");
                    continue;
                }
                LawyerClueLink link = required.Link;
                if (!clueIds.Contains(link.FirstId) || !clueIds.Contains(link.SecondId))
                    errors.Add($"Liên kết bắt buộc '{link.FirstId} - {link.SecondId}' tham chiếu manh mối không tồn tại.");
                if (!links.Add(link))
                    errors.Add($"Liên kết bắt buộc '{link.FirstId} - {link.SecondId}' bị trùng.");
            }

            IReadOnlyList<LawyerReasoningCardDefinition> reasoningCards =
                resolution.ReasoningCards;
            if (reasoningCards != null)
            {
                HashSet<string> reasoningIds = new HashSet<string>(StringComparer.Ordinal);
                for (int index = 0; index < reasoningCards.Count; index++)
                {
                    LawyerReasoningCardDefinition card = reasoningCards[index];
                    if (card == null || string.IsNullOrWhiteSpace(card.Id)
                        || !reasoningIds.Add(card.Id))
                    {
                        errors.Add($"Thẻ suy luận ở vị trí {index} có mã trống hoặc trùng.");
                        continue;
                    }
                    if (card.RequiredLinks == null || card.RequiredLinks.Count == 0)
                    {
                        errors.Add($"Thẻ suy luận '{card.Id}' phải tham chiếu ít nhất một liên kết.");
                        continue;
                    }
                    for (int linkIndex = 0; linkIndex < card.RequiredLinks.Count; linkIndex++)
                    {
                        LawyerRequiredClueLink required = card.RequiredLinks[linkIndex];
                        if (required == null || !required.Link.IsValid
                            || !links.Contains(required.Link))
                        {
                            errors.Add($"Thẻ suy luận '{card.Id}' tham chiếu liên kết không hợp lệ.");
                        }
                    }
                }
            }
        }

        private static void ValidateTestimony(
            LawyerTestimony testimony,
            HashSet<string> clueIds,
            string ownerId,
            List<string> errors)
        {
            if (testimony == null)
                return;
            for (int index = 0; index < testimony.RevealedClueIds.Count; index++)
                if (!clueIds.Contains(testimony.RevealedClueIds[index]))
                    errors.Add($"'{ownerId}' tham chiếu manh mối không tồn tại '{testimony.RevealedClueIds[index]}'.");
        }

#if UNITY_EDITOR
        public void ConfigureInEditor(
            string configuredCaseId,
            string title,
            string summary,
            string objective,
            IEnumerable<string> configuredTimeline,
            IEnumerable<string> configuredWitnesses,
            IEnumerable<string> configuredDetails,
            IEnumerable<LawyerClueDefinition> configuredClues,
            IEnumerable<string> configuredStartingClues,
            string configuredDefendantName,
            IEnumerable<LawyerInterviewQuestion> configuredQuestions,
            LawyerCaseResolution configuredResolution)
        {
            caseId = configuredCaseId ?? string.Empty;
            caseTitle = title ?? string.Empty;
            caseSummary = summary ?? string.Empty;
            investigationObjective = objective ?? string.Empty;
            timeline = configuredTimeline != null ? new List<string>(configuredTimeline) : new List<string>();
            witnesses = configuredWitnesses != null ? new List<string>(configuredWitnesses) : new List<string>();
            caseDetails = configuredDetails != null ? new List<string>(configuredDetails) : new List<string>();
            clues = configuredClues != null ? new List<LawyerClueDefinition>(configuredClues) : new List<LawyerClueDefinition>();
            startingClueIds = configuredStartingClues != null ? new List<string>(configuredStartingClues) : new List<string>();
            defendantName = configuredDefendantName ?? string.Empty;
            questions = configuredQuestions != null ? new List<LawyerInterviewQuestion>(configuredQuestions) : new List<LawyerInterviewQuestion>();
            resolution = configuredResolution ?? new LawyerCaseResolution(null, string.Empty, string.Empty);
        }
#endif
    }
}
