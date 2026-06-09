using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemySpawnManager : NetworkBehaviour
{
    public static EnemySpawnManager Instance;

    [Header("Databases")]
    public WaveDatabase_SO WaveDatabase;
    public GameObject EnemyPrefab;

    [Header("Map & Spawn Settings")]
    public Vector2 MapSize = new Vector2(50f, 25f);
    public float SpawnOffset = 5f;

    private int _currentWaveIndex = 0;
    private Coroutine _waveCoroutine;
    private Coroutine _spawnCoroutine;

    [Header("Wave State (UI)")]
    public NetworkVariable<int> CurrentWave = new NetworkVariable<int>();
    public NetworkVariable<int> TimeRemaining = new NetworkVariable<int>();
    public NetworkVariable<bool> IsResting = new NetworkVariable<bool>();

    public List<Enemy> ActiveEnemies = new List<Enemy>();

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

            // 1. อัปเดตสถานะให้ UI รู้ว่ากำลังเริ่มเวฟไหน
            CurrentWave.Value = _currentWaveIndex + 1;
            IsResting.Value = false;

            Debug.Log($"[WaveManager] Starting Wave {_currentWaveIndex + 1}");

            _spawnCoroutine = StartCoroutine(SpawnEnemies(currentWave));

            // 2. ลูปนับถอยหลังเวลาสู้ (ทีละ 1 วินาที) เพื่อส่งไปให้ Client
            int waveTime = Mathf.CeilToInt(currentWave.WaveDuration);
            while (waveTime > 0)
            {
                TimeRemaining.Value = waveTime;
                yield return new WaitForSeconds(1f);
                waveTime--;
            }

            if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);

            Debug.Log($"[WaveManager] Wave {_currentWaveIndex + 1} Ended! Resting for {currentWave.RestTime}");

            // 3. อัปเดตสถานะให้ UI รู้ว่ากำลังพักหายใจ
            IsResting.Value = true;

            // 4. ลูปนับถอยหลังเวลาพัก (ทีละ 1 วินาที)
            int restTime = Mathf.CeilToInt(currentWave.RestTime);
            while (restTime > 0)
            {
                TimeRemaining.Value = restTime;
                yield return new WaitForSeconds(1f);
                restTime--;
            }

            _currentWaveIndex++;
        }

        Debug.Log("[WaveManager] All Waves Completed!");
        TimeRemaining.Value = 0;

        // TODO : Show Win UI
    }

    private IEnumerator SpawnEnemies(WaveData_SO waveData)
    {
        while (IsSpawned && IsServer)
        {
            if (ActiveEnemies.Count < waveData.MaxActiveEnemies)
            {
                // Random Position
                Vector3 spawnPos = GetEdgeSpawnPosition();

                // Spawn with object pool
                GameObject enemyObj = ObjectPoolManager.Instance.SpawnObject(
                    EnemyPrefab, spawnPos, Quaternion.identity, PoolCategory.Enemies
                );

                if (enemyObj != null && NetworkManager.Singleton.IsListening)
                {
                    enemyObj.GetComponent<NetworkObject>().Spawn(true);

                    Enemy enemyScript = enemyObj.GetComponent<Enemy>();
                    if (enemyScript != null)
                    {
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
        if (types.Count == 0) return null;

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
        if (tiers.Count == 0) return 1;

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

    // Edge Spawn System
    private Vector3 GetEdgeSpawnPosition()
    {
        float halfWidth = (MapSize.x / 2f) + SpawnOffset;
        float halfHeight = (MapSize.y / 2f) + SpawnOffset;

        // 0 = Top, 1 = Bottom, 2 = Left, 3 = Right
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
        // Map Size
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(MapSize.x, MapSize.y, 0));

        // Map Size + Offset for Spawn Enemy
        Gizmos.color = Color.red;
        Vector3 spawnBoundarySize = new Vector3(MapSize.x + (SpawnOffset * 2), MapSize.y + (SpawnOffset * 2), 0);
        Gizmos.DrawWireCube(Vector3.zero, spawnBoundarySize);
    }
}