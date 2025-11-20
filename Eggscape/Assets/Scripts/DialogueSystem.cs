using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Sistema de diálogo com efeito máquina de escrever
/// </summary>
public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    public GameObject continueIndicator;

    [Header("Retratos (Portrait)")]
    [Tooltip("Imagem do retrato que aparece ao lado do texto")]
    public Image speakerPortrait;
    [Tooltip("CanvasGroup no mesmo objeto do retrato para fade in/out")]
    public CanvasGroup speakerPortraitGroup;
    [Min(0f)] public float portraitFadeDuration = 0.15f;

    [Header("Typewriter Settings")]
    public float typingSpeed = 0.05f;
    public AudioClip typingSound;
    public float typingSoundInterval = 0.1f;

    [Header("Input")]
    public KeyCode advanceKey = KeyCode.Space;
    public KeyCode skipKey = KeyCode.Return;

    [System.Serializable]
    public struct SpeakerPortrait
    {
        public string speakerName;
        public Sprite portraitSprite;
        public bool alignRight; // true = direita, false = esquerda
    }

    [System.Serializable]
    public struct SpeakerEmotion
    {
        public string speakerName;
        public string emotionKey; // "happy", "angry", "hurt", etc.
        public Sprite portraitSprite;
        public bool alignRightOverride;
        public bool useAlignOverride;
    }

    [Header("Portrait Configuration")]
    public List<SpeakerPortrait> speakerPortraits = new List<SpeakerPortrait>();
    public List<SpeakerEmotion> speakerEmotions = new List<SpeakerEmotion>();

    private Dictionary<string, SpeakerPortrait> portraitMap = new Dictionary<string, SpeakerPortrait>();
    private Dictionary<(string speaker, string emotion), SpeakerEmotion> emotionMap =
        new Dictionary<(string, string), SpeakerEmotion>();
    private Coroutine portraitFadeCoroutine;

    private Queue<DialogueLine> currentDialogueQueue;
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private bool dialogueActive = false;
    private Action onDialogueComplete;

    // Nova variável para armazenar o texto completo da linha atual
    private string currentFullText = "";

    private AudioSource audioSource;

    [System.Serializable]
    public class DialogueLine
    {
        public string speakerName;
        [TextArea(3, 6)]
        public string text;
        public string emotion; // opcional: "happy", "sad", "angry", etc.
        public float delayAfter = 0f;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (continueIndicator) continueIndicator.SetActive(false);

        BuildPortraitMaps();
    }

    private void Update()
    {
        if (!dialogueActive) return;

        if (Input.GetKeyDown(advanceKey))
        {
            HandleAdvanceRequest();
        }

        if (Input.GetKeyDown(skipKey))
        {
            SkipDialogue();
        }
    }

    /// <summary>
    /// Avança o diálogo a partir de um botão/touch.
    /// </summary>
    public void AdvanceFromUI()
    {
        if (!dialogueActive) return;
        HandleAdvanceRequest();
    }

    /// <summary>
    /// Pula o diálogo atual a partir de um botão/touch.
    /// </summary>
    public void SkipFromUI()
    {
        if (!dialogueActive) return;
        SkipDialogue();
    }

    /// <summary>
    /// Inicia uma sequência de diálogo
    /// </summary>
    public void StartDialogue(List<DialogueLine> lines, Action onComplete = null)
    {
        if (lines == null || lines.Count == 0)
        {
            Debug.LogWarning("[DialogueSystem] Tentou iniciar diálogo vazio!");
            onComplete?.Invoke();
            return;
        }

        currentDialogueQueue = new Queue<DialogueLine>(lines);
        onDialogueComplete = onComplete;
        dialogueActive = true;

        if (dialoguePanel) dialoguePanel.SetActive(true);

        DisplayNextLine();
    }

    private void DisplayNextLine()
    {
        if (currentDialogueQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = currentDialogueQueue.Dequeue();

        // Aplica o retrato do speaker
        ApplyPortrait(line);

        if (speakerNameText)
        {
            speakerNameText.text = line.speakerName;
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // Armazena o texto completo da linha atual
        currentFullText = line.text;
        typingCoroutine = StartCoroutine(TypeText(line.text, line.delayAfter));
    }

    private IEnumerator TypeText(string text, float delayAfter)
    {
        isTyping = true;
        if (continueIndicator) continueIndicator.SetActive(false);

        dialogueText.text = "";
        float soundTimer = 0f;

        foreach (char c in text)
        {
            dialogueText.text += c;

            // Toca som de digitação
            soundTimer += typingSpeed;
            if (typingSound && soundTimer >= typingSoundInterval)
            {
                audioSource.PlayOneShot(typingSound);
                soundTimer = 0f;
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        if (continueIndicator) continueIndicator.SetActive(true);

        // Delay opcional após completar a linha
        if (delayAfter > 0f)
        {
            yield return new WaitForSeconds(delayAfter);
            DisplayNextLine();
        }
    }

    private void CompleteTextImmediately()
    {
        // Para a corrotina de digitação
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        // Mostra o texto completo imediatamente
        dialogueText.text = currentFullText;
        isTyping = false;
        
        if (continueIndicator) continueIndicator.SetActive(true);
    }

    private void HandleAdvanceRequest()
    {
        if (isTyping)
        {
            // Se ainda está digitando, completa o texto imediatamente
            CompleteTextImmediately();
        }
        else
        {
            // Se já terminou de digitar, avança para a próxima linha
            DisplayNextLine();
        }
    }

    private void SkipDialogue()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        currentDialogueQueue.Clear();
        EndDialogue();
    }

    private void EndDialogue()
    {
        dialogueActive = false;
        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (continueIndicator) continueIndicator.SetActive(false);

        onDialogueComplete?.Invoke();
        onDialogueComplete = null;
        currentFullText = "";
    }

    /// <summary>
    /// Verifica se há diálogo ativo
    /// </summary>
    public bool IsDialogueActive()
    {
        return dialogueActive;
    }

    // ========== SISTEMA DE RETRATOS ==========

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

    private void ApplyPortrait(DialogueLine line)
    {
        if (speakerPortrait == null) return;

        Sprite spriteToUse = null;
        bool alignRight = false;
        bool found = false;

        // Tenta encontrar emoção específica primeiro
        if (!string.IsNullOrWhiteSpace(line.emotion) &&
            emotionMap.TryGetValue((line.speakerName, line.emotion), out var emo))
        {
            spriteToUse = emo.portraitSprite;
            alignRight = emo.useAlignOverride ? emo.alignRightOverride
                                              : (portraitMap.TryGetValue(line.speakerName, out var baseP) ? baseP.alignRight : false);
            found = spriteToUse != null;
        }
        // Se não encontrar emoção, usa o retrato padrão
        else if (portraitMap.TryGetValue(line.speakerName, out var basePortrait))
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
}