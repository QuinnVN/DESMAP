using System;
using System.Collections.Generic;
using UnityEngine;

namespace NCKH.VR.NPC
{
    public enum NpcGender
    {
        Female,
        Male
    }

    public enum NpcHairMode
    {
        ExternalPrefab,
        EmbeddedRenderer
    }

    [DisallowMultipleComponent]
    public sealed class NpcAppearanceRandomizer : MonoBehaviour
    {
        private const string HaircutPrefix = "npc_haircut_";
        private const string FacialHairPrefix = "npc_hairfacial_";

        [Header("Placement")]
        [SerializeField] private Transform appearanceRoot;

        [Header("Complete Models")]
        [SerializeField] private GameObject[] femaleModelPrefabs = Array.Empty<GameObject>();
        [SerializeField] private GameObject[] maleModelPrefabs = Array.Empty<GameObject>();

        [Header("Hair")]
        [SerializeField] private NpcHairMode hairMode = NpcHairMode.ExternalPrefab;
        [SerializeField] private GameObject[] hairstylePrefabs = Array.Empty<GameObject>();
        [SerializeField] private GameObject[] maleHairstylePrefabs = Array.Empty<GameObject>();
        [SerializeField] private Material[] hairMaterials = Array.Empty<Material>();
        [SerializeField] private string embeddedHairPrefix = "Hairs";
        [SerializeField, Range(0f, 0.01f)] private float externalHairSurfaceOffset;

        [Header("Lifecycle")]
        [SerializeField] private bool randomizeOnAwake = true;

        private readonly Dictionary<string, Transform> targetBones = new Dictionary<string, Transform>(96);
        private readonly List<SkinnedMeshRenderer> embeddedHairRenderers = new List<SkinnedMeshRenderer>(8);
        private readonly List<Mesh> generatedHairMeshes = new List<Mesh>(4);
        private GameObject activeModel;
        private GameObject activeExternalHair;
        private bool configurationErrorRaised;

        public NpcGender CurrentGender { get; private set; }
        public GameObject CurrentModelPrefab { get; private set; }
        public GameObject CurrentModelInstance => activeModel;
        public GameObject CurrentHairObject { get; private set; }
        public event Action<GameObject> AppearanceCreated;

        private void Awake()
        {
            if (randomizeOnAwake)
                RandomizeAppearance();
        }

        public bool RandomizeAppearance()
        {
            ClearAppearance();

            if (!TryValidateConfiguration())
                return false;

            CurrentGender = SelectGender(UnityEngine.Random.value);
            GameObject[] modelPrefabs = CurrentGender == NpcGender.Female
                ? femaleModelPrefabs
                : maleModelPrefabs;
            CurrentModelPrefab = modelPrefabs[SelectIndex(UnityEngine.Random.value, modelPrefabs.Length)];

            activeModel = InstantiateUnderRoot(CurrentModelPrefab, "NPC Model");
            if (activeModel == null)
                return FailAppearance("Không thể tạo mô hình NPC đã chọn.");

            DisablePackScripts(activeModel);

            if (!TryBindModelRenderersToHumanoidSkeleton())
                return false;

            bool hairReady = hairMode == NpcHairMode.ExternalPrefab
                ? TryAttachExternalHair()
                : TrySelectEmbeddedHair();
            if (!hairReady)
                return false;

            HideExcludedVisuals(activeModel, hairMode == NpcHairMode.ExternalPrefab);
            StripLowerLods(activeModel);
            if (activeExternalHair != null)
                StripLowerLods(activeExternalHair);

            configurationErrorRaised = false;
            AppearanceCreated?.Invoke(activeModel);
            return true;
        }

        public static NpcGender SelectGender(float sample)
        {
            return Mathf.Clamp01(sample) < 0.5f ? NpcGender.Female : NpcGender.Male;
        }

        public static int SelectIndex(float sample, int optionCount)
        {
            if (optionCount <= 0)
                return -1;

            return Mathf.Min(optionCount - 1, Mathf.FloorToInt(Mathf.Clamp01(sample) * optionCount));
        }

