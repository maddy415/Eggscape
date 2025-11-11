using System.Collections.Generic;
using UnityEngine;

public class ScythePool : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private ScytheProjectile prefab;
    [SerializeField] private int preload = 16;
    [SerializeField] private Transform container; // opcional (organização na Hierarchy)

    private readonly Queue<ScytheProjectile> pool = new Queue<ScytheProjectile>();

    private void Awake()
    {
        if (!container) container = transform;
        Warmup();
    }

    private void Warmup()
    {
        for (int i = 0; i < Mathf.Max(1, preload); i++)
        {
            var p = Instantiate(prefab, container);
            p.gameObject.SetActive(false);
            p.AttachPool(this);
            pool.Enqueue(p);
        }
    }

    private ScytheProjectile CreateOne()
    {
        var p = Instantiate(prefab, container);
        p.gameObject.SetActive(false);
        p.AttachPool(this);
        return p;
    }

    public ScytheProjectile Spawn(Vector3 pos, Vector2 dir, float speed, float ttl, LayerMask hitMask)
    {
        var p = pool.Count > 0 ? pool.Dequeue() : CreateOne();
        p.transform.position = pos;
        p.gameObject.SetActive(true);
        p.Initialize(dir, speed, ttl, hitMask);
        return p;
    }

    public void Despawn(ScytheProjectile p)
    {
        if (!p) return;
        p.gameObject.SetActive(false);
        pool.Enqueue(p);
    }
}