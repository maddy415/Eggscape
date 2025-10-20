using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    public GameObject vicCanvas;
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
        groundRef.SetActive(false);
        victoryText.text = "";
        
        // carregar save ao iniciar
        SaveData loaded = SaveSystem.Load();
        //scoreText.text = "Highscore: " + data.highScore;
        Debug.Log("Highscore carregado: " + loaded.highScore + " | levelReached: " + loaded.levelReached);
        // opcional: aplicar loaded.highScore ao UI
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

        if (victoryAchieved && player.canMove == false && Input.GetKeyDown(KeyCode.Space))
        {
            LoadNextScene();
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

            foreach (GameObject obj in GameManager.Instance.objsOnScene)
            {
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
                // ObstacleGen.logObstacle.RemoveAll(item => item == null);
              
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
        SceneManager.LoadScene("lvl_2");
    }
    
}
