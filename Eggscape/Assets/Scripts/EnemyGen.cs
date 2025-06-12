using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class EnemyGen : MonoBehaviour
{
    public GameObject tronco;
    public GameObject spawner;
    
    public float spawnTime = 3f;
    private float timer = 0;
    private bool spawned = false;
    public bool canSpawn = true;
    
    public static List<GameObject>logObstacle = new List<GameObject>();

    private void Start()
    {
        canSpawn = true;
        GameObject[] troncosCena = GameObject.FindGameObjectsWithTag("Obstacle");

    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnTime && canSpawn)
        {
            GameObject birdClone = Instantiate(tronco, spawner.transform.position, Quaternion.identity);
            logObstacle.Add(birdClone);
            GameManager.Instance.objsOnScene.Add(birdClone);
            
            spawned = true;
        }
        if (spawned)
        {
            timer = 0;
            spawned = false;
        }
        
        if (GameManager.Instance.playerAlive == false)
        {
            canSpawn = false;
            foreach (GameObject birdClone in logObstacle)
            {
                if (birdClone != null)
                {
                    birdClone.GetComponent<ObstacleMove>().enabled = false;
                    
                    //Debug.Log("quantia" + logObstacle.Count);
                    
                }
                
                
            }


        }
        
    }
}
