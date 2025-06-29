using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PatternGen : MonoBehaviour
{
    public GameObject[] patterns;
    private List<GameObject> patternsList = new List<GameObject>();
    public GameObject spawnPoint;
    public GameObject SpawnTrigger;
    public GameObject nextPatternSpawn;
    private bool spawned = false;
    private SpawnTriggerHandler handler;
    
    //Anotações pra qnd abrir dnv:
    //Quando o spawn no SpawnTriggerHandler for true, a gnt spawna uma pattern nova por aqui

    void Start()
    {
        handler = SpawnTrigger.GetComponent<SpawnTriggerHandler>();
        nextPatternSpawn = GameObject.FindWithTag("SpawnNextTrigger");

        foreach (GameObject pattern in patterns)
        {
            patternsList.Add(pattern);

        }

        //StartCoroutine(WaitToSpawn());
        SpawnPattern();


    }

    void Update()
    {
        if (handler.TriggeredSpawn)
        {
            Debug.Log("spawned");
            SpawnPattern();
            handler.TriggeredSpawn = false;
        }
    }

    void SpawnPattern()
    {
        GameObject patternClone = Instantiate(patterns[Random.Range(0, patterns.Length)], spawnPoint.transform.position, Quaternion.identity);
        Debug.Log(patternClone);
        
    }
    
    IEnumerator WaitToSpawn()
    {

        yield return new WaitForSeconds(2f);
        SpawnPattern();
        
    }
    
    
}
