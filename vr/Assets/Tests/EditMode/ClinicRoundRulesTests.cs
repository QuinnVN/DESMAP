using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NCKH.VR.Clinic.Tests
{
    public sealed class ClinicRoundRulesTests
    {
        [Test]
        public void PreparationPreservesFullRoundTime()
        {
            ClinicRoundClock clock = new ClinicRoundClock();

            clock.Begin(2f, 60f);
            ClinicClockTransition beforeStart = clock.Tick(1.5f);

            Assert.That(clock.State, Is.EqualTo(ClinicRoundState.Preparing));
            Assert.That(clock.RemainingRoundTime, Is.EqualTo(60f));
            Assert.That(beforeStart, Is.EqualTo(ClinicClockTransition.None));

            ClinicClockTransition start = clock.Tick(0.5f);
            Assert.That(start, Is.EqualTo(ClinicClockTransition.RoundStarted));
            Assert.That(clock.State, Is.EqualTo(ClinicRoundState.Running));
            Assert.That(clock.RemainingRoundTime, Is.EqualTo(60f));
        }

        [TestCase(0f, 1)]
        [TestCase(0.3999f, 1)]
        [TestCase(0.4f, 2)]
        [TestCase(0.6999f, 2)]
        [TestCase(0.7f, 3)]
        [TestCase(0.8999f, 3)]
        [TestCase(0.9f, 4)]
        [TestCase(1f, 4)]
        public void WeightedSeverityUsesConfiguredBoundaries(float roll, int expected)
        {
            int severity = ClinicRoundRules.SelectSeverity(roll, 40f, 30f, 20f, 10f);
            Assert.That(severity, Is.EqualTo(expected));
        }

        [Test]
        public void TreatmentLimitsMatchSeverityRules()
        {
            Assert.That(ClinicRoundRules.TreatmentDuration(1, 30f, 5f), Is.EqualTo(30f));
            Assert.That(ClinicRoundRules.TreatmentDuration(2, 30f, 5f), Is.EqualTo(30f));
            Assert.That(ClinicRoundRules.TreatmentDuration(3, 30f, 5f), Is.EqualTo(30f));
            Assert.That(ClinicRoundRules.TreatmentDuration(4, 30f, 5f), Is.EqualTo(5f));
        }

        [Test]
        public void DefaultCureScoringUsesSeverityAndEmergencyMultiplier()
        {
            Assert.That(ClinicRoundRules.CureScore(1, 1, 1f, 2f), Is.EqualTo(1));
            Assert.That(ClinicRoundRules.CureScore(2, 1, 1f, 2f), Is.EqualTo(2));
            Assert.That(ClinicRoundRules.CureScore(3, 1, 1f, 2f), Is.EqualTo(3));
            Assert.That(ClinicRoundRules.CureScore(4, 1, 1f, 2f), Is.EqualTo(8));
        }

        [Test]
        public void CureScoringSupportsDecimalMultipliersWithHalfUpRounding()
        {
            Assert.That(ClinicRoundRules.CureScore(1, 1, 1.5f, 2f), Is.EqualTo(2));
            Assert.That(ClinicRoundRules.CureScore(3, 1, 1.5f, 2f), Is.EqualTo(5));
            Assert.That(ClinicRoundRules.CureScore(4, 2, 1.25f, 1.5f), Is.EqualTo(15));
        }

        [Test]
        public void NegativeCureSettingsAreTreatedAsZero()
        {
            Assert.That(ClinicRoundRules.CureScore(2, -1, 1f, 2f), Is.Zero);
            Assert.That(ClinicRoundRules.CureScore(2, 1, -1f, 2f), Is.Zero);
            Assert.That(ClinicRoundRules.CureScore(4, 1, 1f, -2f), Is.Zero);
        }

        [Test]
        public void TimeoutPenaltiesAreConfigurableForEverySeverity()
        {
            Assert.That(ClinicRoundRules.TimeoutPenalty(1, 1, 2, 3, 4), Is.EqualTo(1));
            Assert.That(ClinicRoundRules.TimeoutPenalty(2, 1, 2, 3, 4), Is.EqualTo(2));
            Assert.That(ClinicRoundRules.TimeoutPenalty(3, 1, 2, 3, 4), Is.EqualTo(3));
            Assert.That(ClinicRoundRules.TimeoutPenalty(4, 1, 2, 3, 4), Is.EqualTo(4));
        }

        [Test]
        public void ScoreDeltaAllowsNegativeTotals()
        {
            int score = ClinicRoundRules.ScoreDelta(
                PatientResolution.Cured,
                2,
                1,
                1f,
                2f,
                0,
                0,
                0,
                2);
            Assert.That(score, Is.EqualTo(2));

            score += ClinicRoundRules.ScoreDelta(
                PatientResolution.TimedOut,
                2,
                1,
                1f,
                2f,
                0,
                3,
                0,
                2);
            Assert.That(score, Is.EqualTo(-1));

            score += ClinicRoundRules.ScoreDelta(
                PatientResolution.TimedOut,
                4,
                1,
                1f,
                2f,
                0,
                0,
                0,
                2);
            Assert.That(score, Is.EqualTo(-3));
        }

        [Test]
        public void StoppedClockRejectsFurtherTimeProgress()
        {
            ClinicRoundClock clock = new ClinicRoundClock();
            clock.Begin(0f, 60f);
            clock.Stop();

            ClinicClockTransition transition = clock.Tick(100f);

            Assert.That(clock.State, Is.EqualTo(ClinicRoundState.Finished));
            Assert.That(clock.RemainingRoundTime, Is.EqualTo(60f));
            Assert.That(transition, Is.EqualTo(ClinicClockTransition.None));
        }

        [Test]
        public void EmergencyGuaranteeCheckpointIsClampedToRoundDuration()
        {
            MethodInfo clampMethod = typeof(ClinicGameManager).GetMethod(
                "ClampEmergencyGuaranteeCheckpoint",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(clampMethod, Is.Not.Null);
            Assert.That(clampMethod.Invoke(null, new object[] { 30f, 20f }), Is.EqualTo(20f));
            Assert.That(clampMethod.Invoke(null, new object[] { -5f, 20f }), Is.EqualTo(0f));
        }
    }

    public sealed class SicknessDatabaseTests
    {
        private const string MainScenePath = "Assets/Scenes/Doctor/Game 1.unity";

        [TestCase(0f, "Cold")]
        [TestCase(0.2499f, "Cold")]
        [TestCase(0.25f, "Fracture")]
        [TestCase(1f, "Fracture")]
        public void WeightedSelectionUsesConfiguredBoundaries(float roll, string expectedName)
        {
            SicknessDatabase database = CreateDatabase(
                new SicknessEntry("Cold", 1, 1f),
                new SicknessEntry("Fracture", 2, 3f));

            Assert.That(TrySelect(database, roll, null, out SicknessEntry selected), Is.True);
            Assert.That(selected.SicknessName, Is.EqualTo(expectedName));

            Object.DestroyImmediate(database.gameObject);
        }

        [Test]
        public void SeverityFilterSelectsOnlyMatchingEntries()
        {
            SicknessDatabase database = CreateDatabase(
                new SicknessEntry("Cold", 1, 100f),
                new SicknessEntry("Cardiac Arrest", 4, 1f));

            Assert.That(TrySelect(database, 0f, 4, out SicknessEntry selected), Is.True);
            Assert.That(selected.SicknessName, Is.EqualTo("Cardiac Arrest"));
            Assert.That(selected.Severity, Is.EqualTo(4));

            Object.DestroyImmediate(database.gameObject);
        }

        [Test]
        public void InvalidEntriesAreIgnored()
        {
            SicknessDatabase database = CreateDatabase(
                new SicknessEntry("", 1, 10f),
                new SicknessEntry("No Weight", 2, 0f),
                new SicknessEntry("Valid", 3, 1f));

            Assert.That(TrySelect(database, 0f, null, out SicknessEntry selected), Is.True);
            Assert.That(selected.SicknessName, Is.EqualTo("Valid"));

            Object.DestroyImmediate(database.gameObject);
        }

        [Test]
        public void EmptyOrZeroWeightDatabaseCannotSelect()
        {
            SicknessDatabase empty = CreateDatabase();
            SicknessDatabase zeroWeight = CreateDatabase(new SicknessEntry("Cold", 1, 0f));

            Assert.That(empty.TrySelectRandom(out _), Is.False);
            Assert.That(zeroWeight.TrySelectRandom(out _), Is.False);
            Assert.That(zeroWeight.TrySelectRandom(4, out _), Is.False);

            Object.DestroyImmediate(empty.gameObject);
            Object.DestroyImmediate(zeroWeight.gameObject);
        }

        [Test]
        public void MainSceneContainsDatabaseReferencedByManager()
        {
            Scene scene = SceneManager.GetSceneByPath(MainScenePath);
            bool openedForTest = !scene.IsValid() || !scene.isLoaded;
            if (openedForTest)
                scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Additive);

            try
            {
                GameObject databaseObject = FindGameObject(scene, "Sickness Database");
                GameObject managerObject = FindGameObject(scene, "Clinic Game Manager");

                Assert.That(databaseObject, Is.Not.Null);
                Assert.That(managerObject, Is.Not.Null);

                SicknessDatabase database = databaseObject.GetComponent<SicknessDatabase>();
                Assert.That(database, Is.Not.Null);
                Assert.That(database.Entries, Is.Not.Null);

                SerializedObject manager = new SerializedObject(managerObject.GetComponent<ClinicGameManager>());
                SerializedProperty databaseReference = manager.FindProperty("sicknessDatabase");
                Assert.That(databaseReference, Is.Not.Null);
                Assert.That(databaseReference.objectReferenceValue, Is.EqualTo(database));
                Assert.That(manager.FindProperty("debugMode").boolValue, Is.False);
            }
            finally
            {
                if (openedForTest)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static SicknessDatabase CreateDatabase(params SicknessEntry[] entries)
        {
            SicknessDatabase database = new GameObject("Sickness Database").AddComponent<SicknessDatabase>();
            FieldInfo entriesField = typeof(SicknessDatabase).GetField(
                "entries",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(entriesField, Is.Not.Null);
            entriesField.SetValue(database, new List<SicknessEntry>(entries));
            return database;
        }

        private static bool TrySelect(
            SicknessDatabase database,
            float roll,
            int? requiredSeverity,
            out SicknessEntry selected)
        {
            MethodInfo method = typeof(SicknessDatabase).GetMethod(
                "TrySelect",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);

            object[] arguments = { roll, requiredSeverity, null };
            bool result = (bool)method.Invoke(database, arguments);
            selected = (SicknessEntry)arguments[2];
            return result;
        }

        private static GameObject FindGameObject(Scene scene, string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform match = FindDescendant(roots[i].transform, objectName);
                if (match != null)
                    return match.gameObject;
            }

            return null;
        }

        private static Transform FindDescendant(Transform current, string objectName)
        {
            if (current.name == objectName)
                return current;

            for (int i = 0; i < current.childCount; i++)
            {
                Transform match = FindDescendant(current.GetChild(i), objectName);
                if (match != null)
                    return match;
            }

            return null;
        }
    }

    public sealed class ClinicHudLayoutTests
    {
        private const string MainScenePath = "Assets/Scenes/Doctor/Game 1.unity";

        [Test]
        public void MainSceneHudUsesXrSafeLayoutAndReferences()
        {
            Scene scene = SceneManager.GetSceneByPath(MainScenePath);
            bool openedForTest = !scene.IsValid() || !scene.isLoaded;
            if (openedForTest)
                scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Additive);

            try
            {
                GameObject hudObject = FindGameObject(scene, "Player HUD");
                GameObject overlayObject = FindGameObject(scene, "Preparation Overlay");
                GameObject countdownObject = FindGameObject(scene, "Countdown");
                GameObject scoreObject = FindGameObject(scene, "Total Score");
                GameObject timerObject = FindGameObject(scene, "Round Timer");
                GameObject backdropObject = FindGameObject(scene, "Round Timer Backdrop");
                GameObject managerObject = FindGameObject(scene, "Clinic Game Manager");

                Assert.That(hudObject, Is.Not.Null);
                Assert.That(overlayObject, Is.Not.Null);
                Assert.That(countdownObject, Is.Not.Null);
                Assert.That(scoreObject, Is.Not.Null);
                Assert.That(timerObject, Is.Not.Null);
                Assert.That(backdropObject, Is.Not.Null);
                Assert.That(managerObject, Is.Not.Null);

                CanvasScaler scaler = hudObject.GetComponent<CanvasScaler>();
                Assert.That(scaler, Is.Not.Null);
                Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1440f, 1440f)));

                AssertEditableRectWithinViewport(countdownObject.GetComponent<RectTransform>());
                AssertEditableRectWithinViewport(scoreObject.GetComponent<RectTransform>());
                AssertTimerRect(backdropObject.GetComponent<RectTransform>());
                AssertStretchedRect(timerObject.GetComponent<RectTransform>());

                Image backdrop = backdropObject.GetComponent<Image>();
                Assert.That(backdrop, Is.Not.Null);
                Assert.That(backdrop.color, Is.EqualTo(new Color(0f, 0f, 0f, 0.45f)));
                Assert.That(backdrop.raycastTarget, Is.False);
                Assert.That(backdropObject.activeSelf, Is.False);
                Assert.That(timerObject.transform.parent, Is.EqualTo(backdropObject.transform));

                Component timer = timerObject.GetComponent("TextMeshProUGUI");
                Assert.That(timer, Is.Not.Null);
                SerializedObject serializedTimer = new SerializedObject(timer);
                Assert.That(serializedTimer.FindProperty("m_fontSize").floatValue, Is.EqualTo(46f));
                Assert.That(serializedTimer.FindProperty("m_margin").vector4Value, Is.EqualTo(new Vector4(12f, 6f, 12f, 6f)));

                Component score = scoreObject.GetComponent("TextMeshProUGUI");
                Assert.That(score, Is.Not.Null);
                SerializedObject serializedScore = new SerializedObject(score);
                Assert.That(serializedScore.FindProperty("m_enableAutoSizing").boolValue, Is.True);
                Assert.That(serializedScore.FindProperty("m_fontSizeMin").floatValue, Is.EqualTo(32f));
                Assert.That(serializedScore.FindProperty("m_fontSizeMax").floatValue, Is.EqualTo(48f));

                Component countdown = countdownObject.GetComponent("TextMeshProUGUI");
                ClinicRoundHud hud = hudObject.GetComponent<ClinicRoundHud>();
                SerializedObject serializedHud = new SerializedObject(hud);
                AssertReference(serializedHud, "gameManager", managerObject.GetComponent<ClinicGameManager>());
                AssertReference(serializedHud, "roundTimerRoot", backdropObject);
                AssertReference(serializedHud, "roundTimerText", timer);
                AssertReference(serializedHud, "preparationCountdownText", countdown);
                AssertReference(serializedHud, "totalScoreText", score);
                AssertReference(serializedHud, "preparationOverlay", overlayObject.GetComponent<CanvasGroup>());

                SerializedObject serializedManager = new SerializedObject(managerObject.GetComponent<ClinicGameManager>());
                Assert.That(serializedManager.FindProperty("automaticStartReadinessTimeout").floatValue, Is.EqualTo(10f));
                Assert.That(serializedManager.FindProperty("automaticStartStableFrames").intValue, Is.EqualTo(2));
            }
            finally
            {
                if (openedForTest)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void AssertEditableRectWithinViewport(RectTransform rect)
        {
            Assert.That(rect.anchorMin.x, Is.InRange(0f, 1f));
            Assert.That(rect.anchorMin.y, Is.InRange(0f, 1f));
            Assert.That(rect.anchorMax.x, Is.InRange(rect.anchorMin.x, 1f));
            Assert.That(rect.anchorMax.y, Is.InRange(rect.anchorMin.y, 1f));
        }

        private static void AssertTimerRect(RectTransform rect)
        {
            Assert.That(rect.anchorMin, Is.EqualTo(rect.anchorMax));
            Assert.That(rect.anchorMin.x, Is.EqualTo(0.5f));
            Assert.That(rect.anchorMin.y, Is.InRange(0.18f, 0.82f));
            Assert.That(rect.sizeDelta, Is.EqualTo(new Vector2(320f, 80f)));
            Assert.That(rect.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        }

        private static void AssertStretchedRect(RectTransform rect)
        {
            Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(rect.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(rect.sizeDelta, Is.EqualTo(Vector2.zero));
            Assert.That(rect.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        }

        private static void AssertReference(SerializedObject serializedObject, string propertyName, Object expected)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"Missing serialized property {propertyName}.");
            Assert.That(property.objectReferenceValue, Is.EqualTo(expected));
        }

        private static GameObject FindGameObject(Scene scene, string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform match = FindDescendant(roots[i].transform, objectName);
                if (match != null)
                    return match.gameObject;
            }

            return null;
        }

        private static Transform FindDescendant(Transform current, string objectName)
        {
            if (current.name == objectName)
                return current;

            for (int i = 0; i < current.childCount; i++)
            {
                Transform match = FindDescendant(current.GetChild(i), objectName);
                if (match != null)
                    return match;
            }

            return null;
        }
    }
}
