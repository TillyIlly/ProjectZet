using UnityEngine;
using UnityEngine.InputSystem;

public class MovementScript : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float deceleration = 15f;
    [SerializeField] private float airAcceleration = 10f;
    [SerializeField] private float airDeceleration = 8f;
    
    [Header("Jump Settings")]
    [SerializeField] private float jumpPower = 15f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private float coyoteTime = 0.15f; // Time after leaving platform where you can still jump
    [SerializeField] private float jumpBufferTime = 0.2f; // Press jump early and it still registers
    
    [Header("Wall Jump Settings")]
    [SerializeField] private float wallSlideSpeed = 2f;
    [SerializeField] private float wallJumpPower = 12f;
    [SerializeField] private Vector2 wallJumpDirection = new Vector2(1f, 1.5f);
    [SerializeField] private float wallStickTime = 0.25f; // Time you can stick to wall
    [SerializeField] private float wallJumpControlDelay = 0.15f; // Brief loss of control after wall jump
    
    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.49f, 0.03f);
    [SerializeField] private LayerMask groundLayer;
    
    [Header("Wall Check")]
    [SerializeField] private Transform wallCheckRight;
    [SerializeField] private Transform wallCheckLeft;
    [SerializeField] private Vector2 wallCheckSize = new Vector2(0.03f, 0.5f);
    [SerializeField] private LayerMask wallLayer;
    
    // Components
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private PlayerInput playerInput;
    
    // Input actions
    private InputAction moveAction;
    private InputAction jumpAction;
    
    // State variables
    private float horizontalInput;
    private bool jumpPressed;
    private bool jumpReleased;
    private bool isGrounded;
    private bool isTouchingWallRight;
    private bool isTouchingWallLeft;
    private bool isWallSliding;
    private bool canMove = true;
    
    // Jump variables
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool isJumping;
    
    // Wall jump variables
    private float wallStickCounter;
    private float wallJumpControlTimer;
    private int lastWallDirection; // -1 for left, 1 for right
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerInput = GetComponent<PlayerInput>();
        
        // Get input actions
        if (playerInput != null)
        {
            moveAction = playerInput.actions["Move"];
            jumpAction = playerInput.actions["Jump"];
        }
    }
    
    void Start()
    {
        // Normalize wall jump direction
        wallJumpDirection.Normalize();
    }
    
    void OnEnable()
    {
        if (jumpAction != null)
        {
            jumpAction.started += OnJumpPressed;
            jumpAction.canceled += OnJumpReleased;
        }
    }
    
    void OnDisable()
    {
        if (jumpAction != null)
        {
            jumpAction.started -= OnJumpPressed;
            jumpAction.canceled -= OnJumpReleased;
        }
    }
    
    void OnJumpPressed(InputAction.CallbackContext context)
    {
        jumpPressed = true;
        jumpBufferCounter = jumpBufferTime;
    }
    
    void OnJumpReleased(InputAction.CallbackContext context)
    {
        jumpReleased = true;
    }
    
    void Update()
    {
        // Get input
        if (canMove && wallJumpControlTimer <= 0)
        {
            if (moveAction != null)
            {
                Vector2 moveInput = moveAction.ReadValue<Vector2>();
                horizontalInput = moveInput.x;
            }
        }
        else if (wallJumpControlTimer > 0)
        {
            wallJumpControlTimer -= Time.deltaTime;
            // Gradually return control
            float controlReturn = Mathf.Clamp01(1f - (wallJumpControlTimer / wallJumpControlDelay));
            if (moveAction != null)
            {
                Vector2 moveInput = moveAction.ReadValue<Vector2>();
                horizontalInput = Mathf.Lerp(horizontalInput, moveInput.x, controlReturn);
            }
        }
        
        // Check for ground and walls
        CheckGrounded();
        CheckWalls();
        
        // Handle coyote time
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
        
        // Handle jump buffer
        if (jumpBufferCounter > 0)
        {
            jumpBufferCounter -= Time.deltaTime;
        }
        
        // Handle jumping
        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0 && !isJumping)
        {
            Jump();
        }
        
        // Handle wall jumping
        HandleWallSlide();
        if (jumpBufferCounter > 0 && isWallSliding)
        {
            WallJump();
        }
        
        // Handle jump cut (variable jump height)
        if (jumpReleased && rb.linearVelocity.y > 0 && isJumping)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            coyoteTimeCounter = 0f;
            jumpReleased = false;
        }
        
        // Reset jump flags
        jumpPressed = false;
        if (jumpReleased) jumpReleased = false;
        
        // Flip sprite based on movement direction
        if (horizontalInput != 0 && spriteRenderer != null)
        {
            spriteRenderer.flipX = horizontalInput < 0;
        }
    }
    
    void FixedUpdate()
    {
        // Apply horizontal movement
        ApplyMovement();
    }
    
    void ApplyMovement()
    {
        float targetSpeed = horizontalInput * moveSpeed;
        float speedDifference = targetSpeed - rb.linearVelocity.x;
        
        // Choose acceleration based on grounded state and input
        float accelRate;
        if (isGrounded)
        {
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        }
        else
        {
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? airAcceleration : airDeceleration;
        }
        
        // Apply acceleration for snappy movement
        float movement = speedDifference * accelRate * Time.fixedDeltaTime;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x + movement, rb.linearVelocity.y);
    }
    
    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpPower);
        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;
        isJumping = true;
    }
    
    void WallJump()
    {
        // Determine wall jump direction (jump away from wall)
        int jumpDirection = isTouchingWallRight ? -1 : 1;
        
        // Apply wall jump velocity
        Vector2 jumpVelocity = new Vector2(wallJumpDirection.x * jumpDirection * wallJumpPower, 
                                           wallJumpDirection.y * wallJumpPower);
        rb.linearVelocity = jumpVelocity;
        
        // Reset states
        jumpBufferCounter = 0f;
        isJumping = true;
        isWallSliding = false;
        wallStickCounter = 0f;
        
        // Temporarily reduce control for more realistic wall jump
        wallJumpControlTimer = wallJumpControlDelay;
        horizontalInput = jumpDirection; // Force initial direction
    }
    
    void HandleWallSlide()
    {
        bool isTouchingWall = (isTouchingWallRight || isTouchingWallLeft) && !isGrounded;
        bool isMovingTowardWall = (isTouchingWallRight && horizontalInput > 0) || 
                                  (isTouchingWallLeft && horizontalInput < 0);
        
        // Start wall slide
        if (isTouchingWall && isMovingTowardWall && rb.linearVelocity.y < 0)
        {
            isWallSliding = true;
            wallStickCounter = wallStickTime;
            
            // Store wall direction
            lastWallDirection = isTouchingWallRight ? 1 : -1;
        }
        // Continue wall slide
        else if (isWallSliding)
        {
            // Wall stick timer
            if (wallStickCounter > 0)
            {
                wallStickCounter -= Time.deltaTime;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f); // Stick to wall
            }
            else if (isMovingTowardWall && isTouchingWall)
            {
                // Slide down slowly
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
            }
            else
            {
                // Stop wall sliding if not touching wall or not moving toward it
                isWallSliding = false;
            }
        }
    }
    
    void CheckGrounded()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
            
            if (isGrounded && rb.linearVelocity.y <= 0)
            {
                isJumping = false;
            }
        }
    }
    
    void CheckWalls()
    {
        if (wallCheckRight != null)
        {
            isTouchingWallRight = Physics2D.OverlapBox(wallCheckRight.position, wallCheckSize, 0f, wallLayer);
        }
        
        if (wallCheckLeft != null)
        {
            isTouchingWallLeft = Physics2D.OverlapBox(wallCheckLeft.position, wallCheckSize, 0f, wallLayer);
        }
    }
    
    // Visualize detection boxes in editor
    void OnDrawGizmosSelected()
    {
        // Ground check
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
        
        // Wall checks
        if (wallCheckRight != null)
        {
            Gizmos.color = isTouchingWallRight ? Color.green : Color.red;
            Gizmos.DrawWireCube(wallCheckRight.position, wallCheckSize);
        }
        
        if (wallCheckLeft != null)
        {
            Gizmos.color = isTouchingWallLeft ? Color.green : Color.red;
            Gizmos.DrawWireCube(wallCheckLeft.position, wallCheckSize);
        }
    }
}