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
        [SerializeField] private LawyerTestimony response = new LawyerTestimony(string.Empty, null, null);
        [SerializeField] private List<LawyerFollowUpQuestion> followUps = new List<LawyerFollowUpQuestion>();

        public string Id => id;
        public string Prompt => prompt;
        public bool IsKeyQuestion => keyQuestion;
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
            this.response = response ?? new LawyerTestimony(string.Empty, null, null);
            this.followUps = followUps != null
                ? new List<LawyerFollowUpQuestion>(followUps)
                : new List<LawyerFollowUpQuestion>();
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
