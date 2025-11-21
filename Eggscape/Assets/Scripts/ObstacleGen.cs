using UnityEngine;
using System.Collections.Generic;

public class ObstacleGen : MonoBehaviour
{
    public GameObject tronco;
    public GameObject spawner;
    public bool onTutorial;

    public float spawnTime = 3f;
    private float timer = 0;
    private bool spawned = false;
    public bool canSpawn = true;

    public static List<GameObject> logObstacle = new List<GameObject>();

    private void Start()
    {
        canSpawn = true;
        // opcional: cache de objetos na cena (não necessário)
        // GameObject[] troncosCena = GameObject.FindGameObjectsWithTag("Obstacle");
    }

    // agora retorna o clone instanciado
    public GameObject SpawnObstacle()
    {
        GameObject troncoClone = Instantiate(tronco, spawner.transform.position, Quaternion.identity);

        // adiciona nos logs / gerenciadores assim como no Update faz
        logObstacle.Add(troncoClone);
        if (GameManager.Instance != null)
            GameManager.Instance.objsOnScene.Add(troncoClone);

        // se o prefab tiver ObstacleMove e precisa inicializar com LevelSegment, faz aqui:
        ObstacleMove om = troncoClone.GetComponent<ObstacleMove>();
        if (om != null)
        {
            // se você tiver LevelSegment default/atual, passe aqui. Se não, Init(null) é seguro.
            om.Init(null);
        }

        return troncoClone;
    }

    void Update()
    {
        if (onTutorial)
        {
            return;
        }
        else
        {
            timer += Time.deltaTime;
            if (timer >= spawnTime && canSpawn)
            {
                GameObject troncoClone = Instantiate(tronco, spawner.transform.position, Quaternion.identity);
                logObstacle.Add(troncoClone);
                if (GameManager.Instance != null)
                    GameManager.Instance.objsOnScene.Add(troncoClone);

                spawned = true;
            }
            if (spawned)
            {
                timer = 0;
                spawned = false;
            }

            if (GameManager.Instance != null && GameManager.Instance.playerAlive == false)
            {
                canSpawn = false;
                foreach (GameObject troncoClone in logObstacle)
                {
                    if (troncoClone != null)
                    {
                        var om = troncoClone.GetComponent<ObstacleMove>();
                        if (om != null)
                        {
                            // desativa o comportamento para congelar os troncos ao morrer (opcional)
                            om.enabled = false;
                        }
                    }
                }
            }
        }
    }
}
