using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Collider))]
public class ColliderDebugger : MonoBehaviour
{
    public enum ColliderType { Sphere, Box, Capsule }

    [Header("Debug Settings")]
    [SerializeField] private Color colliderColor = Color.green;
    [SerializeField] private ColliderType selectedCollider = ColliderType.Sphere;

    // Example sizes – replace with your collider’s values
    public Vector3 boxSize = new(1, 2, 1);
    public float sphereRadius = 0.5f;
    public float capsuleHeight = 2f;
    public float capsuleRadius = 0.3f;

    void OnRenderObject()
    {
        if (!GameData.Instance.showDebugColliders) return;

        switch (selectedCollider)
        {
            case ColliderType.Sphere:
                RuntimeColliderDrawer.DrawSphere(
                    transform.position + Vector3.up * 1f,
                    sphereRadius, 
                    colliderColor);
                break;

            case ColliderType.Box:
                RuntimeColliderDrawer.DrawBox(transform.position, boxSize, colliderColor);
                break;

            case ColliderType.Capsule:
                RuntimeColliderDrawer.DrawCapsuleFull(
                    transform.position,
                    transform.up,   // capsule axis
                    capsuleRadius,
                    capsuleHeight,
                    colliderColor,
                    12);                 // segments (higher = smoother)
                break;

            default:
                break;
        }
    }
}