        public static int ParseLodIndex(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                return -1;

            int marker = objectName.LastIndexOf("_lod", StringComparison.OrdinalIgnoreCase);
            int digitIndex = marker + 4;
            if (marker < 0 || digitIndex >= objectName.Length)
                return -1;

            int value = 0;
            bool foundDigit = false;
            for (int index = digitIndex; index < objectName.Length; index++)
            {
                char character = objectName[index];
                if (character < '0' || character > '9')
                    break;

                foundDigit = true;
                value = (value * 10) + character - '0';
            }

            return foundDigit ? value : -1;
        }

        private bool TryValidateConfiguration()
        {
            if (appearanceRoot == null)
                return FailConfiguration("NpcAppearanceRandomizer cần Appearance Root.");
            if (!HasOnlyAssignedEntries(femaleModelPrefabs) || !HasOnlyAssignedEntries(maleModelPrefabs))
                return FailConfiguration("Danh sách mô hình NPC nam và nữ phải có ít nhất một prefab hợp lệ.");
            if (hairMode == NpcHairMode.ExternalPrefab && !HasOnlyAssignedEntries(hairstylePrefabs))
                return FailConfiguration("Danh sách kiểu tóc NPC phải có ít nhất một prefab hợp lệ.");
            if (hairMode == NpcHairMode.ExternalPrefab && !HasOnlyAssignedEntries(hairMaterials))
                return FailConfiguration("Danh sách màu tóc NPC phải có ít nhất một vật liệu hợp lệ.");
            if (hairMode == NpcHairMode.ExternalPrefab
                && maleHairstylePrefabs != null && maleHairstylePrefabs.Length > 0
                && !HasOnlyAssignedEntries(maleHairstylePrefabs))
            {
                return FailConfiguration("Danh sách kiểu tóc NPC nam không được chứa prefab trống.");
            }
            if (hairMode == NpcHairMode.EmbeddedRenderer && string.IsNullOrWhiteSpace(embeddedHairPrefix))
                return FailConfiguration("Tên tiền tố tóc có sẵn không được để trống.");

            return true;
        }

        private bool TryAttachExternalHair()
        {
            GameObject[] hairOptions = CurrentGender == NpcGender.Male
                && maleHairstylePrefabs != null && maleHairstylePrefabs.Length > 0
                    ? maleHairstylePrefabs
                    : hairstylePrefabs;
            GameObject hairPrefab = hairOptions[SelectIndex(UnityEngine.Random.value, hairOptions.Length)];
            Material hairMaterial = hairMaterials[SelectIndex(UnityEngine.Random.value, hairMaterials.Length)];
            activeExternalHair = InstantiateUnderRoot(hairPrefab, "NPC Hair");
            if (activeExternalHair == null)
                return FailAppearance($"Không thể tạo kiểu tóc '{hairPrefab.name}'.");

            SkinnedMeshRenderer[] renderers = activeExternalHair.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length == 0)
                return FailAppearance($"Kiểu tóc '{hairPrefab.name}' không có SkinnedMeshRenderer.");

            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                SkinnedMeshRenderer renderer = renderers[rendererIndex];
                Transform[] sourceBones = renderer.bones;
                Transform[] reboundBones = new Transform[sourceBones.Length];
                Mesh sourceMesh = renderer.sharedMesh;
                Matrix4x4[] sourceBindPoses = sourceMesh != null ? sourceMesh.bindposes : null;
                if (sourceMesh == null || sourceBindPoses.Length != sourceBones.Length)
                    return FailAppearance($"Kiểu tóc '{hairPrefab.name}' không có dữ liệu bind pose hợp lệ.");

