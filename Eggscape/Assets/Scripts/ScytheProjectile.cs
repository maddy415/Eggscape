using UnityEngine;

/// <summary>
/// Foice de bullet hell:
/// - RB2D Kinematic + Trigger
/// - Anda em linha reta (dir * speed)
/// - Gira visualmente (opcional)
/// - Expira após lifeTime
/// - Mata o Player ao tocar (exceto se IsAttackActive)
/// </summary>
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
    public Transform visual; // arraste seu sprite aqui (opcional)

    [Header("Hit")]
    public LayerMask hitMask;

    private float t;
    private Rigidbody2D rb;
    private Collider2D col;

    public void Initialize(Vector2 direction, float spd, float ttl, LayerMask mask)
    {
        dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        speed = spd;
        lifeTime = ttl;
        hitMask = mask;
        t = 0f;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
        col.isTrigger = true;

        if (visual == null) visual = transform;
    }

    private void Update()
    {
        t += Time.deltaTime;
        if (t >= lifeTime) { gameObject.SetActive(false); return; }

        rb.MovePosition(rb.position + dir * speed * Time.deltaTime);

        if (spin && visual != null)
            visual.Rotate(0f, 0f, spinDegPerSec * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((hitMask.value & (1 << other.gameObject.layer)) == 0) return;

        var player = other.GetComponent<Player>()
                  ?? other.GetComponentInParent<Player>()
                  ?? other.GetComponentInChildren<Player>();

        if (player != null && !player.IsAttackActive)
            player.Death();

        gameObject.SetActive(false);
    }
}
