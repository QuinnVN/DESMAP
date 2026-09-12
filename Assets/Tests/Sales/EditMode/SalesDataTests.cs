using System;
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
                Transform objectivePanel = mission.objectiveText.transform.parent.Find("Objective Panel");
                Assert.That(objectivePanel, Is.Not.Null);

                Assert.That(mission.recordingIndicator, Is.Not.Null);
                Assert.That(mission.recordingIndicator.transform.parent, Is.SameAs(mission.playerCamera.transform));
                Assert.That(mission.recordingIndicator.transform.localPosition.x, Is.GreaterThan(0f));
                Assert.That(mission.recordingIndicator.transform.localPosition.y, Is.GreaterThan(0f));
                Assert.That(mission.recordingIndicator.activeSelf, Is.False);
Assert.That(objectivePanel.localScale.y, Is.LessThan(.3f));
                Assert.That(mission.choicesRoot.GetComponentsInChildren<SalesActionTarget>(true).Count(t => t.action >= 0), Is.EqualTo(3));
                Assert.That(mission.persuasionRoot, Is.SameAs(mission.resultRoot));
                Assert.That(mission.persuasionTitleText, Is.Not.Null);
                Assert.That(mission.persuasionCountdownText, Is.Not.Null);
                Assert.That(mission.persuasionInstructionText, Is.Not.Null);
                Assert.That(mission.persuasionStatusText, Is.Not.Null);
                Assert.That(mission.retryUploadTarget, Is.Not.Null);
                Assert.That(mission.retryUploadTarget.action, Is.EqualTo(-4));
                Transform retryPanel = mission.retryUploadTarget.transform.Find("Panel");
                Assert.That(retryPanel, Is.Not.Null);
                Renderer retryPanelRenderer = retryPanel.GetComponent<Renderer>();
                Assert.That(retryPanelRenderer, Is.Not.Null);
                Assert.That(retryPanelRenderer.sharedMaterial, Is.Not.Null,
                    "The retry-upload button must have a material or Unity renders it magenta.");
                Assert.That(retryPanelRenderer.sharedMaterial.shader, Is.Not.Null);
                Assert.That(retryPanelRenderer.sharedMaterial.shader.isSupported, Is.True);
                Assert.That(mission.preparationDuration, Is.EqualTo(10f));
                Assert.That(mission.recordingDuration, Is.EqualTo(60f));
                Assert.That(mission.choicesRoot.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>(true)
                    .Where(interactable => interactable.GetComponent<SalesActionTarget>()?.action >= 0)
                    .All(interactable => interactable.selectMode == UnityEngine.XR.Interaction.Toolkit.Interactables.InteractableSelectMode.Single),
                    Is.True, "Each shoe must accept only one controller selection at a time.");
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
        public void SavedRetryButtonDoesNotCoverFailureText()
        {
            const string path = "Assets/Scenes/Sale/Shoes store - showroom with footwear.unity";
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var mission = scene.GetRootGameObjects()
                    .SelectMany(g => g.GetComponentsInChildren<SalesMissionController>(true)).Single();
                Transform buttonPanel = mission.retryUploadTarget.transform.Find("Panel");
                float buttonTop = mission.retryUploadTarget.transform.localPosition.y
                    + buttonPanel.localPosition.y + buttonPanel.localScale.y * .5f;
                float statusBottom = mission.persuasionStatusText.rectTransform.anchoredPosition3D.y
                    - mission.persuasionStatusText.rectTransform.sizeDelta.y * .5f;
                Assert.That(buttonTop, Is.LessThanOrEqualTo(statusBottom - .01f),
                    "The retry-upload button must sit below the upload-failure status text.");
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
            Assert.That(data.closingReply, Is.Not.Empty);
            for (int index = 0; index < data.shoes.Length; index++)
            {
                var shoe = data.shoes[index];
                Assert.That(shoe.properties, Is.Not.Empty);
                Assert.That(shoe.price, Is.GreaterThan(0));
                Assert.That(shoe.objection, Is.Not.Empty);
                Assert.That(shoe.comparisonAlternativeIndex, Is.InRange(0, data.shoes.Length - 1));
                Assert.That(shoe.comparisonAlternativeIndex, Is.Not.EqualTo(index));
                Assert.That(shoe.objection, Does.Contain(data.shoes[shoe.comparisonAlternativeIndex].label));
            }
        }

        [Test]
        public void MissingDialogueOrPrefabIsRejected()
        {
            var data = ScriptableObject.CreateInstance<SalesMissionData>();
            Assert.That(data.IsValid, Is.False);
            data.dialogue = new[] {new SalesDialogueLine {text = "Xin chào"}};
            data.shoes = new[] {new SalesShoeData(), new SalesShoeData(), new SalesShoeData()};
            Assert.That(data.IsValid, Is.False);
            UnityEngine.Object.DestroyImmediate(data);
        }

        [Test]
        public void RecordingIsResampledToSixteenKilohertzBeforeWavEncoding()
        {
            float[] source = { -1f, -.5f, .5f, 1f };
            float[] normalized = SalesAudioResampler.ResampleMono(source, 8000, 16000);
            byte[] wav = NCKH.VR.Lawyer.Pcm16WavEncoder.Encode(normalized, 1, 16000);
            Assert.That(normalized.Length, Is.EqualTo(8));
            Assert.That(BitConverter.ToInt32(wav, 24), Is.EqualTo(16000));
            Assert.That(BitConverter.ToInt16(wav, 22), Is.EqualTo(1));
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
