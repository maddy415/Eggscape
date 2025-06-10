using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    public GameObject vicCanvas;
    public GameObject spawner; 
    public GameObject ground;
    public GameObject fence;
    
    public float score = 0;
    
    public bool playerAlive = true;

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
        ResetScene();
        score += Time.deltaTime;
        Debug.Log(score);
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
    
    
    
}
