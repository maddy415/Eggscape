using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sapo sentinela: vai continuamente pra esquerda (scroll) e, periodicamente, dá um pulo vertical com pré-animação.
/// Ignora colisões com a layer "Bird".
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class FrogIdleJumper : MonoBehaviour
{
    [Header("Pulo e Velocidade")]
    [SerializeField] private float jumpForce = 9.5f;
    [SerializeField] public float speed = 3f; // velocidade constante pra esquerda

    [Header("Tempo entre pulos")]
    [SerializeField] private float minInterval = 2f;
    [SerializeField] private float maxInterval = 3f;
    [SerializeField] private bool jumpOnAwake = true;

    [Header("Pré-pulo (tempo de carregamento da animação)")]
    [SerializeField] private float preJumpDelay = 0.35f;

    [Header("Checagem de chão")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundRadius = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Animação")]
    [SerializeField] private Animator animator;
    [SerializeField] private string animTriggerPreJump = "PreJump";
    [SerializeField] private string animTriggerJump = "Jump";
    [SerializeField] private string animBoolGrounded = "Grounded";

    private Rigidbody2D rb;
    private float timer;
    private bool grounded;
    private bool chargingJump;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        ResetTimer(jumpOnAwake ? 0f : Random.Range(minInterval, maxInterval));

        // Ignora colisão entre Frog e Bird por layer
        int frogLayer = LayerMask.NameToLayer("Frog");
        int birbLayer = LayerMask.NameToLayer("Birb");

        if (frogLayer >= 0 && birbLayer >= 0)
        {
            Physics2D.IgnoreLayerCollision(frogLayer, birbLayer, true);
        }
        else
        {
            Debug.LogWarning("Layer 'Frog' ou 'Bird' não encontrada! Verifica se elas existem no projeto.");
        }
    }

    private void Update()
    {
        
        grounded = IsGrounded();

        if (animator)
            animator.SetBool(animBoolGrounded, grounded);

        if (!chargingJump)
        {
            timer -= Time.deltaTime;

            if (grounded && timer <= 0f)
            {
                StartCoroutine(PrepareJump());
                ResetTimer(Random.Range(minInterval, maxInterval));
            }
        }
    }

    private void FixedUpdate()
    {
        var v = new Vector2(-Mathf.Abs(speed), rb.linearVelocity.y);
        rb.linearVelocity = v;
    }

    



    private IEnumerator PrepareJump()
    {
        chargingJump = true;

        if (animator && !string.IsNullOrEmpty(animTriggerPreJump))
            animator.SetTrigger(animTriggerPreJump);

        yield return new WaitForSeconds(preJumpDelay);

        DoJump();

        chargingJump = false;
    }

    private void DoJump()
    {
        // zera só o Y pra pulo consistente, mantendo a velocidade pra esquerda
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        if (animator && !string.IsNullOrEmpty(animTriggerJump))
            animator.SetTrigger(animTriggerJump);
    }

    private bool IsGrounded()
    {
        Vector3 checkPos = groundCheck ? groundCheck.position : transform.position;
        return Physics2D.OverlapCircle(checkPos, groundRadius, groundLayer);
    }

    private void ResetTimer(float value)
    {
        if (minInterval > maxInterval)
        {
            float t = minInterval;
            minInterval = maxInterval;
            maxInterval = t;
        }
        timer = value;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.9f, 0.4f, 0.5f);
        Vector3 p = groundCheck ? groundCheck.position : transform.position;
        Gizmos.DrawSphere(p, groundRadius);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Destroyer"))
        {
            // Remove antes de destruir (mantém tua lógica existente)
            ObstacleGen.logObstacle.Remove(gameObject);
            GameManager.Instance.objsOnScene.Remove(gameObject);
            Destroy(gameObject);
        }
    }
}
