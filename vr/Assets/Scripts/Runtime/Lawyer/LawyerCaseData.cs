using System;
using System.Collections.Generic;
using UnityEngine;

namespace NCKH.VR.Lawyer
{
    public enum LawyerClueCategory
    {
        Timeline,
        Witness,
        CaseDetail,
        Objective,
        Testimony,
        Contradiction,
        Noise
    }

    public enum LawyerClueStrength
    {
        Strong,
        Weak,
        Noise
    }

    [Serializable]
    public sealed class LawyerClueDefinition
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string title = string.Empty;
        [SerializeField, TextArea(2, 6)] private string description = string.Empty;
        [SerializeField] private LawyerClueCategory category;

        public string Id => id;
        public string Title => title;
        public string Description => description;
        public LawyerClueCategory Category => category;

        public LawyerClueDefinition(string id, string title, string description, LawyerClueCategory category)
        {
            this.id = id ?? string.Empty;
            this.title = title ?? string.Empty;
            this.description = description ?? string.Empty;
            this.category = category;
        }
    }

    [Serializable]
    public sealed class LawyerRequiredClueLink
    {
        [SerializeField] private string firstClueId = string.Empty;
        [SerializeField] private string secondClueId = string.Empty;

        public string FirstClueId => firstClueId;
        public string SecondClueId => secondClueId;
        public LawyerClueLink Link => new LawyerClueLink(firstClueId, secondClueId);

        public LawyerRequiredClueLink(string firstClueId, string secondClueId)
        {
            this.firstClueId = firstClueId ?? string.Empty;
            this.secondClueId = secondClueId ?? string.Empty;
        }
    }

    [Serializable]
    public sealed class LawyerEvidenceChainSlot
    {
        [SerializeField] private List<string> eligibleClueIds = new List<string>();

        public IReadOnlyList<string> EligibleClueIds => eligibleClueIds;

        public LawyerEvidenceChainSlot(IEnumerable<string> clueIds)
        {
            eligibleClueIds = clueIds != null
                ? new List<string>(clueIds)
                : new List<string>();
        }

        public bool Accepts(string clueId) => eligibleClueIds.Contains(clueId);
    }

    [Serializable]
    public sealed class LawyerReasoningCardDefinition
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string title = string.Empty;
        [SerializeField, TextArea(2, 6)] private string description = string.Empty;
        [SerializeField] private List<LawyerRequiredClueLink> requiredLinks =
            new List<LawyerRequiredClueLink>();

        public string Id => id;
        public string Title => title;
        public string Description => description;
        public IReadOnlyList<LawyerRequiredClueLink> RequiredLinks => requiredLinks;

        public LawyerReasoningCardDefinition(
            string id,
            string title,
            string description,
            IEnumerable<LawyerRequiredClueLink> links)
        {
            this.id = id ?? string.Empty;
            this.title = title ?? string.Empty;
            this.description = description ?? string.Empty;
            requiredLinks = links != null
                ? new List<LawyerRequiredClueLink>(links)
                : new List<LawyerRequiredClueLink>();
        }
    }

    [Serializable]
    public sealed class LawyerCaseResolution
    {
        [SerializeField] private List<LawyerRequiredClueLink> requiredLinks =
            new List<LawyerRequiredClueLink>();
        [SerializeField, TextArea(3, 8)] private string truthConclusion = string.Empty;
        [SerializeField, TextArea(3, 8)] private string insufficientEvidenceMessage = string.Empty;
        [SerializeField] private List<LawyerReasoningCardDefinition> reasoningCards =
            new List<LawyerReasoningCardDefinition>();
        [SerializeField] private List<LawyerEvidenceChainSlot> evidenceChainSlots =
            new List<LawyerEvidenceChainSlot>();

        public IReadOnlyList<LawyerRequiredClueLink> RequiredLinks => requiredLinks;
        public string TruthConclusion => truthConclusion;
        public string InsufficientEvidenceMessage => insufficientEvidenceMessage;
        public IReadOnlyList<LawyerReasoningCardDefinition> ReasoningCards => reasoningCards;
        public IReadOnlyList<LawyerEvidenceChainSlot> EvidenceChainSlots => evidenceChainSlots;

        public LawyerCaseResolution(
            IEnumerable<LawyerRequiredClueLink> links,
            string conclusion,
            string insufficientMessage)
        {
            requiredLinks = links != null
                ? new List<LawyerRequiredClueLink>(links)
                : new List<LawyerRequiredClueLink>();
            truthConclusion = conclusion ?? string.Empty;
            insufficientEvidenceMessage = insufficientMessage ?? string.Empty;
            reasoningCards = new List<LawyerReasoningCardDefinition>();
        }

        public LawyerCaseResolution(
            IEnumerable<LawyerRequiredClueLink> links,
            string conclusion,
            string insufficientMessage,
            IEnumerable<LawyerReasoningCardDefinition> cards)
            : this(links, conclusion, insufficientMessage)
        {
            reasoningCards = cards != null
                ? new List<LawyerReasoningCardDefinition>(cards)
                : new List<LawyerReasoningCardDefinition>();
        }

        public LawyerCaseResolution(
            IEnumerable<LawyerEvidenceChainSlot> chainSlots,
            string conclusion,
            string insufficientMessage,
            IEnumerable<LawyerReasoningCardDefinition> cards)
            : this((IEnumerable<LawyerRequiredClueLink>)null, conclusion, insufficientMessage, cards)
        {
            evidenceChainSlots = chainSlots != null
                ? new List<LawyerEvidenceChainSlot>(chainSlots)
                : new List<LawyerEvidenceChainSlot>();
        }

        public bool Contains(LawyerClueLink link)
        {
            for (int index = 0; index < requiredLinks.Count; index++)
            {
                LawyerRequiredClueLink required = requiredLinks[index];
                if (required != null && required.Link.Equals(link))
                    return true;
            }
            return false;
        }

        public IReadOnlyList<LawyerReasoningCardDefinition> GetReasoningCardsOrFallback()
        {
            if (reasoningCards != null && reasoningCards.Count > 0)
                return reasoningCards;

            List<LawyerReasoningCardDefinition> fallback =
                new List<LawyerReasoningCardDefinition>();
            for (int index = 0; index < requiredLinks.Count; index++)
            {
                LawyerRequiredClueLink link = requiredLinks[index];
                if (link == null || !link.Link.IsValid)
                    continue;
                fallback.Add(new LawyerReasoningCardDefinition(
                    $"reasoning-{index + 1}",
                    "Suy luận từ chứng cứ",
                    "Mối liên hệ này tạo thành một luận điểm có thể trình bày trước tòa.",
                    new[] { new LawyerRequiredClueLink(link.FirstClueId, link.SecondClueId) }));
            }
            return fallback;
        }
    }

    [Serializable]
    public sealed class LawyerTestimony
    {
        [SerializeField, TextArea(4, 12)] private string transcript = string.Empty;
        [SerializeField] private AudioClip audioClip;
        [SerializeField] private List<string> revealedClueIds = new List<string>();

        public string Transcript => transcript;
        public AudioClip AudioClip => audioClip;
        public IReadOnlyList<string> RevealedClueIds => revealedClueIds;

        public LawyerTestimony(string transcript, AudioClip audioClip, IEnumerable<string> clueIds)
        {
            this.transcript = transcript ?? string.Empty;
            this.audioClip = audioClip;
            revealedClueIds = clueIds != null ? new List<string>(clueIds) : new List<string>();
        }
    }

    [Serializable]
    public sealed class LawyerFollowUpQuestion
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField, TextArea(2, 5)] private string prompt = string.Empty;
        [SerializeField] private LawyerTestimony response = new LawyerTestimony(string.Empty, null, null);

        public string Id => id;
        public string Prompt => prompt;
        public LawyerTestimony Response => response;

        public LawyerFollowUpQuestion(string id, string prompt, LawyerTestimony response)
        {
            this.id = id ?? string.Empty;
            this.prompt = prompt ?? string.Empty;
            this.response = response ?? new LawyerTestimony(string.Empty, null, null);
        }
    }

    [Serializable]
    public sealed class LawyerInterviewQuestion
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField, TextArea(2, 5)] private string prompt = string.Empty;
        [SerializeField] private bool keyQuestion;
        [SerializeField] private LawyerClueStrength strength = LawyerClueStrength.Weak;
        [SerializeField] private LawyerTestimony response = new LawyerTestimony(string.Empty, null, null);
        [SerializeField] private List<LawyerFollowUpQuestion> followUps = new List<LawyerFollowUpQuestion>();

        public string Id => id;
        public string Prompt => prompt;
        public bool IsKeyQuestion => keyQuestion;
        public LawyerClueStrength Strength => strength;
        public LawyerTestimony Response => response;
        public IReadOnlyList<LawyerFollowUpQuestion> FollowUps => followUps;

        public LawyerInterviewQuestion(
            string id,
            string prompt,
            bool keyQuestion,
            LawyerTestimony response,
            IEnumerable<LawyerFollowUpQuestion> followUps)
        {
            this.id = id ?? string.Empty;
            this.prompt = prompt ?? string.Empty;
            this.keyQuestion = keyQuestion;
            strength = keyQuestion ? LawyerClueStrength.Strong : LawyerClueStrength.Weak;
            this.response = response ?? new LawyerTestimony(string.Empty, null, null);
            this.followUps = followUps != null
                ? new List<LawyerFollowUpQuestion>(followUps)
                : new List<LawyerFollowUpQuestion>();
        }

        public LawyerInterviewQuestion(
            string id,
            string prompt,
            LawyerClueStrength clueStrength,
            LawyerTestimony response)
        {
            this.id = id ?? string.Empty;
            this.prompt = prompt ?? string.Empty;
            keyQuestion = clueStrength == LawyerClueStrength.Strong;
            strength = clueStrength;
            this.response = response ?? new LawyerTestimony(string.Empty, null, null);
            followUps = new List<LawyerFollowUpQuestion>();
        }

        public LawyerFollowUpQuestion GetFollowUp(string followUpId)
        {
            for (int index = 0; index < followUps.Count; index++)
            {
                LawyerFollowUpQuestion candidate = followUps[index];
                if (candidate != null && candidate.Id == followUpId)
                    return candidate;
            }

            return null;
        }
    }

    [Serializable]
    public sealed class LawyerInterviewWave
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string title = string.Empty;
        [SerializeField] private List<LawyerInterviewQuestion> questions =
            new List<LawyerInterviewQuestion>();

        public string Id => id;
        public string Title => title;
        public IReadOnlyList<LawyerInterviewQuestion> Questions => questions;

        public LawyerInterviewWave(
            string id,
            string title,
            IEnumerable<LawyerInterviewQuestion> configuredQuestions)
        {
            this.id = id ?? string.Empty;
            this.title = title ?? string.Empty;
            questions = configuredQuestions != null
                ? new List<LawyerInterviewQuestion>(configuredQuestions)
                : new List<LawyerInterviewQuestion>();
        }
    }

    [Serializable]
    public readonly struct LawyerClueLink : IEquatable<LawyerClueLink>
    {
        public string FirstId { get; }
        public string SecondId { get; }

        public LawyerClueLink(string firstId, string secondId)
        {
            if (string.CompareOrdinal(firstId, secondId) <= 0)
            {
                FirstId = firstId;
                SecondId = secondId;
            }
            else
            {
                FirstId = secondId;
                SecondId = firstId;
            }
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(FirstId)
            && !string.IsNullOrWhiteSpace(SecondId) && FirstId != SecondId;

        public bool Equals(LawyerClueLink other) => FirstId == other.FirstId && SecondId == other.SecondId;
        public override bool Equals(object obj) => obj is LawyerClueLink other && Equals(other);
        public override int GetHashCode() => ((FirstId != null ? FirstId.GetHashCode() : 0) * 397)
            ^ (SecondId != null ? SecondId.GetHashCode() : 0);
    }
}
