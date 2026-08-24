using System.Collections.Generic;
using NCKH.VR.GameControl;
using NCKH.VR.Lawyer;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace NCKH.VR.Lawyer.Tests
{
    public sealed class LawyerSceneWiringTests
    {
        private const string OfficePath = "Assets/Scenes/Lawyer/Office.unity";
        private const string CourtroomPath = "Assets/Scenes/Lawyer/Courtroom_Interior URP.unity";

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
        public void OfficeBoardIsPhysicalAndRayWiringIsStable()
        {
            WithScene(OfficePath, scene =>
            {
                GameObject board = Find(scene, "Lawyer Case Board");
                GameObject player = Find(scene, "VR Player");
                Camera camera = player.GetComponentInChildren<Camera>(true);
                XRRayInteractor ray = player.GetComponentInChildren<XRRayInteractor>(true);
                Transform visual = board.transform.Find("Clue Board");
                BoxCollider collider = board.GetComponent<BoxCollider>();
                XRSimpleInteractable boardInteractable = board.GetComponent<XRSimpleInteractable>();

                Assert.That(camera, Is.Not.Null);
                Assert.That(ray, Is.Not.Null);
                Assert.That(visual, Is.Not.Null);
                Assert.That(collider, Is.Not.Null);
                Assert.That(board.GetComponentInChildren<Canvas>(true), Is.Null);
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

                LawyerBoardFocusLighting focusLighting = board.GetComponent<LawyerBoardFocusLighting>();
                Assert.That(focusLighting.BoardSpotlight, Is.Not.Null);
                Assert.That(focusLighting.BoardSpotlight.cullingMask, Is.EqualTo(1 << boardLayer));
                Assert.That(focusLighting.BoardSpotlight.enabled, Is.False);
                SerializedObject lightingObject = new SerializedObject(focusLighting);
                Assert.That(lightingObject.FindProperty("focusOccluders"), Is.Null);

                Transform clueTemplate = board.transform.Find(
                    "Lawyer Board Content/Board Surface/Clue Container/Clue Card Template");
                Assert.That(clueTemplate, Is.Not.Null);
                Assert.That(clueTemplate.gameObject.activeSelf, Is.False);
                LawyerBoardController controller = board.GetComponent<LawyerBoardController>();
                SerializedObject controllerObject = new SerializedObject(controller);
                Assert.That(controllerObject.FindProperty("clueTemplate").objectReferenceValue,
                    Is.SameAs(clueTemplate.GetComponent<LawyerBoardClue>()));

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
                    Assert.That(scale.y, Is.GreaterThan(0.001f), labels[index].name);
                    Assert.That(scale.z, Is.GreaterThan(0.001f), labels[index].name);
                }

                LawyerPhysicalButton[] physicalButtons = board.GetComponentsInChildren<LawyerPhysicalButton>(true);
                for (int index = 0; index < physicalButtons.Length; index++)
                {
                    Assert.That(physicalButtons[index].gameObject.layer, Is.EqualTo(boardLayer));
                    Assert.That(physicalButtons[index]
                        .GetComponent<LawyerActivationOnlySelectFilter>(), Is.Not.Null);
                    Assert.That((physicalButtons[index].GetComponent<XRSimpleInteractable>()
                        .interactionLayers.value & boardInteractable.interactionLayers.value), Is.Not.Zero);
                }
            });
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
