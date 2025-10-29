using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


/// <summary>
/// Template simples de chefe para fase 1x1 (Player vs Boss).
/// - 1 único script controla: vida, fases, seleção de ataques, execução e morte.
/// - Ataques embutidos (Charge e JumpSmash) para começar rápido.
/// - Extensível: adicione novos ataques só criando um método IEnumerator NovoAtaque().
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BossSimpleController : MonoBehaviour
{
    #region Tipos de dados simples (pra configurar tudo no inspetor)

    public enum AttackType { Charge, JumpSmash }

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

        [Header("Pesagem p/ sorteio")]
        [Tooltip("Quanto maior, mais chances de sair este ataque.")]
        public int weight = 1;

        [Header("Charge params")]
        public float dashSpeed = 12f;
        public float dashDuration = 0.6f;
        public Vector2 chargeHitbox = new Vector2(1.2f, 1.0f);

        [Header("JumpSmash params")]
        public float jumpForce = 12f;
        public float gravityDuringJump = 2.2f;
        public float smashRadius = 3.0f;

        [Header("Dano")]
        public float damage = 15f;

        [Header("Quem leva dano (LayerMask)")]
        public LayerMask hitMask = ~0;
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
    [ReadOnly] public float currentHealth; // só leitura no inspetor
    public bool invulnerable = false;

    [Header("Alvos / Refs")]
    public float speed;
    public Transform player;
    public SpriteRenderer bossSprite;   // opcional (pra piscada)
    public Color telegraphColor = Color.red;

    [Header("Fases (na ordem)")]
    public List<Phase> phases = new List<Phase>();
    
    [Header("Intro")]
    public float introDuration = 1.2f;

    #endregion

    #region Internals

    private Rigidbody2D rb;
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
        currentHealth = maxHealth;
    }

    private void OnEnable()
    {
        // Se quiser iniciar automaticamente, chame BeginIntro() via diretor da cena.
        // Aqui, por simplicidade, vamos já começar:
        BeginIntro();
    }

    #endregion

    #region Ciclo da luta

    public void BeginIntro()
    {
        if (dead) return;
        StopAllCoroutines();
        StartCoroutine(IntroRoutine());
    }

    private IEnumerator IntroRoutine()
    {
        rb.linearVelocity = Vector2.left * speed * Time.deltaTime;

        invulnerable = true;
        Flash(telegraphColor, introDuration);
        yield return new WaitForSeconds(introDuration);
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

            // Transições de fase (tempo/HP) — verificadas a cada loop
            phaseTimer += Time.deltaTime;
            float hpFrac = currentHealth / maxHealth;
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
            GoToPhase(phaseIndex); // fica na última (ou adapte para "Enrage")
    }

    #endregion

    #region Ataques (inline)

    private IEnumerator ExecuteAttack(Attack a)
    {
        // TELEGRAPH
        if (bossSprite) Flash(telegraphColor, a.windup);
        yield return new WaitForSeconds(a.windup);

        // Decide e executa
        switch (a.type)
        {
            case AttackType.Charge:
                yield return Attack_Charge(a);
                break;
            case AttackType.JumpSmash:
                yield return Attack_JumpSmash(a);
                break;
        }

        // Recuperação
        yield return new WaitForSeconds(a.recovery);
    }

    private IEnumerator Attack_Charge(Attack a)
    {
        // Direção simples: vira para o player antes de avançar
        FacePlayerX();

        float t = 0f;
        Vector2 dir = transform.localScale.x >= 0 ? Vector2.right : Vector2.left;

        while (t < a.dashDuration)
        {
            t += Time.deltaTime;
            rb.linearVelocity = new Vector2(dir.x * a.dashSpeed, rb.linearVelocity.y);

            // Hitbox simples por OverlapBox
            var hits = Physics2D.OverlapBoxAll(transform.position, a.chargeHitbox, 0f, a.hitMask);
            foreach (var h in hits)
            {
                ApplyDamageIfDamageable(h.gameObject, a.damage, (Vector2)h.transform.position, -dir);
            }

            yield return null;
        }

        // Para horizontalmente no fim
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private IEnumerator Attack_JumpSmash(Attack a)
    {
        float originalGravity = rb.gravityScale;

        // Sobe
        rb.gravityScale = a.gravityDuringJump;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * a.jumpForce, ForceMode2D.Impulse);

        // Espera o pico e a queda (checa quando velocidade de Y ficar negativa)
        while (rb.linearVelocity.y > -0.1f) yield return null;

        // Pequeno buffer pra garantir contato com chão da arena
        yield return new WaitForSeconds(0.08f);

        // Smash: dano em área
        var hits = Physics2D.OverlapCircleAll(transform.position, a.smashRadius, a.hitMask);
        foreach (var h in hits)
        {
            Vector2 dir = (h.transform.position - transform.position).normalized;
            ApplyDamageIfDamageable(h.gameObject, a.damage, (Vector2)h.transform.position, dir);
        }

        // Restauro gravidade
        rb.gravityScale = originalGravity;

        // (Opcional) micro feedback visual
        if (bossSprite) Flash(Color.white, 0.15f);
    }

    #endregion

    #region Dano / Morte

    public void TakeDamage(float amount)
    {
        if (dead || invulnerable) return;
        currentHealth = Mathf.Max(0, currentHealth - Mathf.Abs(amount));
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void ApplyDamageIfDamageable(GameObject target, float dmg, Vector2 hitPoint, Vector2 hitNormal)
    {
        // Interface opcional: se seu player já usa um script com esse método, adapte aqui.
        // Aqui faremos: se tiver um componente "IDamageableLike" genérico, chamamos; senão, tenta achar um método padrão.
        // Para simplificar, vamos procurar um método público "TakeDamage(float)".
        var comp = target.GetComponent<MonoBehaviour>();
        if (comp != null)
        {
            var m = comp.GetType().GetMethod("TakeDamage", new System.Type[] { typeof(float) });
            if (m != null)
            {
                m.Invoke(comp, new object[] { dmg });
            }
        }
    }

    private void Die()
    {
        dead = true;
        StopAllCoroutines();
        rb.linearVelocity = Vector2.zero;
        // TODO: VFX/SFX de morte, liberar saída, sinalizar diretor etc.
        gameObject.SetActive(false);
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

    #region Gizmos (debug)

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Visual aproximado das áreas de ataque com base na fase atual (apenas debug).
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
                Gizmos.DrawWireSphere(transform.position, atk.smashRadius);
            }
        }
    }
#endif

    #endregion
}

/// <summary>
/// Atributo só pra mostrar campo como ReadOnly no inspetor (cosmético).
/// </summary>
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
