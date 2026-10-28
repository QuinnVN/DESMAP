using System.Linq;
using NCKH.VR.Sales.Editor;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace NCKH.VR.Sales.Tests
{
    public sealed class SalesPart2SceneTests
    {
        [Test]
        public void DifficultCustomerScenarioUsesTheOrderedVietnameseContract()
        {
            SalesPart2ScenarioData scenario = AssetDatabase.LoadAssetAtPath<SalesPart2ScenarioData>(
                SalesPart2ProjectSetup.ScenarioPath);
            Assert.That(scenario, Is.Not.Null, "Run the Part 2 authoring command before this scene contract test.");
            Assert.That(scenario.IsValid, Is.True);
            Assert.That(scenario.customerProfile.name, Is.EqualTo("Lan"));
            Assert.That(scenario.customerProfile.age, Is.EqualTo(22));
            Assert.That(scenario.openingComplaint, Does.StartWith("Em ơi, chị muốn đổi đôi giày này."));
            Assert.That(scenario.storePolicy.DisplayText, Does.Contain("Trong 7 ngày • Giày còn nguyên vẹn"));
            Assert.That(scenario.storePolicy.DisplayText, Does.Contain("Không hoàn tiền hoặc giảm giá"));
            Assert.That(scenario.storePolicy.DisplayText, Does.Contain("Tự xử lý, không chuyển quản lý"));
            Assert.That(scenario.objectives.Select(o => (int)o.objective), Is.EqualTo(new[] { 1, 2, 3, 4 }));
            Assert.That(scenario.ObjectiveText(SalesPart2Objective.ListeningAndClarifying),
                Is.EqualTo("LẮNG NGHE VÀ LÀM RÕ PHÀN NÀN"));
            Assert.That(scenario.ObjectiveText(SalesPart2Objective.FindingRootCause),
                Is.EqualTo("TÌM NGUYÊN NHÂN THỰC SỰ"));
            Assert.That(scenario.ObjectiveText(SalesPart2Objective.ProposeSuitableSolution),
                Is.EqualTo("ĐỀ XUẤT GIẢI PHÁP PHÙ HỢP"));
            Assert.That(scenario.ObjectiveText(SalesPart2Objective.RebuildTrust),
                Is.EqualTo("KHÔI PHỤC NIỀM TIN"));
            Assert.That(scenario.authoredEndings.For(SalesPart2TrustState.Restored),
                Is.EqualTo("Ừ, vậy chị sẽ thử đôi nhẹ hơn. Lần sau chị vẫn sẽ quay lại nhờ em tư vấn."));
            Assert.That(scenario.authoredEndings.For(SalesPart2TrustState.PartiallyRestored),
                Is.EqualTo("Được rồi, chị sẽ thử đổi sang đôi này trước."));
            Assert.That(scenario.authoredEndings.For(SalesPart2TrustState.Lost),
                Is.EqualTo("Thôi, chị không muốn tiếp tục nữa. Lần sau chị sẽ mua ở chỗ khác."));
        }

        [Test]
        public void StoreWiresPart1TransitionPart2HudAndCustomer()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(SalesPart2ProjectSetup.ScenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                SalesPart2ProjectSetup.ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);
            try
            {
                var controllers = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SalesPart2Controller>(true)).ToArray();
                Assert.That(controllers, Has.Length.EqualTo(1));
                var controller = controllers[0];
                var adapter = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SalesRemoteSceneAdapter>(true)).Single();
                Assert.That(adapter.part1, Is.Not.Null);
                Assert.That(adapter.part2, Is.SameAs(controller));
                Assert.That(adapter.SceneId, Is.EqualTo("sale"));
                Assert.That(controller.GetComponent<SalesPart2RequestService>(), Is.Not.Null);
                Assert.That(controller.scenario, Is.Not.Null);
                Assert.That(controller.xrOrigin, Is.Not.Null);
                Assert.That(controller.customer, Is.Not.Null);
                Assert.That(controller.customerAnimator, Is.Not.Null);
                Assert.That(controller.hudRoot, Is.Not.Null);
                Assert.That(controller.customerText, Is.Not.Null);
                Assert.That(controller.objectiveText, Is.Not.Null);
                Assert.That(controller.policyText, Is.Not.Null);
                Assert.That(controller.trustStateText, Is.Not.Null);
                Assert.That(controller.turnDurationSeconds, Is.EqualTo(45f));
                Assert.That(controller.activeTimeLimitSeconds, Is.EqualTo(300f));
                Assert.That(controller.maximumAcceptedTurns, Is.EqualTo(8));
            }
            finally
            {
                if (opened) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void AuthoredLanResourcesAndFontAreAvailable()
        {
            GameObject femalePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/ThirdParty/npc_casual_set_00/Prefabs/npc_csl_00_character_01f_03.prefab");
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/ThirdParty/Project Fonts/Sales Vietnamese.asset");
            AnimationClip walk = AssetDatabase.LoadAllAssetsAtPath(
                "Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Female/Movement/Walk/HumanF@Walk01_Forward.fbx")
                .OfType<AnimationClip>().FirstOrDefault();
            AnimationClip idle = AssetDatabase.LoadAllAssetsAtPath(
                "Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Female/Idles/HumanF@Idle01.fbx")
                .OfType<AnimationClip>().FirstOrDefault();
            Assert.That(femalePrefab, Is.Not.Null);
            Assert.That(femalePrefab.GetComponentInChildren<Animator>(true), Is.Not.Null);
            Assert.That(walk, Is.Not.Null);
            Assert.That(idle, Is.Not.Null);
            Assert.That(font, Is.Not.Null);
            Assert.That(font.HasCharacters("Lan • CHÍNH SÁCH ĐỔI HÀNG", out _, true, false), Is.True);
        }
    }
}
