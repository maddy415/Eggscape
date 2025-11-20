using System.IO;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public int highScore;
    public int levelReached;              // Última fase que o jogador chegou
    public bool[] levelsCompleted;        // Quais fases foram completadas
    public int currentLevel;              // Fase atual do jogador
    public float totalPlayTime;           // Tempo total de jogo (opcional)

    // Construtor para inicializar com valores padrão
    public SaveData()
    {
        highScore = 0;
        levelReached = 0;
        currentLevel = 0;
        levelsCompleted = new bool[10]; // Ajuste conforme o número de fases
        totalPlayTime = 0f;
    }
}

public static class SaveSystem
{
    private static string filename = "/save.json";
    private static string PathFull => Application.persistentDataPath + filename;

    public static void Save(SaveData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(PathFull, json);
            Debug.Log($"[SaveSystem] Save feito em: {PathFull}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[SaveSystem] Erro ao salvar: " + ex.Message);
        }
    }

    public static SaveData Load()
    {
        try
        {
            if (File.Exists(PathFull))
            {
                string json = File.ReadAllText(PathFull);
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                Debug.Log("[SaveSystem] Save carregado: " + PathFull);
                
                // Garantir que o array existe (compatibilidade com saves antigos)
                if (data.levelsCompleted == null)
                {
                    data.levelsCompleted = new bool[10];
                }
                
                return data;
            }
            else
            {
                Debug.Log("[SaveSystem] Nenhum save encontrado, criando novo SaveData.");
                return new SaveData();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[SaveSystem] Erro ao carregar save: " + ex.Message);
            return new SaveData();
        }
    }

    public static void DeleteSave()
    {
        try
        {
            if (File.Exists(PathFull))
            {
                File.Delete(PathFull);
                Debug.Log("[SaveSystem] Save deletado.");
            }
            else
            {
                Debug.Log("[SaveSystem] Nenhum save para deletar.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[SaveSystem] Erro ao deletar save: " + ex.Message);
        }
    }

    public static bool SaveExists()
    {
        return File.Exists(PathFull);
    }
}