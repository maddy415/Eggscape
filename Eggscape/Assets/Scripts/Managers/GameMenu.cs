using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameMenu : MonoBehaviour
{
    [Header("Config")] [Tooltip("Nome da cena do menu principal (se usar por nome).")] [SerializeField]
    private string menuSceneName = "menu";

    [Tooltip("Use build index para voltar ao menu? (Em vez de nome)")] [SerializeField]
    private bool useMenuBuildIndex = false;

    [Tooltip("Índice do menu no Build Settings (se usar por índice).")] [SerializeField]
    private int menuBuildIndex = 0;

    [Tooltip("Painel raiz deste menu (pode ser este objeto).")] [SerializeField]
    private GameObject root;

    [Header("SFX (opcional)")] [SerializeField]
    private AudioSource clickSfx;

    private bool busy;

    void Awake()
    {
        if (root == null) root = gameObject;
    }

    /// <summary>
    /// Reinicia a cena atual com fade.
    /// Botão: "Tentar Novamente"
    /// </summary>
    public void ResetSceneMenu()
    {
        if (busy) return;
        StartCoroutine(ResetRoutine());
    }

    /// <summary>
    /// Volta para o menu principal com fade.
    /// Botão: "Menu Principal"
    /// </summary>
    public void ReturnToMenu()
    {
        if (busy) return;
        StartCoroutine(ReturnRoutine());
    }

    // ================== Rotinas ==================

    private IEnumerator ResetRoutine()
    {
        busy = true;
        if (clickSfx) clickSfx.Play();

        // garante timescale normal e cursor visível, caso esteja pausado
        //UnpauseIfNeeded();

        // esconde o painel pra não ficar clicável durante a transição
        if (root) root.SetActive(false);

        int idx = SceneManager.GetActiveScene().buildIndex;

        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(idx);
        else
            SceneManager.LoadScene(idx);

        yield return null;
    }

    private IEnumerator ReturnRoutine()
    {
        busy = true;
        if (clickSfx) clickSfx.Play();

        //UnpauseIfNeeded();

        if (root) root.SetActive(false);

        if (useMenuBuildIndex)
        {
            if (SceneTransition.Instance != null)
                SceneTransition.Instance.LoadScene(menuBuildIndex);
            else
                SceneManager.LoadScene(menuBuildIndex);
        }
        else
        {
            if (SceneTransition.Instance != null)
                SceneTransition.Instance.LoadScene(menuSceneName);
            else
                SceneManager.LoadScene(menuSceneName);
        }

        yield return null;
    }
}

/*private void UnpauseIfNeeded()
{
    // Se estiver usando PauseManager, retoma antes de trocar de cena
    if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
        PauseManager.Instance.ResumeGame();

    // Belt & suspenders: garante timeScale normal mesmo sem PauseManager
    Time.timeScale = 1f;

    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;
}
}*/
