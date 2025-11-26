using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public static MenuManager instance;

    [Header("UI")]
    [SerializeField] private GameObject settingsRoot;   // painel de configurações
    
    [Header("Botões Progressivos (aparecem após tutorial)")]
    [SerializeField] private GameObject continueButton;     // Botão "Continue" - OCULTO inicialmente
    [SerializeField] private GameObject levelSelectButton;  // Botão "Level Select" - OCULTO inicialmente
    
    [Header("Botões Sempre Visíveis")]
    [SerializeField] private Button newGameButton;          // Botão "New Game" - sempre visível

    [Header("Configuração")]
    [SerializeField] private string storySceneName = "story";  
    [SerializeField] private string controlsSceneName = "controls";      // Nome da cena de lore
    // Nome da cena de lore
    [SerializeField] private string tutorialSceneName = "tutorial"; // Nome da cena de tutorial
    [SerializeField] private int tutorialSceneIndex = 1;            // Índice do tutorial no Build Settings

    private SceneTransition transition;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        transition = FindObjectOfType<SceneTransition>();

        // Configura settings
        if (settingsRoot != null) settingsRoot.SetActive(false);
    }

    void Start()
    {
        RefreshMenuButtons();
        
    }

    // =====================================================
    //   SISTEMA DE DESBLOQUEIO PROGRESSIVO DO MENU
    // =====================================================

    /// <summary>
    /// Atualiza a visibilidade dos botões baseado no progresso do save.
    /// </summary>
    private void RefreshMenuButtons()
    {
        bool hasSaveManager = SaveManager.Instance != null;
    
        if (!hasSaveManager)
        {
            ShowOnlyNewGameMenu();
            return;
        }

        int levelReached = SaveManager.Instance.GetLevelReached();
    
        // ⭐ MUDANÇA: Agora compara com Level Index 0 (primeira fase)
        bool passedTutorial = levelReached > 0; // Se desbloqueou pelo menos a Fase 1
    
        if (passedTutorial)
        {
            ShowFullMenu();
        }
        else
        {
            ShowOnlyNewGameMenu();
        }
    }
    
    /// <summary>
    /// Mostra apenas o botão New Game (primeira vez jogando).
    /// </summary>
    private void ShowOnlyNewGameMenu()
    {
        if (continueButton != null) 
            continueButton.SetActive(false);
        
        if (levelSelectButton != null) 
            levelSelectButton.SetActive(false);
        
        if (newGameButton != null) 
            newGameButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// Mostra menu completo (Continue + Level Select + New Game).
    /// </summary>
    private void ShowFullMenu()
    {
        if (continueButton != null) 
            continueButton.SetActive(true);
        
        if (levelSelectButton != null) 
            levelSelectButton.SetActive(true);
        
        if (newGameButton != null) 
            newGameButton.gameObject.SetActive(true);
    }

    // =====================================================
    //   NAVEGAÇÃO DO MENU
    // =====================================================

    /// <summary>
    /// New Game - sempre vai para a cena de lore (story).
    /// </summary>
    public void NewGame()
    {
        // Reseta o progresso para começar do zero
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ResetProgress();
            Debug.Log("[MenuManager] Novo jogo iniciado - progresso resetado.");
        }

        LoadSceneByName(storySceneName);
    }
    
    public void LoadControlsGame()
    {
        LoadSceneByName(controlsSceneName);
    }

    /// <summary>
    /// Continue - vai para a última fase que o jogador parou.
    /// </summary>
    // ... (mantenha todo o código anterior até os métodos de navegação)

    /// <summary>
    /// Continue - vai para a última fase que o jogador parou.
    /// </summary>
    public void Continue()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("[MenuManager] SaveManager não encontrado!");
            NewGame();
            return;
        }

        int lastLevel = SaveManager.Instance.GetLevelReached();
        string sceneName = GetSceneNameByIndex(lastLevel);

        if (!string.IsNullOrEmpty(sceneName))
        {
            // ===== NOVO: Limpa preservação ao ir para outra fase =====
            if (AudioManager.audioInstance != null)
            {
                AudioManager.audioInstance.ClearPreservedMusicPosition();
            }
            
            LoadSceneByName(sceneName);
            Debug.Log($"[MenuManager] Continue: Carregando {sceneName} (Index: {lastLevel})");
        }
        else
        {
            Debug.LogWarning($"[MenuManager] Cena com índice {lastLevel} não encontrada!");
            NewGame();
        }
    }

    // ===== MÉTODO ORIGINAL LOADGAME (atualizado) =====
    public void LoadGame()
    {
        // ===== NOVO: Limpa preservação ao carregar nova fase =====
        if (AudioManager.audioInstance != null)
        {
            AudioManager.audioInstance.ClearPreservedMusicPosition();
        }
        
        LoadSceneByName("lvl_1");
    }

    public void LoadStory()
    {
        // ===== NOVO: Limpa preservação =====
        if (AudioManager.audioInstance != null)
        {
            AudioManager.audioInstance.ClearPreservedMusicPosition();
        }
        
        LoadSceneByName(storySceneName);
    }

    public void LoadTutorial()
    {
        // ===== NOVO: Limpa preservação =====
        if (AudioManager.audioInstance != null)
        {
            AudioManager.audioInstance.ClearPreservedMusicPosition();
        }
        
        LoadSceneByName(tutorialSceneName);
    }

    public void LoadGame2()
    {
        // ===== NOVO: Limpa preservação =====
        if (AudioManager.audioInstance != null)
        {
            AudioManager.audioInstance.ClearPreservedMusicPosition();
        }
        
        LoadSceneByName("lvl_2");
    }

