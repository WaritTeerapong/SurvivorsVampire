using UnityEngine;

public class SwordVisuals : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;
    private TrailRenderer _trail;
    private GameObject _hitVFXPrefab;

    void Awake()
    {
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _trail = GetComponentInChildren<TrailRenderer>();

        // Fix: Reparent the trail to the root to avoid inheriting any local Y-scaling of the blade sprite
        if (_trail != null && _trail.transform.parent != transform)
        {
            _trail.transform.SetParent(transform, false);
            _trail.transform.localPosition = Vector3.zero;
            _trail.transform.localScale = Vector3.one;
        }
    }

    public void Setup(float range, GameObject hitVFX)
    {
        _hitVFXPrefab = hitVFX;

        if (_spriteRenderer != null)
        {
            _spriteRenderer.enabled = true;

            // Dynamically scale and offset the Sprite GameObject (blade visual length)
            Transform childTransform = _spriteRenderer.transform;
            Vector3 localScale = childTransform.localScale;
            childTransform.localScale = new Vector3(localScale.x, range, localScale.z);
            childTransform.localPosition = new Vector3(0f, range / 2f, 0f);
        }

        if (_trail != null)
        {
            _trail.Clear();
            _trail.startWidth = range;
            _trail.endWidth = 0f;
            _trail.transform.localPosition = new Vector3(0f, range / 2f, 0f);
            _trail.alignment = LineAlignment.View;
            _trail.enabled = true;
        }
    }

    public void Disable()
    {
        if (_spriteRenderer != null) _spriteRenderer.enabled = false;
        if (_trail != null) _trail.enabled = false;
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
