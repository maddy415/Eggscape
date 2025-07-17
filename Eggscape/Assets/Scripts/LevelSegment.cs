using UnityEngine;

/// <summary>
/// Define um segmento da fase, com a quantidade de padrões a spawnar e o tier de dificuldade.
/// </summary>
[CreateAssetMenu(menuName = "Eggscape/Level Segment")]
public class LevelSegment : ScriptableObject
{
    [Tooltip("Quantos patterns vão ser instanciados nesse segmento da fase.")]
    public int patternsToSpawn = 5;

    [Tooltip("Tier de dificuldade que esse segmento vai usar.")]
    public PatternTier patternTier;
}