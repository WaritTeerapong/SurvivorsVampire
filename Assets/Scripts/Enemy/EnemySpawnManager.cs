using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class EnemySpawnManager : NetworkBehaviour
{
    public static EnemySpawnManager Instance;

    public GameObject EnemyPrefab;
    public Transform[] SpawnPoints; // Assume 2 Points for now
    public float SpawnCD = 5f; // 1 unit / 5 seconds

    public int MaxEnemies = 10;
    private int _currentEnemiesCount = 0;
    private Coroutine _spawnCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (EnemyPrefab != null)
        {
            NetworkManager.Singleton.PrefabHandler.AddHandler(
                EnemyPrefab,
                new NetworkObjectPoolHandler(EnemyPrefab, PoolCategory.Enemies)
            );
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (EnemyPrefab != null && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.PrefabHandler.RemoveHandler(EnemyPrefab);
        }

        StopLoop();
    }

    public void SpawnLoop()
    {
        if (!IsServer) return;

        // Spawn Coroutine
        _spawnCoroutine = StartCoroutine(SpawnEnemies());
    }

    private void StopLoop()
    {
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }
    }

    private IEnumerator SpawnEnemies()
    {
        while (IsSpawned && IsServer)
        {
            if (EnemyPrefab == null || SpawnPoints == null || SpawnPoints.Length == 0)
            {
                Debug.LogWarning("EnemyPrefab or SpawnPoints not set in EnemySpawnManager.");
                yield break;
            }

            if (_currentEnemiesCount < MaxEnemies)
            {
                Transform spawnPoint = SpawnPoints[Random.Range(0, SpawnPoints.Length)];
                GameObject enemy = ObjectPoolManager.Instance.SpawnObject(
                    EnemyPrefab, spawnPoint.position, spawnPoint.rotation, PoolCategory.Enemies
                );

                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                {
                    enemy.GetComponent<NetworkObject>().Spawn(true);

                    Enemy enemyScript = enemy.GetComponent<Enemy>();
                    if (enemyScript != null)
                    {
                        enemyScript.OnEnemyDespawned -= HandleEnemyDespawned;
                        enemyScript.OnEnemyDespawned += HandleEnemyDespawned;
                    }

                    _currentEnemiesCount++;
                }
            }

            yield return new WaitForSeconds(SpawnCD);
        }
    }

    private void HandleEnemyDespawned()
    {
        _currentEnemiesCount--;
        if (_currentEnemiesCount < 0) _currentEnemiesCount = 0;
    }

}