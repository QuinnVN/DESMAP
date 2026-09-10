using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NCKH.VR.Lawyer;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace NCKH.VR.Lawyer.Tests
{
    public sealed class LawyerStationaryRigPlayModeTests
    {
        [UnityTearDown]
        public IEnumerator TearDownLawyerSession()
        {
            if (LawyerGameSession.Instance != null)
                Object.Destroy(LawyerGameSession.Instance.gameObject);
            yield return null;
        }

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
        public IEnumerator FocusMaskFollowsDepthResetInsteadOfGeneralFocusState()
        {
            GameObject root = new GameObject("Focus Mask Lifecycle Test");
            GameObject mask = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mask.transform.SetParent(root.transform, false);
            Renderer maskRenderer = mask.GetComponent<Renderer>();
            maskRenderer.enabled = false;
            LawyerBoardFocusLighting lighting =
                root.AddComponent<LawyerBoardFocusLighting>();
            lighting.ConfigureInEditor(
                System.Array.Empty<Light>(), null, maskRenderer);

            lighting.SetFocused(true);
            yield return null;
            Assert.That(lighting.IsFocused, Is.True);
            Assert.That(LawyerBoardFocusLighting.CurrentScreenDimAmount,
                Is.GreaterThan(0f));
            Assert.That(LawyerBoardFocusLighting.CurrentOverlayActive, Is.False);
            Assert.That(maskRenderer.enabled, Is.False);

            lighting.SetOverlayActive(true);
            Assert.That(LawyerBoardFocusLighting.CurrentOverlayActive, Is.True);
            Assert.That(maskRenderer.enabled, Is.True);

            lighting.SetOverlayActive(false);
            Assert.That(LawyerBoardFocusLighting.CurrentOverlayActive, Is.False);
            Assert.That(maskRenderer.enabled, Is.False);

            lighting.enabled = false;
            Assert.That(LawyerBoardFocusLighting.CurrentScreenDimAmount,
                Is.Zero.Within(0.000001f));
            Assert.That(maskRenderer.enabled, Is.False);
            Object.Destroy(root);
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
                for (int frame = 0; frame < 30; frame++)
                    yield return null;
                if (controller.IsOpen)
                {
                    controller.CloseBoard();
                    for (int frame = 0; frame < 60 && controller.IsOpen; frame++)
                        yield return null;
                }
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
                Assert.That(LawyerBoardFocusLighting.CurrentOverlayActive, Is.True);
                for (int frame = 0; frame < 60 && !controller.IsOpen; frame++)
                {
                    Assert.That(board.localScale, Is.EqualTo(originalScale));
                    yield return null;
                }
                for (int frame = 0; frame < 5; frame++)
                    yield return null;
                Assert.That(controller.IsOpen, Is.True);

                Camera camera = Camera.main;
                Assert.That(camera, Is.Not.Null);
                Vector3 toCamera = Vector3.ProjectOnPlane(
                    camera.transform.position - board.position, Vector3.up).normalized;
                Assert.That(Vector3.Dot(board.forward, toCamera), Is.GreaterThan(0.99f));
                Assert.That(Vector3.Distance(camera.transform.position, board.position), Is.InRange(0.60f, 0.80f));
                Assert.That(board.localScale, Is.EqualTo(originalScale));
                Assert.That(board.GetComponent<XRGrabInteractable>(), Is.Null);
                Assert.That(board.GetComponent<XRSimpleInteractable>().enabled, Is.False);

                LawyerClueLinkSystem linkSystem = board.GetComponentInChildren<LawyerClueLinkSystem>(true);
                Canvas finaleCanvas = camera.transform.Find("Lawyer Finale Canvas")
                    .GetComponent<Canvas>();
                Assert.That(finaleCanvas, Is.Not.Null);
                Assert.That(finaleCanvas.name, Is.EqualTo("Lawyer Finale Canvas"));
                Assert.That(finaleCanvas.transform.localPosition,
                    Is.EqualTo(new Vector3(0f, 0f, 1.2f)));
                Assert.That(Quaternion.Angle(
                    finaleCanvas.transform.localRotation, Quaternion.identity),
                    Is.LessThan(0.01f));
                Vector3 hudLocalPosition = finaleCanvas.transform.localPosition;
                Quaternion hudLocalRotation = finaleCanvas.transform.localRotation;
                camera.transform.SetPositionAndRotation(
                    camera.transform.position + new Vector3(0.1f, 0.05f, -0.1f),
                    camera.transform.rotation * Quaternion.Euler(5f, 12f, 0f));
                yield return null;
                Assert.That(finaleCanvas.transform.localPosition, Is.EqualTo(hudLocalPosition));
                Assert.That(Quaternion.Angle(finaleCanvas.transform.localRotation, hudLocalRotation),
                    Is.LessThan(0.01f));
                Assert.That(linkSystem, Is.Not.Null);
                Assert.That(lighting, Is.Not.Null);
                Assert.That(lighting.IsFocused, Is.True);
                Assert.That(lighting.BoardSpotlight.enabled, Is.True);
                Assert.That(lighting.FocusMaskRenderer.enabled, Is.True);
                Assert.That(LawyerBoardFocusLighting.CurrentScreenDimAmount,
                    Is.EqualTo(0.55f).Within(0.01f));
                Assert.That(LawyerBoardFocusLighting.CurrentOverlayActive, Is.True);
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
                    if (Mathf.Abs(state.Key.intensity - state.Value * 0.45f) < 0.02f)
                        dimmedLightCount++;
                Assert.That(dimmedLightCount, Is.GreaterThan(0));

                Quaternion fixedRotation = board.rotation;
                Vector3 fixedPosition = board.position;
                for (int frame = 0; frame < 10; frame++)
                    yield return null;
                Assert.That(Vector3.Distance(board.position, fixedPosition), Is.LessThan(0.0001f));
                Assert.That(Quaternion.Angle(board.rotation, fixedRotation), Is.LessThan(0.001f));

                controller.CloseBoard();
                Assert.That(LawyerBoardFocusLighting.CurrentOverlayActive, Is.True);
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
                Assert.That(lighting.FocusMaskRenderer.enabled, Is.False);
                Assert.That(LawyerBoardFocusLighting.CurrentScreenDimAmount,
                    Is.Zero.Within(0.001f));
                Assert.That(LawyerBoardFocusLighting.CurrentOverlayActive, Is.False);
                foreach (KeyValuePair<Light, float> state in lightIntensities)
                    Assert.That(state.Key.intensity, Is.EqualTo(state.Value).Within(0.02f), state.Key.name);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousIgnore;
            }
        }

        [UnityTest]
        public IEnumerator ReleasedClueSnapsInsideEvidenceSurfaceAndPersistsPosition()
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
                Assert.That(clues.Length, Is.GreaterThanOrEqualTo(2));
                LawyerBoardClue clue = clues[0];
                Assert.That(clue.ClueId, Is.Not.Empty);
                Assert.That(controller.Session.UnlockedClueIds, Does.Contain(clue.ClueId));
                Transform boardSurface = clue.transform.parent.parent;
                Renderer evidenceRenderer = controller.transform.Find(
                    "Lawyer Board Content/Physical Evidence Surface").GetComponent<Renderer>();
                MeshFilter evidenceMesh = evidenceRenderer.GetComponent<MeshFilter>();
                BoxCollider clueCollider = clue.GetComponent<BoxCollider>();
                Bounds evidenceBounds = BoundsInLocalSpace(
                    evidenceMesh.sharedMesh.bounds, evidenceRenderer.transform, boardSurface);
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
                Assert.That(snapped.z, Is.EqualTo(validDepth).Within(0.001f));
                Assert.That(Quaternion.Angle(clue.transform.rotation, boardSurface.rotation),
                    Is.LessThan(0.01f));
                Bounds snappedClueBounds = BoundsInLocalSpace(
                    new Bounds(clueCollider.center, clueCollider.size), clue.transform, boardSurface);
                Assert.That(snappedClueBounds.min.x,
                    Is.GreaterThanOrEqualTo(evidenceBounds.min.x - 0.001f));
                Assert.That(snappedClueBounds.max.x,
                    Is.LessThanOrEqualTo(evidenceBounds.max.x + 0.001f));
                Assert.That(snappedClueBounds.min.y,
                    Is.GreaterThanOrEqualTo(evidenceBounds.min.y - 0.001f));
                Assert.That(snappedClueBounds.max.y,
                    Is.LessThanOrEqualTo(evidenceBounds.max.y + 0.001f));
                Assert.That(controller.Session.TryGetCluePosition(
                    clue.ClueId, out Vector2 savedPosition), Is.True);
                Assert.That(savedPosition.x, Is.EqualTo(snapped.x).Within(0.001f));
                Assert.That(savedPosition.y, Is.EqualTo(snapped.y).Within(0.001f));

                LawyerBoardClue occupiedClue = clues[1];
                Vector2 occupiedPosition = occupiedClue.BoardLocalPosition;
                clue.transform.position = boardSurface.TransformPoint(
                    new Vector3(occupiedPosition.x, occupiedPosition.y, validDepth + 0.5f));
                clue.transform.rotation = boardSurface.rotation * Quaternion.Euler(20f, 45f, 10f);
                grab.selectExited.Invoke(null);

                waitUntil = Time.realtimeSinceStartup + 0.2f;
                while (Time.realtimeSinceStartup < waitUntil)
                    yield return null;

                Bounds resolvedClueBounds = BoundsInLocalSpace(
                    new Bounds(clueCollider.center, clueCollider.size), clue.transform, boardSurface);
                BoxCollider occupiedCollider = occupiedClue.GetComponent<BoxCollider>();
                Bounds occupiedClueBounds = BoundsInLocalSpace(
                    new Bounds(occupiedCollider.center, occupiedCollider.size),
                    occupiedClue.transform,
                    boardSurface);
                bool separated = resolvedClueBounds.max.x <= occupiedClueBounds.min.x + 0.001f
                    || resolvedClueBounds.min.x >= occupiedClueBounds.max.x - 0.001f
                    || resolvedClueBounds.max.y <= occupiedClueBounds.min.y + 0.001f
                    || resolvedClueBounds.min.y >= occupiedClueBounds.max.y - 0.001f;
                Assert.That(separated, Is.True,
                    "Released clue cards must not overlap another placed clue.");
                Assert.That(resolvedClueBounds.min.x,
                    Is.GreaterThanOrEqualTo(evidenceBounds.min.x - 0.001f));
                Assert.That(resolvedClueBounds.max.x,
                    Is.LessThanOrEqualTo(evidenceBounds.max.x + 0.001f));
                Assert.That(resolvedClueBounds.min.y,
                    Is.GreaterThanOrEqualTo(evidenceBounds.min.y - 0.001f));
                Assert.That(resolvedClueBounds.max.y,
                    Is.LessThanOrEqualTo(evidenceBounds.max.y + 0.001f));
                Assert.That(controller.Session.TryGetCluePosition(
                    clue.ClueId, out savedPosition), Is.True);
                Assert.That(savedPosition.x,
                    Is.EqualTo(clue.BoardLocalPosition.x).Within(0.001f));
                Assert.That(savedPosition.y,
                    Is.EqualTo(clue.BoardLocalPosition.y).Within(0.001f));
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousIgnore;
            }
        }

        [UnityTest]
        public IEnumerator CourtroomQuestionNavigationUsesDelayedSlowRepeat()
        {
            XRSimulatedController leftController = null;
            try
            {
                yield return SceneManager.LoadSceneAsync(
                    "Assets/Scenes/Lawyer/Office.unity", LoadSceneMode.Single);
                yield return null;
                LawyerBoardController board = Object.FindAnyObjectByType<LawyerBoardController>();
                board.Session.ResetCase(board.Session.CaseDefinition);
                board.Session.MarkBoardOpened();
                Assert.That(board.Session.TryBeginInterview(), Is.True);

                yield return SceneManager.LoadSceneAsync(
                    LawyerBoardController.CourtroomScenePath, LoadSceneMode.Single);
                yield return null;
                LawyerInterviewController interview =
                    Object.FindAnyObjectByType<LawyerInterviewController>();
                Assert.That(interview, Is.Not.Null);

                leftController = InputSystem.AddDevice<XRSimulatedController>();
                InputSystem.SetDeviceUsage(leftController, CommonUsages.LeftHand);
                FieldInfo selectedIndexField = typeof(LawyerInterviewController).GetField(
                    "selectedMainIndex", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(selectedIndexField, Is.Not.Null);
                Assert.That((int)selectedIndexField.GetValue(interview), Is.EqualTo(0));

                QueueNavigation(leftController, -1f);
                yield return null;
                Assert.That((int)selectedIndexField.GetValue(interview), Is.EqualTo(1));

                yield return new WaitForSecondsRealtime(0.25f);
                Assert.That((int)selectedIndexField.GetValue(interview), Is.EqualTo(1));

                yield return new WaitForSecondsRealtime(0.4f);
                Assert.That((int)selectedIndexField.GetValue(interview), Is.EqualTo(2));

                yield return new WaitForSecondsRealtime(0.2f);
                Assert.That((int)selectedIndexField.GetValue(interview), Is.EqualTo(2));

                yield return new WaitForSecondsRealtime(0.2f);
                Assert.That((int)selectedIndexField.GetValue(interview), Is.EqualTo(3));

                QueueNavigation(leftController, 0f);
                yield return null;
                QueueNavigation(leftController, -1f);
                yield return null;
                Assert.That((int)selectedIndexField.GetValue(interview), Is.EqualTo(4));
            }
            finally
            {
                if (leftController != null && leftController.added)
                    InputSystem.RemoveDevice(leftController);
            }
        }

        [UnityTest]
        public IEnumerator PhysicalContinueButtonLoadsCourtroomScene()
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
            Assert.That(controller.IsOpen, Is.True);

            LawyerPhysicalButton button = controller.transform.Find(
                "Lawyer Board Content/Physical Continue Button")
                .GetComponent<LawyerPhysicalButton>();
            Assert.That(button, Is.Not.Null);
            Assert.That(button.Interactable, Is.True);
            BoxCollider buttonCollider = button.GetComponent<BoxCollider>();
            for (int sample = -1; sample <= 1; sample++)
            {
                Vector3 localTarget = buttonCollider.center + new Vector3(
                    0f, buttonCollider.size.y * 0.4f * sample, buttonCollider.size.z * 0.5f);
                Vector3 target = button.transform.TransformPoint(localTarget);
                Vector3 origin = target + controller.transform.forward * 0.3f;
                Assert.That(Physics.Raycast(
                    origin, -controller.transform.forward, out RaycastHit hit, 0.5f,
                    1 << button.gameObject.layer), Is.True, $"Mẫu nút {sample}");
                Assert.That(hit.collider, Is.SameAs(buttonCollider), $"Mẫu nút {sample}");
            }
            button.GetComponent<XRSimpleInteractable>().activated.Invoke(null);

            float timeout = Time.realtimeSinceStartup + 60f;
            while (SceneManager.GetActiveScene().path != LawyerBoardController.CourtroomScenePath
                && Time.realtimeSinceStartup < timeout)
                yield return null;

            Assert.That(SceneManager.GetActiveScene().path,
                Is.EqualTo(LawyerBoardController.CourtroomScenePath));
            Assert.That(LawyerGameSession.Instance.Phase,
                Is.EqualTo(LawyerSessionPhase.DefendantInterview));
        }

        [UnityTest]
        public IEnumerator EvidenceBoardRejectsWrongLinksAndRevealsTruthAfterRequiredLinks()
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
                CompleteSuccessfulInterview(controller.Session);
                yield return SceneManager.LoadSceneAsync(
                    "Assets/Scenes/Lawyer/Office.unity", LoadSceneMode.Single);
                yield return null;

                controller = Object.FindAnyObjectByType<LawyerBoardController>();
                Assert.That(controller.Session.Phase, Is.EqualTo(LawyerSessionPhase.EvidenceBoard));
                SetPrivateField(controller, "truthDisplayDuration", 5f);
                for (int frame = 0; frame < 60 && !controller.IsOpen; frame++)
                    yield return null;

                LawyerClueLinkSystem linkSystem =
                    controller.GetComponentInChildren<LawyerClueLinkSystem>(true);
                LawyerCluePin[] pins = controller.GetComponentsInChildren<LawyerCluePin>(false);
                Assert.That(linkSystem, Is.Not.Null);
                Assert.That(pins.Length, Is.GreaterThanOrEqualTo(8));
                Assert.That(controller.Session.ClueLinks, Is.Empty);

                LawyerCluePin exit = FindPin(pins, "clue-01");
                LawyerCluePin charger = FindPin(pins, "clue-09");
                LawyerCluePin clearHallway = FindPin(pins, "clue-11");
                LawyerCluePin nam = FindPin(pins, "clue-15");
                LawyerCluePin origin = FindPin(pins, "clue-04");
                LawyerCluePin matchingCharger = FindPin(pins, "clue-10");

                testRayObject = new GameObject("Test Pinch Ray");
                XRRayInteractor testRay = testRayObject.AddComponent<XRRayInteractor>();
                LawyerLinkAttemptResult lastAttempt = LawyerLinkAttemptResult.Invalid;
                linkSystem.LinkAttempted += result => lastAttempt = result;

                linkSystem.BeginLink(exit, testRay);
                linkSystem.CompleteLink(exit, clearHallway.AnchorPosition);
                Assert.That(lastAttempt, Is.EqualTo(LawyerLinkAttemptResult.Incorrect));
                Assert.That(controller.Session.ClueLinks, Is.Empty);

                linkSystem.BeginLink(exit, testRay);
                linkSystem.CompleteLink(exit, charger.AnchorPosition);
                Assert.That(lastAttempt, Is.EqualTo(LawyerLinkAttemptResult.Added));
                Assert.That(controller.Session.ClueLinks, Has.Count.EqualTo(1));
                for (int frame = 0; frame < 5; frame++)
                    yield return null;
                Assert.That(linkSystem.TerminalLinkCount, Is.EqualTo(1));

                linkSystem.UndoLastLink();
                Assert.That(controller.Session.ClueLinks, Is.Empty);
                Assert.That(linkSystem.TerminalLinkCount, Is.EqualTo(1));

                linkSystem.BeginLink(charger, testRay);
                linkSystem.CompleteLink(charger, exit.AnchorPosition);
                linkSystem.BeginLink(clearHallway, testRay);
                linkSystem.CompleteLink(clearHallway, nam.AnchorPosition);
                linkSystem.BeginLink(origin, testRay);
                linkSystem.CompleteLink(origin, matchingCharger.AnchorPosition);
                Assert.That(controller.Session.Phase, Is.EqualTo(LawyerSessionPhase.Resolved));
                Assert.That(controller.Session.ClueLinks, Has.Count.EqualTo(3));
                SetPrivateField(controller, "truthDisplayDuration", 5f);
                LawyerBoardClue[] finalCards = System.Array.Empty<LawyerBoardClue>();
                for (int frame = 0; frame < 60; frame++)
                {
                    finalCards = controller.GetComponentsInChildren<LawyerBoardClue>(false);
                    if (finalCards.Length == 3)
                        break;
                    yield return null;
                }
                Assert.That(finalCards, Has.Length.EqualTo(3));
                for (int index = 0; index < finalCards.Length; index++)
                {
                    Assert.That(finalCards[index].IsReasoningCard, Is.True,
                        $"Thẻ cuối không phải thẻ suy luận: {finalCards[index].name}");
                    BoxCollider diamond = finalCards[index].transform
                        .Find("Diamond Collider")?.GetComponent<BoxCollider>();
                    Assert.That(diamond, Is.Not.Null);
                    Assert.That(diamond.gameObject.activeSelf, Is.True,
                        $"Collider hình thoi chưa bật: {finalCards[index].name}");
                    TMP_Text[] cardText = finalCards[index].GetComponentsInChildren<TMP_Text>(true);
                    for (int textIndex = 0; textIndex < cardText.Length; textIndex++)
                    {
                        cardText[textIndex].ForceMeshUpdate();
                        Assert.That(cardText[textIndex].isTextOverflowing, Is.False,
                            $"Thẻ suy luận bị tràn chữ: {finalCards[index].CardId}");
                    }
                }

                Transform brief = Camera.main.transform.Find(
                    "Lawyer Finale Canvas/Evidence Result Overlay/Result Body");
                Assert.That(brief, Is.Not.Null);
                Assert.That(brief.gameObject.activeInHierarchy, Is.True,
                    "Giao diện sự thật chưa hiển thị");
                Assert.That(brief.GetComponent<TMP_Text>().text,
                    Is.EqualTo(controller.Session.CaseDefinition.Resolution.TruthConclusion));

                SetPrivateField(controller, "truthDisplayDuration", 0.01f);

                float sceneTimeout = Time.realtimeSinceStartup + 60f;
                while (SceneManager.GetActiveScene().path
                    != LawyerBoardController.CourtroomScenePath
                    && Time.realtimeSinceStartup < sceneTimeout)
                    yield return null;
                Assert.That(SceneManager.GetActiveScene().path,
                    Is.EqualTo(LawyerBoardController.CourtroomScenePath));
                Assert.That(LawyerGameSession.Instance.Phase,
                    Is.EqualTo(LawyerSessionPhase.DefensePreparation));
                LawyerDefenseController defense =
                    Object.FindAnyObjectByType<LawyerDefenseController>();
                Assert.That(defense, Is.Not.Null);
                yield return null;
                Assert.That(defense.IsDefenseMode, Is.True, "Chế độ bào chữa chưa bắt đầu");
                Transform interviewHud = Camera.main.transform.Find("Lawyer Interview Canvas");
                Transform defenseHud = Camera.main.transform.Find("Lawyer Defense Canvas");
                Assert.That(interviewHud.gameObject.activeSelf, Is.False);
                Assert.That(defenseHud.gameObject.activeSelf, Is.True);
            }
            finally
            {
                if (testRayObject != null)
                    Object.Destroy(testRayObject);
                LogAssert.ignoreFailingMessages = previousIgnore;
            }
        }

        private static void SetPrivateField<T>(object target, string name, T value)
        {
            FieldInfo field = target.GetType().GetField(
                name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        [UnityTest]
        public IEnumerator EarlyExitWithInsufficientEvidenceStaysInCourtroom()
        {
            XRSimulatedController rightController = null;
            try
            {
                yield return SceneManager.LoadSceneAsync(
                    LawyerBoardController.CourtroomScenePath, LoadSceneMode.Single);
                yield return null;
                LawyerInterviewController interview =
                    Object.FindAnyObjectByType<LawyerInterviewController>();
                Assert.That(interview, Is.Not.Null);
                LawyerGameSession session = interview.Session;
                session.ResetCase(session.CaseDefinition);
                session.MarkBoardOpened();
                Assert.That(session.TryBeginInterview(), Is.True);
                Assert.That(session.TryAskMain("main-outlet", out _), Is.True);
                Assert.That(session.TryAskFollowUp("main-outlet", "outlet-other-device", out _), Is.True);
                Assert.That(session.TryAskMain("main-return", out _), Is.True);

                rightController = InputSystem.AddDevice<XRSimulatedController>();
                InputSystem.SetDeviceUsage(rightController, CommonUsages.RightHand);
                QueueSecondaryButton(rightController, true);
                QueueSecondaryButton(rightController, false);
                yield return null;
                Assert.That(SceneManager.GetActiveScene().path,
                    Is.EqualTo(LawyerBoardController.CourtroomScenePath));
                Assert.That(session.Phase, Is.EqualTo(LawyerSessionPhase.DefendantInterview));

                Transform result = Camera.main.transform.Find(
                    "Lawyer Interview Canvas/Response Panel");
                Assert.That(result, Is.Not.Null);
                Assert.That(result.gameObject.activeSelf, Is.True);
                TMP_Text response = result.Find("Response").GetComponent<TMP_Text>();
                Assert.That(response.text, Does.Contain("CHƯA ĐỦ CHỨNG CỨ"));
                Assert.That(response.text,
                    Does.Contain(session.CaseDefinition.Resolution.InsufficientEvidenceMessage));

                Button continueButton = result.Find("Continue Response").GetComponent<Button>();
                Assert.That(continueButton.GetComponentInChildren<TMP_Text>(true).text,
                    Is.EqualTo("Tiếp tục hỏi"));
                Assert.That(continueButton.interactable, Is.True);
                continueButton.onClick.Invoke();
                yield return null;
                Transform questions = Camera.main.transform.Find("Lawyer Interview Canvas/Main Question Panel");
                Assert.That(questions, Is.Not.Null);
                Assert.That(questions.gameObject.activeSelf, Is.True);
                Assert.That(SceneManager.GetActiveScene().path,
                    Is.EqualTo(LawyerBoardController.CourtroomScenePath));
                Assert.That(LawyerGameSession.Instance.Phase,
                    Is.EqualTo(LawyerSessionPhase.DefendantInterview));
                Assert.That(LawyerGameSession.Instance.MainQuestionsAsked, Is.EqualTo(2));
                Assert.That(LawyerGameSession.Instance.MainQuestionsRemaining, Is.Zero);
                Assert.That(LawyerGameSession.Instance.PendingKeyQuestionId,
                    Is.EqualTo("main-outlet"));
                Assert.That(LawyerGameSession.Instance.ClueLinks, Is.Empty);
                Assert.That(LawyerGameSession.Instance.UnlockedClueIds,
                    Does.Contain("clue-09"));
            }
            finally
            {
                if (rightController != null && rightController.added)
                    InputSystem.RemoveDevice(rightController);
            }
        }

        [UnityTest]
        public IEnumerator CloseBoardActionWithSufficientEvidenceLoadsOfficeBoard()
        {
            XRSimulatedController rightController = null;
            try
            {
                yield return SceneManager.LoadSceneAsync(
                    LawyerBoardController.CourtroomScenePath, LoadSceneMode.Single);
                yield return null;
                LawyerInterviewController interview =
                    Object.FindAnyObjectByType<LawyerInterviewController>();
                Assert.That(interview, Is.Not.Null);
                LawyerGameSession session = interview.Session;
                session.ResetCase(session.CaseDefinition);
                session.MarkBoardOpened();
                Assert.That(session.TryBeginInterview(), Is.True);
                AskSuccessfulInterviewQuestions(session);

                rightController = InputSystem.AddDevice<XRSimulatedController>();
                InputSystem.SetDeviceUsage(rightController, CommonUsages.RightHand);
                QueueSecondaryButton(rightController, true);
                QueueSecondaryButton(rightController, false);

                float timeout = Time.realtimeSinceStartup + 60f;
                while (SceneManager.GetActiveScene().path != LawyerInterviewController.OfficeScenePath
                    && Time.realtimeSinceStartup < timeout)
                    yield return null;
                Assert.That(SceneManager.GetActiveScene().path,
                    Is.EqualTo(LawyerInterviewController.OfficeScenePath));
                Assert.That(LawyerGameSession.Instance.Phase,
                    Is.EqualTo(LawyerSessionPhase.EvidenceBoard));
            }
            finally
            {
                if (rightController != null && rightController.added)
                    InputSystem.RemoveDevice(rightController);
            }
        }

        [UnityTest]
        public IEnumerator FinalResponseContinuationReturnsToOfficeAfterAllQuestions()
        {
            yield return SceneManager.LoadSceneAsync(
                LawyerBoardController.CourtroomScenePath, LoadSceneMode.Single);
            yield return null;
            LawyerInterviewController interview =
                Object.FindAnyObjectByType<LawyerInterviewController>();
            Assert.That(interview, Is.Not.Null);
            LawyerGameSession session = interview.Session;
            session.ResetCase(session.CaseDefinition);
            session.MarkBoardOpened();
            Assert.That(session.TryBeginInterview(), Is.True);
            yield return null;

            InvokePrivate(interview, "AskMain", 1);
            interview.AdvanceResponse();
            yield return null;
            InvokePrivate(interview, "AskMain", 3);
            interview.AdvanceResponse();
            yield return null;
            InvokePrivate(interview, "AskMain", 4);
            interview.AdvanceResponse();
            yield return null;
            InvokePrivate(interview, "AskMain", 0);
            interview.AdvanceResponse();
            yield return null;
            for (int followIndex = 0; followIndex < 3; followIndex++)
            {
                InvokePrivate(interview, "AskFollowUp", followIndex);
                interview.AdvanceResponse();
                yield return null;
                if (followIndex < 2)
                {
                    InvokePrivate(interview, "AskMain", 0);
                    Assert.That(session.Phase, Is.EqualTo(LawyerSessionPhase.DefendantInterview));
                }
            }

            InvokePrivate(interview, "AskMain", 2);
            interview.AdvanceResponse();
            yield return null;
            for (int followIndex = 0; followIndex < 3; followIndex++)
            {
                InvokePrivate(interview, "AskFollowUp", followIndex);
                interview.AdvanceResponse();
                yield return null;
                if (followIndex < 2)
                    InvokePrivate(interview, "AskMain", 2);
            }

            float timeout = Time.realtimeSinceStartup + 60f;
            while (SceneManager.GetActiveScene().path != LawyerInterviewController.OfficeScenePath
                && Time.realtimeSinceStartup < timeout)
                yield return null;
            Assert.That(SceneManager.GetActiveScene().path,
                Is.EqualTo(LawyerInterviewController.OfficeScenePath));
            Assert.That(LawyerGameSession.Instance.Phase,
                Is.EqualTo(LawyerSessionPhase.EvidenceBoard));
        }

        [UnityTest]
        public IEnumerator SufficientEvidenceLoadsOfficeAfterCourtroomInterview()
        {
            yield return SceneManager.LoadSceneAsync(
                LawyerBoardController.CourtroomScenePath, LoadSceneMode.Single);
            yield return null;
            LawyerInterviewController interview =
                Object.FindAnyObjectByType<LawyerInterviewController>();
            Assert.That(interview, Is.Not.Null);
            LawyerGameSession session = interview.Session;
            session.ResetCase(session.CaseDefinition);
            session.MarkBoardOpened();
            Assert.That(session.TryBeginInterview(), Is.True);
            AskSuccessfulInterviewQuestions(session);
            interview.SendMessage("CompleteInterview");

            float timeout = Time.realtimeSinceStartup + 60f;
            while (SceneManager.GetActiveScene().path != LawyerInterviewController.OfficeScenePath
                && Time.realtimeSinceStartup < timeout)
                yield return null;

            Assert.That(SceneManager.GetActiveScene().path,
                Is.EqualTo(LawyerInterviewController.OfficeScenePath));
            Assert.That(LawyerGameSession.Instance.Phase,
                Is.EqualTo(LawyerSessionPhase.EvidenceBoard));
        }

        private static void CompleteSuccessfulInterview(LawyerGameSession session)
        {
            session.MarkBoardOpened();
            Assert.That(session.TryBeginInterview(), Is.True);
            AskSuccessfulInterviewQuestions(session);
            Assert.That(session.TryCompleteInterview(), Is.True);
        }

        private static void AskSuccessfulInterviewQuestions(LawyerGameSession session)
        {
            Assert.That(session.TryAskMain("main-outlet", out _), Is.True);
            Assert.That(session.TryAskFollowUp("main-outlet", "outlet-charger", out _), Is.True);
            Assert.That(session.TryAskFollowUp("main-outlet", "outlet-other-device", out _), Is.True);
            Assert.That(session.TryAskFollowUp("main-outlet", "outlet-warning", out _), Is.True);
            Assert.That(session.TryAskMain("main-other-person", out _), Is.True);
            Assert.That(session.TryAskFollowUp("main-other-person", "other-person-action", out _), Is.True);
            Assert.That(session.TryAskFollowUp("main-other-person", "other-person-purpose", out _), Is.True);
            Assert.That(session.TryAskFollowUp("main-other-person", "other-person-blame", out _), Is.True);
        }

        private static LawyerCluePin FindPin(LawyerCluePin[] pins, string clueId)
        {
            for (int index = 0; index < pins.Length; index++)
                if (pins[index].Clue != null && pins[index].Clue.ClueId == clueId)
                    return pins[index];
            Assert.Fail($"Không tìm thấy chốt của manh mối '{clueId}'.");
            return null;
        }

        private static void QueueNavigation(XRSimulatedController controller, float vertical)
        {
            InputSystem.QueueDeltaStateEvent(
                controller.primary2DAxis, new Vector2(0f, vertical));
            InputSystem.Update();
        }

        private static void QueueSecondaryButton(XRSimulatedController controller, bool pressed)
        {
            using (StateEvent.From(controller, out InputEventPtr eventPointer))
            {
                controller.secondaryButton.WriteValueIntoEvent(pressed ? 1f : 0f, eventPointer);
                InputSystem.QueueEvent(eventPointer);
            }
            InputSystem.Update();
        }

        private static void InvokePrivate(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, arguments);
        }

        private static Bounds BoundsInLocalSpace(
            Bounds sourceBounds, Transform sourceSpace, Transform targetSpace)
        {
            Vector3 center = sourceBounds.center;
            Vector3 extents = sourceBounds.extents;
            Bounds result = new Bounds(
                targetSpace.InverseTransformPoint(sourceSpace.TransformPoint(center - extents)),
                Vector3.zero);
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                result.Encapsulate(targetSpace.InverseTransformPoint(
                    sourceSpace.TransformPoint(corner)));
            }
            return result;
        }
    }
}
