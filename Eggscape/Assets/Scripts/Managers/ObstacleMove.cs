using System;
using UnityEngine;

public class ObstacleMove : MonoBehaviour
{
    public float speed;
    private Rigidbody2D playerRB;
    public LevelSegment currentSegment;


    private void Start()
    {
        GetComponent<ObstacleMove>().speed = currentSegment.velocidade;
    }

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
            GameManager.Instance.objsOnScene.Remove(gameObject);
        }

        /*if (other.gameObject.CompareTag("Attack"))
        {
            
            Destroy(gameObject);
            Debug.Log("tocou");
            GameManager.Instance.objsOnScene.Remove(gameObject);
            
            
            
            
        }*/
    }
    
}
