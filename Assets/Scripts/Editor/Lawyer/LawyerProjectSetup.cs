using System;
using System.Collections.Generic;
using System.Linq;
using NCKH.VR.GameControl;
using NCKH.VR.Lawyer;
using NCKH.VR.NPC;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace NCKH.VR.Lawyer.Editor
{
    public static class LawyerProjectSetup
    {
        private const string OfficeScenePath = "Assets/Scenes/Lawyer/Office.unity";
        private const string CourtroomScenePath = "Assets/Scenes/Lawyer/Courtroom_Interior URP.unity";
        private const string StandbyScenePath = "Assets/Scenes/Standby.unity";
        private const string DataFolder = "Assets/Data/Lawyer";
        private const string PrefabFolder = "Assets/Prefabs/Lawyer";
        private const string MaterialFolder = "Assets/Materials/Lawyer";
        private const string CasePath = DataFolder + "/Placeholder Lawyer Case.asset";
        private const string CluePrefabPath = PrefabFolder + "/Lawyer Clue Card.prefab";
        private const string ControlsPath = "Assets/Input/Lawyer/Lawyer Controls.inputactions";
        private const string LawyerBoardLayerName = "LawyerBoard";
        private const string InteractionLayersPath = "Assets/XRI/Settings/Resources/InteractionLayerSettings.asset";
        private const string FocusDimMaterialPath = MaterialFolder + "/Lawyer Focus Dim.mat";
        private const string FocusMaskMaterialPath = MaterialFolder + "/Lawyer Focus Board Mask.mat";
        private const string MobileRendererPath = "Assets/Settings/Mobile_Renderer.asset";
        private const string PcRendererPath = "Assets/Settings/PC_Renderer.asset";
        private const uint LawyerForegroundRenderingLayerMask = 1u << 31;

        [MenuItem("NCKH VR/Lawyer/Build Phases 1-2")]
        public static void BuildPhasesOneAndTwo()
        {
            EnsureFolder(DataFolder);
            EnsureFolder(PrefabFolder);
            EnsureFolder(MaterialFolder);
            int boardUnityLayer = EnsureUnityLayer(LawyerBoardLayerName);
            int boardInteractionLayer = EnsureInteractionLayer(LawyerBoardLayerName);
            LawyerCaseDefinition caseDefinition = CreatePlaceholderCase();
            InputActionAsset controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            if (controls == null)
                throw new InvalidOperationException($"Không tìm thấy Input Action Asset: {ControlsPath}");

            Material cardMaterial = CreateMaterial(
                MaterialFolder + "/Lawyer Clue Card.mat", new Color(0.92f, 0.86f, 0.67f));
            Material pinMaterial = CreateMaterial(
                MaterialFolder + "/Lawyer String Pin.mat", new Color(0.72f, 0.04f, 0.03f));
            Material stringMaterial = CreateMaterial(
                MaterialFolder + "/Lawyer String.mat", new Color(0.72f, 0.04f, 0.03f),
                "Universal Render Pipeline/Unlit");
            Material focusDimMaterial = CreateShaderMaterial(
                FocusDimMaterialPath, "Hidden/NCKH VR/Lawyer/Focus Dim");
            Material focusMaskMaterial = CreateShaderMaterial(
                FocusMaskMaterialPath, "NCKH VR/Lawyer/Focus Board Mask");
            EnsureFocusDimRendererFeature(
                MobileRendererPath, focusDimMaterial, boardUnityLayer);
            EnsureFocusDimRendererFeature(
                PcRendererPath, focusDimMaterial, boardUnityLayer);
            LawyerBoardClue cluePrefab = CreateCluePrefab(
                cardMaterial, pinMaterial, boardUnityLayer, boardInteractionLayer);

            ConfigureOffice(
                caseDefinition, controls, cluePrefab, stringMaterial, focusMaskMaterial,
                boardUnityLayer, boardInteractionLayer);
            ConfigureCourtroom(caseDefinition, controls);
            ConfigureBuildSettings();
            ConfigureStandbyCatalog();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(OfficeScenePath, OpenSceneMode.Single);
            Debug.Log("Đã xây dựng hai giai đoạn đầu của minigame luật sư.");
        }

        [MenuItem("NCKH VR/Lawyer/Repair Office Clue Board")]
        public static void RepairOfficeClueBoard()
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(MaterialFolder);
            int boardUnityLayer = EnsureUnityLayer(LawyerBoardLayerName);
            int boardInteractionLayer = EnsureInteractionLayer(LawyerBoardLayerName);
            LawyerCaseDefinition caseDefinition = AssetDatabase.LoadAssetAtPath<LawyerCaseDefinition>(CasePath);
            InputActionAsset controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            if (caseDefinition == null || controls == null)
                throw new InvalidOperationException("Thiếu dữ liệu vụ án hoặc Input Action của cảnh Office.");

            Material cardMaterial = CreateMaterial(
                MaterialFolder + "/Lawyer Clue Card.mat", new Color(0.92f, 0.86f, 0.67f));
            Material pinMaterial = CreateMaterial(
                MaterialFolder + "/Lawyer String Pin.mat", new Color(0.72f, 0.04f, 0.03f));
            Material stringMaterial = CreateMaterial(
                MaterialFolder + "/Lawyer String.mat", new Color(0.72f, 0.04f, 0.03f),
                "Universal Render Pipeline/Unlit");
            Material focusDimMaterial = CreateShaderMaterial(
                FocusDimMaterialPath, "Hidden/NCKH VR/Lawyer/Focus Dim");
            Material focusMaskMaterial = CreateShaderMaterial(
                FocusMaskMaterialPath, "NCKH VR/Lawyer/Focus Board Mask");
            EnsureFocusDimRendererFeature(
                MobileRendererPath, focusDimMaterial, boardUnityLayer);
            EnsureFocusDimRendererFeature(
                PcRendererPath, focusDimMaterial, boardUnityLayer);
            LawyerBoardClue cluePrefab = CreateCluePrefab(
                cardMaterial, pinMaterial, boardUnityLayer, boardInteractionLayer);

            ConfigureOffice(
                caseDefinition, controls, cluePrefab, stringMaterial, focusMaskMaterial,
                boardUnityLayer, boardInteractionLayer);
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(OfficeScenePath, OpenSceneMode.Single);
            Debug.Log("Đã sửa tương tác bảng manh mối trong cảnh Office.");
        }

        [MenuItem("NCKH VR/Lawyer/Apply Defense Recording Stage (Targeted)")]
        public static void ApplyDefenseRecordingStageTargeted()
        {
            LawyerCaseDefinition caseDefinition =
                AssetDatabase.LoadAssetAtPath<LawyerCaseDefinition>(CasePath);
            InputActionAsset controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            if (caseDefinition == null || controls == null)
                throw new InvalidOperationException(
                    "Thiếu dữ liệu vụ án hoặc Input Action của trò chơi luật sư.");

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                ApplyOfficeDefenseStageTargeted();
                ApplyCourtroomDefenseStageTargeted(caseDefinition, controls);
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Đã thêm giai đoạn ghi âm bào chữa mà không tái tạo cảnh.");
        }

        [MenuItem("NCKH VR/Lawyer/Migrate Reasoning Board (Targeted)")]
        public static void MigrateReasoningBoardTargeted()
        {
            LawyerCaseDefinition caseDefinition =
                AssetDatabase.LoadAssetAtPath<LawyerCaseDefinition>(CasePath);
            if (caseDefinition == null)
                throw new InvalidOperationException("Thiếu hồ sơ vụ án luật sư.");
            EnsureExplicitReasoningCards(caseDefinition);
            EnsureFullBriefClueText(caseDefinition);

            Scene scene = EditorSceneManager.OpenScene(OfficeScenePath, OpenSceneMode.Single);
            LawyerBoardController controller = FindComponentInScene<LawyerBoardController>(scene);
            if (controller == null)
                throw new InvalidOperationException("Cảnh Office thiếu bảng luật sư.");
            GameObject board = controller.gameObject;
            Transform content = board.transform.Find("Lawyer Board Content");
            Transform surface = content != null ? content.Find("Board Surface") : null;
            Transform clueContainer = surface != null ? surface.Find("Clue Container") : null;
            Transform template = clueContainer != null ? clueContainer.Find("Clue Card Template") : null;
            if (content == null || surface == null || clueContainer == null || template == null)
                throw new InvalidOperationException("Cảnh Office thiếu cấu trúc bảng chứng cứ.");

            int boardLayer = board.layer;
            RemoveGeneratedChild(content, "Physical Objective Sheet");
            RemoveGeneratedChild(content, "Physical Objective Sheet Text");
            RemoveGeneratedChild(content, "Physical Timeline Sheet");
            RemoveGeneratedChild(content, "Physical Timeline Sheet Text");
            RemoveGeneratedChild(content, "Physical Witness Sheet");
            RemoveGeneratedChild(content, "Physical Witness Sheet Text");
            RemoveGeneratedChild(content, "Physical Details Sheet");
            RemoveGeneratedChild(content, "Physical Details Sheet Text");
            RemoveGeneratedChild(content, "Physical Status Plate");
            RemoveGeneratedChild(content, "Physical Status Plate Text");
            RemoveGeneratedChild(content, "Board Defense Brief");
            RemoveGeneratedChild(content, "Board Defense Brief Text");
            RemoveGeneratedChild(content, "Board Toast Canvas");

            GameObject evidenceSurface = FindInScene(scene, "Physical Evidence Surface");
            if (evidenceSurface != null && evidenceSurface.transform.IsChildOf(content))
            {
                evidenceSurface.transform.localPosition = new Vector3(0f, 0.025f, 0.003f);
                evidenceSurface.transform.localScale = new Vector3(1.14f, 0.62f, 0.018f);
            }

            Material reasoningMaterial = CreateMaterial(
                MaterialFolder + "/Lawyer Reasoning Card.mat",
                new Color(0.88f, 0.12f, 0.36f));
            LawyerBoardClue cardTemplate = template.GetComponent<LawyerBoardClue>();
            ConfigureCardText(cardTemplate, reasoningMaterial);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(CluePrefabPath);
            try
            {
                ConfigureCardText(prefabRoot.GetComponent<LawyerBoardClue>(), reasoningMaterial);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, CluePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            Camera boardCamera = FindComponentInScene<Camera>(scene);
            Transform cameraFinale = boardCamera != null
                ? boardCamera.transform.Find("Lawyer Finale Canvas")
                : null;
            if (cameraFinale != null)
                RemoveGeneratedChild(cameraFinale, "Board Toast Canvas");
            Transform toastParent = content;
            CanvasGroup toastGroup = EnsureBoardToastCanvas(toastParent, boardLayer);
            TMP_Text toastText = toastGroup.transform.Find("Toast Panel/Toast Text")
                ?.GetComponent<TMP_Text>();
            if (toastText == null)
                throw new InvalidOperationException("Không thể tạo bảng thông báo tạm thời.");
            toastGroup.alpha = 1f;
            toastGroup.gameObject.SetActive(false);
            TMP_Text defenseInstruction = cameraFinale
                .Find("Evidence Result Overlay/Defense Instruction")?.GetComponent<TMP_Text>();
            if (defenseInstruction != null)
                defenseInstruction.text =
                    "Hãy dùng các mối liên hệ giữa những manh mối đã tìm được và sự thật "
                    + "để trình bày lời bào chữa cho bị cáo.";

            SerializedObject boardObject = new SerializedObject(controller);
            Assign(boardObject, "caseDefinition", caseDefinition);
            Assign(boardObject, "reasoningCardMaterial", reasoningMaterial);
            Assign(boardObject, "statusLabel", toastText);
            Assign(boardObject, "boardDefenseBriefLabel", null);
            Assign(boardObject, "boardDefenseBriefPanel", null);
            Assign(boardObject, "toastGroup", toastGroup);
            Assign(boardObject, "objectiveLabel", null);
            Assign(boardObject, "timelineLabel", null);
            Assign(boardObject, "witnessesLabel", null);
            Assign(boardObject, "detailsLabel", null);
            boardObject.FindProperty("truthDisplayDuration").floatValue = 5f;
            boardObject.FindProperty("toastDuration").floatValue = 1.5f;
            boardObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Đã di chuyển thông tin lên thẻ chứng cứ và thêm thẻ suy luận vật lý.");
        }

        private static void EnsureExplicitReasoningCards(LawyerCaseDefinition asset)
        {
            LawyerCaseResolution resolution = asset.Resolution;
            if (resolution == null || (resolution.ReasoningCards != null
                && resolution.ReasoningCards.Count > 0))
                return;
            LawyerReasoningCardDefinition[] cards = new[]
            {
                new LawyerReasoningCardDefinition(
                    "reasoning-time", "Suy luận về mốc thời gian",
                    "Lời khai rời hiện trường trước 20:00 mâu thuẫn với hình ảnh camera sau 20:10.",
                    new[] { new LawyerRequiredClueLink("testimony-time", "contradiction-camera") }),
                new LawyerReasoningCardDefinition(
                    "reasoning-phone", "Suy luận về cuộc gọi",
                    "Lời khai điện thoại hết pin mâu thuẫn với nhật ký cuộc gọi lúc 20:12.",
                    new[] { new LawyerRequiredClueLink("testimony-phone", "contradiction-call") })
            };
            asset.ConfigureInEditor(
                asset.CaseId, asset.CaseTitle, asset.CaseSummary, asset.InvestigationObjective,
                asset.Timeline, asset.Witnesses, asset.CaseDetails, asset.Clues,
                asset.StartingClueIds, asset.DefendantName, asset.Questions,
                new LawyerCaseResolution(
                    resolution.RequiredLinks, resolution.TruthConclusion,
                    resolution.InsufficientEvidenceMessage, cards));
            EditorUtility.SetDirty(asset);
        }

        private static void EnsureFullBriefClueText(LawyerCaseDefinition asset)
        {
            List<LawyerClueDefinition> clues = new List<LawyerClueDefinition>();
            for (int index = 0; index < asset.Clues.Count; index++)
            {
                LawyerClueDefinition clue = asset.Clues[index];
                if (clue == null)
                    continue;
                string description = clue.Description;
                if (clue.Id == "brief-time")
                    description = "TÓM TẮT VỤ ÁN\n" + asset.CaseSummary
                        + "\n\nMỤC TIÊU\n" + asset.InvestigationObjective;
                else if (clue.Id == "brief-witness")
                    description = "NHÂN CHỨNG\n" + JoinBullets(asset.Witnesses);
                else if (clue.Id == "brief-detail")
                    description = "CHI TIẾT VỤ ÁN\n" + JoinBullets(asset.CaseDetails);
                else if (clue.Id == "brief-objective")
                    description = "DÒNG THỜI GIAN\n" + JoinBullets(asset.Timeline);
                clues.Add(new LawyerClueDefinition(clue.Id, clue.Title, description, clue.Category));
            }
            asset.ConfigureInEditor(
                asset.CaseId, asset.CaseTitle, asset.CaseSummary, asset.InvestigationObjective,
                asset.Timeline, asset.Witnesses, asset.CaseDetails, clues,
                asset.StartingClueIds, asset.DefendantName, asset.Questions, asset.Resolution);
            EditorUtility.SetDirty(asset);
        }

        private static string JoinBullets(IReadOnlyList<string> items)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int index = 0; index < items.Count; index++)
                builder.Append("• ").AppendLine(items[index]);
            return builder.ToString().TrimEnd();
        }

        private static void ConfigureCardText(
            LawyerBoardClue cardTemplate, Material reasoningMaterial)
        {
            if (cardTemplate == null)
                return;
            TMP_Text[] labels = cardTemplate.GetComponentsInChildren<TMP_Text>(true);
            for (int index = 0; index < labels.Length; index++)
            {
                TMP_Text label = labels[index];
                label.enableAutoSizing = label.name == "Title";
                label.fontSizeMin = 18f;
                label.fontSizeMax = 72f;
                if (label.name == "Body")
                {
                    label.fontSize = 14f;
                    label.fontSizeMin = 14f;
                    label.fontSizeMax = 14f;
                }
                if (label.name == "Title")
                    label.rectTransform.sizeDelta = new Vector2(24f, 4f);
                else if (label.name == "Body")
                    label.rectTransform.sizeDelta = new Vector2(24f, 9f);
                EditorUtility.SetDirty(label);
            }
            SerializedObject cardObject = new SerializedObject(cardTemplate);
            Assign(cardObject, "cardRenderer",
                cardTemplate.transform.Find("Card Visual")?.GetComponent<Renderer>());
            Assign(cardObject, "cardVisual", cardTemplate.transform.Find("Card Visual"));
            Assign(cardObject, "cardCollider", cardTemplate.GetComponent<BoxCollider>());
            cardObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(cardTemplate);
        }

        private static CanvasGroup EnsureBoardToastCanvas(Transform parent, int layer)
        {
            Transform existing = parent.Find("Board Toast Canvas");
            if (existing != null)
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            GameObject root = new GameObject("Board Toast Canvas", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rootRect = (RectTransform)root.transform;
            rootRect.localPosition = new Vector3(0f, -0.19f, 0.18f);
            rootRect.localRotation = Quaternion.Euler(0f, 180f, 0f);
            rootRect.localScale = Vector3.one * 0.001f;
            rootRect.sizeDelta = new Vector2(920f, 120f);
            SetLayerRecursively(root, layer);
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 200;
            CanvasGroup group = root.AddComponent<CanvasGroup>();
            GameObject panel = CreatePanel(root.transform, "Toast Panel");
            Image image = CreateImage(panel.transform, "Background", new Color(0.04f, 0.12f, 0.11f, 0.94f));
            Stretch((RectTransform)image.transform);
            TMP_Text text = CreateUiText(
                panel.transform, "Toast Text", Vector2.zero, new Vector2(860f, 92f),
                28f, TextAlignmentOptions.Center);
            text.color = Color.white;
            return group;
        }

        private static void ApplyOfficeDefenseStageTargeted()
        {
            Scene scene = EditorSceneManager.OpenScene(OfficeScenePath, OpenSceneMode.Single);
            LawyerBoardController controller = FindComponentInScene<LawyerBoardController>(scene);
            Camera camera = FindComponentInScene<Camera>(scene);
            if (controller == null || camera == null)
                throw new InvalidOperationException("Cảnh Office thiếu bảng luật sư hoặc camera.");

            Transform finaleTransform = camera.transform.Find("Lawyer Finale Canvas");
            Transform resultPanelTransform = finaleTransform != null
                ? finaleTransform.Find("Evidence Result Overlay")
                : null;
            if (finaleTransform == null || resultPanelTransform == null)
                throw new InvalidOperationException("Cảnh Office thiếu giao diện kết quả manh mối.");

            GetOrAdd<TrackedDeviceGraphicRaycaster>(finaleTransform.gameObject);
            TMP_Text resultTitle = resultPanelTransform.Find("Result Title")
                ?.GetComponent<TMP_Text>();
            TMP_Text resultBody = resultPanelTransform.Find("Result Body")
                ?.GetComponent<TMP_Text>();
            if (resultTitle == null || resultBody == null)
                throw new InvalidOperationException("Giao diện kết quả thiếu tiêu đề hoặc nội dung sự thật.");

            ConfigureUiText(resultTitle, new Vector2(0f, 235f),
                new Vector2(920f, 100f), 58f, TextAlignmentOptions.Center);
            ConfigureUiText(resultBody, new Vector2(0f, 75f),
                new Vector2(870f, 190f), 34f, TextAlignmentOptions.Center);
            TMP_Text instruction = EnsureUiText(
                resultPanelTransform, "Defense Instruction", new Vector2(0f, -125f),
                new Vector2(870f, 150f), 31f, TextAlignmentOptions.Center);
            instruction.text =
                "Hãy dùng các mối liên hệ giữa những manh mối đã tìm được và sự thật "
                + "để trình bày lời bào chữa cho bị cáo.";
            instruction.rectTransform.localScale = Vector3.one * 0.75f;
            Button startButton = EnsureButton(
                resultPanelTransform, "Start Defense Button", "Bắt đầu bào chữa",
                new Vector2(0f, -235f), new Vector2(340f, 70f));
            startButton.gameObject.SetActive(false);
            SetLayerRecursively(finaleTransform.gameObject, finaleTransform.gameObject.layer);
            ApplyOverlayMaterials(resultPanelTransform);

            SerializedObject boardObject = new SerializedObject(controller);
            Assign(boardObject, "defenseInstructionLabel", instruction);
            Assign(boardObject, "startDefenseButton", startButton);
            boardObject.FindProperty("truthDisplayDuration").floatValue = 5f;
            boardObject.FindProperty("truthFinalPosition").vector2Value = new Vector2(0f, 115f);
            boardObject.FindProperty("instructionFinalPosition").vector2Value = Vector2.zero;
            boardObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ApplyCourtroomDefenseStageTargeted(
            LawyerCaseDefinition caseDefinition, InputActionAsset controls)
        {
            Scene scene = EditorSceneManager.OpenScene(CourtroomScenePath, OpenSceneMode.Single);
            LawyerInterviewController interview = FindComponentInScene<LawyerInterviewController>(scene);
            Camera camera = FindComponentInScene<Camera>(scene);
            if (interview == null || camera == null)
                throw new InvalidOperationException("Cảnh phòng xử án thiếu phần xét hỏi hoặc camera.");

            Transform interviewCanvasTransform = camera.transform.Find("Lawyer Interview Canvas");
            if (interviewCanvasTransform == null)
                throw new InvalidOperationException("Cảnh phòng xử án thiếu giao diện xét hỏi.");

            Canvas defenseCanvas = EnsureWorldCanvas(
                camera.transform, "Lawyer Defense Canvas", new Vector2(1000f, 700f),
                0.001f, 210);
            defenseCanvas.transform.localPosition = new Vector3(0f, 0f, 1.2f);
            defenseCanvas.transform.localRotation = Quaternion.identity;
            Image background = EnsureImage(
                defenseCanvas.transform, "Defense Background",
                new Color(0.025f, 0.045f, 0.04f, 0.94f));
            Stretch(background.rectTransform);
            TMP_Text title = EnsureUiText(
                defenseCanvas.transform, "Defense Title", new Vector2(0f, 250f),
                new Vector2(900f, 80f), 50f, TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            TMP_Text countdown = EnsureUiText(
                defenseCanvas.transform, "Defense Countdown", new Vector2(0f, 70f),
                new Vector2(500f, 190f), 130f, TextAlignmentOptions.Center);
            TMP_Text instruction = EnsureUiText(
                defenseCanvas.transform, "Defense Instruction", new Vector2(0f, -95f),
                new Vector2(860f, 110f), 32f, TextAlignmentOptions.Center);
            TMP_Text truth = EnsureUiText(
                defenseCanvas.transform, "Defense Truth", new Vector2(0f, 150f),
                new Vector2(860f, 120f), 28f, TextAlignmentOptions.Center);
            truth.gameObject.SetActive(false);
            TMP_Text status = EnsureUiText(
                defenseCanvas.transform, "Defense Status", new Vector2(0f, -235f),
                new Vector2(860f, 70f), 26f, TextAlignmentOptions.Center);
            ApplyOverlayMaterials(defenseCanvas.transform);

            LawyerDefenseController defenseController =
                GetOrAdd<LawyerDefenseController>(interview.gameObject);
            defenseController.ConfigureInEditor(
                caseDefinition, controls, interview, interviewCanvasTransform.gameObject,
                defenseCanvas.gameObject);
            SerializedObject defenseObject = new SerializedObject(defenseController);
            Assign(defenseObject, "titleLabel", title);
            Assign(defenseObject, "countdownLabel", countdown);
            Assign(defenseObject, "instructionLabel", instruction);
            Assign(defenseObject, "statusLabel", status);
            Assign(defenseObject, "truthLabel", truth);
            defenseObject.FindProperty("preparationDuration").floatValue = 5f;
            defenseObject.FindProperty("recordingDuration").floatValue = 30f;
            defenseObject.FindProperty("uploadAttempts").intValue = 5;
            defenseObject.ApplyModifiedPropertiesWithoutUndo();
            defenseCanvas.gameObject.SetActive(false);
            EditorUtility.SetDirty(defenseController);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static LawyerCaseDefinition CreatePlaceholderCase()
        {
            LawyerCaseDefinition asset = AssetDatabase.LoadAssetAtPath<LawyerCaseDefinition>(CasePath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<LawyerCaseDefinition>();
                AssetDatabase.CreateAsset(asset, CasePath);
            }

            List<LawyerClueDefinition> clues = new List<LawyerClueDefinition>
            {
                new LawyerClueDefinition("brief-time", "Mốc thời gian mẫu", "[NỘI DUNG MẪU] Sự việc được ghi nhận lúc 20:15.", LawyerClueCategory.Timeline),
                new LawyerClueDefinition("brief-witness", "Nhân chứng mẫu", "[NỘI DUNG MẪU] Nhân chứng A có mặt gần hiện trường.", LawyerClueCategory.Witness),
                new LawyerClueDefinition("brief-detail", "Chi tiết hiện trường", "[NỘI DUNG MẪU] Một vật chứng chưa xác định được tìm thấy.", LawyerClueCategory.CaseDetail),
                new LawyerClueDefinition("brief-objective", "Mục tiêu điều tra", "[NỘI DUNG MẪU] Xác minh lời khai và mốc thời gian của bị cáo.", LawyerClueCategory.Objective),
                new LawyerClueDefinition("testimony-time", "Lời khai về thời gian", "Bị cáo nói đã rời địa điểm trước 20:00.", LawyerClueCategory.Testimony),
                new LawyerClueDefinition("contradiction-camera", "Mâu thuẫn camera", "Camera mẫu ghi nhận một người tương tự bị cáo sau 20:10.", LawyerClueCategory.Contradiction),
                new LawyerClueDefinition("testimony-phone", "Lời khai về điện thoại", "Bị cáo nói điện thoại đã hết pin trong buổi tối.", LawyerClueCategory.Testimony),
                new LawyerClueDefinition("contradiction-call", "Mâu thuẫn cuộc gọi", "Nhật ký mẫu ghi nhận một cuộc gọi lúc 20:12.", LawyerClueCategory.Contradiction),
                new LawyerClueDefinition("follow-route", "Tuyến đường mẫu", "Lời khai về tuyến đường có một đoạn chưa được giải thích.", LawyerClueCategory.Testimony),
                new LawyerClueDefinition("follow-receipt", "Biên lai mẫu", "Thời gian trên biên lai không khớp hoàn toàn với lời khai.", LawyerClueCategory.Contradiction)
            };

            LawyerInterviewQuestion keyTime = new LawyerInterviewQuestion(
                "main-time",
                "Ông/bà đã ở đâu trong khoảng 20:00 đến 20:20?",
                true,
                new LawyerTestimony(
                    "[NỘI DUNG MẪU] Tôi đã rời khu vực trước 20:00. Sau đó tôi ghé mua nước, nhưng không nhớ tên cửa hàng. Tôi chắc mình không quay lại.",
                    null,
                    new[] { "testimony-time", "contradiction-camera" }),
                new[]
                {
                    new LawyerFollowUpQuestion("time-route", "Ông/bà đã đi theo tuyến đường nào?", new LawyerTestimony("[NỘI DUNG MẪU] Tôi đi qua đường phía đông rồi đổi hướng vì tắc đường.", null, new[] { "follow-route" })),
                    new LawyerFollowUpQuestion("time-camera", "Vì sao camera vẫn ghi nhận hình ảnh sau 20:10?", new LawyerTestimony("[NỘI DUNG MẪU] Người trong camera có thể chỉ mặc quần áo giống tôi.", null, new[] { "contradiction-camera" })),
                    new LawyerFollowUpQuestion("time-receipt", "Ông/bà còn giữ biên lai mua hàng không?", new LawyerTestimony("[NỘI DUNG MẪU] Tôi có một biên lai, nhưng giờ in trên đó có thể bị sai.", null, new[] { "follow-receipt" }))
                });

            LawyerInterviewQuestion keyPhone = new LawyerInterviewQuestion(
                "main-phone",
                "Điện thoại của ông/bà hoạt động thế nào trong tối hôm đó?",
                true,
                new LawyerTestimony(
                    "[NỘI DUNG MẪU] Điện thoại của tôi hết pin từ sớm nên tôi không gọi cho ai. Tôi để máy trong túi suốt buổi tối.",
                    null,
                    new[] { "testimony-phone", "contradiction-call" }),
                new[]
                {
                    new LawyerFollowUpQuestion("phone-call", "Ông/bà giải thích thế nào về cuộc gọi lúc 20:12?", new LawyerTestimony("[NỘI DUNG MẪU] Có thể cuộc gọi được thực hiện trước khi máy tắt hẳn.", null, new[] { "contradiction-call" })),
                    new LawyerFollowUpQuestion("phone-owner", "Có ai khác sử dụng điện thoại của ông/bà không?", new LawyerTestimony("[NỘI DUNG MẪU] Tôi không đưa điện thoại cho ai, nhưng có để nó trên bàn vài phút.", null, new[] { "testimony-phone" })),
                    new LawyerFollowUpQuestion("phone-location", "Điện thoại ở đâu khi ông/bà mua nước?", new LawyerTestimony("[NỘI DUNG MẪU] Tôi nghĩ nó ở trong túi áo, nhưng tôi không kiểm tra.", null, new[] { "follow-route" }))
                });

            List<LawyerInterviewQuestion> questions = new List<LawyerInterviewQuestion>
            {
                keyTime,
                new LawyerInterviewQuestion("main-hobby", "Ông/bà thường làm gì vào cuối tuần?", false, new LawyerTestimony("[NỘI DUNG MẪU] Tôi thường xem phim và dọn nhà. Điều này không liên quan đến tối hôm đó.", null, null), null),
                keyPhone,
                new LawyerInterviewQuestion("main-food", "Món ăn yêu thích của ông/bà là gì?", false, new LawyerTestimony("[NỘI DUNG MẪU] Tôi thích món ăn gia đình. Tôi không nhớ mình đã ăn gì tối hôm đó.", null, null), null),
                new LawyerInterviewQuestion("main-weather", "Ông/bà cảm thấy thời tiết hôm đó thế nào?", false, new LawyerTestimony("[NỘI DUNG MẪU] Trời hơi nóng và có gió. Tôi không chú ý thêm chi tiết nào.", null, null), null)
            };

            asset.ConfigureInEditor(
                "lawyer-placeholder",
                "[NỘI DUNG MẪU] HỒ SƠ VỤ ÁN",
                "[NỘI DUNG MẪU] Một sự việc giả định đang được điều tra. Toàn bộ tên, địa điểm và tình tiết sẽ được thay thế bằng nội dung chính thức.",
                "Xác minh hai mâu thuẫn quan trọng trong lời khai của bị cáo và thu thập manh mối cho bảng chứng cứ.",
                new[] { "20:00 — Bị cáo khai đã rời khu vực.", "20:10 — Camera mẫu ghi nhận chuyển động.", "20:15 — Nhân chứng A báo cáo sự việc." },
                new[] { "Nhân chứng A — có mặt gần hiện trường.", "Nhân chứng B — cung cấp thông tin nhiễu cần kiểm chứng." },
                new[] { "Vật chứng mẫu chưa xác định nguồn gốc.", "Nhật ký liên lạc có một mốc thời gian cần đối chiếu." },
                clues,
                new[] { "brief-time", "brief-witness", "brief-detail", "brief-objective" },
                "BỊ CÁO MẪU",
                questions,
                new LawyerCaseResolution(
                    new[]
                    {
                        new LawyerRequiredClueLink("testimony-time", "contradiction-camera"),
                        new LawyerRequiredClueLink("testimony-phone", "contradiction-call")
                    },
                    "Vì không có chứng cứ đáng tin cậy xác nhận bị cáo có mặt tại hiện trường vào thời điểm xảy ra sự việc, nên chưa đủ căn cứ quy trách nhiệm cho bị cáo.",
                    "Bạn chưa thu thập đủ manh mối để xác định sự thật. Hãy thử lại và lựa chọn câu hỏi liên quan đến thời gian và điện thoại.",
                    new[]
                    {
                        new LawyerReasoningCardDefinition(
                            "reasoning-time",
                            "Suy luận về mốc thời gian",
                            "Lời khai rời hiện trường trước 20:00 mâu thuẫn với hình ảnh camera sau 20:10.",
                            new[] { new LawyerRequiredClueLink("testimony-time", "contradiction-camera") }),
                        new LawyerReasoningCardDefinition(
                            "reasoning-phone",
                            "Suy luận về cuộc gọi",
                            "Lời khai điện thoại hết pin mâu thuẫn với nhật ký cuộc gọi lúc 20:12.",
                            new[] { new LawyerRequiredClueLink("testimony-phone", "contradiction-call") })
                    }));
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static LawyerBoardClue CreateCluePrefab(
            Material cardMaterial,
            Material pinMaterial,
            int boardUnityLayer,
            int boardInteractionLayer)
        {
            GameObject root = new GameObject("Lawyer Clue Card");
            root.layer = boardUnityLayer;
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.28f, 0.14f, 0.025f);
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            XRGrabInteractable cardGrab = root.AddComponent<XRGrabInteractable>();
            cardGrab.colliders.Clear();
            cardGrab.colliders.Add(collider);
            cardGrab.throwOnDetach = false;
            SetInteractionLayerMask(cardGrab, 1 << boardInteractionLayer);
            LawyerBoardClue clue = root.AddComponent<LawyerBoardClue>();

            GameObject frontAttach = new GameObject("Front Attach");
            frontAttach.layer = boardUnityLayer;
            frontAttach.transform.SetParent(root.transform, false);
            frontAttach.transform.localPosition = new Vector3(0f, 0f, 0.016f);
            cardGrab.attachTransform = frontAttach.transform;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Card Visual";
            visual.transform.SetParent(root.transform, false);
            visual.layer = boardUnityLayer;
            visual.transform.localScale = new Vector3(0.28f, 0.14f, 0.015f);
            UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.GetComponent<MeshRenderer>().sharedMaterial = cardMaterial;

            TMP_Text title = CreateWorldText(root.transform, "Title", new Vector3(0f, 0.035f, 0.011f), new Vector2(24f, 4f), 12f, TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            title.enableAutoSizing = true;
            title.fontSizeMin = 18f;
            title.fontSizeMax = 72f;
            TMP_Text bodyLabel = CreateWorldText(root.transform, "Body", new Vector3(0f, -0.018f, 0.011f), new Vector2(24f, 9f), 8f, TextAlignmentOptions.TopLeft);
            bodyLabel.enableAutoSizing = false;
            bodyLabel.fontSize = 14f;
            bodyLabel.fontSizeMin = 14f;
            bodyLabel.fontSizeMax = 14f;

            GameObject pin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pin.name = "String Pin";
            pin.transform.SetParent(root.transform, false);
            pin.layer = boardUnityLayer;
            pin.transform.localPosition = new Vector3(0.115f, 0.05f, 0.025f);
            pin.transform.localScale = Vector3.one * 0.025f;
            pin.GetComponent<MeshRenderer>().sharedMaterial = pinMaterial;
            SphereCollider pinCollider = pin.GetComponent<SphereCollider>();
            XRSimpleInteractable pinInteractable = pin.AddComponent<XRSimpleInteractable>();
            pinInteractable.colliders.Clear();
            pinInteractable.colliders.Add(pinCollider);
            SetInteractionLayerMask(pinInteractable, 1 << boardInteractionLayer);
            pin.AddComponent<LawyerCluePin>();

            SerializedObject serialized = new SerializedObject(clue);
            serialized.FindProperty("titleLabel").objectReferenceValue = title;
            serialized.FindProperty("bodyLabel").objectReferenceValue = bodyLabel;
            serialized.FindProperty("cardRenderer").objectReferenceValue = visual.GetComponent<Renderer>();
            serialized.FindProperty("cardVisual").objectReferenceValue = visual.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, CluePrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<LawyerBoardClue>(CluePrefabPath);
        }

        private static void ConfigureOffice(
            LawyerCaseDefinition caseDefinition,
            InputActionAsset controls,
            LawyerBoardClue cluePrefab,
            Material stringMaterial,
            Material focusMaskMaterial,
            int boardUnityLayer,
            int boardInteractionLayer)
        {
            Scene scene = EditorSceneManager.OpenScene(OfficeScenePath, OpenSceneMode.Single);
            GameObject player = FindInScene(scene, "VR Player");
            if (player == null)
                throw new InvalidOperationException("Cảnh Office không có VR Player.");
            ConfigureStationaryRig(player);
            EnsureInteractionInfrastructure(scene);

            GameObject boardVisual = FindInScene(scene, "Clue Board");
            if (boardVisual == null)
                throw new InvalidOperationException("Cảnh Office không có Clue Board.");
            GameObject previousWrapper = FindInScene(scene, "Lawyer Case Board");
            Vector3 clueTemplateScale = Vector3.one;
            if (previousWrapper != null)
            {
                Transform previousTemplate = previousWrapper.transform.Find(
                    "Lawyer Board Content/Board Surface/Clue Container/Clue Card Template");
                if (previousTemplate != null)
                    clueTemplateScale = previousTemplate.localScale;
                if (boardVisual.transform.IsChildOf(previousWrapper.transform))
                    boardVisual.transform.SetParent(null, true);
                UnityEngine.Object.DestroyImmediate(previousWrapper);
            }
            clueTemplateScale = Vector3.one * 0.82f;

            Vector3 boardPosition = boardVisual.transform.position;
            Vector3 visualWorldScale = boardVisual.transform.lossyScale;
            Camera playerCamera = player.GetComponentInChildren<Camera>(true);
            if (playerCamera == null)
                throw new InvalidOperationException("Cảnh Office không có camera của người chơi.");
            RemoveGeneratedChild(playerCamera.transform, "Lawyer Finale Canvas");
            Vector3 boardToPlayer = playerCamera != null
                ? Vector3.ProjectOnPlane(playerCamera.transform.position - boardPosition, Vector3.up)
                : Vector3.back;
            Quaternion boardRotation = boardToPlayer.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(boardToPlayer.normalized, Vector3.up)
                : Quaternion.identity;

            GameObject board = new GameObject("Lawyer Case Board");
            board.layer = boardUnityLayer;
            SceneManager.MoveGameObjectToScene(board, scene);
            board.transform.SetPositionAndRotation(boardPosition, boardRotation);

            // The imported mesh is authored on its local X/Z plane with +Y as its normal.
            // The lawyer board runtime uses local X/Y as the surface and +Z as its normal.
            // Normalize the visual once here so every generated child can use the runtime frame.
            boardVisual.transform.SetParent(board.transform, false);
            boardVisual.transform.localPosition = Vector3.zero;
            boardVisual.transform.localRotation = Quaternion.LookRotation(Vector3.right, Vector3.forward);
            boardVisual.transform.localScale = visualWorldScale;
            SetLayerRecursively(boardVisual, boardUnityLayer);
            RemoveGeneratedChild(board.transform, "Lawyer Board Content");
            BoxCollider boardCollider = board.AddComponent<BoxCollider>();
            boardCollider.center = new Vector3(0f, 0.025f, 0f);
            boardCollider.size = new Vector3(1.2f, 0.67f, 0.08f);
            XRSimpleInteractable boardInteractable = board.AddComponent<XRSimpleInteractable>();
            SetInteractionLayerMask(boardInteractable, 1 << boardInteractionLayer);
            SerializedObject boardInteractableObject = new SerializedObject(boardInteractable);
            SerializedProperty boardColliders = boardInteractableObject.FindProperty("m_Colliders");
            boardColliders.arraySize = 1;
            boardColliders.GetArrayElementAtIndex(0).objectReferenceValue = boardCollider;
            boardInteractableObject.ApplyModifiedPropertiesWithoutUndo();
            LawyerBoardController controller = board.AddComponent<LawyerBoardController>();

            GameObject content = new GameObject("Lawyer Board Content");
            content.layer = boardUnityLayer;
            content.transform.SetParent(board.transform, false);
            GameObject focusMask = GameObject.CreatePrimitive(PrimitiveType.Quad);
            focusMask.name = "Board Focus Stencil Mask";
            focusMask.transform.SetParent(content.transform, false);
            focusMask.layer = LayerMask.NameToLayer("Ignore Raycast");
            focusMask.transform.localPosition = new Vector3(0f, 0f, 0.13f);
            focusMask.transform.localScale = new Vector3(1.28f, 0.80f, 1f);
            UnityEngine.Object.DestroyImmediate(focusMask.GetComponent<Collider>());
            MeshRenderer focusMaskRenderer = focusMask.GetComponent<MeshRenderer>();
            focusMaskRenderer.sharedMaterial = focusMaskMaterial;
            focusMaskRenderer.enabled = false;

            Transform surface = new GameObject("Board Surface").transform;
            surface.gameObject.layer = boardUnityLayer;
            surface.SetParent(content.transform, false);
            Transform clueContainer = new GameObject("Clue Container").transform;
            clueContainer.gameObject.layer = boardUnityLayer;
            clueContainer.SetParent(surface, false);
            clueContainer.localPosition = new Vector3(0f, 0f, 0.075f);
            cluePrefab = AssetDatabase.LoadAssetAtPath<LawyerBoardClue>(CluePrefabPath);
            if (cluePrefab == null)
                throw new InvalidOperationException($"Không thể tải lại prefab manh mối: {CluePrefabPath}");
            GameObject clueTemplateObject = (GameObject)PrefabUtility.InstantiatePrefab(
                cluePrefab.gameObject, scene);
            clueTemplateObject.name = "Clue Card Template";
            clueTemplateObject.transform.SetParent(clueContainer, false);
            clueTemplateObject.transform.localPosition = Vector3.zero;
            clueTemplateObject.transform.localRotation = Quaternion.identity;
            clueTemplateObject.transform.localScale = clueTemplateScale;
            clueTemplateObject.SetActive(false);
            LawyerBoardClue clueTemplate = clueTemplateObject.GetComponent<LawyerBoardClue>();
            LawyerClueLinkSystem linkSystem = surface.gameObject.AddComponent<LawyerClueLinkSystem>();
            SetObject(linkSystem, "stringMaterial", stringMaterial);

            Material paperMaterial = CreateMaterial(
                MaterialFolder + "/Lawyer Briefing Paper.mat", new Color(0.86f, 0.82f, 0.66f));
            Material plateMaterial = CreateMaterial(
                MaterialFolder + "/Lawyer Briefing Plate.mat", new Color(0.09f, 0.19f, 0.16f));
            Material corkMaterial = CreateMaterial(
                MaterialFolder + "/Lawyer Evidence Surface.mat", new Color(0.30f, 0.18f, 0.09f));
            Material buttonMaterial = CreateMaterial(
                MaterialFolder + "/Lawyer Physical Button.mat", new Color(0.10f, 0.22f, 0.20f));

            TMP_Text title = CreatePhysicalSheet(
                content.transform, "Physical Title Plate", new Vector3(0f, 0.34f, 0.052f),
                new Vector2(1.04f, 0.075f), plateMaterial, boardUnityLayer,
                new Vector2(98f, 5.5f), 32f, TextAlignmentOptions.Center, Color.white);
            TMP_Text objective = CreatePhysicalSheet(
                content.transform, "Physical Objective Sheet", new Vector3(-0.30f, 0.22f, 0.052f),
                new Vector2(0.50f, 0.15f), paperMaterial, boardUnityLayer,
                new Vector2(28.4016f, 13f), 12f, TextAlignmentOptions.TopLeft, new Color(0.035f, 0.03f, 0.02f));
            TMP_Text timeline = CreatePhysicalSheet(
                content.transform, "Physical Timeline Sheet", new Vector3(-0.30f, 0.025f, 0.052f),
                new Vector2(0.50f, 0.21f), paperMaterial, boardUnityLayer,
                new Vector2(28.4016f, 19f), 10f, TextAlignmentOptions.TopLeft, new Color(0.035f, 0.03f, 0.02f));
            TMP_Text witnesses = CreatePhysicalSheet(
                content.transform, "Physical Witness Sheet", new Vector3(-0.30f, -0.145f, 0.052f),
                new Vector2(0.50f, 0.105f), paperMaterial, boardUnityLayer,
                new Vector2(28.4016f, 8.5f), 9.5f, TextAlignmentOptions.TopLeft, new Color(0.035f, 0.03f, 0.02f));
            TMP_Text details = CreatePhysicalSheet(
                content.transform, "Physical Details Sheet", new Vector3(-0.30f, -0.255f, 0.052f),
                new Vector2(0.50f, 0.095f), paperMaterial, boardUnityLayer,
                new Vector2(28.4016f, 7.5f), 9.5f, TextAlignmentOptions.TopLeft, new Color(0.035f, 0.03f, 0.02f));
            ConfigurePhysicalTextLayout(objective, new Vector2(-0.388f, 0.22f), true);
            ConfigurePhysicalTextLayout(timeline, new Vector2(-0.388f, 0.025f), true);
            ConfigurePhysicalTextLayout(witnesses, new Vector2(-0.388f, -0.145f), true);
            ConfigurePhysicalTextLayout(details, new Vector2(-0.388f, -0.255f), true);

            GameObject evidenceSurface = CreatePhysicalPanel(
                content.transform, "Physical Evidence Surface", new Vector3(0.1466f, 0.0325f, 0.003f),
                new Vector3(0.82655907f, 0.535035f, 0.018f), corkMaterial, boardUnityLayer);
            CreateWorldText(
                content.transform, "Evidence Heading", new Vector3(0.127f, 0.26f, 0.058f),
                new Vector2(48f, 4f), 28f, TextAlignmentOptions.Center).text = "MANH MỐI";

            TMP_Text status = CreatePhysicalSheet(
                content.transform, "Physical Status Plate", new Vector3(0.29f, -0.265f, 0.052f),
                new Vector2(0.54f, 0.06f), plateMaterial, boardUnityLayer,
                new Vector2(66.6422f, 4.5f), 11f, TextAlignmentOptions.Center, Color.white);
            ConfigurePhysicalTextLayout(status, new Vector2(0.20679f, -0.265f), false);
            LawyerPhysicalButton continueButton = CreatePhysicalButton(
                content.transform, "Physical Continue Button", "Đến phòng xử án",
                new Vector3(0.24f, -0.355f, 0.022f), new Vector2(0.31f, 0.065f),
                buttonMaterial, boardUnityLayer, boardInteractionLayer);
            LawyerPhysicalButton undo = CreatePhysicalButton(
                content.transform, "Physical Undo Button", "Hoàn tác",
                new Vector3(0.48f, -0.355f, 0.022f), new Vector2(0.15f, 0.065f),
                buttonMaterial, boardUnityLayer, boardInteractionLayer);

            Canvas finaleCanvas = CreateBoardOverlayCanvas(
                playerCamera.transform, "Lawyer Finale Canvas", new Vector2(1080f, 640f),
                0.001f, 50);
            finaleCanvas.transform.localPosition = new Vector3(0f, 0f, 1.2f);
            finaleCanvas.transform.localRotation = Quaternion.identity;

            GameObject introPanel = CreatePanel(finaleCanvas.transform, "Evidence Intro Overlay");
            CanvasGroup introGroup = introPanel.AddComponent<CanvasGroup>();
            Image introBackground = CreateImage(
                introPanel.transform, "Background", new Color(0.025f, 0.045f, 0.04f, 0.92f));
            Stretch((RectTransform)introBackground.transform);
            TMP_Text introTitle = CreateUiText(
                introPanel.transform, "Intro Title", Vector2.zero, new Vector2(920f, 220f),
                54f, TextAlignmentOptions.Center);
            introTitle.fontStyle = FontStyles.Bold;
            introTitle.text = "Liên kết các cặp manh mối có quan hệ trực tiếp";
            introPanel.SetActive(false);

            GameObject resultPanel = CreatePanel(finaleCanvas.transform, "Evidence Result Overlay");
            Image resultBackground = CreateImage(
                resultPanel.transform, "Background", new Color(0.025f, 0.045f, 0.04f, 0.96f));
            Stretch((RectTransform)resultBackground.transform);
            TMP_Text resultTitle = CreateUiText(
                resultPanel.transform, "Result Title", new Vector2(0f, 235f),
                new Vector2(920f, 100f), 58f, TextAlignmentOptions.Center);
            resultTitle.fontStyle = FontStyles.Bold;
            TMP_Text resultBody = CreateUiText(
                resultPanel.transform, "Result Body", new Vector2(0f, 75f),
                new Vector2(870f, 190f), 34f, TextAlignmentOptions.Center);
            TMP_Text defenseInstruction = CreateUiText(
                resultPanel.transform, "Defense Instruction", new Vector2(0f, -125f),
                new Vector2(870f, 150f), 31f, TextAlignmentOptions.Center);
            defenseInstruction.text =
                "Hãy dùng các mối liên hệ giữa những manh mối đã tìm được và sự thật "
                + "để trình bày lời bào chữa cho bị cáo.";
            defenseInstruction.rectTransform.localScale = Vector3.one * 0.75f;
            Button startDefenseButton = CreateButton(
                resultPanel.transform, "Start Defense Button", "Bắt đầu bào chữa",
                new Vector2(0f, -235f), new Vector2(340f, 70f));
            startDefenseButton.gameObject.SetActive(false);
            resultPanel.SetActive(false);
            SetLayerRecursively(finaleCanvas.gameObject, boardUnityLayer);

            Light[] officeLights = FindComponentsInScene<Light>(scene);
            GameObject spotlightObject = new GameObject("Board Focus Spotlight");
            spotlightObject.layer = boardUnityLayer;
            spotlightObject.transform.SetParent(content.transform, false);
            spotlightObject.transform.localPosition = new Vector3(0f, 0.18f, 0.55f);
            spotlightObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Light spotlight = spotlightObject.AddComponent<Light>();
            spotlight.type = LightType.Spot;
            spotlight.range = 2f;
            spotlight.spotAngle = 110f;
            spotlight.intensity = 0.12f;
            spotlight.shadows = LightShadows.None;
            spotlight.cullingMask = 1 << boardUnityLayer;
            spotlight.enabled = false;
            LawyerBoardFocusLighting focusLighting = board.AddComponent<LawyerBoardFocusLighting>();
            focusLighting.ConfigureInEditor(officeLights, spotlight, focusMaskRenderer);
            EditorUtility.SetDirty(focusLighting);

            XRRayInteractor rightRay = ConfigureOfficeRay(
                player, boardUnityLayer, boardInteractionLayer);
            ConfigureForegroundRenderers(player);
            XRBaseInteractor[] modalInteractors = player.GetComponentsInChildren<XRBaseInteractor>(true);

            SerializedObject boardObject = new SerializedObject(controller);
            Assign(boardObject, "caseDefinition", caseDefinition);
            Assign(boardObject, "controls", controls);
            Assign(boardObject, "boardSurface", surface);
            Assign(boardObject, "clueContainer", clueContainer);
            Assign(boardObject, "clueTemplate", clueTemplate);
            Assign(boardObject, "linkSystem", linkSystem);
            Assign(boardObject, "focusLighting", focusLighting);
            Assign(boardObject, "evidenceSurfaceRenderer",
                evidenceSurface.GetComponent<Renderer>());
            Assign(boardObject, "titleLabel", title);
            Assign(boardObject, "objectiveLabel", objective);
            Assign(boardObject, "timelineLabel", timeline);
            Assign(boardObject, "witnessesLabel", witnesses);
            Assign(boardObject, "detailsLabel", details);
            Assign(boardObject, "statusLabel", status);
            Assign(boardObject, "continueButton", continueButton);
            Assign(boardObject, "undoLinkButton", undo);
            Assign(boardObject, "resultPanel", resultPanel);
            Assign(boardObject, "resultTitleLabel", resultTitle);
            Assign(boardObject, "resultBodyLabel", resultBody);
            Assign(boardObject, "defenseInstructionLabel", defenseInstruction);
            Assign(boardObject, "startDefenseButton", startDefenseButton);
            boardObject.FindProperty("truthDisplayDuration").floatValue = 15f;
            boardObject.FindProperty("truthFinalPosition").vector2Value = new Vector2(0f, 115f);
            boardObject.FindProperty("instructionFinalPosition").vector2Value = Vector2.zero;
            Assign(boardObject, "introTitleGroup", introGroup);
            Assign(boardObject, "introTitleLabel", introTitle);
            boardObject.FindProperty("introDisplayDuration").floatValue = 2f;
            boardObject.FindProperty("introFadeDuration").floatValue = 0.75f;
            Assign(boardObject, "rightRayInteractor", rightRay);
            AssignArray(boardObject, "modalInteractors", modalInteractors);
            List<Transform> obstacleRoots = new List<Transform>();
            GameObject propsRoot = FindInScene(scene, "Props");
            GameObject npcsRoot = FindInScene(scene, "NPCs");
            if (propsRoot != null)
                obstacleRoots.Add(propsRoot.transform);
            if (npcsRoot != null)
                obstacleRoots.Add(npcsRoot.transform);
            AssignArray(boardObject, "focusObstacleRoots", obstacleRoots.ToArray());
            boardObject.FindProperty("expandedDistance").floatValue = 0.70f;
            boardObject.FindProperty("expandedVerticalOffset").floatValue = 0.04f;
            boardObject.FindProperty("focusClearance").floatValue = 0.03f;
            boardObject.FindProperty("maximumFocusLift").floatValue = 0.30f;
            boardObject.FindProperty("maximumFocusPull").floatValue = 0.15f;
            boardObject.FindProperty("focusSearchStep").floatValue = 0.05f;
            boardObject.FindProperty("raisedArcLift").floatValue = 0.25f;
            boardObject.FindProperty("pathSamples").intValue = 12;
            boardObject.FindProperty("boardInteractionMask")
                .FindPropertyRelative("m_Bits").intValue = 1 << boardInteractionLayer;
            boardObject.FindProperty("boardPhysicsMask").intValue = 1 << boardUnityLayer;
            boardObject.ApplyModifiedPropertiesWithoutUndo();
            controller.ConfigureAssetReferencesInEditor(caseDefinition, controls, clueTemplate);
            EditorUtility.SetDirty(controller);

            LawyerRemoteSceneAdapter adapter = GetOrAdd<LawyerRemoteSceneAdapter>(
                EnsureRoot(scene, "Lawyer Game Manager"));
            adapter.ConfigureInEditor(caseDefinition, controller, null);
            EditorUtility.SetDirty(adapter);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static XRRayInteractor ConfigureOfficeRay(
            GameObject player, int boardUnityLayer, int boardInteractionLayer)
        {
            XRRayInteractor rightRay = player.GetComponentInChildren<XRRayInteractor>(true);
            if (rightRay == null)
                throw new InvalidOperationException("VR Player không có Right Ray Interactor.");

            XRDirectInteractor rightDirect = null;
            XRDirectInteractor[] directInteractors = player.GetComponentsInChildren<XRDirectInteractor>(true);
            for (int index = 0; index < directInteractors.Length; index++)
                if (directInteractors[index].name.IndexOf("Right", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    rightDirect = directInteractors[index];
                    break;
                }
            if (rightDirect == null)
                throw new InvalidOperationException("VR Player không có Right Direct Interactor.");

            SerializedObject source = new SerializedObject(rightDirect);
            SerializedObject target = new SerializedObject(rightRay);
            CopyInputReader(source, "m_SelectInput", target, "m_SelectInput");
            CopyInputReader(target, "m_UIPressInput", target, "m_ActivateInput");
            target.FindProperty("m_AllowHoveredActivate").boolValue = true;
            target.FindProperty("m_InteractionLayers").FindPropertyRelative("m_Bits").intValue
                = 1 << boardInteractionLayer;
            target.FindProperty("m_RaycastMask").intValue = 1 << boardUnityLayer;
            target.FindProperty("m_HitClosestOnly").boolValue = true;
            target.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rightRay);

            XRBaseInteractor[] interactors = player.GetComponentsInChildren<XRBaseInteractor>(true);
            for (int index = 0; index < interactors.Length; index++)
            {
                if (interactors[index] == rightRay)
                    continue;
                SerializedObject interactorObject = new SerializedObject(interactors[index]);
                SerializedProperty bits = interactorObject.FindProperty("m_InteractionLayers")
                    ?.FindPropertyRelative("m_Bits");
                if (bits == null)
                    continue;
                if (interactors[index] is XRPokeInteractor)
                    bits.intValue &= ~(1 << boardInteractionLayer);
                else
                    bits.intValue |= 1 << boardInteractionLayer;
                interactorObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(interactors[index]);
            }

            ConfigureStableRayVisual(rightRay);
            return rightRay;
        }

        private static void CopyInputReader(
            SerializedObject source,
            string sourcePropertyName,
            SerializedObject target,
            string targetPropertyName)
        {
            string[] relativeProperties =
            {
                "m_InputSourceMode",
                "m_InputActionReferencePerformed",
                "m_InputActionReferenceValue",
                "m_ObjectReferenceObject"
            };
            SerializedProperty sourceReader = source.FindProperty(sourcePropertyName);
            SerializedProperty targetReader = target.FindProperty(targetPropertyName);
            if (sourceReader == null || targetReader == null)
                throw new InvalidOperationException(
                    $"Không thể sao chép {sourcePropertyName} sang {targetPropertyName} cho Right Ray Interactor.");
            for (int index = 0; index < relativeProperties.Length; index++)
            {
                SerializedProperty from = sourceReader.FindPropertyRelative(relativeProperties[index]);
                SerializedProperty to = targetReader.FindPropertyRelative(relativeProperties[index]);
                if (from == null || to == null)
                    continue;
                if (from.propertyType == SerializedPropertyType.ObjectReference)
                    to.objectReferenceValue = from.objectReferenceValue;
                else if (from.propertyType == SerializedPropertyType.Enum)
                    to.enumValueIndex = from.enumValueIndex;
                else
                    to.intValue = from.intValue;
            }
        }

        private static void ConfigureStableRayVisual(XRRayInteractor ray)
        {
            Gradient invalid = new Gradient();
            invalid.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.55f, 0.65f, 0.68f), 0f),
                    new GradientColorKey(new Color(0.32f, 0.38f, 0.40f), 1f)
                },
                new[] { new GradientAlphaKey(0.75f, 0f), new GradientAlphaKey(0.18f, 1f) });
            Gradient valid = new Gradient();
            valid.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.05f, 0.85f, 1f), 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.25f, 1f) });

            MonoBehaviour[] behaviours = ray.GetComponents<MonoBehaviour>();
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] == null || behaviours[index] == ray)
                    continue;
                SerializedObject serialized = new SerializedObject(behaviours[index]);
                SerializedProperty invalidProperty = serialized.FindProperty("m_InvalidColorGradient");
                SerializedProperty validProperty = serialized.FindProperty("m_ValidColorGradient");
                if (invalidProperty == null || validProperty == null)
                    continue;
                invalidProperty.gradientValue = invalid;
                validProperty.gradientValue = valid;
                SerializedProperty autoLength = serialized.FindProperty("m_AutoAdjustLineLength");
                SerializedProperty hitLength = serialized.FindProperty("m_UseDistanceToHitAsMaxLineLength");
                SerializedProperty lineLength = serialized.FindProperty("m_LineLength");
                SerializedProperty retractionDelay = serialized.FindProperty("m_LineRetractionDelay");
                if (autoLength != null)
                    autoLength.boolValue = false;
                if (hitLength != null)
                    hitLength.boolValue = false;
                if (lineLength != null)
                    lineLength.floatValue = 3f;
                if (retractionDelay != null)
                    retractionDelay.floatValue = 0f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(behaviours[index]);
            }
        }

        private static GameObject CreatePhysicalPanel(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            int layer)
        {
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = name;
            panel.layer = layer;
            panel.transform.SetParent(parent, false);
            panel.transform.localPosition = localPosition;
            panel.transform.localScale = localScale;
            UnityEngine.Object.DestroyImmediate(panel.GetComponent<Collider>());
            panel.GetComponent<MeshRenderer>().sharedMaterial = material;
            return panel;
        }

        private static TMP_Text CreatePhysicalSheet(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector2 size,
            Material material,
            int layer,
            Vector2 textSize,
            float fontSize,
            TextAlignmentOptions alignment,
            Color textColor)
        {
            GameObject panel = CreatePhysicalPanel(
                parent, name, localPosition, new Vector3(size.x, size.y, 0.018f), material, layer);
            TMP_Text label = CreateWorldText(
                parent, name + " Text",
                new Vector3(localPosition.x, localPosition.y, localPosition.z + 0.011f),
                textSize, fontSize, alignment);
            label.color = textColor;
            return label;
        }

        private static void ConfigurePhysicalTextLayout(
            TMP_Text label, Vector2 anchoredPosition, bool autoSize)
        {
            label.rectTransform.anchoredPosition = anchoredPosition;
            label.enableAutoSizing = autoSize;
            if (!autoSize)
                return;
            label.fontSizeMin = 18f;
            label.fontSizeMax = 72f;
        }

        private static LawyerPhysicalButton CreatePhysicalButton(
            Transform parent,
            string name,
            string labelText,
            Vector3 localPosition,
            Vector2 size,
            Material material,
            int unityLayer,
            int interactionLayer)
        {
            GameObject root = new GameObject(name);
            root.layer = unityLayer;
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(size.x, size.y, 0.025f);
            XRSimpleInteractable interactable = root.AddComponent<XRSimpleInteractable>();
            interactable.colliders.Clear();
            interactable.colliders.Add(collider);
            SetInteractionLayerMask(interactable, 1 << interactionLayer);

            GameObject pressVisual = new GameObject("Press Visual");
            pressVisual.layer = unityLayer;
            pressVisual.transform.SetParent(root.transform, false);
            GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh.name = "Button Mesh";
            mesh.layer = unityLayer;
            mesh.transform.SetParent(pressVisual.transform, false);
            mesh.transform.localScale = new Vector3(size.x, size.y, 0.022f);
            UnityEngine.Object.DestroyImmediate(mesh.GetComponent<Collider>());
            MeshRenderer renderer = mesh.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;

            TMP_Text label = CreateWorldText(
                pressVisual.transform, "Label", new Vector3(0f, 0f, 0.016f),
                new Vector2(size.x * 92f, size.y * 75f), 14f, TextAlignmentOptions.Center);
            label.text = labelText;
            label.color = Color.white;

            LawyerPhysicalButton button = root.AddComponent<LawyerPhysicalButton>();
            button.ConfigureInEditor(pressVisual.transform, renderer, label);
            EditorUtility.SetDirty(button);
            return button;
        }

        private static void ConfigureCourtroom(LawyerCaseDefinition caseDefinition, InputActionAsset controls)
        {
            Scene scene = EditorSceneManager.OpenScene(CourtroomScenePath, OpenSceneMode.Single);
            RemoveRoot(scene, "VR Player");
            RemoveRoot(scene, "Lawyer Interview");
            GameObject npcAnchor = FindInScene(scene, "NPCs") ?? EnsureRoot(scene, "NPCs");
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VR Player.prefab");
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.name = "VR Player";
            Vector3 playerPosition = npcAnchor.transform.position - npcAnchor.transform.forward * 2.4f;
            player.transform.position = playerPosition;
            Vector3 lookDirection = Vector3.ProjectOnPlane(npcAnchor.transform.position - playerPosition, Vector3.up);
            player.transform.rotation = lookDirection.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(lookDirection.normalized, Vector3.up)
                : Quaternion.identity;
            ConfigureStationaryRig(player);
            EnsureInteractionInfrastructure(scene);

            Camera playerCamera = player.GetComponentInChildren<Camera>(true);
            GameObject interviewRoot = new GameObject("Lawyer Interview");
            SceneManager.MoveGameObjectToScene(interviewRoot, scene);
            LawyerInterviewController controller = interviewRoot.AddComponent<LawyerInterviewController>();

            GameObject defendant = CreateDefendant(npcAnchor.transform, playerCamera != null ? playerCamera.transform : null);
            AudioSource voice = defendant.GetComponent<AudioSource>();
            if (voice == null)
                voice = defendant.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 1f;

            Canvas canvas = CreateWorldCanvas(
                playerCamera.transform, "Lawyer Interview Canvas", new Vector2(1000f, 700f), 0.001f, 200);
            canvas.transform.localPosition = new Vector3(0.48f, -0.08f, 1.2f);
            canvas.transform.localRotation = Quaternion.identity;
            Image background = CreateImage(canvas.transform, "Interview Background", new Color(0.035f, 0.08f, 0.09f, 0.94f));
            Stretch(background.rectTransform);
            TMP_Text defendantName = CreateUiText(canvas.transform, "Defendant Name", new Vector2(0f, 300f), new Vector2(920f, 58f), 34f, TextAlignmentOptions.Center);
            TMP_Text counter = CreateUiText(canvas.transform, "Question Counter", new Vector2(0f, 245f), new Vector2(920f, 42f), 23f, TextAlignmentOptions.Center);

            GameObject mainPanel = CreatePanel(canvas.transform, "Main Question Panel");
            Button[] mainButtons = new Button[5];
            for (int index = 0; index < mainButtons.Length; index++)
                mainButtons[index] = CreateButton(mainPanel.transform, $"Main Question {index + 1}", string.Empty, new Vector2(0f, 150f - index * 78f), new Vector2(880f, 62f));

            GameObject responsePanel = CreatePanel(canvas.transform, "Response Panel");
            TMP_Text response = CreateUiText(responsePanel.transform, "Response", new Vector2(0f, 35f), new Vector2(860f, 360f), 30f, TextAlignmentOptions.TopLeft);
            TMP_Text responseHint = CreateUiText(responsePanel.transform, "Response Hint", new Vector2(0f, -195f), new Vector2(860f, 48f), 23f, TextAlignmentOptions.Center);
            Button continueButton = CreateButton(responsePanel.transform, "Continue Response", "Tiếp tục", new Vector2(0f, -255f), new Vector2(240f, 58f));

            GameObject followPanel = CreatePanel(canvas.transform, "Follow Up Panel");
            TMP_Text followHeading = CreateUiText(followPanel.transform, "Follow Heading", new Vector2(0f, 180f), new Vector2(880f, 58f), 32f, TextAlignmentOptions.Center);
            followHeading.text = "Chọn 1 câu hỏi tiếp theo";
            Button[] followButtons = new Button[3];
            for (int index = 0; index < followButtons.Length; index++)
                followButtons[index] = CreateButton(followPanel.transform, $"Follow Up {index + 1}", string.Empty, new Vector2(0f, 80f - index * 105f), new Vector2(880f, 78f));

            GameObject toastPanel = CreatePanel(canvas.transform, "Clue Toast");
            RectTransform toastRect = (RectTransform)toastPanel.transform;
            toastRect.anchorMin = new Vector2(0.5f, 1f);
            toastRect.anchorMax = new Vector2(0.5f, 1f);
            toastRect.anchoredPosition = new Vector2(0f, -90f);
            toastRect.sizeDelta = new Vector2(720f, 64f);
            Image toastBackground = toastPanel.AddComponent<Image>();
            toastBackground.color = new Color(0.72f, 0.42f, 0.05f, 0.98f);
            TMP_Text toast = CreateUiText(toastPanel.transform, "Toast Text", Vector2.zero, new Vector2(690f, 56f), 25f, TextAlignmentOptions.Center);
            ApplyOverlayMaterials(canvas.transform);

            Canvas defenseCanvas = CreateWorldCanvas(
                playerCamera.transform, "Lawyer Defense Canvas", new Vector2(1000f, 700f),
                0.001f, 210);
            defenseCanvas.transform.localPosition = new Vector3(0f, 0f, 1.2f);
            defenseCanvas.transform.localRotation = Quaternion.identity;
            Image defenseBackground = CreateImage(
                defenseCanvas.transform, "Defense Background",
                new Color(0.025f, 0.045f, 0.04f, 0.94f));
            Stretch(defenseBackground.rectTransform);
            TMP_Text defenseTitle = CreateUiText(
                defenseCanvas.transform, "Defense Title", new Vector2(0f, 250f),
                new Vector2(900f, 80f), 50f, TextAlignmentOptions.Center);
            defenseTitle.fontStyle = FontStyles.Bold;
            TMP_Text defenseCountdown = CreateUiText(
                defenseCanvas.transform, "Defense Countdown", new Vector2(0f, 70f),
                new Vector2(500f, 190f), 130f, TextAlignmentOptions.Center);
            TMP_Text defenseInstruction = CreateUiText(
                defenseCanvas.transform, "Defense Instruction", new Vector2(0f, -95f),
                new Vector2(860f, 110f), 32f, TextAlignmentOptions.Center);
            TMP_Text defenseStatus = CreateUiText(
                defenseCanvas.transform, "Defense Status", new Vector2(0f, -235f),
                new Vector2(860f, 70f), 26f, TextAlignmentOptions.Center);
            ApplyOverlayMaterials(defenseCanvas.transform);
            defenseCanvas.gameObject.SetActive(false);

            mainPanel.SetActive(true);
            responsePanel.SetActive(false);
            followPanel.SetActive(false);
            toastPanel.SetActive(false);

            SerializedObject interviewObject = new SerializedObject(controller);
            Assign(interviewObject, "caseDefinition", caseDefinition);
            Assign(interviewObject, "controls", controls);
            Assign(interviewObject, "defendantVoice", voice);
            Assign(interviewObject, "mainQuestionPanel", mainPanel);
            Assign(interviewObject, "responsePanel", responsePanel);
            Assign(interviewObject, "followUpPanel", followPanel);
            Assign(interviewObject, "toastPanel", toastPanel);
            Assign(interviewObject, "defendantNameLabel", defendantName);
            Assign(interviewObject, "questionCounterLabel", counter);
            Assign(interviewObject, "responseLabel", response);
            Assign(interviewObject, "responseHintLabel", responseHint);
            Assign(interviewObject, "toastLabel", toast);
            AssignArray(interviewObject, "mainQuestionButtons", mainButtons);
            AssignArray(interviewObject, "followUpButtons", followButtons);
            Assign(interviewObject, "responseContinueButton", continueButton);
            interviewObject.FindProperty("navigationDeadzone").floatValue = 0.65f;
            interviewObject.FindProperty("navigationRepeatDelay").floatValue = 0.6f;
            interviewObject.FindProperty("navigationRepeatRate").floatValue = 0.35f;
            interviewObject.ApplyModifiedPropertiesWithoutUndo();
            controller.ConfigureAssetReferencesInEditor(caseDefinition, controls);
            EditorUtility.SetDirty(controller);

            LawyerDefenseController defenseController =
                interviewRoot.AddComponent<LawyerDefenseController>();
            defenseController.ConfigureInEditor(
                caseDefinition, controls, controller, canvas.gameObject,
                defenseCanvas.gameObject);
            SerializedObject defenseObject = new SerializedObject(defenseController);
            Assign(defenseObject, "titleLabel", defenseTitle);
            Assign(defenseObject, "countdownLabel", defenseCountdown);
            Assign(defenseObject, "instructionLabel", defenseInstruction);
            Assign(defenseObject, "statusLabel", defenseStatus);
            defenseObject.FindProperty("preparationDuration").floatValue = 5f;
            defenseObject.FindProperty("recordingDuration").floatValue = 30f;
            defenseObject.FindProperty("uploadAttempts").intValue = 5;
            defenseObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(defenseController);

            LawyerRemoteSceneAdapter adapter = interviewRoot.AddComponent<LawyerRemoteSceneAdapter>();
            adapter.ConfigureInEditor(caseDefinition, null, controller);
            EditorUtility.SetDirty(adapter);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static GameObject CreateDefendant(Transform parent, Transform lookTarget)
        {
            Transform existing = parent.Find("Lawyer Defendant");
            if (existing != null)
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Casual NPC.prefab");
            GameObject defendant = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            defendant.name = "Lawyer Defendant";
            defendant.transform.localPosition = Vector3.zero;
            defendant.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            NpcMotionPlayer motion = defendant.GetComponent<NpcMotionPlayer>();
            if (motion != null)
            {
                SerializedObject serialized = new SerializedObject(motion);
                serialized.FindProperty("mode").enumValueIndex = (int)NpcMotionMode.StaticHumanoidPose;
                serialized.FindProperty("staticPose").objectReferenceValue = AssetDatabase.LoadAssetAtPath<NpcHumanoidPose>(
                    "Assets/Animations/NPC/Doctor Patient Sitting.asset");
                serialized.FindProperty("trackHead").boolValue = true;
                serialized.FindProperty("lookTarget").objectReferenceValue = lookTarget;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            return defendant;
        }

        private static void ConfigureStationaryRig(GameObject player)
        {
            Transform locomotion = player.transform.Find("Locomotion");
            List<GameObject> disabled = new List<GameObject>();
            if (locomotion != null)
            {
                for (int index = 0; index < locomotion.childCount; index++)
                {
                    Transform child = locomotion.GetChild(index);
                    bool keepTurning = child.name.IndexOf("Turn", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (keepTurning)
                    {
                        child.gameObject.SetActive(true);
                        continue;
                    }
                    if (child.name.IndexOf("Move", StringComparison.OrdinalIgnoreCase) >= 0
                        || child.name.IndexOf("Jump", StringComparison.OrdinalIgnoreCase) >= 0
                        || child.name.IndexOf("Gravity", StringComparison.OrdinalIgnoreCase) >= 0
                        || child.name.IndexOf("Teleport", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        child.gameObject.SetActive(false);
                        disabled.Add(child.gameObject);
                    }
                }
            }
            LawyerStationaryRig stationary = GetOrAdd<LawyerStationaryRig>(player);
            stationary.ConfigureInEditor(disabled.ToArray());
            EditorUtility.SetDirty(stationary);
        }

        private static void EnsureInteractionInfrastructure(Scene scene)
        {
            if (FindComponentInScene<XRInteractionManager>(scene) == null)
                EnsureRoot(scene, "XR Interaction Manager").AddComponent<XRInteractionManager>();
            if (FindComponentInScene<EventSystem>(scene) == null)
            {
                GameObject eventSystem = EnsureRoot(scene, "EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<XRUIInputModule>();
            }
        }

        private static void ConfigureBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            AddBuildScene(scenes, OfficeScenePath);
            AddBuildScene(scenes, CourtroomScenePath);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void AddBuildScene(List<EditorBuildSettingsScene> scenes, string path)
        {
            EditorBuildSettingsScene existing = scenes.FirstOrDefault(scene => scene.path == path);
            if (existing != null)
                existing.enabled = true;
            else
                scenes.Add(new EditorBuildSettingsScene(path, true));
        }

        private static void ConfigureStandbyCatalog()
        {
            Scene scene = EditorSceneManager.OpenScene(StandbyScenePath, OpenSceneMode.Single);
            GameController controller = FindComponentInScene<GameController>(scene);
            if (controller == null)
                throw new InvalidOperationException("Cảnh Standby không có GameController.");
            SerializedObject serialized = new SerializedObject(controller);
            SerializedProperty catalog = serialized.FindProperty("scenes");
            for (int index = 0; index < catalog.arraySize; index++)
            {
                SerializedProperty entry = catalog.GetArrayElementAtIndex(index);
                string id = entry.FindPropertyRelative("id").stringValue;
                string path = entry.FindPropertyRelative("scenePath").stringValue;
                if (id == "lawyer-office" || path == OfficeScenePath)
                {
                    entry.FindPropertyRelative("id").stringValue = "lawyer-office";
                    entry.FindPropertyRelative("scenePath").stringValue = OfficeScenePath;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorSceneManager.SaveScene(scene);
                    return;
                }
            }
            catalog.InsertArrayElementAtIndex(catalog.arraySize);
            SerializedProperty added = catalog.GetArrayElementAtIndex(catalog.arraySize - 1);
            added.FindPropertyRelative("id").stringValue = "lawyer-office";
            added.FindPropertyRelative("scenePath").stringValue = OfficeScenePath;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
        }

        private static Canvas CreateWorldCanvas(Transform parent, string name, Vector2 size, float scale, int sortingOrder)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(TrackedDeviceGraphicRaycaster));
            root.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)root.transform;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one * scale;
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = sortingOrder;
            return canvas;
        }

        private static Canvas EnsureWorldCanvas(
            Transform parent, string name, Vector2 size, float scale, int sortingOrder)
        {
            Transform existing = parent.Find(name);
            if (existing == null)
                return CreateWorldCanvas(parent, name, size, scale, sortingOrder);

            Canvas canvas = GetOrAdd<Canvas>(existing.gameObject);
            GetOrAdd<CanvasScaler>(existing.gameObject);
            GetOrAdd<TrackedDeviceGraphicRaycaster>(existing.gameObject);
            RectTransform rect = existing as RectTransform;
            if (rect == null)
                throw new InvalidOperationException($"{name} không dùng RectTransform.");
            rect.sizeDelta = size;
            rect.localScale = Vector3.one * scale;
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
            return canvas;
        }

        private static Canvas CreateBoardOverlayCanvas(
            Transform parent, string name, Vector2 size, float scale, int sortingOrder)
        {
            GameObject root = new GameObject(
                name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(TrackedDeviceGraphicRaycaster));
            root.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)root.transform;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one * scale;
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
            return canvas;
        }

        private static GameObject CreatePanel(Transform parent, string name)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            Stretch((RectTransform)panel.transform);
            return panel;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Image EnsureImage(Transform parent, string name, Color color)
        {
            Transform existing = parent.Find(name);
            if (existing == null)
                return CreateImage(parent, name, color);
            Image image = existing.GetComponent<Image>();
            if (image == null)
                image = existing.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static TMP_Text CreateUiText(
            Transform parent, string name, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)textObject.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static TMP_Text EnsureUiText(
            Transform parent, string name, Vector2 position, Vector2 size,
            float fontSize, TextAlignmentOptions alignment)
        {
            Transform existing = parent.Find(name);
            TMP_Text text = existing != null ? existing.GetComponent<TMP_Text>() : null;
            if (text == null)
                return CreateUiText(parent, name, position, size, fontSize, alignment);
            ConfigureUiText(text, position, size, fontSize, alignment);
            return text;
        }

        private static void ConfigureUiText(
            TMP_Text text, Vector2 position, Vector2 size,
            float fontSize, TextAlignmentOptions alignment)
        {
            RectTransform rect = text.rectTransform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
        }

        private static TMP_Text CreateWorldText(
            Transform parent, string name, Vector3 localPosition, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshPro));
            textObject.layer = parent.gameObject.layer;
            textObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)textObject.transform;
            rect.localPosition = localPosition;
            rect.localRotation = Quaternion.Euler(0f, 180f, 0f);
            rect.sizeDelta = size;
            rect.localScale = Vector3.one * 0.01f;
            TextMeshPro text = textObject.GetComponent<TextMeshPro>();
            text.fontSize = fontSize;
            text.color = new Color(0.08f, 0.07f, 0.05f);
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)buttonObject.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.08f, 0.15f, 0.17f, 0.96f);
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            TMP_Text text = CreateUiText(buttonObject.transform, "Label", Vector2.zero, size - new Vector2(20f, 10f), 24f, TextAlignmentOptions.Center);
            text.text = label;
            return button;
        }

        private static Button EnsureButton(
            Transform parent, string name, string label, Vector2 position, Vector2 size)
        {
            Transform existing = parent.Find(name);
            Button button = existing != null ? existing.GetComponent<Button>() : null;
            if (button == null)
                return CreateButton(parent, name, label, position, size);

            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.text = label;
                ConfigureUiText(
                    text, Vector2.zero, size - new Vector2(20f, 10f),
                    24f, TextAlignmentOptions.Center);
            }
            return button;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Material CreateMaterial(
            string path,
            Color color,
            string shaderName = "Universal Render Pipeline/Lit")
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null && shaderName == "Universal Render Pipeline/Lit")
                shader = Shader.Find("Standard");
            if (shader == null)
                throw new InvalidOperationException($"Không tìm thấy shader: {shaderName}");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }
            material.color = color;
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0f);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_SpecularHighlights"))
                material.SetFloat("_SpecularHighlights", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateShaderMaterial(string path, string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
                throw new InvalidOperationException($"Không tìm thấy shader: {shaderName}");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFocusDimRendererFeature(
            string rendererPath, Material dimMaterial, int boardUnityLayer)
        {
            UniversalRendererData rendererData =
                AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (rendererData == null)
                throw new InvalidOperationException($"Không tìm thấy URP Renderer Data: {rendererPath}");
            LawyerFocusDimRendererFeature feature = rendererData.rendererFeatures
                .OfType<LawyerFocusDimRendererFeature>().FirstOrDefault();
            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<LawyerFocusDimRendererFeature>();
                feature.name = "Lawyer Focus Dim";
                AssetDatabase.AddObjectToAsset(feature, rendererData);
                rendererData.rendererFeatures.Add(feature);
            }
            feature.ConfigureInEditor(
                dimMaterial, 1 << boardUnityLayer,
                LawyerForegroundRenderingLayerMask);
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(rendererData);
        }

        private static void ConfigureForegroundRenderers(GameObject player)
        {
            Renderer[] renderers = player.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                renderer.renderingLayerMask |= LawyerForegroundRenderingLayerMask;
                EditorUtility.SetDirty(renderer);
            }
        }

        private static void ApplyOverlayMaterials(Transform root)
        {
            Material uiOverlay = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Materials/Doctor/Doctor UI Overlay.mat");
            Material textOverlay = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Materials/Doctor/Doctor TMP Overlay.mat");
            Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
            for (int index = 0; index < graphics.Length; index++)
            {
                if (graphics[index] is TMP_Text text)
                {
                    if (textOverlay != null)
                        text.fontSharedMaterial = textOverlay;
                }
                else if (uiOverlay != null)
                {
                    graphics[index].material = uiOverlay;
                }
            }
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }

        private static GameObject EnsureRoot(Scene scene, string name)
        {
            GameObject existing = FindInScene(scene, name);
            if (existing != null)
                return existing;
            GameObject created = new GameObject(name);
            SceneManager.MoveGameObjectToScene(created, scene);
            return created;
        }

        private static GameObject FindInScene(Scene scene, string name)
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

        private static T FindComponentInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                    return component;
            }
            return null;
        }

        private static T[] FindComponentsInScene<T>(Scene scene) where T : Component
        {
            List<T> results = new List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
                results.AddRange(root.GetComponentsInChildren<T>(true));
            return results.ToArray();
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
                transforms[index].gameObject.layer = layer;
        }

        private static void SetInteractionLayerMask(XRBaseInteractable interactable, int bits)
        {
            SerializedObject serialized = new SerializedObject(interactable);
            serialized.FindProperty("m_InteractionLayers")
                .FindPropertyRelative("m_Bits").intValue = bits;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(interactable);
        }

        private static int EnsureUnityLayer(string layerName)
        {
            int existing = LayerMask.NameToLayer(layerName);
            if (existing >= 0)
                return existing;

            UnityEngine.Object[] tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath(
                "ProjectSettings/TagManager.asset");
            if (tagManagerAssets.Length == 0)
                throw new InvalidOperationException("Không thể đọc ProjectSettings/TagManager.asset.");
            SerializedObject tagManager = new SerializedObject(tagManagerAssets[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            for (int index = 8; index < 31; index++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(index);
                if (!string.IsNullOrEmpty(layer.stringValue))
                    continue;
                layer.stringValue = layerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return index;
            }
            throw new InvalidOperationException("Không còn Unity layer trống cho LawyerBoard.");
        }

        private static int EnsureInteractionLayer(string layerName)
        {
            UnityEngine.Object settings = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                InteractionLayersPath);
            if (settings == null)
                throw new InvalidOperationException($"Không tìm thấy {InteractionLayersPath}.");
            SerializedObject serialized = new SerializedObject(settings);
            SerializedProperty names = serialized.FindProperty("m_LayerNames");
            for (int index = 0; index < names.arraySize; index++)
                if (names.GetArrayElementAtIndex(index).stringValue == layerName)
                    return index;
            for (int index = 2; index < 31; index++)
            {
                SerializedProperty entry = names.GetArrayElementAtIndex(index);
                if (!string.IsNullOrEmpty(entry.stringValue))
                    continue;
                entry.stringValue = layerName;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(settings);
                return index;
            }
            throw new InvalidOperationException("Không còn XRI Interaction Layer trống cho LawyerBoard.");
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void RemoveGeneratedChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
                UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        private static void RemoveRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                    return;
                }
        }

        private static void Assign(SerializedObject target, string propertyName, UnityEngine.Object value)
            => target.FindProperty(propertyName).objectReferenceValue = value;

        private static void AssignArray<T>(SerializedObject target, string propertyName, T[] values)
            where T : UnityEngine.Object
        {
            SerializedProperty property = target.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }

        private static void SetObject(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
