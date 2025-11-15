using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerencia slow motion em objetos individuais sem mexer no Time.timeScale
/// </summary>
public class SlowMotionManager : MonoBehaviour
{
    public static SlowMotionManager Instance { get; private set; }

    [Header("Slow Motion Settings")]
    [Range(0.01f, 1f)]
    public float slowMotionScale = 0.2f; // 20% da velocidade

    [Header("References - Adicione tudo que precisa ficar lento")]
    public List<MonoBehaviour> slowMotionObjects = new List<MonoBehaviour>();

    private List<ISlowMotionable> slowMotionables = new List<ISlowMotionable>();
    private bool isSlowMotionActive = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Converte MonoBehaviours em ISlowMotionable
        CacheSlowMotionables();
    }

    private void CacheSlowMotionables()
    {
        slowMotionables.Clear();

        foreach (var obj in slowMotionObjects)
        {
            if (obj == null) continue;

            if (obj is ISlowMotionable slowable)
            {
                slowMotionables.Add(slowable);
            }
            else
            {
                Debug.LogWarning($"[SlowMotionManager] {obj.name} não implementa ISlowMotionable!");
            }
        }

        Debug.Log($"[SlowMotionManager] {slowMotionables.Count} objetos registrados para slow motion");
    }

    /// <summary>
    /// Ativa slow motion em todos os objetos registrados
    /// </summary>
    public void ActivateSlowMotion()
    {
        if (isSlowMotionActive) return;

        Debug.Log($"[SlowMotionManager] Ativando slow motion (scale: {slowMotionScale})");
        isSlowMotionActive = true;

        foreach (var slowable in slowMotionables)
        {
            slowable.SetSlowMotion(slowMotionScale);
        }
    }

    /// <summary>
    /// Desativa slow motion, retornando tudo à velocidade normal
    /// </summary>
    public void DeactivateSlowMotion()
    {
        if (!isSlowMotionActive) return;

        Debug.Log("[SlowMotionManager] Desativando slow motion");
        isSlowMotionActive = false;

        foreach (var slowable in slowMotionables)
        {
            slowable.ResetSpeed();
        }
    }

    /// <summary>
    /// Verifica se slow motion está ativo
    /// </summary>
    public bool IsSlowMotionActive()
    {
        return isSlowMotionActive;
    }

    /// <summary>
    /// Adiciona um objeto dinamicamente
    /// </summary>
    public void RegisterObject(MonoBehaviour obj)
    {
        if (obj is ISlowMotionable slowable && !slowMotionables.Contains(slowable))
        {
            slowMotionables.Add(slowable);
            
            // Se slow motion já está ativo, aplica imediatamente
            if (isSlowMotionActive)
            {
                slowable.SetSlowMotion(slowMotionScale);
            }
        }
    }

    /// <summary>
    /// Remove um objeto
    /// </summary>
    public void UnregisterObject(MonoBehaviour obj)
    {
        if (obj is ISlowMotionable slowable)
        {
            slowMotionables.Remove(slowable);
        }
    }
}