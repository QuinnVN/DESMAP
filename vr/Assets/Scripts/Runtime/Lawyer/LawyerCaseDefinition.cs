using System;
using System.Collections.Generic;
using UnityEngine;

namespace NCKH.VR.Lawyer
{
    [CreateAssetMenu(fileName = "Lawyer Case", menuName = "NCKH VR/Lawyer/Case")]
    public sealed class LawyerCaseDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField, Tooltip("Ví dụ: club-room-fire")] private string caseId = string.Empty;
        [SerializeField, Tooltip("Ví dụ: VỤ CHÁY PHÒNG CLB")] private string caseTitle = string.Empty;

        [Header("Case Brief")]
        [SerializeField, TextArea(3, 8), Tooltip("Ví dụ: Tóm tắt sự việc, nghi vấn và lời phủ nhận của thân chủ.")]
        private string caseSummary = string.Empty;
        [SerializeField, TextArea(2, 6), Tooltip("Ví dụ: Thu thập và đối chiếu chứng cứ để bảo vệ thân chủ.")]
        private string investigationObjective = string.Empty;
        [SerializeField] private List<string> timeline = new List<string>();
        [SerializeField] private List<string> witnesses = new List<string>();
        [SerializeField] private List<string> caseDetails = new List<string>();

        [Header("Clues")]
        [SerializeField] private List<LawyerClueDefinition> clues = new List<LawyerClueDefinition>();
        [SerializeField] private List<string> startingClueIds = new List<string>();

        [Header("Defendant Interview")]
        [SerializeField, Tooltip("Ví dụ: MINH")] private string defendantName = string.Empty;
        [SerializeField] private List<LawyerInterviewWave> interviewWaves =
            new List<LawyerInterviewWave>();
        [SerializeField, TextArea(4, 12)] private List<string> sampleAnswers =
            new List<string>();

        [Header("Legacy Interview Data")]
        [SerializeField, Min(1)] private int mainQuestionLimit = 2;
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
        public IReadOnlyList<LawyerInterviewWave> InterviewWaves => interviewWaves;
        public IReadOnlyList<string> SampleAnswers => sampleAnswers;
        public int MainQuestionLimit => mainQuestionLimit;
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
            for (int waveIndex = 0; waveIndex < interviewWaves.Count; waveIndex++)
            {
                LawyerInterviewWave wave = interviewWaves[waveIndex];
                if (wave == null)
                    continue;
                for (int questionIndex = 0; questionIndex < wave.Questions.Count; questionIndex++)
                {
                    LawyerInterviewQuestion waveQuestion = wave.Questions[questionIndex];
                    if (waveQuestion != null && waveQuestion.Id == questionId)
                        return waveQuestion;
                }
            }
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
            HashSet<string> clueIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> questionIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < clues.Count; index++)
            {
                LawyerClueDefinition clue = clues[index];
                if (clue == null || string.IsNullOrWhiteSpace(clue.Id) || !clueIds.Add(clue.Id))
                    errors.Add($"Manh mối ở vị trí {index} có mã trống hoặc trùng.");
            }

            if (interviewWaves.Count != 4)
                errors.Add("Hồ sơ luật sư phải có đúng 4 đợt hỏi.");
            HashSet<string> waveIds = new HashSet<string>(StringComparer.Ordinal);
            for (int waveIndex = 0; waveIndex < interviewWaves.Count; waveIndex++)
            {
                LawyerInterviewWave wave = interviewWaves[waveIndex];
                if (wave == null || string.IsNullOrWhiteSpace(wave.Id)
                    || !waveIds.Add(wave.Id))
                {
                    errors.Add($"Đợt hỏi ở vị trí {waveIndex} có mã trống hoặc trùng.");
                    continue;
                }
                if (wave.Questions.Count != 5)
                    errors.Add($"Đợt hỏi '{wave.Id}' phải có đúng 5 câu hỏi.");
                for (int questionIndex = 0; questionIndex < wave.Questions.Count; questionIndex++)
                {
                    LawyerInterviewQuestion question = wave.Questions[questionIndex];
                    if (question == null || string.IsNullOrWhiteSpace(question.Id)
                        || !questionIds.Add(question.Id))
                    {
                        errors.Add($"Câu hỏi ở đợt '{wave.Id}' có mã trống hoặc trùng.");
                        continue;
                    }
                    if (question.FollowUps.Count != 0)
                        errors.Add($"Câu hỏi '{question.Id}' không được có câu hỏi tiếp theo.");
                    if (question.Response == null || question.Response.RevealedClueIds.Count != 1)
                        errors.Add($"Câu hỏi '{question.Id}' phải mở đúng một manh mối.");
                    ValidateTestimony(question.Response, clueIds, question.Id, errors);
                }
            }

            if (sampleAnswers.Count != 5 || sampleAnswers.Exists(string.IsNullOrWhiteSpace))
                errors.Add("Hồ sơ luật sư phải có đúng 5 bài bào chữa mẫu.");

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
            if (string.IsNullOrWhiteSpace(resolution.TruthConclusion))
                errors.Add("Kết luận vụ án không được để trống.");
            if (string.IsNullOrWhiteSpace(resolution.InsufficientEvidenceMessage))
                errors.Add("Thông báo thiếu chứng cứ không được để trống.");

            IReadOnlyList<LawyerEvidenceChainSlot> slots = resolution.EvidenceChainSlots;
            if (slots == null || slots.Count < 2)
                errors.Add("Chuỗi chứng cứ phải có ít nhất hai vị trí.");
            else
            {
                for (int slotIndex = 0; slotIndex < slots.Count; slotIndex++)
                {
                    LawyerEvidenceChainSlot slot = slots[slotIndex];
                    if (slot == null || slot.EligibleClueIds.Count == 0)
                    {
                        errors.Add($"Vị trí chuỗi chứng cứ {slotIndex + 1} chưa có manh mối hợp lệ.");
                        continue;
                    }
                    HashSet<string> slotClues = new HashSet<string>(StringComparer.Ordinal);
                    for (int clueIndex = 0; clueIndex < slot.EligibleClueIds.Count; clueIndex++)
                    {
                        string clueId = slot.EligibleClueIds[clueIndex];
                        if (!clueIds.Contains(clueId))
                            errors.Add($"Vị trí chuỗi chứng cứ {slotIndex + 1} tham chiếu manh mối không tồn tại '{clueId}'.");
                        if (!slotClues.Add(clueId))
                            errors.Add($"Vị trí chuỗi chứng cứ {slotIndex + 1} có manh mối trùng '{clueId}'.");
                    }
                }
            }

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
                int expectedCardCount = slots != null ? Mathf.Max(0, slots.Count - 1) : 0;
                if (reasoningCards.Count != expectedCardCount)
                    errors.Add($"Chuỗi chứng cứ cần đúng {expectedCardCount} thẻ suy luận.");
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
                    if (card.RequiredLinks == null)
                        continue;
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
            IEnumerable<LawyerInterviewWave> configuredWaves,
            LawyerCaseResolution configuredResolution,
            IEnumerable<string> configuredSampleAnswers)
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
            interviewWaves = configuredWaves != null
                ? new List<LawyerInterviewWave>(configuredWaves)
                : new List<LawyerInterviewWave>();
            sampleAnswers = configuredSampleAnswers != null
                ? new List<string>(configuredSampleAnswers)
                : new List<string>();
            mainQuestionLimit = 1;
            questions = new List<LawyerInterviewQuestion>();
            resolution = configuredResolution ?? new LawyerCaseResolution(null, string.Empty, string.Empty);
        }
#endif
    }
}
