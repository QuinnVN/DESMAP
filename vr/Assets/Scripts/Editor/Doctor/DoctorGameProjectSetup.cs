using System;
using System.Collections.Generic;
using System.Linq;
using NCKH.VR.Doctor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace NCKH.VR.Doctor.Editor
{
    public static class DoctorGameProjectSetup
    {
        private const string ScenePath = "Assets/Scenes/Doctor/Game 2.unity";
        private const string GameOneScenePath = "Assets/Scenes/Doctor/Game 1.unity";
        private const string DataFolder = "Assets/Data/Doctor";
        private const string DatabasePath = DataFolder + "/Placeholder Doctor Case Database.asset";
        private const string InputPath = "Assets/Input/Doctor/Doctor Controls.inputactions";
        private const string XriInputPath = "Assets/ThirdParty/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/XRI Default Input Actions.inputactions";
        private const string InputReferenceFolder = "Assets/Input/Doctor/References";
        private const string MaterialFolder = "Assets/Materials/Doctor";
        private const string UiOverlayMaterialPath = MaterialFolder + "/Doctor UI Overlay.mat";
        private const string TmpOverlayMaterialPath = MaterialFolder + "/Doctor TMP Overlay.mat";
        private const string MonitorPrefabPath = "Assets/ThirdParty/Hospital Equipments/Prefabs/Monitor Variant.prefab";
        private const string UiOverlayShaderName = "NCKH/Doctor/UI Overlay";
        private const string HudLayerName = "DoctorHUD";

        [MenuItem("Tools/NCKH VR/Setup Doctor Minigame")]
        public static void SetupDoctorMinigame()
        {
            EnsureFolder("Assets/Data");
            EnsureFolder(DataFolder);
            EnsureFolder(InputReferenceFolder);
            EnsureFolder(MaterialFolder);
            int hudLayer = EnsureLayer(HudLayerName);
            Material uiOverlayMaterial = EnsureUiOverlayMaterial();
            Material tmpOverlayMaterial = EnsureTmpOverlayMaterial();
            DoctorCaseDefinition[] cases = CreatePlaceholderCases();
            DoctorCaseDatabase database = CreateDatabase(cases);
            AssetDatabase.SaveAssets();
            ConfigureScene(database, hudLayer, uiOverlayMaterial, tmpOverlayMaterial);
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Doctor minigame setup completed: HUD, portrait notebook, input actions, and question phases are configured.");
        }

        private static DoctorCaseDefinition[] CreatePlaceholderCases()
        {
            return new[]
            {
                CreateOrUpdateCase(DataFolder + "/Placeholder Case A.asset", "Alex Morgan",
                    Dialogue("Tôi bắt đầu thấy không khỏe từ tối qua sau chuyến đi làm dài như thường lệ. Tôi bị ho khan và sốt nhẹ. Tối qua tôi cũng xem một bộ phim.",
                        Card("a-time", "Khởi phát từ tối qua", DoctorInfoCategory.Time),
                        Card("a-cough", "Ho khan", DoctorInfoCategory.Symptoms),
                        Card("a-fever", "Sốt nhẹ", DoctorInfoCategory.Symptoms),
                        Noise("a-movie", "Xem phim tối qua")),
                    new[]
                    {
                        Phase("a-when", "Bệnh bắt đầu chính xác khi nào?", "Bệnh bắt đầu khoảng bảy giờ tối, ngay sau bữa tối.", Noise("a-dinner", "Ăn tối trước khi xuất hiện triệu chứng")),
                        Phase("a-travel-q", "Gần đây bạn có đi xa không?", "Gần đây tôi không đi xa, thậm chí không ra khỏi thành phố.", Card("a-travel", "Gần đây không đi xa", DoctorInfoCategory.TravelHistory)),
                        Phase("a-symptoms-q", "Bạn có thể mô tả triệu chứng chi tiết hơn không?", "Tôi ho khan và chỉ sốt nhẹ. Uống loại trà yêu thích cũng không đỡ.", Noise("a-tea", "Đã thử uống trà")),
                        Phase("a-history-q", "Bạn có tiền sử bệnh nào liên quan không?", "Tôi có tiền sử hen suyễn, nhưng bệnh đã được kiểm soát nhiều năm.", Card("a-history", "Tiền sử hen suyễn", DoctorInfoCategory.MedicalHistory))
                    }),
                CreateOrUpdateCase(DataFolder + "/Placeholder Case B.asset", "Jordan Lee",
                    Dialogue("Tôi bắt đầu đau bụng từ sáng nay. Tôi thấy buồn nôn và đã nôn hai lần. Hôm qua tôi mua một chiếc ô mới.",
                        Card("b-time", "Khởi phát sáng nay", DoctorInfoCategory.Time),
                        Card("b-pain", "Đau bụng", DoctorInfoCategory.Symptoms),
                        Card("b-nausea", "Buồn nôn", DoctorInfoCategory.Symptoms),
                        Card("b-vomit", "Đã nôn hai lần", DoctorInfoCategory.Symptoms),
                        Noise("b-umbrella", "Mua một chiếc ô mới")),
                    new[]
                    {
                        Phase("b-when", "Cơn đau bắt đầu khi nào?", "Cơn đau bắt đầu ngay sau bữa sáng nay.", Noise("b-breakfast", "Ăn sáng trước khi đau")),
                        Phase("b-travel-q", "Gần đây bạn có đi xa không?", "Tôi trở về từ chuyến đi biển hai ngày trước và dành phần lớn thời gian để đọc sách.", Card("b-travel", "Trở về từ chuyến đi biển hai ngày trước", DoctorInfoCategory.TravelHistory), Noise("b-reading", "Đọc sách trong chuyến đi")),
                        Phase("b-symptoms-q", "Bạn còn triệu chứng nào khác không?", "Không còn triệu chứng nào khác, nhưng tôi mệt vì đã thức khuya.", Noise("b-late", "Đã thức khuya")),
                        Phase("b-history-q", "Tiền sử bệnh của bạn thế nào?", "Tôi không có tiền sử bệnh đáng kể và không dùng thuốc thường xuyên.", Card("b-history", "Không có tiền sử bệnh đáng kể", DoctorInfoCategory.MedicalHistory))
                    }),
                CreateOrUpdateCase(DataFolder + "/Placeholder Case C.asset", "Sam Rivera",
                    Dialogue("Cuối chiều nay cổ họng tôi bắt đầu thắt lại và tôi bị khò khè. Trước khi đến đây, tôi đã sắp xếp lại bàn làm việc.",
                        Card("c-time", "Khởi phát cuối chiều nay", DoctorInfoCategory.Time),
                        Card("c-tight", "Thắt cổ họng", DoctorInfoCategory.Symptoms),
                        Card("c-wheeze", "Khò khè", DoctorInfoCategory.Symptoms),
                        Noise("c-desk", "Sắp xếp lại bàn làm việc")),
                    new[]
                    {
                        Phase("c-exposure-q", "Bạn đã làm gì trước khi triệu chứng xuất hiện?", "Triệu chứng bắt đầu ngay sau khi tôi dọn một kho chứa đầy bụi.", Card("c-exposure", "Triệu chứng xuất hiện sau khi tiếp xúc phòng bụi", DoctorInfoCategory.Time)),
                        Phase("c-travel-q", "Gần đây bạn có đi xa không?", "Trong tháng qua tôi không đi ra ngoài khu vực này.", Card("c-travel", "Không đi xa trong tháng qua", DoctorInfoCategory.TravelHistory)),
                        Phase("c-symptoms-q", "Bạn còn triệu chứng nào khác không?", "Tôi bị khó thở nhưng không sốt.", Card("c-breath", "Khó thở", DoctorInfoCategory.Symptoms), Card("c-no-fever", "Không sốt", DoctorInfoCategory.Symptoms)),
                        Phase("c-history-q", "Bạn có dị ứng hoặc bệnh hô hấp không?", "Tôi bị dị ứng phấn hoa và từng dùng ống hít khi còn nhỏ.", Card("c-allergy", "Dị ứng phấn hoa", DoctorInfoCategory.MedicalHistory), Card("c-inhaler", "Từng dùng ống hít khi còn nhỏ", DoctorInfoCategory.MedicalHistory))
                    })
            };
        }

        private static DoctorCaseDefinition CreateOrUpdateCase(string path, string patientName, DoctorDialogue opening, IEnumerable<DoctorQuestionPhase> phases)
        {
            DoctorCaseDefinition asset = AssetDatabase.LoadAssetAtPath<DoctorCaseDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<DoctorCaseDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }
            asset.ConfigureInEditor(patientName, 1f, opening, phases);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static DoctorCaseDatabase CreateDatabase(IEnumerable<DoctorCaseDefinition> cases)
        {
            DoctorCaseDatabase database = AssetDatabase.LoadAssetAtPath<DoctorCaseDatabase>(DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<DoctorCaseDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }
            database.ConfigureInEditor(cases);
            EditorUtility.SetDirty(database);
            return database;
        }

        private static void ConfigureScene(
            DoctorCaseDatabase database,
            int hudLayer,
            Material uiOverlayMaterial,
            Material tmpOverlayMaterial)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject npc = FindRoot(scene, "NPC") ?? CreatePlaceholderNpc();
            GameObject monitor = FindRoot(scene, "Monitor Variant") ?? CreateMonitor(scene);
            GameObject vrPlayer = FindRequiredRoot(scene, "VR Player");
            Transform cameraTransform = FindRequiredDescendant(vrPlayer.transform, "Main Camera");
            Camera xrCamera = cameraTransform.GetComponent<Camera>();
            EnsureEventSystem(scene);

            Transform model = FindRequiredDescendant(npc.transform, "Capsule");
            Transform oldNpcCanvas = FindDescendant(npc.transform, "Canvas");
            if (oldNpcCanvas != null)
                oldNpcCanvas.gameObject.SetActive(false);

            AudioSource voiceSource = GetOrAdd<AudioSource>(npc);
            voiceSource.playOnAwake = false;
            voiceSource.loop = false;
            voiceSource.spatialBlend = 1f;
            voiceSource.minDistance = 1f;
            voiceSource.maxDistance = 15f;
            DoctorPatientPresenter presenter = GetOrAdd<DoctorPatientPresenter>(npc);
            SetReferences(presenter, ("modelRoot", model.gameObject), ("voiceSource", voiceSource));

            DoctorNotebookInteractable notebookActivation = ConfigureMonitorRayButton(monitor, xrCamera, hudLayer);
            GameObject obsoleteNotebook = FindRoot(scene, "Notebook");
            if (obsoleteNotebook != null)
                UnityEngine.Object.DestroyImmediate(obsoleteNotebook);

            Transform existingHud = FindDescendant(cameraTransform, "Doctor HUD");
            bool createdHud = existingHud == null;
            GameObject hudRoot;
            if (createdHud)
            {
                DestroySceneObjectsNamed(scene, "Doctor UI");
                hudRoot = CreateDoctorHud(cameraTransform, xrCamera, hudLayer, uiOverlayMaterial, tmpOverlayMaterial);
            }
            else
            {
                hudRoot = existingHud.gameObject;
                Transform coreHud = FindRequiredDescendant(hudRoot.transform, "Core HUD Canvas");
                ApplyCoreHudPresentation(coreHud, uiOverlayMaterial, tmpOverlayMaterial);
                SetLayerRecursively(hudRoot, hudLayer);
            }
            DoctorNotebookUI notebookUi = hudRoot.GetComponent<DoctorNotebookUI>();
            ConfigureUiReferences(notebookUi);

            Transform rightHand = FindRequiredDescendant(vrPlayer.transform, "Right hand");
            Transform rightRay = FindRequiredDescendant(rightHand, "Ray Interactor");
            XRRayInteractor rightRayInteractor = rightRay.GetComponent<XRRayInteractor>();
            XRInteractorLineVisual rightRayLineVisual = rightRay.GetComponent<XRInteractorLineVisual>();
            if (rightRayInteractor == null)
                throw new InvalidOperationException("Right hand Ray Interactor requires XRRayInteractor.");
            if (rightRayLineVisual == null)
                throw new InvalidOperationException("Right hand Ray Interactor requires XRInteractorLineVisual.");
            IncludeLayerInRaycastMask(rightRayInteractor, hudLayer);
            ConfigureRightRayScroll(rightRayInteractor);
            if (createdHud)
                ConfigureRightRayVisual(rightRayLineVisual);
            rightRayLineVisual.enabled = true;

            GameObject managerObject = FindRoot(scene, "Doctor Game Manager") ?? new GameObject("Doctor Game Manager");
            DoctorGameManager manager = GetOrAdd<DoctorGameManager>(managerObject);
            Transform locomotion = FindRequiredDescendant(vrPlayer.transform, "Locomotion");
            SetReferences(manager,
                ("caseDatabase", database), ("patientPresenter", presenter),
                ("notebookInteractable", notebookActivation), ("notebookUI", notebookUi),
                ("moveProviderRoot", FindRequiredDescendant(locomotion, "Move").gameObject),
                ("jumpProviderRoot", FindRequiredDescendant(locomotion, "Jump").gameObject));
            SerializedObject managerSerialized = new SerializedObject(manager);
            managerSerialized.FindProperty("preparationDuration").floatValue = 10f;
            managerSerialized.FindProperty("roundDuration").floatValue = 300f;
            managerSerialized.FindProperty("dialogueFallbackDuration").floatValue = 4f;
            managerSerialized.FindProperty("patientTransitionDelay").floatValue = 1f;
            managerSerialized.FindProperty("pointsPerCorrectInfo").intValue = 1;
            managerSerialized.FindProperty("startAutomatically").boolValue = true;
            managerSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(voiceSource);
            EditorUtility.SetDirty(presenter);
            EditorUtility.SetDirty(notebookActivation);
            EditorUtility.SetDirty(notebookUi);
            EditorUtility.SetDirty(rightRayInteractor);
            EditorUtility.SetDirty(rightRayLineVisual);
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static DoctorNotebookInteractable ConfigureMonitorRayButton(GameObject monitor, Camera xrCamera, int layer)
        {
            Transform existing = monitor.transform.Find("Monitor Ray Button");
            if (existing != null && !(existing is RectTransform))
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
                existing = null;
            }
            bool created = existing == null;
            GameObject target = created
                ? new GameObject("Monitor Ray Button", typeof(RectTransform))
                : existing.gameObject;
            RectTransform rect = target.GetComponent<RectTransform>();
            if (created)
                rect.SetParent(monitor.transform, false);
            rect.localPosition = new Vector3(0.184f, 0.224f, 0f);
            rect.localRotation = Quaternion.Euler(0f, -90f, 0f);
            rect.localScale = Vector3.one * 0.001f;
            rect.sizeDelta = new Vector2(520f, 400f);
            Canvas canvas = GetOrAdd<Canvas>(target);
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = xrCamera;
            canvas.sortingOrder = 100;
            TrackedDeviceGraphicRaycaster raycaster = GetOrAdd<TrackedDeviceGraphicRaycaster>(target);
            raycaster.ignoreReversedGraphics = false;
            raycaster.checkFor2DOcclusion = false;
            raycaster.checkFor3DOcclusion = false;
            Image image = GetOrAdd<Image>(target);
            image.color = Color.clear;
            image.raycastTarget = true;
            Button button = GetOrAdd<Button>(target);
            button.targetGraphic = image;
            DoctorNotebookInteractable interactable = GetOrAdd<DoctorNotebookInteractable>(monitor);
            SetReferences(interactable, ("activationButton", button));
            SetLayerRecursively(target, layer);
            return interactable;
        }

        private static GameObject CreateDoctorHud(
            Transform cameraTransform,
            Camera xrCamera,
            int layer,
            Material uiOverlayMaterial,
            Material tmpOverlayMaterial)
        {
            GameObject root = new GameObject("Doctor HUD", typeof(RectTransform), typeof(DoctorNotebookUI));
            root.transform.SetParent(cameraTransform, false);
            root.transform.localPosition = Vector3.zero;
            Canvas core = CreateCanvas(root.transform, "Core HUD Canvas", xrCamera, 0.62f, 220);
            Canvas notebook = CreateCanvas(root.transform, "Notebook HUD Canvas", xrCamera, 0.56f, 200);
            BuildCoreHud(core.transform);
            ApplyCoreHudPresentation(core.transform, uiOverlayMaterial, tmpOverlayMaterial);
            BuildNotebookHud(notebook.transform);
            notebook.gameObject.SetActive(true);
            SetLayerRecursively(root, layer);
            return root;
        }

        private static Canvas CreateCanvas(Transform parent, string name, Camera camera, float planeDistance, int sortingOrder)
        {
            GameObject target = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(TrackedDeviceGraphicRaycaster));
            target.transform.SetParent(parent, false);
            Canvas canvas = target.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = planeDistance;
            canvas.sortingOrder = sortingOrder;
            CanvasScaler scaler = target.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1200f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void BuildCoreHud(Transform root)
        {
            Color dark = new Color(0.025f, 0.07f, 0.08f, 0.95f);
            Color teal = new Color(0.2f, 0.66f, 0.58f, 1f);
            RectTransform timer = CreatePanel(root, "Timer Badge", new Vector2(.41f, .92f), new Vector2(.59f, .985f), dark);
            CreateLabel(timer, "Timer Label", "5:00", Vector2.zero, Vector2.one, 36, Color.white, true);
            RectTransform prep = CreatePanel(root, "Preparation Overlay", Vector2.zero, Vector2.one, dark);
            CreateLabel(prep, "Preparation Title", "CHUẨN BỊ", new Vector2(.15f, .62f), new Vector2(.85f, .74f), 44, teal, true);
            CreateLabel(prep, "Preparation Label", "10", new Vector2(.2f, .32f), new Vector2(.8f, .62f), 170, Color.white, true);

            RectTransform dialogue = CreatePanel(root, "Dialogue Panel", new Vector2(.07f, .035f), new Vector2(.93f, .27f), dark);
            CreateLabel(dialogue, "Dialogue Patient Name", "Bệnh nhân mẫu", new Vector2(.04f, .7f), new Vector2(.96f, .96f), 30, teal, true, TextAlignmentOptions.MidlineLeft);
            CreateLabel(dialogue, "Dialogue Text", "Lời thoại mẫu", new Vector2(.04f, .25f), new Vector2(.96f, .7f), 28, Color.white, false, TextAlignmentOptions.TopLeft);
            CreateLabel(dialogue, "HUD Hint Label", "", new Vector2(.04f, .02f), new Vector2(.96f, .24f), 20, new Color(.75f, .88f, .85f), false, TextAlignmentOptions.MidlineLeft);

            RectTransform questions = CreatePanel(root, "Question Panel", new Vector2(.48f, .30f), new Vector2(.94f, .55f), Color.clear);
            CreateLabel(questions, "Question Counter", "1/1  |  Cần trái: chọn  |  A: hỏi", new Vector2(.03f, .84f), new Vector2(.97f, .99f), 18, Color.white, false, TextAlignmentOptions.MidlineRight);
            CreateScrollArea(questions, "Question Scroll", Vector2.zero, Vector2.one, out ScrollRect qScroll, out RectTransform qContent);
            RectTransform questionArea = qScroll.viewport;
            questionArea.name = "Question Area";
            SetAnchors(questionArea, new Vector2(.02f, .02f), new Vector2(.98f, .98f));
            ConfigureQuestionContent(qContent);
            DoctorQuestionItemView qTemplate = CreateQuestionTemplate(questions);

            RectTransform result = CreatePanel(root, "Result Overlay", Vector2.zero, Vector2.one, dark);
            CreateLabel(result, "Result Score", "TỔNG ĐIỂM\n0", new Vector2(.15f, .25f), new Vector2(.85f, .75f), 72, Color.white, true);
            questions.gameObject.SetActive(false);
            result.gameObject.SetActive(false);
            qTemplate.gameObject.SetActive(false);
        }

        private static void BuildNotebookHud(Transform root)
        {
            Color ink = new Color(.12f, .15f, .15f, 1f);
            Color paper = new Color(.94f, .90f, .76f, .99f);
            Color pale = new Color(.98f, .96f, .88f, 1f);
            Color teal = new Color(.17f, .48f, .43f, 1f);
            RectTransform overlay = CreatePanel(root, "Notebook Overlay", new Vector2(.18f, .015f), new Vector2(.82f, .985f), new Color(.02f, .04f, .04f, .98f));
            RectTransform shell = CreatePanel(overlay, "Notebook Shell", new Vector2(.018f, .012f), new Vector2(.982f, .988f), paper);
            CreateLabel(shell, "Patient Name Label", "GHI CHÚ BỆNH NHÂN", new Vector2(.04f, .91f), new Vector2(.96f, .98f), 34, ink, true, TextAlignmentOptions.MidlineLeft);
            CreateLabel(shell, "Notebook Hint Label", "Kéo chi tiết sang trái, hoặc chọn chi tiết rồi chọn một mục.", new Vector2(.04f, .855f), new Vector2(.96f, .91f), 18, ink, false, TextAlignmentOptions.MidlineLeft);

            RectTransform left = CreatePanel(shell, "Category Column", new Vector2(.025f, .12f), new Vector2(.49f, .85f), pale);
            CreateScrollArea(left, "Category Scroll", new Vector2(.02f, .02f), new Vector2(.98f, .98f), out _, out RectTransform categoryContent);
            CreateCategory(categoryContent, "Time Drop", "THỜI GIAN", out _, out _);
            CreateCategory(categoryContent, "Travel Drop", "LỊCH SỬ ĐI LẠI", out _, out _);
            CreateCategory(categoryContent, "History Drop", "TIỀN SỬ BỆNH", out _, out _);
            CreateCategory(categoryContent, "Symptoms Drop", "TRIỆU CHỨNG", out _, out _);

            RectTransform right = CreatePanel(shell, "Unfiled Drop", new Vector2(.51f, .12f), new Vector2(.975f, .85f), pale);
            Button rightButton = right.gameObject.AddComponent<Button>();
            rightButton.targetGraphic = right.GetComponent<Image>();
            CreateLabel(right, "Details Title", "CHI TIẾT", new Vector2(.04f, .91f), new Vector2(.96f, .99f), 25, teal, true, TextAlignmentOptions.MidlineLeft);
            CreateScrollArea(right, "Details Scroll", new Vector2(.025f, .02f), new Vector2(.975f, .90f), out _, out RectTransform detailsContent);

            Button done = CreateButton(shell, "Done Button", "HOÀN TẤT", new Vector2(.34f, .02f), new Vector2(.66f, .095f), teal, Color.white);
            RectTransform dragLayer = CreateRect(root, "Drag Layer");
            Stretch(dragLayer);
            DoctorInfoCardView cardTemplate = CreateCardTemplate(root);
            overlay.gameObject.SetActive(false);
            cardTemplate.gameObject.SetActive(false);
        }

        private static void ConfigureUiReferences(DoctorNotebookUI ui)
        {
            Transform root = ui.transform;
            Transform questionArea = FindDescendant(root, "Question Area")
                ?? FindRequiredDescendant(root, "Question Scroll Viewport");
            questionArea.name = "Question Area";
            RectTransform questionContent = FindRequiredDescendant(root, "Question Scroll Content").GetComponent<RectTransform>();
            ConfigureQuestionContent(questionContent);
            SetReferences(ui,
                ("timerBadge", FindRequiredDescendant(root, "Timer Badge").gameObject),
                ("timerLabel", FindRequiredDescendant(root, "Timer Label").GetComponent<TMP_Text>()),
                ("preparationOverlay", FindRequiredDescendant(root, "Preparation Overlay").gameObject),
                ("preparationLabel", FindRequiredDescendant(root, "Preparation Label").GetComponent<TMP_Text>()),
                ("dialoguePanel", FindRequiredDescendant(root, "Dialogue Panel").gameObject),
                ("dialoguePatientName", FindRequiredDescendant(root, "Dialogue Patient Name").GetComponent<TMP_Text>()),
                ("dialogueText", FindRequiredDescendant(root, "Dialogue Text").GetComponent<TMP_Text>()),
                ("hudHintLabel", FindRequiredDescendant(root, "HUD Hint Label").GetComponent<TMP_Text>()),
                ("questionPanel", FindRequiredDescendant(root, "Question Panel").gameObject),
                ("questionCounterLabel", FindRequiredDescendant(root, "Question Counter").GetComponent<TMP_Text>()),
                ("questionScroll", FindRequiredDescendant(root, "Question Scroll").GetComponent<ScrollRect>()),
                ("questionArea", questionArea.GetComponent<RectTransform>()),
                ("questionContent", questionContent),
                ("questionItemTemplate", FindRequiredDescendant(root, "Question Item Template").GetComponent<DoctorQuestionItemView>()),
                ("resultOverlay", FindRequiredDescendant(root, "Result Overlay").gameObject),
                ("resultScore", FindRequiredDescendant(root, "Result Score").GetComponent<TMP_Text>()),
                ("notebookCanvasRoot", FindRequiredDescendant(root, "Notebook HUD Canvas").gameObject),
                ("notebookOverlay", FindRequiredDescendant(root, "Notebook Overlay").gameObject),
                ("patientNameLabel", FindRequiredDescendant(root, "Patient Name Label").GetComponent<TMP_Text>()),
                ("notebookHintLabel", FindRequiredDescendant(root, "Notebook Hint Label").GetComponent<TMP_Text>()),
                ("unfiledDrop", FindRequiredDescendant(root, "Unfiled Drop").GetComponent<RectTransform>()),
                ("unfiledContent", FindRequiredDescendant(root, "Details Scroll Content").GetComponent<RectTransform>()),
                ("timeDrop", FindRequiredDescendant(root, "Time Drop").GetComponent<RectTransform>()),
                ("timeContent", FindRequiredDescendant(root, "Time Content").GetComponent<RectTransform>()),
                ("travelDrop", FindRequiredDescendant(root, "Travel Drop").GetComponent<RectTransform>()),
                ("travelContent", FindRequiredDescendant(root, "Travel Content").GetComponent<RectTransform>()),
                ("historyDrop", FindRequiredDescendant(root, "History Drop").GetComponent<RectTransform>()),
                ("historyContent", FindRequiredDescendant(root, "History Content").GetComponent<RectTransform>()),
                ("symptomsDrop", FindRequiredDescendant(root, "Symptoms Drop").GetComponent<RectTransform>()),
                ("symptomsContent", FindRequiredDescendant(root, "Symptoms Content").GetComponent<RectTransform>()),
                ("dragLayer", FindRequiredDescendant(root, "Drag Layer").GetComponent<RectTransform>()),
                ("cardTemplate", FindRequiredDescendant(root, "Card Template").GetComponent<DoctorInfoCardView>()),
                ("doneButton", FindRequiredDescendant(root, "Done Button").GetComponent<Button>()),
                ("unfiledDropButton", FindRequiredDescendant(root, "Unfiled Drop").GetComponent<Button>()),
                ("timeDropButton", FindRequiredDescendant(root, "Time Drop").GetComponent<Button>()),
                ("travelDropButton", FindRequiredDescendant(root, "Travel Drop").GetComponent<Button>()),
                ("historyDropButton", FindRequiredDescendant(root, "History Drop").GetComponent<Button>()),
                ("symptomsDropButton", FindRequiredDescendant(root, "Symptoms Drop").GetComponent<Button>()),
                ("questionNavigateAction", EnsureInputReference("NavigateQuestions")),
                ("questionSelectAction", EnsureInputReference("SelectQuestion")),
                ("notebookCloseAction", EnsureInputReference("CloseNotebook")));
            SetFloatProperties(ui,
                ("questionPreferredPillHeight", 40f),
                ("questionSpacing", 8f),
                ("questionHorizontalPadding", 6f),
                ("questionVerticalPadding", 6f),
                ("questionMinimumFontSize", 5f),
                ("questionMaximumFontSize", 21f));
        }

        private static void ConfigureQuestionContent(RectTransform content)
        {
            content.localScale = Vector3.one;
            content.anchorMin = Vector2.zero;
            content.anchorMax = new Vector2(1f, 0f);
            content.pivot = new Vector2(.5f, 0f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            if (content.TryGetComponent(out ContentSizeFitter fitter))
                fitter.enabled = false;
            if (content.TryGetComponent(out VerticalLayoutGroup layout))
            {
                layout.childAlignment = TextAnchor.LowerCenter;
                layout.spacing = 8f;
                layout.padding = new RectOffset(6, 6, 6, 6);
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
            }
        }

        private static InputActionReference EnsureInputReference(string actionName)
        {
            string path = $"{InputReferenceFolder}/Doctor {actionName}.asset";
            InputActionReference reference = AssetDatabase.LoadAssetAtPath<InputActionReference>(path);
            InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            InputAction action = inputAsset != null ? inputAsset.FindAction("Doctor/" + actionName, true) : null;
            if (reference == null)
            {
                reference = InputActionReference.Create(action);
                reference.name = "Doctor " + actionName;
                AssetDatabase.CreateAsset(reference, path);
            }
            else
                reference.Set(action);
            EditorUtility.SetDirty(reference);
            return reference;
        }

        private static InputActionReference FindXriInputReference(string actionPath)
        {
            InputActionReference reference = AssetDatabase.LoadAllAssetsAtPath(XriInputPath)
                .OfType<InputActionReference>()
                .FirstOrDefault(candidate => candidate.action != null &&
                    candidate.action.actionMap.name + "/" + candidate.action.name == actionPath);
            return reference != null
                ? reference
                : throw new InvalidOperationException($"XRI input action '{actionPath}' is required.");
        }

        private static void CreateCategory(Transform parent, string name, string title, out RectTransform content, out Button button)
        {
            RectTransform panel = CreatePanel(parent, name, Vector2.zero, Vector2.one, new Color(.89f, .91f, .84f, 1f));
            LayoutElement element = panel.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 215f;
            element.preferredHeight = 215f;
            button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = panel.GetComponent<Image>();
            CreateLabel(panel, name + " Title", title, new Vector2(.04f, .74f), new Vector2(.96f, .97f), 22, new Color(.17f, .48f, .43f), true, TextAlignmentOptions.MidlineLeft);
            content = CreateRect(panel, name.Replace(" Drop", " Content"));
            SetAnchors(content, new Vector2(.035f, .04f), new Vector2(.965f, .73f));
            AddVerticalContent(content, 7);
        }

        private static void CreateScrollArea(Transform parent, string name, Vector2 min, Vector2 max, out ScrollRect scroll, out RectTransform content)
        {
            RectTransform area = CreateRect(parent, name);
            SetAnchors(area, min, max);
            Image background = area.gameObject.AddComponent<Image>();
            background.color = new Color(0, 0, 0, .035f);
            RectTransform viewport = CreateRect(area, name + " Viewport");
            SetAnchors(viewport, Vector2.zero, new Vector2(.94f, 1f));
            viewport.gameObject.AddComponent<RectMask2D>();
            content = CreateRect(viewport, name + " Content");
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            AddVerticalContent(content, 8);
            RectTransform bar = CreatePanel(area, name + " Scrollbar", new Vector2(.95f, 0), Vector2.one, new Color(.1f, .18f, .17f, .16f));
            Scrollbar scrollbar = bar.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            RectTransform handle = CreatePanel(bar, "Handle", new Vector2(.12f, 0), new Vector2(.88f, .3f), new Color(.17f, .48f, .43f, .8f));
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handle.GetComponent<Image>();
            scroll = area.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.verticalScrollbar = scrollbar;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35f;
        }

        private static void AddVerticalContent(RectTransform content, float spacing)
        {
            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static DoctorInfoCardView CreateCardTemplate(Transform parent)
        {
            RectTransform rect = CreatePanel(parent, "Card Template", Vector2.zero, Vector2.one, new Color(.98f, .96f, .88f, 1f));
            rect.sizeDelta = new Vector2(380, 82);
            LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 82;
            TMP_Text label = CreateLabel(rect, "Card Label", "Thẻ chi tiết", new Vector2(.04f, .06f), new Vector2(.96f, .94f), 20, new Color(.12f, .15f, .15f), false, TextAlignmentOptions.MidlineLeft);
            DoctorInfoCardView view = rect.gameObject.AddComponent<DoctorInfoCardView>();
            SetReferences(view, ("background", rect.GetComponent<Image>()), ("label", label));
            return view;
        }

        private static DoctorQuestionItemView CreateQuestionTemplate(Transform parent)
        {
            RectTransform rect = CreatePanel(parent, "Question Item Template", Vector2.zero, Vector2.one, new Color(.08f, .15f, .17f, .96f));
            Image background = rect.GetComponent<Image>();
            background.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            background.type = Image.Type.Sliced;
            rect.sizeDelta = new Vector2(500, 40);
            LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 40;
            TMP_Text label = CreateLabel(rect, "Question Label", "Câu hỏi?", new Vector2(.05f, .08f), new Vector2(.95f, .92f), 21, Color.white, false, TextAlignmentOptions.Center);
            label.enableAutoSizing = true;
            label.fontSizeMin = 5f;
            label.fontSizeMax = 21f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            DoctorQuestionItemView view = rect.gameObject.AddComponent<DoctorQuestionItemView>();
            SetReferences(view, ("background", background), ("label", label), ("layoutElement", element));
            return view;
        }

        private static RectTransform CreatePanel(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            RectTransform rect = CreateRect(parent, name);
            SetAnchors(rect, min, max);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 min, Vector2 max, Color color, Color textColor)
        {
            RectTransform rect = CreatePanel(parent, name, min, max, color);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            CreateLabel(rect, "Label", label, Vector2.zero, Vector2.one, 26, textColor, true);
            return button;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, string value, Vector2 min, Vector2 max, float size, Color color, bool bold, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            RectTransform rect = CreateRect(parent, name);
            SetAnchors(rect, min, max);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
                text.font = TMP_Settings.defaultFontAsset;
            return text;
        }

        private static RectTransform CreateRect(Transform parent, string name)
        {
            GameObject target = new GameObject(name, typeof(RectTransform));
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Stretch(RectTransform rect) => SetAnchors(rect, Vector2.zero, Vector2.one);

        private static void EnsureEventSystem(Scene scene)
        {
            GameObject target = FindRoot(scene, "EventSystem") ?? new GameObject("EventSystem");
            GetOrAdd<EventSystem>(target);
            BaseInputModule[] modules = target.GetComponents<BaseInputModule>();
            for (int i = 0; i < modules.Length; i++)
                if (!(modules[i] is XRUIInputModule))
                    UnityEngine.Object.DestroyImmediate(modules[i]);
            GetOrAdd<XRUIInputModule>(target);
        }

        private static int EnsureLayer(string layerName)
        {
            int existing = LayerMask.NameToLayer(layerName);
            if (existing >= 0)
                return existing;
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty slot = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(slot.stringValue))
                {
                    slot.stringValue = layerName;
                    tagManager.ApplyModifiedPropertiesWithoutUndo();
                    return i;
                }
            }
            throw new InvalidOperationException("No free user layer is available for DoctorHUD.");
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = layer;
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(GameOneScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true)
            };
        }

        private static DoctorDialogue Dialogue(string transcript, params DoctorInfoCard[] cards) => new DoctorDialogue(transcript, null, cards);
        private static DoctorQuestionPhase Phase(string id, string question, string response, params DoctorInfoCard[] cards) => new DoctorQuestionPhase(id, question, Dialogue(response, cards));
        private static DoctorInfoCard Card(string id, string text, DoctorInfoCategory category) => new DoctorInfoCard(id, text, category, true);
        private static DoctorInfoCard Noise(string id, string text) => new DoctorInfoCard(id, text, DoctorInfoCategory.Time, false);

        private static GameObject CreatePlaceholderNpc()
        {
            GameObject npc = new GameObject("NPC");
            npc.transform.position = new Vector3(-1.154f, 0, 1.43f);
            GameObject model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            model.name = "Capsule";
            model.transform.SetParent(npc.transform, false);
            model.transform.localPosition = new Vector3(0, .85f, 0);
            return npc;
        }

        private static GameObject CreateMonitor(Scene scene)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonitorPrefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"Missing monitor prefab at '{MonitorPrefabPath}'.");

            GameObject monitor = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            monitor.name = "Monitor Variant";
            monitor.transform.SetPositionAndRotation(
                new Vector3(0.669f, 0.887f, 1.026f),
                Quaternion.Euler(0f, 90f, 0f));
            monitor.transform.localScale = Vector3.one * 1.2033f;
            return monitor;
        }

        private static Material EnsureUiOverlayMaterial()
        {
            Shader shader = Shader.Find(UiOverlayShaderName);
            if (shader == null)
                throw new InvalidOperationException($"Missing Doctor overlay shader '{UiOverlayShaderName}'.");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(UiOverlayMaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "Doctor UI Overlay" };
                AssetDatabase.CreateAsset(material, UiOverlayMaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureTmpOverlayMaterial()
        {
            Material source = TMP_Settings.defaultFontAsset != null ? TMP_Settings.defaultFontAsset.material : null;
            Shader shader = Shader.Find("TextMeshPro/Distance Field Overlay");
            if (source == null || shader == null)
                throw new InvalidOperationException("TextMesh Pro default font and overlay shader are required for the Doctor HUD.");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(TmpOverlayMaterialPath);
            if (material == null)
            {
                material = new Material(source) { name = "Doctor TMP Overlay" };
                AssetDatabase.CreateAsset(material, TmpOverlayMaterialPath);
            }
            else
            {
                material.CopyPropertiesFromMaterial(source);
            }

            material.shader = shader;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ApplyCoreHudPresentation(Transform core, Material uiOverlayMaterial, Material tmpOverlayMaterial)
        {
            Graphic[] graphics = core.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                graphic.raycastTarget = false;
                if (graphic is TMP_Text text)
                    text.fontSharedMaterial = tmpOverlayMaterial;
                else
                    graphic.material = uiOverlayMaterial;
                EditorUtility.SetDirty(graphic);
            }
        }

        private static void ConfigureRightRayVisual(XRInteractorLineVisual lineVisual)
        {
            lineVisual.lineWidth = 0.003f;
            lineVisual.invalidColorGradient = CreateRayGradient(
                new Color(.25f, .92f, .82f, .62f),
                new Color(.25f, .92f, .82f, .12f));
            lineVisual.validColorGradient = CreateRayGradient(
                new Color(.55f, 1f, .9f, .9f),
                new Color(.3f, .95f, .8f, .4f));
            lineVisual.enabled = true;
        }

        private static void IncludeLayerInRaycastMask(XRRayInteractor rayInteractor, int layer)
        {
            LayerMask mask = rayInteractor.raycastMask;
            mask.value |= 1 << layer;
            rayInteractor.raycastMask = mask;
        }

        private static void ConfigureRightRayScroll(XRRayInteractor rayInteractor)
        {
            SerializedObject serializedRay = new SerializedObject(rayInteractor);
            SerializedProperty scrollReference = serializedRay.FindProperty(
                "m_UIScrollInput.m_InputActionReference");
            if (scrollReference == null)
                throw new InvalidOperationException("Right hand Ray Interactor UI scroll input could not be configured.");
            scrollReference.objectReferenceValue = FindXriInputReference("XRI Left Interaction/UI Scroll");
            serializedRay.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Gradient CreateRayGradient(Color start, Color end)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(start, 0f),
                    new GradientColorKey(end, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(start.a, 0f),
                    new GradientAlphaKey(end.a, 1f)
                });
            return gradient;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            int split = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, split));
            AssetDatabase.CreateFolder(path.Substring(0, split), path.Substring(split + 1));
        }

        private static void DestroySceneObjectsNamed(Scene scene, string name)
        {
            List<GameObject> matches = new List<GameObject>();
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
                foreach (Transform child in sceneRoot.GetComponentsInChildren<Transform>(true))
                    if (child.name == name)
                        matches.Add(child.gameObject);
            foreach (GameObject match in matches)
                UnityEngine.Object.DestroyImmediate(match);
        }

        private static GameObject FindRoot(Scene scene, string name) => scene.GetRootGameObjects().FirstOrDefault(candidate => candidate.name == name);
        private static GameObject FindRequiredRoot(Scene scene, string name) => FindRoot(scene, name) ?? throw new InvalidOperationException($"Scene requires '{name}'.");
        private static Transform FindDescendant(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == name);
        private static Transform FindRequiredDescendant(Transform root, string name) => FindDescendant(root, name) ?? throw new InvalidOperationException($"'{root.name}' requires '{name}'.");
        private static T GetOrAdd<T>(GameObject target) where T : Component => target.GetComponent<T>() ?? target.AddComponent<T>();

        private static void SetReferences(UnityEngine.Object target, params (string property, UnityEngine.Object value)[] references)
        {
            SerializedObject serialized = new SerializedObject(target);
            foreach ((string property, UnityEngine.Object value) reference in references)
            {
                SerializedProperty property = serialized.FindProperty(reference.property) ?? throw new InvalidOperationException($"Missing '{reference.property}' on {target}.");
                property.objectReferenceValue = reference.value;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloatProperties(UnityEngine.Object target, params (string property, float value)[] values)
        {
            SerializedObject serialized = new SerializedObject(target);
            foreach ((string property, float value) item in values)
            {
                SerializedProperty property = serialized.FindProperty(item.property) ?? throw new InvalidOperationException($"Missing '{item.property}' on {target}.");
                property.floatValue = item.value;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
