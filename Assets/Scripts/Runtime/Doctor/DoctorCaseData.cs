using System;
using System.Collections.Generic;
using UnityEngine;

namespace NCKH.VR.Doctor
{
    public enum DoctorInfoCategory
    {
        Time,
        TravelHistory,
        Symptoms,
        MedicalHistory
    }

    [Serializable]
    public sealed class DoctorInfoCard
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField, TextArea(2, 4)] private string text = string.Empty;
        [SerializeField] private DoctorInfoCategory correctCategory;
        [SerializeField] private bool essential = true;

        public string Id => id;
        public string Text => text;
        public DoctorInfoCategory CorrectCategory => correctCategory;
        public bool IsEssential => essential;

        public DoctorInfoCard(string id, string text, DoctorInfoCategory correctCategory, bool essential)
        {
            this.id = id ?? string.Empty;
            this.text = text ?? string.Empty;
            this.correctCategory = correctCategory;
            this.essential = essential;
        }
    }

    [Serializable]
    public sealed class DoctorDialogue
    {
        [SerializeField, TextArea(4, 10)] private string transcript = string.Empty;
        [SerializeField] private AudioClip audioClip;
        [SerializeField] private List<DoctorInfoCard> selectableCards = new List<DoctorInfoCard>();

        public string Transcript => transcript;
        public AudioClip AudioClip => audioClip;
        public IReadOnlyList<DoctorInfoCard> SelectableCards => selectableCards;

        public DoctorDialogue(string transcript, AudioClip audioClip, IEnumerable<DoctorInfoCard> selectableCards)
        {
            this.transcript = transcript ?? string.Empty;
            this.audioClip = audioClip;
            this.selectableCards = selectableCards != null
                ? new List<DoctorInfoCard>(selectableCards)
                : new List<DoctorInfoCard>();
        }
    }

    [Serializable]
    public sealed class DoctorQuestionPhase
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField, TextArea(2, 4)] private string question = string.Empty;
        [SerializeField] private DoctorDialogue response =
            new DoctorDialogue("Câu trả lời mẫu.", null, null);

        public string Id => id;
        public string Question => question;
        public DoctorDialogue Response => response;
        public bool IsValid => !string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(question);

        public DoctorQuestionPhase(string id, string question, DoctorDialogue response)
        {
            this.id = id ?? string.Empty;
            this.question = question ?? string.Empty;
            this.response = response ?? new DoctorDialogue(string.Empty, null, null);
        }
    }
}
