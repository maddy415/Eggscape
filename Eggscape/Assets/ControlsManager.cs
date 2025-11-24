using UnityEngine;

public class ControlsManager : MonoBehaviour
{
    [SerializeField] private string storySceneName = "story";  

    void Update()
    {
        // Detecta se a tecla Espaço foi pressionada
        if (Input.GetKeyDown(KeyCode.Space))
        {
            LoadStoryNow();
        }
    }

    public void LoadStoryNow()
    {
        if (!string.IsNullOrEmpty(storySceneName))
        {
            // Verifica se o SceneTransition existe na cena
            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.LoadScene(storySceneName);
            }
            else
            {
                // Fallback normal caso não tenha SceneTransition
                UnityEngine.SceneManagement.SceneManager.LoadScene(storySceneName);
            }
        }
        else
        {
            Debug.LogWarning("Nome da cena não definido!");
        }
    }
}