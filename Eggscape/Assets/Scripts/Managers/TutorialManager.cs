using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    // ========== REFERÊNCIAS ==========
    [Header("Referências de Cena")]
    public Player player;
    public TutorialEgg nerdEgg;
    public ObstacleGen obsGen;
    public GameObject objectGen;

    // ========== UI DE DIÁLOGO ==========
    [Header("UI de Diálogo")]
    public GameObject dialogueBox;
    public Image dialoguePanelImage;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI nameText;

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
    public float spawnIndex;

    [Header("Máquina de Escrever")]
    public float typingSpeed = 0.03f;
    public bool allowSkipTypingWithClick = true;
    public AudioSource typeBlip;

    // 🔒 NOVO: índices de falas onde não pode pular nem avançar
    [Header("Bloqueio de Skip")]
    [Tooltip("Diálogos nestes índices não podem ser pulados (nem pular digitação, nem avançar).")]
    public int[] nonSkippableIndices = { 3 };

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

    // ========== DIÁLOGOS ==========
    [System.Serializable]
    public struct DialogueLine
    {
        public string speaker;
        [TextArea(2, 4)] public string text;
    }

    [Header("Falas do Tutorial")]
    public DialogueLine[] dialogues =
    {
        new DialogueLine { speaker = "NerdEgg", text = "Chicken, precisamos correr, as outras galinhas estão nos esperando!" },
        new DialogueLine { speaker = "NerdEgg", text = "Primeiro, vou te ensinar a pular troncos. Tem um vindo aí, aperte 'Espaço' para pular!" },
        new DialogueLine { speaker = "Chicken", text = "Entendido! Estou pronta!" },
        new DialogueLine { speaker = "NerdEgg", text = "Perfeito. Agora, siga em frente sem hesitar!" } // <- exemplo bloqueado
    };

    private void Start()
    {
        if (player == null) player = GameObject.FindWithTag("Player")?.GetComponent<Player>();
        if (nerdEgg == null) nerdEgg = GameObject.FindWithTag("TutorialEgg")?.GetComponent<TutorialEgg>();
        if (obsGen == null && objectGen != null) obsGen = objectGen.GetComponent<ObstacleGen>();

        if (dialogueBox != null) dialogueBox.SetActive(false);
        ApplyStyleOnce();
    }

    private void Update()
    {
        HandlePlayerMovement();
        HandleWalkingCutscene();
        HandleDialogueDisplay();
        HandleInput();
    }

    private void HandlePlayerMovement()
    {
        player.CanMove = !onCutscene;
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
        // se o índice atual estiver bloqueado, ignora qualquer clique
        if (IsCurrentDialogueLocked()) return;

        if (Input.GetMouseButtonDown(0))
        {
            // Se ainda está digitando, pular a digitação (se permitido)
            if (isTyping && allowSkipTypingWithClick)
            {
                skipTyping = true;
                return;
            }

            // Avançar para a próxima fala
            if (firstDialogueShown)
            {
                currentIndex++;
                if (currentIndex < dialogues.Length)
                {
                    ShowDialogue(currentIndex);

                    if (currentIndex == spawnIndex && !hasSpawned)
                        StartCoroutine(SpawnDelay());
                }
                else
                {
                    CloseDialogueBox();
                }
            }
        }
    }

    private void ShowDialogue(int index)
    {
        if (index < 0 || index >= dialogues.Length) return;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        if (nameText != null)
        {
            bool hasName = !string.IsNullOrWhiteSpace(dialogues[index].speaker);
            nameText.gameObject.SetActive(hasName);
            if (hasName) nameText.text = dialogues[index].speaker;
        }

        dialogueText.text = "";
        typingCoroutine = StartCoroutine(TypeText(dialogues[index].text));
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

    private IEnumerator SpawnDelay()
    {
        onCutscene = false;
        yield return new WaitForSeconds(spawnTime);
        if (obsGen != null) obsGen.SpawnObstacle();
        hasSpawned = true;
    }

    private void OpenDialogueBox()
    {
        if (dialogueBox != null && !dialogueBox.activeSelf)
            dialogueBox.SetActive(true);
    }

    private void CloseDialogueBox()
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

    // ========= NOVO: função utilitária =========
    private bool IsCurrentDialogueLocked()
    {
        foreach (int idx in nonSkippableIndices)
            if (currentIndex == idx) return true;
        return false;
    }
}
