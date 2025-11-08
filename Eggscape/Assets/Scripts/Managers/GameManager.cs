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
    
    public bool victoryAchieved = false;

    // ========= DEBUG DE VITÓRIA (NOVO) =========
    [Header("Debug de Vitória")]
    public bool debugVictoryHUD = false;                  // F3 liga/desliga
    public TextMeshProUGUI debugVictoryText;              // opcional: arrasta um TMP aqui
    [Range(0.1f, 2f)] public float debugRefresh = 0.25f;  // freq de atualização da HUD
    private float debugTimer;
    private string debugCache = "";                       // buffer do texto da HUD
    public int debugListMax = 6;                          // quantos objs listar

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this; 
        }
        else
        {
            Destroy(gameObject); 
        }
    }

    private void Start()
    {
        frogJumper = frog.GetComponent<FrogIdleJumper>();
        
        groundRef.SetActive(false);
        victoryText.text = "";
        
        // carregar save ao iniciar
        SaveData loaded = SaveSystem.Load();
        Debug.Log("Highscore carregado: " + loaded.highScore + " | levelReached: " + loaded.levelReached);
    }

    private void Update()
    {
        //score += Time.deltaTime;
        scoreText.text = "Score: "+Convert.ToInt32(score);
        
        if (!victoryTriggered && sceneTime >= timeGoal)
        {
            Victory();
        }
        
        if (waitingForVictory && objsOnScene.Count <= 0)
        {
            Debug.Log("cabo os bixo ganhou");
            waitingForVictory = false;
            victoryAchieved = true;
        }
        
        ResetScene();
        
        if (playerAlive)
        {
            sceneTime += Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.J) && isCheatOn==false)
        {
            CheatOn();
        }
        else if (Input.GetKeyDown(KeyCode.J) && isCheatOn)
        {
            isCheatOn = false;
            Debug.Log("Cheat desativado");
        }

        if (victoryAchieved && player.CanMove == false && Input.GetKeyDown(KeyCode.Space))
        {
            LoadNextScene();
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            LoadNextScene();
        }

        // ===== DEBUG: toggles (NOVO) =====
        if (Input.GetKeyDown(KeyCode.F3)) debugVictoryHUD = !debugVictoryHUD;
        if (Input.GetKeyDown(KeyCode.F4)) DumpVictoryDebugToConsole();

        // ===== DEBUG: atualiza HUD com frequência leve (NOVO) =====
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
            vicCanvas.SetActive(true); //Mostra a tela de game over

            StopSpawners();
            
            ground.GetComponent<GroundGen>().enabled = false;
            fence.GetComponent<GroundGen>().enabled = false;
            barn.GetComponent<ObstacleMove>().enabled = false;

            foreach (GameObject obj in GameManager.Instance.objsOnScene)
            {
                if (obj.layer == 9)
                {
                    frogJumper.speed = 0;

                }
                if (obj.layer != 8)
                {
                    obj.GetComponent<ObstacleMove>().enabled = false; //Isso potencialmente ta mal otimizado pra caralho
                }
            }
        }
    }

    void ResetScene()
    {
        if (playerAlive == false)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
        
        if(Input.GetKeyDown(KeyCode.H))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    void Victory()
    {
        victoryTriggered = true;
        waitingForVictory = true;

        StopSpawners();
        // salvar progresso ao vencer
        SaveData data = new SaveData();
        data.highScore = Mathf.Max((int)score, SaveSystem.Load().highScore); // manter melhor pontuação
        data.levelReached = SceneManager.GetActiveScene().buildIndex; // ou +1 se preferir
        SaveSystem.Save(data);

        Debug.Log("Progresso salvo!");
        Debug.Log("Vitória iniciada. Esperando limpar a cena...");
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
        patternGen.canSpawn = false;
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
            SceneManager.LoadScene(nextIndex);
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

    // ========= MÉTODOS DE DEBUG (NOVO) =========
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
}
