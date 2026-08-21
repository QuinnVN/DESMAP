using System;
using System.Collections.Generic;
using UnityEngine;

namespace NCKH.VR.Clinic
{
    public enum PatientGender
    {
        Female,
        Male
    }

    [DisallowMultipleComponent]
    public sealed class PatientAppearanceController : MonoBehaviour
    {
        private const string EmergencyParameter = "Emergency";
        private const string FrozenPoseState = "Base Layer.Frozen Supine";
        private const float FrozenPoseNormalizedTime = 0.625f;
        private const string HaircutPrefix = "npc_haircut_";
        private const string FacialHairPrefix = "npc_hairfacial_";

        [Header("Placement")]
        [SerializeField] private Transform appearanceRoot;

        [Header("Complete Characters")]
        [SerializeField] private GameObject[] femaleCharacterPrefabs = Array.Empty<GameObject>();
        [SerializeField] private GameObject[] maleCharacterPrefabs = Array.Empty<GameObject>();

        [Header("Modular Hair")]
        [SerializeField] private GameObject[] hairstylePrefabs = Array.Empty<GameObject>();
        [SerializeField] private GameObject[] facialHairPrefabs = Array.Empty<GameObject>();
        [SerializeField, Range(0f, 1f)] private float maleFacialHairChance = 0.5f;

        [Header("Animation")]
        [SerializeField] private RuntimeAnimatorController lyingAnimatorController;

        private readonly Dictionary<string, Transform> targetBones = new Dictionary<string, Transform>(80);
        private readonly List<Renderer> activeRenderers = new List<Renderer>(64);
        private readonly List<Renderer> attachmentRenderers = new List<Renderer>(16);
        private GameObject activeCharacter;
        private GameObject activeHairstyle;
        private GameObject activeFacialHair;
        private bool configurationErrorRaised;
        private bool appearanceRootDetached;

        public PatientGender CurrentGender { get; private set; }
        public GameObject CurrentCharacterPrefab { get; private set; }
        public GameObject CurrentHairstylePrefab { get; private set; }
        public GameObject CurrentFacialHairPrefab { get; private set; }
        public Animator CurrentAnimator { get; private set; }
        public IReadOnlyList<Renderer> ActiveRenderers => activeRenderers;

        public bool RandomizeAppearance(bool emergency)
        {
            ClearAppearance();

            if (!TryValidateConfiguration())
                return false;

            PrepareAppearanceRoot();

            CurrentGender = SelectGender(UnityEngine.Random.value);
            GameObject[] characterPrefabs = CurrentGender == PatientGender.Female
                ? femaleCharacterPrefabs
                : maleCharacterPrefabs;

            CurrentCharacterPrefab = characterPrefabs[SelectIndex(UnityEngine.Random.value, characterPrefabs.Length)];
            CurrentHairstylePrefab = hairstylePrefabs[SelectIndex(UnityEngine.Random.value, hairstylePrefabs.Length)];
            CurrentFacialHairPrefab = ShouldUseFacialHair(
                    CurrentGender,
                    UnityEngine.Random.value,
                    maleFacialHairChance)
                ? facialHairPrefabs[SelectIndex(UnityEngine.Random.value, facialHairPrefabs.Length)]
                : null;

            activeCharacter = InstantiateUnderRoot(CurrentCharacterPrefab, "Patient Character");
            if (activeCharacter == null)
                return FailAppearance("Không thể tạo mô hình bệnh nhân đã chọn.");

            CurrentAnimator = activeCharacter.GetComponent<Animator>();
            if (CurrentAnimator == null || !CurrentAnimator.isHuman)
                return FailAppearance($"Mô hình '{CurrentCharacterPrefab.name}' cần một Animator humanoid hợp lệ.");

            DisableEmbeddedHair(activeCharacter);
            CacheTargetBones(activeCharacter.transform);

            activeHairstyle = AttachSkinnedPrefab(CurrentHairstylePrefab, "Patient Hairstyle");
            if (activeHairstyle == null)
                return FailAppearance($"Không thể gắn kiểu tóc '{CurrentHairstylePrefab.name}' vào bệnh nhân.");

            if (CurrentFacialHairPrefab != null)
            {
                activeFacialHair = AttachSkinnedPrefab(CurrentFacialHairPrefab, "Patient Facial Hair");
                if (activeFacialHair == null)
                    return FailAppearance($"Không thể gắn râu '{CurrentFacialHairPrefab.name}' vào bệnh nhân.");
            }

            MergeAttachmentLods();
            CollectActiveRenderers();
            if (activeRenderers.Count == 0)
                return FailAppearance("Mô hình bệnh nhân không có Renderer khả dụng.");

            CurrentAnimator.runtimeAnimatorController = lyingAnimatorController;
            CurrentAnimator.applyRootMotion = false;
            CurrentAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            CurrentAnimator.enabled = true;
            CurrentAnimator.speed = 1f;
            CurrentAnimator.Rebind();
            CurrentAnimator.SetBool(EmergencyParameter, emergency);
            int frozenPoseStateHash = Animator.StringToHash(FrozenPoseState);
            if (!CurrentAnimator.HasState(0, frozenPoseStateHash))
                return FailAppearance($"Bộ điều khiển hoạt ảnh cần trạng thái '{FrozenPoseState}'.");

            CurrentAnimator.Play(frozenPoseStateHash, 0, FrozenPoseNormalizedTime);
            CurrentAnimator.Update(0f);
            CurrentAnimator.speed = 0f;
            CurrentAnimator.enabled = false;
            configurationErrorRaised = false;
            return true;
        }

