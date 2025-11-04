using UnityEngine;
using System.Collections.Generic;

public class CloudsManager : MonoBehaviour
{
    [Header("Configurações Gerais")]
    [Tooltip("Velocidade base de movimento das nuvens (em unidades por segundo).")]
    public float baseSpeed = 1.5f;

    [Tooltip("Variação percentual aleatória de velocidade (ex: 0.2 = ±20%).")]
    [Range(0f, 1f)] public float speedVariation = 0.25f;

    [Tooltip("Pontos possíveis de respawn (só a posição X será usada).")]
    public Transform[] respawnPoints;

    [Tooltip("Se verdadeiro, as nuvens respawnam em pontos aleatórios da lista.")]
    public bool useRandomRespawn = true;

    [Header("Trigger de Reset")]
    [Tooltip("Tag do trigger invisível que causa o respawn.")]
    public string triggerTag = "CloudReset";

    // Estrutura para guardar info individual de cada nuvem
    private class CloudData
    {
        public Transform transform;
        public float speed;
        public float originalY;
    }

    private List<CloudData> clouds = new List<CloudData>();

    private void Start()
    {
        // pega todos os filhos e gera velocidades únicas
        foreach (Transform child in transform)
        {
            CloudData data = new CloudData();
            data.transform = child;
            data.originalY = child.position.y;

            float variation = 1f + Random.Range(-speedVariation, speedVariation);
            data.speed = baseSpeed * variation;

            clouds.Add(data);
        }
    }

    private void Update()
    {
        // Move cada nuvem individualmente
        foreach (CloudData c in clouds)
        {
            if (c.transform != null)
            {
                c.transform.Translate(Vector3.left * c.speed * Time.deltaTime, Space.World);
            }
        }
    }

    public void RespawnCloud(Transform cloudTransform)
    {
        CloudData cloud = clouds.Find(c => c.transform == cloudTransform);
        if (cloud == null) return;

        if (respawnPoints.Length == 0)
        {
            Debug.LogWarning("Nenhum ponto de respawn definido no CloudsManager.");
            return;
        }

        // escolhe ponto base (só usa o X)
        Transform target = useRandomRespawn
            ? respawnPoints[Random.Range(0, respawnPoints.Length)]
            : respawnPoints[0];

        // mantém a altura original
        Vector3 newPos = new Vector3(target.position.x, cloud.originalY, cloudTransform.position.z);
        cloudTransform.position = newPos;

        // pode gerar nova variação de velocidade pra dar vida
        float variation = 1f + Random.Range(-speedVariation, speedVariation);
        cloud.speed = baseSpeed * variation;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // opcional: se o collider do trigger estiver no mesmo objeto
        if (other.CompareTag(triggerTag))
        {
            RespawnCloud(other.transform);
        }
    }
}
