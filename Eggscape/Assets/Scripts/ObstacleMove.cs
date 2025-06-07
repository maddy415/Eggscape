using System;
using UnityEngine;

public class ObstacleMove : MonoBehaviour
{
    public float speed = 5f;
    void Update()
    {
        transform.Translate(Vector3.left * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Destroyer"))
        {
            
            Destroy(gameObject);
            ObstacleGen.logObstacle.Remove(gameObject);
            Debug.Log("removeu");
        }
    }
    
}
