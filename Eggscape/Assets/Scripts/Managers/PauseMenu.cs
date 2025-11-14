using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    [Header("Refs")]
    [SerializeField] private GameObject pauseRoot;   // painel/canvas do pause
    [SerializeField] private Player player;          // referência pro Player

    [Header("Behaviour")]
    [SerializeField] private bool stopTime = true;   // se true, usa Time.timeScale = 0
    [SerializeField] private bool pauseMusic = false; // opcional, se quiser mexer na música

    private void Awake()
    {
        if (pauseRoot != null)
            pauseRoot.SetActive(false);

        if (player == null)
            player = FindObjectOfType<Player>();
    }

    private void Update()
    {
        // Esc para pausar/despausar
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (IsPaused) ResumeGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        if (IsPaused) return;
        IsPaused = true;

        if (pauseRoot != null)
            pauseRoot.SetActive(true);

        if (stopTime)
            Time.timeScale = 0f;

        if (player != null)
            player.CanMove = false; // usa tua propriedade do Player

        if (pauseMusic && AudioManager.audioInstance != null)
        {
            // se tiver método específico, troca aqui
            //AudioManager.audioInstance.musicSource.Pause();
        }

        // se quiser mostrar o mouse:
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ResumeGame()
    {
        if (!IsPaused) return;
        IsPaused = false;

        if (pauseRoot != null)
            pauseRoot.SetActive(false);

        if (stopTime)
            Time.timeScale = 1f;

        if (player != null)
            player.CanMove = true;

        if (pauseMusic && AudioManager.audioInstance != null)
        {
            //AudioManager.audioInstance.musicSource.UnPause();
        }

        // se tava usando mouse travado:
        // Cursor.visible = false;
        // Cursor.lockState = CursorLockMode.Locked;
    }

    // Se quiser um botão "Sair pro menu"
    public void QuitToMainMenu(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}
