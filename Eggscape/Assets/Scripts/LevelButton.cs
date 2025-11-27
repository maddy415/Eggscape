using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Componente individual de cada botão de fase.
/// Gerencia a aparência e interação de um botão no menu de seleção.
/// </summary>
public class LevelButton : MonoBehaviour
{
    [Header("Configuração da Fase")]
    public int levelIndex;                          // Índice da fase (0, 1, 2...)
    public string sceneName;                        // Nome da cena a carregar (ex: "lvl_1")

    [Header("Componentes UI")]
    public Button button;
    public Image buttonImage;
    public TextMeshProUGUI levelText;               // Texto "Fase 1", "Fase 2"...
    public GameObject lockIcon;                     // Ícone de cadeado (opcional)
    public GameObject checkIcon;                    // Ícone de checkmark (opcional)

    [Header("Cores (Opcional - sobrescreve LevelSelectManager)")]
    public bool useCustomColors = false;
    public Color customUnlockedColor = Color.white;
    public Color customLockedColor = Color.gray;
    public Color customCompletedColor = Color.green;

    // Estado interno
    private bool isUnlocked;
    private bool isCompleted;
    private LevelSelectManager manager;
    private bool isInitialized = false;

    void Awake()
    {
        // Verificar componentes obrigatórios
        if (button == null)
        {
            button = GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError($"[LevelButton] Botão {levelIndex} ({gameObject.name}) não tem componente Button!", this);
            }
        }

        if (buttonImage == null)
        {
            buttonImage = GetComponent<Image>();
        }

