using UnityEngine;

/// <summary>
/// Componente para controlar velocidade de Animators durante slow motion
/// Adicione este script em objetos com Animator (Sol, Nuvens, etc)
/// </summary>
[RequireComponent(typeof(Animator))]
public class AnimatorSlowMotion : MonoBehaviour, ISlowMotionable
{
    private Animator animator;
    private float originalSpeed = 1f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator != null)
        {
            originalSpeed = animator.speed;
        }
    }

    public void SetSlowMotion(float scale)
    {
        if (animator != null)
        {
            animator.speed = originalSpeed * scale;
            Debug.Log($"[AnimatorSlowMotion] {gameObject.name} animator speed: {animator.speed}");
        }
    }

    public void ResetSpeed()
    {
        if (animator != null)
        {
            animator.speed = originalSpeed;
            Debug.Log($"[AnimatorSlowMotion] {gameObject.name} velocidade normal restaurada");
        }
    }
}