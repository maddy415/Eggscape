using UnityEngine;

public class TelegraphedStrike : MonoBehaviour
{
    [Header("Preparation Phase")]
    [SerializeField]
    private float preparationDuration = 1.25f;

    [SerializeField]
    private float preparationMoveDistance = 1.5f;

    [Header("Attack Phase")]
    [SerializeField]
    private float attackSpeed = 20f;

    [SerializeField]
    private float attackOvershootDistance = 1.5f;

    [SerializeField]
    private float selfDestructDelayAfterAttack = 0.5f;

    private Vector3 _recordedTargetPosition;
    private Vector3 _preparationStartPosition;
    private float _phaseTimer;
    private bool _isAttacking;
    private bool _hasInitialized;
    private Vector3 _attackDirection;
    private float _attackTravelDistance;
    private float _attackDistanceCovered;

    public void Initialize(Vector3 targetPosition)
    {
        _recordedTargetPosition = targetPosition;
        _preparationStartPosition = transform.position;
        _phaseTimer = 0f;
        _isAttacking = false;
        _attackDistanceCovered = 0f;
        _hasInitialized = true;
    }

    private void Update()
    {
        if (!_hasInitialized)
        {
            return;
        }

        if (_isAttacking)
        {
            UpdateAttackPhase();
        }
        else
        {
            UpdatePreparationPhase();
        }
    }

    private void UpdatePreparationPhase()
    {
        _phaseTimer += Time.deltaTime;

        float t = Mathf.Clamp01(preparationDuration > 0f ? _phaseTimer / preparationDuration : 1f);
        float displacement = Mathf.Max(0f, preparationMoveDistance) * t;
        transform.position = _preparationStartPosition - Vector3.up * displacement;

        if (_phaseTimer >= preparationDuration)
        {
            BeginAttack();
        }
    }

    private void BeginAttack()
    {
        Vector3 direction = _recordedTargetPosition - transform.position;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            direction = Vector3.up;
        }

        _attackDirection = direction.normalized;
        _attackTravelDistance = direction.magnitude + Mathf.Max(0f, attackOvershootDistance);
        _attackDistanceCovered = 0f;
        _phaseTimer = 0f;
        _isAttacking = true;
    }

    private void UpdateAttackPhase()
    {
        float frameDistance = Mathf.Max(0f, attackSpeed) * Time.deltaTime;
        transform.position += _attackDirection * frameDistance;
        _attackDistanceCovered += frameDistance;

        if (_attackDistanceCovered >= _attackTravelDistance || frameDistance <= Mathf.Epsilon)
        {
            _isAttacking = false;
            _phaseTimer = 0f;
        }
    }

    private void LateUpdate()
    {
        if (!_hasInitialized)
        {
            return;
        }

        if (!_isAttacking && _attackDistanceCovered > 0f)
        {
            _phaseTimer += Time.deltaTime;
            if (_phaseTimer >= selfDestructDelayAfterAttack)
            {
                Destroy(gameObject);
            }
        }
    }
}
