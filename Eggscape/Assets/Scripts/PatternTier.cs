using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Representa um tier de dificuldade, com uma lista de padrões que podem ser usados.
/// </summary>
[CreateAssetMenu(menuName = "Eggscape/Pattern Tier")]
public class PatternTier : ScriptableObject
{
    [Tooltip("Lista de padrões (prefabs) que pertencem a esse tier de dificuldade.")]
    public List<GameObject> patterns;
}