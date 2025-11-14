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

    [Header("Typewriter Settings")]
    public float typingSpeed = 0.05f;
    public AudioClip typingSound;
    public float typingSoundInterval = 0.1f;

    [Header("Input")]
    public KeyCode advanceKey = KeyCode.Space;
    public KeyCode skipKey = KeyCode.Return;

    private Queue<DialogueLine> currentDialogueQueue;
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private bool dialogueActive = false;
    private Action onDialogueComplete;

    private AudioSource audioSource;

    [System.Serializable]
    public class DialogueLine
    {
        public string speakerName;
        [TextArea(3, 6)]
        public string text;
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
    }

    private void Update()
    {
        if (!dialogueActive) return;

        if (Input.GetKeyDown(advanceKey))
        {
            if (isTyping)
            {
                // Completa o texto imediatamente
                CompleteText();
            }
            else
            {
                // Avança para próxima linha
                DisplayNextLine();
            }
        }

        if (Input.GetKeyDown(skipKey))
        {
            SkipDialogue();
        }
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

        if (speakerNameText)
        {
            speakerNameText.text = line.speakerName;
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

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

    private void CompleteText()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // Mostra o texto completo
        DialogueLine currentLine = new DialogueLine();
        if (dialogueText.text.Length < 500) // Proteção
        {
            isTyping = false;
            if (continueIndicator) continueIndicator.SetActive(true);
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
    }

    /// <summary>
    /// Verifica se há diálogo ativo
    /// </summary>
    public bool IsDialogueActive()
    {
        return dialogueActive;
    }
}