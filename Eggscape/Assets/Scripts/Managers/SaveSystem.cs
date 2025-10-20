using System.IO;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public int highScore;
    public int levelReached;
    // adicione mais campos aqui conforme necessário:
    // public float lastScore;
    // public List<string> collectedItems;
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
            Debug.Log($"Save feito em: {PathFull}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Erro ao salvar: " + ex.Message);
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
                Debug.Log("Save carregado: " + PathFull);
                return data;
            }
            else
            {
                Debug.Log("Nenhum save encontrado, retornando SaveData padrão.");
                return new SaveData();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Erro ao carregar save: " + ex.Message);
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
                Debug.Log("Save deletado.");
            }
            else
            {
                Debug.Log("Nenhum save para deletar.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Erro ao deletar save: " + ex.Message);
        }
    }
}