        public static PatientGender SelectGender(float sample)
        {
            return Mathf.Clamp01(sample) < 0.5f ? PatientGender.Female : PatientGender.Male;
        }

        public static int SelectIndex(float sample, int optionCount)
        {
            if (optionCount <= 0)
                return -1;

            return Mathf.Min(optionCount - 1, Mathf.FloorToInt(Mathf.Clamp01(sample) * optionCount));
        }

        public static bool ShouldUseFacialHair(PatientGender gender, float sample, float chance)
        {
            return gender == PatientGender.Male && Mathf.Clamp01(sample) < Mathf.Clamp01(chance);
        }

        private bool TryValidateConfiguration()
        {
            if (appearanceRoot == null)
                return FailConfiguration("PatientAppearanceController cần Appearance Root.");
            if (!HasOnlyAssignedEntries(femaleCharacterPrefabs) || !HasOnlyAssignedEntries(maleCharacterPrefabs))
                return FailConfiguration("Danh sách mô hình bệnh nhân nam và nữ phải có ít nhất một prefab hợp lệ.");
            if (!HasOnlyAssignedEntries(hairstylePrefabs))
                return FailConfiguration("Danh sách kiểu tóc phải có ít nhất một prefab hợp lệ.");
            if (maleFacialHairChance > 0f && !HasOnlyAssignedEntries(facialHairPrefabs))
                return FailConfiguration("Danh sách râu phải có prefab hợp lệ khi xác suất râu lớn hơn 0.");
            if (lyingAnimatorController == null)
                return FailConfiguration("PatientAppearanceController cần bộ điều khiển hoạt ảnh nằm trên giường.");

            return true;
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

        private static bool HasOnlyAssignedEntries(GameObject[] prefabs)
        {
            if (prefabs == null || prefabs.Length == 0)
                return false;

            for (int i = 0; i < prefabs.Length; i++)
            {
                if (prefabs[i] == null)
                    return false;
            }

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

        private void PrepareAppearanceRoot()
        {
            if (appearanceRootDetached || appearanceRoot == null || appearanceRoot.parent != transform)
                return;

            Transform runtimeParent = transform.parent;
            appearanceRoot.SetParent(runtimeParent, true);
            appearanceRoot.localScale = Vector3.one;
            appearanceRootDetached = true;
        }

        private GameObject AttachSkinnedPrefab(GameObject prefab, string instanceName)
        {
            GameObject attachment = InstantiateUnderRoot(prefab, instanceName);
            if (attachment == null)
                return null;

            SkinnedMeshRenderer[] renderers = attachment.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length == 0)
            {
                DestroyObject(attachment);
                return null;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                SkinnedMeshRenderer renderer = renderers[i];
                Transform[] sourceBones = renderer.bones;
                Transform[] reboundBones = new Transform[sourceBones.Length];
                for (int boneIndex = 0; boneIndex < sourceBones.Length; boneIndex++)
                {
                    Transform sourceBone = sourceBones[boneIndex];
                    if (sourceBone == null || !targetBones.TryGetValue(sourceBone.name, out Transform targetBone))
                    {
                        DestroyObject(attachment);
                        return null;
                    }

                    reboundBones[boneIndex] = targetBone;
                }

                renderer.bones = reboundBones;
                if (renderer.rootBone != null && targetBones.TryGetValue(renderer.rootBone.name, out Transform rootBone))
                    renderer.rootBone = rootBone;

                attachmentRenderers.Add(renderer);
            }

            return attachment;
        }

        private void DisableEmbeddedHair(GameObject character)
        {
            Renderer[] renderers = character.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                string rendererName = renderers[i].name;
                if (rendererName.StartsWith(HaircutPrefix, StringComparison.OrdinalIgnoreCase)
                    || rendererName.StartsWith(FacialHairPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    renderers[i].enabled = false;
                }
            }
        }

        private void CacheTargetBones(Transform characterRoot)
        {
            targetBones.Clear();
            Transform[] transforms = characterRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform target = transforms[i];
                if (!targetBones.ContainsKey(target.name))
                    targetBones.Add(target.name, target);
            }
        }

