using UnityEngine;

public class ObstacleMove : MonoBehaviour
{
    public float speed = 5f;
    
    void Update()
    {
        transform.Translate(Vector3.left * speed * Time.deltaTime, Space.World);
    }
}
