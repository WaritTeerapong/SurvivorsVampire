using UnityEngine;
using Unity.Netcode;

public class BossAOEController : NetworkBehaviour
{
    [Header("=== AOE Settings ===")]
    public float DamageRadius = 2f;
    public float WarningDuration = 1.5f;
    public LayerMask PlayerLayerMask;

    [Header("=== VFX ===")]
    public GameObject ExplosionVFXPrefab;

    [Header("=== References ===")]
    [SerializeField] private SpriteRenderer _spriteRenderer;

    private int _damage;
    private bool _isTracking;
    private float _trackingTimeLeft;
    private Transform _targetTransform;

    [Rpc(SendTo.Server)]
    public void InitializeAOERpc(int damage, ulong targetNetworkId, bool isTracking, float trackTime)
    {
        _damage = damage;
        _isTracking = isTracking;
        _trackingTimeLeft = trackTime;

        // Try to find the target player on the network
        if (isTracking && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkId, out NetworkObject targetObj))
        {
            _targetTransform = targetObj.transform;
        }
        else if (isTracking)
        {
            // Debug.LogWarning("BossAOEController: Target NetworkObject not found for tracking.");
        }

        float totalLifetime = WarningDuration + trackTime;

        // Schedule the explosion
        Invoke(nameof(Explode), totalLifetime);

        // Notify all clients to play the visual warning warning
        TriggerWarningVFXRpc(totalLifetime);
    }

    private void Update()
    {
        if (!IsServer) return;

        // Handle tracking logic for Phase 1
        if (_isTracking && _targetTransform != null)
        {
            _trackingTimeLeft -= Time.deltaTime;
            transform.position = _targetTransform.position;

            if (_trackingTimeLeft <= 0f)
            {
                _isTracking = false; // Lock position after tracking time ends
            }
        }
    }

    private void Explode()
    {
        if (!IsServer) return;

        // Detect players within the explosion radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, DamageRadius, PlayerLayerMask);
        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent<Player>(out Player player))
            {
                player.TakeDamageRpc(_damage);
            }
        }

        PlayExplosionVFXRpc();
        NetworkObject.Despawn(true);
    }

    [Rpc(SendTo.Everyone)]
    private void TriggerWarningVFXRpc(float totalTime)
    {
        if (_spriteRenderer != null)
        {
            // Note: You can replace this with DG.Tweening (DOTween) for better animation
            // Example: _spriteRenderer.color = new Color(1, 0, 0, 0.2f);
            // _spriteRenderer.DOColor(new Color(1, 0, 0, 0.8f), totalTime).SetEase(Ease.InQuad);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void PlayExplosionVFXRpc()
    {
        if (ExplosionVFXPrefab != null && ObjectPoolManager.Instance != null)
        {
            ParticleSystem ps = ObjectPoolManager.Instance.SpawnObject<ParticleSystem>(
                ExplosionVFXPrefab,
                transform.position,
                Quaternion.identity,
                PoolCategory.VFX
            );
            if (ps != null) ps.Play();
        }
        else
        {
            // Debug.LogWarning("BossAOEController: Missing VFX Prefab or ObjectPoolManager instance.");
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, DamageRadius);
    }
}