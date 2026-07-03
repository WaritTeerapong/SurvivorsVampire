using UnityEngine;

public class ProjectileVisuals : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;
    private TrailRenderer _trail;
    private GameObject _hitVFXPrefab;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        _trail = GetComponent<TrailRenderer>() ?? GetComponentInChildren<TrailRenderer>();
    }

    public void Setup(GameObject hitVFX)
    {
        _hitVFXPrefab = hitVFX;

        if (_spriteRenderer != null) _spriteRenderer.enabled = true;

        if (_trail != null)
        {
            _trail.Clear();
            _trail.enabled = true;
        }
    }

    public void Disable()
    {
        if (_spriteRenderer != null) _spriteRenderer.enabled = false;
    }

    public void SpawnHitVFX(Vector3 position)
    {
        if (_hitVFXPrefab != null && ObjectPoolManager.Instance != null)
        {
            ParticleSystem ps = ObjectPoolManager.Instance.SpawnObject<ParticleSystem>(
                _hitVFXPrefab,
                position,
                Quaternion.identity,
                PoolCategory.Default
            );
            if (ps != null) ps.Play();
        }
    }
}
