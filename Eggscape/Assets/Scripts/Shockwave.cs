using UnityEngine;

/// <summary>
/// Shockwave: se move numa direção, mata o Player ao encostar (chama Player.Death) e pode sumir após um tempo.
/// Recomendações do prefab: Rigidbody2D (Kinematic), Collider2D (isTrigger = true), sem gravidade.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Shockwave : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Direção padrão caso não seja definida via Initialize().")]
    public Vector2 initialDirection = Vector2.right;

    [Tooltip("Velocidade horizontal da shockwave.")]
    public float speed = 12f;

    [Tooltip("Se true e houver Rigidbody2D, usa velocity. Se false, usa Translate (recomendado p/ trigger kinematic).")]
    public bool usePhysicsIfAvailable = false; // <- agora padrão é Translate

    [Header("Lifetime")]
    [Tooltip("Tempo para destruir automaticamente. <=0 desativa autodestruição.")]
    public float lifetime = 4f;

    [Header("Visual")]
    [Tooltip("Virar o sprite no eixo X quando a direção for para a esquerda.")]
    public bool flipSpriteByDirection = true;

    [Header("Colisão")]
    [Tooltip("Destruir a shockwave ao colidir com QUALQUER coisa (além do Player).")]
    public bool destroyOnAnyCollision = false;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Vector2 moveDir = Vector2.right;
    private float lifeTimer;

    private void Reset()
    {
        // Garantir que o collider seja trigger por padrão
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
        var body = GetComponent<Rigidbody2D>();
        if (body)
        {
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();

        moveDir = (initialDirection.sqrMagnitude > 0.0001f) ? initialDirection.normalized : Vector2.right;
        ApplyDirectionToVisual();

        // Se for usar física, setamos a velocity (não vai empurrar nada pois é kinematic+trigger)
        if (rb != null && usePhysicsIfAvailable)
        {
            rb.linearVelocity = moveDir * speed;
            rb.gravityScale = 0f;
        }

        lifeTimer = lifetime;
    }

    private void Update()
    {
        // Movimento manual se não estiver usando physics
        if (rb == null || !usePhysicsIfAvailable)
        {
            transform.position += (Vector3)(moveDir * speed * Time.deltaTime);
        }

        // Autodestruição
        if (lifetime > 0f)
        {
            lifeTimer -= Time.deltaTime;
            if (lifeTimer <= 0f)
                Destroy(gameObject);
        }
    }

    /// <summary> Defina a direção após instanciar. </summary>
    public void Initialize(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.0001f)
            moveDir = direction.normalized;

        ApplyDirectionToVisual();

        if (rb != null && usePhysicsIfAvailable)
        {
            rb.linearVelocity = moveDir * speed;
        }
    }

    private void ApplyDirectionToVisual()
    {
        if (!flipSpriteByDirection || sr == null) return;
        sr.flipX = moveDir.x < -0.0001f;
    }

    // ---- Colisão/Trigger ----

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (TryKillPlayer(other.gameObject)) return;
        if (destroyOnAnyCollision) Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        // Idealmente não chega aqui (trigger+kinematic), mas mantenho por robustez
        if (TryKillPlayer(other.gameObject)) return;
        if (destroyOnAnyCollision) Destroy(gameObject);
    }

    private bool TryKillPlayer(GameObject go)
    {
        var player = go.GetComponent<Player>()
                 ?? go.GetComponentInParent<Player>()
                 ?? go.GetComponentInChildren<Player>();

        if (player != null)
        {
            try { player.Death(); } catch { /* ignore */ }
            Destroy(gameObject);
            return true;
        }
        return false;
    }
}
