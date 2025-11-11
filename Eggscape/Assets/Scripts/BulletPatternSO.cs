using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BulletPattern", menuName = "Boss/Bullet Pattern", order = 0)]
public class BulletPatternSO : ScriptableObject
{
    public enum PatternType { Radial, Spiral, Fan, Aimed, Sequence }

    [Header("Tipo")]
    public PatternType pattern = PatternType.Radial;

    [Header("Timing")]
    [Tooltip("Duração total do ataque (segundos). O Boss também tem a própria duração; use o que preferir como referência.")]
    public float duration = 3f;

    [Tooltip("Tiros por segundo (ticks).")]
    public float shotsPerSecond = 6f;

    [Header("Bala")]
    public float bulletSpeed = 12f;
    public float bulletLifeTime = 3f;

    [Header("Radial")]
    public int radialCount = 16;

    [Header("Spiral")]
    [Tooltip("Revoluções por segundo da espiral.")]
    public float spiralRevsPerSec = 1f;
    [Tooltip("Número de braços (linhas) da espiral.")]
    public int spiralArms = 1;

    [Header("Fan (Leque)")]
    public int fanCount = 5;
    public float fanSpreadDeg = 50f;
    [Tooltip("Ângulo base do leque (0° = direita).")]
    public float fanBaseDeg = 0f;

    [Header("Aimed (Mirado)")]
    public int aimedBulletsPerBurst = 3;
    public float aimedSpreadDeg = 20f;

    [Header("Sequence (Artesanal)")]
    [Tooltip("Ângulos em graus disparados na ordem dada, repetindo em loop (opcional).")]
    public List<float> sequenceAnglesDeg = new List<float>(){ 0f, 45f, -45f, 90f, -90f };
    public bool sequenceLoop = true;

    // ----------------- API -----------------

    public float FireInterval => Mathf.Max(0.01f, 1f / Mathf.Max(0.01f, shotsPerSecond));

    /// <summary>
    /// Gera um conjunto de direções (vetores normalizados) para ESTE “burst/tick”.
    /// </summary>
    public List<Vector2> GenerateBurst(float elapsed, Transform boss, Transform player)
    {
        switch (pattern)
        {
            case PatternType.Radial:  return GenRadial();
            case PatternType.Spiral:  return GenSpiral(elapsed);
            case PatternType.Fan:     return GenFan();
            case PatternType.Aimed:   return GenAimed(boss, player);
            case PatternType.Sequence:return GenSequence();
            default:                  return GenRadial();
        }
    }

    // ---- Implementações ----
    private List<Vector2> GenRadial()
    {
        int n = Mathf.Max(1, radialCount);
        float step = 360f / n;
        var list = new List<Vector2>(n);
        for (int i = 0; i < n; i++)
        {
            float ang = step * i;
            list.Add(AngleToDir(ang));
        }
        return list;
    }

    private List<Vector2> GenSpiral(float elapsed)
    {
        float baseAng = elapsed * spiralRevsPerSec * 360f;
        int arms = Mathf.Max(1, spiralArms);
        float armStep = 360f / arms;

        var list = new List<Vector2>(arms);
        for (int i = 0; i < arms; i++)
        {
            float ang = baseAng + armStep * i;
            list.Add(AngleToDir(ang));
        }
        return list;
    }

    private List<Vector2> GenFan()
    {
        int count = Mathf.Max(1, fanCount);
        float spread = fanSpreadDeg;
        float half = (count > 1) ? spread * 0.5f : 0f;
        float step = (count > 1) ? spread / (count - 1) : 0f;

        var list = new List<Vector2>(count);
        for (int i = 0; i < count; i++)
        {
            float ang = fanBaseDeg - half + step * i;
            list.Add(AngleToDir(ang));
        }
        return list;
    }

    private List<Vector2> GenAimed(Transform boss, Transform player)
    {
        Vector2 dirToPlayer = Vector2.right;
        if (player != null)
        {
            dirToPlayer = (player.position - boss.position);
            if (dirToPlayer.sqrMagnitude > 0.0001f) dirToPlayer.Normalize();
            else dirToPlayer = Vector2.right;
        }

        int count = Mathf.Max(1, aimedBulletsPerBurst);
        float spread = aimedSpreadDeg;
        float half = (count > 1) ? spread * 0.5f : 0f;
        float step = (count > 1) ? spread / (count - 1) : 0f;

        float baseAng = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;

        var list = new List<Vector2>(count);
        for (int i = 0; i < count; i++)
        {
            float ang = baseAng - half + step * i;
            list.Add(AngleToDir(ang));
        }
        return list;
    }

    private int sequenceIndex = 0;
    private List<Vector2> GenSequence()
    {
        var list = new List<Vector2>(1);
        if (sequenceAnglesDeg == null || sequenceAnglesDeg.Count == 0)
        {
            list.Add(Vector2.right);
            return list;
        }

        float ang = sequenceAnglesDeg[Mathf.Clamp(sequenceIndex, 0, sequenceAnglesDeg.Count - 1)];
        list.Add(AngleToDir(ang));

        sequenceIndex++;
        if (sequenceLoop) sequenceIndex %= sequenceAnglesDeg.Count;
        else sequenceIndex = Mathf.Min(sequenceIndex, sequenceAnglesDeg.Count - 1);
        return list;
    }

    private Vector2 AngleToDir(float angDeg)
    {
        float rad = angDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }
}
