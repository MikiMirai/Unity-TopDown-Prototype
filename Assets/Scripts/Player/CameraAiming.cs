using UnityEngine;
using UnityEngine.InputSystem;

public class CameraAiming : MonoBehaviour
{
    [Header("Rotation")]
    public float rotateSpeedDegPerSec = 720f;

    [Header("Optional")]
    public Transform debugAimTarget; // visual helper (e.g., a small sphere)

    private Camera cam;
    private PlayerControls controls;

    private void Awake()
    {
        // Get camera (or drag in via Inspector if you prefer)
        cam = Camera.main;
        controls = new PlayerControls();
    }

    private void OnEnable() => controls.Player.Enable();
    private void OnDisable() => controls.Player.Disable();

    private void Update()
    {
        // 1) Mouse aiming: absolute pointer position -> ray -> plane at player height
        if (Mouse.current != null && cam != null)
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();
            Ray ray = cam.ScreenPointToRay(screenPos);

            // Horizontal plane at player's current Y
            Plane plane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                AimTowards(hitPoint);
                return; // prefer mouse when present
            }
        }

        // 2) Gamepad fallback: right stick gives a direction, not a point
        Vector2 stick = controls.Player.Look.ReadValue<Vector2>();
        if (stick.sqrMagnitude > 0.01f)
        {
            Vector3 dir = new Vector3(stick.x, 0f, stick.y);
            AimTowards(transform.position + dir);
        }
    }

    private void AimTowards(Vector3 worldPoint)
    {
        Vector3 flatDir = worldPoint - transform.position;
        flatDir.y = 0f;
        if (flatDir.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(flatDir);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, target, rotateSpeedDegPerSec * Time.deltaTime);

        if (debugAimTarget) debugAimTarget.position = worldPoint;
    }
}
