using UnityEngine;

[ExecuteAlways]
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public float smoothTime = 0.3f;

    [Header("Offsets (auto-calculated if you move camera in Scene view)")]
    public Vector3 positionOffset = new(0, 10, -10);

    private Vector3 velocity = Vector3.zero;

    private void LateUpdate()
    {
        if (!target) return;

        Vector3 targetPosition = target.position + positionOffset;

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }
}
