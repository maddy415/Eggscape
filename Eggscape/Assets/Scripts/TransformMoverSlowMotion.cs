using UnityEngine;

/// <summary>
/// Para objetos que se movem via Transform (nuvens, sprites, etc)
/// Permite definir movimento e direção, afetado por slow motion
/// </summary>
public class TransformMoverSlowMotion : MonoBehaviour, ISlowMotionable
{
    [Header("Movement Settings")]
    public Vector3 moveDirection = Vector3.left;
    public float moveSpeed = 2f;
    public bool moveOnStart = true;

    private float speedMultiplier = 1f;
    private bool isMoving = false;

    private void Start()
    {
        if (moveOnStart)
        {
            isMoving = true;
        }
    }

    private void Update()
    {
        if (!isMoving) return;

        float currentSpeed = moveSpeed * speedMultiplier;
        transform.Translate(moveDirection * currentSpeed * Time.deltaTime, Space.World);
    }

    public void SetSlowMotion(float scale)
    {
        speedMultiplier = scale;
        Debug.Log($"[TransformMover] {gameObject.name} slow motion: {scale * 100}%");
    }

    public void ResetSpeed()
    {
        speedMultiplier = 1f;
        Debug.Log($"[TransformMover] {gameObject.name} velocidade normal");
    }

    /// <summary>
    /// Controla se o objeto está se movendo
    /// </summary>
    public void SetMoving(bool moving)
    {
        isMoving = moving;
    }
}