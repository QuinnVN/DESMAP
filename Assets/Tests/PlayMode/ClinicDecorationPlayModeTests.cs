using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NCKH.VR.Clinic.Tests
{
    public sealed class ClinicDecorationPlayModeTests
    {
        [UnityTest]
        public IEnumerator StandardPatientShowsGlucoseStandAtBedAnchor()
        {
            ClinicGameManager manager = CreateManager(
                new SicknessEntry("Common Cold", 2, 1f),
                0f,
                0.5f,
                out TestObjects objects);

            objects.Bed.transform.rotation = Quaternion.Euler(5f, 270f, 2f);
            manager.StartRound();
            yield return null;

            GameObject decoration = objects.Bed.ActiveDecoration;
            Assert.That(decoration, Is.Not.Null);
            Assert.That(decoration.name, Is.EqualTo(objects.GlucosePrefab.name));
            Assert.That(Vector3.Distance(decoration.transform.position, objects.Anchor.position), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(decoration.transform.rotation, objects.Bed.transform.rotation), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(decoration.transform.localScale, Vector3.one * 1.41f), Is.LessThan(0.0001f));

            objects.Destroy();
        }

        [UnityTest]
        public IEnumerator SeverityFourPatientShowsEkgAtBedAnchor()
        {
            ClinicGameManager manager = CreateManager(
                new SicknessEntry("Cardiac Arrest", ClinicRoundRules.MaximumSeverity, 1f),
                0f,
                0.5f,
                out TestObjects objects);

            objects.Bed.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            manager.StartRound();
            yield return null;

            GameObject decoration = objects.Bed.ActiveDecoration;
            Assert.That(decoration, Is.Not.Null);
            Assert.That(decoration.name, Is.EqualTo(objects.EkgPrefab.name));
            Assert.That(Vector3.Distance(decoration.transform.position, objects.Anchor.position), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(decoration.transform.rotation, objects.Bed.transform.rotation), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(decoration.transform.localScale, Vector3.one * 1.15f), Is.LessThan(0.0001f));

            objects.Destroy();
        }

        [UnityTest]
        public IEnumerator DecorationStaysThroughFadeThenHidesAndIsReused()
        {
            ClinicGameManager manager = CreateManager(
                new SicknessEntry("Common Cold", 1, 1f),
                0.05f,
                0.2f,
                out TestObjects objects);
            SetField(manager, "roundDuration", 3f);

            manager.StartRound();
            yield return new WaitForSeconds(0.06f);

            GameObject firstDecoration = objects.Bed.ActiveDecoration;
            Assert.That(firstDecoration, Is.Not.Null);
            Assert.That(objects.Bed.Patient.State, Is.EqualTo(PatientActorState.Active));

            InvokeNonPublic(objects.Bed.Patient, "Resolve", PatientResolution.Cured);
            Assert.That(objects.Bed.Patient.State, Is.EqualTo(PatientActorState.FadingOut));
            Assert.That(objects.Bed.ActiveDecoration, Is.SameAs(firstDecoration));
            Assert.That(firstDecoration.activeSelf, Is.True);

            yield return new WaitForSeconds(0.06f);

            Assert.That(objects.Bed.Patient.State, Is.EqualTo(PatientActorState.Inactive));
            Assert.That(objects.Bed.ActiveDecoration, Is.Null);
            Assert.That(firstDecoration.activeSelf, Is.False);

            yield return new WaitForSeconds(0.16f);

            Assert.That(objects.Bed.ActiveDecoration, Is.SameAs(firstDecoration));
            Assert.That(firstDecoration.activeSelf, Is.True);

            objects.Destroy();
        }

        [UnityTest]
        public IEnumerator MissingDecorationReferencesAreSafeNoOp()
        {
            ClinicGameManager manager = CreateManager(
                new SicknessEntry("Common Cold", 1, 1f),
                0f,
                0.5f,
                out TestObjects objects);
            SetField(manager, "glucoseStandPrefab", null);
            SetField(manager, "ekgMachinePrefab", null);

            manager.StartRound();
            yield return null;

            Assert.That(objects.Bed.IsOccupied, Is.True);
            Assert.That(objects.Bed.ActiveDecoration, Is.Null);

            objects.Destroy();
        }

        private static ClinicGameManager CreateManager(
            SicknessEntry sickness,
            float fadeDuration,
            float cooldown,
            out TestObjects objects)
        {
            objects = new TestObjects();

            objects.PatientTemplate = new GameObject("Patient Template");
            objects.PatientTemplate.SetActive(false);
            PatientActor patientPrefab = objects.PatientTemplate.AddComponent<PatientActor>();

            objects.BedObject = new GameObject("Bed");
            objects.Bed = objects.BedObject.AddComponent<PatientBedSlot>();
            objects.Anchor = new GameObject("Decoration Anchor").transform;
            objects.Anchor.SetParent(objects.Bed.transform, false);
            objects.Anchor.position = new Vector3(3f, 0.25f, -4f);
            SetField(objects.Bed, "decorationAnchor", objects.Anchor);

            objects.HeadObject = new GameObject("Head");
            objects.DecorationRoot = new GameObject("Deco");

            objects.GlucosePrefab = new GameObject("Glucose Test Prefab");
            objects.GlucosePrefab.transform.localScale = Vector3.one * 2f;
            objects.GlucosePrefab.SetActive(false);
            objects.EkgPrefab = new GameObject("EKG Test Prefab");
            objects.EkgPrefab.transform.localScale = Vector3.one * 3f;
            objects.EkgPrefab.SetActive(false);

            objects.ManagerObject = new GameObject("Manager");
            objects.ManagerObject.SetActive(false);
            ClinicGameManager manager = objects.ManagerObject.AddComponent<ClinicGameManager>();
            SicknessDatabase database = objects.ManagerObject.AddComponent<SicknessDatabase>();
            SetField(database, "entries", new List<SicknessEntry> { sickness });
            SetField(manager, "patientPrefab", patientPrefab);
            SetField(manager, "playerHead", objects.HeadObject.transform);
            SetField(manager, "bedSlots", new[] { objects.Bed });
            SetField(manager, "sicknessDatabase", database);
            SetField(manager, "glucoseStandPrefab", objects.GlucosePrefab);
            SetField(manager, "ekgMachinePrefab", objects.EkgPrefab);
            SetField(manager, "decorationRoot", objects.DecorationRoot.transform);
            SetField(manager, "glucoseStandScale", 1.41f);
            SetField(manager, "ekgMachineScale", 1.15f);
            SetField(manager, "initialPreparationDelay", 0f);
            SetField(manager, "roundDuration", 1f);
            SetField(manager, "standardTreatmentDuration", 30f);
            SetField(manager, "emergencyTreatmentDuration", 30f);
            SetField(manager, "patientFadeDuration", fadeDuration);
            SetField(manager, "resolvedBedCooldown", cooldown);
            SetField(manager, "guaranteeEmergencyEachRound", false);
            SetField(manager, "startAutomatically", false);
            objects.ManagerObject.SetActive(true);
            return manager;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private static void InvokeNonPublic(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Expected method '{methodName}' on {target.GetType().Name}.");
            method.Invoke(target, arguments);
        }

        private sealed class TestObjects
        {
            public GameObject ManagerObject;
            public GameObject PatientTemplate;
            public GameObject BedObject;
            public PatientBedSlot Bed;
            public Transform Anchor;
            public GameObject HeadObject;
            public GameObject DecorationRoot;
            public GameObject GlucosePrefab;
            public GameObject EkgPrefab;

            public void Destroy()
            {
                Object.Destroy(ManagerObject);
                Object.Destroy(PatientTemplate);
                Object.Destroy(BedObject);
                Object.Destroy(HeadObject);
                Object.Destroy(DecorationRoot);
                Object.Destroy(GlucosePrefab);
                Object.Destroy(EkgPrefab);
            }
        }
    }
}
