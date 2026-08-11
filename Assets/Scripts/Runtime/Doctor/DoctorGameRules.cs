using System;
using System.Collections.Generic;

namespace NCKH.VR.Doctor
{
    public static class DoctorGameRules
    {
        public static int CountCorrectNotes(
            IEnumerable<DoctorInfoCard> cards,
            IReadOnlyDictionary<string, DoctorInfoCategory?> placements)
        {
            if (cards == null || placements == null)
                return 0;

            int correctCount = 0;
            HashSet<string> scoredIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (DoctorInfoCard card in cards)
            {
                if (card == null
                    || !card.IsEssential
                    || string.IsNullOrWhiteSpace(card.Id)
                    || !scoredIds.Add(card.Id))
                {
                    continue;
                }

                if (placements.TryGetValue(card.Id, out DoctorInfoCategory? placement)
                    && placement.HasValue
                    && placement.Value == card.CorrectCategory)
                {
                    correctCount++;
                }
            }

            return correctCount;
        }
    }

    public sealed class DoctorQuestionTracker
    {
        private readonly HashSet<DoctorInfoCategory> askedCategories =
            new HashSet<DoctorInfoCategory>();

        public int MaximumQuestions { get; private set; }
        public int QuestionsAsked => askedCategories.Count;
        public bool CanAskAny => QuestionsAsked < MaximumQuestions;

        public DoctorQuestionTracker(int maximumQuestions)
        {
            Reset(maximumQuestions);
        }

        public bool HasAsked(DoctorInfoCategory category)
        {
            return askedCategories.Contains(category);
        }

        public bool TryAsk(DoctorInfoCategory category)
        {
            if (!CanAskAny || askedCategories.Contains(category))
                return false;

            askedCategories.Add(category);
            return true;
        }

        public void Reset(int maximumQuestions)
        {
            MaximumQuestions = Math.Max(0, maximumQuestions);
            askedCategories.Clear();
        }
    }
}