                Matrix4x4[] reboundBindPoses = new Matrix4x4[sourceBones.Length];
                for (int boneIndex = 0; boneIndex < sourceBones.Length; boneIndex++)
                {
                    Transform sourceBone = sourceBones[boneIndex];
                    if (sourceBone == null || !targetBones.TryGetValue(sourceBone.name, out Transform targetBone))
                        return FailAppearance($"Không thể gắn kiểu tóc '{hairPrefab.name}' vào bộ xương NPC.");

                    reboundBones[boneIndex] = targetBone;
                    // Preserve the hair's source deformation at the instant it is attached,
                    // then let subsequent Humanoid motion come from the selected model.
                    reboundBindPoses[boneIndex] = targetBone.worldToLocalMatrix
                        * sourceBone.localToWorldMatrix * sourceBindPoses[boneIndex];
                }

                Mesh reboundMesh = Instantiate(sourceMesh);
                reboundMesh.name = sourceMesh.name + " (NPC Rebound)";
                reboundMesh.bindposes = reboundBindPoses;
                ExpandMeshSurface(reboundMesh, externalHairSurfaceOffset);
                renderer.sharedMesh = reboundMesh;
                renderer.bones = reboundBones;
                renderer.sharedMaterial = hairMaterial;
                renderer.updateWhenOffscreen = true;
                generatedHairMeshes.Add(reboundMesh);
                if (renderer.rootBone != null && targetBones.TryGetValue(renderer.rootBone.name, out Transform rootBone))
                    renderer.rootBone = rootBone;
            }

