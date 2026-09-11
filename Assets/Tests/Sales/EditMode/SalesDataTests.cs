using System.Linq;
using NCKH.VR.Sales.Editor;
using NCKH.VR.Lawyer;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Jump;

namespace NCKH.VR.Sales.Tests
{
    public sealed class SalesDataTests
    {
        [Test]
        public void SavedSceneUsesAForButtonlessDialogueAndBothRaysForShoeSelection()
        {
            const string path = "Assets/Scenes/Sale/Shoes store - showroom with footwear.unity";
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var mission = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SalesMissionController>(true)).Single();
                Assert.That(mission.GetComponentsInChildren<LawyerActivationOnlySelectFilter>(true), Is.Empty,
                    "Sales buttons must accept the controller Select action.");
                Assert.That(mission.dialogueRoot.GetComponentsInChildren<SalesActionTarget>(true), Is.Empty,
                    "The conversation panel must not contain interactive buttons.");
                Assert.That(mission.nextText.text, Is.EqualTo("NHẤN A ĐỂ TIẾP TỤC"));
                JumpProvider jump = mission.playerCamera.transform.root.GetComponentInChildren<JumpProvider>(true);
                Assert.That(jump, Is.Not.Null);
                Assert.That(jump.gameObject.activeSelf, Is.False,
                    "A must not trigger jumping in the Sales scene.");
                Assert.That(mission.speakerText.rectTransform.anchoredPosition.y, Is.GreaterThan(.2f));
                Assert.That(mission.dialogueText.rectTransform.anchoredPosition.y, Is.LessThan(.1f));
                Assert.That(mission.choicesRoot.GetComponentsInChildren<SalesActionTarget>(true).Count(t => t.action >= 0), Is.EqualTo(3));
                var rays = mission.playerCamera.transform.root.GetComponentsInChildren<XRRayInteractor>(true);
                var targets = mission.GetComponentsInChildren<SalesActionTarget>(true);
                Assert.That(rays.Length, Is.GreaterThanOrEqualTo(2));
                foreach (var ray in rays)
                {
                    Assert.That(ray.allowHoveredActivate, Is.True, ray.name);
                    Assert.That(targets.All(target =>
                        (ray.interactionLayers.value & target.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>().interactionLayers.value) != 0),
                        Is.True, ray.name + " cannot interact with every sales button.");
                    Assert.That(targets.All(target =>
                        (ray.raycastMask.value & (1 << target.gameObject.layer)) != 0),
                        Is.True, ray.name + " physics mask cannot hit every sales button collider.");
                    XRInteractorLineVisual lineVisual = ray.GetComponent<XRInteractorLineVisual>();
                    Assert.That(lineVisual, Is.Not.Null, ray.name + " has no line visual.");
                    Assert.That(lineVisual.invalidColorGradient.Evaluate(0f).a, Is.GreaterThan(0f),
                        ray.name + " is invisible until it finds a valid target.");
                    var activate = ray.activateInput.inputActionReferencePerformed;
                    Assert.That(activate, Is.Not.Null, ray.name + " has no Activate action.");
                    string expectedMap = ray.handedness == InteractorHandedness.Left
                        ? "XRI Left Interaction" : "XRI Right Interaction";
                    Assert.That(activate.action.actionMap.name, Is.EqualTo(expectedMap), ray.name);
                    Assert.That(activate.action.name, Is.EqualTo("Activate"), ray.name);
                }
                var texts = mission.GetComponentsInChildren<TMPro.TMP_Text>(true);
                foreach (var text in texts)
                    Assert.That(text.font.HasCharacters(text.text, out uint[] missing, true, false), Is.True,
                        "Missing glyphs in " + text.text);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        [Test]
        public void PlaceholderHasThreeRequestedShoesAndOneBestFit()
        {
            var data = AssetDatabase.LoadAssetAtPath<SalesMissionData>(SalesProjectSetup.DataPath);
            Assert.That(data, Is.Not.Null);
            Assert.That(data.IsValid, Is.True);
            CollectionAssert.AreEqual(new[] {"1A", "3B", "8A"}, data.shoes.Select(s => s.label));
            Assert.That(data.shoes.Count(s => s.bestFit), Is.EqualTo(1));
            Assert.That(data.shoes.Select(s => s.rotationSpeed).Distinct().Count(), Is.EqualTo(3));
            foreach (var shoe in data.shoes) Assert.That(shoe.properties, Is.Not.Empty);
        }

        [Test]
        public void MissingDialogueOrPrefabIsRejected()
        {
            var data = ScriptableObject.CreateInstance<SalesMissionData>();
            Assert.That(data.IsValid, Is.False);
            data.dialogue = new[] {new SalesDialogueLine {text = "Xin chào"}};
            data.shoes = new[] {new SalesShoeData(), new SalesShoeData(), new SalesShoeData()};
            Assert.That(data.IsValid, Is.False);
            Object.DestroyImmediate(data);
        }

        [TestCase("Assets/Settings/Mobile_Renderer.asset")]
        public void TargetRenderersHaveDepthResetFocusFeature(string path)
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            var feature = renderer.rendererFeatures.OfType<LawyerFocusDimRendererFeature>().Single();
            Assert.That(feature.isActive, Is.True);
            Assert.That(feature.BoardLayerMask.value & (1 << 9), Is.Not.Zero);
            Assert.That(feature.DimMaterial.passCount, Is.GreaterThanOrEqualTo(2));
        }
    }
}
