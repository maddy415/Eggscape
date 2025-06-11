using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    public GameObject vicCanvas;
    public GameObject spawner; 
    public GameObject ground;
    public GameObject fence;
    public List<GameObject> objsOnScene = new List<GameObject>();
    
    public float score = 0;
    
    public bool playerAlive = true;
    private bool victoryTriggered = false;
    private bool waitingForVictory = false;
    
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

    private void Update()
    {
        //Debug.Log(objsOnScene.Count);
        if (!victoryTriggered && score >= 5)
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
            score += Time.deltaTime;
        }
        
        //Debug.Log(score);
    }

    public void StopScene()
    {
        if (playerAlive == false)
        {
            Debug.Log("Player is dead");
            vicCanvas.SetActive(true); //Mostra a tela de game over

            spawner.GetComponent<ObstacleGen>().canSpawn = false;
            ground.GetComponent<GroundGen>().enabled = false;
            fence.GetComponent<GroundGen>().enabled = false;

            
            
        }
    }

    void ResetScene()
    {
        if (playerAlive == false)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                ObstacleGen.logObstacle.RemoveAll(item => item == null);
              
            }
        }
    }

    void Victory()
    {
        victoryTriggered = true;
        waitingForVictory = true;

        spawner.GetComponent<ObstacleGen>().canSpawn = false;
        Debug.Log("Vitória iniciada. Esperando limpar a cena...");
    }

    
}
