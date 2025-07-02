using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class UniversalPlayerController_2 : MonoBehaviour
{
    [Header("Movement Settings")]
    public float maxSpeed = 10f;
    public float acceleration = 50f;
    public float groundDeceleration = 30f;
    public float airDeceleration = 20f;
    [Range(0f, 1f)] public float airControl = 0.5f; // Added air control multiplier

    [Header("Jump Settings")]
    public float jumpPower = 20f;
    public float maxFallSpeed = 25f;
    public float fallGravity = 60f;
    public float jumpEndEarlyMultiplier = 2.5f;
    public float coyoteTime = 0.2f;
    public float jumpBufferTime = 0.2f;
    public float jumpGravityScale = 0.5f; // Separate variable for jump gravity

    [Header("Ground Detection")]
    public float groundCheckOffset = 0.1f;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Optional Features")]
    public bool enableCrouch = true;
    public bool enableWallJump = true;
    public bool enableDash = true;
    public bool enableDoubleJump = false; // Added double jump option

    [Header("Double Jump Settings")]
    [SerializeField] private int maxJumps = 2;
    [SerializeField] private float doubleJumpMultiplier = 0.8f;

    [Header("Crouch Settings")]
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [SerializeField] private Collider2D crouchDisableCollider;
    [SerializeField] private Transform ceilingCheck;
    [SerializeField] private float ceilingCheckRadius = 0.2f;

    [Header("Wall Settings")]
    [SerializeField] private float wallSlideSpeed = 2f;
    [SerializeField] private float wallJumpForce = 15f;
    [SerializeField] private Vector2 wallJumpDirection = new Vector2(1, 1);
    [SerializeField] private float wallCheckDistance = 0.5f;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallJumpInputLockTime = 0.2f; // Added input lock after wall jump

    [Header("Ledge Grab Settings")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private Transform ledgeCheck;
    [SerializeField] private float ledgeHangOffsetY = 0.5f;
    [SerializeField] private float ledgeHangOffsetX = 0.5f;
    [SerializeField] private float ledgeDetectionRadius = 0.1f;
    [SerializeField] private float ledgeHangTime = 3f; // Auto-drop after time

    [Header("Dash Settings")]
    [SerializeField] private float dashForce = 20f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private bool dashResetsInAir = true; // Reset dash when touching wall/ground

    [Header("Audio & Visual")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private ParticleSystem jumpParticles;
    [SerializeField] private ParticleSystem landParticles;
    [SerializeField] private ParticleSystem dashParticles; // Added dash particles
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landSound;
    [SerializeField] private AudioClip dashSound; // Added dash sound

    [Header("Events")]
    public UnityEvent OnJump;
    public UnityEvent OnLand;
    public UnityEvent OnDash;
    public UnityEvent OnWallJump;
    public UnityEvent OnLedgeGrab;

    // Core components
    private Rigidbody2D rb;
    private Collider2D col;
    
    // Input states (captured in Update)
    private Vector2 inputVector;
    private bool jumpPressed;
    private bool jumpHeld;
    private bool dashPressed;
    private bool climbPressed;
    private bool dropPressed;
    
    // Movement states
    private Vector2 velocity;
    private bool grounded;
    private bool wasGrounded;
    private int jumpCount; // Changed from jumpConsumed to jump count
    private bool earlyJumpCut;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private float lastDashTime;
    private bool crouching;
    private bool facingRight = true;
    private bool isDashing;
    private bool dashAvailable = true;

    // Wall & Ledge states
    private bool isTouchingWall;
    private bool isWallSliding;
    private bool isLedgeGrabbing;
    private Vector2 ledgePosition;
    private float ledgeGrabStartTime;
    private float wallJumpInputLockTimer;

    // Performance optimization
    private Vector2 groundCheckPos;
    private Vector2 wallDirection;
    private static readonly WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();

    #region Unity Lifecycle
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        
        // Validate required components
        ValidateComponents();
        
        // Cache initial gravity scale
        rb.gravityScale = 1f;
    }

    void Start()
    {
        // Initialize dash availability
        dashAvailable = true;
    }

    void Update()
    {
        CaptureInput();
        UpdateTimers();
        HandleAnimator();
        
        // Handle ledge grab input in Update for responsiveness
        if (isLedgeGrabbing)
        {
            HandleLedgeGrabInput();
        }
    }

    void FixedUpdate()
    {
        CheckGroundState();
        CheckWallState();
        
        if (!isDashing && !isLedgeGrabbing)
        {
            HandleMovement();
            HandleJump();
        }
        
        if (!isLedgeGrabbing)
        {
            HandleWallSlide();
            CheckLedgeGrab();
        }
        else
        {
            HandleLedgeGrab();
        }
        
        ApplyVelocity();
    }
    
    #endregion

    #region Component Validation
    
    void ValidateComponents()
    {
        if (enableCrouch && ceilingCheck == null)
        {
            Debug.LogWarning($"[{name}] Crouch enabled but no ceiling check assigned!");
        }
        
        if (enableWallJump && (wallCheck == null || ledgeCheck == null))
        {
            Debug.LogWarning($"[{name}] Wall jump enabled but wall/ledge checks not assigned!");
        }
        
        if (enableDash && dashParticles == null)
        {
            Debug.LogWarning($"[{name}] Dash enabled but no dash particles assigned!");
        }
    }
    
    #endregion

    #region Input Handling
    
    void CaptureInput()
    {
        inputVector = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        jumpPressed = Input.GetButtonDown("Jump");
        jumpHeld = Input.GetButton("Jump");
        
        if (enableDash)
            dashPressed = Input.GetKeyDown(KeyCode.LeftShift);
            
        climbPressed = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
        dropPressed = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
        
        // Only handle flip if not input locked
        if (wallJumpInputLockTimer <= 0f)
        {
            HandleFlip(inputVector.x);
        }
        
        if (dashPressed && enableDash)
        {
            TryDash();
        }
    }
    
    #endregion

    #region Timers
    
    void UpdateTimers()
    {
        // Jump buffer
        if (jumpPressed)
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter = Mathf.Max(0, jumpBufferCounter - Time.deltaTime);

        // Coyote time
        if (grounded)
            coyoteTimeCounter = coyoteTime;
        else
            coyoteTimeCounter = Mathf.Max(0, coyoteTimeCounter - Time.deltaTime);
            
        // Wall jump input lock timer
        if (wallJumpInputLockTimer > 0f)
            wallJumpInputLockTimer -= Time.deltaTime;
            
        // Ledge grab auto-drop timer
        if (isLedgeGrabbing && Time.time - ledgeGrabStartTime > ledgeHangTime)
        {
            EndLedgeGrab();
        }
    }

    #endregion

    #region Ground & Wall Detection

    void CheckGroundState()
    {
        wasGrounded = grounded;
        groundCheckPos = (Vector2)transform.position + Vector2.down * groundCheckOffset;
        grounded = Physics2D.OverlapCircle(groundCheckPos, groundCheckRadius, groundLayer);

        // Landing detection
        if (!wasGrounded && grounded)
        {
            OnLanded();
        }

        // Reset abilities when grounded
        if (grounded)
        {
            jumpCount = 0;
            earlyJumpCut = false;
            if (dashResetsInAir)
                dashAvailable = true;
        }
        if (grounded)
{
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1f, groundLayer);
            if (hit)
            {
                float desiredY = hit.point.y + col.bounds.extents.y;
                transform.position = new Vector3(transform.position.x, desiredY, transform.position.z);
            }
}
    }
    
    void CheckWallState()
    {
        if (!enableWallJump) return;
        
        wallDirection = facingRight ? Vector2.right : Vector2.left;
        isTouchingWall = Physics2D.Raycast(transform.position, wallDirection, wallCheckDistance, wallLayer);
        
        // Reset dash when touching wall
        if (isTouchingWall && dashResetsInAir)
            dashAvailable = true;
    }
    
    #endregion

    #region Movement
    
    void HandleMovement()
    {
        float inputX = inputVector.x;
        
        // Apply input lock after wall jump
        if (wallJumpInputLockTimer > 0f)
            inputX = 0f;
            
        float targetSpeed = inputX * maxSpeed;
        
        // Handle crouching
        HandleCrouch(ref targetSpeed);
        
        // Apply acceleration/deceleration based on ground state
        float accel = grounded ? acceleration : acceleration * airControl;
        float decel = grounded ? groundDeceleration : airDeceleration;
        
        if (Mathf.Approximately(inputX, 0))
        {
            velocity.x = Mathf.MoveTowards(rb.velocity.x, 0, decel * Time.fixedDeltaTime);
        }
        else
        {
            velocity.x = Mathf.MoveTowards(rb.velocity.x, targetSpeed, accel * Time.fixedDeltaTime);
        }
        
        // Handle gravity and falling
        HandleGravity();
    }
    
    void HandleCrouch(ref float targetSpeed)
    {
        if (!enableCrouch) return;
        
        bool wantsToCrouch = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        
        if (wantsToCrouch && grounded)
        {
            if (!crouching)
            {
                crouching = true;
                if (crouchDisableCollider != null)
                    crouchDisableCollider.enabled = false;
            }
            targetSpeed *= crouchSpeedMultiplier;
        }
        else if (crouching)
        {
            // Check if we can stand up
            bool canStandUp = ceilingCheck == null || 
                            !Physics2D.OverlapCircle(ceilingCheck.position, ceilingCheckRadius, groundLayer);
            
            if (canStandUp)
            {
                crouching = false;
                if (crouchDisableCollider != null)
                    crouchDisableCollider.enabled = true;
            }
        }
    }
    
    void HandleGravity()
    {
        float targetFallSpeed = -maxFallSpeed;
        
        // Reduced gravity during jump hold
        bool jumpGravityReduction = jumpHeld && rb.velocity.y > 0 && !earlyJumpCut;
        float gravity = jumpGravityReduction ? fallGravity * jumpGravityScale : fallGravity;
        
        velocity.y = Mathf.MoveTowards(rb.velocity.y, targetFallSpeed, gravity * Time.fixedDeltaTime);
    }
    
    #endregion

    #region Jumping
    
    void HandleJump()
    {
        int maxJumpCount = enableDoubleJump ? maxJumps : 1;
        
        // Normal jump or double jump
        if (jumpBufferCounter > 0f && (coyoteTimeCounter > 0f || jumpCount < maxJumpCount))
        {
            float jumpForce = jumpCount == 0 ? jumpPower : jumpPower * doubleJumpMultiplier;
            PerformJump(jumpForce);
            jumpCount++;
        }
        
        // Wall jump
        if (enableWallJump && jumpBufferCounter > 0f && isTouchingWall && !grounded)
        {
            PerformWallJump();
        }
        
        // Early jump cut
        if (!jumpHeld && rb.velocity.y > 0 && !earlyJumpCut)
        {
            earlyJumpCut = true;
            velocity.y *= (1f / jumpEndEarlyMultiplier);
        }
    }
    
    void PerformJump(float jumpForce)
    {
        velocity.y = jumpForce;
        earlyJumpCut = false;
        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;
        
        // Effects
        PlayJumpEffects();
        OnJump?.Invoke();
    }
    
    void PerformWallJump()
    {
        Vector2 wallJumpVel = new Vector2(
            wallJumpDirection.x * wallJumpForce * (facingRight ? -1 : 1),
            wallJumpDirection.y * wallJumpForce
        );
        
        velocity = wallJumpVel;
        jumpCount = maxJumps; // Consume all jumps
        jumpBufferCounter = 0f;
        wallJumpInputLockTimer = wallJumpInputLockTime;
        
        PlayJumpEffects();
        OnJump?.Invoke();
        OnWallJump?.Invoke();
    }
    
    #endregion

    #region Wall Mechanics
    
    void HandleWallSlide()
    {
        if (!enableWallJump) return;
        
        bool shouldWallSlide = !grounded && isTouchingWall && rb.velocity.y < 0 && 
                              !Mathf.Approximately(inputVector.x, 0) && wallJumpInputLockTimer <= 0f;
        
        if (shouldWallSlide)
        {
            isWallSliding = true;
            velocity.y = Mathf.Max(velocity.y, -wallSlideSpeed);
        }
        else
        {
            isWallSliding = false;
        }
    }
    
    void CheckLedgeGrab()
    {
        if (!enableWallJump || isLedgeGrabbing || grounded) return;
        if (wallCheck == null || ledgeCheck == null) return;
        
        bool wallHit = Physics2D.OverlapCircle(wallCheck.position, ledgeDetectionRadius, wallLayer);
        bool ledgeFree = !Physics2D.OverlapCircle(ledgeCheck.position, ledgeDetectionRadius, wallLayer);
        
        if (wallHit && ledgeFree && rb.velocity.y <= 0)
        {
            StartLedgeGrab();
        }
    }
    
    void StartLedgeGrab()
    {
        isLedgeGrabbing = true;
        ledgeGrabStartTime = Time.time;
        rb.gravityScale = 0f;
        
        // Calculate ledge position more smoothly
        ledgePosition = new Vector2(
            transform.position.x + (facingRight ? ledgeHangOffsetX : -ledgeHangOffsetX),
            transform.position.y + ledgeHangOffsetY
        );
        
        OnLedgeGrab?.Invoke();
    }
    
    void HandleLedgeGrab()
    {
        rb.velocity = Vector2.zero;
        transform.position = Vector2.Lerp(transform.position, ledgePosition, Time.fixedDeltaTime * 10f);
    }
    
    void HandleLedgeGrabInput()
    {
        if (!isLedgeGrabbing) return;
        
        if (climbPressed)
        {
            // Climb up
            EndLedgeGrab();
            PerformJump(jumpPower * 0.75f);
        }
        else if (dropPressed || inputVector.x != 0 && Mathf.Sign(inputVector.x) != (facingRight ? 1 : -1))
        {
            // Drop down or move away from wall
            EndLedgeGrab();
        }
    }
    
    void EndLedgeGrab()
    {
        isLedgeGrabbing = false;
        rb.gravityScale = 1f;
    }
    
    #endregion

    #region Dash
    
    void TryDash()
    {
        if (!dashAvailable || Time.time < lastDashTime + dashCooldown) return;
        
        lastDashTime = Time.time;
        isDashing = true;
        dashAvailable = false;
        
        Vector2 dashDirection = inputVector.normalized;
        if (dashDirection == Vector2.zero)
        {
            dashDirection = Vector2.right * (facingRight ? 1 : -1);
        }
        
        velocity = dashDirection * dashForce;
        
        // Effects
        PlayDashEffects();
        OnDash?.Invoke();
        
        // Use coroutine for better control
        StartCoroutine(DashCoroutine());
    }
    
    System.Collections.IEnumerator DashCoroutine()
    {
        float dashTimer = 0f;
        while (dashTimer < dashDuration)
        {
            dashTimer += Time.fixedDeltaTime;
            yield return waitForFixedUpdate;
        }
        
        isDashing = false;
    }
    
    #endregion

    #region Physics Application
    
    void ApplyVelocity()
    {
        if (!isDashing && !isLedgeGrabbing)
        {
            rb.velocity = velocity;
        }
    }
    
    #endregion

    #region Animation & Visual
    
    void HandleAnimator()
    {
        if (animator == null) return;
        
        animator.SetBool("Grounded", grounded);
        animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
        animator.SetFloat("InputSpeed", Mathf.Abs(inputVector.x));
        animator.SetBool("Crouch", crouching);
        animator.SetBool("WallSliding", isWallSliding);
        animator.SetBool("LedgeHang", isLedgeGrabbing);
        animator.SetFloat("VerticalSpeed", rb.velocity.y);
        animator.SetBool("Dashing", isDashing);
        
        if (isDashing)
            animator.SetTrigger("Dash");
    }
    
    void HandleFlip(float moveX)
    {
        if (isLedgeGrabbing || Mathf.Approximately(moveX, 0)) return;
        
        if (moveX > 0 && !facingRight)
            Flip();
        else if (moveX < 0 && facingRight)
            Flip();
    }
    
    void Flip()
    {
        facingRight = !facingRight;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !facingRight;
        }
        else
        {
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
    }
    
    #endregion

    #region Effects
    
    void PlayJumpEffects()
    {
        if (jumpParticles != null)
            jumpParticles.Play();
            
        if (audioSource != null && jumpSound != null)
            audioSource.PlayOneShot(jumpSound);
    }
    
    void PlayDashEffects()
    {
        if (dashParticles != null)
            dashParticles.Play();
            
        if (audioSource != null && dashSound != null)
            audioSource.PlayOneShot(dashSound);
    }
    
    void OnLanded()
    {
        if (landParticles != null)
            landParticles.Play();
            
        if (audioSource != null && landSound != null)
            audioSource.PlayOneShot(landSound);
            
        OnLand?.Invoke();
    }
    
    #endregion

    #region Public Methods
    
    public void SetMaxSpeed(float newMaxSpeed)
    {
        maxSpeed = newMaxSpeed;
    }
    
    public void SetJumpPower(float newJumpPower)
    {
        jumpPower = newJumpPower;
    }
    
    public bool IsGrounded => grounded;
    public bool IsDashing => isDashing;
    public bool IsWallSliding => isWallSliding;
    public bool IsLedgeGrabbing => isLedgeGrabbing;
    public Vector2 Velocity => rb.velocity;
    
    #endregion

    #region Debug
    
    void OnDrawGizmosSelected()
    {
        // Ground check
        Gizmos.color = grounded ? Color.green : Color.red;
        Vector2 groundPos = (Vector2)transform.position + Vector2.down * groundCheckOffset;
        Gizmos.DrawWireSphere(groundPos, groundCheckRadius);
        
        // Wall check
        if (enableWallJump)
        {
            Gizmos.color = isTouchingWall ? Color.blue : Color.cyan;
            Vector2 wallDir = facingRight ? Vector2.right : Vector2.left;
            Gizmos.DrawRay(transform.position, wallDir * wallCheckDistance);
            
            // Ledge checks
            if (wallCheck != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(wallCheck.position, ledgeDetectionRadius);
            }
            
            if (ledgeCheck != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(ledgeCheck.position, ledgeDetectionRadius);
            }
        }
        
        // Ceiling check
        if (enableCrouch && ceilingCheck != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(ceilingCheck.position, ceilingCheckRadius);
        }
    }
    
    #endregion
}