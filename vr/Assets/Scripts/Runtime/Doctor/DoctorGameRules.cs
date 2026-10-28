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
                if (card == null || !card.IsEssential || string.IsNullOrWhiteSpace(card.Id) || !scoredIds.Add(card.Id))
                    continue;

                if (placements.TryGetValue(card.Id, out DoctorInfoCategory? placement)
                    && placement.HasValue
                    && placement.Value == card.CorrectCategory)
                {
                    correctCount++;
                }
            }

            return correctCount;
        }

        public static int CycleSelection(int currentIndex, int direction, int itemCount)
        {
            if (itemCount <= 0)
                return -1;
            if (currentIndex < 0 || currentIndex >= itemCount)
                currentIndex = 0;
            if (direction == 0)
                return currentIndex;

            int next = currentIndex + Math.Sign(direction);
            if (next < 0)
                next = itemCount - 1;
            else if (next >= itemCount)
                next = 0;
            return next;
        }

        public static float CalculateQuestionItemHeight(
            float questionAreaHeight,
            int itemCount,
            float verticalPadding,
            float spacing,
            float preferredHeight)
        {
            if (itemCount <= 0 || questionAreaHeight <= 0f || preferredHeight <= 0f)
                return 0f;

            float safeHeight = Math.Max(0f, questionAreaHeight);
            float safePadding = Math.Max(0f, verticalPadding);
            float safeSpacing = Math.Max(0f, spacing);
            float gaps = safeSpacing * Math.Max(0, itemCount - 1);
            float availableForItems = safeHeight - safePadding * 2f - gaps;
            if (availableForItems <= 0f)
                return 0f;

            return Math.Min(preferredHeight, availableForItems / itemCount);
        }
    }

    public sealed class DoctorQuestionTracker
    {
        private readonly HashSet<string> askedIds = new HashSet<string>(StringComparer.Ordinal);

        public int QuestionsAsked => askedIds.Count;

        public bool HasAsked(string id) => !string.IsNullOrWhiteSpace(id) && askedIds.Contains(id);

        public bool TryAsk(string id)
        {
            return !string.IsNullOrWhiteSpace(id) && askedIds.Add(id);
        }

        public void Reset() => askedIds.Clear();

        public void GetRemaining(IReadOnlyList<DoctorQuestionPhase> phases, List<DoctorQuestionPhase> destination)
        {
            destination.Clear();
            if (phases == null)
                return;

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < phases.Count; i++)
            {
                DoctorQuestionPhase phase = phases[i];
                if (phase != null && phase.IsValid && seen.Add(phase.Id) && !askedIds.Contains(phase.Id))
                    destination.Add(phase);
            }
        }
    }
}
