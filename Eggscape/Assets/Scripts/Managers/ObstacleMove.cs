using UnityEngine;

public class ObstacleMove : MonoBehaviour
{
    [Header("Velocidade aplicada em runtime")]
    public float speed;

    [Header("Multiplicador de velocidade (1 = normal)")]
    [HideInInspector]
    public float speedMultiplier = 1f;

    // Opcional: apenas para debug no Inspetor (não arraste nada aqui!)
    [HideInInspector] public LevelSegment currentSegment;

    void Update()
    {
        // Move globalmente para a esquerda, agora usando o multiplicador
        transform.Translate(Vector3.left * speed * speedMultiplier * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Destroyer"))
        {
            // Remove antes de destruir (mantém tua lógica existente)
            ObstacleGen.logObstacle.Remove(gameObject);
            if (GameManager.Instance != null)
                GameManager.Instance.objsOnScene.Remove(gameObject);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Recebe o LevelSegment atual e aplica sua velocidade.
    /// Deve ser chamado pelo spawner imediatamente após Instantiate.
    /// </summary>
    public void Init(LevelSegment seg)
    {
        currentSegment = seg;
        if (seg != null)
        {
            speed = seg.velocidade;
        }
        else
        {
            //speed = 0f;
            Debug.LogWarning($"[ObstacleMove] Nenhum LevelSegment passado para '{name}'. Velocidade = 0.");
        }
    }

    // Helpers opcionais
    public void SetSpeedMultiplier(float value)
    {
        speedMultiplier = value;
    }

    public void ResetSpeedMultiplier()
    {
        speedMultiplier = 1f;
    }
}