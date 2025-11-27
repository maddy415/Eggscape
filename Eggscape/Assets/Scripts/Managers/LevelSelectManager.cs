using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

/// <summary>
/// Gerencia o menu de seleção de fases.
/// Desbloqueia fases baseado no progresso salvo.
/// </summary>
public class LevelSelectManager : MonoBehaviour
{
    [Header("Configuração de Botões")]
    [SerializeField] private LevelButton[] levelButtons; // Array de botões de fase

    [Header("Visual")]
    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private Color lockedColor = Color.gray;
    [SerializeField] private Color completedColor = Color.green;

    [Header("Layout Fix")]
    [SerializeField] private bool forceLayoutRebuild = true;
    [SerializeField] private RectTransform layoutContainer; // Container com Layout Group (opcional)

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private SceneTransition transition;

    void Awake()
    {
        // Encontrar ou buscar componentes
        transition = FindObjectOfType<SceneTransition>();
        
        // Se não especificou manualmente, buscar todos os LevelButtons na cena
        if (levelButtons == null || levelButtons.Length == 0)
        {
            levelButtons = FindObjectsOfType<LevelButton>();
            Debug.Log($"[LevelSelect] Encontrados automaticamente {levelButtons.Length} botões na cena");
        }
    }

    void Start()
    {
        // Corrigir layout antes de inicializar os botões
        if (forceLayoutRebuild)
        {
            StartCoroutine(InitializeWithLayoutFix());
        }
        else
        {
            RefreshLevelButtons();
        }
    }

    /// <summary>
    /// Inicializa os botões após corrigir o layout.
    /// </summary>
    private IEnumerator InitializeWithLayoutFix()
    {
        // Forçar rebuild do Canvas
        Canvas.ForceUpdateCanvases();

        // Se tem um container específico, rebuildar ele
        if (layoutContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutContainer);
        }
        else
        {
            // Tentar encontrar automaticamente
            LayoutGroup[] layoutGroups = GetComponentsInChildren<LayoutGroup>();
            foreach (var layout in layoutGroups)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(layout.GetComponent<RectTransform>());
            }
        }

        // Aguardar 2 frames para garantir que tudo está renderizado
        yield return null;
        yield return null;

        // Agora inicializar os botões
        RefreshLevelButtons();

        // Forçar rebuild final
        Canvas.ForceUpdateCanvases();

        if (showDebugLogs)
        {
            Debug.Log("[LevelSelect] Layout reconstruído e botões inicializados!");
        }
    }

    /// <summary>
    /// Atualiza o estado visual de todos os botões baseado no save.
    /// </summary>
    public void RefreshLevelButtons()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("[LevelSelect] SaveManager não encontrado! Crie um GameObject com SaveManager na cena.");
            return;
        }

        if (levelButtons == null || levelButtons.Length == 0)
        {
            Debug.LogError("[LevelSelect] Nenhum botão de fase configurado!");
            return;
        }

        Debug.Log($"[LevelSelect] Atualizando {levelButtons.Length} botões...");

        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (levelButtons[i] == null)
            {
                Debug.LogWarning($"[LevelSelect] Botão no índice {i} está null!");
                continue;
            }

            int buttonIndex = levelButtons[i].levelIndex;
            bool unlocked = SaveManager.Instance.IsLevelUnlocked(buttonIndex);
            bool completed = SaveManager.Instance.IsLevelCompleted(buttonIndex);

            levelButtons[i].Setup(buttonIndex, unlocked, completed, this);
            levelButtons[i].UpdateVisuals(unlocked, completed, unlockedColor, lockedColor, completedColor);

            if (showDebugLogs)
            {
                Debug.Log($"[LevelSelect] Botão {i} (Fase {buttonIndex}): Unlocked={unlocked}, Completed={completed}");
            }
        }

        Debug.Log($"[LevelSelect] ✓ Botões atualizados! Progresso: Fase {SaveManager.Instance.GetLevelReached()}/{levelButtons.Length}");
    }

    /// <summary>
    /// Carrega uma fase específica (chamado pelos botões).
    /// </summary>
    public void LoadLevel(int levelIndex, string sceneName)
    {
        Debug.Log($"[LevelSelect] LoadLevel chamado - Index: {levelIndex}, Scene: {sceneName}");

        if (SaveManager.Instance == null)
        {
            Debug.LogError("[LevelSelect] SaveManager não encontrado!");
            return;
        }

        if (!SaveManager.Instance.IsLevelUnlocked(levelIndex))
        {
            Debug.LogWarning($"[LevelSelect] Tentativa de carregar fase bloqueada: {levelIndex}");
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"[LevelSelect] Nome da cena vazio para fase {levelIndex}!");
            return;
        }

        SaveManager.Instance.SetCurrentLevel(levelIndex);

        Debug.Log($"[LevelSelect] ✓ Carregando fase {levelIndex}: {sceneName}");

        if (transition != null)
        {
            transition.LoadScene(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    /// <summary>
    /// Volta para o menu principal.
    /// </summary>
    public void BackToMainMenu()
    {
        if (transition != null)
            transition.LoadScene("MainMenu");
        else
            SceneManager.LoadScene("MainMenu");
    }

    // ==========================================
    //   MÉTODOS DE DEBUG
    // ==========================================

    [ContextMenu("Debug: Force Layout Rebuild")]
    public void DebugForceLayoutRebuild()
    {
        StartCoroutine(InitializeWithLayoutFix());
    }

    [ContextMenu("Debug: List All Buttons")]
    public void DebugListButtons()
    {
        Debug.Log("=== LISTA DE BOTÕES ===");
        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (levelButtons[i] != null)
            {
                var btn = levelButtons[i];
                var rectTransform = btn.GetComponent<RectTransform>();
                Debug.Log($"Botão {i}: Name={btn.gameObject.name}, LevelIndex={btn.levelIndex}, Scene={btn.sceneName}, " +
                         $"Position={rectTransform.anchoredPosition}, Size={rectTransform.sizeDelta}, HasButton={btn.button != null}");
            }
            else
            {
                Debug.Log($"Botão {i}: NULL");
            }
        }
    }

    [ContextMenu("Debug: Check Raycast Targets")]
    public void DebugCheckRaycastTargets()
    {
        Debug.Log("=== VERIFICAÇÃO DE RAYCAST TARGETS ===");
        foreach (var btn in levelButtons)
        {
            if (btn != null)
            {
                Image[] images = btn.GetComponentsInChildren<Image>();
                Debug.Log($"Botão {btn.levelIndex} ({btn.gameObject.name}):");
                foreach (var img in images)
                {
                    Debug.Log($"  - Image em '{img.gameObject.name}': Raycast Target = {img.raycastTarget}");
                }
            }
        }
    }

    [ContextMenu("Debug: Unlock All Levels")]
    public void DebugUnlockAll()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager não encontrado!");
            return;
        }

        for (int i = 0; i < levelButtons.Length; i++)
        {
            SaveManager.Instance.CompleteLevel(i, 0);
        }

        RefreshLevelButtons();
        Debug.Log("[LevelSelect] Todas as fases desbloqueadas!");
    }

    [ContextMenu("Debug: Reset Progress")]
    public void DebugResetProgress()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ResetProgress();
            RefreshLevelButtons();
            Debug.Log("[LevelSelect] Progresso resetado!");
        }
    }
}