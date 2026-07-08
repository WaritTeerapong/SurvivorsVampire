using Unity.Netcode;
using UnityEngine;

public class XPOrb : NetworkBehaviour
{
    public int XPValue { get; private set; }

    [Header("=== Homing Settings ===")]
    public float HomingSpeed = 25f;
    private bool _isHoming = false;
    private Transform _targetPlayer;

    public void Initialize(int xp)
    {
        XPValue = xp;
        _isHoming = false;
        _targetPlayer = null;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        // Ensure orb removes itself from active list when collected or cleared
        if (IsServer && XPDropManager.Instance != null)
        {
            XPDropManager.Instance.RemoveXP(this);
        }
    }

    public void StartHoming()
    {
        if (!IsServer) return;

        Player closestPlayer = null;
        float closestDistance = float.MaxValue;

        // Find the nearest player
        if (PlayerManager.Instance != null)
        {
            foreach (Player p in PlayerManager.Instance.AllPlayers)
            {
                // Skip downed/died players if you want, or let XP fly to their ghost.
                if (p == null || p.IsDownOrDied) continue;

                float dist = Vector3.Distance(transform.position, p.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestPlayer = p;
                }
            }
        }

        if (closestPlayer != null)
        {
            _targetPlayer = closestPlayer.TargetPoint != null ? closestPlayer.TargetPoint : closestPlayer.transform;
            _isHoming = true;
        }
    }

    private void Update()
    {
        // Move towards target player only on server side (NetworkTransform will sync it)
        if (!IsServer || !_isHoming || _targetPlayer == null) return;

        transform.position = Vector3.MoveTowards(transform.position, _targetPlayer.position, HomingSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            PlayerLevelManager.Instance.RequestGainXPRpc(XPValue);

            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
        }
    }
}