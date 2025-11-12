using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    public Animator transitionAnim;
    public float transitionTime = 1f;

    private static SceneTransition instance;
    private bool firstSceneLoaded = false;
    private Canvas fadeCanvas;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            fadeCanvas = GetComponentInChildren<Canvas>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!firstSceneLoaded)
        {
            firstSceneLoaded = true;
            transitionAnim.Play("FadeIn", 0, 1f);
            fadeCanvas.sortingOrder = 0;
            return;
        }

        // quando a nova cena carregar, toca o FadeIn e abaixa o canvas
        StartCoroutine(FadeInRoutine());
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(Transition(sceneName));
    }

    private IEnumerator Transition(string sceneName)
    {
        // sobe o fade pra frente
        fadeCanvas.sortingOrder = 2;

        transitionAnim.SetTrigger("Start");
        yield return new WaitForSeconds(transitionTime);
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeInRoutine()
    {
        transitionAnim.Play("FadeIn", 0, 0f);
        yield return new WaitForSeconds(transitionTime);
        // depois que o fade sumir, abaixa ele de volta
        fadeCanvas.sortingOrder = 0;
    }
}