using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Instancia padrões (chunks) com base em segments do LevelData.
/// O LevelSegment ativo fica SOMENTE aqui e é injetado nos ObstacleMove dos chunks ao spawnar.
/// </summary>
public class PatternGen : MonoBehaviour
{
    [Header("Tiers de dificuldade")]
    [Tooltip("Cada tier representa um nível de dificuldade com seus próprios patterns.")]
    public List<PatternTier> difficultyTiers;

    [Header("Configuração da fase")]
    [Tooltip("ScriptableObject que define os segmentos da fase.")]
    public LevelData levelData;

    [Header("Objetos de cena")]
    [Tooltip("Ponto onde os patterns vão ser instanciados.")]
    public GameObject spawnPoint;

    [Tooltip("Objeto com o script SpawnTriggerHandler, que detecta quando spawnar o próximo padrão.")]
    public GameObject SpawnTrigger;

    [Tooltip("Objeto com a tag 'SpawnNextTrigger' (compatibilidade).")]
    public GameObject nextPatternSpawn;

    [Header("Controle de spawn")]
    [Tooltip("Se falso, impede o spawn de novos patterns.")]
    public bool canSpawn = true;

    [Tooltip("Se verdadeiro, os patterns serão spawnados em ordem ao invés de aleatoriamente.")]
    public bool spawnInOrder = false;

    [Header("Injeção de Segmento")]
    [Tooltip("Aplica o LevelSegment atual a todos os ObstacleMove do pattern instanciado.")]
    public bool propagateSegmentToChildren = true;

    [Header("Debug")]
    [Tooltip("Segmento atualmente ativo (apenas leitura).")]
    public LevelSegment activeSegment; // só pra visualizar no Inspector

    // Internos
    private SpawnTriggerHandler handler;
    private GameObject lastPattern;
    private int orderedPatternIndex = 0;

    [Header("Legado (se ainda usa)")]
    public GameObject[] patterns;
    public List<GameObject> patternsList = new();

    // Progresso por segmento
    private int currentSegmentIndex = 0;
    private int patternsSpawnedInSegment = 0;

    void Start()
    {
        handler = SpawnTrigger.GetComponent<SpawnTriggerHandler>();
        nextPatternSpawn = GameObject.FindWithTag("SpawnNextTrigger");

        // Suporte à lista legada
        foreach (GameObject pattern in patterns)
            patternsList.Add(pattern);

        SpawnPattern();
    }

    void Update()
    {
        if (handler != null && handler.TriggeredSpawn)
        {
            Debug.Log("TriggeredSpawn detectado!");
            SpawnPattern();
            handler.TriggeredSpawn = false;
        }
    }

    void SpawnPattern()
    {
        if (!canSpawn)
        {
            Debug.Log("Spawn bloqueado!");
            return;
        }

        if (levelData == null || levelData.segments == null || levelData.segments.Count == 0)
        {
            Debug.LogWarning("[PatternGen] LevelData ou seus segments estão nulos/vazios.");
            canSpawn = false;
            return;
        }

        if (currentSegmentIndex >= levelData.segments.Count)
        {
            Debug.Log("Fase finalizada! Nenhum padrão novo será gerado.");
            canSpawn = false;
            return;
        }

        // Define o segmento ativo (só no spawner)
        activeSegment = levelData.segments[currentSegmentIndex];

        if (activeSegment == null)
        {
            Debug.LogWarning($"[PatternGen] Segmento {currentSegmentIndex} é nulo.");
            AvancaSegmento();
            return;
        }

        if (activeSegment.patternTier == null || activeSegment.patternTier.patterns == null || activeSegment.patternTier.patterns.Count == 0)
        {
            Debug.LogWarning($"[PatternGen] PatternTier do segmento '{activeSegment.name}' está nulo ou sem patterns.");
            AvancaSegmento();
            return;
        }

        var tierList = activeSegment.patternTier.patterns;
        GameObject chosenPattern;

        // Seleção ordenada ou aleatória (evitando repetição)
        if (spawnInOrder)
        {
            chosenPattern = tierList[orderedPatternIndex];
            orderedPatternIndex = (orderedPatternIndex + 1) % tierList.Count;
        }
        else
        {
            do
            {
                chosenPattern = tierList[Random.Range(0, tierList.Count)];
            }
            while (chosenPattern == lastPattern && tierList.Count > 1);
        }

        lastPattern = chosenPattern;

        // Instancia o chunk/pattern
        GameObject patternClone = Instantiate(chosenPattern, spawnPoint.transform.position, Quaternion.identity);
        

        // Injeção do LevelSegment nos ObstacleMove
        if (propagateSegmentToChildren)
            InjectSegmentIntoChildren(patternClone, activeSegment);

        // Registra no GameManager (mantém tua lógica existente)
        foreach (Transform child in patternClone.transform)
            GameManager.Instance.objsOnScene.Add(child.gameObject);

        // DEBUG: posição do spawn
        Vector3 pos = spawnPoint.transform.position;
        Debug.Log($"[PatternGen] 📦 Pattern '{chosenPattern.name}' instanciado em: X={pos.x:F2}, Y={pos.y:F2}, Z={pos.z:F2} | Segmento #{currentSegmentIndex}");
        // Contabiliza e verifica avanço de segmento
        patternsSpawnedInSegment++;
        if (patternsSpawnedInSegment >= activeSegment.patternsToSpawn)
            AvancaSegmento();
    }

    IEnumerator WaitToSpawn()
    {
        yield return new WaitForSeconds(2f);
        SpawnPattern();
    }

    /// <summary>
    /// Aplica o LevelSegment a todos os ObstacleMove do objeto root.
    /// </summary>
    private void InjectSegmentIntoChildren(GameObject root, LevelSegment seg)
    {
        // Obstáculos
        foreach (var m in root.GetComponentsInChildren<ObstacleMove>(true))
            m.Init(seg);

        // Pássaros
        foreach (var bird in root.GetComponentsInChildren<BirdMove>(true))
            bird.Init(seg);
    }


    /// <summary>
    /// Avança para o próximo segmento e reseta contadores necessários.
    /// </summary>
    private void AvancaSegmento()
    {
        currentSegmentIndex++;
        patternsSpawnedInSegment = 0;
        orderedPatternIndex = 0;
    }
}
