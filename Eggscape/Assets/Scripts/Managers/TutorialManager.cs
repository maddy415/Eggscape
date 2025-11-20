using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    // ========== REFERÊNCIAS ==========
    [Header("Referências de Cena")]
    public Player player;
    public TutorialEgg nerdEgg;
    public ObstacleGen obsGen;
    public GameObject objectGen;
    public Rigidbody2D generalRb;
    public GameObject general;
    public Transform generalTransform;

    // ========== UI DE DIÁLOGO ==========
    [Header("UI de Diálogo")]
    public GameObject dialogueBox;
    public Image dialoguePanelImage;         // fundo/caixa do diálogo (mantém cor/estilo)
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI nameText;

    // ========== RETRATOS (PORTRAIT) ==========
    [Header("Retratos (Portrait)")]
    [Tooltip("Imagem do retrato que aparece ao lado do texto")]
    public Image speakerPortrait;            // Image do retrato no Canvas
    [Tooltip("CanvasGroup no mesmo objeto do retrato para desaparecer/aparecer com fade")]
    public CanvasGroup speakerPortraitGroup; // CanvasGroup para fade (opcional, mas recomendado)
    [Min(0f)] public float portraitFadeDuration = 0.15f;
    [Tooltip("Deslocamento horizontal padrão do retrato (valor positivo). Será virado a +X (direita) ou -X (esquerda) automaticamente.")]
    public float portraitOffsetX = 380f;

    [System.Serializable]
    public struct SpeakerPortrait
    {
        public string speakerName;   // "Chicken"
        public Sprite portraitSprite;// sprite padrão
        public bool alignRight;      // true = direita, false = esquerda
    }

    // (opcional) variações por emoção/estado
    [System.Serializable]
    public struct SpeakerEmotion
    {
        public string speakerName;   // "Chicken"
        public string emotionKey;    // "happy", "angry", "hurt" ...
        public Sprite portraitSprite;
        public bool alignRightOverride; // se quiser forçar o lado nessa emoção
        public bool useAlignOverride;   // marca se o alignRightOverride deve valer
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

    // ========== CONFIGURAÇÕES ==========
    [Header("Configurações Gerais")]
    public float walkTime = 1.5f;
    public float spawnTime = 0.75f;
    [Tooltip("Índice do diálogo em que deve spawnar o obstáculo.")]
    public int spawnIndex = 1;
    public float generalSpeed = 5f;

    [Header("Troca de Cena")]
    [Tooltip("Nome da próxima cena a ser carregada após o último diálogo")]
    public string nextSceneName = "MainGame";
    [Tooltip("Tempo de espera (em segundos) após o último diálogo antes de trocar de cena")]
    public float delayBeforeSceneChange = 1.5f;

    [Header("Máquina de Escrever")]
    public float typingSpeed = 0.03f;
    public bool allowSkipTypingWithClick = true;
    public AudioSource typeBlip;

    // 🔒 Falas que não podem ser puladas
    [Header("Bloqueio de Skip")]
    [Tooltip("Diálogos nestes índices NÃO podem ser pulados (nem pular digitação, nem avançar) ENQUANTO estiver nos diálogos do tutorial.")]
    public int[] nonSkippableIndices = { 1 }; // 2º diálogo inicial (index 1)

    // ========== CONTROLE ==========
    private float walkTimer;
    private bool onCutscene = true;
    private bool isWalkingCutscene = true;
    private int currentIndex = 0;
    private bool hasSpawned = false;
    private bool firstDialogueShown = false;

    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private bool skipTyping = false;

    // Flag pra saber se estamos usando as falas da General
    private bool usingGeneralDialogues = false;

    // ========== UI DE PROMPT DE AÇÃO ==========
    [Header("Prompt de Ação (ex.: APERTE ESPAÇO)")]
    [Tooltip("Objeto de UI (no Canvas) que contém o texto grande do prompt. Pode ser um painel com TMP grande.")]
    public CanvasGroup jumpPromptGroup;     // use um CanvasGroup no objeto do prompt
    public TextMeshProUGUI jumpPromptText;  // o TMP grandão do "APERTE ESPAÇO"
    [Tooltip("Quanto tempo o prompt permanece na tela antes de ocultar.")]
    public float jumpPromptDuration = 2.0f;
    [Tooltip("Duração do fade-in/out do prompt.")]
    public float jumpPromptFade = 0.2f;
    [Tooltip("Texto exibido quando é hora de pular.")]
    public string jumpPromptMessage = "APERTE ESPAÇO";
    [Tooltip("Palavra-chave que, quando aparecer no diálogo, dispara o prompt.")]
    public string jumpKeyword = "PULEEEE";
    [Tooltip("Tempo (em segundos) de atraso antes do prompt aparecer.")]
    public float jumpPromptDelay = 0.5f;

    private Coroutine jumpPromptRoutine;

    // ========== DIÁLOGOS ==========
    [System.Serializable]
    public struct DialogueLine
    {
        public string speaker;
        [TextArea(2, 4)] public string text;
        public string emotion; // opcional: deixe vazio se não usar
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

    // Sequência ativa (começa nas falas do tutorial)
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

        // 🔒 tutorial inteiro sem ataque
        if (player != null)
        {
            player.canAttack = false;
        }

        // deixa o prompt oculto no início
        if (jumpPromptGroup != null)
        {
            jumpPromptGroup.alpha = 0f;
            jumpPromptGroup.gameObject.SetActive(false);
        }
    }

    private void BuildPortraitMaps()
    {
        portraitMap.Clear();
        foreach (var p in speakerPortraits)
        {
            if (!string.IsNullOrWhiteSpace(p.speakerName) && p.portraitSprite != null)
                portraitMap[p.speakerName] = p;
        }

        emotionMap.Clear();
        foreach (var e in speakerEmotions)
        {
            if (!string.IsNullOrWhiteSpace(e.speakerName) &&
                !string.IsNullOrWhiteSpace(e.emotionKey) &&
                e.portraitSprite != null)
            {
                emotionMap[(e.speakerName, e.emotionKey)] = e;
            }
        }
    }

    // ========== LOOP ==========
    private void Update()
    {
        HandlePlayerMovement();
        HandleWalkingCutscene();
        HandleDialogueDisplay();
        HandleInput();
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
        // Se o diálogo atual estiver travado (ex.: 2º diálogo do tutorial), ignora cliques
        if (IsCurrentDialogueLocked()) return;

        if (Input.GetMouseButtonDown(0))
        {
            AdvanceDialogue();
        }
    }

    public void AdvanceDialogueFromUI()
    {
        if (IsCurrentDialogueLocked()) return;
        AdvanceDialogue();
    }

    private void AdvanceDialogue()
    {
        // 1) se estiver digitando, completa na hora e NÃO avança ainda
        if (isTyping && allowSkipTypingWithClick)
        {
            CompleteTypingImmediately();
            return;
        }

        // 2) se já mostrou tudo, agora pode avançar
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
                // Quando terminar os diálogos, carrega a próxima cena
                StartCoroutine(LoadNextSceneAfterDelay());
            }
        }
    }

    private void ShowDialogue(int index)
    {
        if (index < 0 || index >= activeDialogues.Length) return;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        // >>> RETRATO DO SPEAKER (SISTEMA CORRIGIDO) <<<
        ApplyPortrait(activeDialogues[index]);

        // Nome do speaker
        if (nameText != null)
        {
            bool hasName = !string.IsNullOrWhiteSpace(activeDialogues[index].speaker);
            nameText.gameObject.SetActive(hasName);
            if (hasName) nameText.text = activeDialogues[index].speaker;
        }

        // Texto com máquina de escrever
        dialogueText.text = "";
        typingCoroutine = StartCoroutine(TypeText(activeDialogues[index].text));

        // >>> PROMPT "APERTE ESPAÇO" se for a fala do PULEEEE
        TryShowJumpPromptIfNeeded(activeDialogues[index]);
    }

    // --------- SISTEMA DE RETRATO CORRIGIDO (igual ao DialogueSystem) ----------
    private void ApplyPortrait(DialogueLine line)
    {
        if (speakerPortrait == null) return;

        Sprite spriteToUse = null;
        bool alignRight = false;
        bool found = false;

        // Tenta encontrar emoção específica primeiro
        if (!string.IsNullOrWhiteSpace(line.emotion) &&
            emotionMap.TryGetValue((line.speaker, line.emotion), out var emo))
        {
            spriteToUse = emo.portraitSprite;
            alignRight = emo.useAlignOverride ? emo.alignRightOverride
                                              : (portraitMap.TryGetValue(line.speaker, out var baseP) ? baseP.alignRight : false);
            found = spriteToUse != null;
        }
        // Se não encontrar emoção, usa o retrato padrão
        else if (portraitMap.TryGetValue(line.speaker, out var basePortrait))
        {
            spriteToUse = basePortrait.portraitSprite;
            alignRight = basePortrait.alignRight;
            found = spriteToUse != null;
        }

        if (!found)
        {
            // Não há sprite definido para este speaker/emoção - oculta o retrato
            if (speakerPortraitGroup != null) speakerPortraitGroup.alpha = 0f;
            speakerPortrait.gameObject.SetActive(false);
            return;
        }

        // Troca o sprite com efeito de fade
        if (portraitFadeCoroutine != null) StopCoroutine(portraitFadeCoroutine);
        portraitFadeCoroutine = StartCoroutine(FadePortraitTo(spriteToUse));
    }

    private IEnumerator FadePortraitTo(Sprite targetSprite)
    {
        if (!speakerPortrait.gameObject.activeSelf) 
            speakerPortrait.gameObject.SetActive(true);

        // 🔧 GARANTE QUE O ASPECT RATIO SEJA PRESERVADO (FIX PARA IMAGENS ESTICADAS)
        speakerPortrait.preserveAspect = true;

        // Se não houver CanvasGroup, apenas troca o sprite
        if (speakerPortraitGroup == null)
        {
            speakerPortrait.sprite = targetSprite;
            yield break;
        }

        float d = Mathf.Max(0.0001f, portraitFadeDuration);

        // Fade out
        float t = 0f;
        float startAlpha = speakerPortraitGroup.alpha;
        while (t < d)
        {
            t += Time.deltaTime;
            speakerPortraitGroup.alpha = Mathf.Lerp(startAlpha, 0f, t / d);
            yield return null;
        }
        speakerPortraitGroup.alpha = 0f;

        // Troca o sprite
        speakerPortrait.sprite = targetSprite;

        // Fade in
        t = 0f;
        while (t < d)
        {
            t += Time.deltaTime;
            speakerPortraitGroup.alpha = Mathf.Lerp(0f, 1f, t / d);
            yield return null;
        }
        speakerPortraitGroup.alpha = 1f;
    }

    // --------- Máquina de escrever ----------
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
        // encerra a coroutine e mostra tudo imediatamente
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
        if (obsGen != null) obsGen.SpawnObstacle();
        hasSpawned = true;
    }

    // --------- Cena da General andando ----------
    public IEnumerator GeneralWalk()
    {
        if (general != null) general.SetActive(true);

        float timer = 0f;
        while (timer < 1.5f)
        {
            // se estiver usando Unity 2022/2023 com physics2D changes:
            // generalRb.velocity = Vector2.left * generalSpeed;
            generalRb.linearVelocity = Vector2.left * generalSpeed;
            timer += Time.deltaTime;
            yield return null;
        }
        // generalRb.velocity = Vector2.zero;
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
        // 🔓 Assim que entra no diálogo da General, nenhuma fala é travada
        if (usingGeneralDialogues) return false;

        foreach (int idx in nonSkippableIndices)
            if (currentIndex == idx) return true;

        return false;
    }

    // ======= MÉTODO PÚBLICO: chamar as falas da General =======
    public void ShowGeneralDialogue()
    {
        usingGeneralDialogues = true;   // a partir daqui, nada é "não pulável"
        activeDialogues = generalDialogues; // troca sequência ativa
        currentIndex = 0;
        OpenDialogueBox();
        ShowDialogue(currentIndex);
    }

    // ======= PROMPT "APERTE ESPAÇO" =======
    private void TryShowJumpPromptIfNeeded(DialogueLine line)
    {
        // Só mostra se for o NerdEgg falando e a fala contiver a palavra-chave (case-insensitive)
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

        // Se já tiver um prompt rolando, reinicia
        if (jumpPromptRoutine != null) StopCoroutine(jumpPromptRoutine);
        jumpPromptRoutine = StartCoroutine(JumpPromptRoutine(duration));
    }

    private IEnumerator JumpPromptRoutine(float duration)
    {
        // ⏳ Aguarda o delay configurável antes de mostrar
        if (jumpPromptDelay > 0f)
            yield return new WaitForSeconds(jumpPromptDelay);

        // Ativa e faz fade-in
        jumpPromptGroup.gameObject.SetActive(true);
        yield return FadeCanvasGroup(jumpPromptGroup, 0f, 1f, jumpPromptFade);

        // Mantém na tela por 'duration'
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // Fade-out e desativa
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
    
        if (!string.IsNullOrWhiteSpace(nextSceneName))
        {
            // Usa o SceneTransition se existir, senão carrega direto
            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.LoadScene(nextSceneName);
            }
            else
            {
                SceneManager.LoadScene(nextSceneName);
            }
        }
        else
        {
            Debug.LogWarning("TutorialManager: Nome da próxima cena não foi configurado!");
        }
    }
}