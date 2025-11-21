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
    [SerializeField] private MobileInputBridge mobileInput;
    public GameObject obstacleMove;
    public ObstacleMove obsMove;

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
    public bool IsAttackActive => attackHB != null && attackHB.enabled;

    [Header("Attack Damage")]
    [SerializeField] public float attackDamage = 20f;   // dano que o boss vai tomar

    [Header("Knockback")]
    [SerializeField] private float kbForce;
    
    [Header("Squash & Stretch")]
    [SerializeField] private bool enableSquashStretch = true;

    [SerializeField] private float jumpStretchX = 0.85f;
    [SerializeField] private float jumpStretchY = 1.15f;

    [SerializeField] private float landSquashX = 1.2f;
    [SerializeField] private float landSquashY = 0.8f;

    [SerializeField] private float stretchReturnSpeed = 10f;

    // controle interno
    private bool squashStretchActive = false;
    private Vector3 defaultScale;
    private bool wasGroundedLastFrame = false;


    [Header("Effects")]
    [SerializeField] private GameObject explosion;

    [Header("State Flags")]
    [SerializeField] private bool canMove = true;
    [HideInInspector] public bool canJump = true;
    [HideInInspector] public bool canAttack = true;  // PÚBLICO - controlado pela cutscene

    [Header("Misc.")]
    [SerializeField] private float torqueForce;

    // [NEW] Giro durante o ataque
    [Header("Attack Spin")]
    [SerializeField] private bool attackSpinEnabled = true;
    [SerializeField, Tooltip("Velocidade do giro durante o ataque (graus/seg).")]
    private float attackSpinSpeed = 1080f;
    [SerializeField, Tooltip("Se true, gira apenas o sprite (recomendado).")]
    private bool spinSpriteOnlyDuringAttack = true;

    private bool attackSpinActive;
    private float attackSpinAngle;
    private Transform attackSpinTarget;

    // [NEW] Controle de giro do SPRITE ao morrer (sem mexer no corpo)
    [Header("Death Spin (Sprite Only)")]
    [SerializeField, Tooltip("Velocidade de rotação do SPRITE após morrer (graus/seg).")]
    private float deathSpinSpeed = 720f;
    private bool spinOnDeathActive = false;
    private float currentSpriteRotation = 0f;

    // Internal state values -------------------------------------------------
    private bool playerDead;
    private bool isGrounded;
    private bool isJumping;
    private bool isFalling;
    private bool isAttacking;
    private bool attackReady = true;  // RENOMEADO - cooldown interno do ataque
    private bool isKnockbacking;

    private bool UsingMobileInput => mobileInput != null && mobileInput.UseMobileInput;

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
        obstacleMove.GetComponent<ObstacleMove>();
        CacheComponents();
        CacheMobileInput();
        rb.gravityScale = defaultGS;
        defaultScale = sprite.transform.localScale;
    }

    private void CacheComponents()
    {
        if (!rb)
            rb = GetComponent<Rigidbody2D>();

        if (!bodyCollider)
            bodyCollider = GetComponent<BoxCollider2D>();

        if (!sprite)
            sprite = GetComponentInChildren<SpriteRenderer>();

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

        // alvo de rotação: sprite se existir, senão o próprio transform
        attackSpinTarget = (spinSpriteOnlyDuringAttack && sprite != null)
            ? sprite.transform
            : transform;
    }

    private void CacheMobileInput()
    {
        if (mobileInput == null)
        {
            mobileInput = FindFirstObjectByType<MobileInputBridge>();
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
        else
        {
            // MESMO SEM CANMOVE, permite ataque durante slow motion
            HandleAttackInput();
        }

        HandleVictorySequence();

        // [NEW] Rotação apenas no SPRITE quando morto (não altera o corpo/colisor)
        if (playerDead && spinOnDeathActive && sprite != null)
        {
            currentSpriteRotation += deathSpinSpeed * Time.deltaTime;
            sprite.transform.localRotation = Quaternion.Euler(0f, 0f, currentSpriteRotation);
        }
        //HandleSquashStretch();

    }

    #region Jump Logic

    private void HandleJump()
    {
        RefreshGroundedStatus();

        bool jumpPressed = ReadJumpPressed();
        bool jumpHeld = ReadJumpHeld();
        bool jumpReleased = ReadJumpReleased();

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

        float moveInput = ReadHorizontalInput();
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
        if (ReadFastFallPressed() && !isGrounded)
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
        // Detecta input de ataque
        bool attackInput = ReadAttackPressed();
        
        // DEBUG
        if (attackInput)
        {
            Debug.Log($"[Player] Input detectado! attackReady={attackReady}, canAttack={canAttack}");
        }
        
        if (attackInput && attackReady && canAttack)
        {
            Debug.Log("[Player] ATAQUE INICIADO!");
            BeginAttack();
        }

        UpdateAttackCooldown();
        UpdateAirAttackTimer();
    }

    private void BeginAttack()
    {
        attackReady = false;
    
        // FORÇA a ativação imediata do hitbox
        if (attackHB != null)
        {
            attackHB.enabled = true;
        }

        attackTimer = 0f;
        isAttacking = true;
        rb.gravityScale = 0f;
    
        // 🔧 Cancela o pulo sustentado
        isJumping = false;
        jumpTimer = 0f;
    
        // 🔧 Zera a velocidade vertical
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

        Debug.Log($"[Player] ATAQUE INICIADO! IsAttackActive={IsAttackActive}, HitBox={attackHB != null && attackHB.enabled}");

        // ativa spin do ataque
        if (attackSpinEnabled)
        {
            attackSpinActive = true;
            attackSpinAngle = 0f;
        }
    }

    /// <summary>
    /// Método público para forçar início do ataque (usado pela cutscene)
    /// </summary>
    public void ForceAttack()
    {
        Debug.Log($"[Player] ForceAttack chamado! attackReady={attackReady}, canAttack={canAttack}, CanMove={CanMove}");
        
        // FORÇA o ataque mesmo que attackReady seja false
        attackReady = true; // Reseta o cooldown
        canAttack = true;   // Garante que pode atacar
        
        if (attackHB != null)
        {
            Debug.Log("[Player] Forçando ataque diretamente...");
            BeginAttack();
        }
        else
        {
            Debug.LogError("[Player] attackHB é NULL!");
        }
    }

    private void HandleOngoingAttackMovement()
    {
        if (!isAttacking)
        {
            return;
        }

        if (!isKnockbacking)
        {
            // USA UNSCALED para funcionar em slow motion
            float actualAttackForce = sprite != null && sprite.flipX ? -attackForce : attackForce;
            rb.linearVelocity = new Vector2(actualAttackForce, rb.linearVelocity.y);
            
            Debug.Log($"[Player] Aplicando movimento do ataque! Velocity={rb.linearVelocity.x}");

            if (!isGrounded && ReadFastFallPressed())
            {
                CancelAttack();
                return;
            }
        }

        // GIRO DURANTE O ATAQUE - usa unscaledDeltaTime
        if (attackSpinEnabled && attackSpinActive && attackSpinTarget != null && !playerDead)
        {
            float dir = sprite != null && sprite.flipX ? -1f : 1f;
            attackSpinAngle += attackSpinSpeed * Time.unscaledDeltaTime * dir;
            attackSpinTarget.localRotation = Quaternion.Euler(0f, 0f, attackSpinAngle);
        }
    }

    private void UpdateAttackCooldown()
    {
        if (attackReady)
        {
            return;
        }

        // USA UNSCALED para funcionar em slow motion
        attackCDTimer += Time.unscaledDeltaTime;

        if (attackCDTimer >= attackCD)
        {
            attackReady = true;
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

        // USA UNSCALED para funcionar em slow motion
        attackTimer += Time.unscaledDeltaTime;

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

        // RESET DO GIRO DO ATAQUE
        if (attackSpinActive && attackSpinTarget != null)
        {
            attackSpinActive = false;
            attackSpinTarget.localRotation = Quaternion.Euler(0f, 0f, 0f);
        }
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
        GameManager.Instance.victoryText.text = GetVictoryPrompt();
    }

    public void Death()
    {
        if (playerDead)
        {
            return;
        }

        GameObject explosionInstance = Instantiate(explosion, transform.position, Quaternion.identity);
        AudioManager.audioInstance.ExplodeSFX();
        Destroy(explosionInstance, 1f);

        playerDead = true;
        CanMove = false;

        rb.linearVelocity = new Vector2(-10f, 25f) * impForce * Time.deltaTime;
        bodyCollider.enabled = false;
        rb.freezeRotation = false;
        rb.AddTorque(torqueForce);

        // se estava girando no ataque, desativa para evitar soma dupla
        attackSpinActive = false;

        // inicia spin de morte a partir do ângulo atual do sprite (fica suave)
        if (sprite != null)
        {
            currentSpriteRotation = sprite.transform.localEulerAngles.z;
        }

        // ativa giro apenas no SPRITE (o corpo/colisor pode rodar pela física)
        spinOnDeathActive = true;
    }

    #endregion

    #region Collision Handling

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (!other.gameObject.CompareTag("Obstacle") || GameManager.Instance.isCheatOn)
            return;

        
        // Se o ataque está ativo OU se estou no knockback (ou seja, acertei algo)
        // então IGNORA a colisão física com o obstáculo.
        if (IsAttackActive || isKnockbacking)
        {
            Debug.Log("[Player] Colisão com obstáculo ignorada (ataque/knockback ativo).");
            return;
        }
        // =======================

        GameManager.Instance.playerAlive = false;
        Debug.Log("morreu burro");
        GameManager.Instance.StopScene();
        Death();
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        
        if (other.gameObject.layer==13)
        {
            obsMove.SetSpeedMultiplier(1f);
            Debug.Log("colidiu com o trigger");
        }
        
        if (!other.gameObject.CompareTag("JumpDetector"))
        {
            return;
        }

        GameManager.Instance.UpdateScore(1);
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
    
    #region Misc.
    
    private void HandleSquashStretch()
    {
        if (!enableSquashStretch || sprite == null)
            return;

        // STRETCH ao iniciar salto
        if (!wasGroundedLastFrame && isGrounded) 
        {
            // acabou de aterrissar → squash
            sprite.transform.localScale = new Vector3(landSquashX, landSquashY, 1f);
        }
        else if (wasGroundedLastFrame && !isGrounded)
        {
            // acabou de sair do chão → stretch
            sprite.transform.localScale = new Vector3(jumpStretchX, jumpStretchY, 1f);
        }

        // Voltar pro tamanho normal suavemente
        sprite.transform.localScale = Vector3.Lerp(
            sprite.transform.localScale,
            defaultScale,
            Time.deltaTime * stretchReturnSpeed
        );

        wasGroundedLastFrame = isGrounded;
    }

    private float ReadHorizontalInput()
    {
        return UsingMobileInput ? mobileInput.Horizontal : Input.GetAxisRaw("Horizontal");
    }

    private bool ReadJumpPressed()
    {
        return UsingMobileInput ? mobileInput.JumpPressedThisFrame : Input.GetButtonDown("Jump");
    }

    private bool ReadJumpHeld()
    {
        return UsingMobileInput ? mobileInput.JumpHeld : Input.GetButton("Jump");
    }

    private bool ReadJumpReleased()
    {
        return UsingMobileInput ? mobileInput.JumpReleasedThisFrame : Input.GetButtonUp("Jump");
    }

    private bool ReadAttackPressed()
    {
        return UsingMobileInput ? mobileInput.AttackPressedThisFrame : Input.GetMouseButtonDown(0);
    }

    private bool ReadFastFallPressed()
    {
        return UsingMobileInput ? mobileInput.FastFallPressedThisFrame : Input.GetKeyDown(KeyCode.S);
    }

    private string GetVictoryPrompt()
    {
        return Application.isMobilePlatform
            ? "Passou de fase! Toque para continuar"
            : "Passou de fase! Aperte \"Espaço\" para continuar";
    }

    
    
    #endregion
}