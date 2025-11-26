using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameObject vicCanvas;
    public FrogIdleJumper frogJumper;
    public GameObject barn; 
    public GameObject frog;
    public GameObject spawnerLog;
    public GameObject spawnerBird;
    public GameObject ground;
    public GameObject fence;
    public List<GameObject> objsOnScene = new List<GameObject>();
    public GameObject groundRef;
    public Player player;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI victoryText;
    public float score;
    public PatternGen patternGen;
    public GameObject saveData;

    public float sceneTime = 0;
    public float timeGoal;

    public bool playerAlive = true;
    private bool victoryTriggered = false;
    private bool waitingForVictory = false;
    public bool isCheatOn = false;
    private bool mobileVictoryContinueRequested;

    public bool victoryAchieved = false;

    // ========= DEBUG DE VITÓRIA =========
    [Header("Debug de Vitória")]
    public bool debugVictoryHUD = false;                  // F3 liga/desliga
    public TextMeshProUGUI debugVictoryText;              // opcional: arrasta um TMP aqui
    [Range(0.1f, 2f)] public float debugRefresh = 0.25f;  // freq de atualização da HUD
    private float debugTimer;
    private string debugCache = "";                       // buffer do texto da HUD
    public int debugListMax = 6;                          // quantos objs listar

    // ========= TRANSITION (fade) =========
    private SceneTransition transition;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        victoryText.enabled = false;
        groundRef.SetActive(false);
    
        frogJumper = GetComponent<FrogIdleJumper>();
        //frogJumper.enabled = true;

        if (groundRef != null) groundRef.SetActive(false);
        if (victoryText != null) victoryText.text = "";

        // pegar o SceneTransition (fade) se existir
        transition = SceneTransition.Instance != null
            ? SceneTransition.Instance
            : FindObjectOfType<SceneTransition>(true);

        // ===== VERIFICAR SE SAVEMANAGER EXISTE =====
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("[GameManager] SaveManager não encontrado! Crie um GameObject persistente com SaveManager.");
        }
        // ===========================================
    }

    private void Update()
    {
        // HUD de score
        if (scoreText) scoreText.text = "Score: " + Convert.ToInt32(score);

        // condição de vitória por tempo
        if (!victoryTriggered && sceneTime >= timeGoal)
        {
            Victory();
        }

        // aguardando limpar a cena (sem inimigos)
        if (waitingForVictory && objsOnScene.Count <= 0)
        {
            Debug.Log("cabo os bixo ganhou");
            waitingForVictory = false;
            victoryText.enabled = true;
            victoryAchieved = true;
        }

        // input de reset
        ResetScene();

        // cronômetro enquanto player vivo
        if (playerAlive)
        {
            sceneTime += Time.deltaTime;
        }

        // cheat toggle (editor/dev builds apenas)
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Input.GetKeyDown(KeyCode.J) && isCheatOn == false)
        {
            CheatOn();
        }
        else if (Input.GetKeyDown(KeyCode.J) && isCheatOn)
        {
            isCheatOn = false;
            Debug.Log("Cheat desativado");
        }
