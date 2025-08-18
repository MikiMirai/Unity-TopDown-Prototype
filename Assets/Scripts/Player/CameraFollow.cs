using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Offsets (auto-calculated if you move camera in Scene view)")]
    public Vector3 positionOffset = new Vector3(0, 10, -10);
    public Vector3 rotationOffset = new Vector3(45, 0, 0);

    [Tooltip("If enabled, moving/rotating the camera in Scene View will update offsets.")]
    public bool updateOffsetsInEditor = true;

    private void LateUpdate()
    {
        if (!target) return;

        // In play mode: follow strictly
        if (Application.isPlaying)
        {
            transform.position = target.position + positionOffset;
            transform.rotation = Quaternion.Euler(rotationOffset);
        }
#if UNITY_EDITOR
        else if (updateOffsetsInEditor && target != null)
        {
            // In editor, if designer moves the camera, capture new offsets
            positionOffset = transform.position - target.position;
            rotationOffset = transform.rotation.eulerAngles;
        }
#endif
    }

    private void OnValidate()
    {
        if (target && Application.isPlaying)
        {
            transform.position = target.position + positionOffset;
            transform.rotation = Quaternion.Euler(rotationOffset);
        }
    }
}
