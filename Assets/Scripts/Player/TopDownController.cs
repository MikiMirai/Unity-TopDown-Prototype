using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class TopDownController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 6f;
    [SerializeField] private float acceleration = 50f;   // Ramp-up speed for MoveTowards
    [SerializeField] private float deceleration = 40f;   // Ramp-down speed for MoveTowards

    [Header("SmoothDamp Settings")]
    [SerializeField] private bool useSmoothDamp = false; // Soft easing method
    [SerializeField] private float smoothTime = 0.15f;   // Responsiveness for SmoothDamp

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

    [Header("Gravity")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundedGravity = -2f; // Small downward force to keep controller grounded
    [SerializeField] private Vector3 downwardVelocity;

    [Header("Rotation")]
    [SerializeField] private float rotateSpeedDegPerSec = 720f;

    [Header("Combat")]
    [SerializeField] private bool noLocomotionMelee = false;
    [Tooltip("How far the player lunges forward (world units)")]
    [SerializeField] private float _attackMoveBlockDuration = 0.25f;   // seconds
    [SerializeField] private float attackCooldown = 0.25f;
    [SerializeField] private float _lungeDistance = 0.35f;
    [Tooltip("How long the lunge takes (seconds)")]
    [SerializeField] private float _lungeDuration = 0.12f;
    [Tooltip("Curve for easing (optional)")]
    [SerializeField] private AnimationCurve _lungeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Attack buffering")]
    [SerializeField] private float attackInputBufferWindow = 0.15f;   // seconds
    private bool bufferedAttackPending;

    [Header("Shooting (Projectiles)")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 20f;

    [Header("References")]
    [SerializeField] private DashCooldownUI dashUI; // Cache reference
    [SerializeField] private AttackState attackState;

    [Header("Optional")]
    [SerializeField] private Transform debugAimTarget; // Visual helper

    [Header("Deabug")]
    [SerializeField] private bool isMouseMoving = true;
    [SerializeField] private Vector2 lastMousePos;

    // ---- PRIVATE REFS ----
    private CharacterController characterController;
    private Camera cam;
    private Animator animator;
    private PlayerControls controls;

    // ---- INPUT ----
    private bool calculateControls = true;
    private Vector2 moveInput;
    private Vector2 lookStick;            // Gamepad look

    // ---- PRIVATE VARS ----
    private Vector3 moveVelocity;         // Horizontal velocity
    private Vector3 smoothDampVel;        // Horizontal velocity with SmoothDamp
    private Vector3 dashDirection;        // Dash direction while control is locked
    private bool bufferedDashPending;     // Dash button press buffering
    private Coroutine _lungeRoutine;
    private bool _isAttackInitiated = false;


    // ---- TIMERS ----
    private float dashTimer = 0f;         // Dash amount in time
    private float dashCooldownTimer = 0f; // Dash Cooldown
    private float _attackInitiatedTime = -Mathf.Infinity;   // Last time Attack() was called
    private float _nextAttackTime;
    private float moveBlockTimer = 0f;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
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

        controls.Player.Attack.performed += HandleAttackInput;
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
            animator.SetFloat("Speed", characterController.velocity.magnitude / _moveSpeed);
        }

        if (attackState.isAttacking)
        {
            if (!InBlockWindow() && moveInput.sqrMagnitude > 0.01f)
            {
                CancelAttack();
            }
        }

        if (CanMove())
        {
            HandleMovement();
        }

        if (calculateControls)
        {
            HandleDashState();
            HandleAiming();
        }

        if (dashUI != null)
        {
            dashUI.UpdateDashCooldown(dashCooldownTimer, dashCooldown);
        }

        if (bufferedAttackPending && Time.time >= _nextAttackTime)
        {
            Attack();
            bufferedAttackPending = false;
        }

        if (moveBlockTimer > 0f)
            moveBlockTimer -= Time.deltaTime;
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

        Vector3 inputDir = CameraRelative(moveInput, cam);

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
        Vector3 inputDir = CameraRelative(moveInput, cam);

        // ---- DASH OVERRIDES normal velocity while active ----
        if (dashTimer > 0f)
        {
            moveVelocity = dashDirection * dashSpeed;
        }
        else // Handle normal movement velocity
        {
            // ---- Target velocity ----
            Vector3 targetVelocity = inputDir * _moveSpeed;

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
        if (characterController.isGrounded)
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
        characterController.Move(finalMove * Time.deltaTime);
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

    private void HandleAttackInput(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return; // Ignore cancellations

        bool readyNow = Time.time >= _nextAttackTime;

        if (readyNow)
        {
            Attack(); // Set the new cooldown
            return;
        }

        // Buffer only if we are in the last X seconds of cooldown
        if (!bufferedAttackPending &&
        _nextAttackTime > Time.time &&
        Time.time >= _nextAttackTime - attackInputBufferWindow)
        {
            bufferedAttackPending = true;
        }
    }

    private void Attack()
    {
        attackState.TryAttack();

        StartMeleeAttack();

        moveBlockTimer = _attackMoveBlockDuration;

        _nextAttackTime = Time.time + attackCooldown;
    }

    private void StartMeleeAttack()
    {
        // Stop any previous lunge (in case of rapid attacks)
        if (_lungeRoutine != null) StopCoroutine(_lungeRoutine);
        _lungeRoutine = StartCoroutine(LungeCoroutine());
    }

    private void CancelAttack()
    {
        if (_lungeRoutine != null)
        {
            StopCoroutine(_lungeRoutine);
            _lungeRoutine = null;
        }

        attackState.Cancel();
        OnAttackFinished();

        bufferedAttackPending = false;
    }

    private void OnAttackFinished()
    {
        _isAttackInitiated = false;

        // Keep the flag alive until block time is over
        if (moveBlockTimer <= 0f)
            _isAttackInitiated = false;
    }

    private IEnumerator LungeCoroutine()
    {
        Vector3 startPos = transform.position;
        Vector3 target = startPos + transform.forward * _lungeDistance;

        float elapsed = 0f;
        while (elapsed < _lungeDuration && !attackState.IsCancelled)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _lungeDuration);
            t = _lungeCurve.Evaluate(t);

            Vector3 desiredPos = Vector3.Lerp(startPos, target, t);
            characterController.Move(desiredPos - transform.position);

            yield return null;
        }

        // Snap to final position if we finished the curve
        if (!attackState.IsCancelled)
            characterController.Move(target - transform.position);

        _lungeRoutine = null; // Mark coroutine as finished
        OnAttackFinished();
    }
    #endregion

    #region Helpers
    private static Vector3 CameraRelative(Vector2 input, Camera cam)
    {
        var forward = cam.transform.forward;
        forward.y = 0; forward.Normalize();

        var right = cam.transform.right;
        right.y = 0; right.Normalize();

        return (forward * input.y + right * input.x).normalized;
    }

    private bool InBlockWindow()
    {
        // _isAttackInitiated becomes true when an attack starts.
        // It is reset only after the attack finishes or gets cancelled.
        return _isAttackInitiated &&
               Time.time - _attackInitiatedTime < _attackMoveBlockDuration;
    }

    bool CanMove()
    {
        if (!calculateControls) return false;

        // Block movement while the timer is running
        if (moveBlockTimer > 0f)
            return false;

        if (noLocomotionMelee)
        {
            return calculateControls && !attackState.isAttacking;
        }

        return true; // Free to move
    }

    private bool CanAttack()
    {
        if (Time.time < _nextAttackTime) return false;
        _nextAttackTime = Time.time + attackCooldown;
        return true;
    }
    #endregion
}
