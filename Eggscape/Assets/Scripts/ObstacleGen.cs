using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ObstacleGen : MonoBehaviour
{
    public GameObject tronco;
    public GameObject spawner;
    public bool onTutorial;
    
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

    public GameObject SpawnObstacle()
    {
        GameObject troncoClone = Instantiate(tronco, spawner.transform.position, Quaternion.identity);
        return troncoClone;
    }

    
    
    void Update()
    {
        if (onTutorial)
        {
            return;
        }
        else
        {
            timer += Time.deltaTime;
            if (timer >= spawnTime && canSpawn)
            {
                GameObject troncoClone = Instantiate(tronco, spawner.transform.position, Quaternion.identity);
                logObstacle.Add(troncoClone);
                GameManager.Instance.objsOnScene.Add(troncoClone);
            
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
                foreach (GameObject troncoClone in logObstacle)
                {
                    if (troncoClone != null)
                    {
                        troncoClone.GetComponent<ObstacleMove>().enabled = false;
                    
                        //Debug.Log("quantia" + logObstacle.Count);
                    
                    }
                
                
                }


            }
        }
        }
        
        



    }


