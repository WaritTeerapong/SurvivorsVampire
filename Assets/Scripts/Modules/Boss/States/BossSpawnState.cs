using Unity.Netcode;
using UnityEngine;

public class BossSpawnState : IBossState
{
    private float _castTimer;
    private const float CAST_DURATION = 1.0f;
    private const float SPAWN_RADIUS = 3.0f;

    public void OnEnter(Boss boss)
    {
        if (!boss.IsServer) return;

        _castTimer = CAST_DURATION;
        boss.PlayAnimation(boss.SPAWN);

        // Note: Trigger necromancy/summon animation here

        var minionConfig = boss.CurrentPhase.Value == 1 ? boss.BossData.P1_Minions : boss.BossData.P2_Minions;
        if (minionConfig == null) return;

        foreach (var setup in minionConfig)
        {
            if (setup.EnemyType == null)
            {
                // Debug.LogWarning("EnemyTypeData_SO is missing in BossMinionSetup.");
                continue;
            }

            for (int i = 0; i < setup.Amount; i++)
            {
                Vector2 randomOffset = Random.insideUnitCircle.normalized * SPAWN_RADIUS;
                Vector3 finalSpawnPos = boss.transform.position + (Vector3)randomOffset;

                // Note: Pass the EnemyType directly to your Spawn Manager or instantiate its prefab here
                // Example: EnemySpawnManager.Instance.SpawnEnemy(setup.EnemyType, setup.Tier, finalSpawnPos);
                GameObject enemyObj = ObjectPoolManager.Instance.SpawnObject<GameObject>(
                    setup.EnemyType.EnemyPrefab, finalSpawnPos, Quaternion.identity, PoolCategory.Enemies
                );

                if (enemyObj != null && enemyObj.TryGetComponent<NetworkObject>(out NetworkObject netObj))
                {
                    // If returning from pool, it might not be spawned on the network yet
                    if (!netObj.IsSpawned)
                    {
                        netObj.Spawn(true);
                    }

                    if (enemyObj.TryGetComponent<Enemy>(out Enemy enemyScript))
                    {
                        enemyScript.InitStats(setup.EnemyType, setup.Tier);
                    }
                }
            }
        }
    }

    public void OnUpdate(Boss boss)
    {
        if (!boss.IsServer) return;

        _castTimer -= Time.deltaTime;

        if (_castTimer <= 0f)
        {
            boss.SpawnTimer = boss.CurrentPhase.Value == 1 ? boss.BossData.P1_SpawnCooldown : boss.BossData.P2_SpawnCooldown;
            boss.SwitchState(boss.ChaseState);
        }
    }

    public void OnExit(Boss boss)
    {
        // Optional: Clean up summoning particles
    }
}