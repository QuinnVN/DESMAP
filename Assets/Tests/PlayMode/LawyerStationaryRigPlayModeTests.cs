using System.Collections;
using System.Collections.Generic;
using NCKH.VR.Lawyer;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace NCKH.VR.Lawyer.Tests
{
    public sealed class LawyerStationaryRigPlayModeTests
    {
        [UnityTest]
        public IEnumerator EnforceStationaryStateNeverDisablesTurningOrReenablesTranslation()
        {
            GameObject player = new GameObject("Test Player");
            GameObject move = new GameObject("Move");
            GameObject gravity = new GameObject("Gravity");
            GameObject jump = new GameObject("Jump");
            GameObject turn = new GameObject("Turn");
            move.transform.SetParent(player.transform);
            gravity.transform.SetParent(player.transform);
            jump.transform.SetParent(player.transform);
            turn.transform.SetParent(player.transform);
            LawyerStationaryRig rig = player.AddComponent<LawyerStationaryRig>();

            rig.ConfigureInEditor(new[] { move, gravity, jump });
            yield return null;
            Assert.That(move.activeSelf, Is.False);
            Assert.That(gravity.activeSelf, Is.False);
            Assert.That(jump.activeSelf, Is.False);
            Assert.That(turn.activeSelf, Is.True);

            rig.enabled = false;
            rig.enabled = true;
            yield return null;
            Assert.That(move.activeSelf, Is.False);
            Assert.That(turn.activeSelf, Is.True);
            Object.Destroy(player);
        }

        [UnityTest]
        public IEnumerator OfficeBoardOpensFacingCameraAndClosesToNormalizedPose()
        {
            bool previousIgnore = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            try
            {
                yield return SceneManager.LoadSceneAsync(
                    "Assets/Scenes/Lawyer/Office.unity", LoadSceneMode.Single);
                yield return null;

                LawyerBoardController controller = Object.FindAnyObjectByType<LawyerBoardController>();
                Assert.That(controller, Is.Not.Null);
                controller.Session.ResetCase(controller.Session.CaseDefinition);
                Transform board = controller.transform;
                Vector3 originalPosition = board.position;
                Quaternion originalRotation = board.rotation;
                Vector3 originalScale = board.localScale;

                Transform template = board.Find(
                    "Lawyer Board Content/Board Surface/Clue Container/Clue Card Template");
                Assert.That(template, Is.Not.Null);
                Assert.That(template.gameObject.activeSelf, Is.False);
                LawyerBoardClue[] cluesBeforeFocus =
                    board.GetComponentsInChildren<LawyerBoardClue>(false);
                Assert.That(cluesBeforeFocus.Length, Is.GreaterThan(0));
                foreach (LawyerBoardClue clue in cluesBeforeFocus)
                {
                    Assert.That(clue.GetComponent<XRGrabInteractable>().enabled, Is.False);
                    Assert.That(clue.GetComponentInChildren<XRSimpleInteractable>().enabled, Is.False);
                }

                Renderer[] officeRenderers = Object.FindObjectsByType<Renderer>(
                    FindObjectsInactive.Include);
                Dictionary<Renderer, bool> rendererStates = new Dictionary<Renderer, bool>();
                foreach (Renderer renderer in officeRenderers)
                    if (renderer != null && !renderer.transform.IsChildOf(board))
                        rendererStates.Add(renderer, renderer.forceRenderingOff);

                LawyerBoardFocusLighting lighting = board.GetComponent<LawyerBoardFocusLighting>();
                Light[] sceneLights = Object.FindObjectsByType<Light>(
                    FindObjectsInactive.Include);
                Dictionary<Light, float> lightIntensities = new Dictionary<Light, float>();
                foreach (Light light in sceneLights)
                    if (light != null && light != lighting.BoardSpotlight && light.intensity > 0f)
                        lightIntensities.Add(light, light.intensity);

                controller.OpenBoard();
                for (int frame = 0; frame < 60 && !controller.IsOpen; frame++)
                    yield return null;
                for (int frame = 0; frame < 5; frame++)
                    yield return null;
                Assert.That(controller.IsOpen, Is.True);

                Camera camera = Camera.main;
                Assert.That(camera, Is.Not.Null);
                Vector3 toCamera = (camera.transform.position - board.position).normalized;
                Assert.That(Vector3.Dot(board.forward, toCamera), Is.GreaterThan(0.99f));
                Assert.That(Vector3.Distance(camera.transform.position, board.position), Is.InRange(0.82f, 0.90f));
                Assert.That(board.localScale, Is.EqualTo(originalScale));
                Assert.That(board.GetComponent<XRGrabInteractable>(), Is.Null);
                Assert.That(board.GetComponent<XRSimpleInteractable>().enabled, Is.False);

                LawyerClueLinkSystem linkSystem = board.GetComponentInChildren<LawyerClueLinkSystem>(true);
                Assert.That(board.GetComponentInChildren<Canvas>(true), Is.Null);
                Assert.That(linkSystem, Is.Not.Null);
                Assert.That(lighting, Is.Not.Null);
                Assert.That(lighting.IsFocused, Is.True);
                Assert.That(lighting.BoardSpotlight.enabled, Is.True);
                Assert.That(Vector3.Dot(linkSystem.transform.forward, board.forward), Is.GreaterThan(0.999f));
                Assert.That(board.GetComponentsInChildren<LawyerPhysicalButton>(true).Length, Is.EqualTo(2));

                LawyerBoardClue[] clues = board.GetComponentsInChildren<LawyerBoardClue>(false);
                Assert.That(clues.Length, Is.EqualTo(cluesBeforeFocus.Length));
                foreach (LawyerBoardClue clue in clues)
                {
                    Assert.That(clue.GetComponent<XRGrabInteractable>().enabled, Is.True);
                    Assert.That(clue.GetComponentInChildren<XRSimpleInteractable>().enabled, Is.True);
                    float boardDepth = board.InverseTransformPoint(clue.transform.position).z;
                    Assert.That(boardDepth, Is.InRange(0.065f, 0.09f));
                    foreach (TMP_Text label in clue.GetComponentsInChildren<TMP_Text>(true))
                        Assert.That(Vector3.Dot(label.transform.forward, board.forward), Is.LessThan(-0.999f));
                }

                foreach (KeyValuePair<Renderer, bool> state in rendererStates)
                    Assert.That(state.Key.forceRenderingOff, Is.EqualTo(state.Value), state.Key.name);

                int dimmedLightCount = 0;
                foreach (KeyValuePair<Light, float> state in lightIntensities)
                    if (Mathf.Abs(state.Key.intensity - state.Value * 0.2f) < 0.02f)
                        dimmedLightCount++;
                Assert.That(dimmedLightCount, Is.GreaterThan(0));

                Quaternion fixedRotation = board.rotation;
                Vector3 fixedPosition = board.position;
                for (int frame = 0; frame < 10; frame++)
                    yield return null;
                Assert.That(Vector3.Distance(board.position, fixedPosition), Is.LessThan(0.0001f));
                Assert.That(Quaternion.Angle(board.rotation, fixedRotation), Is.LessThan(0.001f));

                controller.CloseBoard();
                for (int frame = 0; frame < 60 && controller.IsOpen; frame++)
                    yield return null;
                Assert.That(controller.IsOpen, Is.False);
                Assert.That(Vector3.Distance(board.position, originalPosition), Is.LessThan(0.001f));
                Assert.That(Quaternion.Angle(board.rotation, originalRotation), Is.LessThan(0.01f));
                Assert.That(board.GetComponent<XRSimpleInteractable>().enabled, Is.True);
                LawyerBoardClue[] cluesAfterClose =
                    board.GetComponentsInChildren<LawyerBoardClue>(false);
                Assert.That(cluesAfterClose.Length, Is.EqualTo(cluesBeforeFocus.Length));
                foreach (LawyerBoardClue clue in cluesAfterClose)
                {
                    Assert.That(clue.GetComponent<XRGrabInteractable>().enabled, Is.False);
                    Assert.That(clue.GetComponentInChildren<XRSimpleInteractable>().enabled, Is.False);
                }
                for (int frame = 0; frame < 30 && lighting.BoardSpotlight.enabled; frame++)
                    yield return null;
                Assert.That(lighting.IsFocused, Is.False);
                Assert.That(lighting.BoardSpotlight.enabled, Is.False);
                foreach (KeyValuePair<Light, float> state in lightIntensities)
                    Assert.That(state.Key.intensity, Is.EqualTo(state.Value).Within(0.02f), state.Key.name);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousIgnore;
            }
        }

        [UnityTest]
        public IEnumerator ReleasedClueSnapsToBoardBoundsDepthAndRotationAndPersistsPosition()
        {
            bool previousIgnore = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            try
            {
                yield return SceneManager.LoadSceneAsync(
                    "Assets/Scenes/Lawyer/Office.unity", LoadSceneMode.Single);
                yield return null;

                LawyerBoardController controller = Object.FindAnyObjectByType<LawyerBoardController>();
                Assert.That(controller, Is.Not.Null);
                controller.Session.ResetCase(controller.Session.CaseDefinition);
                controller.OpenBoard();
                for (int frame = 0; frame < 60 && !controller.IsOpen; frame++)
                    yield return null;

                LawyerBoardClue[] clues =
                    controller.GetComponentsInChildren<LawyerBoardClue>(false);
                Assert.That(clues.Length, Is.GreaterThan(0));
                LawyerBoardClue clue = clues[0];
                Assert.That(clue.ClueId, Is.Not.Empty);
                Assert.That(controller.Session.UnlockedClueIds, Does.Contain(clue.ClueId));
                Transform boardSurface = clue.transform.parent.parent;
                float validDepth = boardSurface.InverseTransformPoint(clue.transform.position).z;
                clue.transform.position = boardSurface.TransformPoint(
                    new Vector3(2f, -2f, validDepth + 0.5f));
                clue.transform.rotation = boardSurface.rotation * Quaternion.Euler(30f, 60f, 15f);

                XRGrabInteractable grab = clue.GetComponent<XRGrabInteractable>();
                grab.selectExited.Invoke(null);

                float waitUntil = Time.realtimeSinceStartup + 0.2f;
                while (Time.realtimeSinceStartup < waitUntil)
                    yield return null;

                Vector3 snapped = boardSurface.InverseTransformPoint(clue.transform.position);
                Assert.That(snapped.x, Is.EqualTo(0.48f).Within(0.001f));
                Assert.That(snapped.y, Is.EqualTo(-0.28f).Within(0.001f));
                Assert.That(snapped.z, Is.EqualTo(validDepth).Within(0.001f));
                Assert.That(Quaternion.Angle(clue.transform.rotation, boardSurface.rotation),
                    Is.LessThan(0.01f));
                Assert.That(controller.Session.TryGetCluePosition(
                    clue.ClueId, out Vector2 savedPosition), Is.True);
                Assert.That(savedPosition.x, Is.EqualTo(0.48f).Within(0.001f));
                Assert.That(savedPosition.y, Is.EqualTo(-0.28f).Within(0.001f));
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousIgnore;
            }
        }

        [UnityTest]
        public IEnumerator PinchLinkCommitsOnlyNearAnotherPinAndUndoRemovesIt()
        {
            bool previousIgnore = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            GameObject testRayObject = null;
            try
            {
                yield return SceneManager.LoadSceneAsync(
                    "Assets/Scenes/Lawyer/Office.unity", LoadSceneMode.Single);
                yield return null;

                LawyerBoardController controller = Object.FindAnyObjectByType<LawyerBoardController>();
                Assert.That(controller, Is.Not.Null);
                controller.Session.ResetCase(controller.Session.CaseDefinition);
                controller.OpenBoard();
                for (int frame = 0; frame < 60 && !controller.IsOpen; frame++)
                    yield return null;

                LawyerClueLinkSystem linkSystem =
                    controller.GetComponentInChildren<LawyerClueLinkSystem>(true);
                LawyerCluePin[] pins = controller.GetComponentsInChildren<LawyerCluePin>(false);
                Assert.That(linkSystem, Is.Not.Null);
                Assert.That(pins.Length, Is.GreaterThanOrEqualTo(2));
                Assert.That(controller.Session.ClueLinks, Is.Empty);

                testRayObject = new GameObject("Test Pinch Ray");
                XRRayInteractor testRay = testRayObject.AddComponent<XRRayInteractor>();
                linkSystem.BeginLink(pins[0], testRay);
                linkSystem.CompleteLink(pins[0], pins[1].AnchorPosition);
                Assert.That(controller.Session.ClueLinks, Has.Count.EqualTo(1));

                linkSystem.BeginLink(pins[0], testRay);
                linkSystem.CompleteLink(pins[0], pins[1].AnchorPosition + Vector3.one * 10f);
                Assert.That(controller.Session.ClueLinks, Has.Count.EqualTo(1));

                linkSystem.UndoLastLink();
                Assert.That(controller.Session.ClueLinks, Is.Empty);
            }
            finally
            {
                if (testRayObject != null)
                    Object.Destroy(testRayObject);
                LogAssert.ignoreFailingMessages = previousIgnore;
            }
        }
    }
}
