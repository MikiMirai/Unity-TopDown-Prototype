using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ColliderDebugger : MonoBehaviour
{
    public enum ColliderType { Sphere, Box, Capsule }

    [Header("Debug Settings")]
    [SerializeField] private Color colliderColor = Color.green;
    [SerializeField] private ColliderType colliderType = ColliderType.Sphere;

    // Example sizes – replace with your collider’s values
    [Header("Box")]
    public Vector3 boxSize = new(1, 2, 1);

    [Header("Sphere")]
    public float sphereRadius = 0.5f;
    public int sphereSegments = 12;

    [Header("Capsule")]
    public float capsuleHeight = 2f;
    public float capsuleRadius = 0.3f;
    public int capsuleSegments = 12;

    void OnRenderObject()
    {
        if (!GameData.Instance.showDebugColliders) return;

        switch (colliderType)
        {
            case ColliderType.Sphere:
                RenderSphere();

                //RuntimeColliderDrawer.DrawSphere(
                //    transform.position + Vector3.up * 1f,
                //    sphereRadius,
                //    colliderColor);
                break;

            case ColliderType.Box:
                RenderBox();

                //RuntimeColliderDrawer.DrawBox(transform.position, boxSize, colliderColor);
                break;

            case ColliderType.Capsule:
                RenderCapsule();
                
                //RuntimeColliderDrawer.DrawCapsuleFull(
                //    transform.position,
                //    transform.up,   // capsule axis
                //    capsuleRadius,
                //    capsuleHeight,
                //    colliderColor,
                //    12);                 // segments (higher = smoother)
                break;

            default:
                break;
        }
    }

    void RenderSphere()
    {
        RuntimeColliderDrawer.GetSphereVertices(
            center: transform.position,
            rotation: transform.rotation,
            radius: sphereRadius,
            segments: sphereSegments,
            out Vector3[] verts,
            out (int, int)[] edges);

        RuntimeColliderDrawer.RenderLines(verts, edges, colliderColor);
    }

    void RenderBox()
    {
        RuntimeColliderDrawer.GetBoxVertices(
            center: transform.position,
            rotation: transform.rotation,
            halfExtents: boxSize,
            out Vector3[] verts,
            out (int, int)[] edges);

        RuntimeColliderDrawer.RenderLines(verts, edges, colliderColor);
    }

    void RenderCapsule()
    {
        RuntimeColliderDrawer.GetCapsuleVertices(
            center: transform.position,
            rotation: transform.rotation,
            radius: capsuleRadius,
            height: capsuleHeight,
            segments: capsuleSegments,
            out Vector3[] verts,
            out (int, int)[] edges);

        RuntimeColliderDrawer.RenderLines(verts, edges, colliderColor);
    }
}
