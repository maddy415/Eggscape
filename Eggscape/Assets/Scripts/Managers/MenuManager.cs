using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public static MenuManager instance;

    [Header("UI")]
    [SerializeField] private GameObject settingsRoot;   // painel de configurações

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

    // =====================
    //   Mudança de Cenas
    // =====================

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

    // =====================
    //   Configurações
    // =====================

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
}