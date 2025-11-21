using UnityEngine;

/// <summary>
/// Converte entre Build Index (posição no Build Settings) e Level Index (número da fase para o jogador).
/// Use este sistema se suas fases não começam no índice 0 do Build Settings.
/// </summary>
public static class LevelIndexMapper
{
    // ====================================
    //   CONFIGURAÇÃO (AJUSTE AQUI!)
    // ====================================
    
    /// <summary>
    /// Build Index da PRIMEIRA fase jogável (lvl_1).
    /// No seu caso, provavelmente é 3 (depois de MainMenu, story, tutorial).
    /// </summary>
    public const int FIRST_LEVEL_BUILD_INDEX = 3;

    // ====================================
    //   MÉTODOS DE CONVERSÃO
    // ====================================

    /// <summary>
    /// Converte Build Index → Level Index
    /// Exemplo: Build Index 3 (lvl_1) → Level Index 0
    /// </summary>
    public static int BuildIndexToLevelIndex(int buildIndex)
    {
        return buildIndex - FIRST_LEVEL_BUILD_INDEX;
    }

    /// <summary>
    /// Converte Level Index → Build Index
    /// Exemplo: Level Index 0 → Build Index 3 (lvl_1)
    /// </summary>
    public static int LevelIndexToBuildIndex(int levelIndex)
    {
        return levelIndex + FIRST_LEVEL_BUILD_INDEX;
    }

    /// <summary>
    /// Verifica se um Build Index corresponde a uma fase jogável.
    /// </summary>
    public static bool IsBuildIndexALevel(int buildIndex)
    {
        return buildIndex >= FIRST_LEVEL_BUILD_INDEX;
    }

    /// <summary>
    /// Retorna o Level Index da cena atual (ou -1 se não for uma fase).
    /// </summary>
    public static int GetCurrentLevelIndex()
    {
        int buildIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        
        if (IsBuildIndexALevel(buildIndex))
            return BuildIndexToLevelIndex(buildIndex);
        
        return -1; // Não é uma fase (é menu, story, tutorial...)
    }

    // ====================================
    //   DEBUG (OPCIONAL)
    // ====================================

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Level Mapper - Show Current Mapping")]
    private static void ShowCurrentMapping()
    {
        int totalScenes = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
        
        Debug.Log("=== MAPEAMENTO DE FASES ===");
        Debug.Log($"Primeira fase jogável: Build Index {FIRST_LEVEL_BUILD_INDEX}");
        Debug.Log("");
        
        for (int i = 0; i < totalScenes; i++)
        {
            string path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            
            if (IsBuildIndexALevel(i))
            {
                int levelIndex = BuildIndexToLevelIndex(i);
                Debug.Log($"Build Index {i}: {name} → Level Index {levelIndex} (Fase {levelIndex + 1})");
            }
            else
            {
                Debug.Log($"Build Index {i}: {name} → (Não é fase jogável)");
            }
        }
    }
#endif
}