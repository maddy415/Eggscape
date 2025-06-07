using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    public GameObject vicCanvas;
    public GameObject spawner;
    public GameObject tronco;
    public GameObject ground;
    
    public static bool playerAlive = true;

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
        playerAlive = true;
    }

    private void Update()
    {
        ResetScene();
    }

    public void StopScene()
    {
        playerAlive = false;
        if (playerAlive == false)
        {
            Debug.Log("Player is dead");
            vicCanvas.SetActive(true); //Mostra a tela de game over

            spawner.GetComponent<ObstacleGen>().canSpawn = false;
            ground.GetComponent<GroundGen>().enabled = false;

            
            
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
                foreach (GameObject troncoClone in ObstacleGen.logObstacle)
                {
                    troncoClone.GetComponent<ObstacleMove>().enabled = false;
                }
            }
        }
    }
    
    
    
}
