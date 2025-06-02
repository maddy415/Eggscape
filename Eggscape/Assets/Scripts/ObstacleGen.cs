using UnityEngine;
using UnityEngine.UIElements;

public class ObstacleGen : MonoBehaviour
{
    public GameObject tronco;
    public GameObject spawner;
    
    public float spawnTime = 3f;
    public float speed = 5f;
    private float timer = 0;
    private bool spawned = false;
 
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnTime)
        {
            Instantiate(tronco, spawner.transform.position, Quaternion.identity);
            spawned = true;
        }
        if (spawned)
        {
            timer = 0;
            spawned = false;
        }
        
    }
}
