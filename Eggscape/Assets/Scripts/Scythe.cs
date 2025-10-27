using UnityEngine;

public class Scythe : MonoBehaviour
{
    [SerializeField]
    private float speed = 6f;

    [SerializeField]
    private float lifetime = 10f;

    [SerializeField]
    [Tooltip("How quickly the scythe adjusts its direction towards the target.")]
    private float followResponsiveness = 5f;

    private Vector3 _direction;
    private float _timeAlive;
    private Transform _target;

    public void Initialize(Vector3 direction, Transform target)
    {
        _direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.down;
        _target = target;
    }

    private void Update()
    {
        UpdateDirection();
        transform.position += _direction * (speed * Time.deltaTime);
        _timeAlive += Time.deltaTime;

        if (_timeAlive >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void UpdateDirection()
    {
        if (_target == null)
        {
            return;
        }

        Vector3 desiredDirection = _target.position - transform.position;
        if (desiredDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        desiredDirection.Normalize();
        float interpolationFactor = 1f - Mathf.Exp(-followResponsiveness * Time.deltaTime);
        _direction = Vector3.Slerp(_direction, desiredDirection, interpolationFactor);
        _direction.Normalize();
    }
}
