using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NCKH.VR.Clinic.Tests
{
    public sealed class PatientAppearancePlayModeTests
    {
        [UnityTest]
        public IEnumerator GameOneSpawnsRebindsAndAnimatesHumanoidPatients()
        {
            bool previousIgnore = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            try
            {
                yield return SceneManager.LoadSceneAsync("Game 1", LoadSceneMode.Single);
                yield return null;

                ClinicGameManager manager = Object.FindFirstObjectByType<ClinicGameManager>();
                Assert.That(manager, Is.Not.Null);
                SetField(manager, "initialPreparationDelay", 0f);
                SetField(manager, "roundDuration", 30f);
                SetField(manager, "arrivalInterval", 30f);
                SetField(manager, "patientFadeDuration", 0f);
                manager.StartRound();
                yield return null;
                yield return null;

                PatientBedSlot[] beds = Object.FindObjectsByType<PatientBedSlot>(FindObjectsSortMode.None);
                PatientActor activePatient = null;
                PatientBedSlot occupiedBed = null;
                for (int i = 0; i < beds.Length; i++)
                {
                    if (!beds[i].IsOccupied)
                        continue;

                    activePatient = beds[i].Patient;
                    occupiedBed = beds[i];
                    break;
                }

                Assert.That(activePatient, Is.Not.Null);
                Assert.That(occupiedBed, Is.Not.Null);
                activePatient.Activate("Stable", ClinicRoundRules.MinimumSeverity, 5f, null, 0f, false);
                yield return null;
                PatientAppearanceController appearance = activePatient.GetComponent<PatientAppearanceController>();
                Assert.That(appearance, Is.Not.Null);
                Assert.That(appearance.CurrentCharacterPrefab, Is.Not.Null);
                Assert.That(appearance.CurrentHairstylePrefab, Is.Not.Null);
                Assert.That(appearance.ActiveRenderers.Count, Is.GreaterThan(0));
                Assert.That(appearance.CurrentAnimator, Is.Not.Null);
                Assert.That(appearance.CurrentAnimator.runtimeAnimatorController.name, Is.EqualTo("Patient Lying"));
                Assert.That(appearance.CurrentAnimator.GetBool("Emergency"), Is.False,
                    "The deterministic normal patient should keep the Emergency parameter false.");
                Assert.That(appearance.CurrentAnimator.enabled, Is.False,
                    "The sampled patient pose should have its Animator disabled.");
                Assert.That(appearance.CurrentAnimator.speed, Is.Zero);

                Transform hips = appearance.CurrentAnimator.GetBoneTransform(HumanBodyBones.Hips);
                Quaternion frozenHipsRotation = hips.rotation;
                yield return null;
                yield return null;
                Assert.That(Quaternion.Angle(frozenHipsRotation, hips.rotation), Is.LessThan(0.001f));

                Transform characterRoot = appearance.CurrentAnimator.transform;
                SkinnedMeshRenderer attachedHair = FindAttachmentRenderer(characterRoot.parent, "Patient Hairstyle");
                Assert.That(attachedHair, Is.Not.Null);
                for (int i = 0; i < attachedHair.bones.Length; i++)
                    Assert.That(attachedHair.bones[i].IsChildOf(characterRoot), Is.True);

                float mattressDistance = Vector3.Distance(characterRoot.position, occupiedBed.PatientAnchor.position);
                Assert.That(mattressDistance, Is.LessThan(1f));
                Assert.That(Quaternion.Angle(characterRoot.rotation, occupiedBed.PatientAnchor.rotation),
                    Is.GreaterThan(45f), "Only the visual should be rotated into a supine pose.");

                Animator previousAnimator = appearance.CurrentAnimator;
                Assert.That(appearance.RandomizeAppearance(true), Is.True);
                yield return null;
                Assert.That(appearance.CurrentAnimator, Is.Not.SameAs(previousAnimator));
                Assert.That(appearance.CurrentAnimator.GetBool("Emergency"), Is.True);
                Assert.That(appearance.CurrentAnimator.enabled, Is.False,
                    "Emergency patients must use the same frozen pose.");
                Assert.That(appearance.CurrentAnimator.speed, Is.Zero);
                Assert.That(appearance.CurrentFacialHairPrefab == null || appearance.CurrentGender == PatientGender.Male,
                    Is.True);

                HashSet<PatientGender> sampledGenders = new HashSet<PatientGender>();
                for (int attempt = 0; attempt < 16 && sampledGenders.Count < 2; attempt++)
                {
                    Assert.That(appearance.RandomizeAppearance(false), Is.True);
                    sampledGenders.Add(appearance.CurrentGender);
                    Assert.That(appearance.CurrentAnimator.enabled, Is.False);
                    Assert.That(appearance.CurrentAnimator.speed, Is.Zero);
                    yield return null;
                }
                Assert.That(sampledGenders, Is.EquivalentTo(new[] { PatientGender.Female, PatientGender.Male }));

                activePatient.Activate(
                    "Cardiac Arrest",
                    ClinicRoundRules.MaximumSeverity,
                    5f,
                    null,
                    0f,
                    false);
                yield return null;
                appearance = activePatient.GetComponent<PatientAppearanceController>();
                AssertRendererColors(appearance.ActiveRenderers, Color.red, true);

                SetField(activePatient, "enableEmergencyAlert", false);
                yield return null;
                Assert.That(activePatient.EmergencyAlertEnabled, Is.False,
                    "The runtime Inspector toggle should disable the alert.");
                AssertRendererColors(appearance.ActiveRenderers, Color.clear, false);
                AudioSource alertAudio = activePatient.GetComponent<AudioSource>();
                Assert.That(alertAudio == null || !alertAudio.isPlaying, Is.True);

                SetField(activePatient, "enableEmergencyAlert", true);
                activePatient.Activate("Stable", ClinicRoundRules.MinimumSeverity, 5f, null, 0f, false);
                yield return null;
                appearance = activePatient.GetComponent<PatientAppearanceController>();
                AssertRendererColors(appearance.ActiveRenderers, Color.clear, false);

                manager.StopRound();
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousIgnore;
            }
        }

        [UnityTest]
        public IEnumerator CuredAppearanceFadesAndCannotOverlapSameFrameReplacement()
        {
            bool previousIgnore = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            try
            {
                yield return SceneManager.LoadSceneAsync("Game 1", LoadSceneMode.Single);
                yield return null;

                ClinicGameManager manager = Object.FindFirstObjectByType<ClinicGameManager>();
                Assert.That(manager, Is.Not.Null);
                SetField(manager, "initialPreparationDelay", 0f);
                SetField(manager, "roundDuration", 30f);
                SetField(manager, "arrivalInterval", 30f);
                SetField(manager, "patientFadeDuration", 0.1f);
                manager.StartRound();
                yield return null;
                yield return null;

                PatientBedSlot[] beds = Object.FindObjectsByType<PatientBedSlot>(FindObjectsSortMode.None);
                PatientActor patient = null;
                for (int i = 0; i < beds.Length; i++)
                {
                    if (beds[i].IsOccupied)
                    {
                        patient = beds[i].Patient;
                        break;
                    }
                }

                Assert.That(patient, Is.Not.Null);
                patient.Activate("Stable", ClinicRoundRules.MinimumSeverity, 5f, null, 0.1f, false);
                PatientAppearanceController appearance = patient.GetComponent<PatientAppearanceController>();
                Assert.That(appearance, Is.Not.Null);
                Assert.That(appearance.CurrentAnimator, Is.Not.Null);
                Transform appearanceRoot = appearance.CurrentAnimator.transform.parent;
                GameObject curedCharacter = appearance.CurrentAnimator.gameObject;

                AssertRendererSurface(appearance.ActiveRenderers, 1f,
                    "A humanoid must use transparent materials while fading in.");
                for (int frame = 0; frame < 30 && patient.State != PatientActorState.Active; frame++)
                    yield return null;
                Assert.That(patient.State, Is.EqualTo(PatientActorState.Active));
                AssertRendererSurface(appearance.ActiveRenderers, 0f,
                    "A fully visible humanoid must restore its original opaque materials.");

                InvokeNonPublic(patient, "Resolve", PatientResolution.Cured);
                AssertRendererSurface(appearance.ActiveRenderers, 1f,
                    "A cured humanoid must switch to transparent materials before fading out.");

                // Complete and reuse synchronously to cover Unity's deferred Destroy window.
                InvokeNonPublic(patient, "CompleteFadeOut");
                Assert.That(curedCharacter.activeInHierarchy, Is.False,
                    "The cured hierarchy must be hidden before deferred destruction completes.");
                Assert.That(appearance.CurrentAnimator, Is.Null);

                patient.Activate(
                    "Cardiac Arrest",
                    ClinicRoundRules.MaximumSeverity,
                    5f,
                    null,
                    0f,
                    false);

                int activeCharacters = 0;
                for (int i = 0; i < appearanceRoot.childCount; i++)
                {
                    Transform child = appearanceRoot.GetChild(i);
                    if (child.gameObject.activeInHierarchy && child.name == "Patient Character")
                        activeCharacters++;
                }

                Assert.That(activeCharacters, Is.EqualTo(1),
                    "Only the replacement character may remain active during the deferred Destroy frame.");
                yield return null;
                AssertRendererColors(appearance.ActiveRenderers, Color.red, true);
                manager.StopRound();
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousIgnore;
            }
        }

        private static SkinnedMeshRenderer FindAttachmentRenderer(Transform appearanceRoot, string attachmentName)
        {
            Transform attachment = appearanceRoot.Find(attachmentName);
            return attachment != null ? attachment.GetComponentInChildren<SkinnedMeshRenderer>(true) : null;
        }

        private static void AssertRendererColors(
            IReadOnlyList<Renderer> renderers,
            Color expectedFlashColor,
            bool expectFlash)
        {
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            int checkedMaterials = 0;
            for (int rendererIndex = 0; rendererIndex < renderers.Count; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                Material[] materials = renderer.sharedMaterials;
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];
                    if (material == null)
                        continue;

                    bool hasBaseColor = material.HasProperty("_BaseColor");
                    bool hasColor = material.HasProperty("_Color");
                    if (!hasBaseColor && !hasColor)
                        continue;

                    propertyBlock.Clear();
                    renderer.GetPropertyBlock(propertyBlock, materialIndex);
                    Color actual = hasBaseColor
                        ? propertyBlock.GetColor("_BaseColor")
                        : propertyBlock.GetColor("_Color");
                    Color expected = expectFlash
                        ? new Color(expectedFlashColor.r, expectedFlashColor.g, expectedFlashColor.b, actual.a)
                        : hasBaseColor
                            ? material.GetColor("_BaseColor")
                            : material.GetColor("_Color");
                    Assert.That(Vector4.Distance(actual, expected), Is.LessThan(0.001f));
                    checkedMaterials++;
                }
            }

            Assert.That(checkedMaterials, Is.GreaterThan(0));
        }

        private static void AssertRendererSurface(
            IReadOnlyList<Renderer> renderers,
            float expectedSurface,
            string message)
        {
            int checkedMaterials = 0;
            for (int rendererIndex = 0; rendererIndex < renderers.Count; rendererIndex++)
            {
                Material[] materials = renderers[rendererIndex].sharedMaterials;
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];
                    if (material == null || !material.HasProperty("_Surface"))
                        continue;

                    Assert.That(material.GetFloat("_Surface"), Is.EqualTo(expectedSurface).Within(0.001f), message);
                    checkedMaterials++;
                }
            }

            Assert.That(checkedMaterials, Is.GreaterThan(0));
        }

        private static object InvokeNonPublic(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing method {methodName}.");
            return method.Invoke(target, arguments);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName}.");
            field.SetValue(target, value);
        }
    }
}
