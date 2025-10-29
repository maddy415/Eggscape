using UnityEngine;

/// <summary>
/// Faz o objeto flutuar suavemente pra cima e pra baixo.
/// Ideal pra bosses, power-ups ou objetos parados que precisam parecer "vivos".
/// </summary>
public class HoverMotion : MonoBehaviour
{
    [Header("Configuração do movimento")]
    [Tooltip("Altura máxima de oscilação (em unidades do mundo).")]
    public float amplitude = 0.5f;

    [Tooltip("Velocidade da oscilação (ciclos por segundo).")]
    public float frequency = 1f;

    [Tooltip("Se verdadeiro, aplica uma rotação leve junto com o movimento.")]
    public bool swayRotation = false;

    [Tooltip("Intensidade da rotação se swayRotation estiver ativo.")]
    public float rotationAmount = 5f;

    [Header("Opções")]
    [Tooltip("Define o ponto base manualmente. Se vazio, usa a posição inicial do objeto.")]
    public Transform basePoint;

    private Vector3 startPos;
    private float seed; // pra evitar que múltiplos objetos fiquem em fase idêntica

    private void Start()
    {
        startPos = basePoint ? basePoint.position : transform.position;
        seed = Random.value * 10f; // offset aleatório pra cada instância
    }

    private void Update()
    {
        // movimento vertical senoidal
        float newY = startPos.y + Mathf.Sin((Time.time + seed) * frequency) * amplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // rotação opcional (pra dar um balanço leve)
        if (swayRotation)
        {
            float rot = Mathf.Sin((Time.time + seed) * frequency * 1.2f) * rotationAmount;
            transform.rotation = Quaternion.Euler(0, 0, rot);
        }
    }
}