#endif

        // avançar fase (quando vitória alcançada e player travado)
        if (victoryAchieved && player != null && player.CanMove == false && (Input.GetKeyDown(KeyCode.Space) || mobileVictoryContinueRequested))
        {
            mobileVictoryContinueRequested = false;
            LoadNextScene();
        }

        // avançar fase (forçado)
        if (Input.GetKeyDown(KeyCode.N))
        {
            LoadNextScene();
        }

        // ===== DEBUG: toggles =====
        if (Input.GetKeyDown(KeyCode.F3)) debugVictoryHUD = !debugVictoryHUD;
        if (Input.GetKeyDown(KeyCode.F4)) DumpVictoryDebugToConsole();

        // ===== DEBUG: atualiza HUD com frequência leve =====
        if (debugVictoryHUD)
        {
            debugTimer -= Time.deltaTime;
            if (debugTimer <= 0f)
            {
                UpdateVictoryDebug();
                debugTimer = debugRefresh;
            }
        }
    }

    public void StopScene()
    {
        if (playerAlive == false)
        {
            Debug.Log("Player is dead");
            if (vicCanvas) vicCanvas.SetActive(true); //Mostra a tela de game over

            StopSpawners();

            if (ground) ground.GetComponent<GroundGen>().enabled = false;
            if (fence) fence.GetComponent<GroundGen>().enabled = false;
            if (barn) barn.GetComponent<ObstacleMove>().enabled = false;
            //if (frog) frog.GetComponent<FrogIdleJumper>().enabled = false;

            foreach (GameObject obj in GameManager.Instance.objsOnScene)
            {
                if (obj == null) continue;

                if (obj.layer == 9 && frogJumper != null)
                {
                    frogJumper.speed = 0;
                }
                if (obj.layer != 8)
                {
                    var mv = obj.GetComponent<ObstacleMove>();
                    if (mv) mv.enabled = false; //Isso potencialmente ta mal otimizado pra caralho
                }
            }
        }
    }

    // ... (mantenha todo o código anterior até o método ResetScene)

    public void ResetScene()
    {
        if (playerAlive == false)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                // ===== NOVO: Preserva a música antes de recarregar =====
                if (AudioManager.audioInstance != null)
                {
                    AudioManager.audioInstance.PreserveMusicPosition();
                }
                
                LoadWithTransitionByIndex(SceneManager.GetActiveScene().buildIndex);
                return;
            }
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            // ===== NOVO: Preserva a música antes de recarregar =====
            if (AudioManager.audioInstance != null)
            {
                AudioManager.audioInstance.PreserveMusicPosition();
            }
            
            LoadWithTransitionByIndex(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void RestartFromUI()
    {
        if (playerAlive == false)
        {
            // ===== NOVO: Preserva a música antes de recarregar =====
            if (AudioManager.audioInstance != null)
            {
                AudioManager.audioInstance.PreserveMusicPosition();
            }
            
            LoadWithTransitionByIndex(SceneManager.GetActiveScene().buildIndex);
        }
    }

// ... (resto do código permanece igual)
    // ==========================================
//   SUBSTITUA O MÉTODO Victory() NO GameManager.cs
// ==========================================

    void Victory()
    {
        victoryTriggered = true;
        waitingForVictory = true;
    
        StopSpawners();

        // ===== INTEGRAÇÃO COM SAVEMANAGER (USANDO MAPPER) =====
        if (SaveManager.Instance != null)
        {
            int currentBuildIndex = SceneManager.GetActiveScene().buildIndex;
        
            // Converte Build Index → Level Index
            int levelIndex = LevelIndexMapper.BuildIndexToLevelIndex(currentBuildIndex);
        
            int currentScore = (int)score;
        
            // Salvar progresso
            SaveManager.Instance.CompleteLevel(levelIndex, currentScore);
        
            Debug.Log($"✅ [GameManager] Vitória! Build Index {currentBuildIndex} → Level Index {levelIndex} | Score: {currentScore}");
        }
        else
        {
            Debug.LogError("❌ [GameManager] SaveManager NÃO ENCONTRADO!");
        }
        // ======================================================

        Debug.Log("⏳ Vitória iniciada. Esperando limpar a cena...");
    }


    public void Debug_SaveNow()
    {
        SaveData d = new SaveData { highScore = (int)score, levelReached = SceneManager.GetActiveScene().buildIndex };
        SaveSystem.Save(d);
    }

    public void Debug_LoadNow()
    {
        SaveData d = SaveSystem.Load();
        Debug.Log("LoadNow -> highscore: " + d.highScore);
    }

    public void Debug_DeleteSave()
    {
        SaveSystem.DeleteSave();
    }

    void StopSpawners()
    {
        if (patternGen != null) patternGen.canSpawn = false;
    }

    void CheatOn() //Desativa a detecção de colisão no OnCollisionEnter2D do Player
    {
        isCheatOn = true;
        Debug.Log("Cheat ativado");
    }

    public void LoadNextScene()
    {
        // Pega o índice da cena atual
        int currentIndex = SceneManager.GetActiveScene().buildIndex;

        // Calcula o índice da próxima cena
        int nextIndex = currentIndex + 1;

        // Se ainda houver próxima cena na lista, carrega
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            // antes: SceneManager.LoadScene(nextIndex);
            LoadWithTransitionByIndex(nextIndex);
        }
        else
        {
            Debug.LogWarning("Não há próxima cena configurada no Build Settings!");
        }
    }

    public void UpdateScore(int peso)
    {
        if (peso == 2)
        {
            score += 2;
        }
        else
        {
            score++;
        }
    }

    // ========= MÉTODOS DE DEBUG =========
    private void UpdateVictoryDebug()
    {
        // limpa nulos pra contagem honesta
        objsOnScene.RemoveAll(x => x == null);

        debugCache = BuildVictoryDebugString();

        // HUD com TMP se fornecido
        if (debugVictoryText != null)
        {
            debugVictoryText.text = debugCache;
        }
        // se não tiver TMP, OnGUI exibe o fallback
    }

    private string BuildVictoryDebugString()
    {
        bool timeOk = sceneTime >= timeGoal;
        int count = objsOnScene.Count;

        // coleta alguns nomes de objetos que restam
        List<string> names = new List<string>();
        for (int i = 0; i < count && i < debugListMax; i++)
        {
            var obj = objsOnScene[i];
            if (obj == null) continue;
            int layer = obj.layer;
            string nm = obj.name;
            names.Add($"{nm} [L{layer}]");
        }

        string objsList = names.Count > 0 ? string.Join(", ", names) : "(nenhum listado)";
        string spawner = (patternGen != null) ? (patternGen.canSpawn ? "ATIVO" : "PARADO") : "desconhecido";

        float faltaTempo = Mathf.Max(0f, timeGoal - sceneTime);

        return
            $"[VICTORY DEBUG]\n" +
            $"Tempo: {sceneTime:F1}s / {timeGoal:F1}s  (falta ~{faltaTempo:F1}s)  | TimeOK: {timeOk}\n" +
            $"Objs restantes: {count}  | WaitingClear: {waitingForVictory}\n" +
            $"Flags -> triggered:{victoryTriggered}  waiting:{waitingForVictory}  achieved:{victoryAchieved}  alive:{playerAlive}\n" +
            $"Spawner: {spawner}\n" +
            $"Lista (até {debugListMax}): {objsList}\n";
    }

    private void DumpVictoryDebugToConsole()
    {
        UpdateVictoryDebug(); // força atualizar antes do log
        Debug.Log(debugCache);
    }

    // HUD fallback se não usar TMP
    private void OnGUI()
    {
        if (!debugVictoryHUD || debugVictoryText != null) return;

        var style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 12,
            wordWrap = true
        };

        float w = 520f;
        float h = 160f;
        GUI.Box(new Rect(10, 10, w, h), debugCache, style);
    }

    // ========= HELPERS DE LOAD COM FADE =========
    private void LoadWithTransitionByIndex(int buildIndex)
    {
        if (transition != null)
            transition.LoadScene(buildIndex);
        else
            SceneManager.LoadScene(buildIndex);
    }

    private void LoadWithTransitionByName(string sceneName)
    {
        if (transition != null)
            transition.LoadScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }
    
#if UNITY_EDITOR
    [ContextMenu("Debug: Desbloquear Todas as Fases")]
    private void DebugUnlockAllLevels()
    {
        if (SaveManager.Instance != null)
        {
            SaveData save = SaveManager.Instance.GetCurrentSave();
            save.levelReached = 99; // Desbloqueia todas
            for (int i = 0; i < save.levelsCompleted.Length; i++)
            {
                save.levelsCompleted[i] = true;
            }
            SaveManager.Instance.SaveGame();
            Debug.Log("[DEBUG] Todas as fases desbloqueadas!");
        }
    }

    [ContextMenu("Debug: Resetar Progresso")]
    private void DebugResetProgress()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ResetProgress();
            Debug.Log("[DEBUG] Progresso resetado!");
        }
    }

    [ContextMenu("Debug: Mostrar Progresso Atual")]
    private void DebugShowProgress()
    {
        if (SaveManager.Instance != null)
        {
            SaveData save = SaveManager.Instance.GetCurrentSave();
            Debug.Log($"=== PROGRESSO ATUAL ===\n" +
                      $"HighScore: {save.highScore}\n" +
                      $"Level Reached: {save.levelReached}\n" +
                      $"Current Level: {save.currentLevel}");
        }
    }
#endif
}