            CurrentHairObject = activeExternalHair;
            return true;
        }

        private bool TrySelectEmbeddedHair()
        {
            embeddedHairRenderers.Clear();
            SkinnedMeshRenderer[] renderers = activeModel.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                SkinnedMeshRenderer renderer = renderers[index];
                if (renderer.name.StartsWith(embeddedHairPrefix, StringComparison.OrdinalIgnoreCase))
                    embeddedHairRenderers.Add(renderer);
            }

            if (embeddedHairRenderers.Count == 0)
                return FailAppearance($"Mô hình '{CurrentModelPrefab.name}' không có kiểu tóc phù hợp.");

            int selectedIndex = SelectIndex(UnityEngine.Random.value, embeddedHairRenderers.Count);
            for (int index = 0; index < embeddedHairRenderers.Count; index++)
                embeddedHairRenderers[index].gameObject.SetActive(index == selectedIndex);

            CurrentHairObject = embeddedHairRenderers[selectedIndex].gameObject;
            return true;
        }

        private GameObject InstantiateUnderRoot(GameObject prefab, string instanceName)
        {
            if (prefab == null || appearanceRoot == null)
                return null;

            GameObject instance = Instantiate(prefab, appearanceRoot, false);
            instance.name = instanceName;
            Transform instanceTransform = instance.transform;
            instanceTransform.localPosition = Vector3.zero;
            instanceTransform.localRotation = Quaternion.identity;
            instanceTransform.localScale = Vector3.one;
            return instance;
        }

        private static void DisablePackScripts(GameObject model)
        {
            MonoBehaviour[] behaviours = model.GetComponentsInChildren<MonoBehaviour>(true);
            for (int index = 0; index < behaviours.Length; index++)
                behaviours[index].enabled = false;
        }

        private static void HideExcludedVisuals(GameObject model, bool hideModelHaircuts)
        {
            Transform[] transforms = model.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                Transform candidate = transforms[index];
                string candidateName = candidate.name;
                if (candidateName.Equals("Hat", StringComparison.OrdinalIgnoreCase)
                    || candidateName.Equals("Glasses", StringComparison.OrdinalIgnoreCase)
                    || (hideModelHaircuts
                        && candidateName.StartsWith(HaircutPrefix, StringComparison.OrdinalIgnoreCase))
                    || candidateName.StartsWith(FacialHairPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    candidate.gameObject.SetActive(false);
                }
            }
        }

        private static void StripLowerLods(GameObject visualRoot)
        {
            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                int lodIndex = ParseLodIndex(renderer.name);
                if (lodIndex <= 0)
                    continue;

                renderer.forceRenderingOff = true;
                renderer.enabled = false;
                DestroyObject(renderer);
            }

            LODGroup[] lodGroups = visualRoot.GetComponentsInChildren<LODGroup>(true);
            for (int index = 0; index < lodGroups.Length; index++)
                DestroyObject(lodGroups[index]);
        }

        private static void ExpandMeshSurface(Mesh mesh, float distance)
        {
            if (mesh == null || distance <= 0f)
                return;

            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            if (vertices.Length != normals.Length)
                return;

            for (int index = 0; index < vertices.Length; index++)
                vertices[index] += normals[index] * distance;
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
        }

        private void CacheTargetBones(Transform modelRoot)
        {
            targetBones.Clear();
            Animator animator = modelRoot.GetComponentInChildren<Animator>(true);
            if (animator == null || !animator.isHuman || !animator.avatar.isValid)
                return;

            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hips == null)
                return;

            Transform skeletonRoot = hips.parent != null ? hips.parent : hips;
            Transform[] transforms = skeletonRoot.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                Transform target = transforms[index];
                if (!targetBones.ContainsKey(target.name))
                    targetBones.Add(target.name, target);
            }

            // Imported modular characters can contain duplicate, same-named skeletons
            // under clothing or hair. The Avatar is authoritative for Humanoid bones.
            for (int index = 0; index < (int)HumanBodyBones.LastBone; index++)
            {
                Transform humanBone = animator.GetBoneTransform((HumanBodyBones)index);
                if (humanBone != null)
                    targetBones[humanBone.name] = humanBone;
            }
        }

        private bool TryBindModelRenderersToHumanoidSkeleton()
        {
            CacheTargetBones(activeModel.transform);
            if (targetBones.Count == 0)
                return FailAppearance($"Mô hình '{CurrentModelPrefab.name}' không có bộ xương Humanoid hợp lệ.");

            SkinnedMeshRenderer[] renderers = activeModel.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                SkinnedMeshRenderer renderer = renderers[rendererIndex];
                Transform[] sourceBones = renderer.bones;
                Transform[] reboundBones = new Transform[sourceBones.Length];
                for (int boneIndex = 0; boneIndex < sourceBones.Length; boneIndex++)
                {
                    Transform sourceBone = sourceBones[boneIndex];
                    if (sourceBone == null || !targetBones.TryGetValue(sourceBone.name, out Transform targetBone))
                    {
                        return FailAppearance(
                            $"Không thể gắn '{renderer.name}' vào bộ xương của mô hình '{CurrentModelPrefab.name}'.");
                    }

                    reboundBones[boneIndex] = targetBone;
                }

                renderer.bones = reboundBones;
                renderer.updateWhenOffscreen = true;
                if (renderer.rootBone != null
                    && targetBones.TryGetValue(renderer.rootBone.name, out Transform rootBone))
                {
                    renderer.rootBone = rootBone;
                }
            }

            return true;
        }

        public void ClearAppearance()
        {
            for (int meshIndex = 0; meshIndex < generatedHairMeshes.Count; meshIndex++)
                DestroyObject(generatedHairMeshes[meshIndex]);
            generatedHairMeshes.Clear();

            if (appearanceRoot != null)
            {
                for (int childIndex = appearanceRoot.childCount - 1; childIndex >= 0; childIndex--)
                {
                    GameObject child = appearanceRoot.GetChild(childIndex).gameObject;
                    child.SetActive(false);
                    DestroyObject(child);
                }
            }

            activeModel = null;
            activeExternalHair = null;
            CurrentModelPrefab = null;
            CurrentHairObject = null;
            targetBones.Clear();
            embeddedHairRenderers.Clear();
        }

        private bool FailConfiguration(string message)
        {
            if (!configurationErrorRaised)
            {
                configurationErrorRaised = true;
                Debug.LogError(message, this);
            }

            return false;
        }

        private bool FailAppearance(string message)
        {
            Debug.LogError(message, this);
            ClearAppearance();
            return false;
        }

        private static bool HasOnlyAssignedEntries<T>(T[] assets) where T : UnityEngine.Object
        {
            if (assets == null || assets.Length == 0)
                return false;

            for (int index = 0; index < assets.Length; index++)
            {
                if (assets[index] == null)
                    return false;
            }

            return true;
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }
    }
}
