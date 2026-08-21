using System;
using UnityEngine;

namespace NCKH.VR.NPC
{
    [CreateAssetMenu(fileName = "NPC Humanoid Pose", menuName = "NCKH VR/NPC/Humanoid Pose")]
    public sealed class NpcHumanoidPose : ScriptableObject
    {
        [SerializeField] private Vector3 bodyPosition;
        [SerializeField] private Quaternion bodyRotation = Quaternion.identity;
        [SerializeField] private float[] muscles = Array.Empty<float>();

        public Vector3 BodyPosition => bodyPosition;
        public Quaternion BodyRotation => bodyRotation;
        public int MuscleCount => muscles != null ? muscles.Length : 0;
        public bool IsComplete => MuscleCount == HumanTrait.MuscleCount;

        public void CopyTo(ref HumanPose destination)
        {
            destination.bodyPosition = bodyPosition;
            destination.bodyRotation = IsUsableRotation(bodyRotation) ? bodyRotation : Quaternion.identity;
            if (destination.muscles == null || destination.muscles.Length != HumanTrait.MuscleCount)
                destination.muscles = new float[HumanTrait.MuscleCount];

            Array.Clear(destination.muscles, 0, destination.muscles.Length);
            if (muscles != null)
                Array.Copy(muscles, destination.muscles, Mathf.Min(muscles.Length, destination.muscles.Length));
        }

        public void Capture(HumanPose source, Animator sourceAnimator)
        {
            float humanScale = sourceAnimator != null ? Mathf.Max(0.0001f, sourceAnimator.humanScale) : 1f;
            Transform sourceTransform = sourceAnimator != null ? sourceAnimator.transform : null;
            Quaternion sourceRotation = sourceTransform != null ? sourceTransform.rotation : Quaternion.identity;
            Vector3 sourcePosition = sourceTransform != null ? sourceTransform.position : Vector3.zero;
            Vector3 worldBodyPosition = source.bodyPosition * humanScale;
            bodyPosition = Quaternion.Inverse(sourceRotation)
                * (worldBodyPosition - sourcePosition) / humanScale;
            bodyRotation = Quaternion.Inverse(sourceRotation)
                * (IsUsableRotation(source.bodyRotation) ? source.bodyRotation : Quaternion.identity);
            muscles = new float[HumanTrait.MuscleCount];
            if (source.muscles != null)
                Array.Copy(source.muscles, muscles, Mathf.Min(source.muscles.Length, muscles.Length));
        }

        public void Configure(Vector3 position, Quaternion rotation, float[] muscleValues)
        {
            bodyPosition = position;
            bodyRotation = IsUsableRotation(rotation) ? rotation : Quaternion.identity;
            muscles = new float[HumanTrait.MuscleCount];
            if (muscleValues != null)
                Array.Copy(muscleValues, muscles, Mathf.Min(muscleValues.Length, muscles.Length));
        }

        private static bool IsUsableRotation(Quaternion rotation)
        {
            return rotation.x * rotation.x + rotation.y * rotation.y
                + rotation.z * rotation.z + rotation.w * rotation.w > 0.0001f;
        }
    }
}
