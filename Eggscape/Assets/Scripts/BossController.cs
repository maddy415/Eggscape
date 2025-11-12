using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Random = UnityEngine.Random;

/// <summary>
/// Boss 2D com fases e ataques:
/// - Charge (com âncora; dash pode ser cancelado por ataque do player)
/// - JumpSmash (clássico)
/// - JumpSuperHigh (lock-on/meteoro + shockwaves ao pousar)
/// - BulletHell (recuo opcional + padrões via BulletPatternSO + OBJECT POOL)
/// Player morre com 1 hit ao encostar (exceto enquanto está atacando).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BossSimpleController : MonoBehaviour
{
    #region Tipos de dados (Inspector)

    public enum AttackType { Charge, JumpSmash, JumpSuperHigh, BulletHell }

    [System.Serializable]
    public class Attack
    {
        public string name = "Charge";
        public AttackType type = AttackType.Charge;

        [Header("Telegraph/Timing")]
        public float windup = 0.5f;
        public float recovery = 0.5f;

        [Header("One-hit on touch (sempre que encostar)")]
        public bool killOnTouch = true;
        public LayerMask playerLayers = 0;

        [Header("Pesagem p/ sorteio")]
        public int weight = 1;

        // ------------------- Charge -------------------
        [Header("Charge params")]
        public float dashSpeed = 12f;
        public float dashDuration = 0.6f;
        public Vector2 chargeHitbox = new Vector2(1.2f, 1.0f);

        // ------------------- JumpSmash (clássico) -------------------
        [Header("JumpSmash (clássico)")]
        public float jumpForceSmash = 12f;
        public float gravityDuringJumpSmash = 2.2f;
        public float smashRadiusSmash = 3.0f;

        // ------------------- JumpSuperHigh (meteoro lock-on) -------------------
        [Header("JumpSuperHigh (meteoro lock-on)")]
        public float jumpForceSuper = 16f;
        public float riseGravitySuper = 2.0f;

        public GameObject lockOnIndicatorPrefab;
        public float indicatorYOffset = -1.2f;

        public float lockOnDelay = 0.7f;
        public float lockOnFreeze = 0.6f;
        public float followXSharpness = 20f;

        public float fallImpulse = 40f;
        public float fallGravity = 10f;

        public bool hideWhileOffscreen = true;
        public float offscreenMargin = 0.08f;

        public float smashRadiusSuper = 3.0f;

        // ------------------- Mask -------------------
        [Header("Quem leva hit (LayerMask)")]
        public LayerMask hitMask = ~0;

        [Header("Identificação de Player (one-hit)")]
        public LayerMask playerMask = 0;
    }

    [System.Serializable]
    public class Phase
    {
        public string phaseName = "Fase 1";
        public List<Attack> attacks = new List<Attack>();
        public float minDelayBetween = 0.6f;
        public float maxDelayBetween = 1.4f;

        [Header("Transição de fase (use um ou ambos)")]
        [Range(0, 1f)] public float hpThreshold = 0f;
        public float timeLimit = 0f;
    }

    #endregion

    #region Inspector

    [Header("Vida do Boss")]
    public float maxHealth = 120f;
    [ReadOnly] public float currentHealth;
    public bool invulnerable = false;

    [Header("Alvos / Refs")]
    public float speed = 6f;
    public Transform player;
    public SpriteRenderer bossSprite;
    public Color telegraphColor = Color.red;

    [Header("Fases (na ordem)")]
    public List<Phase> phases = new List<Phase>();

    [Header("Intro")]
    public float introDuration = 1.2f;

    [Header("Ground Check")]
    public LayerMask groundMask = ~0;
    public float groundProbeDistance = 0.15f;

    // ======= Charge Anchor =======
    [Header("Charge Anchor (ponto fixo do dash)")]
    public bool useChargeAnchor = true;
    public Transform chargeAnchor;
    public Collider2D chargeAnchorTrigger;
    public bool requireAnchorTriggerForDash = true;
    public float chargeLeapUpForce = 10f;
    public float chargeLeapHorizSpeed = 6f;
    public float chargeAirAccel = 40f;
    public float chargePreDashHold = 0.35f;
    public float chargeAnchorXTolerance = 0.15f;

    // ======= Cancel do dash ao ser atingido =======
    [Header("Charge Cancel ao ser atingido")]
    public LayerMask playerAttackMask;
    public float dashCancelKnockback = 10f;
    public float dashCancelStun = 0.2f;

    // ======= Shockwave do JumpSuperHigh =======
    [Header("Shockwave (JumpSuperHigh landing)")]
    public GameObject shockwavePrefab;
    public float shockwaveSpawnYOffset = 0.15f;
    public float shockwaveInitialSpeed = 12f;

    // ======= Bullet Hell =======
    [Header("Bullet Hell")]
    public BulletPatternSO bulletPattern;
    public GameObject bulletPrefab;
    public LayerMask bulletHitMask = 0;
    public float bulletHellDurationOverride = 0f;

    [Header("Bullet Hell – Retreat")]
    public float bhMinDistanceToPlayer = 5f;
    public float bhRetreatTargetDistance = 8f;
    public float bhRetreatSpeed = 8f;
    public float bhRetreatMaxTime = 1.5f;

    [Header("Bullet Hell – Spawn")]
    public float bulletSpawnYOffset = 0.25f;

    [Header("Bullet Hell – OBJECT POOL")]
    [Tooltip("Pool de foices. Se nulo, usa Instantiate como fallback.")]
    public ScythePool scythePool;
    
    [Header("Trilha Sonora")]
    public AudioClip bossTheme;
    public AudioClip phaseChangeTheme;
    public AudioClip deathTheme;

    #endregion

    #region Internals
    private Rigidbody2D rb;
    private Collider2D col;
    private int phaseIndex = -1;
    private float phaseTimer = 0f;
    private bool dead = false;

    // Charge/Anchor
    private bool anchorTouchedThisCharge = false;

    // Dash em execução (para cancelar on-hit)
    private bool isChargingDash = false;
    private bool dashWasCancelled = false;
    #endregion

    #region Unity

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        currentHealth = maxHealth;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        if (!col) col = GetComponentInChildren<Collider2D>();
        currentHealth = maxHealth;

        if (chargeAnchor != null && chargeAnchorTrigger == null)
            chargeAnchorTrigger = chargeAnchor.GetComponent<Collider2D>();
    }

    private void Start()
    {
        BeginIntro();
    }

    #endregion

    #region Ciclo da luta

    public void BeginIntro()
    {
        if (dead) return;
        StartCoroutine(IntroRoutine());
    }

    private IEnumerator IntroRoutine()
    {
        AudioManager.audioInstance.Crossfade(bossTheme, 1f);
        invulnerable = true;
        if (bossSprite) Flash(telegraphColor, introDuration);

        rb.linearVelocity = Vector2.left * speed;
        yield return new WaitForSeconds(introDuration);

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        invulnerable = false;
        GoToPhase(0);
    }

    private void GoToPhase(int idx)
    {
        if (phases == null || phases.Count == 0) { Debug.LogWarning("Sem fases configuradas."); return; }
        phaseIndex = Mathf.Clamp(idx, 0, phases.Count - 1);
        phaseTimer = 0f;
        StopAllCoroutines();
        StartCoroutine(PhaseLoop());
    }

    private IEnumerator PhaseLoop()
    {
        while (!dead)
        {
            var phase = phases[phaseIndex];

            bool isLast = (phaseIndex == phases.Count - 1);
            phaseTimer += Time.deltaTime;
            float hpFrac = currentHealth / maxHealth;

            if (!isLast)
            {
                if (phase.hpThreshold > 0 && hpFrac <= phase.hpThreshold)
                {
                    TryAdvancePhase();
                    yield break;
                }
                if (phase.timeLimit > 0 && phaseTimer >= phase.timeLimit)
                {
                    TryAdvancePhase();
                    yield break;
                }
            }

            var attack = PickWeighted(phase.attacks);
            if (attack == null) { yield return null; continue; }

            yield return ExecuteAttack(attack);

            float delay = Random.Range(phase.minDelayBetween, phase.maxDelayBetween);
            yield return new WaitForSeconds(delay);
        }
    }

    private void TryAdvancePhase()
    {
        int next = phaseIndex + 1;
        if (next < phases.Count) GoToPhase(next);
        else phaseTimer = 0f;
    }

    #endregion

    #region Ataques

    private IEnumerator ExecuteAttack(Attack a)
    {
        if (bossSprite) Flash(telegraphColor, a.windup);
        yield return new WaitForSeconds(a.windup);

        switch (a.type)
        {
            case AttackType.Charge:
                yield return Attack_Charge(a);
                break;
            case AttackType.JumpSmash:
                yield return Attack_JumpSmash(a);
                break;
            case AttackType.JumpSuperHigh:
                yield return Attack_JumpSuperHigh(a);
                break;
            case AttackType.BulletHell:
                yield return Attack_BulletHell(a);
                break;
        }

        yield return new WaitForSeconds(a.recovery);
    }

    // ====== Charge ======
    private IEnumerator Attack_Charge(Attack a)
    {
        if (!useChargeAnchor || chargeAnchor == null)
        {
            FacePlayerX();

            float t0 = 0f;
            Vector2 dir0 = transform.localScale.x >= 0 ? Vector2.right : Vector2.left;

            isChargingDash = true;
            dashWasCancelled = false;

            while (t0 < a.dashDuration && !dashWasCancelled)
            {
                t0 += Time.deltaTime;
                rb.linearVelocity = new Vector2(dir0.x * a.dashSpeed, rb.linearVelocity.y);

                var hits0 = Physics2D.OverlapBoxAll(transform.position, a.chargeHitbox, 0f, a.hitMask);
                foreach (var h in hits0) ApplyPlayerHitIfAny(h.gameObject, a);
                yield return null;
            }

            isChargingDash = false;
            if (dashWasCancelled && dashCancelStun > 0f) yield return new WaitForSeconds(dashCancelStun);
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            yield break;
        }

        // -------- USANDO ÂNCORA --------
        anchorTouchedThisCharge = false;
        if (chargeAnchorTrigger == null && chargeAnchor != null)
            chargeAnchorTrigger = chargeAnchor.GetComponent<Collider2D>();

        float dx0 = chargeAnchor.position.x - transform.position.x;
        float absDx0 = Mathf.Abs(dx0);

        // A) já perto do anchor
        if (absDx0 <= chargeAnchorXTolerance)
        {
            yield return StartCoroutine(WaitUntilGrounded(0.8f));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            rb.linearVelocity = Vector2.zero;

            FacePlayerX();
            if (chargePreDashHold > 0f) yield return new WaitForSeconds(chargePreDashHold);

            float elapsedA = 0f;
            Vector2 dirA = transform.localScale.x >= 0 ? Vector2.right : Vector2.left;

            isChargingDash = true;
            dashWasCancelled = false;

            while (elapsedA < a.dashDuration && !dashWasCancelled)
            {
                elapsedA += Time.fixedDeltaTime;
                rb.linearVelocity = new Vector2(dirA.x * a.dashSpeed, 0f);

                var hitsA = Physics2D.OverlapBoxAll(transform.position, a.chargeHitbox, 0f, a.hitMask);
                foreach (var h in hitsA) ApplyPlayerHitIfAny(h.gameObject, a);

                yield return new WaitForFixedUpdate();
            }

            isChargingDash = false;
            if (dashWasCancelled && dashCancelStun > 0f) yield return new WaitForSeconds(dashCancelStun);
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            yield break;
        }

        // B) longe do anchor -> salto até lá
        FaceX(chargeAnchor.position.x);

        float initialDirX = Mathf.Sign(chargeAnchor.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(initialDirX * chargeLeapHorizSpeed, 0f);
        rb.AddForce(Vector2.up * chargeLeapUpForce, ForceMode2D.Impulse);

        yield return StartCoroutine(WaitUntilAirborne(0.4f));

        while (!IsGrounded())
        {
            float dx = chargeAnchor.position.x - transform.position.x;
            float dirX = Mathf.Sign(dx);
            float absDx = Mathf.Abs(dx);

            float targetVx = dirX * chargeLeapHorizSpeed;
            if (absDx < 1f) targetVx *= Mathf.Clamp01(absDx);

            float newVx = Mathf.MoveTowards(rb.linearVelocity.x, targetVx, chargeAirAccel * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newVx, rb.linearVelocity.y);

            yield return new WaitForFixedUpdate();
        }

        yield return StartCoroutine(WaitForAnchorTouchAndGrounded(chargeAnchor.position.x, chargeAnchorXTolerance, requireAnchorTriggerForDash, 1.0f));

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        rb.linearVelocity = Vector2.zero;

        FacePlayerX();
        if (chargePreDashHold > 0f) yield return new WaitForSeconds(chargePreDashHold);

        float elapsed = 0f;
        Vector2 dir = transform.localScale.x >= 0 ? Vector2.right : Vector2.left;

        isChargingDash = true;
        dashWasCancelled = false;

        while (elapsed < a.dashDuration && !dashWasCancelled)
        {
            elapsed += Time.fixedDeltaTime;
            rb.linearVelocity = new Vector2(dir.x * a.dashSpeed, 0f);

            var hits = Physics2D.OverlapBoxAll(transform.position, a.chargeHitbox, 0f, a.hitMask);
            foreach (var h in hits) ApplyPlayerHitIfAny(h.gameObject, a);

            yield return new WaitForFixedUpdate();
        }

        isChargingDash = false;
        if (dashWasCancelled && dashCancelStun > 0f) yield return new WaitForSeconds(dashCancelStun);
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    // ====== JumpSmash ======
    private IEnumerator Attack_JumpSmash(Attack a)
    {
        float originalGravity = rb.gravityScale;

        rb.gravityScale = a.gravityDuringJumpSmash;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * a.jumpForceSmash, ForceMode2D.Impulse);

        while (rb.linearVelocity.y > -0.1f) yield return null;

        yield return StartCoroutine(WaitUntilGrounded(0.4f));

        var hits = Physics2D.OverlapCircleAll(transform.position, a.smashRadiusSmash, a.hitMask);
        foreach (var h in hits) ApplyPlayerHitIfAny(h.gameObject, a);

        rb.gravityScale = originalGravity;

        if (bossSprite) Flash(Color.white, 0.15f);
    }

    // ====== JumpSuperHigh ======
    private IEnumerator Attack_JumpSuperHigh(Attack a)
    {
        float originalGravity = rb.gravityScale;

        rb.gravityScale = a.riseGravitySuper;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(Vector2.up * a.jumpForceSuper, ForceMode2D.Impulse);

        yield return StartCoroutine(WaitUntilAboveCameraTop(a.offscreenMargin));

        float hoverY = rb.position.y;
        if (a.hideWhileOffscreen && bossSprite) bossSprite.enabled = false;

        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        GameObject indicator = null;
        float t = 0f;
        while (t < a.lockOnDelay)
        {
            t += Time.deltaTime;

            if (player)
            {
                float targetX = player.position.x;
                float newX = Mathf.Lerp(rb.position.x, targetX, Time.deltaTime * Mathf.Max(1f, a.followXSharpness));
                rb.MovePosition(new Vector2(newX, hoverY));

                if (a.lockOnIndicatorPrefab)
                {
                    Vector3 indPos = new Vector3(player.position.x, player.position.y + a.indicatorYOffset, 0f);
                    if (!indicator) indicator = Instantiate(a.lockOnIndicatorPrefab, indPos, Quaternion.identity);
                    else indicator.transform.position = indPos;
                }
            }

            yield return null;
        }

        float dropX = player ? player.position.x : rb.position.x;
        float freezeT = 0f;
        while (freezeT < a.lockOnFreeze)
        {
            freezeT += Time.deltaTime;

            rb.MovePosition(new Vector2(rb.position.x, hoverY));

            if (indicator)
            {
                float baseY = player ? player.position.y : (indicator.transform.position.y - a.indicatorYOffset);
                indicator.transform.position = new Vector3(dropX, baseY + a.indicatorYOffset, 0f);
            }

            yield return null;
        }

        transform.position = new Vector3(dropX, hoverY, transform.position.z);
        rb.gravityScale = a.fallGravity;
        rb.linearVelocity = Vector2.zero;
        rb.linearDamping = 0f;
        rb.AddForce(Vector2.down * a.fallImpulse, ForceMode2D.Impulse);

        yield return StartCoroutine(WaitUntilReenterCameraFromTop());
        if (bossSprite) bossSprite.enabled = true;

        while (!IsGrounded())
        {
            rb.MovePosition(new Vector2(dropX, rb.position.y));
            yield return new WaitForFixedUpdate();
        }

        var hits2 = Physics2D.OverlapCircleAll(transform.position, a.smashRadiusSuper, a.hitMask);
        foreach (var h in hits2) ApplyPlayerHitIfAny(h.gameObject, a);

        if (shockwavePrefab != null)
        {
            float baseY = col != null ? col.bounds.min.y + shockwaveSpawnYOffset
                                      : transform.position.y + shockwaveSpawnYOffset;
            Vector3 spawnPos = new Vector3(transform.position.x, baseY, transform.position.z);

            var swR = Instantiate(shockwavePrefab, spawnPos, Quaternion.identity);
            var cR = swR.GetComponent<Shockwave>();
            if (cR != null) { cR.speed = shockwaveInitialSpeed; cR.Initialize(Vector2.right); }

            var swL = Instantiate(shockwavePrefab, spawnPos, Quaternion.identity);
            var cL = swL.GetComponent<Shockwave>();
            if (cL != null) { cL.speed = shockwaveInitialSpeed; cL.Initialize(Vector2.left); }
        }

        if (indicator) Destroy(indicator);

        rb.gravityScale = originalGravity;

        if (bossSprite) Flash(Color.white, 0.15f);
    }

    // ====== Bullet Hell ======
    private IEnumerator Attack_BulletHell(Attack a)
    {
        if (bulletPattern == null || (bulletPrefab == null && scythePool == null)) yield break;

        // 1) RETREAT se colado demais
        if (player != null)
        {
            float dx = Mathf.Abs(player.position.x - transform.position.x);
            if (dx < bhMinDistanceToPlayer)
            {
                float timer = 0f;
                while (timer < bhRetreatMaxTime)
                {
                    dx = Mathf.Abs(player.position.x - transform.position.x);
                    if (dx >= bhRetreatTargetDistance) break;

                    float dir = Mathf.Sign(transform.position.x - player.position.x);
                    rb.linearVelocity = new Vector2(dir * bhRetreatSpeed, rb.linearVelocity.y);
                    if (IsGrounded()) rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

                    FacePlayerX();
                    timer += Time.deltaTime;
                    yield return null;
                }
            }
        }

        // 2) PARA o boss e começa a atirar parado
        rb.linearVelocity = Vector2.zero;

        float duration = bulletHellDurationOverride > 0f ? bulletHellDurationOverride : bulletPattern.duration;
        float elapsed = 0f;
        float fireInt = bulletPattern.FireInterval;
        float fireT = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fireT += Time.deltaTime;

            if (fireT >= fireInt)
            {
                fireT -= fireInt;

                var dirs = bulletPattern.GenerateBurst(elapsed, transform, player);
                SpawnBullets(dirs, bulletPattern);
            }

            yield return null;
        }
    }

    #endregion

    #region Helpers (câmera / chão / âncora / bullet)

    private void SpawnBullets(List<Vector2> dirs, BulletPatternSO so)
    {
        if (dirs == null || dirs.Count == 0) return;

        float baseY = (col != null ? col.bounds.min.y : transform.position.y) + bulletSpawnYOffset;
        Vector3 spawnPos = new Vector3(transform.position.x, baseY, 0f);

        foreach (var d in dirs)
        {
            // ======== POOL ========
            if (scythePool != null)
            {
                var sc = scythePool.Spawn(
                    spawnPos,
                    d,
                    so.bulletSpeed,
                    so.bulletLifeTime,
                    bulletHitMask
                );

                sc.Initialize(d, so.bulletSpeed, so.bulletLifeTime, bulletHitMask);
                continue;
            }

            // ======== Fallback: Instantiate ========
            var go = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

            var scythe = go.GetComponent<ScytheProjectile>();
            if (scythe != null)
            {
                scythe.Initialize(d, so.bulletSpeed, so.bulletLifeTime, bulletHitMask);
                continue;
            }

            var rb2 = go.GetComponent<Rigidbody2D>();
            if (rb2 != null)
            {
                rb2.bodyType = RigidbodyType2D.Kinematic;
                rb2.linearVelocity = d * so.bulletSpeed;
            }
        }
    }

    private IEnumerator WaitUntilAboveCameraTop(float margin = 0.02f, float timeout = 3f)
    {
        var cam = Camera.main;
        float t = 0f;

        while (true)
        {
            t += Time.deltaTime;
            if (t > timeout) yield break;

            if (cam)
            {
                Vector3 vp = cam.WorldToViewportPoint(transform.position);
                if (vp.y > 1f + margin) yield break;
            }
            yield return null;
        }
    }

    private IEnumerator WaitUntilAirborne(float timeout = 0.4f)
    {
        float t = 0f;
        yield return null;

        while (t < timeout && IsGrounded())
        {
            t += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator WaitForAnchorTouchAndGrounded(float anchorX, float tol, bool requireTrigger, float timeout)
    {
        float t = 0f;
        while (t < timeout)
        {
            bool grounded = IsGrounded();
            float dx = Mathf.Abs(transform.position.x - anchorX);

            bool withinX = (dx <= tol);
            bool okAnchor = requireTrigger ? anchorTouchedThisCharge : withinX;

            if (grounded && okAnchor) yield break;

            if (grounded && !requireTrigger && !withinX)
            {
                float dir = Mathf.Sign(anchorX - transform.position.x);
                rb.linearVelocity = new Vector2(dir * Mathf.Min(chargeLeapHorizSpeed, 3f), rb.linearVelocity.y);
            }

            t += Time.deltaTime;
            yield return null;
        }

        if (IsGrounded())
        {
            float dx = Mathf.Abs(transform.position.x - anchorX);
            if (dx <= tol) yield break;
        }
    }

    private IEnumerator WaitUntilReenterCameraFromTop(float timeout = 3f)
    {
        var cam = Camera.main;
        float t = 0f;

        while (true)
        {
            t += Time.deltaTime;
            if (t > timeout) yield break;

            if (cam)
            {
                var vp = cam.WorldToViewportPoint(transform.position);
                if (vp.y <= 1f) yield break;
            }
            yield return null;
        }
    }

    private bool IsGrounded()
    {
        if (!col) return false;
        Bounds b = col.bounds;
        float castDist = groundProbeDistance;
        RaycastHit2D hit = Physics2D.BoxCast(b.center, b.size, 0f, Vector2.down, castDist, groundMask);
        return hit.collider != null;
    }

    private IEnumerator WaitUntilGrounded(float timeout = 1.5f)
    {
        float t = 0f;
        while (t < timeout)
        {
            if (IsGrounded()) yield break;
            t += Time.deltaTime;
            yield return null;
        }
    }

    #endregion

    #region Dano / Morte do Boss

    public void TakeDamage(float amount)
    {
        if (dead || invulnerable) return;
        currentHealth = Mathf.Max(0, currentHealth - Mathf.Abs(amount));
        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        dead = true;
        StopAllCoroutines();
        rb.linearVelocity = Vector2.zero;
        gameObject.SetActive(false);
    }

    #endregion

    #region One-Hit no Player

    private void ApplyPlayerHitIfAny(GameObject target, Attack a)
    {
        bool isPlayerTag = target.CompareTag("Player");
        bool isPlayerLayer = (a.playerMask.value & (1 << target.layer)) != 0;
        if (!isPlayerTag && !isPlayerLayer) return;

        var playerComp = target.GetComponent<Player>()
                         ?? target.GetComponentInParent<Player>()
                         ?? target.GetComponentInChildren<Player>();

        if (playerComp != null && playerComp.IsAttackActive) return;

        if (playerComp != null)
        {
            try { playerComp.Death(); }
            catch (Exception e) { Debug.LogError($"[Boss] Falha ao chamar Player.Death(): {e.Message}"); }
        }
        else
        {
            Debug.LogWarning("[Boss] Player detectado mas não achei componente 'Player'.");
        }
    }

    #endregion

    #region Utilidades

    private Attack PickWeighted(List<Attack> list)
    {
        if (list == null || list.Count == 0) return null;
        int sum = 0; foreach (var a in list) sum += Mathf.Max(1, a.weight);
        int r = Random.Range(0, sum);
        int acc = 0;
        foreach (var a in list)
        {
            acc += Mathf.Max(1, a.weight);
            if (r < acc) return a;
        }
        return list[0];
    }

    private void FacePlayerX()
    {
        if (!player) return;
        float dir = Mathf.Sign(player.position.x - transform.position.x);
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (dir >= 0 ? 1f : -1f);
        transform.localScale = s;
    }

    private void FaceX(float worldX)
    {
        float dir = Mathf.Sign(worldX - transform.position.x);
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (dir >= 0 ? 1f : -1f);
        transform.localScale = s;
    }

    private void Flash(Color c, float duration)
    {
        if (!bossSprite) return;
        StartCoroutine(FlashRoutine(c, duration));
    }
    private IEnumerator FlashRoutine(Color c, float duration)
    {
        Color original = bossSprite.color;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.PingPong(t * 8f, 1f);
            bossSprite.color = Color.Lerp(original, c, p);
            yield return null;
        }
        bossSprite.color = original;
    }

    private static bool IsInLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

    #endregion

    #region Collisions / Triggers

    private void TryKillPlayerOnContact(GameObject hit)
    {
        var playerRef = hit.GetComponent<Player>()
                        ?? hit.GetComponentInParent<Player>()
                        ?? hit.GetComponentInChildren<Player>();

        if (playerRef == null) return;
        if (playerRef.IsAttackActive) return;

        try { playerRef.Death(); }
        catch (Exception e) { Debug.LogError($"[Boss] Falha ao chamar Player.Death() no contato: {e.Message}"); }
    }

    private void MaybeCancelDashFromHit(Component hitter)
    {
        if (!isChargingDash) return;

        int layer = hitter.gameObject.layer;
        if (!IsInLayerMask(layer, playerAttackMask)) return;

        var playerComp = hitter.GetComponent<Player>()
                       ?? hitter.GetComponentInParent<Player>()
                       ?? hitter.GetComponentInChildren<Player>();

        float dmg = 0f;
        if (playerComp != null)
        {
            dmg = Mathf.Max(0f, playerComp.attackDamage);
            TakeDamage(dmg);
            Debug.Log($"[Boss] Dash CANCELADO por ataque do player. Dano: {dmg} | HP: {currentHealth}/{maxHealth}");
        }
        else
        {
            Debug.Log("[Boss] Dash CANCELADO por ataque (sem Player encontrado).");
        }

        dashWasCancelled = true;

        float dir = Mathf.Sign(transform.position.x - hitter.transform.position.x);
        if (dir == 0f) dir = (transform.localScale.x >= 0) ? -1f : 1f;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        rb.AddForce(new Vector2(dir * dashCancelKnockback, 0f), ForceMode2D.Impulse);
    }

    private void OnCollisionEnter2D(Collision2D c)
    {
        MaybeCancelDashFromHit(c.collider);
        TryKillPlayerOnContact(c.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (useChargeAnchor && chargeAnchorTrigger != null && other == chargeAnchorTrigger)
        {
            anchorTouchedThisCharge = true;
            return;
        }

        MaybeCancelDashFromHit(other);
        TryKillPlayerOnContact(other.gameObject);
    }

    #endregion

    #region Gizmos (debug)

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (phases == null || phases.Count == 0) return;
        int idx = Mathf.Clamp(phaseIndex, 0, phases.Count - 1);
        if (idx < 0 || idx >= phases.Count) return;

        var phase = phases[idx];
        foreach (var atk in phase.attacks)
        {
            if (atk.type == AttackType.Charge)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(transform.position, new Vector3(atk.chargeHitbox.x, atk.chargeHitbox.y, 0));
            }
            else if (atk.type == AttackType.JumpSmash)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, atk.smashRadiusSmash);
            }
            else if (atk.type == AttackType.JumpSuperHigh)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, atk.smashRadiusSuper);
            }
            else // BulletHell
            {
                Gizmos.color = new Color(0.2f, 1f, 1f, 0.65f);
                Gizmos.DrawWireSphere(transform.position, 0.25f);
            }
        }

        if (useChargeAnchor && chargeAnchor != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(chargeAnchor.position, 0.12f);

            Gizmos.color = new Color(1f, 0f, 1f, 0.35f);
            Gizmos.DrawCube(new Vector3(chargeAnchor.position.x, transform.position.y, 0f),
                            new Vector3(chargeAnchorXTolerance * 2f, 0.05f, 0f));
        }
    }
#endif

    #endregion
}

/// <summary> ReadOnly no Inspector (cosmético). </summary>
public class ReadOnlyAttribute : PropertyAttribute {}
#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
        EditorGUI.GetPropertyHeight(property, label, true);
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;
    }
}
#endif