        // Adicionar listener uma única vez no Awake
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClick);
            Debug.Log($"[LevelButton] Listener adicionado ao botão {levelIndex} ({gameObject.name})");
        }
    }

    void Start()
    {
        // NÃO inicializar aqui - deixar o LevelSelectManager fazer isso
        // Apenas garantir que temos referência ao manager
        if (manager == null)
        {
            manager = FindObjectOfType<LevelSelectManager>();
        }
    }

    /// <summary>
    /// Atualiza o estado do botão baseado no save.
    /// Chamado automaticamente pelo LevelSelectManager.
    /// </summary>
    public void Setup(int index, bool unlocked, bool completed, LevelSelectManager selectManager)
    {
        levelIndex = index;
        isUnlocked = unlocked;
        isCompleted = completed;
        manager = selectManager;
        isInitialized = true;

        // Configurar interatividade
        if (button != null)
        {
            button.interactable = unlocked;
            
            // NÃO remover listeners aqui - eles já foram adicionados no Awake
            // Apenas garantir que existe um
            if (button.onClick.GetPersistentEventCount() == 0)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnButtonClick);
            }
        }

        Debug.Log($"[LevelButton] Fase {levelIndex} ({gameObject.name}) configurada - Unlocked: {unlocked}, Completed: {completed}, Button Interactable: {button?.interactable}");
    }

    /// <summary>
    /// Atualiza a aparência visual do botão.
    /// </summary>
    public void UpdateVisuals(bool unlocked, bool completed, Color unlockedCol, Color lockedCol, Color completedCol)
    {
        isUnlocked = unlocked;
        isCompleted = completed;

        // Usar cores customizadas se habilitado
        if (useCustomColors)
        {
            unlockedCol = customUnlockedColor;
            lockedCol = customLockedColor;
            completedCol = customCompletedColor;
        }

        // Atualizar cor do botão
        if (buttonImage != null)
        {
            if (completed)
                buttonImage.color = completedCol;
            else if (unlocked)
                buttonImage.color = unlockedCol;
            else
                buttonImage.color = lockedCol;
        }

        // Ícones
        if (lockIcon != null)
            lockIcon.SetActive(!unlocked);

        if (checkIcon != null)
            checkIcon.SetActive(completed);

        // Texto do nível
        if (levelText != null)
        {
            if (unlocked)
                levelText.text = $"Fase {levelIndex + 1}";
            else
                levelText.text = "???";
        }

        Debug.Log($"[LevelButton] Visuais atualizados para fase {levelIndex} - Unlocked: {unlocked}, Completed: {completed}");
    }

    /// <summary>
    /// Chamado quando o botão é clicado.
    /// </summary>
    private void OnButtonClick()
    {
        Debug.Log($"[LevelButton] Botão {levelIndex} ({gameObject.name}) CLICADO! Unlocked: {isUnlocked}, Initialized: {isInitialized}");

        if (!isInitialized)
        {
            Debug.LogError($"[LevelButton] Botão {levelIndex} não foi inicializado pelo LevelSelectManager!");
            return;
        }

        if (!isUnlocked)
        {
            Debug.LogWarning($"[LevelButton] Fase {levelIndex} está bloqueada!");
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"[LevelButton] sceneName não configurado para fase {levelIndex}!");
            return;
        }

        if (manager == null)
        {
            Debug.LogError($"[LevelButton] Manager não encontrado! Tentando encontrar...");
            manager = FindObjectOfType<LevelSelectManager>();
            
            if (manager == null)
            {
                Debug.LogError($"[LevelButton] LevelSelectManager não existe na cena!");
                return;
            }
        }

        Debug.Log($"[LevelButton] Carregando fase {levelIndex} - Cena: {sceneName}");
        manager.LoadLevel(levelIndex, sceneName);
    }

    /// <summary>
    /// Atualiza o botão diretamente do SaveManager (útil para refresh manual).
    /// </summary>
    public void RefreshFromSave()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning($"[LevelButton] SaveManager não encontrado! Botão {levelIndex} não atualizado.");
            return;
        }

        bool unlocked = SaveManager.Instance.IsLevelUnlocked(levelIndex);
        bool completed = SaveManager.Instance.IsLevelCompleted(levelIndex);

        if (manager == null)
        {
            manager = FindObjectOfType<LevelSelectManager>();
        }

        Setup(levelIndex, unlocked, completed, manager);
        
        // Aplicar visuais com cores padrão
        UpdateVisuals(unlocked, completed, Color.white, Color.gray, Color.green);
    }

    // ==========================================
    //   MÉTODOS DE DEBUG
    // ==========================================

    /// <summary>
    /// Testa se o botão está funcionando.
    /// </summary>
    [ContextMenu("Debug: Test Click")]
    public void DebugTestClick()
    {
        Debug.Log($"=== DEBUG CLICK - Fase {levelIndex} ===");
        Debug.Log($"GameObject: {gameObject.name}");
        Debug.Log($"Button existe: {button != null}");
        Debug.Log($"Button interactable: {button?.interactable}");
        Debug.Log($"isUnlocked: {isUnlocked}");
        Debug.Log($"isCompleted: {isCompleted}");
        Debug.Log($"isInitialized: {isInitialized}");
        Debug.Log($"sceneName: {sceneName}");
        Debug.Log($"Manager existe: {manager != null}");
        Debug.Log($"Listeners count: {button?.onClick.GetPersistentEventCount()}");
        
        OnButtonClick();
    }

    /// <summary>
    /// Força o desbloqueio do botão (útil para testes).
    /// </summary>
    [ContextMenu("Debug: Unlock This Level")]
    public void DebugUnlock()
    {
        isUnlocked = true;
        if (button != null) button.interactable = true;
        UpdateVisuals(true, isCompleted, Color.white, Color.gray, Color.green);
        Debug.Log($"[LevelButton] Fase {levelIndex} desbloqueada manualmente!");
    }

    /// <summary>
    /// Marca a fase como completada (útil para testes).
    /// </summary>
    [ContextMenu("Debug: Mark As Completed")]
    public void DebugComplete()
    {
        isCompleted = true;
        UpdateVisuals(isUnlocked, true, Color.white, Color.gray, Color.green);
        Debug.Log($"[LevelButton] Fase {levelIndex} marcada como completa!");
    }
}