using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemySpawnManager : NetworkBehaviour
{
    public static EnemySpawnManager Instance;

    [Header("=== Databases ===")]
    public WaveDatabase_SO WaveDatabase;
    public GameObject EnemyPrefab;

    [Header("=== Map & Spawn Settings ===")]
    public Vector2 MapSize = new Vector2(50f, 25f);
    public float SpawnOffset = 5f;

    private int _currentWaveIndex = 0;
    private Coroutine _waveCoroutine;
    private Coroutine _spawnCoroutine;

    [Header("=== Wave State (UI) ===")]
    public NetworkVariable<int> CurrentWave = new NetworkVariable<int>();
    public NetworkVariable<int> TimeRemaining = new NetworkVariable<int>();
    public NetworkVariable<bool> IsResting = new NetworkVariable<bool>();

    public List<Enemy> ActiveEnemies = new List<Enemy>();

    private bool _isBossDefeated;
    private bool _canSpawnEnemies;

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

        if (IsServer) SpawnLoop();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (EnemyPrefab != null && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.PrefabHandler.RemoveHandler(EnemyPrefab);
        }

        ActiveEnemies.Clear();
        StopAllCoroutines();
    }

    public void SpawnLoop()
    {
        if (!IsServer || WaveDatabase == null || WaveDatabase.Waves.Count == 0) return;

        _currentWaveIndex = 0;
        _waveCoroutine = StartCoroutine(WaveRoutine());
    }

    private IEnumerator WaveRoutine()
    {
        while (_currentWaveIndex < WaveDatabase.Waves.Count)
        {
            WaveData_SO currentWave = WaveDatabase.Waves[_currentWaveIndex];

            CurrentWave.Value = _currentWaveIndex + 1;
            IsResting.Value = false;

            if (currentWave.IsBossWave)
            {
                _canSpawnEnemies = false;
                _isBossDefeated = false;
                TimeRemaining.Value = -1; // Flag for UI

                SpawnBossAtCenter(currentWave.BossPrefab);

                yield return new WaitUntil(() => _isBossDefeated);

                // Note: No PullAllXPToPlayers() here as requested.
            }
            else
            {
                _canSpawnEnemies = true;
                _spawnCoroutine = StartCoroutine(SpawnEnemies(currentWave));

                int waveTime = Mathf.CeilToInt(currentWave.WaveDuration);
                while (waveTime > 0)
                {
                    TimeRemaining.Value = waveTime;
                    yield return new WaitForSeconds(1f);
                    waveTime--;
                }

                // Force stop spawning safely
                _canSpawnEnemies = false;
                if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);

                // === PULL XP TO PLAYERS WHEN NON-BOSS WAVE ENDS ===
                if (XPDropManager.Instance != null)
                {
                    XPDropManager.Instance.PullAllXPToPlayers();
                }
            }

            IsResting.Value = true;
            _canSpawnEnemies = false; // Double check to ensure no spawning during rest

            int restTime = Mathf.CeilToInt(currentWave.RestTime);
            while (restTime > 0)
            {
                TimeRemaining.Value = restTime;
                yield return new WaitForSeconds(1f);
                restTime--;
            }

            _currentWaveIndex++;
        }

        TimeRemaining.Value = 0;
    }

    private void SpawnBossAtCenter(GameObject bossPrefab)
    {
        if (bossPrefab == null)
        {
            // Debug.LogError("[EnemySpawnManager] Boss Prefab is missing.");
            return;
        }

        GameObject bossObj = ObjectPoolManager.Instance.SpawnObject<GameObject>(
            bossPrefab, Vector3.zero, Quaternion.identity, PoolCategory.Enemies
        );

        if (bossObj != null && NetworkManager.Singleton.IsListening)
        {
            if (bossObj.TryGetComponent<NetworkObject>(out NetworkObject netObj))
            {
                if (!netObj.IsSpawned) netObj.Spawn(true);
            }

            if (bossObj.TryGetComponent<Boss>(out Boss bossScript))
            {
                bossScript.OnBossDied -= HandleBossDefeated;
                bossScript.OnBossDied += HandleBossDefeated;
            }
        }
    }

    private void HandleBossDefeated()
    {
        _isBossDefeated = true;
    }

    private IEnumerator SpawnEnemies(WaveData_SO waveData)
    {
        // Loop will immediately break if _canSpawnEnemies becomes false
        while (IsSpawned && IsServer && _canSpawnEnemies)
        {
            if (ActiveEnemies.Count < waveData.MaxActiveEnemies)
            {
                EnemyTypeData_SO enemyType = GetRandomEnemyType();

                // Safety Check: Prevent NullReferenceException if data is missing or loading fails
                if (enemyType == null || enemyType.EnemyPrefab == null)
                {
                    // Debug.LogWarning("[EnemySpawnManager] EnemyType or Prefab is missing. Skipping spawn cycle.");
                    yield return new WaitForSeconds(1f);
                    continue; // Skip this loop iteration safely
                }

                Vector3 spawnPos = GetEdgeSpawnPosition();
                int tierLevel = GetRandomEnemyTier();
                GameObject selectedPrefab = enemyType.EnemyPrefab;

                GameObject enemyObj = ObjectPoolManager.Instance.SpawnObject<GameObject>(
                    selectedPrefab, spawnPos, Quaternion.identity, PoolCategory.Enemies
                );

                if (enemyObj != null && NetworkManager.Singleton.IsListening)
                {
                    enemyObj.GetComponent<NetworkObject>().Spawn(true);

                    Enemy enemyScript = enemyObj.GetComponent<Enemy>();
                    if (enemyScript != null)
                    {
                        enemyScript.InitStats(enemyType, tierLevel);
                        ActiveEnemies.Add(enemyScript);

                        enemyScript.OnEnemyDespawned -= HandleEnemyDespawned;
                        enemyScript.OnEnemyDespawned += HandleEnemyDespawned;
                    }
                }
            }

            yield return new WaitForSeconds(waveData.SpawnCD);
        }
    }

    private void HandleEnemyDespawned(Enemy enemy)
    {
        if (ActiveEnemies.Contains(enemy))
        {
            ActiveEnemies.Remove(enemy);
        }
    }

    public EnemyTypeData_SO GetRandomEnemyType()
    {
        if (WaveDatabase == null || _currentWaveIndex >= WaveDatabase.Waves.Count) return null;

        var types = WaveDatabase.Waves[_currentWaveIndex].AllowedEnemyTypes;
        if (types == null || types.Count == 0) return null;

        float totalWeight = 0;
        foreach (var t in types) totalWeight += t.Weight;

        float randomVal = Random.Range(0f, totalWeight);
        foreach (var t in types)
        {
            if (randomVal <= t.Weight) return t.EnemyType;
            randomVal -= t.Weight;
        }

        return types[0].EnemyType;
    }

    public int GetRandomEnemyTier()
    {
        if (WaveDatabase == null || _currentWaveIndex >= WaveDatabase.Waves.Count) return 1;

        var tiers = WaveDatabase.Waves[_currentWaveIndex].AllowedEnemyTiers;
        if (tiers == null || tiers.Count == 0) return 1;

        float totalWeight = 0;
        foreach (var t in tiers) totalWeight += t.Weight;

        float randomVal = Random.Range(0f, totalWeight);
        foreach (var t in tiers)
        {
            if (randomVal <= t.Weight) return Mathf.Max(1, t.Tier);
            randomVal -= t.Weight;
        }

        return 1;
    }

    private Vector3 GetEdgeSpawnPosition()
    {
        float halfWidth = (MapSize.x / 2f) + SpawnOffset;
        float halfHeight = (MapSize.y / 2f) + SpawnOffset;

        int edge = Random.Range(0, 4);
        Vector3 spawnPos = Vector3.zero;

        switch (edge)
        {
            case 0: // Top
                spawnPos = new Vector3(Random.Range(-halfWidth, halfWidth), halfHeight, 0);
                break;
            case 1: // Bottom
                spawnPos = new Vector3(Random.Range(-halfWidth, halfWidth), -halfHeight, 0);
                break;
            case 2: // Left
                spawnPos = new Vector3(-halfWidth, Random.Range(-halfHeight, halfHeight), 0);
                break;
            case 3: // Right
                spawnPos = new Vector3(halfWidth, Random.Range(-halfHeight, halfHeight), 0);
                break;
        }

        return spawnPos;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(MapSize.x, MapSize.y, 0));

        Gizmos.color = Color.red;
        Vector3 spawnBoundarySize = new Vector3(MapSize.x + (SpawnOffset * 2), MapSize.y + (SpawnOffset * 2), 0);
        Gizmos.DrawWireCube(Vector3.zero, spawnBoundarySize);
    }
}