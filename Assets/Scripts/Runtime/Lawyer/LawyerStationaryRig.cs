using UnityEngine;

namespace NCKH.VR.Lawyer
{
    [DisallowMultipleComponent]
    public sealed class LawyerStationaryRig : MonoBehaviour
    {
        [Tooltip("Move, gravity, jump, and teleport roots. Turning must not be included.")]
        [SerializeField] private GameObject[] disabledLocomotionRoots = System.Array.Empty<GameObject>();

        private void Awake() => EnforceStationaryState();
        private void OnEnable() => EnforceStationaryState();

        public void EnforceStationaryState()
        {
            for (int index = 0; index < disabledLocomotionRoots.Length; index++)
                if (disabledLocomotionRoots[index] != null)
                    disabledLocomotionRoots[index].SetActive(false);
        }

#if UNITY_EDITOR
        public void ConfigureInEditor(GameObject[] roots)
        {
            disabledLocomotionRoots = roots ?? System.Array.Empty<GameObject>();
            EnforceStationaryState();
        }
#endif
    }
}
