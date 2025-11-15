using UnityEngine;

/// <summary>
/// Controla velocidade de Rigidbody2D durante slow motion
/// Use para Boss, nuvens que se movem com física, etc
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class RigidbodySlowMotion : MonoBehaviour, ISlowMotionable
{
    private Rigidbody2D rb;
    private Vector2 storedVelocity;
    private float currentScale = 1f;
    private bool isInSlowMotion = false;

    [Header("Settings")]
    [Tooltip("Se true, armazena velocidade ao entrar em slow motion e restaura ao sair")]
    public bool storeAndRestoreVelocity = true;
    
    private void Start()
    {
        // Registro automático no SlowMotionManager, se existir na cena
        if (SlowMotionManager.Instance != null)
        {
            SlowMotionManager.Instance.RegisterObject(this);
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        // Se está em slow motion, aplica escala continuamente
        if (isInSlowMotion && rb != null)
        {
            // Multiplica a velocidade atual pela escala
            rb.linearVelocity = rb.linearVelocity.normalized * rb.linearVelocity.magnitude * currentScale;
        }
    }

    public void SetSlowMotion(float scale)
    {
        if (rb == null) return;

        currentScale = scale;
        isInSlowMotion = true;

        if (storeAndRestoreVelocity)
        {
            // Armazena velocidade atual antes de aplicar slow motion
            storedVelocity = rb.linearVelocity;
        }

        // Aplica escala imediatamente
        rb.linearVelocity *= scale;

        Debug.Log($"[RigidbodySlowMotion] {gameObject.name} slow motion: {scale * 100}%");
    }

    public void ResetSpeed()
    {
        if (rb == null) return;

        isInSlowMotion = false;
        currentScale = 1f;

        if (storeAndRestoreVelocity && storedVelocity != Vector2.zero)
        {
            // Restaura velocidade original
            rb.linearVelocity = storedVelocity;
            Debug.Log($"[RigidbodySlowMotion] {gameObject.name} velocidade restaurada: {storedVelocity}");
        }
        else
        {
            // Apenas remove a escala
            rb.linearVelocity /= currentScale;
            Debug.Log($"[RigidbodySlowMotion] {gameObject.name} velocidade normal");
        }
    }
}