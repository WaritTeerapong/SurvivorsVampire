using UnityEngine;
using Unity.Netcode;

public class BossAOEState : IBossState
{
    private float _castTimer;
    private const float CAST_DURATION = 1.0f;

    public void OnEnter(Boss boss)
    {
        _castTimer = CAST_DURATION;
        boss.PlayAnimation(boss.AOE);

        boss.PlaySFXClientRpc("BossAOECasting");

        if (boss.BossData.AOEPrefab == null || PlayerManager.Instance == null) return;

        int attackDamage = boss.CurrentPhase.Value == 1 ? boss.BossData.P1_AOEDamage : boss.BossData.P2_AOEDamage;

        foreach (Transform target in PlayerManager.Instance.ActiveTargets)
        {
            if (target == null) continue;

            if (boss.CurrentPhase.Value == 1)
            {
                ulong targetNetId = target.GetComponent<NetworkObject>().NetworkObjectId;
                SpawnAOEPrefab(boss, target.position, attackDamage, targetNetId, true);
            }
            else
            {
                Vector2 playerForward = GetPlayerForwardDirection(target);
                for (int i = 0; i < 3; i++)
                {
                    Vector3 offsetPosition = target.position + (Vector3)(playerForward * (i * boss.BossData.P2_AOEDistanceOffset));
                    SpawnAOEPrefab(boss, offsetPosition, attackDamage, 0, false);
                }
            }
        }
    }

    public void OnUpdate(Boss boss)
    {
        _castTimer -= Time.deltaTime;
        if (_castTimer <= 0f)
        {
            boss.AOETimer = boss.CurrentPhase.Value == 1 ? boss.BossData.P1_AOECooldown : boss.BossData.P2_AOECooldown;
            boss.SwitchState(boss.ChaseState);
        }
    }

    public void OnExit(Boss boss)
    {
    }

    private void SpawnAOEPrefab(Boss boss, Vector3 position, int damage, ulong targetId, bool isTracking)
    {
        GameObject aoeObj = ObjectPoolManager.Instance.SpawnObject<GameObject>(
            boss.BossData.AOEPrefab, position, Quaternion.identity, PoolCategory.Default
        );

        if (aoeObj.TryGetComponent<NetworkObject>(out NetworkObject netObj) && !netObj.IsSpawned)
        {
            netObj.Spawn(true);
        }

        if (aoeObj.TryGetComponent<BossAOEController>(out BossAOEController controller))
        {
            float trackTime = boss.CurrentPhase.Value == 1 ? boss.BossData.P1_AOETrackingTime : 0f;
            float trackSpeed = boss.CurrentPhase.Value == 1 ? boss.BossData.P1_AOETrackingSpeed : 0f;

            controller.InitializeAOERpc(damage, targetId, isTracking, trackTime, trackSpeed);
        }
    }

    private Vector2 GetPlayerForwardDirection(Transform playerTransform)
    {
        Player player = playerTransform.GetComponent<Player>();
        if (player != null && player.InputHandler != null && player.InputHandler.MoveInput != Vector2.zero)
        {
            return player.InputHandler.MoveInput.normalized;
        }
        return Vector2.right;
    }
}