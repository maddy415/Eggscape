using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Handles player movement, attacks, knockback and state transitions.
/// The behaviour matches the original implementation but is organized in
/// clear, documented steps to make future adjustments safer.
/// </summary>
public class Player : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [FormerlySerializedAs("bc")]
    [SerializeField] private BoxCollider2D bodyCollider;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Transform feetPos;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private BoxCollider2D attackHB;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float groundDistance = 0.25f;
    [SerializeField] private float impForce = 4f;
    [SerializeField] private float jumpTime = 0.5f;
    [SerializeField] private float defaultGS = 1f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpBufferTime = 0.2f;

    [Header("Attack Settings")]
    [SerializeField] private float attackAirTime;
    [SerializeField] private float attackCD;
    [SerializeField] private float attackForce;

    [Header("Knockback")]
    [SerializeField] private float kbForce;

    [Header("Effects")]
    [SerializeField] private GameObject explosion;

    [Header("State Flags")]
    [SerializeField] private bool canMove = true;

    // Internal state values -------------------------------------------------
    private bool playerDead;
    private bool isGrounded;
    private bool isJumping;
    private bool isFalling;
    private bool isAttacking;
    private bool canAttack = true;
    private bool isKnockbacking;

    // Timers ---------------------------------------------------------------
    private float jumpTimer;
    private float jumpBufferCounter;
    private float attackTimer;
    private float attackCDTimer;

    /// <summary>
    /// Exposes movement availability to other systems (GameManager, Tutorial, etc.).
    /// </summary>
    public bool CanMove
    {
        get => canMove;
        set => canMove = value;
    }

    private void Start()
    {
        CacheComponents();
        rb.gravityScale = defaultGS;
    }

    private void CacheComponents()
    {
        if (!rb)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (!bodyCollider)
        {
            bodyCollider = GetComponent<BoxCollider2D>();
        }

        if (!sprite)
        {
            sprite = GetComponentInChildren<SpriteRenderer>();
        }

        if (!attackHB)
        {
            foreach (BoxCollider2D collider in GetComponentsInChildren<BoxCollider2D>())
            {
                if (collider != bodyCollider)
                {
                    attackHB = collider;
                    break;
                }
            }
        }
    }

    private void Update()
    {
        HandleOngoingAttackMovement();

        if (CanMove)
        {
            HandleHorizontalMovement();
            HandleJump();
            HandleAttackInput();
        }

        HandleVictorySequence();
    }

    #region Jump Logic

    /// <summary>
    /// Centralizes the jump sequence: buffer, start, sustain and release.
    /// </summary>
    private void HandleJump()
    {
        RefreshGroundedStatus();

        bool jumpPressed = Input.GetButtonDown("Jump");
        bool jumpHeld = Input.GetButton("Jump");
        bool jumpReleased = Input.GetButtonUp("Jump");

        UpdateJumpBuffer(jumpPressed);
        TryConsumeJumpBuffer();
        ApplySustainedJump(jumpHeld);
        HandleJumpRelease(jumpReleased);
    }

    private void RefreshGroundedStatus()
    {
        if (!feetPos) return;

        isGrounded = Physics2D.OverlapCircle(feetPos.position, groundDistance, groundLayer);
    }

    /// <summary>
    /// Stores the jump input for a short time so the player can buffer a jump.
    /// </summary>
    private void UpdateJumpBuffer(bool jumpPressed)
    {
        if (jumpPressed)
        {
            jumpBufferCounter = jumpBufferTime;

            if (isGrounded)
            {
                AudioManager.audioInstance.JumpSFX();
            }
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }

    /// <summary>
    /// Starts the jump when grounded while a buffered input is still active.
    /// </summary>
    private void TryConsumeJumpBuffer()
    {
        if (!isGrounded || jumpBufferCounter <= 0f)
        {
            return;
        }

        BeginJump();
    }

    private void BeginJump()
    {
        isJumping = true;
        jumpTimer = 0f;
        jumpBufferCounter = 0f;
        rb.linearVelocity = Vector2.up * jumpForce;
    }

    /// <summary>
    /// Extends the jump height while the button is held and the timer allows it.
    /// </summary>
    private void ApplySustainedJump(bool jumpHeld)
    {
        if (!isJumping || !jumpHeld)
        {
            return;
        }

        if (jumpTimer < jumpTime)
        {
            rb.linearVelocity = Vector2.up * jumpForce;
            jumpTimer += Time.deltaTime;
        }
        else
        {
            EndJumpHold();
        }
    }

    private void EndJumpHold()
    {
        isJumping = false;
        jumpTimer = 0f;
    }

    private void HandleJumpRelease(bool jumpReleased)
    {
        if (!jumpReleased)
        {
            return;
        }

        isJumping = false;
    }

    #endregion

    #region Horizontal Movement

    private void HandleHorizontalMovement()
    {
        if (playerDead)
        {
            return;
        }

        float moveInput = Input.GetAxisRaw("Horizontal");
        MoveHorizontally(moveInput);
        UpdateSpriteFacing(moveInput);
        HandleFastFall();
    }

    private void MoveHorizontally(float moveInput)
    {
        Vector3 displacement = new Vector3(moveInput, 0f, 0f) * moveSpeed * Time.deltaTime;
        transform.position += displacement;
    }

    private void UpdateSpriteFacing(float moveInput)
    {
        if (moveInput > 0f)
        {
            sprite.flipX = false;
        }
        else if (moveInput < 0f)
        {
            sprite.flipX = true;
        }
    }

    private void HandleFastFall()
    {
        if (Input.GetKeyDown(KeyCode.S) && !isGrounded)
        {
            rb.linearVelocity = new Vector2(0f, -20f);
            isFalling = true;
        }
        else if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            isFalling = false;
        }
    }

    #endregion

    #region Attack Logic

    private void HandleAttackInput()
    {
        if (Input.GetMouseButtonDown(0) && canAttack)
        {
            BeginAttack();
        }

        UpdateAttackCooldown();
        UpdateAirAttackTimer();
    }

    private void BeginAttack()
    {
        canAttack = false;
        attackHB.enabled = true;

        attackTimer = 0f;
        isAttacking = true;
        rb.gravityScale = 0f;
    }

    private void HandleOngoingAttackMovement()
    {
        if (!isAttacking)
        {
            return;
        }

        if (!isKnockbacking)
        {
            rb.linearVelocity = new Vector2(attackForce, 0f);

            if (!isGrounded && Input.GetKeyDown(KeyCode.S))
            {
                CancelAttack();
            }
        }
    }

    private void UpdateAttackCooldown()
    {
        if (canAttack)
        {
            return;
        }

        attackCDTimer += Time.deltaTime;

        if (attackCDTimer >= attackCD)
        {
            canAttack = true;
            attackCDTimer = 0f;
            isKnockbacking = false;
        }
    }

    private void UpdateAirAttackTimer()
    {
        if (!isAttacking)
        {
            return;
        }

        attackTimer += Time.deltaTime;

        if (attackTimer >= attackAirTime)
        {
            CancelAttack();
        }
    }

    private void CancelAttack()
    {
        Debug.Log("passou o tempo");
        rb.gravityScale = defaultGS;
        attackTimer = 0f;
        isAttacking = false;
        jumpTimer = 0f;

        if (!isKnockbacking)
        {
            rb.linearVelocity = Vector3.zero;
        }

        attackHB.enabled = false;
    }

    #endregion

    #region Game State Management

    private void HandleVictorySequence()
    {
        if (!isGrounded || !GameManager.Instance.victoryAchieved)
        {
            return;
        }

        CanMove = false;
        transform.position += Vector3.right * Time.deltaTime * 15f;
        GameManager.Instance.victoryText.text = "Passou de fase! Aperte \"Espaço\" para continuar";
    }

    private void Death()
    {
        if (playerDead)
        {
            return;
        }

        GameObject explosionInstance = Instantiate(explosion, transform.position, Quaternion.identity);
        AudioManager.audioInstance.ExplodeSFX();
        Destroy(explosionInstance, 1.1f);

        playerDead = true;
        CanMove = false;

        rb.linearVelocity = new Vector2(-10f, 25f) * impForce * Time.deltaTime;
        bodyCollider.enabled = false;
        rb.freezeRotation = false;
        rb.AddTorque(40f);
    }

    #endregion

    #region Collision Handling

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (!other.gameObject.CompareTag("Obstacle") || GameManager.Instance.isCheatOn)
        {
            return;
        }

        GameManager.Instance.playerAlive = false;
        Debug.Log("morreu burro");
        GameManager.Instance.StopScene();
        Death();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.gameObject.CompareTag("JumpDetector"))
        {
            return;
        }

        GameManager.Instance.score++;
    }

    #endregion

    #region Knockback

    public void Knockback()
    {
        rb.linearVelocity = new Vector3(-kbForce, rb.linearVelocity.y, 0f);
        Debug.Log("knockback");
        isKnockbacking = true;
    }

    #endregion
}
