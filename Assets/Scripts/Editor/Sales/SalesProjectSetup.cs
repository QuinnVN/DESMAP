using System;
using System.Collections.Generic;
using System.Linq;
using NCKH.VR.Lawyer;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Jump;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

namespace NCKH.VR.Sales.Editor
{
    public static class SalesProjectSetup
    {
        public const string DataPath = "Assets/Data/Sales/Placeholder Sales Mission.asset";
        private const string FontPath = "Assets/Data/Sales/Sales Vietnamese.asset";
        private static TMP_FontAsset font;
        private static Material panel;
        private static Material accent;

        [MenuItem("NCKH/Sales/Set Up Part 1 In Open Sale Scene")]
        public static void Setup()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.path.StartsWith("Assets/Scenes/Sale/", StringComparison.Ordinal))
                throw new InvalidOperationException("Open the Sale scene before setup.");
            if (Find("Sales Mission") != null)
                throw new InvalidOperationException("Sales Mission already exists. Preserve existing wiring.");
            EnsureFolder("Assets/Data/Sales");
            EnsureFolder("Assets/Materials/Sales");
            var player = Find("VR Player");
            if (player == null) throw new InvalidOperationException("The open scene needs its VR Player.");
            Camera camera = player.GetComponentInChildren<Camera>(true);
            if (camera == null) throw new InvalidOperationException("VR Player camera missing.");
            DisableJump(player);
            EnsureBothControllerRays(player);
            // Preserve the rig and all bindings; disable the imported animated demo camera only.
            foreach (var root in scene.GetRootGameObjects())
                if (root != player && root.GetComponent<Camera>() != null) root.SetActive(false);
            camera.gameObject.tag = "MainCamera";
            camera.enabled = true;
            camera.cullingMask |= 1 << 9;
            // Sales uses the existing activation-only buttons: permit trigger activation while hovered.
            foreach (var behaviour in player.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null) continue;
                var serialized = new SerializedObject(behaviour);
                var hoveredActivation = serialized.FindProperty("m_AllowHoveredActivate");
                if (hoveredActivation == null) continue;
                hoveredActivation.boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            // Begin this mini-mission at the entrance; position the XR Origin, never its tracked camera.
            player.transform.SetPositionAndRotation(new Vector3(6.1f, .02f, -8.8f), Quaternion.Euler(0, 90, 0));
            ConfigureRenderers();
            font = CreateFont();
            panel = MakeMaterial("Panel", new Color(.018f, .033f, .049f), "Universal Render Pipeline/Unlit");
            accent = MakeMaterial("Accent", new Color(.06f, .34f, .28f), "Universal Render Pipeline/Unlit");
            var data = CreateData();
            var rootObject = new GameObject("Sales Mission");
            var mission = rootObject.AddComponent<SalesMissionController>();
            mission.data = data;
            mission.playerCamera = camera;
            mission.locomotion = player.GetComponentsInChildren<Behaviour>(true)
                .Where(b => b.GetType().Namespace != null && b.GetType().Namespace.Contains("Locomotion")
                    && b.GetType().Name.EndsWith("Provider", StringComparison.Ordinal)).ToArray();
            var npc = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Casual NPC.prefab"));
            npc.name = "Sales Customer - Minh";
            npc.transform.SetParent(rootObject.transform);
            npc.transform.SetPositionAndRotation(new Vector3(8.35f, .03f, -8.8f), Quaternion.Euler(0, 270, 0));
            mission.customer = npc.transform;
            ConfigureCustomerPose(npc, camera.transform);
            mission.voice = npc.AddComponent<AudioSource>();
            mission.voice.playOnAwake = false;
            mission.voice.spatialBlend = 0;
            var beacon = NewTransform("Customer Marker", rootObject.transform);
            beacon.SetPositionAndRotation(new Vector3(8.3f, 2.2f, -8.8f), Quaternion.Euler(0, 90, 0));
            mission.customerMarker = beacon.gameObject;
            Text(beacon, "KHÁCH HÀNG\nĐến gần để trò chuyện", Vector3.zero, 1.3f, .4f, 3.4f);

