using UnityEngine;

/// <summary>
/// Centralized player controller responsible for locomotion, combat actions,
/// and runtime state toggles consumed by higher-level managers.
/// </summary>

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class Player : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private BoxCollider2D bc;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Transform feetPos;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private BoxCollider2D attackHB;
    [SerializeField] private GameObject explosion;

#if UNITY_EDITOR
    [Header("Debug Utilities")]
    [Tooltip("When enabled, logs state transitions for the attack hitbox to the console.")]
    [SerializeField] private bool logAttackHitboxState;
    [Tooltip("Runtime indicator that reflects whether the attack hitbox collider is currently enabled.")]
    [SerializeField] private bool debugAttackHitboxEnabled;
#endif

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float groundDistance = 0.25f;
    [SerializeField] private float defaultGS = 3f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float jumpTime = 0.5f;
    [SerializeField] private float jumpBufferTime = 0.2f;

    [Header("Attack Settings")]
    [SerializeField] private float attackForce = 12f;
    [SerializeField] private float attackAirTime = 0.3f;
    [SerializeField] private float attackCD = 0.5f;

    [Header("Knockback Settings")]
    [SerializeField] private float kbForce = 6f;

    [Header("Death Settings")]
    [SerializeField] private float impForce = 4f;
    [SerializeField] private float deathTorque = 40f;

    private bool isGrounded;
    private bool isJumping;
    private bool isFalling;
    private bool isAttacking;
    private bool isDead;
    private bool canAttack = true;
    private bool isKnockbacking;

    private float jumpTimer;
    private float jumpBufferCounter;
    private float attackTimer;
    private float attackCooldownTimer;

    private PlayerInputState inputState;
    private bool canMove = true;

    public bool CanMove => canMove;
    public bool IsKnockbacking => isKnockbacking;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        bc = GetComponent<BoxCollider2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        if (feetPos == null)
        {
            feetPos = transform;
        }
    }

    private void Awake()
    {
        CacheComponentReferences();
    }

    private void Start()
    {
        if (rb != null)
        {
            rb.gravityScale = defaultGS;
        }

        SetAttackHitboxActive(false);
    }

    private void Update()
    {
        if (isDead)
        {
            return;
        }

        UpdateGroundState();
        HandleVictorySequence();
        CacheInputs();

        if (canMove)
        {
            HandleHorizontalMovement();
            HandleFastFall();
            HandleJump();
            HandleAttack();
        }

        HandleAttackMotion();
        UpdateAttackState();
    }

    private void CacheComponentReferences()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (bc == null)
        {
            bc = GetComponent<BoxCollider2D>();
        }

        if (sprite == null)
        {
            sprite = GetComponentInChildren<SpriteRenderer>();
        }

        if (feetPos == null)
        {
            feetPos = transform;
        }

        if (attackHB == null)
        {
            foreach (var collider in GetComponentsInChildren<BoxCollider2D>())
            {
                if (collider != bc)
                {
                    attackHB = collider;
                    break;
                }
            }
        }

        if (attackHB == null)
        {
            Debug.LogWarning("Player attack hitbox reference is missing. Assign it in the inspector.", this);
        }
    }

    private void CacheInputs()
    {
        inputState.Horizontal = canMove ? Input.GetAxisRaw("Horizontal") : 0f;
        inputState.JumpPressed = Input.GetButtonDown("Jump");
        inputState.JumpHeld = Input.GetButton("Jump");
        inputState.JumpReleased = Input.GetButtonUp("Jump");
        inputState.AttackPressed = Input.GetMouseButtonDown(0);
        inputState.FastFallPressed = Input.GetKeyDown(KeyCode.S);
    }

    private void UpdateGroundState()
    {
        if (feetPos == null)
        {
            return;
        }

        isGrounded = Physics2D.OverlapCircle(feetPos.position, groundDistance, groundLayer);
        if (isGrounded)
        {
            isFalling = false;
        }
    }

    private void HandleVictorySequence()
    {
        if (!isGrounded)
        {
            return;
        }

        var manager = GameManager.Instance;
        if (manager == null || !manager.victoryAchieved)
        {
            return;
        }

        SetMovementEnabled(false);
        transform.position += Vector3.right * Time.deltaTime * 15f;
        if (manager.victoryText != null)
        {
            manager.victoryText.text = "Passou de fase! Aperte \"Espaço\" para continuar";
        }
    }

    private void HandleHorizontalMovement()
    {
        if (Mathf.Approximately(inputState.Horizontal, 0f))
        {
            return;
        }

        RecoverFromKnockback(inputState.Horizontal);

        Vector3 displacement = new Vector3(inputState.Horizontal, 0f, 0f) * moveSpeed * Time.deltaTime;
        transform.position += displacement;

        if (sprite != null)
        {
            sprite.flipX = inputState.Horizontal < 0f;
        }
    }

    private void RecoverFromKnockback(float inputDirection)
    {
        if (!isKnockbacking || rb == null || Mathf.Approximately(inputDirection, 0f))
        {
            return;
        }

        float velocityX = rb.velocity.x;
        if (Mathf.Approximately(velocityX, 0f) || Mathf.Sign(inputDirection) != Mathf.Sign(velocityX))
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            isKnockbacking = false;
        }
    }

    private void HandleFastFall()
    {
        if (rb == null)
        {
            return;
        }

        if (inputState.FastFallPressed && !isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, -20f);
            isFalling = true;
        }
        else if (isGrounded && isFalling)
        {
            rb.velocity = new Vector2(rb.velocity.x, 0f);
            isFalling = false;
        }
    }

    private void HandleJump()
    {
        if (inputState.JumpPressed)
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        if (isGrounded && jumpBufferCounter > 0f)
        {
            StartJump();
        }

        if (isGrounded && inputState.JumpPressed)
        {
            AudioManager.audioInstance?.JumpSFX();
        }

        if (isJumping && inputState.JumpHeld)
        {
            if (jumpTimer < jumpTime)
            {
                if (rb != null)
                {
                    rb.velocity = new Vector2(rb.velocity.x, jumpForce);
                }

                jumpTimer += Time.deltaTime;
            }
            else
            {
                isJumping = false;
                jumpTimer = 0f;
            }
        }

        if (inputState.JumpReleased)
        {
            isJumping = false;
        }
    }

    private void StartJump()
    {
        if (rb == null)
        {
            return;
        }

        isJumping = true;
        jumpTimer = 0f;
        jumpBufferCounter = 0f;
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }

    private void HandleAttack()
    {
        if (!inputState.AttackPressed || !canAttack)
        {
            return;
        }

        BeginAttack();
    }

    private void BeginAttack()
    {
        canAttack = false;
        isAttacking = true;
        isKnockbacking = false;
        attackTimer = 0f;
        attackCooldownTimer = 0f;

        if (rb != null)
        {
            rb.gravityScale = 0f;
        }

        SetAttackHitboxActive(true);
    }

    private void HandleAttackMotion()
    {
        if (rb == null)
        {
            return;
        }

        if (isAttacking && !isKnockbacking)
        {
            rb.velocity = new Vector2(attackForce, 0f);

            if (!isGrounded && inputState.FastFallPressed)
            {
                CancelAttack();
            }
        }
    }

    private void UpdateAttackState()
    {
        float deltaTime = Time.deltaTime;

        if (isAttacking)
        {
            attackTimer += deltaTime;
            if (attackTimer >= attackAirTime)
            {
                CancelAttack();
            }
        }

        if (!canAttack)
        {
            attackCooldownTimer += deltaTime;
            if (attackCooldownTimer >= attackCD)
            {
                canAttack = true;
                attackCooldownTimer = 0f;

                if (isKnockbacking && rb != null)
                {
                    rb.velocity = new Vector2(0f, rb.velocity.y);
                }

                isKnockbacking = false;
            }
        }
    }

    private void CancelAttack()
    {
        if (rb != null)
        {
            rb.gravityScale = defaultGS;
            if (!isKnockbacking)
            {
                rb.velocity = Vector2.zero;
            }
        }

        isAttacking = false;
        attackTimer = 0f;
        jumpTimer = 0f;

        SetAttackHitboxActive(false);
    }

    public void SetMovementEnabled(bool enabled)
    {
        if (isDead)
        {
            canMove = false;
            return;
        }

        canMove = enabled;

        if (!canMove && rb != null)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }
    }

    public void Knockback()
    {
        if (rb == null)
        {
            return;
        }

        isKnockbacking = true;
        rb.velocity = new Vector2(-kbForce, rb.velocity.y);
        canAttack = false;
        attackCooldownTimer = 0f;
        CancelAttack();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Obstacle"))
        {
            return;
        }

        var manager = GameManager.Instance;
        if (manager != null && manager.isCheatOn)
        {
            return;
        }

        if (manager != null)
        {
            manager.playerAlive = false;
            manager.StopScene();
        }

        Die();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.gameObject.CompareTag("JumpDetector"))
        {
            return;
        }

        var manager = GameManager.Instance;
        if (manager != null)
        {
            manager.score++;
        }
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        SetMovementEnabled(false);
        CancelAttack();
        canAttack = false;

        GameObject explosionInstance = null;
        if (explosion != null)
        {
            explosionInstance = Instantiate(explosion, transform.position, Quaternion.identity);
            AudioManager.audioInstance?.ExplodeSFX();
        }

        if (explosionInstance != null)
        {
            Destroy(explosionInstance, 1.1f);
        }

        if (bc != null)
        {
            bc.enabled = false;
        }

        if (rb != null)
        {
            rb.freezeRotation = false;
            rb.velocity = new Vector2(-10f, 25f) * impForce * Time.deltaTime;
            rb.AddTorque(deathTorque, ForceMode2D.Impulse);
        }
    }

    private void LateUpdate()
    {
#if UNITY_EDITOR
        debugAttackHitboxEnabled = attackHB != null && attackHB.enabled;
#endif
    }

    private void SetAttackHitboxActive(bool enabled)
    {
        if (attackHB == null)
        {
            return;
        }

        if (attackHB.enabled == enabled)
        {
#if UNITY_EDITOR
            debugAttackHitboxEnabled = attackHB.enabled;
#endif
            return;
        }

        attackHB.enabled = enabled;

#if UNITY_EDITOR
        debugAttackHitboxEnabled = enabled;
        if (logAttackHitboxState)
        {
            Debug.Log($"[Player] Attack hitbox {(enabled ? "ENABLED" : "DISABLED")} at t={Time.time:F2}", this);
        }
#endif
    }

    private struct PlayerInputState
    {
        public float Horizontal;
        public bool JumpPressed;
        public bool JumpHeld;
        public bool JumpReleased;
        public bool AttackPressed;
        public bool FastFallPressed;
    }
}
