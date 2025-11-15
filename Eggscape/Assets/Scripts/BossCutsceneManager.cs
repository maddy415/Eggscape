using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerencia a cutscene inicial e o tutorial de parry do boss
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
    public float slowMotionScale = 0.15f;
    public float slowMotionDuration = 2f;
    public float promptTriggerDistance = 3f; // Distância para triggar o slow motion

    [Header("Dialogues")]
    public List<DialogueSystem.DialogueLine> chickenIntroDialogue;
    public List<DialogueSystem.DialogueLine> afterBossAppearDialogue;
    public List<DialogueSystem.DialogueLine> afterParryDialogue;

    [Header("Camera Shake (Optional)")]
    public float shakeIntensity = 0.3f;
    public float shakeDuration = 0.5f;

    private bool cutsceneComplete = false;
    private bool parryTutorialComplete = false;
    private Rigidbody2D bossRb;

    private void Start()
    {
        if (boss == null) boss = FindFirstObjectByType<BossController>();
        if (player == null) player = FindFirstObjectByType<Player>();

        if (parryPrompt) parryPrompt.SetActive(false);

        StartCoroutine(InitialCutscene());
    }

    private IEnumerator InitialCutscene()
    {
        // Desativa controle do player
        if (player) player.enabled = false;

        // Boss começa invisível/inativo
        if (boss)
        {
            boss.enabled = false;
            boss.gameObject.SetActive(false);
            bossRb = boss.GetComponent<Rigidbody2D>();
        }

        // 1. Chicken anda sozinha
        yield return StartCoroutine(ChickenAutoWalk());

        // 2. Diálogos da Chicken
        if (chickenIntroDialogue != null && chickenIntroDialogue.Count > 0)
        {
            bool dialogueDone = false;
            DialogueSystem.Instance.StartDialogue(chickenIntroDialogue, () => dialogueDone = true);
            yield return new WaitUntil(() => dialogueDone);
        }

        // 3. Boss aparece (Eurell entrance)
        yield return StartCoroutine(BossEntrance());

        // 4. Diálogos após boss aparecer
        if (afterBossAppearDialogue != null && afterBossAppearDialogue.Count > 0)
        {
            bool dialogueDone = false;
            DialogueSystem.Instance.StartDialogue(afterBossAppearDialogue, () => dialogueDone = true);
            yield return new WaitUntil(() => dialogueDone);
        }

        // 5. Tutorial de Parry (primeiro dash)
        // 6. Diálogos após parry
        if (afterParryDialogue != null && afterParryDialogue.Count > 0)
        {
            bool dialogueDone = false;
            DialogueSystem.Instance.StartDialogue(afterParryDialogue, () => dialogueDone = true);
            yield return new WaitUntil(() => dialogueDone);
        }

        // 7. Inicia a batalha normal
        cutsceneComplete = true;
        if (player) player.enabled = true;
        if (boss)
        {
            boss.enabled = true;
            boss.StartBossFight(); // Método que vamos adicionar no BossController
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

        // Posiciona boss no spawn point
        if (bossSpawnPoint)
        {
            boss.transform.position = bossSpawnPoint.position;
        }

        boss.gameObject.SetActive(true);

        // Aplica força de entrada
        if (bossRb)
        {
            bossRb.linearVelocity = Vector2.zero;
            Vector2 force = bossEntranceDirection.normalized * bossEntranceForce;
            bossRb.AddForce(force, ForceMode2D.Impulse);
        }

        // Camera shake opcional
        if (shakeIntensity > 0f)
        {
            StartCoroutine(CameraShake());
        }

        // Espera o boss pousar
        yield return new WaitForSeconds(1f);
    }

    private bool parryHitConfirmed = false; // Nova flag

    

    /// <summary>
    /// Chamado pelo Boss quando ele levar hit durante o tutorial
    /// </summary>
    public void ConfirmParryHit()
    {
        parryHitConfirmed = true;
        Debug.Log("[Cutscene] ConfirmParryHit() - Boss levou o hit!");
    }

    /// <summary>
    /// Chamado pelo BossController quando o dash está próximo do player
    /// </summary>
    public IEnumerator TriggerParryPrompt()
    {
        Debug.Log("[Cutscene] Slow motion ATIVADO!");
        
        // Ativa slow motion
        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // REMOVE TODAS AS RESTRIÇÕES DO PLAYER
        if (player)
        {
            player.CanMove = true;
            player.canJump = true;
            player.canAttack = true;
            Debug.Log("[Cutscene] TODAS restrições removidas! Player totalmente livre!");
        }

        // Mostra prompt
        if (parryPrompt) parryPrompt.SetActive(true);

        // Aguarda input do player
        float timer = 0f;
        bool attackExecuted = false;

        while (timer < slowMotionDuration)
        {
            timer += Time.unscaledDeltaTime;

            // DETECTA O INPUT (não espera o ataque estar ativo)
            if (Input.GetMouseButtonDown(0) && !attackExecuted)
            {
                attackExecuted = true;
                Debug.Log("[Cutscene] INPUT DETECTADO! Cancelando slow motion AGORA!");
                
                // CANCELA SLOW MOTION IMEDIATAMENTE
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
                
                // Esconde o prompt
                if (parryPrompt) parryPrompt.SetActive(false);
                
                Debug.Log("[Cutscene] Aguardando normalização do tempo...");
                
                // Espera alguns frames para tudo estabilizar
                yield return null;
                yield return null;
                
                // FORÇA O ATAQUE DO PLAYER
                if (player != null)
                {
                    Debug.Log("[Cutscene] Chamando ForceAttack()...");
                    player.ForceAttack();
                    
                    // Espera o ataque iniciar
                    yield return null;
                    yield return null;
                    
                    // Verifica se o ataque está ativo
                    Debug.Log($"[Cutscene] Após ForceAttack: IsAttackActive={player.IsAttackActive}");
                }
                else
                {
                    Debug.LogError("[Cutscene] Player é NULL!");
                }
                
                OnParrySuccess();
                break;
            }

            yield return null;
        }

        // Se não atacou no tempo
        if (!attackExecuted)
        {
            Debug.Log("[Cutscene] Tempo esgotado - não atacou a tempo");
            
            // VOLTA O TEMPO AO NORMAL
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            
            if (parryPrompt) parryPrompt.SetActive(false);
            
            OnParryFailed();
        }

        Debug.Log("[Cutscene] Sequência de parry finalizada!");

        parryTutorialComplete = true;
    }

    private void OnParrySuccess()
    {
        Debug.Log("[Cutscene] Parry bem-sucedido!");

        // Cancela o dash do boss
        if (boss)
        {
            //boss.CancelCurrentDash();
        }

        // Feedback visual/sonoro aqui (partículas, som, etc)
    }

    private void OnParryFailed()
    {
        Debug.Log("[Cutscene] Falhou no parry!");
        // Player pode tomar dano ou só avisar
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

    public bool IsCutsceneComplete()
    {
        return cutsceneComplete;
    }
}