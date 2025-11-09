using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Random = UnityEngine.Random;

/// <summary>
/// Boss 2D com fases e ataques:
/// - Charge
/// - JumpSmash (clássico)
/// - JumpSuperHigh (lock-on/meteoro)
/// Player morre com 1 hit ao encostar (exceto enquanto está atacando).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BossSimpleController : MonoBehaviour
{
    #region Tipos de dados (Inspector)

    public enum AttackType { Charge, JumpSmash, JumpSuperHigh }

    [System.Serializable]
    public class Attack
    {
        public string name = "Charge";
        public AttackType type = AttackType.Charge;

        [Header("Telegraph/Timing")]
        [Tooltip("Tempo antes de executar (telegraph/aviso).")]
        public float windup = 0.5f;
        [Tooltip("Tempo de recuperação após o ataque terminar.")]
        public float recovery = 0.5f;

        [Header("One-hit on touch (sempre que encostar)")]
        public bool killOnTouch = true;
        [Tooltip("Layers considerados Player para kill por contato (além da tag 'Player').")]
        public LayerMask playerLayers = 0;

        [Header("Pesagem p/ sorteio")]
        [Tooltip("Quanto maior, mais chances de sair este ataque.")]
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
        [Tooltip("Força para subir para fora da câmera.")]
        public float jumpForceSuper = 16f;
        [Tooltip("Gravidade durante a subida (antes de sair da câmera).")]
        public float riseGravitySuper = 2.0f;

        [Tooltip("Prefab do indicador que aparece durante o lock-on (opcional).")]
        public GameObject lockOnIndicatorPrefab;
        [Tooltip("Offset vertical do indicador em relação ao player.")]
        public float indicatorYOffset = -1.2f;

        [Tooltip("Tempo seguindo o X do player antes de travar o alvo.")]
        public float lockOnDelay = 0.7f;
        [Tooltip("Tempo que o indicador fica PARADO (alvo travado) antes da queda.")]
        public float lockOnFreeze = 0.6f;
        [Tooltip("Quão rápido o boss acompanha o X do player lá em cima.")]
        public float followXSharpness = 20f;

        [Tooltip("Força do impulso para baixo quando solta a queda.")]
        public float fallImpulse = 40f;
        [Tooltip("Gravidade durante a queda do meteoro.")]
        public float fallGravity = 10f;

        [Tooltip("Esconder o sprite do boss enquanto estiver fora da câmera.")]
        public bool hideWhileOffscreen = true;
        [Tooltip("Margem de viewport para considerar 'fora da câmera' (1 + margin).")]
        public float offscreenMargin = 0.08f;

        [Tooltip("Raio do impacto do meteoro.")]
        public float smashRadiusSuper = 3.0f;

        // ------------------- Mask -------------------
        [Header("Quem leva hit (LayerMask)")]
        public LayerMask hitMask = ~0;

        [Header("Identificação de Player (one-hit)")]
        [Tooltip("Layers considerados Player (além de tag 'Player').")]
        public LayerMask playerMask = 0;
    }

    [System.Serializable]
    public class Phase
    {
        public string phaseName = "Fase 1";
        [Tooltip("Ataques possíveis nesta fase.")]
        public List<Attack> attacks = new List<Attack>();
        [Tooltip("Delay mínimo entre ataques.")]
        public float minDelayBetween = 0.6f;
        [Tooltip("Delay máximo entre ataques.")]
        public float maxDelayBetween = 1.4f;

        [Header("Transição de fase (use um ou ambos)")]
        [Range(0, 1f)] public float hpThreshold = 0f;   // troca quando HP <= fração
        public float timeLimit = 0f;                    // troca depois de X segundos
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
    [Tooltip("Camadas consideradas chão para detectar aterrissagem.")]
    public LayerMask groundMask = ~0;
    [Tooltip("Distância extra para o boxcast de chão.")]
    public float groundProbeDistance = 0.15f;

    #endregion

    #region Internals
    private Rigidbody2D rb;
    private Collider2D col;
    private int phaseIndex = -1;
    private float phaseTimer = 0f;
    private bool dead = false;
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
    }

    private void Start()
    {
        BeginIntro();
    }

    private void OnEnable()
    {
        // vazio de propósito
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
        invulnerable = true;
        if (bossSprite) Flash(telegraphColor, introDuration);

        // movimento simples de entrada
        rb.linearVelocity = Vector2.left * speed;
        yield return new WaitForSeconds(introDuration);

        // parar e iniciar fase 0
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

            // Transições (evitar loop infinito na última fase)
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

            // Escolhe um ataque por peso
            var attack = PickWeighted(phase.attacks);
            if (attack == null) { yield return null; continue; }

            // Executa
            yield return ExecuteAttack(attack);

            // Delay entre ataques
            float delay = Random.Range(phase.minDelayBetween, phase.maxDelayBetween);
            yield return new WaitForSeconds(delay);
        }
    }

    private void TryAdvancePhase()
    {
        int next = phaseIndex + 1;
        if (next < phases.Count)
            GoToPhase(next);
        else
            phaseTimer = 0f; // última fase: não reinicia PhaseLoop
    }

    #endregion

    #region Ataques

    private IEnumerator ExecuteAttack(Attack a)
    {
        // Telegraph
        if (bossSprite) Flash(telegraphColor, a.windup);
        yield return new WaitForSeconds(a.windup);

        // Execução
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
        }

        // Recovery
        yield return new WaitForSeconds(a.recovery);
    }

    private IEnumerator Attack_Charge(Attack a)
    {
        FacePlayerX();

        float t = 0f;
        Vector2 dir = transform.localScale.x >= 0 ? Vector2.right : Vector2.left;

        while (t < a.dashDuration)
        {
            t += Time.deltaTime;
            rb.linearVelocity = new Vector2(dir.x * a.dashSpeed, rb.linearVelocity.y);

            // Hitbox (caixa)
            var hits = Physics2D.OverlapBoxAll(transform.position, a.chargeHitbox, 0f, a.hitMask);
            foreach (var h in hits)
            {
                ApplyPlayerHitIfAny(h.gameObject, a);
            }

            yield return null;
        }

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    // JumpSmash clássico — params separados
    private IEnumerator Attack_JumpSmash(Attack a)
    {
        float originalGravity = rb.gravityScale;

        // Sobe
        rb.gravityScale = a.gravityDuringJumpSmash;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * a.jumpForceSmash, ForceMode2D.Impulse);

        // Espera começar a cair
        while (rb.linearVelocity.y > -0.1f) yield return null;

        // Espera tocar o chão (robusto)
        yield return StartCoroutine(WaitUntilGrounded(0.4f));

        // Impacto
        var hits = Physics2D.OverlapCircleAll(transform.position, a.smashRadiusSmash, a.hitMask);
        foreach (var h in hits) ApplyPlayerHitIfAny(h.gameObject, a);

        // Restaura gravidade só DEPOIS de tocar o chão
        rb.gravityScale = originalGravity;

        if (bossSprite) Flash(Color.white, 0.15f);
    }

    // JumpSuperHigh (meteoro lock-on) — params separados
    private IEnumerator Attack_JumpSuperHigh(Attack a)
    {
        float originalGravity = rb.gravityScale;

        // 1) Sobe
        rb.gravityScale = a.riseGravitySuper;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(Vector2.up * a.jumpForceSuper, ForceMode2D.Impulse);

        // 2) Espera sair da câmera (acima do topo)
        yield return StartCoroutine(WaitUntilAboveCameraTop(a.offscreenMargin));

        // Manter fora da câmera e opcionalmente oculto
        float hoverY = rb.position.y;
        if (a.hideWhileOffscreen && bossSprite) bossSprite.enabled = false;

        // paira fora da câmera (sem gravidade)
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        // 3) Lock-on: seguir X do player por lockOnDelay, com indicador acompanhando
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

        // 4) Trava o alvo e congela indicador
        float dropX = player ? player.position.x : rb.position.x;
        float freezeT = 0f;
        while (freezeT < a.lockOnFreeze)
        {
            freezeT += Time.deltaTime;

            // boss continua fora da câmera, pairando
            rb.MovePosition(new Vector2(rb.position.x, hoverY));

            // indicador PARADO no X travado
            if (indicator)
            {
                float baseY = player ? player.position.y : (indicator.transform.position.y - a.indicatorYOffset);
                indicator.transform.position = new Vector3(dropX, baseY + a.indicatorYOffset, 0f);
            }

            yield return null;
        }

        // 5) Inicia a queda na posição travada (mantém sprite oculto até reentrar)
        transform.position = new Vector3(dropX, hoverY, transform.position.z);
        rb.gravityScale = a.fallGravity;   // queda forte
        rb.linearVelocity = Vector2.zero;
        rb.linearDamping = 0f;
        rb.AddForce(Vector2.down * a.fallImpulse, ForceMode2D.Impulse);

        // espera reentrar na câmera para mostrar o sprite
        yield return StartCoroutine(WaitUntilReenterCameraFromTop());
        if (bossSprite) bossSprite.enabled = true;

        // mantém X travado até tocar o chão
        while (!IsGrounded())
        {
            rb.MovePosition(new Vector2(dropX, rb.position.y));
            yield return new WaitForFixedUpdate();
        }

        // 6) Impacto (no chão)
        var hits = Physics2D.OverlapCircleAll(transform.position, a.smashRadiusSuper, a.hitMask);
        foreach (var h in hits) ApplyPlayerHitIfAny(h.gameObject, a);

        // destruir indicador SÓ depois do impacto
        // (deixa alguns frames pra VFX, se preferir: yield return null;)
        if (indicator) Destroy(indicator);

        // Restaura gravidade original APÓS aterrissar
        rb.gravityScale = originalGravity;

        if (bossSprite) Flash(Color.white, 0.15f);
    }

    #endregion

    #region Helpers (câmera / chão)

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
                // reentrou quando cruzar o topo (y <= 1)
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

        // se o player está atacando, NÃO mata aqui
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

    #endregion

    #region KillOnTouch

    private void TryKillPlayerOnContact(GameObject hit)
    {
        var playerRef = hit.GetComponent<Player>()
                        ?? hit.GetComponentInParent<Player>()
                        ?? hit.GetComponentInChildren<Player>();

        if (playerRef == null) return;

        // se o player está atacando, não mata
        if (playerRef.IsAttackActive) return;

        try { playerRef.Death(); }
        catch (Exception e) { Debug.LogError($"[Boss] Falha ao chamar Player.Death() no contato: {e.Message}"); }
    }

    private void OnCollisionEnter2D(Collision2D c)
    {
        TryKillPlayerOnContact(c.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
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
            else // JumpSuperHigh
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, atk.smashRadiusSuper);
            }
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
