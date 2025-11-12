using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance { get; private set; }

    [Header("Animator / Fade")]
    [Tooltip("Animator com as animações FadeOut (0->1) e FadeIn (1->0).")]
    public Animator transitionAnim;
    [Tooltip("Trigger que toca o FadeOut no Animator.")]
    public string fadeOutTrigger = "Start";
    [Tooltip("Nome do state de entrada (deve ser o FadeIn, alpha 1->0).")]
    public string fadeInStateName = "FadeIn";
    [Tooltip("Duração do fade (segundos). Deve bater com as animações.")]
    public float transitionTime = 1f;

    [Header("Canvas Sorting")]
    [Tooltip("Canvas do overlay de fade. Deve estar como Screen Space - Overlay e Override Sorting = ON.")]
    public Canvas fadeCanvas;
    [Tooltip("Ordem enquanto o fade está ativo (sobre a UI).")]
    public int sortingFront = 2;
    [Tooltip("Ordem quando o fade termina (atrás da UI).")]
    public int sortingBack = 0;

    private bool firstSceneLoaded = false;

    private void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (!fadeCanvas)
                fadeCanvas = GetComponentInChildren<Canvas>(includeInactive: true);

            if (fadeCanvas)
                fadeCanvas.overrideSorting = true;

            // Garante que não esteja cobrindo no início
            if (fadeCanvas) fadeCanvas.sortingOrder = sortingBack;

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Ao dar Play no Editor direto numa cena, esse callback dispara;
        // queremos ignorar o primeiro fade-in.
        if (!firstSceneLoaded)
        {
            firstSceneLoaded = true;

            // Se por qualquer motivo a tela estiver preta, garante limpar:
            if (transitionAnim && !string.IsNullOrEmpty(fadeInStateName))
                transitionAnim.Play(fadeInStateName, 0, 1f);

            if (fadeCanvas) fadeCanvas.sortingOrder = sortingBack;
            return;
        }

        // Nas transições normais, toca o FadeIn e desce o canvas depois
        StartCoroutine(FadeInRoutine());
    }

    // ========= API pública =========

    public void LoadScene(string sceneName)
    {
        StartCoroutine(TransitionByName(sceneName));
    }

    public void LoadScene(int buildIndex)
    {
        StartCoroutine(TransitionByIndex(buildIndex));
    }

    // ========= Núcleo =========

    private IEnumerator TransitionStart()
    {
        if (fadeCanvas) fadeCanvas.sortingOrder = sortingFront;

        if (transitionAnim && !string.IsNullOrEmpty(fadeOutTrigger))
            transitionAnim.SetTrigger(fadeOutTrigger);

        yield return new WaitForSeconds(transitionTime);
    }

    private IEnumerator TransitionByName(string sceneName)
    {
        yield return TransitionStart();
        SceneManager.LoadScene(sceneName);
        // O FadeIn rola no OnSceneLoaded()
    }

    private IEnumerator TransitionByIndex(int buildIndex)
    {
        yield return TransitionStart();
        SceneManager.LoadScene(buildIndex);
        // O FadeIn rola no OnSceneLoaded()
    }

    private IEnumerator FadeInRoutine()
    {
        if (transitionAnim && !string.IsNullOrEmpty(fadeInStateName))
            transitionAnim.Play(fadeInStateName, 0, 0f);

        yield return new WaitForSeconds(transitionTime);

        if (fadeCanvas) fadeCanvas.sortingOrder = sortingBack;
    }
}
