using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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

    private SceneTransition transition;

    void Start()
    {
        transition = FindObjectOfType<SceneTransition>();
        RefreshLevelButtons();
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

        for (int i = 0; i < levelButtons.Length; i++)
        {
            bool unlocked = SaveManager.Instance.IsLevelUnlocked(i);
            bool completed = SaveManager.Instance.IsLevelCompleted(i);

            levelButtons[i].Setup(i, unlocked, completed, this);
            levelButtons[i].UpdateVisuals(unlocked, completed, unlockedColor, lockedColor, completedColor);
        }

        Debug.Log($"[LevelSelect] Botões atualizados! Fases desbloqueadas até: {SaveManager.Instance.GetLevelReached()}");
    }

    /// <summary>
    /// Carrega uma fase específica (chamado pelos botões).
    /// </summary>
    public void LoadLevel(int levelIndex, string sceneName)
    {
        if (!SaveManager.Instance.IsLevelUnlocked(levelIndex))
        {
            Debug.LogWarning($"[LevelSelect] Tentativa de carregar fase bloqueada: {levelIndex}");
            return;
        }

        SaveManager.Instance.SetCurrentLevel(levelIndex);

        if (transition != null)
            transition.LoadScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);

        Debug.Log($"[LevelSelect] Carregando fase: {sceneName} (Index: {levelIndex})");
    }

    /// <summary>
    /// Volta para o menu principal.
    /// </summary>
    public void BackToMainMenu()
    {
        if (transition != null)
            transition.LoadScene("MainMenu"); // Ajuste o nome da sua cena de menu principal
        else
            SceneManager.LoadScene("MainMenu");
    }
}

// ==========================================
//   CLASSE DO BOTÃO DE FASE
// ==========================================

/*
[System.Serializable]
public class LevelButton : MonoBehaviour
{
    [Header("Configuração")]
    public int levelIndex;                          // Índice da fase (0, 1, 2...)
    public string sceneName;                        // Nome da cena a carregar (ex: "lvl_1")

    [Header("Componentes UI")]
    public Button button;
    public Image buttonImage;
    public TextMeshProUGUI levelText;               // Texto "Fase 1", "Fase 2"...
    public GameObject lockIcon;                     // Ícone de cadeado (opcional)
    public GameObject checkIcon;                    // Ícone de checkmark (opcional)

    private bool isUnlocked;
    private bool isCompleted;
    private LevelSelectManager manager;

    /// <summary>
    /// Inicializa o botão com suas informações.
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
    }

    /// <summary>
    /// Atualiza a aparência visual do botão.
    /// </summary>
    public void UpdateVisuals(bool unlocked, bool completed, Color unlockedCol, Color lockedCol, Color completedCol)
    {
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
            levelText.text = unlocked ? $"Fase {levelIndex + 1}" : "???";
        }
    }

    /// <summary>
    /// Chamado quando o botão é clicado.
    /// </summary>
    private void OnButtonClick()
    {
        if (isUnlocked && manager != null)
        {
            manager.LoadLevel(levelIndex, sceneName);
        }
    }
*/

