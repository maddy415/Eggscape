using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public static MenuManager instance;

    private SceneTransition transition;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // opcional: se quiser manter entre cenas
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // tenta achar o controlador de transição na cena
        transition = FindObjectOfType<SceneTransition>();
    }

    public void LoadGame()
    {
        if (transition != null)
        {
            transition.LoadScene("lvl_1");
        }
        else
        {
            SceneManager.LoadScene("lvl_1");
        }

        Debug.Log("Lvl 1 Loaded");
    }

    public void LoadTutorial()
    {
        if (transition != null)
        {
            transition.LoadScene("tutorial");
        }
        else
        {
            SceneManager.LoadScene("tutorial");
        }

        Debug.Log("Tutorial Loaded");
    }

    public void LoadGame2()
    {
        if (transition != null)
        {
            transition.LoadScene("lvl_2");
        }
        else
        {
            SceneManager.LoadScene("lvl_2");
        }

        Debug.Log("Lvl 2 Loaded");
    }
}