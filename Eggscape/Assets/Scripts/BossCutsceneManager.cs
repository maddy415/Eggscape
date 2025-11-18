using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerencia a cutscene inicial e tutorial de parry com slow motion individual
/// </summary>
public class BossCutsceneManager : MonoBehaviour
{
    [Header("References")]
    public BossController boss;
    public Player player;
    public Transform bossSpawnPoint;

    [Header("Chicken Walk Settings")]
    public float chickenWalkDuration = 3f;
    public float chickenWalkSpeed = 2f;

    [Header("Boss Entrance")]
    public float bossEntranceForce = 15f;
    public Vector2 bossEntranceDirection = new Vector2(-1f, 0.5f);

    [Header("Tutorial Parry")]
    public GameObject parryPrompt;
    public float parryWindupMultiplier = 2f;
    public float promptTriggerDistance = 3f;
    public float parryTimeWindow = 2f;

    [Header("Dialogues")]
    public List<DialogueSystem.DialogueLine> chickenIntroDialogue;
    public List<DialogueSystem.DialogueLine> afterBossAppearDialogue;
    public List<DialogueSystem.DialogueLine> afterParryDialogue;

    [Header("Camera Shake")]
    public float shakeIntensity = 0.3f;
    public float shakeDuration = 0.5f;

    [Header("Delay Before After Parry Dialogue")]
    public float delayBeforeAfterParryDialogue = 0.5f;

    private bool cutsceneComplete = false;
    public bool parryTutorialComplete = false;
    private bool parrySuccessful = false;
    private Rigidbody2D bossRb;
    private bool bossWasHit = false;

    private void Start()
    {
        if (boss == null) boss = FindFirstObjectByType<BossController>();
        if (player == null) player = FindFirstObjectByType<Player>();

        if (parryPrompt) parryPrompt.SetActive(false);

        StartCoroutine(InitialCutscene());
    }

    private IEnumerator InitialCutscene()
    {
        if (player) player.CanMove = false;
        if (player) player.canAttack = false;


        if (boss)
        {
            boss.enabled = false;
            boss.gameObject.SetActive(false);
            bossRb = boss.GetComponent<Rigidbody2D>();
        }

        yield return StartCoroutine(ChickenAutoWalk());

        if (chickenIntroDialogue != null && chickenIntroDialogue.Count > 0)
        {
            bool dialogueDone = false;
            DialogueSystem.Instance.StartDialogue(chickenIntroDialogue, () => dialogueDone = true);
            yield return new WaitUntil(() => dialogueDone);
        }

        yield return StartCoroutine(BossEntrance());

        if (afterBossAppearDialogue != null && afterBossAppearDialogue.Count > 0)
        {
            bool dialogueDone = false;
            DialogueSystem.Instance.StartDialogue(afterBossAppearDialogue, () => dialogueDone = true);
            yield return new WaitUntil(() => dialogueDone);
        }

        if (player) player.canAttack = true;


        yield return StartCoroutine(ParryTutorial());

        yield return new WaitForSeconds(delayBeforeAfterParryDialogue);

        if (afterParryDialogue != null && afterParryDialogue.Count > 0)
        {
            bool dialogueDone = false;
            DialogueSystem.Instance.StartDialogue(afterParryDialogue, () => dialogueDone = true);
            yield return new WaitUntil(() => dialogueDone);
        }

        cutsceneComplete = true;
        if (player) player.CanMove = true;
        if (boss)
        {
            boss.enabled = true;
            if (player) player.CanMove = true;
            boss.StartBossFight();
        }
    }

    private IEnumerator ChickenAutoWalk()
    {
        if (!player) yield break;

        float elapsed = 0f;
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();

        while (elapsed < chickenWalkDuration)
        {
            elapsed += Time.deltaTime;

            if (playerRb)
            {
                playerRb.linearVelocity = new Vector2(chickenWalkSpeed, playerRb.linearVelocity.y);
            }

            yield return null;
        }

        if (playerRb)
        {
            playerRb.linearVelocity = new Vector2(0f, playerRb.linearVelocity.y);
        }
    }

