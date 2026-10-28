using System;
using System.Collections.Generic;
using System.Linq;
using NCKH.VR.NPC;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace NCKH.VR.Sales.Editor
{
    /// <summary>
    /// Authoring for the returning-customer scenario. This deliberately lives
    /// beside, rather than inside, the Part 1 setup so rerunning it cannot
    /// rewrite the Part 1 shoe-comparison contract.
    /// </summary>
    public static class SalesPart2ProjectSetup
    {
        public const string ScenePath = "Assets/Scenes/Sale/Store.unity";
        public const string ScenarioPath = "Assets/Data/Sales/Sales Part 2 Difficult Customer.asset";
        private const string FontPath = "Assets/ThirdParty/Project Fonts/Sales Vietnamese.asset";
        private const string FemaleCustomerPrefabPath =
            "Assets/ThirdParty/npc_casual_set_00/Prefabs/npc_csl_00_character_01f_03.prefab";
        private const string WalkClipPath =
            "Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Female/Movement/Walk/HumanF@Walk01_Forward.fbx";
        private const string IdleClipPath =
            "Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Female/Idles/HumanF@Idle01.fbx";

        [MenuItem("NCKH/Sales/Set Up Part 2 Difficult Customer In Store Scene")]
        public static void SetupPart2InOpenSaleScene()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            if (!string.Equals(scene.path, ScenePath, StringComparison.Ordinal))
                throw new InvalidOperationException("Mở Assets/Scenes/Sale/Store.unity trước khi thiết lập Phần 2.");

            SalesMissionController part1 = FindSingle<SalesMissionController>(scene);
            if (part1 == null)
                throw new InvalidOperationException("Cảnh Sale cần có Sales Mission của Phần 1.");
            GameObject player = part1.playerCamera != null ? part1.playerCamera.transform.root.gameObject : Find("VR Player", scene);
            if (player == null)
                throw new InvalidOperationException("Cảnh Sale cần có VR Player.");
            Camera camera = part1.playerCamera != null ? part1.playerCamera : player.GetComponentInChildren<Camera>(true);
            if (camera == null)
                throw new InvalidOperationException("VR Player cần có camera theo dõi.");

            EnsureFolder("Assets/Data/Sales");
            TMP_FontAsset font = EnsureVietnameseFont();
            SalesPart2ScenarioData scenario = EnsureScenario();
            Transform authoringRoot = EnsureTransform(part1.transform, "Sales Part 2 Authoring");
            Transform anchors = EnsureTransform(authoringRoot, "Authored Anchors");

            Transform playerAnchor = EnsureAnchor(anchors, "Cashier Anchor", new Vector3(9.05f, .02f, 0f), new Vector3(0f, 270f, 0f));
            Transform customerStart = EnsureAnchor(anchors, "Lan Entrance", new Vector3(8.35f, .03f, -8.8f), new Vector3(0f, 90f, 0f));
            Transform waypointOne = EnsureAnchor(anchors, "Lan Waypoint 01", new Vector3(7.15f, .03f, -6f), new Vector3(0f, 90f, 0f));
            Transform waypointTwo = EnsureAnchor(anchors, "Lan Waypoint 02", new Vector3(7.15f, .03f, -2.3f), new Vector3(0f, 90f, 0f));
            Transform customerStop = EnsureAnchor(anchors, "Lan Counter Stop", new Vector3(7.5f, .03f, 0f), new Vector3(0f, 90f, 0f));
            Transform headTarget = camera.transform;

            Transform customer = EnsureLan(part1.transform, customerStart);
            Animator customerAnimator = customer.GetComponentInChildren<Animator>(true);
            AnimationClip walkClip = LoadClip(WalkClipPath, "HumanF@Walk01_Forward");
            AnimationClip idleClip = LoadClip(IdleClipPath, "HumanF@Idle01");
            if (customerAnimator == null)
                Debug.LogWarning("Lan chưa có Animator Humanoid; hãy kiểm tra prefab nữ đã nhập đúng avatar.", customer);

            Transform hud = EnsureTransform(camera.transform, "Sales Part 2 HUD");
            hud.localScale = Vector3.one * .45f;
            Material panelMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sales/Panel.mat");
            if (panelMaterial != null)
            {
                EnsureHudPlate(hud, "Dialogue Panel", new Vector3(0f, -.12f, 1.37f), new Vector3(1.95f, .58f, .025f), panelMaterial);
                EnsureHudPlate(hud, "Policy Panel", new Vector3(-.92f, .23f, 1.37f), new Vector3(.9f, .66f, .025f), panelMaterial);
                EnsureHudPlate(hud, "Objective Panel", new Vector3(0f, .43f, 1.37f), new Vector3(2.35f, .23f, .025f), panelMaterial);
                EnsureHudPlate(hud, "Status Panel", new Vector3(0f, -.53f, 1.37f), new Vector3(2.35f, .38f, .025f), panelMaterial);
            }
            TMP_Text customerText = EnsureHudText(hud, "Lan Reply", "", new Vector3(0f, -.12f, 1.35f), new Vector2(1.8f, .42f), 2.8f, TextAlignmentOptions.TopLeft, font);
            TMP_Text objectiveText = EnsureHudText(hud, "Current Objective", scenario.ObjectiveText(SalesPart2Objective.ListeningAndClarifying), new Vector3(0f, .43f, 1.35f), new Vector2(2.25f, .16f), 2.7f, TextAlignmentOptions.Center, font);
            TMP_Text policyText = EnsureHudText(hud, "Store Policy", scenario.storePolicy.DisplayText, new Vector3(-.92f, .23f, 1.35f), new Vector2(.82f, .52f), 2.2f, TextAlignmentOptions.TopLeft, font);
            TMP_Text statusText = EnsureHudText(hud, "Part 2 Status", "ĐANG CHỜ KHÁCH HÀNG", new Vector3(0f, -.42f, 1.35f), new Vector2(1.8f, .13f), 1.8f, TextAlignmentOptions.Center, font);
            TMP_Text timerText = EnsureHudText(hud, "Active Timer", "05:00", new Vector3(.62f, -.35f, 1.35f), new Vector2(.7f, .17f), 2.2f, TextAlignmentOptions.Center, font);
            TMP_Text trustStateText = EnsureHudText(hud, "Trust State", "", new Vector3(0f, -.65f, 1.35f), new Vector2(2.35f, .25f), 2.2f, TextAlignmentOptions.Center, font);

            CanvasGroup fadeCanvas = EnsureFadeCanvas(camera.transform);
            Behaviour[] locomotion = player.GetComponentsInChildren<Behaviour>(true)
                .Where(b => b != null && b.GetType().Namespace != null
                    && b.GetType().Namespace.IndexOf("Locomotion", StringComparison.Ordinal) >= 0
                    && b.GetType().Name.EndsWith("Provider", StringComparison.Ordinal))
                .ToArray();

            SalesPart2Controller controller = part1.GetComponent<SalesPart2Controller>();
            if (controller == null)
                controller = part1.gameObject.AddComponent<SalesPart2Controller>();
            controller.scenario = scenario;
            controller.hudRoot = hud.gameObject;
            controller.xrOrigin = player.transform;
            controller.playerAnchor = playerAnchor;
            controller.customer = customer;
            controller.customerStart = customerStart;
            controller.customerWaypoints = new[] { waypointOne, waypointTwo };
            controller.customerStop = customerStop;
            controller.customerAnimator = customerAnimator;
            controller.walkClip = walkClip;
            controller.idleClip = idleClip;
            controller.headTarget = headTarget;
            controller.fadeCanvas = fadeCanvas;
            controller.customerText = customerText;
            controller.objectiveText = objectiveText;
            controller.policyText = policyText;
            controller.statusText = statusText;
            controller.timerText = timerText;
            controller.trustStateText = trustStateText;
            controller.recordingIndicator = part1.recordingIndicator;
            controller.locomotion = locomotion;
            SalesPart2RequestService service = part1.GetComponent<SalesPart2RequestService>();
            if (service == null)
                service = part1.gameObject.AddComponent<SalesPart2RequestService>();
            controller.requestService = service;
            var adapter = part1.GetComponent<SalesRemoteSceneAdapter>();
            if (adapter == null) adapter = part1.gameObject.AddComponent<SalesRemoteSceneAdapter>();
            adapter.part1 = part1; adapter.part2 = controller;
            EditorUtility.SetDirty(adapter);
            // Part 1 owns the initial scene. The runtime transition explicitly
            // enables this controller, Lan, and the Part 2 HUD after its upload
            // has completed and the fade is covering the XR Origin reposition.
            controller.enabled = false;
            customer.gameObject.SetActive(false);
            hud.gameObject.SetActive(false);

            EditorUtility.SetDirty(scenario);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Đã thiết lập Sales Phần 2: khách hàng quay lại Lan, tuyến đường, HUD và chính sách.");
        }

        public static SalesPart2ScenarioData EnsureScenarioAssetForTests()
        {
            EnsureFolder("Assets/Data/Sales");
            return EnsureScenario();
        }

        private static SalesPart2ScenarioData EnsureScenario()
        {
            SalesPart2ScenarioData scenario = AssetDatabase.LoadAssetAtPath<SalesPart2ScenarioData>(ScenarioPath);
            if (scenario == null)
            {
                scenario = ScriptableObject.CreateInstance<SalesPart2ScenarioData>();
                AssetDatabase.CreateAsset(scenario, ScenarioPath);
            }

            if (scenario.customerProfile == null) scenario.customerProfile = new SalesCustomerProfile();
            if (scenario.storePolicy == null) scenario.storePolicy = new SalesStorePolicy();
            if (scenario.authoredEndings == null) scenario.authoredEndings = new SalesPart2AuthoredEndings();
            scenario.customerProfile.name = "Lan";
            scenario.customerProfile.age = 22;
            scenario.customerProfile.role = "Sinh viên đại học";
            scenario.customerProfile.playerAddress = "em";
            scenario.customerProfile.customerAddress = "chị";
            scenario.openingComplaint = "Em ơi, chị muốn đổi đôi giày này. Chị mới mua ở đây ba ngày trước, nhưng mang vào thì bị đau chân. Lần trước em tư vấn đôi này phù hợp với chị nên chị khá thất vọng.";
            scenario.storePolicy.title = "CHÍNH SÁCH ĐỔI HÀNG";
            scenario.storePolicy.eligibility = "Trong 7 ngày • Giày còn nguyên vẹn";
            scenario.storePolicy.exclusions = "Không hoàn tiền hoặc giảm giá";
            scenario.storePolicy.escalation = "Tự xử lý, không chuyển quản lý";
            scenario.mandatoryTrustChallenge = "Nhưng lần trước em cũng tư vấn đôi này phù hợp với chị. Làm sao chị biết lần này sẽ không gặp vấn đề tương tự?";
            scenario.objectives = new[]
            {
                new SalesPart2ObjectiveData(SalesPart2Objective.ListeningAndClarifying, "LẮNG NGHE VÀ LÀM RÕ PHÀN NÀN"),
                new SalesPart2ObjectiveData(SalesPart2Objective.FindingRootCause, "TÌM NGUYÊN NHÂN THỰC SỰ"),
                new SalesPart2ObjectiveData(SalesPart2Objective.ProposeSuitableSolution, "ĐỀ XUẤT GIẢI PHÁP PHÙ HỢP"),
                new SalesPart2ObjectiveData(SalesPart2Objective.RebuildTrust, "KHÔI PHỤC NIỀM TIN")
            };
            scenario.authoredEndings.restored = "Ừ, vậy chị sẽ thử đôi nhẹ hơn. Lần sau chị vẫn sẽ quay lại nhờ em tư vấn.";
            scenario.authoredEndings.partiallyRestored = "Được rồi, chị sẽ thử đổi sang đôi này trước.";
            scenario.authoredEndings.lost = "Thôi, chị không muốn tiếp tục nữa. Lần sau chị sẽ mua ở chỗ khác.";
            scenario.fixedFactIds = new[]
            {
                "correct_size", "undamaged_exchange_eligible", "full_day_at_university",
                "walked_bus_stop_to_campus", "walked_between_classes", "late_day_discomfort",
                "previous_shoes_lighter", "thin_socks", "likes_color", "needs_mismatch_heavy_shoes"
            };
            EditorUtility.SetDirty(scenario);
            return scenario;
        }

        private static Transform EnsureLan(Transform parent, Transform start)
        {
            Transform existing = parent.Find("Lan - Returning Customer");
            if (existing == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FemaleCustomerPrefabPath);
                if (prefab == null)
                    throw new InvalidOperationException("Không tìm thấy prefab humanoid nữ cho Lan: " + FemaleCustomerPrefabPath);
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                instance.name = "Lan - Returning Customer";
                existing = instance.transform;
            }

            existing.SetPositionAndRotation(start.position, start.rotation);
            existing.localScale = Vector3.one;
            foreach (Renderer renderer in existing.GetComponentsInChildren<Renderer>(true))
                renderer.shadowCastingMode = ShadowCastingMode.On;
            return existing;
        }

        private static CanvasGroup EnsureFadeCanvas(Transform camera)
        {
            Transform existing = camera.Find("Sales Part 2 Fade");
            GameObject fade = existing != null ? existing.gameObject : new GameObject("Sales Part 2 Fade");
            if (existing == null) fade.transform.SetParent(camera, false);
            fade.layer = 9;
            Canvas canvas = fade.GetComponent<Canvas>();
            if (canvas == null) canvas = fade.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera.GetComponent<Camera>();
            canvas.sortingOrder = 100;
            RectTransform rect = fade.GetComponent<RectTransform>();
            rect.localPosition = new Vector3(0f, 0f, .22f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = new Vector3(.002f, .002f, .002f);
            rect.sizeDelta = new Vector2(1000f, 1000f);
            Image image = fade.GetComponentInChildren<Image>(true);
            if (image == null)
            {
                GameObject imageObject = new GameObject("Black Fade Panel", typeof(RectTransform), typeof(Image));
                imageObject.transform.SetParent(fade.transform, false);
                imageObject.layer = 9;
                image = imageObject.GetComponent<Image>();
            }
            RectTransform imageRect = image.rectTransform;
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            image.color = Color.black;
            CanvasGroup group = fade.GetComponent<CanvasGroup>();
            if (group == null) group = fade.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return group;
        }

        private static TMP_Text EnsureHudText(Transform parent, string name, string value, Vector3 position,
            Vector2 size, float fontSize, TextAlignmentOptions alignment, TMP_FontAsset font)
        {
            Transform existing = parent.Find(name);
            GameObject go = existing != null ? existing.gameObject : new GameObject(name);
            if (existing == null) go.transform.SetParent(parent, false);
            go.layer = 9;
            TextMeshPro text = go.GetComponent<TextMeshPro>();
            if (text == null) text = go.AddComponent<TextMeshPro>();
            text.font = font;
            text.fontSize = fontSize * .2f;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.rectTransform.sizeDelta = size;
            text.rectTransform.anchoredPosition3D = position;
            text.text = value;
            return text;
        }

        private static GameObject EnsureHudPlate(Transform parent, string name, Vector3 position,
            Vector3 size, Material material)
        {
            Transform existing = parent.Find(name);
            GameObject plate = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (existing == null) plate.transform.SetParent(parent, false);
            plate.name = name;
            plate.layer = 9;
            plate.transform.localPosition = position;
            plate.transform.localRotation = Quaternion.identity;
            plate.transform.localScale = size;
            Collider collider = plate.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            Renderer renderer = plate.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            plate.transform.SetAsFirstSibling();
            return plate;
        }

        private static TMP_FontAsset EnsureVietnameseFont()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
                throw new InvalidOperationException("Thiếu font Sales Vietnamese: " + FontPath);
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789đĐăĂâÂêÊôÔơƠưƯáàảãạấầẩẫậắằẳẵặéèẻẽẹếềểễệíìỉĩịóòỏõọốồổỗộớờởỡợúùủũụứừửữựýỳỷỹỵÁÀẢÃẠẤẦẨẪẬẮẰẲẴẶÉÈẺẼẸẾỀỂỄỆÍÌỈĨỊÓÒỎÕỌỐỒỔỖỘỚỜỞỠỢÚÙỦŨỤỨỪỬỮỰÝỲỶỸỴ •:,.!?";
            font.TryAddCharacters(chars, out string missing);
            if (!string.IsNullOrEmpty(missing))
                Debug.LogWarning("Sales Part 2 font còn thiếu glyph: " + missing);
            EditorUtility.SetDirty(font);
            return font;
        }

        private static AnimationClip LoadClip(string path, string preferredName)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            AnimationClip clip = assets.OfType<AnimationClip>().FirstOrDefault(a => a.name == preferredName)
                ?? assets.OfType<AnimationClip>().FirstOrDefault();
            if (clip == null)
                Debug.LogWarning("Không tìm thấy AnimationClip tại " + path);
            return clip;
        }

        private static Transform EnsureAnchor(Transform parent, string name, Vector3 position, Vector3 euler)
        {
            Transform anchor = EnsureTransform(parent, name);
            anchor.SetPositionAndRotation(position, Quaternion.Euler(euler));
            anchor.gameObject.hideFlags = HideFlags.None;
            return anchor;
        }

        private static Transform EnsureTransform(Transform parent, string name)
        {
            Transform result = parent.Find(name);
            if (result != null) return result;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static GameObject Find(string name, Scene scene)
            => scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);

        private static T FindSingle<T>(Scene scene) where T : Component
            => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).FirstOrDefault();

        private static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Split('/').Skip(1))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }
    }
}
