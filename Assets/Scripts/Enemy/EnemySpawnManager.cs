using Unity.Netcode;
using UnityEngine;

public class EnemySpawnManager : NetworkBehaviour
{
    public static EnemySpawnManager Instance;

    public GameObject EnemyPrefab;
    public Transform[] SpawnPoints;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SpawnEnemies()
    {
        if (!IsServer) return;

        if (EnemyPrefab == null || SpawnPoints == null || SpawnPoints.Length == 0)
        {
            Debug.LogWarning("EnemyPrefab or SpawnPoints not set in EnemySpawnManager.");
            return;
        }

        foreach (Transform spawnPoint in SpawnPoints)
        {
            GameObject enemy = Instantiate(EnemyPrefab, spawnPoint.position, spawnPoint.rotation);
            enemy.GetComponent<NetworkObject>().Spawn();
        }
    }
}