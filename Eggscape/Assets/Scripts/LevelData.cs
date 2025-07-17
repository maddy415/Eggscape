using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Armazena a configuração completa de uma fase. É um ScriptableObject que você cria e preenche no Inspector.
/// </summary>
[CreateAssetMenu(menuName = "Eggscape/Level Config")]
public class LevelData : ScriptableObject
{
    [Tooltip("Lista de segmentos que definem a progressão de dificuldade ao longo da fase.")]
    public List<LevelSegment> segments;

    [Tooltip("Tiers de dificuldade disponíveis para essa fase. Cada índice corresponde ao Tier ID.")]
    public List<PatternTier> tiers;
}