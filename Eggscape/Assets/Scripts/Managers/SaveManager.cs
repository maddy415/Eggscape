using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gerenciador central de saves do jogo.
/// Singleton que persiste entre cenas e gerencia todo o progresso do jogador.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Configuração de Fases")]
    [SerializeField] private int totalLevels = 10; // Número total de fases no jogo

    private SaveData currentSave;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ==========================================
    //   CARREGAR E SALVAR
    // ==========================================

    /// <summary>
    /// Carrega o save do disco. Chamado automaticamente no Awake.
    /// </summary>
    public void LoadGame()
    {
        currentSave = SaveSystem.Load();
        
        // Garantir que o array tem o tamanho correto
        if (currentSave.levelsCompleted == null || currentSave.levelsCompleted.Length != totalLevels)
        {
            bool[] oldCompleted = currentSave.levelsCompleted;
            currentSave.levelsCompleted = new bool[totalLevels];
            
            // Copiar dados antigos se existirem
            if (oldCompleted != null)
            {
                for (int i = 0; i < Mathf.Min(oldCompleted.Length, totalLevels); i++)
                {
                    currentSave.levelsCompleted[i] = oldCompleted[i];
                }
            }
        }

        Debug.Log($"[SaveManager] Jogo carregado! Level Reached: {currentSave.levelReached}, HighScore: {currentSave.highScore}");
    }

    /// <summary>
    /// Salva o progresso atual no disco.
    /// </summary>
    public void SaveGame()
    {
        SaveSystem.Save(currentSave);
        Debug.Log($"[SaveManager] Progresso salvo! Level: {currentSave.currentLevel}, Score: {currentSave.highScore}");
    }

    // ==========================================
    //   GERENCIAMENTO DE PROGRESSO
    // ==========================================

    /// <summary>
    /// Marca uma fase como completada e desbloqueia a próxima.
    /// Chamado automaticamente quando o jogador vence.
    /// </summary>
    public void CompleteLevel(int levelIndex, int score)
    {
        // Marcar como completada
        if (levelIndex >= 0 && levelIndex < currentSave.levelsCompleted.Length)
        {
            currentSave.levelsCompleted[levelIndex] = true;
        }

        // Atualizar level reached
        if (levelIndex >= currentSave.levelReached)
        {
            currentSave.levelReached = levelIndex + 1; // Desbloqueia a próxima
        }

        // Atualizar highscore
        if (score > currentSave.highScore)
        {
            currentSave.highScore = score;
        }

        currentSave.currentLevel = levelIndex;

        SaveGame();
        Debug.Log($"[SaveManager] Fase {levelIndex} completada! Próxima fase desbloqueada: {currentSave.levelReached}");
    }

    /// <summary>
    /// Verifica se uma fase está desbloqueada.
    /// </summary>
    public bool IsLevelUnlocked(int levelIndex)
    {
        // Primeira fase sempre desbloqueada
        if (levelIndex == 0) return true;

        // Fase desbloqueada se chegou até ela
        return levelIndex <= currentSave.levelReached;
    }

    /// <summary>
    /// Verifica se uma fase foi completada.
    /// </summary>
    public bool IsLevelCompleted(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= currentSave.levelsCompleted.Length)
            return false;

        return currentSave.levelsCompleted[levelIndex];
    }

    // ==========================================
    //   GETTERS E SETTERS
    // ==========================================

    public int GetHighScore() => currentSave.highScore;
    public int GetLevelReached() => currentSave.levelReached;
    public int GetCurrentLevel() => currentSave.currentLevel;
    public SaveData GetCurrentSave() => currentSave;

    /// <summary>
    /// Reseta todo o progresso (útil para testes ou botão de reset).
    /// </summary>
    public void ResetProgress()
    {
        currentSave = new SaveData();
        currentSave.levelsCompleted = new bool[totalLevels];
        SaveGame();
        Debug.Log("[SaveManager] Progresso resetado!");
    }

    /// <summary>
    /// Deleta o arquivo de save completamente.
    /// </summary>
    public void DeleteSave()
    {
        SaveSystem.DeleteSave();
        currentSave = new SaveData();
        currentSave.levelsCompleted = new bool[totalLevels];
        Debug.Log("[SaveManager] Save deletado!");
    }

    // ==========================================
    //   MÉTODOS DE CONVENIÊNCIA
    // ==========================================

    /// <summary>
    /// Retorna o índice da cena pela build index.
    /// </summary>
    public int GetSceneBuildIndex(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Salva o índice da fase atual antes de carregá-la.
    /// </summary>
    public void SetCurrentLevel(int levelIndex)
    {
        currentSave.currentLevel = levelIndex;
        SaveGame();
    }
}