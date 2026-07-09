using Unity.Netcode;
using UnityEngine;

public class XPOrb : NetworkBehaviour
{
    public int XPValue { get; private set; }

    [Header("=== Homing Settings ===")]
    public float HomingSpeed = 25f;
    private bool _isHoming = false;
    private Transform _targetPlayer;

    // Prevent double collection from both Update and OnTriggerEnter2D
    private bool _isCollected = false;

    public void Initialize(int xp)
    {
        XPValue = xp;
        _isHoming = false;
        _isCollected = false;
        _targetPlayer = null;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
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

        if (PlayerManager.Instance != null)
        {
            foreach (Player p in PlayerManager.Instance.AllPlayers)
            {
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
        if (!IsServer || !_isHoming || _targetPlayer == null || _isCollected) return;

        // Use unscaledDeltaTime to allow movement even when Time.timeScale is 0
        transform.position = Vector3.MoveTowards(transform.position, _targetPlayer.position, HomingSpeed * Time.unscaledDeltaTime);

        // Manual distance check bypasses the need for physics update, 
        // allowing the orb to be collected while the game is paused.
        if (Vector3.Distance(transform.position, _targetPlayer.position) <= 0.5f)
        {
            CollectOrb();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer || _isCollected) return;

        if (other.CompareTag("Player"))
        {
            CollectOrb();
        }
    }

    private void CollectOrb()
    {
        _isCollected = true;
        PlayerLevelManager.Instance.RequestGainXPRpc(XPValue);

        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }
}