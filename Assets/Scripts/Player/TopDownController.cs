using UnityEngine;
using UnityEngine.InputSystem;

public class TopDownController : MonoBehaviour
{
    // ---- MOVEMENT ----
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float acceleration = 50f;   // Ramp-up speed for MoveTowards
    public float deceleration = 40f;   // Ramp-down speed for MoveTowards

    [Header("SmoothDamp Settings")]
    public bool useSmoothDamp = false; // Soft easing method
    public float smoothTime = 0.15f;   // Responsiveness for SmoothDamp

    // ---- DASH ----
    [Header("Dash")]
    [Tooltip("Speed during dash")]
    [SerializeField] private float dashSpeed = 14f;
    [Tooltip("How long the dash lasts (seconds)")]
    [SerializeField] private float dashDuration = 0.2f;
    [Tooltip("Cooldown between dashes (seconds)")]
    [SerializeField] private float dashCooldown = 0.8f;
    [Tooltip("Consume stamina/mana? (hook for later)")]
    [SerializeField] private bool dashCostsResource = false;
    [Tooltip("Amount of resource used per dash")]
    [SerializeField] private float dashCost = 25f;
    [Tooltip("Input buffer: Press in LAST X seconds of cooldown to auto-dash when ready (0 = no buffer)")]
    [SerializeField] private float dashInputBufferWindow = 0.2f;

    // ---- GRAVITY ----
    [Header("Gravity")]
    public float gravity = -9.81f;
    public float groundedGravity = -2f; // Small downward force to keep controller grounded
    [SerializeField] private Vector3 downwardVelocity;

    [Header("Rotation")]
    public float rotateSpeedDegPerSec = 720f;

    [Header("Combat")]
    public bool noLocomotionMelee = false;
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 20f;

    [Header("References")]
    [SerializeField] private DashCooldownUI dashUI; // Cache reference
    [SerializeField] private AttackState attackState;

    private CharacterController controller;
    private Camera cam;
    private Animator animator;
    private PlayerControls controls;

    [Header("Optional")]
    public Transform debugAimTarget; // Visual helper

    // ---- INPUT ----
    private bool calculateControls = true;
    private Vector2 moveInput;
    private Vector2 lookStick;            // Gamepad look

    private Vector3 moveVelocity;         // Horizontal velocity
    private Vector3 smoothDampVel;        // Horizontal velocity with SmoothDamp
    private float dashTimer = 0f;         // Dash amount in time
    private float dashCooldownTimer = 0f; // Dash Cooldown
    private Vector3 dashDirection;        // Dash direction while control is locked
    private bool bufferedDashPending;     // Dash button press buffering

    [Header("Deabug")]
    [SerializeField] private bool isMouseMoving = true;
    [SerializeField] private Vector2 lastMousePos;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        cam = Camera.main;
        animator = GetComponent<Animator>();
        controls = new PlayerControls();
        attackState = GetComponent<AttackState>();

        EventManager.OnPlayerDeath += OnGameOverEvent;

        // Bind input
        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        controls.Player.Look.performed += ctx => lookStick = ctx.ReadValue<Vector2>();
        controls.Player.Look.canceled += ctx => lookStick = Vector2.zero;

        controls.Player.Attack.performed += ctx => Attack();
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
        if (animator != null)
        {
            animator.SetFloat("Speed", controller.velocity.magnitude / moveSpeed);
        }

        if (CanMove())
        {
            HandleMovement();
            HandleDashState();
        }

        if (calculateControls)
        {
            HandleAiming();
        }

        if (dashUI != null)
        {
            dashUI.UpdateDashCooldown(dashCooldownTimer, dashCooldown);
        }
    }

    bool CanMove()
    {
        if (noLocomotionMelee)
        {
            if (calculateControls && !attackState.isAttacking)
            {
                return true;
            }

            return false;
        }
        else if (calculateControls)
        {
            return true;
        }

        return false;
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
    private void StartDash()
    {
        Vector3 camForward = cam.transform.forward;
        camForward.y = 0;
        camForward.Normalize();

        Vector3 camRight = cam.transform.right;
        camRight.y = 0;
        camRight.Normalize();

        Vector3 inputDir = camForward * moveInput.y + camRight * moveInput.x;

        dashDirection = (inputDir.sqrMagnitude > 0.01f) ? inputDir.normalized : camForward;

        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;

        if (dashCostsResource)
        {
            // TODO: Spend mana/stamina
        }
    }
    private void HandleDashState()
    {
        // ---- COOLDOWN ----
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        // ----- END DASH -----
        if (dashTimer > 0f)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f) dashTimer = 0f;
        }

        // ---- START DASH ----
        // Consume buffer if ready
        if (bufferedDashPending && CanDash())
        {
            StartDash();
            bufferedDashPending = false;
        }
    }

    private bool CanDash()
    {
        return dashTimer <= 0f && dashCooldownTimer <= 0f;
    }

    public void OnDash(InputAction.CallbackContext ctx)
    {
        Debug.Log("Dash performed!");

        if (ctx.performed)
        {
            // IMMEDIATE DASH if possible
            if (CanDash())
            {
                StartDash();
            }
            // BUFFER ONLY in last X seconds of COOLDOWN (NOT during active dash)
            else if (dashTimer <= 0f && dashCooldownTimer > 0f && dashCooldownTimer <= dashInputBufferWindow)
            {
                bufferedDashPending = true;
            }
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
        if (dashTimer > 0f)
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
        // Always read mouse position
        Vector2 screenPos = Mouse.current.position.ReadValue();

        // Determine if mouse is moving
        if (screenPos == lastMousePos)
        {
            isMouseMoving = false;
        }
        else isMouseMoving = true;

        // Always set last position
        lastMousePos = screenPos;

        // Fallback: gamepad stick
        if (lookStick.sqrMagnitude > 0.01f || !isMouseMoving)
        {
            isMouseMoving = false;

            Vector3 dir = new Vector3(lookStick.x, 0f, lookStick.y);
            AimTowards(transform.position + dir);
            return;
        }

        // Prefer mouse aiming if mouse exists
        if (Mouse.current != null && cam != null)
        {
            Ray ray = cam.ScreenPointToRay(screenPos);

            Plane plane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                AimTowards(hitPoint);
                return;
            }
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

    #region Melee Attack
    // -------- Attacking --------
    private void Attack()
    {
        attackState.TryAttack();
    }

    #endregion
}