            var hud = NewTransform("Mission Objective", camera.transform);
            hud.localPosition = new Vector3(0, .43f, 1.4f);
            mission.objectiveText = Text(hud, "ĐẾN GẶP KHÁCH Ở CỬA CHÍNH", Vector3.zero, 1.8f, .12f, 2.8f);
            var dialogue = NewTransform("Customer Dialogue", rootObject.transform);
            mission.dialogueRoot = dialogue;
            Plate(dialogue, new Vector3(0, 0, .025f), new Vector3(1.85f, .64f, .025f), panel);
            mission.speakerText = Text(dialogue, "", new Vector3(-.02f, .23f, -.01f), 1.68f, .09f, 3.2f, TextAlignmentOptions.Left);
            mission.dialogueText = Text(dialogue, "", new Vector3(0, .035f, -.01f), 1.68f, .28f, 3.6f, TextAlignmentOptions.TopLeft);
            mission.nextText = Text(dialogue, "NHẤN A ĐỂ TIẾP TỤC", new Vector3(0, -.23f, -.03f), 1.68f, .09f, 2.3f);

            var display = NewTransform("Shoe Comparison Display", rootObject.transform);
            mission.displayRoot = display;
            var mask = Plate(display, new Vector3(0, 0, .45f), new Vector3(3.5f, 2.3f, .01f),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Lawyer/Lawyer Focus Board Mask.mat"));
            mission.focus = rootObject.AddComponent<LawyerBoardFocusLighting>();
            mission.focus.ConfigureInEditor(Array.Empty<Light>(), null, mask.GetComponent<Renderer>());
            var choices = NewTransform("Three Shoe Choices", display);
            mission.choicesRoot = choices.gameObject;
            Text(choices, "CHỌN ĐÔI GIÀY PHÙ HỢP NHẤT", new Vector3(0, .69f, -.02f), 2.8f, .16f, 4.4f);
            Text(choices, "Thông tin sản phẩm minh họa · So sánh với nhu cầu đã nghe", new Vector3(0, .53f, -.02f), 2.8f, .12f, 2.6f);
            for (int i = 0; i < 3; i++)
            {
                var shoe = data.shoes[i];
                Transform card = NewTransform("Shoe " + shoe.label, choices);
                card.localPosition = new Vector3((i - 1) * .96f, -.09f, 0);
                Plate(card, new Vector3(0, -.04f, .08f), new Vector3(.89f, 1.1f, .018f), panel);
                var action = card.gameObject.AddComponent<SalesActionTarget>();
                var collider = card.gameObject.AddComponent<BoxCollider>();
                collider.center = new Vector3(0, -.04f, .07f);
                collider.size = new Vector3(.9f, 1.1f, .56f);
                action.mission = mission;
                action.action = i;
                action.idleSpeed = shoe.rotationSpeed;
                action.caption = Text(card, shoe.label, new Vector3(0, .39f, -.03f), .8f, .14f, 5.1f);
                var pivot = NewTransform("Rotation Pivot", card);
                pivot.localPosition = new Vector3(0, .12f, -.03f);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(shoe.prefab);
                model.transform.SetParent(pivot, false);
                foreach (Collider c in model.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
                Bounds bounds = model.GetComponentInChildren<Renderer>().bounds;
                float size = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                Vector3 center = model.transform.InverseTransformPoint(bounds.center);
                model.transform.localScale = Vector3.one * (.61f / size);
                model.transform.localPosition = -Vector3.Scale(center, model.transform.localScale);
                pivot.localRotation = Quaternion.Euler(12, -25, 0);
                action.spinningModel = pivot;
                SetLayer(model.transform);
                Text(card, shoe.properties, new Vector3(0, -.29f, -.03f), .77f, .34f, 2.6f, TextAlignmentOptions.TopLeft);
                Text(card, "CHỌN " + shoe.label + "  ·  " + (i + 1), new Vector3(0, -.52f, -.03f), .8f, .09f, 2.6f);
                var interaction = card.GetComponent<XRSimpleInteractable>();
                interaction.selectMode = InteractableSelectMode.Single;
                interaction.colliders.Clear(); interaction.colliders.Add(collider);
            }
            Text(choices, "Trỏ tia vào giày để xoay nhanh hơn  ·  Bóp cò để chọn", new Vector3(0, -.8f, -.02f), 2.9f, .12f, 2.7f);
            Transform result = NewTransform("Selection Result", display);
            mission.resultRoot = result.gameObject;
            Plate(result, new Vector3(0, 0, .05f), new Vector3(2.4f, 1.3f, .02f), panel);
            mission.resultText = Text(result, "", new Vector3(0, .08f, -.03f), 2.1f, .92f, 3.6f, TextAlignmentOptions.TopLeft);
            Button(result, mission, -3, "Thử lại", new Vector3(0, -.5f, -.03f), new Vector2(.75f, .16f));
            dialogue.gameObject.SetActive(false);
            display.gameObject.SetActive(false);
            SetLayer(rootObject.transform);
            // NPC belongs to the shop, not to the overlay render layer.
            foreach (Transform t in npc.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 0;
            EditorUtility.SetDirty(mission);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Sales Part 1 setup complete.");
        }

        [MenuItem("NCKH/Sales/Apply A Button Dialogue Controls To Saved Sale Scene")]
        public static void ApplyDialogueControlsToSavedScene()
        {
            const string scenePath = "Assets/Scenes/Sale/Shoes store - showroom with footwear.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            SalesMissionController mission = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<SalesMissionController>(true))
                .Single();
            GameObject player = mission.playerCamera.transform.root.gameObject;
            DisableJump(player);

            SalesActionTarget[] dialogueButtons = mission.dialogueRoot
                .GetComponentsInChildren<SalesActionTarget>(true);
            foreach (SalesActionTarget dialogueButton in dialogueButtons)
                UnityEngine.Object.DestroyImmediate(dialogueButton.gameObject);

            TMP_Text[] dialogueLabels = mission.dialogueRoot.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text label in dialogueLabels)
            {
                if (label == mission.speakerText || label == mission.dialogueText) continue;
                UnityEngine.Object.DestroyImmediate(label.gameObject);
            }

            font = CreateFont();
            mission.nextText = Text(
                mission.dialogueRoot,
                "NHẤN A ĐỂ TIẾP TỤC",
                new Vector3(0, -.23f, -.03f),
                1.68f,
                .09f,
                2.3f);
            EditorUtility.SetDirty(mission);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Sales dialogue now advances with A and no longer uses conversation buttons.");
        }

        [MenuItem("NCKH/Sales/Repair Both Controller Rays In Open Sale Scene")]
        public static void RepairBothControllerRays()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.path.StartsWith("Assets/Scenes/Sale/", StringComparison.Ordinal))
                throw new InvalidOperationException("Open the Sale scene before repairing controller rays.");
            var player = Find("VR Player");
            if (player == null) throw new InvalidOperationException("The open scene needs its VR Player.");
            EnsureBothControllerRays(player);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Sales controller rays verified for both hands.");
        }

        private static void EnsureBothControllerRays(GameObject player)
        {
            var rays = player.GetComponentsInChildren<XRRayInteractor>(true);
            var leftHand = player.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Left hand");
            var rightHand = player.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Right hand");
            var leftDirect = leftHand != null ? leftHand.GetComponentInChildren<XRDirectInteractor>(true) : null;
            var rightDirect = rightHand != null ? rightHand.GetComponentInChildren<XRDirectInteractor>(true) : null;
            var leftGroup = leftHand != null ? leftHand.GetComponent<XRInteractionGroup>() : null;
            var rightRay = rightHand != null ? rightHand.GetComponentInChildren<XRRayInteractor>(true) : null;
            if (rightRay == null || leftHand == null || leftDirect == null || rightDirect == null || leftGroup == null)
                throw new InvalidOperationException("The VR Player needs its existing right ray and both direct interactors.");

            var leftRay = leftHand.GetComponentInChildren<XRRayInteractor>(true);
            if (leftRay == null)
            {
                var leftRayObject = UnityEngine.Object.Instantiate(rightRay.gameObject, leftHand, false);
                leftRayObject.name = "Left Ray Interactor";
                leftRay = leftRayObject.GetComponent<XRRayInteractor>();
                EditorUtility.SetDirty(leftRayObject);
            }
            ConfigureRayInput(rightRay, rightDirect, 2);
            ConfigureRayInput(leftRay, leftDirect, 1);

            var group = new SerializedObject(leftGroup);
            var members = group.FindProperty("m_StartingGroupMembers");
            bool registered = Enumerable.Range(0, members.arraySize)
                .Any(i => members.GetArrayElementAtIndex(i).objectReferenceValue == leftRay);
            if (!registered)
            {
                members.InsertArrayElementAtIndex(members.arraySize);
                members.GetArrayElementAtIndex(members.arraySize - 1).objectReferenceValue = leftRay;
                group.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(leftGroup);
        }

        private static void DisableJump(GameObject player)
        {
            JumpProvider jump = player.GetComponentInChildren<JumpProvider>(true);
            if (jump != null) jump.gameObject.SetActive(false);
        }

        private static void ConfigureRayInput(XRRayInteractor ray, XRDirectInteractor sourceInteractor, int handedness)
        {
            var target = new SerializedObject(ray);
            var source = new SerializedObject(sourceInteractor);
            target.CopyFromSerializedProperty(source.FindProperty("m_SelectInput"));
            target.CopyFromSerializedProperty(source.FindProperty("m_ActivateInput"));
            target.FindProperty("m_InteractionLayers.m_Bits").uintValue = 1;
            SerializedProperty raycastMask = target.FindProperty("m_RaycastMask.m_Bits");
            raycastMask.uintValue |= 1u << 9;
            target.FindProperty("m_Handedness").enumValueIndex = handedness;
            target.FindProperty("m_AllowHoveredActivate").boolValue = true;
            target.ApplyModifiedPropertiesWithoutUndo();
            XRInteractorLineVisual lineVisual = ray.GetComponent<XRInteractorLineVisual>();
            if (lineVisual == null)
                throw new InvalidOperationException(ray.name + " requires an XR Interactor Line Visual.");
            lineVisual.invalidColorGradient = CreateRayGradient(new Color(.2f, .75f, 1f), .7f, .08f);
            EditorUtility.SetDirty(lineVisual);
            EditorUtility.SetDirty(ray);
        }

        private static Gradient CreateRayGradient(Color color, float startAlpha, float endAlpha)
        {
            return new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(color, 1f)
                },
                alphaKeys = new[]
                {
                    new GradientAlphaKey(startAlpha, 0f),
                    new GradientAlphaKey(endAlpha, 1f)
                }
            };
        }

        private static SalesMissionData CreateData()
        {
            var data = AssetDatabase.LoadAssetAtPath<SalesMissionData>(DataPath);
            if (data != null) return data;
            data = ScriptableObject.CreateInstance<SalesMissionData>();
            data.dialogue = new[] {
                new SalesDialogueLine { text = "Chào bạn! Mình muốn tìm một đôi giày để chạy bộ nhẹ khoảng 3 đến 5 cây số mỗi sáng. Mình mới tập nên thích đôi nào êm chân, nhẹ và thoáng một chút." },
                new SalesDialogueLine { text = "Mình thường chạy trên đường nhựa, thỉnh thoảng đi bộ nữa. Ngân sách của mình khoảng một triệu rưỡi trở xuống. Bạn chọn giúp mình một đôi phù hợp nhé!" }
            };
            data.shoes = new[] {
                Shoe("1A", "Chạy bộ đường nhựa\nĐệm êm · Nhẹ · Thoáng\nGiá: 1.290.000 đ", "Phù hợp: 1A có đệm êm, nhẹ và thoáng cho chạy bộ đường nhựa; giá nằm trong ngân sách của Minh.", true, 17),
                Shoe("3B", "Bóng rổ trong nhà\nÔm cổ chân · Đế chắc\nGiá: 1.450.000 đ", "3B nằm trong ngân sách nhưng thiết kế cho bóng rổ. Đôi này chưa đáp ứng tốt nhu cầu chạy bộ nhẹ và thoáng của Minh.", false, 23),
                Shoe("8A", "Chạy địa hình\nĐế bám sâu · Chống nước\nGiá: 1.890.000 đ", "8A phù hợp địa hình gồ ghề nhưng vượt ngân sách. Khả năng chống nước cũng không phải ưu tiên Minh đã nêu.", false, 29)
            };
            AssetDatabase.CreateAsset(data, DataPath);
            return data;
        }

        private static SalesShoeData Shoe(string label, string properties, string feedback, bool best, float speed)
            => new SalesShoeData { label = label, properties = properties, feedback = feedback, bestFit = best, rotationSpeed = speed,
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Shoes store - showroom with footwear/Models/Prefabs/Shoes_" + label + ".prefab") };

        private static TMP_FontAsset CreateFont()
        {
            var result = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (result != null) return result;
            result = TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf"));
            result.name = "Sales Vietnamese";
            result.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            AssetDatabase.CreateAsset(result, FontPath);
            AssetDatabase.AddObjectToAsset(result.material, result);
            foreach (var texture in result.atlasTextures) AssetDatabase.AddObjectToAsset(texture, result);
            // Dedicated atlas prevents changing the user's shared TMP fallback asset.
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789đĐăĂâÂêÊôÔơƠưƯáàảãạấầẩẫậắằẳẵặéèẻẽẹếềểễệíìỉĩịóòỏõọốồổỗộớờởỡợúùủũụứừửữựýỳỷỹỵÁÀẢÃẠẤẦẨẪẬẮẰẲẴẶÉÈẺẼẸẾỀỂỄỆÍÌỈĨỊÓÒỎÕỌỐỒỔỖỘỚỜỞỠỢÚÙỦŨỤỨỪỬỮỰÝỲỶỸỴ →·:.,!-";
            result.TryAddCharacters(chars, out string missing);
            if (!string.IsNullOrEmpty(missing)) Debug.LogWarning("Sales font missing: " + missing);
            result.material.SetFloat("_ZTestMode", 8);
            EditorUtility.SetDirty(result);
            return result;
        }

        private static void ConfigureRenderers()
        {
            const string path = "Assets/Settings/Mobile_Renderer.asset";
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            if (renderer == null || renderer.rendererFeatures.OfType<LawyerFocusDimRendererFeature>().Any()) return;
            var feature = ScriptableObject.CreateInstance<LawyerFocusDimRendererFeature>();
            feature.name = "Lawyer Focus Dim";
            feature.ConfigureInEditor(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Lawyer/Lawyer Focus Dim.mat"), 1 << 9, 1u << 31);
            AssetDatabase.AddObjectToAsset(feature, renderer);
            renderer.rendererFeatures.Add(feature);
            EditorUtility.SetDirty(renderer);
        }

        private static Material MakeMaterial(string name, Color color, string shader)
        {
            string path = "Assets/Materials/Sales/" + name + ".mat";
            var result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result != null) return result;
            result = new Material(Shader.Find(shader)) { name = name };
            result.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(result, path);
            return result;
        }
        private static Transform NewTransform(string name, Transform parent)
        {
            var go = new GameObject(name); go.layer = 9; go.transform.SetParent(parent, false); return go.transform;
        }
        private static GameObject Plate(Transform parent, Vector3 position, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = "Panel"; go.layer = 9;
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = size;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = material;
            go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }
        private static TMP_Text Text(Transform parent, string value, Vector3 position, float width, float height, float size, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var t = NewTransform("Text", parent);
            var text = t.gameObject.AddComponent<TextMeshPro>(); text.font = font; text.fontSize = size * .2f;
            text.text = value; text.alignment = align; text.color = Color.white;
            text.rectTransform.sizeDelta = new Vector2(width, height);
            text.rectTransform.anchoredPosition3D = position;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }
        private static SalesActionTarget Button(Transform parent, SalesMissionController mission, int action, string label, Vector3 position, Vector2 size)
        {
            var t = NewTransform(label, parent); t.localPosition = position;
            Plate(t, new Vector3(0, 0, .025f), new Vector3(size.x, size.y, .025f), accent);
            var target = t.gameObject.AddComponent<SalesActionTarget>();
            var box = t.gameObject.AddComponent<BoxCollider>(); box.size = new Vector3(size.x, size.y, .08f);
            var interaction = t.GetComponent<XRSimpleInteractable>(); interaction.colliders.Clear(); interaction.colliders.Add(box);
            target.mission = mission; target.action = action;
            target.caption = Text(t, label, Vector3.zero, size.x * .95f, size.y * .9f, 2.8f);
            return target;
        }
        private static void SetLayer(Transform root)
        { foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 9; }
        private static GameObject Find(string name) => EditorSceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == name);
        private static void ConfigureCustomerPose(GameObject npc, Transform camera)
        {
            const string path = "Assets/Data/Sales/Customer Standing Pose.asset";
            var pose = AssetDatabase.LoadAssetAtPath<NCKH.VR.NPC.NpcHumanoidPose>(path);
            if (pose == null)
            {
                pose = ScriptableObject.CreateInstance<NCKH.VR.NPC.NpcHumanoidPose>();
                var muscles = new float[HumanTrait.MuscleCount];
                for (int i = 0; i < muscles.Length; i++)
                {
                    string name = HumanTrait.MuscleName[i];
                    if (name.Contains("Arm Down-Up")) muscles[i] = -.8f;
                    if (name.Contains("Shoulder Down-Up")) muscles[i] = -.2f;
                    if (name.Contains("Forearm Stretch")) muscles[i] = 1f;
                }
                pose.Configure(new Vector3(0, .95f, 0), Quaternion.identity, muscles);
                AssetDatabase.CreateAsset(pose, path);
            }
            var motion = new SerializedObject(npc.GetComponent<NCKH.VR.NPC.NpcMotionPlayer>());
            motion.FindProperty("mode").enumValueIndex = 1;
            motion.FindProperty("staticPose").objectReferenceValue = pose;
            motion.FindProperty("trackHead").boolValue = true;
            motion.FindProperty("lookTarget").objectReferenceValue = camera;
            motion.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Split('/').Skip(1)) { if (!AssetDatabase.IsValidFolder(current + "/" + part)) AssetDatabase.CreateFolder(current, part); current += "/" + part; }
        }
    }
}
