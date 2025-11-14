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
    public KeyCode parryKey = KeyCode.Mouse1;
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
        yield return StartCoroutine(ParryTutorial());

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

    private IEnumerator ParryTutorial()
    {
        if (!boss || !player) yield break;

        parryTutorialComplete = false;

        // GARANTE que o player pode atacar ANTES do dash começar
        if (player)
        {
            player.canAttack = true;
            Debug.Log("[Cutscene] canAttack garantido como TRUE antes do dash");
        }

        // Faz o boss iniciar o primeiro dash manualmente
        boss.enabled = true;
        StartCoroutine(boss.ExecuteFirstDashWithTutorial(this));

        // Aguarda o parry ser executado
        yield return new WaitUntil(() => parryTutorialComplete);
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

        // GARANTE que o ataque está liberado
        if (player)
        {
            player.canAttack = true;
            Debug.Log($"[Cutscene] canAttack setado para: {player.canAttack}");
        }

        // Mostra prompt
        if (parryPrompt) parryPrompt.SetActive(true);

        // Aguarda input do player
        float timer = 0f;
        bool attackInputDetected = false;

        while (timer < slowMotionDuration)
        {
            timer += Time.unscaledDeltaTime;

            // Detecta quando o ATAQUE ESTÁ ATIVO (não só o input)
            if (player != null && player.IsAttackActive)
            {
                Debug.Log("[Cutscene] PARRY EXECUTADO! Ataque está ativo!");
                OnParrySuccess();
                
                // ESPERA 1 FRAME para o ataque processar ANTES de voltar o tempo
                yield return null;
                break;
            }

            // Detecta input mas não quebra o loop ainda
            if (Input.GetMouseButtonDown(0) && !attackInputDetected)
            {
                /*Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;*/
                attackInputDetected = true;
                Debug.Log("[Cutscene] Input detectado - aguardando ataque ativar...");
            }

            yield return null;
        }

        // Se não conseguiu parry
        if (player == null || !player.IsAttackActive)
        {
            Debug.Log("[Cutscene] Tempo esgotado - falhou no parry");
            OnParryFailed();
        }

        // Retorna tempo normal DEPOIS do ataque estar ativo
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        if (parryPrompt) parryPrompt.SetActive(false);

        Debug.Log("[Cutscene] Tempo voltou ao normal!");

        parryTutorialComplete = true;
    }

    private void OnParrySuccess()
    {
        Debug.Log("[Cutscene] Parry bem-sucedido!");

        // Cancela o dash do boss
        if (boss)
        {
            boss.CancelCurrentDash();
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