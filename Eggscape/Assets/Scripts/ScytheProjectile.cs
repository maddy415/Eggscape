using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class ScytheProjectile : MonoBehaviour
{
    [Header("Move")]
    public float speed = 12f;
    public float lifeTime = 3f;
    public Vector2 dir = Vector2.right;

    [Header("Spin")]
    public bool spin = true;
    public float spinDegPerSec = 360f;
    public Transform visual;

    [Header("Hit (layers que PODEM morrer)")]
    public LayerMask hitMask; // ex.: Player, PlayerLayer

    // Internos
    private float t;
    private Rigidbody2D rb;
    private Collider2D col;
    private ScythePool pool; // referência ao pool (pra devolver a foice)

    // =======================
    // POOL LINK
    // =======================
    public void AttachPool(ScythePool p)
    {
        pool = p;
    }

    // Inicializa com novos parâmetros
    public void Initialize(Vector2 direction, float spd, float ttl, LayerMask mask)
    {
        dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        speed = spd;
        lifeTime = ttl;
        hitMask = mask;
        t = 0f;

        if (visual)
            visual.localRotation = Quaternion.identity;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        col.isTrigger = true;

        if (!visual) visual = transform;
    }

    private void FixedUpdate()
    {
        t += Time.fixedDeltaTime;
        if (t >= lifeTime)
        {
            Despawn();
            return;
        }

        rb.MovePosition(rb.position + dir * speed * Time.fixedDeltaTime);

        if (spin && visual)
            visual.Rotate(0f, 0f, spinDegPerSec * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((hitMask.value & (1 << other.gameObject.layer)) == 0) return;

        var p = other.GetComponent<Player>()
              ?? other.GetComponentInParent<Player>()
              ?? other.GetComponentInChildren<Player>();

        if (p != null)
        {
            // se quiser imunidade durante ataque, descomente:
            // if (p.IsAttackActive) return;

            p.Death();
            Despawn();
        }
    }

    private void Despawn()
    {
        if (pool != null)
            pool.Despawn(this);
        else
            gameObject.SetActive(false); // fallback se não tiver pool
    }
}
