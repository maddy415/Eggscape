using UnityEngine;

/// <summary>
/// Controla velocidade de Rigidbody2D durante slow motion.
/// 
/// - Quando o slow ativa: multiplica a velocidade atual pelo scale UMA vez.
/// - Quando o slow termina: desfaz essa multiplicação.
/// 
/// NÃO usa FixedUpdate para reaplicar escala infinitamente
/// (isso evita que o boss "congele").
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class RigidbodySlowMotion : MonoBehaviour, ISlowMotionable
{
    private Rigidbody2D rb;
    private float currentScale = 1f;
    private bool isInSlowMotion = false;

    private Vector2 storedVelocity;
    private float originalGravityScale;

    [Header("Settings")]
    [SerializeField] private bool scaleGravity = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravityScale = rb.gravityScale;
    }

    private void Start()
    {
        // Registro automático no SlowMotionManager, se existir na cena
        if (SlowMotionManager.Instance != null)
        {
            SlowMotionManager.Instance.RegisterObject(this);
        }
    }

    public void SetSlowMotion(float scale)
    {
        if (rb == null) return;

        scale = Mathf.Clamp(scale, 0.01f, 1f);

        // Se já estava em slow, desfaz o scale anterior antes de aplicar o novo
        if (isInSlowMotion && !Mathf.Approximately(currentScale, 1f))
        {
            rb.linearVelocity /= currentScale;
            if (scaleGravity)
                rb.gravityScale /= currentScale;
        }

        // Guarda a nova escala
        currentScale = scale;
        isInSlowMotion = true;

        // Aplica o slow uma vez só
        rb.linearVelocity *= currentScale;

        if (scaleGravity)
            rb.gravityScale *= currentScale;

        Debug.Log($"[RigidbodySlowMotion] {gameObject.name} slow: {(currentScale * 100f):0}%");
    }

    public void ResetSpeed()
    {
        if (!isInSlowMotion || rb == null)
            return;

        // Desfaz a escala aplicada
        if (!Mathf.Approximately(currentScale, 1f))
        {
            rb.linearVelocity /= currentScale;

            if (scaleGravity)
                rb.gravityScale = originalGravityScale;
        }

        currentScale = 1f;
        isInSlowMotion = false;

        Debug.Log($"[RigidbodySlowMotion] {gameObject.name} voltou ao normal");
    }
}
