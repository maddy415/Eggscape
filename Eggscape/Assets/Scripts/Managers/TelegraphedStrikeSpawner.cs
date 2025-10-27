using UnityEngine;

public class TelegraphedStrikeSpawner : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField]
    private Transform player;

    [SerializeField]
    private GameObject strikePrefab;

    [Header("Spawn Settings")]
    [SerializeField]
    private float spawnDepthOffset = 2f;

    [SerializeField]
    private KeyCode triggerKey = KeyCode.L;

    private void Update()
    {
        if (Input.GetKeyDown(triggerKey))
        {
            SpawnStrike();
        }
    }

    private void SpawnStrike()
    {
        if (player == null || strikePrefab == null)
        {
            Debug.LogWarning("TelegraphedStrikeSpawner is missing references to the player or strike prefab.");
            return;
        }

        Vector3 targetPosition = player.position;
        Vector3 spawnPosition = targetPosition - Vector3.up * spawnDepthOffset;

        GameObject strikeInstance = Instantiate(strikePrefab, spawnPosition, Quaternion.identity);
        if (strikeInstance.TryGetComponent(out TelegraphedStrike strike))
        {
            strike.Initialize(targetPosition);
        }
    }
}
