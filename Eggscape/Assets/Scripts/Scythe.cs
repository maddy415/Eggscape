using UnityEngine;

public class Scythe : MonoBehaviour
{
    [SerializeField] private float speed = 6f;
    [SerializeField] private float lifetime = 10f;

    [SerializeField, Tooltip("How quickly the scythe adjusts its direction towards the target.")]
    private float followResponsiveness = 5f;

    private Vector3 _direction;
    private float _timeAlive;
    private Transform _target;

    // ---------------------- [NEW] Sprite-only spin ----------------------
    [Header("Visual Spin (Sprite Only)")]
    [SerializeField, Tooltip("Degrees per second for the sprite spin. Does not affect movement.")]
    private float spinSpeed = 720f;                       // [NEW]
    [SerializeField, Tooltip("Optional. If empty, will auto-grab child SpriteRenderer transform.")]
    private Transform spriteTransform;                    // [NEW]
    private float _spinZ;                                  // [NEW]
    // -------------------------------------------------------------------

    private void Awake()
    {
        // [NEW] Auto-assign the sprite transform if not set in Inspector
        if (spriteTransform == null)
        {
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null) spriteTransform = sr.transform;
        }
    }

    public void Initialize(Vector3 direction, Transform target)
    {
        _direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.down;
        _target = target;
    }

    private void Update()
    {
        UpdateDirection();

        // Movement (unchanged)
        transform.position += _direction * (speed * Time.deltaTime);
        _timeAlive += Time.deltaTime;

        // [NEW] Spin only the sprite, not the body/rigidbody
        if (spriteTransform != null && Mathf.Abs(spinSpeed) > 0f)
        {
            _spinZ += spinSpeed * Time.deltaTime;
            spriteTransform.localRotation = Quaternion.Euler(0f, 0f, _spinZ);
        }

        if (_timeAlive >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void UpdateDirection()
    {
        if (_target == null) return;

        Vector3 desiredDirection = _target.position - transform.position;
        if (desiredDirection.sqrMagnitude <= Mathf.Epsilon) return;

        desiredDirection.Normalize();
        float interpolationFactor = 1f - Mathf.Exp(-followResponsiveness * Time.deltaTime);
        _direction = Vector3.Slerp(_direction, desiredDirection, interpolationFactor);
        _direction.Normalize();
    }
}
