using System;
using UnityEngine;

public class ObstacleMove : MonoBehaviour
{
    public float speed;
    public LevelSegment currentSegment;

    private void Start()
    {
        /*if (currentSegment != null)
        {
            speed = currentSegment.velocidade;
        }
        else
        {
            Debug.LogError($"[ObstacleMove] currentSegment está NULL em '{name}'. " +
                           $"Garanta que o spawner chame Init() antes do Start ou preencha no prefab.");
            enabled = false; // opcional: desabilita pra evitar Update com speed 0
        }*/
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
    }

    // >>> Forma correta de injetar o LevelSegment <<<
    public void Init(LevelSegment seg)
    {
        currentSegment = seg;
        speed = seg != null ? seg.velocidade : 0f;
        if (!enabled) enabled = true;
    }
}