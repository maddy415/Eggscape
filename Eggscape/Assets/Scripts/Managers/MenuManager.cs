using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public static MenuManager instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }


    public void LoadGame()
    {
        SceneManager.LoadScene("lvl_1");
        Debug.Log("Lvl 1 Loaded");
    }
    
    public void LoadGame2()
    {
        SceneManager.LoadScene("lvl_2");
        Debug.Log("Lvl 2 Loaded");

    }
}
