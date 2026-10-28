using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace NCKH.VR.NPC
{
    public enum NpcMotionMode
    {
        None,
        StaticHumanoidPose,
        AnimationClip
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(NpcAppearanceRandomizer))]
    public sealed class NpcMotionPlayer : MonoBehaviour
    {
        [Header("Motion")]
        [SerializeField] private NpcMotionMode mode;
        [SerializeField] private NpcHumanoidPose staticPose;
        [SerializeField] private AnimationClip animationClip;
        [SerializeField, Min(0f)] private float playbackSpeed = 1f;
        [SerializeField] private bool loopAnimation = true;

        [Header("Model Placement")]
        [SerializeField] private Vector3 modelLocalPosition;
        [SerializeField] private Vector3 modelLocalEulerAngles;

        [Header("Head Tracking")]
        [SerializeField] private bool trackHead;
        [SerializeField] private Transform lookTarget;
        [SerializeField, Range(0f, 90f)] private float maximumYaw = 50f;
        [SerializeField, Range(0f, 60f)] private float maximumPitch = 25f;
        [SerializeField, Min(0f)] private float headTurnSpeed = 120f;

        private NpcAppearanceRandomizer appearanceRandomizer;
        private GameObject boundModel;
        private Animator boundAnimator;
        private Transform headBone;
        private HumanPoseHandler poseHandler;
        private HumanPose humanPose;
        private PlayableGraph playableGraph;
        private AnimationClipPlayable clipPlayable;
        private Vector3 currentLookDirection;
        private bool warningRaised;

        public NpcMotionMode Mode => mode;
        public NpcHumanoidPose StaticPose => staticPose;
        public AnimationClip AnimationClip => animationClip;
        public Transform LookTarget => lookTarget;
        public bool TrackHead => trackHead;
        public GameObject BoundModel => boundModel;

        private void Awake()
        {
            appearanceRandomizer = GetComponent<NpcAppearanceRandomizer>();
        }

        private void OnEnable()
        {
            if (appearanceRandomizer == null)
                appearanceRandomizer = GetComponent<NpcAppearanceRandomizer>();

            appearanceRandomizer.AppearanceCreated += HandleAppearanceCreated;
            TryBindCurrentAppearance();
        }

        private void Start()
        {
            TryBindCurrentAppearance();
        }

        private void OnDisable()
        {
            if (appearanceRandomizer != null)
                appearanceRandomizer.AppearanceCreated -= HandleAppearanceCreated;
            ReleaseMotionResources();
        }

        private void OnDestroy()
        {
            ReleaseMotionResources();
        }

        private void LateUpdate()
        {
            if (boundModel == null || boundAnimator == null)
                return;

            if (mode == NpcMotionMode.StaticHumanoidPose && poseHandler != null)
                poseHandler.SetHumanPose(ref humanPose);

            UpdateLoopingClip();
            UpdateHeadTracking();
        }

        private void HandleAppearanceCreated(GameObject model)
        {
            Bind(model);
        }

        private void TryBindCurrentAppearance()
        {
            GameObject current = appearanceRandomizer != null
                ? appearanceRandomizer.CurrentModelInstance
                : null;
            if (current != null && current != boundModel)
                Bind(current);
        }

        private void Bind(GameObject model)
        {
            ReleaseMotionResources();
            if (model == null || mode == NpcMotionMode.None)
                return;

            boundModel = model;
            Transform modelTransform = model.transform;
            modelTransform.localPosition = modelLocalPosition;
            modelTransform.localRotation = Quaternion.Euler(modelLocalEulerAngles);

            boundAnimator = model.GetComponentInChildren<Animator>(true);
            if (boundAnimator == null || boundAnimator.avatar == null || !boundAnimator.avatar.isHuman)
            {
                WarnOnce("NpcMotionPlayer cần một Animator Humanoid hợp lệ trên mô hình NPC.");
                ReleaseMotionResources();
                return;
            }

            boundAnimator.applyRootMotion = false;
            boundAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            headBone = boundAnimator.GetBoneTransform(HumanBodyBones.Head);
            currentLookDirection = modelTransform.forward;

            if (mode == NpcMotionMode.StaticHumanoidPose)
                BindStaticPose();
            else if (mode == NpcMotionMode.AnimationClip)
                BindAnimationClip();
        }

        private void BindStaticPose()
        {
            if (staticPose == null || !staticPose.IsComplete)
            {
                WarnOnce("NpcMotionPlayer cần một Humanoid Pose có đủ dữ liệu cơ bắp.");
                return;
            }

            poseHandler = new HumanPoseHandler(boundAnimator.avatar, boundAnimator.transform);
            humanPose = new HumanPose { muscles = new float[HumanTrait.MuscleCount] };
            staticPose.CopyTo(ref humanPose);
            poseHandler.SetHumanPose(ref humanPose);
        }

        private void BindAnimationClip()
        {
            if (animationClip == null)
            {
                WarnOnce("NpcMotionPlayer đang ở chế độ Animation Clip nhưng chưa được gán clip.");
                return;
            }

            playableGraph = PlayableGraph.Create($"NPC Motion - {name}");
            playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            clipPlayable = AnimationClipPlayable.Create(playableGraph, animationClip);
            clipPlayable.SetApplyFootIK(false);
            clipPlayable.SetApplyPlayableIK(false);
            clipPlayable.SetSpeed(playbackSpeed);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(
                playableGraph, "NPC Animation", boundAnimator);
            output.SetSourcePlayable(clipPlayable);
            playableGraph.Play();
        }

        private void UpdateLoopingClip()
        {
            if (mode != NpcMotionMode.AnimationClip || !loopAnimation
                || !playableGraph.IsValid() || animationClip == null || animationClip.length <= 0f)
                return;

            double time = clipPlayable.GetTime();
            if (time >= animationClip.length)
                clipPlayable.SetTime(time % animationClip.length);
        }

        private void UpdateHeadTracking()
        {
            if (!trackHead || headBone == null || boundModel == null)
                return;

            Transform modelTransform = boundModel.transform;
            Vector3 desiredDirection = modelTransform.forward;
            if (lookTarget != null)
            {
                Vector3 toTarget = lookTarget.position - headBone.position;
                if (toTarget.sqrMagnitude > 0.0001f)
                    desiredDirection = ClampLookDirection(modelTransform, toTarget.normalized);
            }

            if (currentLookDirection.sqrMagnitude < 0.0001f)
                currentLookDirection = modelTransform.forward;
            currentLookDirection = Vector3.RotateTowards(
                currentLookDirection.normalized,
                desiredDirection,
                headTurnSpeed * Mathf.Deg2Rad * Time.deltaTime,
                0f);

            Vector3 baseForward = headBone.forward;
            if (baseForward.sqrMagnitude > 0.0001f && currentLookDirection.sqrMagnitude > 0.0001f)
                headBone.rotation = Quaternion.FromToRotation(baseForward, currentLookDirection) * headBone.rotation;
        }

        private Vector3 ClampLookDirection(Transform modelTransform, Vector3 worldDirection)
        {
            Vector3 localDirection = modelTransform.InverseTransformDirection(worldDirection);
            float yaw = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
            float horizontal = Mathf.Sqrt(localDirection.x * localDirection.x
                + localDirection.z * localDirection.z);
            float pitch = Mathf.Atan2(localDirection.y, horizontal) * Mathf.Rad2Deg;
            yaw = Mathf.Clamp(yaw, -maximumYaw, maximumYaw);
            pitch = Mathf.Clamp(pitch, -maximumPitch, maximumPitch);
            return modelTransform.TransformDirection(
                Quaternion.Euler(-pitch, yaw, 0f) * Vector3.forward).normalized;
        }

        private void ReleaseMotionResources()
        {
            if (playableGraph.IsValid())
                playableGraph.Destroy();
            if (poseHandler != null)
            {
                poseHandler.Dispose();
                poseHandler = null;
            }

            boundModel = null;
            boundAnimator = null;
            headBone = null;
            clipPlayable = default;
            humanPose = default;
        }

        private void WarnOnce(string message)
        {
            if (warningRaised)
                return;
            warningRaised = true;
            Debug.LogWarning(message, this);
        }
    }
}
