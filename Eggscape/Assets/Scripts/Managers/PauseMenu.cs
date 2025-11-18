using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    [Header("Refs")]
    [SerializeField] private GameObject pauseRoot;     // painel/canvas do pause
    [SerializeField] private GameObject settingsRoot;  // painel/canvas de Configurações
    [SerializeField] private Player player;            // referência pro Player

    [Header("Behaviour")]
    [SerializeField] private bool stopTime = true;      // se true, usa Time.timeScale = 0
    [SerializeField] private bool pauseMusic = false;   // opcional, se quiser mexer na música

    private void Awake()
    {
        if (pauseRoot != null)
            pauseRoot.SetActive(false);

        if (settingsRoot != null)
            settingsRoot.SetActive(false);

        if (player == null)
            player = FindObjectOfType<Player>();
    }

    private void Update()
    {
        // Esc para pausar/despausar ou sair do menu de configs
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Se já tá pausado e o painel de settings está aberto, fecha ele e volta pro pause
            if (IsPaused && settingsRoot != null && settingsRoot.activeSelf)
            {
                CloseSettings();
            }
            else
            {
                TogglePause();
            }
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

        if (settingsRoot != null)
            settingsRoot.SetActive(false); // sempre começa no menu de pause, não no settings

        if (stopTime)
            Time.timeScale = 0f;

        if (player != null)
            player.CanMove = false; // usa tua propriedade do Player

        if (pauseMusic && AudioManager.audioInstance != null)
        {
            AudioManager.audioInstance.PauseMusic(true);
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

        if (settingsRoot != null)
            settingsRoot.SetActive(false);

        if (stopTime)
            Time.timeScale = 1f;

        if (player != null)
            player.CanMove = true;

        if (pauseMusic && AudioManager.audioInstance != null)
        {
            AudioManager.audioInstance.PauseMusic(false);
        }

        // se tava usando mouse travado, tu pode voltar a travar aqui se quiser
        // Cursor.visible = false;
        // Cursor.lockState = CursorLockMode.Locked;
    }

    // Botão "Configurações" (no menu de pause)
    public void OpenSettings()
    {
        if (!IsPaused)
            PauseGame(); // garante que o jogo esteja pausado

        if (pauseRoot != null)
            pauseRoot.SetActive(false);

        if (settingsRoot != null)
            settingsRoot.SetActive(true);
    }

    // Botão "Voltar" (no menu de configurações)
    public void CloseSettings()
    {
        if (settingsRoot != null)
            settingsRoot.SetActive(false);

        if (pauseRoot != null)
            pauseRoot.SetActive(true);
    }

    // Botão "Sair pro menu"
    public void QuitToMainMenu(string sceneName)
    {
        Time.timeScale = 1f;
        IsPaused = false;

        if (settingsRoot != null)
            settingsRoot.SetActive(false);

        if (pauseRoot != null)
            pauseRoot.SetActive(false);

        SceneManager.LoadScene(sceneName);
    }
}
