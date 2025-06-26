using System.Collections.Generic;
using UnityEngine;

public class PatternGen : MonoBehaviour
{
    public GameObject[] patterns;
    private List<GameObject> patternsList = new List<GameObject>();
    public GameObject spawnPoint;

    void Start()
    {
        foreach (GameObject pattern in patterns)
        {
            patternsList.Add(pattern);
            
        }
        
        SpawnPattern();
        
    }

    void SpawnPattern()
    {
        Instantiate(patterns[Random.Range(0, patterns.Length)], spawnPoint.transform.position, Quaternion.identity);
    }
    
    
    
   
}
