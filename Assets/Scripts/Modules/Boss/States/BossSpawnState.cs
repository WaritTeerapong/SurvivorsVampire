using System.Collections;
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

        boss.PlaySFXClientRpc("BossSummon");

        boss.StartCoroutine(WaitToSpawnMinions(boss));
    }

    private IEnumerator WaitToSpawnMinions(Boss boss)
    {
        yield return new WaitForSeconds(CAST_DURATION / 1.5f);

        var minionConfig = boss.CurrentPhase.Value == 1 ? boss.BossData.P1_Minions : boss.BossData.P2_Minions;
        if (minionConfig == null) yield break;

        foreach (var setup in minionConfig)
        {
            if (setup.EnemyType == null) continue;

            for (int i = 0; i < setup.Amount; i++)
            {
                Vector2 randomOffset = Random.insideUnitCircle.normalized * SPAWN_RADIUS;
                Vector3 finalSpawnPos = boss.transform.position + (Vector3)randomOffset;

                GameObject enemyObj = ObjectPoolManager.Instance.SpawnObject<GameObject>(
                    setup.EnemyType.EnemyPrefab, finalSpawnPos, Quaternion.identity, PoolCategory.Enemies
                );

                if (enemyObj != null && enemyObj.TryGetComponent<NetworkObject>(out NetworkObject netObj))
                {
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
    }
}