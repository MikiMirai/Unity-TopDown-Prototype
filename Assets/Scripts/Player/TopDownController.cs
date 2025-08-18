using UnityEngine;
using UnityEngine.InputSystem;

public class TopDownController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Rotation")]
    public float rotateSpeedDegPerSec = 720f;

    [Header("Shooting")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 20f;

    private CharacterController controller;
    private Camera cam;
    private PlayerControls controls;

    [Header("Optional")]
    public Transform debugAimTarget; // visual helper (e.g., a small sphere)

    private Vector2 moveInput;
    private Vector2 lookStick; // gamepad look

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        cam = Camera.main;
        controls = new PlayerControls();

        // Bind input
        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        controls.Player.Look.performed += ctx => lookStick = ctx.ReadValue<Vector2>();
        controls.Player.Look.canceled += ctx => lookStick = Vector2.zero;

        controls.Player.Attack.performed += ctx => Shoot();
    }

    private void OnEnable() => controls.Player.Enable();
    private void OnDisable() => controls.Player.Disable();

    private void Update()
    {
        HandleMovement();
        HandleAiming();
    }

    // -------- Movement --------
    private void HandleMovement()
    {
        if (moveInput.sqrMagnitude < 0.01f) return;

        // Camera-relative movement (XZ only)
        Vector3 camForward = cam.transform.forward;
        camForward.y = 0;
        camForward.Normalize();

        Vector3 camRight = cam.transform.right;
        camRight.y = 0;
        camRight.Normalize();

        Vector3 moveDir = camForward * moveInput.y + camRight * moveInput.x;
        controller.Move(moveDir * moveSpeed * Time.deltaTime);
    }

    // -------- Aiming --------
    private void HandleAiming()
    {
        // Prefer mouse aiming if mouse exists
        if (Mouse.current != null && cam != null)
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();
            Ray ray = cam.ScreenPointToRay(screenPos);

            Plane plane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                AimTowards(hitPoint);
                return;
            }
        }

        // Fallback: gamepad stick
        if (lookStick.sqrMagnitude > 0.01f)
        {
            Vector3 dir = new Vector3(lookStick.x, 0f, lookStick.y);
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

    // -------- Shooting --------
    private void Shoot()
    {
        if (!projectilePrefab || !firePoint) return;

        GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb)
            rb.linearVelocity = firePoint.forward * projectileSpeed;
    }
}
