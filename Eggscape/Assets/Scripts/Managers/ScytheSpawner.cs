using System.Collections;
using UnityEngine;

public class ScytheSpawner : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField]
    private Transform player;

    [SerializeField]
    private GameObject scythePrefab;

    [Header("Spawn Settings")]
    [SerializeField]
    private float spawnHeightOffset = 3f;

    [SerializeField]
    private float spawnInterval = 1f;

    [SerializeField]
    private float spawnDuration = 5f;

    private Coroutine _spawnRoutine;

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Input.GetKeyDown(KeyCode.K))
        {
            ToggleSpawnRoutine();
        }
#endif
    }

    private void ToggleSpawnRoutine()
    {
        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
        }

        _spawnRoutine = StartCoroutine(SpawnScythesForDuration());
    }

    private IEnumerator SpawnScythesForDuration()
    {
        float elapsedTime = 0f;

        while (elapsedTime < spawnDuration)
        {
            SpawnScythe();
            float waitTime = Mathf.Min(spawnInterval, spawnDuration - elapsedTime);
            yield return new WaitForSeconds(waitTime);
            elapsedTime += waitTime;
        }

        _spawnRoutine = null;
    }

    private void SpawnScythe()
    {
        if (player == null || scythePrefab == null)
        {
            Debug.LogWarning("ScytheSpawner is missing references to player or scythePrefab.");
            return;
        }

        Vector3 spawnPosition = player.position + Vector3.up * spawnHeightOffset;
        GameObject scytheInstance = Instantiate(scythePrefab, spawnPosition, Quaternion.identity);

        Scythe scytheComponent = scytheInstance.GetComponent<Scythe>();
        if (scytheComponent != null)
        {
            Vector3 direction = (player.position - spawnPosition).normalized;
            scytheComponent.Initialize(direction, player);
        }
    }
}
