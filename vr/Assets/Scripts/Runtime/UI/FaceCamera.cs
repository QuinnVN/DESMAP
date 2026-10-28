using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    [SerializeField] private Transform head;

    private void LateUpdate()
    {
        if (head == null) return;

        Vector3 flatForward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        if (flatForward.sqrMagnitude < 0.0001f) return;

        transform.LookAt(transform.position + flatForward, Vector3.up);
    }
}
