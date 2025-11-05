using UnityEngine;

/// <summary>
/// Faz o pássaro se mover usando a velocidade definida no LevelSegment atual.
/// </summary>
public class BirdMove : MonoBehaviour
{
    [HideInInspector] public LevelSegment currentSegment;

    void Update()
    {
        if (currentSegment == null) return;

        // Move o pássaro para a esquerda com a velocidade definida no segmento
        transform.Translate(Vector3.left * currentSegment.birdSpeed * Time.deltaTime, Space.World);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Destroyer"))
        {
            // Remove antes de destruir (mantém tua lógica existente)
            ObstacleGen.logObstacle.Remove(gameObject);
            GameManager.Instance.objsOnScene.Remove(gameObject);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Recebe o LevelSegment atual no spawn.
    /// </summary>
    public void Init(LevelSegment seg)
    {
        currentSegment = seg;
    }
}