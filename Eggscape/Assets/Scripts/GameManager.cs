using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public GameObject vicCanvas;
    public static GameManager Instance;
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
    }

    public void StopScene()
    {
        playerAlive = false;
        if (playerAlive == false)
        {
            Debug.Log("Player is dead");
            vicCanvas.SetActive(true);
            
            Time.timeScale = 0f; //Congelar tudo q depende de fisica
        }
    }

    void ResetScene()
    {
        if (playerAlive == false)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                Time.timeScale = 1f;
            }
        }
    }
    
    
    
}
