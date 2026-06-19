using Unity.Netcode;
using UnityEngine;

public class SPlayerCombat : NetworkBehaviour
{
    private Player _player;
    public GameObject BulletPrefab;
    public Transform FirePoint;
    private float _atkTimer = 0f;

    private float DebugATKRange = 1f;

    void Awake()
    {
        _player = GetComponent<Player>();
        DebugATKRange = _player.Stats.CurrentStats.Value.ATKRange;
    }

    void Update()
    {
        if (!IsOwner) return;

        if (_player.IsDowned) return;

        HandleAutoAttack();
    }

    private void HandleAutoAttack()
    {
        _player.Detector.FindNearestTarget();

        if (_player.Detector.NearestTarget == null) return;

        float atkSpeed = _player.Stats.CurrentStats.Value.ATKSpeed;
        if (atkSpeed <= 0) return;

        float atkCD = 1f / atkSpeed;
        _atkTimer += Time.deltaTime;

        if (_atkTimer >= atkCD)
        {
            _atkTimer = 0f;
            NetworkObject targetNetObj = _player.Detector.NearestTarget.GetComponent<NetworkObject>();
            if (targetNetObj != null)
            {
                RequestFireServerRpc(targetNetObj.NetworkObjectId);
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void RequestFireServerRpc(ulong targetNetworkID) => FireClientRpc(targetNetworkID);

    [Rpc(SendTo.Everyone)]
    private void FireClientRpc(ulong targetNetworkID)
    {
        if (BulletPrefab == null || ObjectPoolManager.Instance == null) return;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkID, out NetworkObject targetObj))
        {
            Vector3 spawnPos = FirePoint != null ? FirePoint.position : transform.position;
            GameObject bulletObj = ObjectPoolManager.Instance.SpawnObject(BulletPrefab, spawnPos, Quaternion.identity, PoolCategory.Projectiles);

            if (bulletObj != null)
            {
                Bullet bulletScript = bulletObj.GetComponent<Bullet>();
                if (bulletScript != null) bulletScript.Initialize(targetObj.transform, _player.Stats.CurrentStats.Value.ATKDamage);
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("PlayerShoot", spawnPos);
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, DebugATKRange);
    }
}