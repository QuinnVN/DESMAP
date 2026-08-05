using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NCKH.VR.Clinic.Tests
{
    public sealed class ClinicGameplayPlayModeTests
    {
        [UnityTest]
        public IEnumerator ManagerWaitsForPreparationBeforeFirstArrival()
        {
            GameObject templateObject = new GameObject("Patient Template");
            templateObject.SetActive(false);
            PatientActor template = templateObject.AddComponent<PatientActor>();

            GameObject bedObject = new GameObject("Bed");
            PatientBedSlot bed = bedObject.AddComponent<PatientBedSlot>();

            GameObject headObject = new GameObject("Head");
            headObject.transform.position = Vector3.forward;

            GameObject managerObject = new GameObject("Manager");
            managerObject.SetActive(false);
            ClinicGameManager manager = managerObject.AddComponent<ClinicGameManager>();
            SetField(manager, "patientPrefab", template);
            SetField(manager, "playerHead", headObject.transform);
            SetField(manager, "bedSlots", new[] { bed });
            SetField(manager, "initialPreparationDelay", 0.05f);
            SetField(manager, "roundDuration", 1f);
            SetField(manager, "patientFadeDuration", 0f);
            SetField(manager, "startAutomatically", false);
            managerObject.SetActive(true);

            manager.StartRound();
            Assert.That(manager.RoundState, Is.EqualTo(ClinicRoundState.Preparing));
            Assert.That(manager.RemainingRoundTime, Is.EqualTo(1f));
            Assert.That(bed.IsOccupied, Is.False);

            yield return new WaitForSeconds(0.02f);
            Assert.That(bed.IsOccupied, Is.False);
            Assert.That(manager.RemainingRoundTime, Is.EqualTo(1f));

            yield return new WaitForSeconds(0.05f);
            Assert.That(manager.RoundState, Is.EqualTo(ClinicRoundState.Running));
            Assert.That(bed.IsOccupied, Is.True);

            Object.Destroy(managerObject);
            Object.Destroy(templateObject);
            Object.Destroy(bedObject);
            Object.Destroy(headObject);
        }

        [UnityTest]
        public IEnumerator AutomaticStartWaitsForStableFramesBeforePreparation()
        {
            ClinicGameManager manager = CreateManagerForHudTest(
                2f,
                60f,
                out GameObject managerObject,
                out GameObject templateObject,
                out GameObject bedObject,
                out GameObject headObject,
                startAutomatically: true,
                automaticStartReadinessTimeout: 0f,
                automaticStartStableFrames: 2);

            int activationFrame = Time.frameCount;
            Assert.That(manager.RoundState, Is.EqualTo(ClinicRoundState.Idle));

            while (manager.RoundState == ClinicRoundState.Idle
                && Time.frameCount - activationFrame < 10)
            {
                yield return null;
            }

            Assert.That(manager.RoundState, Is.EqualTo(ClinicRoundState.Preparing));
            Assert.That(Time.frameCount - activationFrame, Is.GreaterThanOrEqualTo(2));
            Assert.That(manager.RemainingPreparationTime, Is.GreaterThan(1.8f));
            Assert.That(manager.RemainingRoundTime, Is.EqualTo(60f));

            Object.Destroy(managerObject);
            Object.Destroy(templateObject);
            Object.Destroy(bedObject);
            Object.Destroy(headObject);
        }

        [UnityTest]
        public IEnumerator HudShowsScoreAndKeepsLocomotionFrozenWhenRoundStops()
        {
            ClinicGameManager manager = CreateManagerForHudTest(2f, 60f, out GameObject managerObject, out GameObject templateObject, out GameObject bedObject, out GameObject headObject);
            GameObject locomotion = new GameObject("Locomotion");
            ClinicRoundHud hud = CreateHud(manager, locomotion, 0.01f, out TMP_Text roundTimer, out TMP_Text preparationTimer, out TMP_Text totalScore, out CanvasGroup overlay);

            RectTransform timerRect = (RectTransform)roundTimer.transform.parent;
            RectTransform overlayRect = (RectTransform)overlay.transform;
            Vector2 editorPosition = new Vector2(24f, -36f);
            Vector2 overlayPosition = new Vector2(8f, 16f);
            Vector2 preparationPosition = new Vector2(-18f, 30f);
            Vector2 scorePosition = new Vector2(12f, -20f);
            timerRect.anchoredPosition = editorPosition;
            overlayRect.anchoredPosition = overlayPosition;
            preparationTimer.rectTransform.anchoredPosition = preparationPosition;
            totalScore.rectTransform.anchoredPosition = scorePosition;

            manager.StartRound();
            yield return null;

            Assert.That(manager.RoundState, Is.EqualTo(ClinicRoundState.Preparing));
            Assert.That(roundTimer.gameObject.activeSelf, Is.True);
            Assert.That(roundTimer.text, Is.EqualTo("1:00"));
            Assert.That(timerRect.anchoredPosition, Is.EqualTo(editorPosition));
            Assert.That(preparationTimer.text, Is.EqualTo("2"));
            Assert.That(preparationTimer.rectTransform.anchoredPosition, Is.EqualTo(preparationPosition));
            Assert.That(overlay.gameObject.activeSelf, Is.True);
            Assert.That(overlay.alpha, Is.EqualTo(1f));
            Assert.That(overlayRect.anchoredPosition, Is.EqualTo(overlayPosition));
            Assert.That(locomotion.activeSelf, Is.False);

            SetField(manager, "<Score>k__BackingField", 42);
            manager.StopRound();
            yield return null;
            Assert.That(locomotion.activeSelf, Is.False);
            Assert.That(overlay.gameObject.activeSelf, Is.True);
            Assert.That(overlay.alpha, Is.EqualTo(1f));
            Assert.That(preparationTimer.gameObject.activeSelf, Is.False);
            Assert.That(totalScore.gameObject.activeSelf, Is.True);
            Assert.That(totalScore.text, Is.EqualTo("TOTAL SCORE\n42"));
            Assert.That(totalScore.rectTransform.anchoredPosition, Is.EqualTo(scorePosition));

            hud.enabled = false;
            Assert.That(locomotion.activeSelf, Is.True);
            Assert.That(overlay.gameObject.activeSelf, Is.False);

            Object.Destroy(hud.gameObject);
            Object.Destroy(locomotion);
            Object.Destroy(managerObject);
            Object.Destroy(templateObject);
            Object.Destroy(bedObject);
            Object.Destroy(headObject);
        }

        [UnityTest]
        public IEnumerator HudUnfreezesFadesAndRetainsZeroAtRoundEnd()
        {
            ClinicGameManager manager = CreateManagerForHudTest(0.03f, 1f, out GameObject managerObject, out GameObject templateObject, out GameObject bedObject, out GameObject headObject);
            GameObject locomotion = new GameObject("Locomotion");
            ClinicRoundHud hud = CreateHud(manager, locomotion, 0.01f, out TMP_Text roundTimer, out _, out TMP_Text totalScore, out CanvasGroup overlay);

            manager.StartRound();
            yield return null;
            Assert.That(locomotion.activeSelf, Is.False);

            yield return new WaitForSeconds(0.05f);
            Assert.That(manager.RoundState, Is.EqualTo(ClinicRoundState.Running));
            Assert.That(locomotion.activeSelf, Is.True);
            Assert.That(roundTimer.transform.parent.gameObject.activeSelf, Is.True);
            Assert.That(roundTimer.gameObject.activeSelf, Is.True);

            yield return new WaitForSeconds(0.03f);
            Assert.That(overlay.alpha, Is.Zero);
            Assert.That(overlay.gameObject.activeSelf, Is.False);
            Assert.That(roundTimer.transform.parent.gameObject.activeSelf, Is.True);
            Assert.That(roundTimer.gameObject.activeSelf, Is.True);

            manager.StopRound();
            yield return null;
            Assert.That(manager.RoundState, Is.EqualTo(ClinicRoundState.Finished));
            Assert.That(roundTimer.gameObject.activeSelf, Is.True);
            Assert.That(roundTimer.text, Is.EqualTo("0:00"));
            Assert.That(locomotion.activeSelf, Is.False);
            Assert.That(overlay.gameObject.activeSelf, Is.True);
            Assert.That(overlay.alpha, Is.EqualTo(1f));
            Assert.That(totalScore.gameObject.activeSelf, Is.True);
            Assert.That(totalScore.text, Is.EqualTo("TOTAL SCORE\n0"));

            Object.Destroy(hud.gameObject);
            Object.Destroy(locomotion);
            Object.Destroy(managerObject);
            Object.Destroy(templateObject);
            Object.Destroy(bedObject);
            Object.Destroy(headObject);
        }

        [UnityTest]
        public IEnumerator CureResolvesOnceAndCompletesFadeOut()
        {
            PatientActor patient = CreateInteractivePatient(out Button button, out Transform head);
            int cureCount = 0;
            int inactiveCount = 0;
            patient.Resolved += (_, resolution) =>
            {
                if (resolution == PatientResolution.Cured)
                    cureCount++;
            };
            patient.BecameInactive += _ => inactiveCount++;

            patient.Activate(1, 30f, "Sickness: [Placeholder]", head, 0f);
            yield return null;

            Assert.That(patient.State, Is.EqualTo(PatientActorState.Active));
            Assert.That(button.interactable, Is.True);
            button.onClick.Invoke();
            button.onClick.Invoke();
            Assert.That(cureCount, Is.EqualTo(1));

            yield return null;
            Assert.That(patient.State, Is.EqualTo(PatientActorState.Inactive));
            Assert.That(inactiveCount, Is.EqualTo(1));
            Assert.That(patient.gameObject.activeSelf, Is.True);
            Assert.That(patient.enabled, Is.False);
            Renderer[] renderers = (Renderer[])GetField(patient, "fadeRenderers");
            Canvas[] canvases = (Canvas[])GetField(patient, "patientCanvases");
            Assert.That(renderers[0].enabled, Is.False);
            Assert.That(canvases[0].enabled, Is.False);

            Object.Destroy(head.gameObject);
            Object.Destroy(patient.gameObject);
        }

        [UnityTest]
        public IEnumerator CureButtonOrbitsWithYawOnly()
        {
            PatientActor patient = CreateInteractivePatient(out _, out Transform head);
            Transform pivot = (Transform)GetField(patient, "cureOrbitPivot");
            Transform cureCanvas = (Transform)GetField(patient, "cureCanvas");
            head.position = new Vector3(2f, 3f, 0f);
            SetField(patient, "orbitHeight", 0.5f);

            Assert.That((float)GetField(patient, "orbitRadius"), Is.EqualTo(1.1f));

            patient.Activate(1, 30f, "Sickness: [Placeholder]", head, 0f);
            yield return null;

            Assert.That(pivot.position.y, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(Mathf.DeltaAngle(0f, pivot.eulerAngles.x), Is.EqualTo(0f).Within(0.001f));
            Assert.That(Mathf.DeltaAngle(0f, pivot.eulerAngles.z), Is.EqualTo(0f).Within(0.001f));
            Assert.That(cureCanvas.localPosition.x, Is.EqualTo(-1f).Within(0.001f));
            Assert.That(cureCanvas.localPosition.z, Is.EqualTo(1.1f).Within(0.001f));
            Assert.That(Mathf.DeltaAngle(180f, cureCanvas.localEulerAngles.y), Is.EqualTo(0f).Within(0.001f));

            Object.Destroy(head.gameObject);
            Object.Destroy(patient.gameObject);
        }

        [UnityTest]
        public IEnumerator PatientLabelIncludesSeverity()
        {
            PatientActor patient = CreateInteractivePatient(out _, out Transform head);
            GameObject textObject = new GameObject(
                "Sickness Text",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(patient.transform, false);
            TMP_Text text = textObject.GetComponent<TMP_Text>();
            SetField(patient, "sicknessText", text);

            patient.Activate(4, 5f, "Sickness: [Placeholder]", head, 0f);
            yield return null;

            Assert.That(text.text, Is.EqualTo("Sickness: [Placeholder] (Severity 4)"));

            Object.Destroy(head.gameObject);
            Object.Destroy(patient.gameObject);
        }

        [Test]
        public void BedCooldownBlocksPatientUntilItExpires()
        {
            GameObject bedObject = new GameObject("Bed");
            PatientBedSlot bed = bedObject.AddComponent<PatientBedSlot>();

            InvokeNonPublic(bed, "BeginCooldown", 5f);
            Assert.That(bed.CanAcceptPatient, Is.False);
            Assert.That(bed.RemainingCooldown, Is.EqualTo(5f));

            Assert.That(InvokeNonPublic(bed, "TickCooldown", 4.9f), Is.False);
            Assert.That(bed.CanAcceptPatient, Is.False);
            Assert.That(InvokeNonPublic(bed, "TickCooldown", 0.1f), Is.True);
            Assert.That(bed.CanAcceptPatient, Is.True);

            Object.DestroyImmediate(bedObject);
        }

        [UnityTest]
        public IEnumerator CuredPatientReusesBedOnlyAfterCooldown()
        {
            PatientActor template = CreateInteractivePatient(out _, out Transform head);
            template.name = "Patient Template";

            GameObject bedObject = new GameObject("Bed");
            PatientBedSlot bed = bedObject.AddComponent<PatientBedSlot>();

            GameObject managerObject = new GameObject("Manager");
            managerObject.SetActive(false);
            ClinicGameManager manager = managerObject.AddComponent<ClinicGameManager>();
            SetField(manager, "patientPrefab", template);
            SetField(manager, "playerHead", head);
            SetField(manager, "bedSlots", new[] { bed });
            SetField(manager, "initialPreparationDelay", 0f);
            SetField(manager, "roundDuration", 1f);
            SetField(manager, "patientFadeDuration", 0f);
            SetField(manager, "resolvedBedCooldown", 0.2f);
            SetField(manager, "startAutomatically", false);
            managerObject.SetActive(true);

            manager.StartRound();
            PatientActor pooledPatient = bed.Patient;
            yield return null;

            Button cureButton = (Button)GetField(pooledPatient, "cureButton");
            Assert.That(pooledPatient.State, Is.EqualTo(PatientActorState.Active));
            int expectedCureScore = pooledPatient.Severity == ClinicRoundRules.MaximumSeverity
                ? pooledPatient.Severity * 2
                : pooledPatient.Severity;
            cureButton.onClick.Invoke();
            Assert.That(manager.Score, Is.EqualTo(expectedCureScore));
            yield return null;

            Assert.That(bed.Patient, Is.SameAs(pooledPatient));
            Assert.That(bed.IsOccupied, Is.False);
            Assert.That(bed.RemainingCooldown, Is.GreaterThan(0f));

            yield return new WaitForSeconds(0.05f);
            Assert.That(bed.IsOccupied, Is.False);

            yield return new WaitForSeconds(0.2f);
            Assert.That(bed.Patient, Is.SameAs(pooledPatient));
            Assert.That(bed.IsOccupied, Is.True);

            Object.Destroy(managerObject);
            Object.Destroy(template.gameObject);
            Object.Destroy(bedObject);
            Object.Destroy(head.gameObject);
        }

        [UnityTest]
        public IEnumerator TimedOutPatientReusesBedOnlyAfterCooldown()
        {
            PatientActor template = CreateInteractivePatient(out _, out Transform head);
            template.name = "Patient Template";

            GameObject bedObject = new GameObject("Bed");
            PatientBedSlot bed = bedObject.AddComponent<PatientBedSlot>();

            GameObject managerObject = new GameObject("Manager");
            managerObject.SetActive(false);
            ClinicGameManager manager = managerObject.AddComponent<ClinicGameManager>();
            SetField(manager, "patientPrefab", template);
            SetField(manager, "playerHead", head);
            SetField(manager, "bedSlots", new[] { bed });
            SetField(manager, "initialPreparationDelay", 0f);
            SetField(manager, "roundDuration", 1f);
            SetField(manager, "patientFadeDuration", 0f);
            SetField(manager, "resolvedBedCooldown", 0.2f);
            SetField(manager, "startAutomatically", false);
            managerObject.SetActive(true);

            manager.StartRound();
            PatientActor pooledPatient = bed.Patient;
            yield return null;

            Assert.That(pooledPatient.State, Is.EqualTo(PatientActorState.Active));
            InvokeNonPublic(pooledPatient, "Resolve", PatientResolution.TimedOut);
            yield return null;

            Assert.That(bed.Patient, Is.SameAs(pooledPatient));
            Assert.That(bed.IsOccupied, Is.False);
            Assert.That(bed.RemainingCooldown, Is.GreaterThan(0f));

            yield return new WaitForSeconds(0.05f);
            Assert.That(bed.IsOccupied, Is.False);

            yield return new WaitForSeconds(0.2f);
            Assert.That(bed.Patient, Is.SameAs(pooledPatient));
            Assert.That(bed.IsOccupied, Is.True);

            Object.Destroy(managerObject);
            Object.Destroy(template.gameObject);
            Object.Destroy(bedObject);
            Object.Destroy(head.gameObject);
        }

        private static PatientActor CreateInteractivePatient(out Button button, out Transform head)
        {
            GameObject root = new GameObject("npc");
            root.SetActive(false);

            Transform pivot = new GameObject("Cure Orbit Pivot").transform;
            pivot.SetParent(root.transform, false);

            GameObject canvasObject = new GameObject("Cure", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(pivot, false);

            GameObject buttonObject = new GameObject(
                "Button",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(CanvasGroup));
            buttonObject.transform.SetParent(canvasObject.transform, false);
            button = buttonObject.GetComponent<Button>();

            PatientActor patient = root.AddComponent<PatientActor>();
            MeshRenderer bodyRenderer = root.AddComponent<MeshRenderer>();
            SetField(patient, "cureButton", button);
            SetField(patient, "cureCanvasGroup", buttonObject.GetComponent<CanvasGroup>());
            SetField(patient, "cureOrbitPivot", pivot);
            SetField(patient, "cureCanvas", canvasObject.transform);
            SetField(patient, "fadeRenderers", new Renderer[] { bodyRenderer });
            SetField(patient, "patientCanvases", new[] { canvasObject.GetComponent<Canvas>() });
            SetField(patient, "proximityFadeDuration", 0f);

            head = new GameObject("Head").transform;
            head.position = Vector3.forward;
            return patient;
        }

        private static ClinicGameManager CreateManagerForHudTest(
            float preparationDuration,
            float roundDuration,
            out GameObject managerObject,
            out GameObject templateObject,
            out GameObject bedObject,
            out GameObject headObject,
            bool startAutomatically = false,
            float automaticStartReadinessTimeout = 10f,
            int automaticStartStableFrames = 2)
        {
            templateObject = new GameObject("Patient Template");
            templateObject.SetActive(false);
            PatientActor template = templateObject.AddComponent<PatientActor>();

            bedObject = new GameObject("Bed");
            PatientBedSlot bed = bedObject.AddComponent<PatientBedSlot>();

            headObject = new GameObject("Head");

            managerObject = new GameObject("Manager");
            managerObject.SetActive(false);
            ClinicGameManager manager = managerObject.AddComponent<ClinicGameManager>();
            SetField(manager, "patientPrefab", template);
            SetField(manager, "playerHead", headObject.transform);
            SetField(manager, "bedSlots", new[] { bed });
            SetField(manager, "initialPreparationDelay", preparationDuration);
            SetField(manager, "roundDuration", roundDuration);
            SetField(manager, "patientFadeDuration", 0f);
            SetField(manager, "startAutomatically", startAutomatically);
            SetField(manager, "automaticStartReadinessTimeout", automaticStartReadinessTimeout);
            SetField(manager, "automaticStartStableFrames", automaticStartStableFrames);
            managerObject.SetActive(true);
            return manager;
        }

        private static ClinicRoundHud CreateHud(
            ClinicGameManager manager,
            GameObject locomotion,
            float fadeDuration,
            out TMP_Text roundTimer,
            out TMP_Text preparationTimer,
            out TMP_Text totalScore,
            out CanvasGroup overlay)
        {
            GameObject hudObject = new GameObject("HUD");
            hudObject.SetActive(false);

            GameObject roundTimerRoot = new GameObject(
                "Round Timer Panel",
                typeof(RectTransform));
            roundTimerRoot.transform.SetParent(hudObject.transform, false);

            GameObject roundTimerObject = new GameObject(
                "Round Timer",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            roundTimerObject.transform.SetParent(roundTimerRoot.transform, false);
            roundTimer = roundTimerObject.GetComponent<TMP_Text>();

            GameObject overlayObject = new GameObject(
                "Preparation Overlay",
                typeof(RectTransform),
                typeof(CanvasGroup));
            overlayObject.transform.SetParent(hudObject.transform, false);
            overlay = overlayObject.GetComponent<CanvasGroup>();

            GameObject preparationTimerObject = new GameObject(
                "Preparation Timer",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            preparationTimerObject.transform.SetParent(overlayObject.transform, false);
            preparationTimer = preparationTimerObject.GetComponent<TMP_Text>();

            GameObject totalScoreObject = new GameObject(
                "Total Score",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            totalScoreObject.transform.SetParent(overlayObject.transform, false);
            totalScore = totalScoreObject.GetComponent<TMP_Text>();

            ClinicRoundHud hud = hudObject.AddComponent<ClinicRoundHud>();
            SetField(hud, "gameManager", manager);
            SetField(hud, "locomotionRoot", locomotion);
            SetField(hud, "roundTimerRoot", roundTimerRoot);
            SetField(hud, "roundTimerText", roundTimer);
            SetField(hud, "preparationCountdownText", preparationTimer);
            SetField(hud, "totalScoreText", totalScore);
            SetField(hud, "preparationOverlay", overlay);
            SetField(hud, "preparationFadeDuration", fadeDuration);
            hudObject.SetActive(true);
            return hud;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {name}");
            field.SetValue(target, value);
        }

        private static object GetField(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {name}");
            return field.GetValue(target);
        }

        private static object InvokeNonPublic(object target, string name, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing method {name}");
            return method.Invoke(target, arguments);
        }
    }
}
