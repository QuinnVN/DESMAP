using System;
using UnityEngine;

namespace NCKH.VR.Sales
{
    public enum SalesPart2Objective
    {
        ListeningAndClarifying = 1,
        FindingRootCause = 2,
        ProposeSuitableSolution = 3,
        RebuildTrust = 4
    }

    public enum SalesPart2TrustState
    {
        Unknown = 0,
        Restored = 1,
        PartiallyRestored = 2,
        Lost = 3
    }

    [Serializable]
    public sealed class SalesCustomerProfile
    {
        public string name = "Lan";
        [Min(0)] public int age = 22;
        public string role = "Sinh viên đại học";
        public string playerAddress = "em";
        public string customerAddress = "chị";

        public bool IsValid => !string.IsNullOrWhiteSpace(name)
            && age > 0
            && !string.IsNullOrWhiteSpace(playerAddress)
            && !string.IsNullOrWhiteSpace(customerAddress);
    }

    [Serializable]
    public sealed class SalesStorePolicy
    {
        public string title = "CHÍNH SÁCH ĐỔI HÀNG";
        [TextArea(2, 4)] public string eligibility = "Trong 7 ngày • Giày còn nguyên vẹn";
        public string exclusions = "Không hoàn tiền hoặc giảm giá";
        public string escalation = "Tự xử lý, không chuyển quản lý";

        public string DisplayText => string.Join("\n", title, eligibility, exclusions, escalation);

        public bool IsValid => !string.IsNullOrWhiteSpace(title)
            && !string.IsNullOrWhiteSpace(eligibility)
            && !string.IsNullOrWhiteSpace(exclusions)
            && !string.IsNullOrWhiteSpace(escalation);
    }

    [Serializable]
    public sealed class SalesPart2ObjectiveData
    {
        public SalesPart2Objective objective;
        [TextArea(2, 4)] public string displayText;

        public SalesPart2ObjectiveData()
        {
        }

        public SalesPart2ObjectiveData(SalesPart2Objective objective, string displayText)
        {
            this.objective = objective;
            this.displayText = displayText;
        }

        public bool IsValid => (int)objective >= 1 && (int)objective <= 4
            && !string.IsNullOrWhiteSpace(displayText);
    }

    [Serializable]
    public sealed class SalesPart2AuthoredEndings
    {
        [TextArea(2, 4)] public string restored =
            "Ừ, vậy chị sẽ thử đôi nhẹ hơn. Lần sau chị vẫn sẽ quay lại nhờ em tư vấn.";
        [TextArea(2, 4)] public string partiallyRestored =
            "Được rồi, chị sẽ thử đổi sang đôi này trước.";
        [TextArea(2, 4)] public string lost =
            "Thôi, chị không muốn tiếp tục nữa. Lần sau chị sẽ mua ở chỗ khác.";

        public string For(SalesPart2TrustState state)
        {
            switch (state)
            {
                case SalesPart2TrustState.Restored: return restored;
                case SalesPart2TrustState.PartiallyRestored: return partiallyRestored;
                case SalesPart2TrustState.Lost: return lost;
                default: return string.Empty;
            }
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(restored)
            && !string.IsNullOrWhiteSpace(partiallyRestored)
            && !string.IsNullOrWhiteSpace(lost);
    }

    /// <summary>
    /// Authored content for the returning-customer conversation. It is intentionally
    /// separate from SalesMissionData, which represents Part 1's shoe comparison.
    /// </summary>
    [CreateAssetMenu(menuName = "NCKH/Sales/Part 2 Scenario Data")]
    public sealed class SalesPart2ScenarioData : ScriptableObject
    {
        public SalesCustomerProfile customerProfile = new SalesCustomerProfile();
        [TextArea(3, 6)] public string openingComplaint =
            "Em ơi, chị muốn đổi đôi giày này. Chị mới mua ở đây ba ngày trước, nhưng mang vào thì bị đau chân. Lần trước em tư vấn đôi này phù hợp với chị nên chị khá thất vọng.";
        public SalesStorePolicy storePolicy = new SalesStorePolicy();
        [TextArea(2, 4)] public string mandatoryTrustChallenge =
            "Nhưng lần trước em cũng tư vấn đôi này phù hợp với chị. Làm sao chị biết lần này sẽ không gặp vấn đề tương tự?";
        public SalesPart2ObjectiveData[] objectives =
        {
            new SalesPart2ObjectiveData(SalesPart2Objective.ListeningAndClarifying, "LẮNG NGHE VÀ LÀM RÕ PHÀN NÀN"),
            new SalesPart2ObjectiveData(SalesPart2Objective.FindingRootCause, "TÌM NGUYÊN NHÂN THỰC SỰ"),
            new SalesPart2ObjectiveData(SalesPart2Objective.ProposeSuitableSolution, "ĐỀ XUẤT GIẢI PHÁP PHÙ HỢP"),
            new SalesPart2ObjectiveData(SalesPart2Objective.RebuildTrust, "KHÔI PHỤC NIỀM TIN")
        };
        public SalesPart2AuthoredEndings authoredEndings = new SalesPart2AuthoredEndings();

        // Fixed facts are sent to the final analyzer and are never treated as
        // player-discovered evidence by the client.
        public string[] fixedFactIds =
        {
            "correct_size", "undamaged_exchange_eligible", "full_day_at_university",
            "walked_bus_stop_to_campus", "walked_between_classes", "late_day_discomfort",
            "previous_shoes_lighter", "thin_socks", "likes_color", "needs_mismatch_heavy_shoes"
        };

        public bool IsValid
        {
            get
            {
                if (!customerProfile.IsValid || !storePolicy.IsValid
                    || string.IsNullOrWhiteSpace(openingComplaint)
                    || string.IsNullOrWhiteSpace(mandatoryTrustChallenge)
                    || !authoredEndings.IsValid || objectives == null || objectives.Length != 4)
                    return false;
                for (int index = 0; index < objectives.Length; index++)
                    if (objectives[index] == null || !objectives[index].IsValid
                        || (int)objectives[index].objective != index + 1)
                        return false;
                return fixedFactIds != null && fixedFactIds.Length >= 8;
            }
        }

        public string ObjectiveText(SalesPart2Objective objective)
        {
            if (objectives != null)
                for (int index = 0; index < objectives.Length; index++)
                    if (objectives[index] != null && objectives[index].objective == objective)
                        return objectives[index].displayText;
            return objective.ToString();
        }

        public string Ending(SalesPart2TrustState state)
        {
            return authoredEndings != null ? authoredEndings.For(state) : string.Empty;
        }
    }
}
