using UnityEngine;
using UnityEngine.InputSystem;

public class TopDownController : MonoBehaviour
{
    // ---- MOVEMENT ----
    [Header("Movement")]
    public float moveSpeed = 10f;
    public float acceleration = 20f;   // Ramp-up speed for MoveTowards
    public float deceleration = 10f;   // Ramp-down speed for MoveTowards

    [Header("SmoothDamp Settings")]
    public bool useSmoothDamp = false; // Toggle in Inspector
    public float smoothTime = 0.15f;   // Responsiveness for SmoothDamp

    // ---- DASH ----
    [Header("Dash")]
    [Tooltip("Speed during dash")]
    [SerializeField] private float dashSpeed = 20f;
    [Tooltip("How long the dash lasts (seconds)")]
    [SerializeField] private float dashDuration = 0.25f;
    [Tooltip("Cooldown between dashes (seconds)")]
    [SerializeField] private float dashCooldown = 0.8f;
    [Tooltip("Consume stamina/mana? (hook for later)")]
    [SerializeField] private bool dashCostsResource = false;
    [Tooltip("Amount of resource used per dash")]
    [SerializeField] private float dashCost = 25f;

    // ---- GRAVITY ----
    [Header("Gravity")]
    public float gravity = -9.81f;
    public float groundedGravity = -2f; // Small downward force to keep controller grounded
    [SerializeField] private Vector3 downwardVelocity;

    [Header("Rotation")]
    public float rotateSpeedDegPerSec = 720f;

    [Header("Shooting")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 20f;

    [Header("References")]
    [SerializeField] private DashCooldownUI dashUI; // Cache reference

    private CharacterController controller;
    private Camera cam;
    private PlayerControls controls;

    [Header("Optional")]
    public Transform debugAimTarget; // Visual helper

    // ---- INPUT ----
    private bool calculateControls = true;
    private Vector2 moveInput;
    private Vector2 lookStick;            // Gamepad look
    private bool dashPressed;

    private Vector3 moveVelocity;         // Current horizontal velocity
    private Vector3 smoothDampVel;        // Ref velocity for SmoothDamp
    private float dashTimer = 0f;         // counts down while dashing
    private float dashCooldownTimer = 0f; // prevents spam
    private Vector3 dashDirection;        // locked direction for the dash

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        cam = Camera.main;
        controls = new PlayerControls();

        EventManager.OnPlayerDeath += OnGameOverEvent;

        // Bind input
        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        controls.Player.Look.performed += ctx => lookStick = ctx.ReadValue<Vector2>();
        controls.Player.Look.canceled += ctx => lookStick = Vector2.zero;

        controls.Player.Attack.performed += ctx => Shoot();
        controls.Player.Dash.performed += ctx => OnDash(ctx);
        controls.Player.DebugCollider.performed += ctx => TriggerDebugCollider();
    }

    private void OnDestroy()
    {
        EventManager.OnPlayerDeath -= OnGameOverEvent;
    }

    private void OnEnable() => controls.Player.Enable();
    private void OnDisable() => controls.Player.Disable();

    private void Update()
    {
        if (calculateControls)
        {
            HandleMovement();
            HandleDashState();
            HandleAiming();
        }

        if (dashUI != null)
        {
            dashUI.UpdateDashCooldown(dashCooldownTimer, dashCooldown);
        }
    }

    public void OnDash(InputAction.CallbackContext ctx)
    {
        Debug.Log("Dash performed!");

        if (ctx.performed) dashPressed = true;
    }

    void OnGameOverEvent()
    {
        calculateControls = false;
    }

    void TriggerDebugCollider()
    {
        GameData.Instance.showDebugColliders = !GameData.Instance.showDebugColliders;
    }

    #region Dash
    private void HandleDashState()
    {
        // ----- COOLDOWN -----
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        // ----- START DASH -----
        if (dashPressed && dashTimer <= 0f && dashCooldownTimer <= 0f)
        {
            Vector3 camForward = cam.transform.forward;
            camForward.y = 0;
            camForward.Normalize();

            Vector3 camRight = cam.transform.right;
            camRight.y = 0;
            camRight.Normalize();

            Vector3 inputDir = camForward * moveInput.y + camRight * moveInput.x;

            dashDirection = (inputDir.sqrMagnitude > 0.01f)
                            ? inputDir.normalized
                            : camForward;

            dashTimer = dashDuration;      // <-- dash length
            dashCooldownTimer = dashCooldown;      // <-- cooldown

            if (dashCostsResource) { /* spend */ }
            dashPressed = false;
        }

        // ----- END DASH -----
        if (dashTimer > 0f)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f) dashTimer = 0f;
        }
    }
    #endregion

    #region Movement
    // -------- Movement --------
    private void HandleMovement()
    {
        // ---- Camera-relative movement ----
        Vector3 camForward = cam.transform.forward;
        camForward.y = 0;
        camForward.Normalize();

        Vector3 camRight = cam.transform.right;
        camRight.y = 0;
        camRight.Normalize();

        Vector3 inputDir = camForward * moveInput.y + camRight * moveInput.x;
        inputDir.Normalize();

        // ---- DASH OVERRIDES normal velocity while active ----
        if (dashTimer > 0f)                     // <-- ONLY WHILE DASHING
        {
            moveVelocity = dashDirection * dashSpeed;
        }
        else
        {
            // ---- Target velocity ----
            Vector3 targetVelocity = inputDir * moveSpeed;

            if (useSmoothDamp)
            {
                // SmoothDamp version (soft easing, floatier feel)
                moveVelocity = Vector3.SmoothDamp(
                    moveVelocity, targetVelocity, ref smoothDampVel, smoothTime);
            }
            else
            {
                // MoveTowards version (more snappy & responsive)
                if (inputDir.sqrMagnitude > 0.01f)
                {
                    moveVelocity = Vector3.MoveTowards(
                        moveVelocity, targetVelocity, acceleration * Time.deltaTime);
                }
                else
                {
                    moveVelocity = Vector3.MoveTowards(
                        moveVelocity, Vector3.zero, deceleration * Time.deltaTime);
                }
            }
        }

        // ---- Gravity ----
        if (controller.isGrounded)
        {
            // Reset Y downwardVelocity when grounded
            downwardVelocity.y = groundedGravity;
        }
        else
        {
            // Apply gravity with cap
            downwardVelocity.y += gravity * Time.deltaTime;
            if (downwardVelocity.y < gravity) // Don’t go faster than gravity itself
                downwardVelocity.y = gravity;
        }

        // ---- Apply Movement ----
        Vector3 finalMove = moveVelocity + new Vector3(0, downwardVelocity.y, 0);
        controller.Move(finalMove * Time.deltaTime);
    }
#endregion

#region Aiming
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
#endregion

#region Shooting
    // -------- Shooting --------
    private void Shoot()
    {
        if (!projectilePrefab || !firePoint) return;

        GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb)
            rb.linearVelocity = firePoint.forward * projectileSpeed;
    }
#endregion
}
