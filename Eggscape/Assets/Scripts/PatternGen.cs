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
    private bool spawned = false;
    
    //Anotações pra qnd abrir dnv:
    //Quando o spawn no SpawnTriggerHandler for true, a gnt spawna uma pattern nova por aqui

    void Start()
    {
        foreach (GameObject pattern in patterns)
        {
            patternsList.Add(pattern);

        }

        StartCoroutine(WaitToSpawn());
        SpawnTrigger.GetComponent<SpawnTriggerHandler>();
        

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
