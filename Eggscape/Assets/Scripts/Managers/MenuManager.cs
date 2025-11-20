using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public static MenuManager instance;

    [Header("UI")]
    [SerializeField] private GameObject settingsRoot;   // painel de configurações
    
    [Header("Botões do Menu Principal")]
    [SerializeField] private Button continueButton;     // Botão "Continue"
    [SerializeField] private Button newGameButton;      // Botão "New Game"
    [SerializeField] private Button levelSelectButton;  // Botão "Level Select"

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

        // Configura só o settings:
        if (settingsRoot != null) settingsRoot.SetActive(false);
    }

    void Start()
    {
        RefreshContinueButton();
    }

    // =====================================================
    //   NOVO: SISTEMA DE CONTINUE
    // =====================================================

    /// <summary>
    /// Atualiza a visibilidade/interatividade do botão Continue.
    /// </summary>
    private void RefreshContinueButton()
    {
        if (continueButton == null) return;

        // Se existe save e o jogador passou da primeira fase
        bool hasSave = SaveManager.Instance != null && SaveManager.Instance.GetLevelReached() > 0;

        continueButton.gameObject.SetActive(hasSave);
        continueButton.interactable = hasSave;

        Debug.Log($"[MenuManager] Continue button: {(hasSave ? "ATIVO" : "INATIVO")}");
    }

    /// <summary>
    /// Continua do último nível jogado.
    /// </summary>
    public void Continue()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("[MenuManager] SaveManager não encontrado!");
            LoadGame(); // Fallback para primeira fase
            return;
        }

        int lastLevel = SaveManager.Instance.GetLevelReached();
        
        // Se completou tudo, volta para a última fase
        // Se não, carrega a próxima fase não completada
        string sceneName = GetSceneNameByIndex(lastLevel);

        if (!string.IsNullOrEmpty(sceneName))
        {
            if (transition != null) 
                transition.LoadScene(sceneName);
            else 
                SceneManager.LoadScene(sceneName);

            Debug.Log($"[MenuManager] Continue: Carregando {sceneName} (Index: {lastLevel})");
        }
        else
        {
            Debug.LogWarning($"[MenuManager] Cena com índice {lastLevel} não encontrada!");
            LoadGame(); // Fallback
        }
    }

    /// <summary>
    /// Abre o menu de seleção de fases.
    /// </summary>
    public void OpenLevelSelect()
    {
        if (transition != null) 
            transition.LoadScene("LevelSelect"); // Nome da sua cena de seleção
        else 
            SceneManager.LoadScene("LevelSelect");

        Debug.Log("[MenuManager] Level Select aberto");
    }

    // =====================================================
    //   MÉTODOS ORIGINAIS
    // =====================================================

    public void LoadGame()
    {
        if (transition != null) transition.LoadScene("lvl_1");
        else SceneManager.LoadScene("lvl_1");

        Debug.Log("lvl_1 Loaded");
    }

    public void LoadStory()
    {
        if (transition != null) transition.LoadScene("story");
        else SceneManager.LoadScene("story");

        Debug.Log("story Loaded");
    }

    public void LoadTutorial()
    {
        if (transition != null) transition.LoadScene("tutorial");
        else SceneManager.LoadScene("tutorial");

        Debug.Log("tutorial Loaded");
    }

    public void LoadGame2()
    {
        if (transition != null) transition.LoadScene("lvl_2");
        else SceneManager.LoadScene("lvl_2");

        Debug.Log("lvl_2 Loaded");
    }

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

    /// <summary>
    /// Retorna o nome da cena baseado no índice do Build Settings.
    /// </summary>
    private string GetSceneNameByIndex(int index)
    {
        if (index < 0 || index >= SceneManager.sceneCountInBuildSettings)
            return null;

        string path = SceneUtility.GetScenePathByBuildIndex(index);
        return System.IO.Path.GetFileNameWithoutExtension(path);
    }

    // =====================================================
    //   DEBUG (OPCIONAL)
    // =====================================================

#if UNITY_EDITOR
    [ContextMenu("Reset Save")]
    private void DebugResetSave()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ResetProgress();
            RefreshContinueButton();
            Debug.Log("[MenuManager] Save resetado!");
        }
    }
#endif
}