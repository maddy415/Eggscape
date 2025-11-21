using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    // ========== REFERÊNCIAS DE CENA ==========
    [Header("Referências de Cena")]
    public Player player;
    public TutorialEgg nerdEgg;
    public ObstacleGen obsGen;
    public GameObject objectGen;
    public Rigidbody2D generalRb;
    public GameObject general;                // Tenente Clara (prefab/objeto)
    public Transform generalTransform;        // onde a general deve aparecer (opcional)

    // ========== UI DE DIÁLOGO ==========
    [Header("UI de Diálogo")]
    public GameObject dialogueBox;
    public Image dialoguePanelImage;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI nameText;

    // ========== RETRATOS (PORTRAIT) ==========
    [Header("Retratos (Portrait)")]
    public Image speakerPortrait;
    public CanvasGroup speakerPortraitGroup;
    [Min(0f)] public float portraitFadeDuration = 0.15f;
    public float portraitOffsetX = 380f;

    [System.Serializable]
    public struct SpeakerPortrait
    {
        public string speakerName;
        public Sprite portraitSprite;
        public bool alignRight;
    }

    [System.Serializable]
    public struct SpeakerEmotion
    {
        public string speakerName;
        public string emotionKey;
        public Sprite portraitSprite;
        public bool alignRightOverride;
        public bool useAlignOverride;
    }

    public List<SpeakerPortrait> speakerPortraits = new List<SpeakerPortrait>();
    public List<SpeakerEmotion> speakerEmotions = new List<SpeakerEmotion>();

    private Dictionary<string, SpeakerPortrait> portraitMap = new Dictionary<string, SpeakerPortrait>();
    private Dictionary<(string speaker, string emotion), SpeakerEmotion> emotionMap =
        new Dictionary<(string, string), SpeakerEmotion>();

    private Coroutine portraitFadeCoroutine;

    // ========== ESTILO ==========
    [Header("Estilo (opcional)")]
    public Color panelColor = new Color(0, 0, 0, 0.65f);
    public Color nameColor = new Color(1f, 0.93f, 0.35f, 1f);
    [Min(10)] public int dialogueFontSize = 32;
    [Min(10)] public int nameFontSize = 26;
    public TextAlignmentOptions dialogueAlignment = TextAlignmentOptions.TopLeft;

    // ========== CONFIGS ==========
    [Header("Configurações Gerais")]
    public float walkTime = 1.5f;
    public float spawnTime = 0.75f;
    [Tooltip("Índice do diálogo em que deve spawnar o obstáculo.")]
    public int spawnIndex = 1;
    public float generalSpeed = 5f;

    [Header("Jump Trigger by Layer")]
    [Tooltip("Layer(s) considered as the jump trigger. Configure the JumpTrigger object to use one of these layers.")]
    public LayerMask jumpTriggerLayer; // setar no inspector (ex: JumpTrigger)

    [Header("Slow Motion do Tronco")]
    [Tooltip("Distância do tronco ao player para ativar o slow motion")]
    public float slowMotionDistance = 5f;
    [Tooltip("Velocidade reduzida do tronco (porcentagem da velocidade original, ex: 0.3 = 30%)")]
    [Range(0.1f, 1f)] public float slowMotionSpeed = 0.3f;

    // runtime
    private GameObject spawnedObstacle;
    private Rigidbody2D obstacleRb;
    private ObstacleMove obstacleMove;
    private float originalObstacleSpeed;
    private bool isSlowMotionActive = false;
    private bool hasPassedObstacle = false;

    [Header("Troca de Cena")]
    public string nextSceneName = "MainGame";
    public float delayBeforeSceneChange = 1.5f;

    [Header("Máquina de Escrever")]
    public float typingSpeed = 0.03f;
    public bool allowSkipTypingWithClick = true;
    public AudioSource typeBlip;

    [Header("Bloqueio de Skip")]
    public int[] nonSkippableIndices = { 1 };

    // controle
    private float walkTimer;
    private bool onCutscene = true;
    private bool isWalkingCutscene = true;
    private int currentIndex = 0;
    private bool hasSpawned = false;
    private bool firstDialogueShown = false;

    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private bool skipTyping = false;

    // flag
    private bool usingGeneralDialogues = false;

    // prompt
    [Header("Prompt de Ação (ex.: APERTE ESPAÇO)")]
    public CanvasGroup jumpPromptGroup;
    public TextMeshProUGUI jumpPromptText;
    public float jumpPromptDuration = 2.0f;
    public float jumpPromptFade = 0.2f;
    public string jumpPromptMessage = "APERTE ESPAÇO";
    public string jumpKeyword = "PULEEEE";
    public float jumpPromptDelay = 0.5f;
    private Coroutine jumpPromptRoutine;

    // ========== DIÁLOGOS ==========
    [System.Serializable]
    public struct DialogueLine
    {
        public string speaker;
        [TextArea(2,4)] public string text;
        public string emotion;
    }

    [Header("Falas do Tutorial (padrão)")]
    public DialogueLine[] dialogues =
    {
        new DialogueLine { speaker = "Ovinho", text = "Chicken, precisamos correr, as outras galinhas estão nos esperando!", emotion = "" },
        new DialogueLine { speaker = "Ovinho", text = "Primeiro, vou te ensinar a pular troncos. Tem um vindo aí, aperte 'Espaço' para pular!", emotion = "" },
        new DialogueLine { speaker = "Chicken", text = "Entendido! Estou pronta!", emotion = "happy" },
        new DialogueLine { speaker = "Ovinho", text = "Perfeito. Agora, siga em frente sem hesitar!", emotion = "" }
    };

    [Header("Falas da General")]
    public DialogueLine[] generalDialogues =
    {
        new DialogueLine { speaker = "Tenente Clara", text = "Soldada Chicken, situação crítica!", emotion = "" },
        new DialogueLine { speaker = "Tenente Clara", text = "Mantenha a calma e continue a missão.", emotion = "" }
    };

    private DialogueLine[] activeDialogues;

    // ========== INICIALIZAÇÃO ==========
    private void Start()
    {
        if (player == null) player = GameObject.FindWithTag("Player")?.GetComponent<Player>();
        if (nerdEgg == null) nerdEgg = GameObject.FindWithTag("TutorialEgg")?.GetComponent<TutorialEgg>();
        if (obsGen == null && objectGen != null) obsGen = objectGen.GetComponent<ObstacleGen>();

        if (dialogueBox != null) dialogueBox.SetActive(false);
        ApplyStyleOnce();
        BuildPortraitMaps();

        // começa usando as falas do tutorial
        activeDialogues = dialogues;
        usingGeneralDialogues = false;

        // tutorial sem ataque
        if (player != null) player.canAttack = false;

        // prompt off
        if (jumpPromptGroup != null)
        {
            jumpPromptGroup.alpha = 0f;
            jumpPromptGroup.gameObject.SetActive(false);
        }

        // garante que a general esteja oculta até o trigger
        if (general != null) general.SetActive(false);
    }

    private void BuildPortraitMaps()
    {
        portraitMap.Clear();
        foreach (var p in speakerPortraits)
            if (!string.IsNullOrWhiteSpace(p.speakerName) && p.portraitSprite != null)
                portraitMap[p.speakerName] = p;

        emotionMap.Clear();
        foreach (var e in speakerEmotions)
            if (!string.IsNullOrWhiteSpace(e.speakerName) && !string.IsNullOrWhiteSpace(e.emotionKey) && e.portraitSprite != null)
                emotionMap[(e.speakerName, e.emotionKey)] = e;
    }

    // ========== LOOP ==========
    private void Update()
    {
        HandlePlayerMovement();
        HandleWalkingCutscene();
        HandleDialogueDisplay();
        HandleInput();
        HandleObstacleSlowMotion();
    }

    private void HandlePlayerMovement()
    {
        if (player != null) player.CanMove = !onCutscene;
    }

    private void HandleWalkingCutscene()
    {
        walkTimer += Time.deltaTime;
        if (isWalkingCutscene && player != null)
            player.transform.position += Vector3.right * Time.deltaTime * 5f;

        if (walkTimer > walkTime)
        {
            isWalkingCutscene = false;
            walkTimer = 0;
        }
    }

    private void HandleDialogueDisplay()
    {
        if (nerdEgg != null && !nerdEgg.isWalkingCutscene && !firstDialogueShown)
        {
            OpenDialogueBox();
            ShowDialogue(currentIndex);
            firstDialogueShown = true;
        }
    }

    private void HandleInput()
    {
        if (IsCurrentDialogueLocked()) return;

        if (Input.GetMouseButtonDown(0))
            AdvanceDialogue();
    }

    public void AdvanceDialogueFromUI()
    {
        if (IsCurrentDialogueLocked()) return;
        AdvanceDialogue();
    }

    private void AdvanceDialogue()
    {
        if (isTyping && allowSkipTypingWithClick)
        {
            CompleteTypingImmediately();
            return;
        }

        if (firstDialogueShown)
        {
            currentIndex++;
            if (currentIndex < activeDialogues.Length)
            {
                ShowDialogue(currentIndex);

                if (currentIndex == spawnIndex && !hasSpawned)
                    StartCoroutine(SpawnDelay());
            }
            else
            {
                CloseDialogueBox();
                StartCoroutine(LoadNextSceneAfterDelay());
            }
        }
    }

    private void ShowDialogue(int index)
    {
        if (index < 0 || index >= activeDialogues.Length) return;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        ApplyPortrait(activeDialogues[index]);

        if (nameText != null)
        {
            bool hasName = !string.IsNullOrWhiteSpace(activeDialogues[index].speaker);
            nameText.gameObject.SetActive(hasName);
            if (hasName) nameText.text = activeDialogues[index].speaker;
        }

        dialogueText.text = "";
        typingCoroutine = StartCoroutine(TypeText(activeDialogues[index].text));

        TryShowJumpPromptIfNeeded(activeDialogues[index]);
    }

    private void ApplyPortrait(DialogueLine line)
    {
        if (speakerPortrait == null) return;

        Sprite spriteToUse = null;
        bool found = false;

        if (!string.IsNullOrWhiteSpace(line.emotion) &&
            emotionMap.TryGetValue((line.speaker, line.emotion), out var emo))
        {
            spriteToUse = emo.portraitSprite;
            found = spriteToUse != null;
        }
        else if (portraitMap.TryGetValue(line.speaker, out var basePortrait))
        {
            spriteToUse = basePortrait.portraitSprite;
            found = spriteToUse != null;
        }

        if (!found)
        {
            if (speakerPortraitGroup != null) speakerPortraitGroup.alpha = 0f;
            speakerPortrait.gameObject.SetActive(false);
            return;
        }

        if (portraitFadeCoroutine != null) StopCoroutine(portraitFadeCoroutine);
        portraitFadeCoroutine = StartCoroutine(FadePortraitTo(spriteToUse));
    }

    private IEnumerator FadePortraitTo(Sprite targetSprite)
    {
        if (!speakerPortrait.gameObject.activeSelf)
            speakerPortrait.gameObject.SetActive(true);

        speakerPortrait.preserveAspect = true;

        if (speakerPortraitGroup == null)
        {
            speakerPortrait.sprite = targetSprite;
            yield break;
        }

        float d = Mathf.Max(0.0001f, portraitFadeDuration);
        float t = 0f;
        float startAlpha = speakerPortraitGroup.alpha;
        while (t < d)
        {
            t += Time.deltaTime;
            speakerPortraitGroup.alpha = Mathf.Lerp(startAlpha, 0f, t / d);
            yield return null;
        }
        speakerPortraitGroup.alpha = 0f;

        speakerPortrait.sprite = targetSprite;

        t = 0f;
        while (t < d)
        {
            t += Time.deltaTime;
            speakerPortraitGroup.alpha = Mathf.Lerp(0f, 1f, t / d);
            yield return null;
        }
        speakerPortraitGroup.alpha = 1f;
    }

    private IEnumerator TypeText(string fullText)
    {
        isTyping = true;
        skipTyping = false;

        foreach (char c in fullText)
        {
            if (skipTyping)
            {
                dialogueText.text = fullText;
                break;
            }

            dialogueText.text += c;

            if (typeBlip != null) typeBlip.Play();
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    private void CompleteTypingImmediately()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        skipTyping = true;
        dialogueText.text = GetCurrentTextRaw();
        isTyping = false;
    }

    private string GetCurrentTextRaw()
    {
        if (currentIndex >= 0 && currentIndex < activeDialogues.Length)
            return activeDialogues[currentIndex].text;
        return string.Empty;
    }

    // --------- Spawn de obstáculo ----------
    private IEnumerator SpawnDelay()
    {
        onCutscene = false;
        yield return new WaitForSeconds(spawnTime);

        if (obsGen != null)
        {
            // pega o objeto criado diretamente do spawner
            GameObject troncoClone = obsGen.SpawnObstacle();

            spawnedObstacle = troncoClone;
            obstacleRb = troncoClone.GetComponent<Rigidbody2D>();
            obstacleMove = troncoClone.GetComponent<ObstacleMove>();

            if (obstacleMove != null)
            {
                originalObstacleSpeed = obstacleMove.speed;
                Debug.Log($"[TutorialManager] Tronco spawnado, speed base = {originalObstacleSpeed}");
            }
            else
            {
                Debug.LogWarning("[TutorialManager] Tronco spawnado, mas sem ObstacleMove!");
            }

            // procura o trigger pelo LayerMask configurado (mais robusto que nome)
            TutorialObstacleTrigger trigger = null;
            if (jumpTriggerLayer != 0)
            {
                Transform[] children = troncoClone.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in children)
                {
                    if (child == troncoClone.transform) continue;
                    if ((jumpTriggerLayer.value & (1 << child.gameObject.layer)) != 0)
                    {
                        trigger = child.GetComponent<TutorialObstacleTrigger>();
                        if (trigger == null)
                            trigger = child.gameObject.AddComponent<TutorialObstacleTrigger>();
                        break;
                    }
                }
            }

            if (trigger == null)
            {
                trigger = troncoClone.GetComponentInChildren<TutorialObstacleTrigger>();
            }

            if (trigger != null)
            {
                trigger.tutorialManager = this;
            }
            else
            {
                Debug.LogWarning("[TutorialManager] Nenhum TutorialObstacleTrigger encontrado/adicionado no tronco (procura por layer falhou).");
            }
        }

        hasSpawned = true;
    }

    private void HandleObstacleSlowMotion()
    {
        if (spawnedObstacle == null || player == null || hasPassedObstacle)
            return;

        float distance = Vector3.Distance(player.transform.position, spawnedObstacle.transform.position);

        if (distance <= slowMotionDistance && !isSlowMotionActive)
            ActivateSlowMotion();

        // fallback: se o player já ultrapassou o tronco no eixo X, considera "passou"
        float offset = 0.2f;
        if (!hasPassedObstacle && player.transform.position.x > spawnedObstacle.transform.position.x + offset)
        {
            Debug.Log("[TutorialManager] Fallback: player passou o tronco no eixo X.");
            OnPlayerPassedObstacle(); // somente restauração, NÃO spawna General
        }
    }

    private void ActivateSlowMotion()
    {
        if (hasPassedObstacle || isSlowMotionActive) return;

        if (obstacleMove == null)
        {
            Debug.LogWarning("[TutorialManager] Tentou ativar slow motion, mas ObstacleMove é nulo!");
            return;
        }

        isSlowMotionActive = true;
        obstacleMove.SetSpeedMultiplier(slowMotionSpeed);
        Debug.Log($"[TutorialManager] Slow motion ativado! Multiplier = {slowMotionSpeed}");
    }

    // chamado pelo sistema de fallback / restauração (não spawna a General)
    public void OnPlayerPassedObstacle()
    {
        if (hasPassedObstacle) return;

        hasPassedObstacle = true;
        isSlowMotionActive = false;

        if (obstacleMove != null)
            obstacleMove.ResetSpeedMultiplier();

        Debug.Log("[TutorialManager] Player passou pelo tronco (restauração). Velocidade restaurada.");
        // NOTA: não spawnar a General aqui
    }

    // chamado EXCLUSIVAMENTE pelo TutorialObstacleTrigger quando o Player sair do JumpTrigger
    public void OnPlayerTriggeredPass()
    {
        // garante restauração básica
        OnPlayerPassedObstacle();

        // agora faz a parte que só o trigger deve fazer: spawn + diálogo da General
        Debug.Log("[TutorialManager] OnPlayerTriggeredPass chamado — spawnando General e iniciando diálogo.");
        StartCoroutine(SpawnGeneralAndTalk());
    }

    
    // ========== Spawn da General e diálogo ==========
    // ========== Spawn da General e diálogo ==========
    private IEnumerator SpawnGeneralAndTalk()
    {
        // ativa e posiciona a general
        if (general != null)
        {
            general.SetActive(true);

            if (generalTransform != null)
                general.transform.position = generalTransform.position;

            // Ignora colisão entre General e Player
            if (player != null)
            {
                // Busca collider na General (pode estar no próprio objeto ou em filhos)
                Collider2D generalCollider = general.GetComponentInChildren<Collider2D>();
                
                // Busca collider no Player (pode estar no próprio objeto ou em filhos como GFX)
                Collider2D playerCollider = player.GetComponentInChildren<Collider2D>();

                if (generalCollider != null && playerCollider != null)
                {
                    Physics2D.IgnoreCollision(generalCollider, playerCollider, true);
                    Debug.Log($"[TutorialManager] Colisão desabilitada entre {generalCollider.gameObject.name} e {playerCollider.gameObject.name}");
                }
                else
                {
                    Debug.LogWarning($"[TutorialManager] Colliders não encontrados! General: {(generalCollider != null ? "OK" : "NULL")}, Player: {(playerCollider != null ? "OK" : "NULL")}");
                }
            }
        }
        else
        {
            Debug.LogWarning("[TutorialManager] SpawnGeneralAndTalk chamado, mas 'general' é nulo!");
        }

        // inicia a caminhada (não bloqueante)
        if (generalRb != null && general != null)
            StartCoroutine(GeneralWalk());

        // troca pra falas da General e abre a caixa
        ShowGeneralDialogue();

        yield break;
    }
    // --------- Cena da General andando ----------
    public IEnumerator GeneralWalk()
    {
        if (general != null) general.SetActive(true);

        float timer = 0f;
        while (timer < 1.5f)
        {
            if (generalRb != null)
                generalRb.linearVelocity = Vector2.left * generalSpeed;
            timer += Time.deltaTime;
            yield return null;
        }
        if (generalRb != null)
            generalRb.linearVelocity = Vector2.zero;
    }

    // --------- UI Básica ----------
    private void OpenDialogueBox()
    {
        if (dialogueBox != null && !dialogueBox.activeSelf)
            dialogueBox.SetActive(true);
    }

    public void CloseDialogueBox()
    {
        if (dialogueBox != null && dialogueBox.activeSelf)
            dialogueBox.SetActive(false);
    }

    private void ApplyStyleOnce()
    {
        if (dialoguePanelImage != null)
            dialoguePanelImage.color = panelColor;

        if (dialogueText != null)
        {
            dialogueText.fontSize = dialogueFontSize;
            dialogueText.alignment = dialogueAlignment;
        }

        if (nameText != null)
        {
            nameText.fontSize = nameFontSize;
            nameText.color = nameColor;
        }
    }

    private bool IsCurrentDialogueLocked()
    {
        if (usingGeneralDialogues) return false;

        foreach (int idx in nonSkippableIndices)
            if (currentIndex == idx) return true;

        return false;
    }

    // ======= MÉTODO PÚBLICO: chamar as falas da General =======
    public void ShowGeneralDialogue()
    {
        usingGeneralDialogues = true;
        activeDialogues = generalDialogues;
        currentIndex = 0;
        OpenDialogueBox();
        ShowDialogue(currentIndex);
    }

    // ======= PROMPT "APERTE ESPAÇO" =======
    private void TryShowJumpPromptIfNeeded(DialogueLine line)
    {
        if (line.speaker != null && line.text != null &&
            line.speaker.Trim().Equals("Ovinho", System.StringComparison.OrdinalIgnoreCase) &&
            line.text.ToUpperInvariant().Contains(jumpKeyword.ToUpperInvariant()))
        {
            ShowJumpPrompt(jumpPromptMessage, jumpPromptDuration);
        }
    }

    public void ShowJumpPrompt(string message, float duration)
    {
        if (jumpPromptGroup == null || jumpPromptText == null)
            return;

        jumpPromptText.text = string.IsNullOrWhiteSpace(message) ? jumpPromptMessage : message;

        if (jumpPromptRoutine != null) StopCoroutine(jumpPromptRoutine);
        jumpPromptRoutine = StartCoroutine(JumpPromptRoutine(duration));
    }

    private IEnumerator JumpPromptRoutine(float duration)
    {
        if (jumpPromptDelay > 0f)
            yield return new WaitForSeconds(jumpPromptDelay);

        jumpPromptGroup.gameObject.SetActive(true);
        yield return FadeCanvasGroup(jumpPromptGroup, 0f, 1f, jumpPromptFade);

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        yield return FadeCanvasGroup(jumpPromptGroup, 1f, 0f, jumpPromptFade);
        jumpPromptGroup.gameObject.SetActive(false);
        jumpPromptRoutine = null;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float time)
    {
        if (cg == null) yield break;
        float d = Mathf.Max(0.0001f, time);
        float t = 0f;
        cg.alpha = from;
        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / d);
            yield return null;
        }
        cg.alpha = to;
    }

    // ======= TROCA DE CENA =======
    private IEnumerator LoadNextSceneAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeSceneChange);

        if (SaveManager.Instance != null)
        {
            int tutorialBuildIndex = SceneManager.GetActiveScene().buildIndex;
            int levelToUnlock = 0;
            SaveManager.Instance.CompleteLevel(levelToUnlock, 0);
            Debug.Log($"✅ [TutorialManager] Tutorial completado! Primeira fase (Level Index 0) desbloqueada.");
        }
        else
        {
            Debug.LogError("❌ [TutorialManager] SaveManager NÃO ENCONTRADO!");
        }

        if (!string.IsNullOrWhiteSpace(nextSceneName))
        {
            if (SceneTransition.Instance != null)
                SceneTransition.Instance.LoadScene(nextSceneName);
            else
                SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("TutorialManager: Nome da próxima cena não foi configurado!");
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Debug: Forçar Completar Tutorial")]
    private void DebugCompleteTutorial()
    {
        if (SaveManager.Instance != null)
        {
            int tutorialIndex = SceneManager.GetActiveScene().buildIndex;
            SaveManager.Instance.CompleteLevel(tutorialIndex, 0);
            Debug.Log($"[DEBUG] Tutorial forçado como completo! LevelReached: {SaveManager.Instance.GetLevelReached()}");
        }
    }
#endif
}
