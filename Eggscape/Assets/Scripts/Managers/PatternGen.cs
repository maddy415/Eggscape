using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Script que instancia padrões (chunks) com base em um sistema de dificuldade dividido em fases.
/// Agora adaptado para funcionar com tiers + controle por LevelData.
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

    [Tooltip("Tag usada pra localizar o trigger de spawn (não obrigatório, mas mantido por compatibilidade).")]
    public GameObject nextPatternSpawn;

    [Header("Controle de spawn")]
    [Tooltip("Se estiver falso, impede o spawn de novos patterns.")]
    public bool canSpawn = true;

    private SpawnTriggerHandler handler; // Referência ao script do trigger
    private GameObject lastPattern;      // Último padrão instanciado, pra evitar repetição

    [Header("Internos")]
    public GameObject[] patterns;                    // (LEGADO) array antigo de patterns
    public List<GameObject> patternsList = new();    // (LEGADO) lista com os patterns originais

    // Controle de progressão do level
    private int currentSegmentIndex = 0;             // Em qual segmento (fase) estamos
    private int patternsSpawnedInSegment = 0;        // Quantos padrões já foram spawnados nesse segmento

    void Start()
    {
        // Pega o handler do trigger
        handler = SpawnTrigger.GetComponent<SpawnTriggerHandler>();

        // (LEGADO) encontra objeto com tag de próximo spawn
        nextPatternSpawn = GameObject.FindWithTag("SpawnNextTrigger");

        // (LEGADO) adiciona os patterns antigos à lista
        foreach (GameObject pattern in patterns)
        {
            patternsList.Add(pattern);
        }

        // Spawna o primeiro padrão ao iniciar
        SpawnPattern();
    }

    void Update()
    {
        // Quando o trigger de colisão indicar que devemos spawnar o próximo
        if (handler.TriggeredSpawn)
        {
            Debug.Log("TriggeredSpawn detectado!");
            SpawnPattern();
            handler.TriggeredSpawn = false; // reseta o gatilho
        }
    }

    void SpawnPattern()
    {
        Debug.Log("SpawnPattern() foi chamado");

        if (!canSpawn) {
            Debug.Log("Spawn bloqueado!");
            return;
        }

        if (currentSegmentIndex < levelData.segments.Count)
        {
            LevelSegment segment = levelData.segments[currentSegmentIndex];

            // Novo jeito: usamos patternTier diretamente
            if (segment.patternTier != null && segment.patternTier.patterns.Count > 0)
            {
                var tierList = segment.patternTier.patterns;

                GameObject chosenPattern;
                do
                {
                    chosenPattern = tierList[Random.Range(0, tierList.Count)];
                }
                while (chosenPattern == lastPattern && tierList.Count > 1);

                lastPattern = chosenPattern;

                GameObject patternClone = Instantiate(chosenPattern, spawnPoint.transform.position, Quaternion.identity);

                foreach (Transform child in patternClone.transform)
                {
                    GameManager.Instance.objsOnScene.Add(child.gameObject);
                }

                Debug.Log($"Spawned pattern: {chosenPattern.name} | Tier: {segment.patternTier.name}");

                patternsSpawnedInSegment++;

                if (patternsSpawnedInSegment >= segment.patternsToSpawn)
                {
                    currentSegmentIndex++;
                    patternsSpawnedInSegment = 0;
                }
            }
            else
            {
                Debug.LogWarning($"O PatternTier de {segment.name} está nulo ou vazio!");
            }
        }
        else
        {
            Debug.Log("Fase finalizada! Nenhum padrão novo será gerado.");
            canSpawn = false;
        }
    }


    // (LEGADO) método antigo, você pode usar se quiser delays antes de spawnar
    IEnumerator WaitToSpawn()
    {
        yield return new WaitForSeconds(2f);
        SpawnPattern();
    }
}
