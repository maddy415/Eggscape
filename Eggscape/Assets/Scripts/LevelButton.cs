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

    void Start()
    {
        // Auto-setup se o manager estiver na cena
        manager = FindObjectOfType<LevelSelectManager>();
        
        if (manager != null)
        {
            RefreshFromSave();
        }
        else
        {
            Debug.LogWarning($"[LevelButton] LevelSelectManager não encontrado! Botão {levelIndex} não será configurado automaticamente.");
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

        // Configurar interatividade
        if (button != null)
        {
            button.interactable = unlocked;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClick);
        }

        Debug.Log($"[LevelButton] Fase {levelIndex} configurada - Unlocked: {unlocked}, Completed: {completed}");
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
    }

    /// <summary>
    /// Chamado quando o botão é clicado.
    /// </summary>
    private void OnButtonClick()
    {
        if (!isUnlocked)
        {
            Debug.LogWarning($"[LevelButton] Tentativa de clicar em fase bloqueada: {levelIndex}");
            // Opcional: tocar som de "erro" ou mostrar mensagem
            return;
        }

        if (manager != null && !string.IsNullOrEmpty(sceneName))
        {
            manager.LoadLevel(levelIndex, sceneName);
        }
        else
        {
            Debug.LogError($"[LevelButton] Manager ou sceneName não configurado! Fase {levelIndex}");
        }
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

        // Pegar cores do manager se existir
        if (manager != null)
        {
            Setup(levelIndex, unlocked, completed, manager);
            // Nota: as cores serão aplicadas pelo LevelSelectManager
        }
        else
        {
            // Fallback: usar cores padrão
            Setup(levelIndex, unlocked, completed, null);
            UpdateVisuals(unlocked, completed, Color.white, Color.gray, Color.green);
        }
    }

    // ==========================================
    //   MÉTODOS ÚTEIS (OPCIONAL)
    // ==========================================

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