// ... (resto do código permanece igual)
    // =====================================================
    //   CONFIGURAÇÕES
    // =====================================================

    public void OpenSettings()
    {
        if (settingsRoot != null)
            settingsRoot.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsRoot != null)
            settingsRoot.SetActive(false);
    }

    // =====================================================
    //   HELPERS
    // =====================================================

    public void LoadSceneByName(string sceneName)
    {
        if (transition != null) 
            transition.LoadScene(sceneName);
        else 
            SceneManager.LoadScene(sceneName);

        Debug.Log($"[MenuManager] {sceneName} carregado");
    }
    
    public void QuitGame()
    {
        Application.Quit();
    }

    private string GetSceneNameByIndex(int index)
    {
        if (index < 0 || index >= SceneManager.sceneCountInBuildSettings)
            return null;

        string path = SceneUtility.GetScenePathByBuildIndex(index);
        return System.IO.Path.GetFileNameWithoutExtension(path);
    }

    // =====================================================
    //   DEBUG (EDITOR ONLY)
    // =====================================================

#if UNITY_EDITOR
    [ContextMenu("Debug: Simular Primeira Vez")]
    private void DebugSimulateFirstTime()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ResetProgress();
            RefreshMenuButtons();
            Debug.Log("[DEBUG] Progresso resetado - simulando primeira vez!");
        }
    }

    [ContextMenu("Debug: Simular Veterano")]
    private void DebugSimulateVeteran()
    {
        if (SaveManager.Instance != null)
        {
            // Simula que passou do tutorial
            SaveData save = SaveManager.Instance.GetCurrentSave();
            save.levelReached = tutorialSceneIndex + 1; // Passa do tutorial
            SaveManager.Instance.SaveGame();
            RefreshMenuButtons();
            Debug.Log("[DEBUG] Simulando veterano - Continue/Level Select ativos!");
        }
    }

    [ContextMenu("Debug: Refresh Menu")]
    private void DebugRefreshMenu()
    {
        RefreshMenuButtons();
        Debug.Log("[DEBUG] Menu atualizado manualmente!");
    }
#endif
}