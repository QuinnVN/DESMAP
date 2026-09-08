using System.Collections.Generic;
using System.IO;
using System.Linq;
using NCKH.VR.GameControl;
using NCKH.VR.Lawyer;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace NCKH.VR.Lawyer.Tests
{
    public sealed class LawyerSceneWiringTests
    {
        private const string OfficePath = "Assets/Scenes/Lawyer/Office.unity";
        private const string CourtroomPath = "Assets/Scenes/Lawyer/Courtroom_Interior URP.unity";
        private const string FocusDimShaderPath =
            "Assets/Shaders/Lawyer/LawyerFocusDim.shader";

        [Test]
        public void ResizedCardsKeepFixedBodyTextInsidePhysicalBounds()
        {
            var definition = AssetDatabase.LoadAssetAtPath<LawyerCaseDefinition>(
                "Assets/Data/Lawyer/Placeholder Lawyer Case.asset");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Lawyer/Lawyer Clue Card.prefab");
            var surface = new GameObject("Card layout verification");
            try
            {
                foreach (var clue in definition.Clues)
                {
                    var instance = Object.Instantiate(prefab, surface.transform);
                    try
                    {
                        var card = instance.GetComponent<LawyerBoardClue>();
                        card.Configure(clue, surface.transform, Vector2.zero,
                            Vector2.one * 10f, null, null);
                        instance.SetActive(true);
                        var body = instance.transform.Find("Body").GetComponent<TMP_Text>();
                        body.ForceMeshUpdate(true, true);
                        Assert.That(body.enableAutoSizing, Is.False, clue.Id);
                        Assert.That(body.fontSize, Is.EqualTo(14f), clue.Id);
                        Assert.That(body.isTextOverflowing, Is.False, clue.Id);
                        var collider = instance.GetComponent<BoxCollider>();
                        var corners = new Vector3[4];
                        foreach (var label in instance.GetComponentsInChildren<TMP_Text>())
                        {
                            label.rectTransform.GetWorldCorners(corners);
                            foreach (var corner in corners)
                            {
                                Vector3 local = instance.transform.InverseTransformPoint(corner);
                                Assert.That(Mathf.Abs(local.x - collider.center.x),
                                    Is.LessThanOrEqualTo(collider.size.x * 0.5f + 0.001f), clue.Id);
                                Assert.That(Mathf.Abs(local.y - collider.center.y),
                                    Is.LessThanOrEqualTo(collider.size.y * 0.5f + 0.001f), clue.Id);
                            }
                        }
                        var attach = instance.GetComponent<XRGrabInteractable>().attachTransform;
                        Assert.That(attach.localPosition.x, Is.EqualTo(0f).Within(0.001f));
                        Assert.That(attach.localPosition.y, Is.EqualTo(0f).Within(0.001f));
                    }
                    finally { Object.DestroyImmediate(instance); }
                }
            }
            finally { Object.DestroyImmediate(surface); }
        }

        [TestCase(OfficePath)]
        [TestCase(CourtroomPath)]
        public void PlayerIsStationaryButTurningAndInteractionRemainEnabled(string scenePath)
        {
            WithScene(scenePath, scene =>
            {
                GameObject player = Find(scene, "VR Player");
                Assert.That(player, Is.Not.Null);
                Assert.That(player.GetComponent<LawyerStationaryRig>(), Is.Not.Null);
                Transform locomotion = player.transform.Find("Locomotion");
                Assert.That(locomotion, Is.Not.Null);
                Assert.That(locomotion.Find("Move").gameObject.activeSelf, Is.False);
                Assert.That(locomotion.Find("Gravity").gameObject.activeSelf, Is.False);
                Assert.That(locomotion.Find("Jump").gameObject.activeSelf, Is.False);
                Assert.That(locomotion.Find("Turn").gameObject.activeSelf, Is.True);
                Assert.That(player.GetComponentInChildren<Camera>(true), Is.Not.Null);
                Assert.That(player.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>(true), Is.Not.Null);
            });
        }

        [Test]
        public void OfficeContainsInteractiveBoardAndRemoteEntryAdapter()
        {
            WithScene(OfficePath, scene =>
            {
                GameObject board = Find(scene, "Lawyer Case Board");
                Assert.That(board, Is.Not.Null);
                Assert.That(board.GetComponent<LawyerBoardController>(), Is.Not.Null);
                Assert.That(board.GetComponent<XRSimpleInteractable>(), Is.Not.Null);
                Assert.That(board.transform.Find("Clue Board"), Is.Not.Null);
                Assert.That(FindComponent<LawyerRemoteSceneAdapter>(scene), Is.Not.Null);
            });
        }

        [Test]
        public void OfficeBoardUsesPhysicalEvidenceAreaAndUnlitStrings()
        {
            WithScene(OfficePath, scene =>
            {
                GameObject board = Find(scene, "Lawyer Case Board");
                GameObject evidenceSurfaceObject = Find(scene, "Physical Evidence Surface");
                Assert.That(board, Is.Not.Null);
                Assert.That(evidenceSurfaceObject, Is.Not.Null);

                Renderer evidenceSurface = evidenceSurfaceObject.GetComponent<Renderer>();
                LawyerBoardController controller = board.GetComponent<LawyerBoardController>();
                Assert.That(evidenceSurface, Is.Not.Null);
                Assert.That(controller, Is.Not.Null);

                SerializedObject controllerObject = new SerializedObject(controller);
                Assert.That(controllerObject.FindProperty("evidenceSurfaceRenderer")
                    .objectReferenceValue, Is.SameAs(evidenceSurface));
                Assert.That(controllerObject.FindProperty("clueHalfBounds"), Is.Null);

                LawyerClueLinkSystem linkSystem =
                    board.GetComponentInChildren<LawyerClueLinkSystem>(true);
                Assert.That(linkSystem, Is.Not.Null);
                SerializedObject linkObject = new SerializedObject(linkSystem);
                Material stringMaterial = (Material)linkObject.FindProperty("stringMaterial")
                    .objectReferenceValue;
                Assert.That(stringMaterial, Is.Not.Null);
                Assert.That(stringMaterial.shader.name,
                    Is.EqualTo("Universal Render Pipeline/Unlit"));
            });
        }

        [Test]
        public void OfficeBoardIsPhysicalAndRayWiringIsStable()
        {
            WithScene(OfficePath, scene =>
            {
                GameObject board = Find(scene, "Lawyer Case Board");
                GameObject player = Find(scene, "VR Player");
                Camera camera = player.GetComponentInChildren<Camera>(true);
                XRRayInteractor ray = player.GetComponentInChildren<XRRayInteractor>(true);
                GameObject evidenceSurfaceObject = Find(scene, "Physical Evidence Surface");
                Transform visual = board.transform.Find("Clue Board");
                BoxCollider collider = board.GetComponent<BoxCollider>();
                XRSimpleInteractable boardInteractable = board.GetComponent<XRSimpleInteractable>();

                Assert.That(camera, Is.Not.Null);
                Assert.That(ray, Is.Not.Null);
                Assert.That(evidenceSurfaceObject, Is.Not.Null);
                Assert.That(visual, Is.Not.Null);
                Assert.That(collider, Is.Not.Null);
                Assert.That(board.GetComponentsInChildren<Canvas>(true), Is.Empty);
                Transform finaleHud = camera.transform.Find("Lawyer Finale Canvas");
                Assert.That(finaleHud, Is.Not.Null);
                Assert.That(finaleHud.GetComponent<Canvas>().renderMode,
                    Is.EqualTo(RenderMode.WorldSpace));
                Assert.That(finaleHud.localPosition,
                    Is.EqualTo(new Vector3(0f, 0f, 1.2f)));
                Assert.That(Quaternion.Angle(finaleHud.localRotation, Quaternion.identity),
                    Is.LessThan(0.01f));
                Assert.That(finaleHud.GetComponentInChildren<TrackedDeviceGraphicRaycaster>(true),
                    Is.Not.Null);
                Button startDefense = finaleHud.GetComponentInChildren<Button>(true);
                Assert.That(startDefense, Is.Not.Null);
                Assert.That(startDefense.name, Is.EqualTo("Start Defense Button"));
                Assert.That(startDefense.GetComponentInChildren<TMP_Text>(true).text,
                    Is.EqualTo("Bắt đầu bào chữa"));
                Assert.That(board.GetComponentInChildren<TrackedDeviceGraphicRaycaster>(true), Is.Null);
                Assert.That(board.GetComponentInChildren<UnityEngine.UI.Button>(true), Is.Null);
                Assert.That(board.GetComponentsInChildren<LawyerPhysicalButton>(true).Length, Is.EqualTo(2));
                Assert.That(board.GetComponent<LawyerBoardFocusLighting>(), Is.Not.Null);
                Assert.That(board.GetComponent<XRGrabInteractable>(), Is.Null);
                Assert.That(board.GetComponent<Rigidbody>(), Is.Null);
                Assert.That(board.GetComponent<LawyerActivationOnlySelectFilter>(), Is.Not.Null);

                int boardLayer = LayerMask.NameToLayer("LawyerBoard");
                Assert.That(boardLayer, Is.GreaterThanOrEqualTo(8));
                Assert.That(board.layer, Is.EqualTo(boardLayer));
                Assert.That(ray.raycastMask.value, Is.EqualTo(1 << boardLayer));
                Assert.That((ray.interactionLayers.value & boardInteractable.interactionLayers.value), Is.Not.Zero);
                SerializedObject rayObject = new SerializedObject(ray);
                Assert.That(rayObject.FindProperty("m_HitClosestOnly").boolValue, Is.True);
                Assert.That(rayObject.FindProperty("m_AllowHoveredActivate").boolValue, Is.True);
                Assert.That(rayObject.FindProperty("m_SelectInput")
                    .FindPropertyRelative("m_InputActionReferencePerformed").objectReferenceValue, Is.Not.Null);
                Assert.That(rayObject.FindProperty("m_SelectInput")
                    .FindPropertyRelative("m_InputActionReferenceValue").objectReferenceValue, Is.Not.Null);
                SerializedProperty activateInput = rayObject.FindProperty("m_ActivateInput");
                SerializedProperty uiPressInput = rayObject.FindProperty("m_UIPressInput");
                Assert.That(activateInput.FindPropertyRelative("m_InputActionReferencePerformed")
                    .objectReferenceValue, Is.SameAs(uiPressInput
                    .FindPropertyRelative("m_InputActionReferencePerformed").objectReferenceValue));
                Assert.That(activateInput.FindPropertyRelative("m_InputActionReferenceValue")
                    .objectReferenceValue, Is.SameAs(uiPressInput
                    .FindPropertyRelative("m_InputActionReferenceValue").objectReferenceValue));
                Assert.That(boardInteractable.colliders.Count, Is.EqualTo(1));
                Assert.That(boardInteractable.colliders[0], Is.SameAs(collider));

                XRDirectInteractor[] directInteractors = player.GetComponentsInChildren<XRDirectInteractor>(true);
                Assert.That(directInteractors.Length, Is.GreaterThanOrEqualTo(2));
                for (int index = 0; index < directInteractors.Length; index++)
                {
                    Assert.That((directInteractors[index].interactionLayers.value & 1), Is.Not.Zero);
                    Assert.That((directInteractors[index].interactionLayers.value
                        & boardInteractable.interactionLayers.value), Is.Not.Zero);
                }

                Vector3 horizontalToCamera = Vector3.ProjectOnPlane(
                    camera.transform.position - board.transform.position, Vector3.up).normalized;
                Assert.That(Vector3.Dot(board.transform.forward, horizontalToCamera), Is.GreaterThan(0.99f));

                MeshFilter meshFilter = visual.GetComponent<MeshFilter>();
                Assert.That(meshFilter, Is.Not.Null);
                Assert.That(meshFilter.sharedMesh, Is.Not.Null);
                Bounds visualBounds = BoundsInLocalSpace(
                    meshFilter.sharedMesh.bounds, visual, board.transform);
                Assert.That(visualBounds.size.x, Is.GreaterThan(visualBounds.size.y));
                Assert.That(visualBounds.size.y, Is.GreaterThan(visualBounds.size.z * 10f));
                Assert.That(Mathf.Abs(visualBounds.center.z), Is.LessThan(0.05f));
                Assert.That(collider.size.x, Is.GreaterThan(collider.size.z * 10f));
                Assert.That(collider.size.y, Is.GreaterThan(collider.size.z * 5f));
                Assert.That(collider.center, Is.EqualTo(new Vector3(0f, 0.025f, 0f)));
                Assert.That(collider.size, Is.EqualTo(new Vector3(1.2f, 0.67f, 0.08f)));

                Transform evidenceSurfaceTransform = evidenceSurfaceObject.transform;
                Assert.That(evidenceSurfaceTransform.localPosition,
                    Is.EqualTo(new Vector3(0f, 0.025f, 0.003f)));
                Assert.That(evidenceSurfaceTransform.localScale,
                    Is.EqualTo(new Vector3(1.14f, 0.62f, 0.018f)));
                AssertPhysicalText(scene, "Physical Title Plate Text",
                    new Vector2(0f, 0.34f), new Vector2(98f, 5.5f), 32f, false);
                Assert.That(Find(scene, "Physical Objective Sheet"), Is.Null);
                Assert.That(Find(scene, "Physical Timeline Sheet"), Is.Null);
                Assert.That(Find(scene, "Physical Witness Sheet"), Is.Null);
                Assert.That(Find(scene, "Physical Details Sheet"), Is.Null);
                AssertPhysicalText(scene, "Evidence Heading",
                    new Vector2(0.127f, 0.26f), new Vector2(48f, 4f), 28f, false);
                Assert.That(Find(scene, "Physical Status Plate"), Is.Null);
                Assert.That(Find(scene, "Board Toast Canvas"), Is.Not.Null);

                LawyerBoardFocusLighting focusLighting = board.GetComponent<LawyerBoardFocusLighting>();
                Assert.That(focusLighting.BoardSpotlight, Is.Not.Null);
                Assert.That(focusLighting.BoardSpotlight.cullingMask, Is.EqualTo(1 << boardLayer));
                Assert.That(focusLighting.BoardSpotlight.enabled, Is.False);
                SerializedObject lightingObject = new SerializedObject(focusLighting);
                Assert.That(lightingObject.FindProperty("focusOccluders"), Is.Null);
                Assert.That(focusLighting.DimMultiplier, Is.EqualTo(0.45f).Within(0.001f));
                Assert.That(focusLighting.ScreenDimAmount, Is.EqualTo(0.55f).Within(0.001f));

                Assert.That(Find(scene, "Physical Focus Backdrop"), Is.Null);
                GameObject focusMask = Find(scene, "Board Focus Stencil Mask");
                Assert.That(focusMask, Is.Not.Null);
                Assert.That(focusMask.layer, Is.EqualTo(LayerMask.NameToLayer("Ignore Raycast")));
                Assert.That(focusMask.layer, Is.Not.EqualTo(boardLayer));
                Renderer focusMaskRenderer = focusMask.GetComponent<Renderer>();
                Assert.That(focusMaskRenderer, Is.Not.Null);
                Assert.That(focusMaskRenderer.enabled, Is.False);
                Assert.That(focusMask.GetComponent<Collider>(), Is.Null);
                Assert.That(focusMaskRenderer.sharedMaterial.shader.name,
                    Is.EqualTo("NCKH VR/Lawyer/Focus Board Mask"));
                Assert.That(focusMask.transform.localScale.x, Is.EqualTo(1.28f).Within(0.001f));
                Assert.That(focusMask.transform.localScale.y, Is.EqualTo(0.80f).Within(0.001f));
                Assert.That(focusLighting.FocusMaskRenderer, Is.SameAs(focusMaskRenderer));

                const uint foregroundMask = 1u << 31;
                Renderer[] playerRenderers = player.GetComponentsInChildren<Renderer>(true);
                Assert.That(playerRenderers.Length, Is.GreaterThan(0));
                for (int index = 0; index < playerRenderers.Length; index++)
                {
                    Assert.That((playerRenderers[index].renderingLayerMask & foregroundMask),
                        Is.Not.Zero, playerRenderers[index].name);
                    Assert.That((playerRenderers[index].renderingLayerMask & 1u),
                        Is.Not.Zero, playerRenderers[index].name);
                }

                Transform clueTemplate = board.transform.Find(
                    "Lawyer Board Content/Board Surface/Clue Container/Clue Card Template");
                Assert.That(clueTemplate, Is.Not.Null);
                Assert.That(clueTemplate.gameObject.activeSelf, Is.False);
                Assert.That(clueTemplate.localScale, Is.EqualTo(Vector3.one * 0.82f));
                LawyerBoardController controller = board.GetComponent<LawyerBoardController>();
                SerializedObject controllerObject = new SerializedObject(controller);
                Assert.That(controllerObject.FindProperty("clueTemplate").objectReferenceValue,
                    Is.SameAs(clueTemplate.GetComponent<LawyerBoardClue>()));
                SerializedProperty obstacleRoots = controllerObject.FindProperty("focusObstacleRoots");
                Assert.That(obstacleRoots.arraySize, Is.EqualTo(2));
                HashSet<string> obstacleNames = new HashSet<string>();
                for (int index = 0; index < obstacleRoots.arraySize; index++)
                    obstacleNames.Add(((Transform)obstacleRoots.GetArrayElementAtIndex(index)
                        .objectReferenceValue).name);
                Assert.That(obstacleNames, Is.EquivalentTo(new[] { "Props", "NPCs" }));
                Assert.That(controllerObject.FindProperty("expandedDistance").floatValue,
                    Is.EqualTo(0.70f).Within(0.001f));
                Assert.That(controllerObject.FindProperty("expandedVerticalOffset").floatValue,
                    Is.EqualTo(0.04f).Within(0.001f));
                Assert.That(controllerObject.FindProperty("focusClearance").floatValue,
                    Is.EqualTo(0.03f).Within(0.001f));
                Assert.That(controllerObject.FindProperty("maximumFocusLift").floatValue,
                    Is.EqualTo(0.30f).Within(0.001f));
                Assert.That(controllerObject.FindProperty("maximumFocusPull").floatValue,
                    Is.EqualTo(0.15f).Within(0.001f));
                Assert.That(controllerObject.FindProperty("focusSearchStep").floatValue,
                    Is.EqualTo(0.05f).Within(0.001f));
                Assert.That(controllerObject.FindProperty("raisedArcLift").floatValue,
                    Is.EqualTo(0.25f).Within(0.001f));
                Assert.That(controllerObject.FindProperty("pathSamples").intValue, Is.EqualTo(12));
                GameObject resultPanel = (GameObject)controllerObject.FindProperty("resultPanel")
                    .objectReferenceValue;
                Assert.That(resultPanel, Is.Not.Null);
                Assert.That(resultPanel.name, Is.EqualTo("Evidence Result Overlay"));
                Assert.That(resultPanel.activeSelf, Is.False);
                Assert.That(controllerObject.FindProperty("resultTitleLabel").objectReferenceValue,
                    Is.Not.Null);
                Assert.That(controllerObject.FindProperty("resultBodyLabel").objectReferenceValue,
                    Is.Not.Null);
                Assert.That(controllerObject.FindProperty("introTitleGroup").objectReferenceValue,
                    Is.Not.Null);
                Assert.That(controllerObject.FindProperty("introTitleLabel").objectReferenceValue,
                    Is.Not.Null);
                Assert.That(controllerObject.FindProperty("introDisplayDuration").floatValue,
                    Is.EqualTo(2f).Within(0.001f));
                Assert.That(controllerObject.FindProperty("introFadeDuration").floatValue,
                    Is.EqualTo(0.75f).Within(0.001f));

                XRGrabInteractable clueGrab = clueTemplate.GetComponent<XRGrabInteractable>();
                Assert.That(clueGrab, Is.Not.Null);
                Assert.That(clueGrab.throwOnDetach, Is.False);
                Assert.That(clueGrab.attachTransform, Is.Not.Null);
                Assert.That(clueGrab.attachTransform.name, Is.EqualTo("Front Attach"));
                Assert.That(clueGrab.attachTransform.localPosition.z, Is.GreaterThan(0f));
                Assert.That(clueTemplate.GetComponent<Rigidbody>().isKinematic, Is.True);
                Assert.That(clueTemplate.GetComponent<Rigidbody>().useGravity, Is.False);
                SerializedObject clueObject = new SerializedObject(
                    clueTemplate.GetComponent<LawyerBoardClue>());
                Assert.That(clueObject.FindProperty("snapDuration").floatValue,
                    Is.EqualTo(0.1f).Within(0.001f));
                TMP_Text cardTitle = clueTemplate.transform.Find("Title").GetComponent<TMP_Text>();
                TMP_Text cardBody = clueTemplate.transform.Find("Body").GetComponent<TMP_Text>();
                Assert.That(cardTitle.enableAutoSizing, Is.True);
                Assert.That(cardTitle.fontSizeMin, Is.EqualTo(18f).Within(0.001f));
                Assert.That(cardBody.enableAutoSizing, Is.True);
                Assert.That(cardBody.fontSizeMin, Is.EqualTo(12f).Within(0.001f));

                LawyerClueLinkSystem linkSystem = board.GetComponentInChildren<LawyerClueLinkSystem>(true);

                LawyerCluePin cluePin = clueTemplate.GetComponentInChildren<LawyerCluePin>(true);
                Assert.That(cluePin, Is.Not.Null);
                Assert.That(cluePin.GetComponent<XRSimpleInteractable>(), Is.Not.Null);
                Assert.That(cluePin.GetComponent<LawyerActivationOnlySelectFilter>(), Is.Not.Null);
                Assert.That(cluePin.GetComponent<XRGrabInteractable>(), Is.Null);
                Assert.That(cluePin.GetComponent<Rigidbody>(), Is.Null);

                TMP_Text[] labels = board.GetComponentsInChildren<TMP_Text>(true);
                Assert.That(labels.Length, Is.GreaterThanOrEqualTo(9));
                for (int index = 0; index < labels.Length; index++)
                {
                    Assert.That(labels[index].gameObject.layer, Is.EqualTo(boardLayer));
                    Vector3 scale = labels[index].transform.lossyScale;
                    Assert.That(scale.y, Is.GreaterThan(0.0001f), labels[index].name);
                    Assert.That(scale.z, Is.GreaterThan(0.0001f), labels[index].name);
                }

                LawyerPhysicalButton[] physicalButtons = board.GetComponentsInChildren<LawyerPhysicalButton>(true);
                for (int index = 0; index < physicalButtons.Length; index++)
                {
                    Assert.That(physicalButtons[index].gameObject.layer, Is.EqualTo(boardLayer));
                    Assert.That(physicalButtons[index]
                        .GetComponent<LawyerActivationOnlySelectFilter>(), Is.Not.Null);
                    Assert.That((physicalButtons[index].GetComponent<XRSimpleInteractable>()
                        .interactionLayers.value & boardInteractable.interactionLayers.value), Is.Not.Zero);
                    SerializedObject buttonObject = new SerializedObject(physicalButtons[index]);
                    Transform pressVisual = buttonObject.FindProperty("pressVisual").objectReferenceValue as Transform;
                    TMP_Text buttonLabel = buttonObject.FindProperty("label").objectReferenceValue as TMP_Text;
                    Assert.That(pressVisual, Is.Not.Null, physicalButtons[index].name);
                    Assert.That(buttonLabel, Is.Not.Null, physicalButtons[index].name);
                    Assert.That(buttonLabel.transform.parent, Is.SameAs(pressVisual),
                        physicalButtons[index].name);
                    BoxCollider buttonCollider = physicalButtons[index].GetComponent<BoxCollider>();
                    Bounds buttonBounds = new Bounds(
                        physicalButtons[index].transform.localPosition + buttonCollider.center,
                        buttonCollider.size);
                    Assert.That(new Bounds(collider.center, collider.size).Intersects(buttonBounds),
                        Is.False, physicalButtons[index].name);
                }
            });
        }

        [Test]
        public void FocusDimFeatureIsInstalledInBothRenderers()
        {
            AssertFocusDimFeature("Assets/Settings/Mobile_Renderer.asset");
            AssertFocusDimFeature("Assets/Settings/PC_Renderer.asset");
        }

        [Test]
        public void FocusDimShaderUsesStencilOnlyForFallbackDepthReset()
        {
            string source = File.ReadAllText(FocusDimShaderPath);
            int dimPassStart = source.IndexOf("Name \"Focus Dim\"",
                System.StringComparison.Ordinal);
            int resetPassStart = source.IndexOf("Name \"Reset Board Depth\"",
                System.StringComparison.Ordinal);

            Assert.That(dimPassStart, Is.GreaterThanOrEqualTo(0));
            Assert.That(resetPassStart, Is.GreaterThan(dimPassStart));
            string dimPass = source.Substring(dimPassStart, resetPassStart - dimPassStart);
            string resetPass = source.Substring(resetPassStart);
            Assert.That(dimPass, Does.Not.Contain("Stencil"));
            Assert.That(resetPass, Does.Contain("Stencil"));
            Assert.That(resetPass, Does.Contain("Ref 1"));
            Assert.That(resetPass, Does.Contain("Comp Equal"));
        }

        [Test]
        public void CourtroomContainsDefendantAndCompleteInterviewUi()
        {
            WithScene(CourtroomPath, scene =>
            {
                GameObject defendant = Find(scene, "Lawyer Defendant");
                Assert.That(defendant, Is.Not.Null);
                Assert.That(defendant.GetComponent<AudioSource>(), Is.Not.Null);
                LawyerInterviewController controller = FindComponent<LawyerInterviewController>(scene);
                Assert.That(controller, Is.Not.Null);
                SerializedObject serialized = new SerializedObject(controller);
                Assert.That(serialized.FindProperty("mainQuestionButtons").arraySize, Is.EqualTo(5));
                Assert.That(serialized.FindProperty("followUpButtons").arraySize, Is.EqualTo(3));
                Assert.That(serialized.FindProperty("questionCounterLabel").objectReferenceValue, Is.Not.Null);
                Assert.That(serialized.FindProperty("responsePanel").objectReferenceValue, Is.Not.Null);
                Assert.That(serialized.FindProperty("responseLabel").objectReferenceValue, Is.Not.Null);
                Assert.That(serialized.FindProperty("responseHintLabel").objectReferenceValue, Is.Not.Null);
                Assert.That(serialized.FindProperty("responseContinueButton").objectReferenceValue, Is.Not.Null);
                Assert.That(serialized.FindProperty("navigationDeadzone").floatValue,
                    Is.EqualTo(0.65f).Within(0.001f));
                Assert.That(serialized.FindProperty("navigationRepeatDelay").floatValue,
                    Is.EqualTo(0.6f).Within(0.001f));
                Assert.That(serialized.FindProperty("navigationRepeatRate").floatValue,
                    Is.EqualTo(0.35f).Within(0.001f));

                LawyerDefenseController defense = FindComponent<LawyerDefenseController>(scene);
                Assert.That(defense, Is.Not.Null);
                SerializedObject defenseSerialized = new SerializedObject(defense);
                Assert.That(defenseSerialized.FindProperty("interviewHud").objectReferenceValue,
                    Is.Not.Null);
                Assert.That(defenseSerialized.FindProperty("defenseHud").objectReferenceValue,
                    Is.Not.Null);
                Assert.That(defenseSerialized.FindProperty("preparationDuration").floatValue,
                    Is.EqualTo(5f).Within(0.001f));
                Assert.That(defenseSerialized.FindProperty("recordingDuration").floatValue,
                    Is.EqualTo(30f).Within(0.001f));
                Assert.That(defenseSerialized.FindProperty("uploadAttempts").intValue,
                    Is.EqualTo(5));
                Assert.That(Find(scene, "Lawyer Defense Canvas").activeSelf, Is.False);

                InputActionAsset controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                    "Assets/Input/Lawyer/Lawyer Controls.inputactions");
                InputAction stop = controls.FindAction("Lawyer/StopDefense", true);
                Assert.That(stop.bindings.Any(binding =>
                    binding.path == "<XRController>{RightHand}/primaryButton"), Is.True);
            });
        }

        [Test]
        public void BothScenesAreBuiltButOnlyOfficeIsCatalogued()
        {
            HashSet<string> buildScenes = new HashSet<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                if (scene.enabled)
                    buildScenes.Add(scene.path);
            Assert.That(buildScenes, Does.Contain(OfficePath));
            Assert.That(buildScenes, Does.Contain(CourtroomPath));

            WithScene("Assets/Scenes/Standby.unity", scene =>
            {
                GameController controller = FindComponent<GameController>(scene);
                SerializedObject serialized = new SerializedObject(controller);
                SerializedProperty catalog = serialized.FindProperty("scenes");
                int officeCount = 0;
                int courtroomCount = 0;
                for (int index = 0; index < catalog.arraySize; index++)
                {
                    string path = catalog.GetArrayElementAtIndex(index)
                        .FindPropertyRelative("scenePath").stringValue;
                    if (path == OfficePath)
                        officeCount++;
                    if (path == CourtroomPath)
                        courtroomCount++;
                }
                Assert.That(officeCount, Is.EqualTo(1));
                Assert.That(courtroomCount, Is.Zero);
            });
        }

        private static void WithScene(string path, System.Action<Scene> assertion)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened)
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                assertion(scene);
            }
            finally
            {
                if (opened)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static GameObject Find(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                for (int index = 0; index < transforms.Length; index++)
                    if (transforms[index].name == name)
                        return transforms[index].gameObject;
            }
            return null;
        }

        private static T FindComponent<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T result = root.GetComponentInChildren<T>(true);
                if (result != null)
                    return result;
            }
            return null;
        }

        private static void AssertPhysicalText(
            Scene scene,
            string name,
            Vector2 anchoredPosition,
            Vector2 size,
            float baseFontSize,
            bool autoSize)
        {
            TMP_Text label = Find(scene, name).GetComponent<TMP_Text>();
            Assert.That(label, Is.Not.Null, name);
            Assert.That(label.rectTransform.anchoredPosition, Is.EqualTo(anchoredPosition), name);
            Assert.That(label.rectTransform.sizeDelta, Is.EqualTo(size), name);
            Assert.That(label.enableAutoSizing, Is.EqualTo(autoSize), name);
            SerializedObject serialized = new SerializedObject(label);
            Assert.That(serialized.FindProperty("m_fontSizeBase").floatValue,
                Is.EqualTo(baseFontSize).Within(0.001f), name);
            if (!autoSize)
                return;
            Assert.That(label.fontSizeMin, Is.EqualTo(18f).Within(0.001f), name);
            Assert.That(label.fontSizeMax, Is.EqualTo(72f).Within(0.001f), name);
        }

        private static void AssertFocusDimFeature(string rendererPath)
        {
            UniversalRendererData rendererData =
                AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            Assert.That(rendererData, Is.Not.Null, rendererPath);
            LawyerFocusDimRendererFeature[] features = System.Array.FindAll(
                rendererData.rendererFeatures.ToArray(),
                feature => feature is LawyerFocusDimRendererFeature)
                .Cast<LawyerFocusDimRendererFeature>().ToArray();
            Assert.That(features.Length, Is.EqualTo(1), rendererPath);
            Assert.That(features[0].isActive, Is.True, rendererPath);
            Assert.That(features[0].DimMaterial, Is.Not.Null, rendererPath);
            Assert.That(features[0].DimMaterial.shader.name,
                Is.EqualTo("Hidden/NCKH VR/Lawyer/Focus Dim"), rendererPath);
            Assert.That(features[0].DimMaterial.passCount, Is.EqualTo(2), rendererPath);
            Assert.That(features[0].DimMaterial.GetPassName(1),
                Is.EqualTo("Reset Board Depth"), rendererPath);
            Assert.That(features[0].BoardLayerMask.value,
                Is.EqualTo(1 << LayerMask.NameToLayer("LawyerBoard")), rendererPath);
            Assert.That(features[0].ForegroundRenderingLayerMask,
                Is.EqualTo(1u << 31), rendererPath);
        }

        private static Bounds BoundsInLocalSpace(
            Bounds sourceBounds, Transform sourceSpace, Transform targetSpace)
        {
            Vector3 center = sourceBounds.center;
            Vector3 extents = sourceBounds.extents;
            Bounds result = new Bounds(
                targetSpace.InverseTransformPoint(sourceSpace.TransformPoint(
                    center + new Vector3(-extents.x, -extents.y, -extents.z))),
                Vector3.zero);
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
                result.Encapsulate(targetSpace.InverseTransformPoint(sourceSpace.TransformPoint(
                    center + Vector3.Scale(extents, new Vector3(x, y, z)))));
            return result;
        }
    }
}