    private IEnumerator BossEntrance()
    {
        if (!boss) yield break;

        if (bossSpawnPoint)
        {
            boss.transform.position = bossSpawnPoint.position;
        }

        boss.gameObject.SetActive(true);

        if (bossRb)
        {
            bossRb.linearVelocity = Vector2.zero;
            Vector2 force = bossEntranceDirection.normalized * bossEntranceForce;
            bossRb.AddForce(force, ForceMode2D.Impulse);
        }

        if (shakeIntensity > 0f)
        {
            StartCoroutine(CameraShake());
        }

        yield return new WaitForSeconds(1f);
    }

    public IEnumerator ParryTutorial()
    {
        if (!boss || !player) yield break;

        parryTutorialComplete = false;
        parrySuccessful = false;

        boss.enabled = true;
        StartCoroutine(boss.ExecuteTutorialDash(this));

        yield return new WaitUntil(() => parryTutorialComplete);
    }

   public IEnumerator TriggerParrySlowMotion()
{
    Debug.Log("[Cutscene] SLOW MOTION ATIVADO!");

    if (SlowMotionManager.Instance != null)
    {
        SlowMotionManager.Instance.ActivateSlowMotion();
    }

    if (parryPrompt) parryPrompt.SetActive(true);

    float elapsed = 0f;
    bool attackDetected = false;
    bossWasHit = false; // Reset

    while (elapsed < parryTimeWindow && !attackDetected)
    {
        elapsed += Time.deltaTime;

        // Detecta INPUT de ataque
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("[Cutscene] Input de ataque detectado, aguardando colisão...");
        }

        // SÓ CONTA COMO PARRY se o BOSS FOI ATINGIDO
        if (bossWasHit)
        {
            Debug.Log("[Cutscene] PARRY BEM-SUCEDIDO! Boss foi atingido!");
            attackDetected = true;
            parrySuccessful = true;

            // DESATIVA SLOW MOTION
            if (SlowMotionManager.Instance != null)
            {
                SlowMotionManager.Instance.DeactivateSlowMotion();
            }

            if (parryPrompt) parryPrompt.SetActive(false);

            // CANCELA O DASH
            if (boss != null)
            {
                boss.dashWasCancelled = true;
            }

            // Espera alguns frames
            yield return null;
            yield return null;
            yield return null;

            // APLICA KNOCKBACK NO BOSS
            if (boss != null)
            {
                Debug.Log("[Cutscene] Aplicando knockback no boss...");
                
                Rigidbody2D bossRb = boss.GetComponent<Rigidbody2D>();
                if (bossRb != null)
                {
                    float dir = Mathf.Sign(boss.transform.position.x - player.transform.position.x);
                    if (dir == 0f) dir = (boss.transform.localScale.x >= 0) ? -1f : 1f;
                    
                    bossRb.linearVelocity = Vector2.zero;
                    bossRb.AddForce(new Vector2(dir * boss.dashCancelKnockback, 0f), ForceMode2D.Impulse);
                    
                    Debug.Log($"[Cutscene] Knockback aplicado! Direção: {dir}, Força: {boss.dashCancelKnockback}");
                }
            }

            // TOCA SOM DE PARRY
            if (AudioManager.audioInstance != null)
            {
                AudioManager.audioInstance.BossParrySFX();
                Debug.Log("[Cutscene] Som de parry tocado!");
            }

            break;
        }

        yield return null;
    }

    if (!attackDetected)
    {
        Debug.Log("[Cutscene] Tempo esgotado - parry falhou!");
        
        if (SlowMotionManager.Instance != null)
        {
            SlowMotionManager.Instance.DeactivateSlowMotion();
        }

        if (parryPrompt) parryPrompt.SetActive(false);
    }

    parryTutorialComplete = true;
}

    private IEnumerator CameraShake()
    {
        var cam = Camera.main;
        if (!cam) yield break;

        Vector3 originalPos = cam.transform.position;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;

            float x = Random.Range(-1f, 1f) * shakeIntensity;
            float y = Random.Range(-1f, 1f) * shakeIntensity;

            cam.transform.position = originalPos + new Vector3(x, y, 0f);

            yield return null;
        }

        cam.transform.position = originalPos;
    }
    
    public void OnBossHitByPlayer()
    {
        bossWasHit = true;
        Debug.Log("[Cutscene] Boss foi atingido pelo player!");
    }

    public bool IsCutsceneComplete()
    {
        return cutsceneComplete;
    }

    public bool WasParrySuccessful()
    {
        return parrySuccessful;
    }
}