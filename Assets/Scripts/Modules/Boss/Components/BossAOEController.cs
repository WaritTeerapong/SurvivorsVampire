using UnityEngine;
using Unity.Netcode;
using DG.Tweening;

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
    private float _trackingSpeed;

    [Rpc(SendTo.Server)]
    public void InitializeAOERpc(int damage, ulong targetNetworkId, bool isTracking, float trackTime, float trackingSpeed)
    {
        _damage = damage;
        _isTracking = isTracking;
        _trackingTimeLeft = trackTime;
        _trackingSpeed = trackingSpeed;

        if (isTracking && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkId, out NetworkObject targetObj))
        {
            _targetTransform = targetObj.transform;
        }

        float totalLifetime = WarningDuration + trackTime;
        Invoke(nameof(Explode), totalLifetime);
        TriggerWarningVFXRpc(totalLifetime);
    }

    private void Update()
    {
        if (!IsServer) return;

        if (_isTracking && _targetTransform != null)
        {
            _trackingTimeLeft -= Time.deltaTime;

            transform.position = Vector2.Lerp(
                transform.position,
                _targetTransform.position,
                _trackingSpeed * Time.deltaTime
            );

            if (_trackingTimeLeft <= 0f)
            {
                _isTracking = false;
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
            if (hit.TryGetComponent<IDamageble>(out IDamageble damageable))
            {
                damageable.TakeDamage(_damage);
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
            _spriteRenderer.DOKill();
            _spriteRenderer.DOColor(new Color(1, 0, 0, 0.8f), totalTime).SetEase(Ease.InQuad);
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