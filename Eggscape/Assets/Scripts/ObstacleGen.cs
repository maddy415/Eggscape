using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ObstacleGen : MonoBehaviour
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

    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnTime && canSpawn == true)
        {
            GameObject troncoClone = Instantiate(tronco, spawner.transform.position, Quaternion.identity);
            logObstacle.Add(troncoClone);
            spawned = true;
        }
        if (spawned)
        {
            timer = 0;
            spawned = false;
        }
        
        if (GameManager.playerAlive == false)
        {
            canSpawn = false;

            foreach (GameObject troncoClone in logObstacle)
            {
                if (troncoClone != null)
                {
                    troncoClone.GetComponent<ObstacleMove>().enabled = false;
                    Debug.Log("quantia" + logObstacle.Count);
                }
            }


        }
        
    }
}