        private void MergeAttachmentLods()
        {
            LODGroup lodGroup = activeCharacter.GetComponent<LODGroup>();
            if (lodGroup == null || attachmentRenderers.Count == 0)
                return;

            LOD[] lods = lodGroup.GetLODs();
            List<Renderer>[] additions = new List<Renderer>[lods.Length];
            for (int i = 0; i < attachmentRenderers.Count; i++)
            {
                Renderer renderer = attachmentRenderers[i];
                int lodIndex = ParseLodIndex(renderer.name, lods.Length);
                if (lodIndex < 0)
                    lodIndex = 0;

                additions[lodIndex] ??= new List<Renderer>(2);
                additions[lodIndex].Add(renderer);
            }

            for (int i = 0; i < lods.Length; i++)
            {
                if (additions[i] == null)
                    continue;

                Renderer[] existing = lods[i].renderers;
                Renderer[] merged = new Renderer[existing.Length + additions[i].Count];
                Array.Copy(existing, merged, existing.Length);
                additions[i].CopyTo(merged, existing.Length);
                lods[i].renderers = merged;
            }

            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();
        }

        private static int ParseLodIndex(string rendererName, int lodCount)
        {
            int marker = rendererName.LastIndexOf("_lod", StringComparison.OrdinalIgnoreCase);
            if (marker < 0 || marker + 4 >= rendererName.Length)
                return -1;

            char value = rendererName[marker + 4];
            if (value < '0' || value > '9')
                return -1;

            int index = value - '0';
            return index < lodCount ? index : -1;
        }

        private void CollectActiveRenderers()
        {
            activeRenderers.Clear();
            Renderer[] characterRenderers = activeCharacter.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < characterRenderers.Length; i++)
            {
                Renderer renderer = characterRenderers[i];
                if (renderer.enabled)
                    activeRenderers.Add(renderer);
            }

            for (int i = 0; i < attachmentRenderers.Count; i++)
            {
                Renderer renderer = attachmentRenderers[i];
                if (renderer != null && !activeRenderers.Contains(renderer))
                    activeRenderers.Add(renderer);
            }
        }

        public void ClearAppearance()
        {
            if (appearanceRoot != null)
            {
                for (int childIndex = appearanceRoot.childCount - 1; childIndex >= 0; childIndex--)
                {
                    GameObject child = appearanceRoot.GetChild(childIndex).gameObject;
                    Renderer[] childRenderers = child.GetComponentsInChildren<Renderer>(true);
                    for (int rendererIndex = 0; rendererIndex < childRenderers.Length; rendererIndex++)
                    {
                        Renderer renderer = childRenderers[rendererIndex];
                        renderer.forceRenderingOff = true;
                        renderer.enabled = false;
                    }

                    // Destroy is deferred in Play Mode, so hide the complete hierarchy first.
                    // This prevents a cured appearance from overlapping its pooled replacement.
                    child.SetActive(false);
                    DestroyObject(child);
                }
            }

            activeCharacter = null;
            activeHairstyle = null;
            activeFacialHair = null;
            CurrentCharacterPrefab = null;
            CurrentHairstylePrefab = null;
            CurrentFacialHairPrefab = null;
            CurrentAnimator = null;
            activeRenderers.Clear();
            attachmentRenderers.Clear();
            targetBones.Clear();
        }

        private void OnDestroy()
        {
            ClearAppearance();
            if (appearanceRootDetached && appearanceRoot != null)
                DestroyObject(appearanceRoot.gameObject);
        }

        private void OnValidate()
        {
            maleFacialHairChance = Mathf.Clamp01(maleFacialHairChance);
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
