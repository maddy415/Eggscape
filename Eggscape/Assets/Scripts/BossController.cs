using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Random = UnityEngine.Random;

/// <summary>
/// Boss 2D com fases, ataques e integração com sistema de cutscene/tutorial
/// AGORA COM SISTEMA DE EFEITOS VISUAIS/SONOROS NO WINDUP
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BossController : MonoBehaviour
{
    #region Tipos de dados (Inspector)

    public enum AttackType { Charge, JumpSmash, JumpSuperHigh, BulletHell }

    /// <summary>
    /// Configuração de efeitos visuais e sonoros para o windup de ataques
    /// </summary>
    [System.Serializable]
    public class WindupEffects
    {
        [Header("Audio")]
        [Tooltip("Som tocado no início do windup")]
        public AudioClip windupSound;
        [Range(0f, 1f)] public float windupSoundVolume = 1f;

        [Header("GameObject Activation")]
        [Tooltip("GameObject que será ativado durante o windup e desativado após")]
        public GameObject objectToActivate;
        
        [Tooltip("Se true, objeto será desativado após o windup. Se false, permanecerá ativo")]
        public bool deactivateAfterWindup = true;

        [Header("Visual Effects")]
        [Tooltip("Prefab de efeito visual a ser instanciado (ex: partículas de energia)")]
        public GameObject visualEffectPrefab;
        
        [Tooltip("Posição relativa ao boss onde spawnar o efeito (local space)")]
        public Vector3 effectSpawnOffset = Vector3.zero;
        
        [Tooltip("Anexar o efeito ao boss? (segue movimento)")]
        public bool attachEffectToBoss = true;

        [Header("Color Transition")]
        [Tooltip("Fazer transição de cor no sprite durante windup?")]
        public bool useColorTransition = false;
        
        [Tooltip("Cor alvo no final do windup (ex: vermelho claro)")]
        public Color targetColor = new Color(1f, 0.5f, 0.5f, 1f);
        
        [Tooltip("Curva de animação da cor (0=início, 1=fim do windup)")]
        public AnimationCurve colorCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Scale Pulse")]
        [Tooltip("Pulsar o tamanho do boss durante windup?")]
        public bool useScalePulse = false;
        
        [Tooltip("Multiplicador de escala máximo (ex: 1.1 = 10% maior)")]
        public float scalePulseAmount = 1.1f;
        
        [Tooltip("Velocidade da pulsação (ciclos por segundo)")]
        public float scalePulseSpeed = 4f;

        [Header("Squash (Amassamento Vertical)")]
        [Tooltip("Amassar o boss verticalmente durante windup? (bom para pulos)")]
        public bool useSquash = false;
        
        [Tooltip("Escala Y no ponto máximo do amassamento (ex: 0.6 = 40% mais baixo)")]
        [Range(0.3f, 1f)] public float squashScaleY = 0.7f;
        
        [Tooltip("Escala X quando amassado (ex: 1.3 = 30% mais largo para compensar)")]
        [Range(1f, 2f)] public float squashScaleX = 1.2f;
        
        [Tooltip("Curva de animação do squash (0=normal, 1=amassado máximo)")]
        public AnimationCurve squashCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Tooltip("Retornar à escala normal no final do windup? (false = mantém amassado)")]
        public bool releaseSquashAtEnd = false;

        [Header("Animation")]
        [Tooltip("Nome do trigger/bool no Animator para ativar")]
        public string animatorTrigger = "";
        
        [Tooltip("É um Bool? (senão, usa Trigger)")]
        public bool animatorIsBool = false;
        
        [Tooltip("Se Bool, setar para false após o windup?")]
        public bool resetBoolAfterWindup = true;

        [Header("Screen Shake")]
        [Tooltip("Intensidade do screen shake durante windup")]
        public float shakeIntensity = 0f;
        
        [Tooltip("Frequência do shake (maior = mais rápido)")]
        public float shakeFrequency = 10f;
    }

    [System.Serializable]
    public class Attack
    {
        public string name = "Charge";
        public AttackType type = AttackType.Charge;

        [Header("Telegraph/Timing")]
        public float windup = 0.5f;
        public float recovery = 0.5f;

        [Header("Windup Effects")]
        [Tooltip("Efeitos do windup principal (ou segundo windup no caso do Charge com âncora)")]
        public WindupEffects windupEffects = new WindupEffects();

        [Header("One-hit on touch")]
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

        [Header("Transição de fase")]
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
    public Player playerScript;
    public SpriteRenderer bossSprite;
    public Animator bossAnimator;
    public Color telegraphColor = Color.red;

    [Header("Effect Spawn Point")]
    [Tooltip("Transform filho para spawnar efeitos (se null, usa a posição do boss)")]
    public Transform effectSpawnPoint;

    [Header("Fases (na ordem)")]
    public List<Phase> phases = new List<Phase>();

    [Header("Intro")]
    public float introDuration = 1.2f;

    [Header("Ground Check")]
    public LayerMask groundMask = ~0;
    public float groundProbeDistance = 0.15f;

    [Header("Charge Anchor")]
    public bool useChargeAnchor = true;
    public Transform chargeAnchor;
    public Collider2D chargeAnchorTrigger;
    public bool requireAnchorTriggerForDash = true;
    public float chargeLeapUpForce = 10f;
    public float chargeLeapHorizSpeed = 6f;
    public float chargeAirAccel = 40f;
    public float chargePreDashHold = 0.35f;
    public float chargeAnchorXTolerance = 0.15f;

    [Header("Charge Cancel ao ser atingido")]
    public LayerMask playerAttackMask;
    public float dashCancelKnockback = 10f;
    public float dashCancelStun = 0.2f;

    [Header("Shockwave (JumpSuperHigh landing)")]
    public GameObject shockwavePrefab;
    public float shockwaveSpawnYOffset = 0.15f;
    public float shockwaveInitialSpeed = 12f;

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
    public ScythePool scythePool;
    
    [Header("Trilha Sonora")]
    public AudioClip bossTheme;
    public AudioClip phaseChangeTheme;
    public AudioClip deathTheme;

    [Header("Damage Feedback")]
    [Tooltip("Cor do flash quando toma dano")]
    public Color damageFlashColor = Color.white;
    [Tooltip("Duração do flash de dano (em segundos)")]
    public float damageFlashDuration = 0.15f;
    [Tooltip("Número de vezes que a cor pisca")]
    public int damageFlashBlinks = 2;

    [Header("Death Sequence")]
    [Tooltip("Animação de morte do boss (se usar Animator)")]
    public string deathAnimationTrigger = "Death";
    [Tooltip("Animação após o diálogo de morte")]
    public string postDialogueAnimationTrigger = "PostDeath";
    [Tooltip("Delay antes de iniciar o diálogo de morte")]
    public float delayBeforeDeathDialogue = 1f;
    [Tooltip("Diálogo que aparece quando o boss morre")]
    public List<DialogueSystem.DialogueLine> deathDialogue;
    [Tooltip("Nome ou índice da próxima cena (deixe vazio para próxima cena)")]
    public string nextSceneName = "";
    [Tooltip("Se true, usa nextSceneName. Se false, carrega próxima cena por índice")]
    public bool useSceneName = false;

    #endregion

    #region Internals
    private Rigidbody2D rb;
    private Collider2D col;
    private int phaseIndex = -1;
    private float phaseTimer = 0f;
    private bool dead = false;

    private bool anchorTouchedThisCharge = false;
    private bool isChargingDash = false;
    [HideInInspector] public bool dashWasCancelled = false;

    // Para controle de efeitos
    private GameObject currentWindupEffect;
    private Color originalSpriteColor;
    private Vector3 originalScale;
    private Coroutine windupEffectCoroutine;
    private WindupEffects currentWindupFX; // Armazena o windup atual para cleanup forçado
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

        // Captura cor e escala originais
        if (bossSprite)
        {
            originalSpriteColor = bossSprite.color;
            Debug.Log($"[Boss Awake] Cor original capturada: {originalSpriteColor}");
        }
        else
        {
            Debug.LogWarning("[Boss Awake] bossSprite está NULL! Cor não será alterada.");
        }
        
        originalScale = transform.localScale;
        Debug.Log($"[Boss Awake] Escala original capturada: {originalScale}");
    }

    private void Start()
    {
        if (playerScript == null && player != null)
        {
            playerScript = player.GetComponent<Player>();
        }
    }

    #endregion

    #region Inicialização

    public void StartBossFight()
    {
        if (dead) return;
        
        // ===== GARANTIA: RESETA COR E ESCALA ANTES DA LUTA =====
        if (bossSprite != null)
        {
            bossSprite.color = originalSpriteColor;
            Debug.Log($"[Boss Fight] Cor resetada para: {originalSpriteColor}");
        }
        transform.localScale = originalScale;
        Debug.Log($"[Boss Fight] Escala resetada para: {originalScale}");
        // =======================================================
        
        BeginIntro();
    }

    public void BeginIntro()
    {
        if (dead) return;
        StartCoroutine(IntroRoutine());
    }

    private IEnumerator IntroRoutine()
    {
        AudioManager.audioInstance.Crossfade(bossTheme, 1f);
        invulnerable = true;
        
        // Flash removido - não pisca mais na intro

        yield return new WaitForSeconds(introDuration);

        invulnerable = false;
        GoToPhase(0);
    }

    private void GoToPhase(int idx)
    {
        if (phases == null || phases.Count == 0)
        {
            Debug.LogWarning("Sem fases configuradas.");
            return;
        }
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

    #region Tutorial Dash

    public IEnumerator ExecuteTutorialDash(BossCutsceneManager cutsceneManager)
    {
        Attack chargeAttack = null;
        if (phases.Count > 0 && phases[0].attacks.Count > 0)
        {
            foreach (var atk in phases[0].attacks)
            {
                if (atk.type == AttackType.Charge)
                {
                    chargeAttack = atk;
                    break;
                }
            }
        }

        if (chargeAttack == null)
        {
            Debug.LogWarning("[Boss] Nenhum ataque Charge encontrado para tutorial!");
            yield break;
        }

        // Salva a cor ANTES do tutorial começar
        Color colorBeforeTutorial = bossSprite != null ? bossSprite.color : originalSpriteColor;
        Debug.Log($"[Boss Tutorial] Cor antes do tutorial: {colorBeforeTutorial}");

        float tutorialWindup = chargeAttack.windup * cutsceneManager.parryWindupMultiplier;

        // ===== INICIA EFEITOS DO WINDUP =====
        yield return StartCoroutine(PlayWindupEffects(chargeAttack.windupEffects, tutorialWindup));
        // ====================================

        FacePlayerX();
        Vector2 dashDir = transform.localScale.x >= 0 ? Vector2.right : Vector2.left;
        float elapsed = 0f;

        isChargingDash = true;
        dashWasCancelled = false;
        bool slowMotionTriggered = false;

        while (!dashWasCancelled)
        {
            elapsed += Time.deltaTime;

            float speedMultiplier = 1f;
            if (SlowMotionManager.Instance != null && SlowMotionManager.Instance.IsSlowMotionActive())
            {
                speedMultiplier = SlowMotionManager.Instance.slowMotionScale;
            }

            rb.linearVelocity = new Vector2(dashDir.x * chargeAttack.dashSpeed * speedMultiplier, rb.linearVelocity.y);

            if (!slowMotionTriggered && player != null)
            {
                float distance = Vector2.Distance(transform.position, player.position);

                if (distance <= cutsceneManager.promptTriggerDistance)
                {
                    slowMotionTriggered = true;
                    Debug.Log($"[Boss] Distância alcançada ({distance:F2}), triggando slow motion!");
                    
                    StartCoroutine(cutsceneManager.TriggerParrySlowMotion());
                }
            }

            var hits = Physics2D.OverlapBoxAll(transform.position, chargeAttack.chargeHitbox, 0f, chargeAttack.hitMask);
            if (hits.Length > 0)
            {
                foreach (var h in hits)
                {
                    if (h.CompareTag("Player") || (chargeAttack.playerMask.value & (1 << h.gameObject.layer)) != 0)
                    {
                        Debug.Log("[Boss] COLIDIU COM O PLAYER! Saindo do dash.");
                        ApplyPlayerHitIfAny(h.gameObject, chargeAttack);
                        dashWasCancelled = true;
                        break;
                    }
                }
            }

            yield return null;
        }

        isChargingDash = false;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        // ===== FORÇA RESTAURAÇÃO DA COR (GARANTIA SEGURA) =====
        // 1. Tenta restaurar para a cor original do jogo
        if (bossSprite != null)
        {
            bossSprite.color = originalSpriteColor;
            Debug.Log($"[Boss Tutorial] Cor restaurada para ORIGINAL: {originalSpriteColor}");
        }
        
        // 2. Garante que o GameObject de efeito foi destruído
        if (currentWindupEffect != null)
        {
            Destroy(currentWindupEffect);
            currentWindupEffect = null;
            Debug.Log("[Boss Tutorial] Efeito visual destruído manualmente");
        }
        
        // 3. Restaura escala original
        transform.localScale = originalScale;
        Debug.Log($"[Boss Tutorial] Escala restaurada para: {originalScale}");
        // ========================================================

        Debug.Log("[Boss] Tutorial dash finalizado!");
    }

    #endregion

    #region Sistema de Efeitos do Windup

    /// <summary>
    /// Executa todos os efeitos visuais/sonoros durante o windup
    /// </summary>
    private IEnumerator PlayWindupEffects(WindupEffects fx, float duration)
    {
        if (fx == null) yield break;

        // Armazena referência do windup atual para poder cancelar se necessário
        currentWindupFX = fx;

        // CRÍTICO: Sempre restaura para a cor ORIGINAL primeiro
        // Isso evita que o boss fique "preso" em uma cor de windup anterior
        if (bossSprite != null && fx.useColorTransition)
        {
            bossSprite.color = originalSpriteColor;
            Debug.Log($"[Boss Windup] Resetando cor para ORIGINAL antes do windup: {originalSpriteColor}");
        }

        // Salva a cor ATUAL (agora sempre será a original) para restaurar depois
        Color colorBeforeWindup = bossSprite != null ? bossSprite.color : Color.white;

        // Ativa GameObject específico
        if (fx.objectToActivate != null)
        {
            fx.objectToActivate.SetActive(true);
            Debug.Log($"[Boss Windup] Ativando objeto: {fx.objectToActivate.name}");
        }

        // Toca som
        if (fx.windupSound != null && AudioManager.audioInstance != null)
        {
            AudioSource sfxSource = AudioManager.audioInstance.GetComponent<AudioSource>();
            if (sfxSource != null)
            {
                sfxSource.PlayOneShot(fx.windupSound, fx.windupSoundVolume);
            }
        }

        // Spawna efeito visual
        if (fx.visualEffectPrefab != null)
        {
            Vector3 spawnPos = (effectSpawnPoint != null ? effectSpawnPoint.position : transform.position) + fx.effectSpawnOffset;
            
            currentWindupEffect = Instantiate(fx.visualEffectPrefab, spawnPos, Quaternion.identity);
            
            if (fx.attachEffectToBoss)
            {
                Transform parentTransform = effectSpawnPoint != null ? effectSpawnPoint : transform;
                currentWindupEffect.transform.SetParent(parentTransform);
                currentWindupEffect.transform.localPosition = fx.effectSpawnOffset;
            }
            
            // Garante que está ativo
            currentWindupEffect.SetActive(true);
        }

        // Ativa trigger do Animator
        if (!string.IsNullOrEmpty(fx.animatorTrigger) && bossAnimator != null)
        {
            if (fx.animatorIsBool)
            {
                bossAnimator.SetBool(fx.animatorTrigger, true);
            }
            else
            {
                bossAnimator.SetTrigger(fx.animatorTrigger);
            }
        }

        // Executa animações durante o windup
        float elapsed = 0f;
        
        Debug.Log($"[Boss Windup] Iniciando loop de animação. Duration: {duration}s");
        Debug.Log($"[Boss Windup] useColorTransition: {fx.useColorTransition}, useSquash: {fx.useSquash}, useScalePulse: {fx.useScalePulse}");
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Transição de cor
            if (fx.useColorTransition && bossSprite != null)
            {
                float curveValue = fx.colorCurve.Evaluate(t);
                Color newColor = Color.Lerp(originalSpriteColor, fx.targetColor, curveValue);
                bossSprite.color = newColor;
                
                // Log apenas a cada 0.1 segundos para não encher o console
                if (Mathf.Abs(elapsed % 0.1f) < Time.deltaTime)
                {
                    Debug.Log($"[Boss Windup] t={t:F2}, curveValue={curveValue:F2}, cor={newColor}");
                }
            }

            // Pulse de escala (NÃO COMBINAR COM SQUASH!)
            if (fx.useScalePulse && !fx.useSquash)
            {
                float pulse = Mathf.Sin(elapsed * fx.scalePulseSpeed * Mathf.PI * 2f) * 0.5f + 0.5f;
                float scaleMult = Mathf.Lerp(1f, fx.scalePulseAmount, pulse * t);
                transform.localScale = originalScale * scaleMult;
            }

            // Squash (Amassamento Vertical)
            if (fx.useSquash)
            {
                float squashAmount = fx.squashCurve.Evaluate(t);
                
                // Interpola entre escala normal e escala amassada
                float scaleY = Mathf.Lerp(1f, fx.squashScaleY, squashAmount);
                float scaleX = Mathf.Lerp(1f, fx.squashScaleX, squashAmount);
                
                Vector3 squashedScale = originalScale;
                squashedScale.x *= scaleX;
                squashedScale.y *= scaleY;
                
                transform.localScale = squashedScale;
                
                // Log apenas a cada 0.1 segundos
                if (Mathf.Abs(elapsed % 0.1f) < Time.deltaTime)
                {
                    Debug.Log($"[Boss Windup] t={t:F2}, squashAmount={squashAmount:F2}, scale={squashedScale}");
                }
            }

            // Screen shake
            if (fx.shakeIntensity > 0f)
            {
                ApplyScreenShake(fx.shakeIntensity, fx.shakeFrequency);
            }

            yield return null;
        }

        Debug.Log("[Boss Windup] Loop de animação completo!");

        // Se releaseSquashAtEnd estiver ativo, retorna à escala normal
        if (fx.useSquash && fx.releaseSquashAtEnd)
        {
            transform.localScale = originalScale;
            Debug.Log($"[Boss Windup] Restaurando escala para: {originalScale}");
        }

        // LIMPA EFEITOS IMEDIATAMENTE APÓS O WINDUP
        Debug.Log("[Boss Windup] Windup completo, limpando efeitos...");
        CleanupWindupEffects(fx, colorBeforeWindup);
        
        // Limpa referência
        currentWindupFX = null;
    }

    /// <summary>
    /// Remove todos os efeitos visuais criados no windup
    /// </summary>
    private void CleanupWindupEffects(WindupEffects fx, Color colorToRestore)
    {
        if (fx == null) return;

        // Desativa GameObject específico
        if (fx.objectToActivate != null && fx.deactivateAfterWindup)
        {
            Debug.Log($"[Boss Windup] Desativando objeto: {fx.objectToActivate.name}");
            fx.objectToActivate.SetActive(false);
        }

        // Destrói efeito visual instanciado
        if (currentWindupEffect != null)
        {
            // Se for um sistema de partículas, para a emissão antes de destruir
            ParticleSystem ps = currentWindupEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            
            Debug.Log($"[Boss Windup] Destruindo efeito visual: {currentWindupEffect.name}");
            Destroy(currentWindupEffect);
            currentWindupEffect = null;
        }

        // SEMPRE restaura para a cor ORIGINAL (não a cor "anterior")
        // Isso garante que o boss nunca fique preso em vermelho
        if (fx.useColorTransition && bossSprite != null)
        {
            bossSprite.color = originalSpriteColor;
            Debug.Log($"[Boss Windup] Restaurando cor para ORIGINAL: {originalSpriteColor}");
        }

        // Restaura escala original (sempre restaura no cleanup)
        if (fx.useScalePulse || fx.useSquash)
        {
            transform.localScale = originalScale;
        }

        // Reseta bool do Animator
        if (fx.animatorIsBool && fx.resetBoolAfterWindup && !string.IsNullOrEmpty(fx.animatorTrigger) && bossAnimator != null)
        {
            bossAnimator.SetBool(fx.animatorTrigger, false);
        }
    }

    /// <summary>
    /// Aplica screen shake na câmera
    /// </summary>
    private void ApplyScreenShake(float intensity, float frequency)
    {
        var cam = Camera.main;
        if (cam == null) return;

        float x = Mathf.PerlinNoise(Time.time * frequency, 0f) * 2f - 1f;
        float y = Mathf.PerlinNoise(0f, Time.time * frequency) * 2f - 1f;

        cam.transform.position += new Vector3(x, y, 0f) * intensity * Time.deltaTime;
    }

    #endregion

    #region Ataques

    private IEnumerator ExecuteAttack(Attack a)
    {
        // ===== EXECUTA EFEITOS DO WINDUP =====
        // NOTA: Não use Flash() aqui, pois conflita com windupEffects.useColorTransition
        // Se você quiser o flash antigo, desative useColorTransition no windupEffects
        yield return StartCoroutine(PlayWindupEffects(a.windupEffects, a.windup));
        // =====================================

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

        if (absDx0 <= chargeAnchorXTolerance)
        {
            yield return StartCoroutine(WaitUntilGrounded(0.8f));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            rb.linearVelocity = Vector2.zero;

            FacePlayerX();
            
            // ===== EFEITOS DO SEGUNDO WINDUP (após pousar na âncora) =====
            if (chargePreDashHold > 0f)
            {
                yield return StartCoroutine(PlayWindupEffects(a.windupEffects, chargePreDashHold));
            }
            // ==============================================================

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
        
        // ===== EFEITOS DO SEGUNDO WINDUP (após pousar na âncora) =====
        if (chargePreDashHold > 0f)
        {
            yield return StartCoroutine(PlayWindupEffects(a.windupEffects, chargePreDashHold));
        }
        // ==============================================================

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

    #region Helpers

    private void SpawnBullets(List<Vector2> dirs, BulletPatternSO so)
    {
        if (dirs == null || dirs.Count == 0) return;

        float baseY = (col != null ? col.bounds.min.y : transform.position.y) + bulletSpawnYOffset;
        Vector3 spawnPos = new Vector3(transform.position.x, baseY, 0f);

        foreach (var d in dirs)
        {
            if (scythePool != null)
            {
                var sc = scythePool.Spawn(spawnPos, d, so.bulletSpeed, so.bulletLifeTime, bulletHitMask);
                sc.Initialize(d, so.bulletSpeed, so.bulletLifeTime, bulletHitMask);
                continue;
            }

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
        Debug.Log($"[Boss] TakeDamage chamado! Amount: {amount}, Dead: {dead}, Invulnerable: {invulnerable}");
        
        if (dead || invulnerable)
        {
            Debug.Log("[Boss] Dano ignorado (morto ou invulnerável)");
            return;
        }
        
        currentHealth = Mathf.Max(0, currentHealth - Mathf.Abs(amount));
        Debug.Log($"[Boss] HP: {currentHealth}/{maxHealth}");
        
        // Flash de dano
        if (bossSprite != null && damageFlashDuration > 0f)
        {
            Debug.Log("[Boss] Iniciando damage flash!");
            StartCoroutine(DamageFlash());
        }
        else
        {
            if (bossSprite == null) Debug.LogError("[Boss] bossSprite está NULL, não pode fazer flash!");
            if (damageFlashDuration <= 0f) Debug.LogError("[Boss] damageFlashDuration está 0 ou negativo!");
        }
        
        if (currentHealth <= 0) Die();
    }

    /// <summary>
    /// Método de teste público para verificar se o flash funciona
    /// </summary>
    [ContextMenu("Test Damage Flash")]
    public void TestDamageFlash()
    {
        Debug.Log("[Boss TEST] Testando damage flash manualmente...");
        if (bossSprite == null)
        {
            Debug.LogError("[Boss TEST] bossSprite está NULL!");
            return;
        }
        StartCoroutine(DamageFlash());
    }

    /// <summary>
    /// Efeito de flash rápido quando o boss toma dano
    /// </summary>
    private IEnumerator DamageFlash()
    {
        if (bossSprite == null)
        {
            Debug.LogWarning("[Boss Flash] bossSprite é NULL!");
            yield break;
        }

        // CRÍTICO: Usa a cor ORIGINAL como base do flash
        // Não importa se o boss está vermelho, verde, etc - sempre pisca da ORIGINAL
        Color colorBeforeFlash = originalSpriteColor;
        Debug.Log($"[Boss Flash] Iniciando! Cor base (ORIGINAL): {colorBeforeFlash}, cor de dano: {damageFlashColor}, blinks: {damageFlashBlinks}");
        
        float blinkTime = damageFlashDuration / (damageFlashBlinks * 2f);
        Debug.Log($"[Boss Flash] Tempo de cada blink: {blinkTime}s");
        
        for (int i = 0; i < damageFlashBlinks; i++)
        {
            // Pisca para a cor de dano
            bossSprite.color = damageFlashColor;
            Debug.Log($"[Boss Flash] Blink {i+1}/{damageFlashBlinks} - Mudando para cor de dano");
            yield return new WaitForSeconds(blinkTime);
            
            // Volta para a cor ORIGINAL (não a cor que estava antes!)
            bossSprite.color = colorBeforeFlash;
            Debug.Log($"[Boss Flash] Blink {i+1}/{damageFlashBlinks} - Voltando para cor ORIGINAL");
            yield return new WaitForSeconds(blinkTime);
        }
        
        // Garante que termina na cor ORIGINAL
        bossSprite.color = colorBeforeFlash;
        Debug.Log($"[Boss Flash] Flash de dano completo! Cor final: {colorBeforeFlash}");
    }

    private void Die()
    {
        if (dead) return; // Evita múltiplas chamadas
        
        dead = true;
        StopAllCoroutines();
        rb.linearVelocity = Vector2.zero;
        
        // NÃO desativa o GameObject - inicia sequência de morte
        StartCoroutine(DeathSequence());
    }

    /// <summary>
    /// Sequência completa de morte: animação → diálogo → animação final → transição
    /// </summary>
    private IEnumerator DeathSequence()
    {
        Debug.Log("[Boss] Iniciando sequência de morte...");

        // Toca música de morte
        if (deathTheme != null && AudioManager.audioInstance != null)
        {
            AudioManager.audioInstance.Crossfade(deathTheme, 1f);
        }

        // Desabilita controles do player
        if (playerScript != null)
        {
            playerScript.CanMove = false;
            playerScript.canAttack = false;
        }

        // Toca animação de morte
        if (bossAnimator != null && !string.IsNullOrEmpty(deathAnimationTrigger))
        {
            bossAnimator.SetTrigger(deathAnimationTrigger);
            Debug.Log($"[Boss] Tocando animação: {deathAnimationTrigger}");
        }

        // Delay antes do diálogo
        yield return new WaitForSeconds(delayBeforeDeathDialogue);

        // Inicia diálogo de morte (se existir)
        if (deathDialogue != null && deathDialogue.Count > 0 && DialogueSystem.Instance != null)
        {
            Debug.Log("[Boss] Iniciando diálogo de morte...");
            bool dialogueDone = false;
            DialogueSystem.Instance.StartDialogue(deathDialogue, () => dialogueDone = true);
            
            // Aguarda o diálogo terminar
            yield return new WaitUntil(() => dialogueDone);
            Debug.Log("[Boss] Diálogo de morte concluído!");
        }

        // Toca animação pós-diálogo (se configurada)
        if (bossAnimator != null && !string.IsNullOrEmpty(postDialogueAnimationTrigger))
        {
            bossAnimator.SetTrigger(postDialogueAnimationTrigger);
            Debug.Log($"[Boss] Tocando animação pós-diálogo: {postDialogueAnimationTrigger}");
            yield return new WaitForSeconds(1f); // Tempo para a animação começar
        }

        // Transição de cena com fade
        LoadNextScene();
    }

    /// <summary>
    /// Carrega a próxima cena com fade (usando SceneTransition)
    /// </summary>
    private void LoadNextScene()
    {
        Debug.Log("[Boss] Carregando próxima cena...");

        if (SceneTransition.Instance != null)
        {
            if (useSceneName && !string.IsNullOrEmpty(nextSceneName))
            {
                Debug.Log($"[Boss] Carregando cena: {nextSceneName}");
                SceneTransition.Instance.LoadScene(nextSceneName);
            }
            else
            {
                int currentIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
                int nextIndex = currentIndex + 1;
                
                if (nextIndex < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings)
                {
                    Debug.Log($"[Boss] Carregando próxima cena (índice {nextIndex})");
                    SceneTransition.Instance.LoadScene(nextIndex);
                }
                else
                {
                    Debug.LogWarning("[Boss] Não há próxima cena! Voltando para o menu principal...");
                    SceneTransition.Instance.LoadScene(0); // Volta para a primeira cena (geralmente menu)
                }
            }
        }
        else
        {
            Debug.LogError("[Boss] SceneTransition não encontrado! Carregando cena sem fade.");
            
            if (useSceneName && !string.IsNullOrEmpty(nextSceneName))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                int currentIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
                int nextIndex = currentIndex + 1;
                UnityEngine.SceneManagement.SceneManager.LoadScene(nextIndex);
            }
        }
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
            
            // ===== LIMPA EFEITOS DO WINDUP IMEDIATAMENTE =====
            // Força limpeza dos efeitos visuais antes de aplicar dano
            if (currentWindupFX != null)
            {
                Debug.Log("[Boss] Dash cancelado! Limpando efeitos do windup...");
                CleanupWindupEffects(currentWindupFX, originalSpriteColor);
                currentWindupFX = null;
            }
            // =================================================
            
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
            else
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