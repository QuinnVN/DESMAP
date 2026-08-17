using System.Collections;
using System.Collections.Generic;
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
            SetSicknessEntries(manager, new SicknessEntry("Common Cold", 1, 1f));
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
            Assert.That(bed.Patient.SicknessName, Is.EqualTo("Common Cold"));
            Assert.That(bed.Patient.Severity, Is.EqualTo(1));

            Object.Destroy(managerObject);
            Object.Destroy(templateObject);
            Object.Destroy(bedObject);
            Object.Destroy(headObject);
        }

        [UnityTest]
        public IEnumerator EmergencyGuaranteeForcesNextSpawnAfterCheckpoint()
        {
            ClinicGameManager manager = CreateEmergencyGuaranteeManager(
                out GameObject managerObject,
                out GameObject templateObject,
                out GameObject bedObject,
                out GameObject headObject);
            PatientBedSlot bed = bedObject.GetComponent<PatientBedSlot>();

            SetSicknessEntries(manager, new SicknessEntry("Common Cold", 1, 1f));
            SetField(manager, "emergencyGuaranteeCheckpoint", 0.05f);
            manager.StartRound();
            yield return null;

            Assert.That(bed.Patient.Severity, Is.EqualTo(1));

            yield return new WaitForSeconds(0.06f);
            SetSicknessEntries(manager, new SicknessEntry("Cardiac Arrest", 4, 1f));
            InvokeNonPublic(bed.Patient, "Resolve", PatientResolution.Cured);
            yield return null;

            Assert.That(bed.Patient.Severity, Is.EqualTo(ClinicRoundRules.MaximumSeverity));
            Assert.That(bed.Patient.SicknessName, Is.EqualTo("Cardiac Arrest"));

            DestroyEmergencyGuaranteeObjects(managerObject, templateObject, bedObject, headObject);
        }

        [UnityTest]
        public IEnumerator EmptyDatabaseSkipsSpawnAndWarns()
        {
            ClinicGameManager manager = CreateEmergencyGuaranteeManager(
                out GameObject managerObject,
                out GameObject templateObject,
                out GameObject bedObject,
                out GameObject headObject);
            PatientBedSlot bed = bedObject.GetComponent<PatientBedSlot>();
            SetField(manager, "guaranteeEmergencyEachRound", false);
            SetSicknessEntries(manager);

            LogAssert.Expect(
                LogType.Warning,
                "ClinicGameManager requires a Sickness Database containing at least one named entry "
                + "with a positive weight. The patient spawn was skipped.");
            manager.StartRound();
            yield return null;

            Assert.That(bed.IsOccupied, Is.False);
            DestroyEmergencyGuaranteeObjects(managerObject, templateObject, bedObject, headObject);
        }

        [UnityTest]
        public IEnumerator MissingSeverityFourEntrySkipsForcedEmergencyAndWarns()
        {
            ClinicGameManager manager = CreateEmergencyGuaranteeManager(
                out GameObject managerObject,
                out GameObject templateObject,
                out GameObject bedObject,
                out GameObject headObject);
            PatientBedSlot bed = bedObject.GetComponent<PatientBedSlot>();
            SetField(manager, "emergencyGuaranteeCheckpoint", 0f);
            SetSicknessEntries(manager, new SicknessEntry("Common Cold", 1, 1f));

            LogAssert.Expect(
                LogType.Warning,
                "Sickness Database has no valid severity 4 entry with a positive weight. "
                + "The forced emergency spawn was skipped.");
            manager.StartRound();
            yield return null;

            Assert.That(bed.IsOccupied, Is.False);
            DestroyEmergencyGuaranteeObjects(managerObject, templateObject, bedObject, headObject);
        }

        [UnityTest]
        public IEnumerator NaturalEmergencyBeforeCheckpointPreventsForcedEmergency()
        {
            ClinicGameManager manager = CreateEmergencyGuaranteeManager(
                out GameObject managerObject,
                out GameObject templateObject,
                out GameObject bedObject,
                out GameObject headObject);
            PatientBedSlot bed = bedObject.GetComponent<PatientBedSlot>();

            SetSicknessEntries(manager, new SicknessEntry("Cardiac Arrest", 4, 1f));
            SetField(manager, "emergencyGuaranteeCheckpoint", 0.05f);
            manager.StartRound();
            yield return null;

            Assert.That(bed.Patient.Severity, Is.EqualTo(ClinicRoundRules.MaximumSeverity));

            SetSicknessEntries(manager, new SicknessEntry("Common Cold", 1, 1f));
            yield return new WaitForSeconds(0.06f);
            InvokeNonPublic(bed.Patient, "Resolve", PatientResolution.Cured);
            yield return null;

            Assert.That(bed.Patient.Severity, Is.EqualTo(1));

            DestroyEmergencyGuaranteeObjects(managerObject, templateObject, bedObject, headObject);
        }

        [UnityTest]
        public IEnumerator EmergencyGuaranteeResetsForEachRound()
        {
            ClinicGameManager manager = CreateEmergencyGuaranteeManager(
                out GameObject managerObject,
                out GameObject templateObject,
                out GameObject bedObject,
                out GameObject headObject);
            PatientBedSlot bed = bedObject.GetComponent<PatientBedSlot>();

            SetSicknessEntries(manager, new SicknessEntry("Cardiac Arrest", 4, 1f));
            SetField(manager, "emergencyGuaranteeCheckpoint", 0f);
            manager.StartRound();
            yield return null;
            Assert.That(bed.Patient.Severity, Is.EqualTo(ClinicRoundRules.MaximumSeverity));

            manager.StopRound();
            SetSicknessEntries(manager, new SicknessEntry("Stroke", 4, 1f));
            manager.StartRound();
            yield return null;

            Assert.That(bed.Patient.Severity, Is.EqualTo(ClinicRoundRules.MaximumSeverity));

            DestroyEmergencyGuaranteeObjects(managerObject, templateObject, bedObject, headObject);
        }

        [UnityTest]
        public IEnumerator DisabledEmergencyGuaranteeUsesConfiguredWeights()
        {
            ClinicGameManager manager = CreateEmergencyGuaranteeManager(
                out GameObject managerObject,
                out GameObject templateObject,
                out GameObject bedObject,
                out GameObject headObject);
            PatientBedSlot bed = bedObject.GetComponent<PatientBedSlot>();

            SetSicknessEntries(manager, new SicknessEntry("Common Cold", 1, 1f));
            SetField(manager, "guaranteeEmergencyEachRound", false);
            SetField(manager, "emergencyGuaranteeCheckpoint", 0f);
            manager.StartRound();
            yield return null;

            Assert.That(bed.Patient.Severity, Is.EqualTo(1));

            DestroyEmergencyGuaranteeObjects(managerObject, templateObject, bedObject, headObject);
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
            Assert.That(totalScore.text, Is.EqualTo("TỔNG ĐIỂM\n42"));
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
            Assert.That(totalScore.text, Is.EqualTo("TỔNG ĐIỂM\n0"));

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

            patient.Activate("Common Cold", 1, 30f, head, 0f, false);
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

            patient.Activate("Common Cold", 1, 30f, head, 0f, false);
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
        public IEnumerator SicknessWindowBillboardsWithYawOnlyAndKeepsItsPosition()
        {
            PatientActor patient = CreateInteractivePatient(out _, out Transform head);
            GameObject chatObject = new GameObject(
                "Chat box",
                typeof(RectTransform),
                typeof(CanvasGroup));
            chatObject.transform.SetParent(patient.transform, false);
            chatObject.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            CanvasGroup chatGroup = chatObject.GetComponent<CanvasGroup>();
            SetField(patient, "chatCanvasGroup", chatGroup);

            Vector3 initialPosition = chatObject.transform.position;
            head.position = new Vector3(2f, 8f, 3f);

            patient.Activate("Common Cold", 1, 30f, head, 0f, false);
            yield return null;

            Quaternion firstRotation = ExpectedCanvasRotation(patient.transform.position, head.position);
            Assert.That(Quaternion.Angle(chatObject.transform.rotation, firstRotation), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(chatObject.transform.position, initialPosition), Is.LessThan(0.001f));
            Assert.That(Mathf.Abs(Vector3.Dot(chatObject.transform.forward, Vector3.up)), Is.LessThan(0.001f));
            Assert.That(Vector3.Dot(chatObject.transform.up, Vector3.up), Is.GreaterThan(0.999f));

            head.position = new Vector3(2f, -5f, 3f);
            yield return null;

            Assert.That(Quaternion.Angle(chatObject.transform.rotation, firstRotation), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(chatObject.transform.position, initialPosition), Is.LessThan(0.001f));

            head.position = new Vector3(-4f, 12f, 1f);
            yield return null;

            Quaternion secondRotation = ExpectedCanvasRotation(patient.transform.position, head.position);
            Assert.That(Quaternion.Angle(chatObject.transform.rotation, secondRotation), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(chatObject.transform.position, initialPosition), Is.LessThan(0.001f));
            Assert.That(Mathf.Abs(Vector3.Dot(chatObject.transform.forward, Vector3.up)), Is.LessThan(0.001f));
            Assert.That(Vector3.Dot(chatObject.transform.up, Vector3.up), Is.GreaterThan(0.999f));

            Object.Destroy(head.gameObject);
            Object.Destroy(patient.gameObject);
        }

        [UnityTest]
        public IEnumerator PatientLabelRespectsDebugMode()
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

            patient.Activate("Cardiac Arrest", 4, 5f, head, 0f, false);
            yield return null;

            Assert.That(patient.SicknessName, Is.EqualTo("Cardiac Arrest"));
            Assert.That(text.text, Is.EqualTo("Bệnh: Cardiac Arrest"));

            patient.Activate("Cardiac Arrest", 4, 5f, head, 0f, true);
            Assert.That(text.text, Is.EqualTo("Bệnh: Cardiac Arrest (Mức độ 4)"));

            Object.Destroy(head.gameObject);
            Object.Destroy(patient.gameObject);
        }

        [UnityTest]
        public IEnumerator SeverityFourStartsDirectionalAudioAndFlashesRed()
        {
            PatientActor patient = CreateInteractivePatient(out _, out Transform head);
            AudioClip clip = AudioClip.Create("Dying SFX Test", 4410, 1, 44100, false);
            SetField(patient, "dyingSfx", clip);
            SetField(patient, "emergencyFlashInterval", 0.05f);

            patient.Activate("Cardiac Arrest", ClinicRoundRules.MaximumSeverity, 5f, head, 0f, false);
            yield return null;

            AudioSource source = patient.GetComponent<AudioSource>();
            Assert.That(source, Is.Not.Null);
            Assert.That(source.clip, Is.SameAs(clip));
            Assert.That(source.loop, Is.True);
            Assert.That(source.spatialBlend, Is.EqualTo(1f));
            Assert.That(source.dopplerLevel, Is.EqualTo(0f));
            Assert.That(source.minDistance, Is.EqualTo(1f));
            Assert.That(source.maxDistance, Is.EqualTo(15f));

            Renderer renderer = ((Renderer[])GetField(patient, "fadeRenderers"))[0];
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            Assert.That(propertyBlock.GetColor("_BaseColor"), Is.EqualTo(Color.red));

            InvokeNonPublic(patient, "UpdateEmergencyFlash", 0.05f);
            InvokeNonPublic(patient, "ApplyVisuals");
            renderer.GetPropertyBlock(propertyBlock);
            Assert.That(propertyBlock.GetColor("_BaseColor"), Is.EqualTo(Color.white));

            patient.DeactivateImmediately();
            Assert.That(source.isPlaying, Is.False);

            Object.Destroy(clip);
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
            SetSicknessEntries(manager, new SicknessEntry("Common Cold", 1, 1f));
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
            SetSicknessEntries(manager, new SicknessEntry("Common Cold", 1, 1f));
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

        private static Quaternion ExpectedCanvasRotation(Vector3 patientPosition, Vector3 headPosition)
        {
            Vector3 flatDirection = headPosition - patientPosition;
            flatDirection.y = 0f;
            return Quaternion.LookRotation(flatDirection.normalized, Vector3.up)
                * Quaternion.Euler(0f, 180f, 0f);
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
            SetSicknessEntries(manager, new SicknessEntry("Common Cold", 1, 1f));
            SetField(manager, "initialPreparationDelay", preparationDuration);
            SetField(manager, "roundDuration", roundDuration);
            SetField(manager, "patientFadeDuration", 0f);
            SetField(manager, "startAutomatically", startAutomatically);
            SetField(manager, "automaticStartReadinessTimeout", automaticStartReadinessTimeout);
            SetField(manager, "automaticStartStableFrames", automaticStartStableFrames);
            managerObject.SetActive(true);
            return manager;
        }

        private static ClinicGameManager CreateEmergencyGuaranteeManager(
            out GameObject managerObject,
            out GameObject templateObject,
            out GameObject bedObject,
            out GameObject headObject)
        {
            ClinicGameManager manager = CreateManagerForHudTest(
                0f,
                1f,
                out managerObject,
                out templateObject,
                out bedObject,
                out headObject);
            SetField(manager, "patientFadeDuration", 0f);
            SetField(manager, "resolvedBedCooldown", 0f);
            SetField(manager, "guaranteeEmergencyEachRound", true);
            return manager;
        }

        private static void SetSicknessEntries(
            ClinicGameManager manager,
            params SicknessEntry[] entries)
        {
            SicknessDatabase database = (SicknessDatabase)GetField(manager, "sicknessDatabase");
            if (database == null)
            {
                database = manager.gameObject.AddComponent<SicknessDatabase>();
                SetField(manager, "sicknessDatabase", database);
            }

            SetField(database, "entries", new List<SicknessEntry>(entries));
        }

        private static void DestroyEmergencyGuaranteeObjects(
            GameObject managerObject,
            GameObject templateObject,
            GameObject bedObject,
            GameObject headObject)
        {
            Object.Destroy(managerObject);
            Object.Destroy(templateObject);
            Object.Destroy(bedObject);
            Object.Destroy(headObject);
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
