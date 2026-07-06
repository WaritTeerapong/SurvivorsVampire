using System;
using UnityEngine;

public class BladeCollision : MonoBehaviour
{
    [HideInInspector]
    public float Range;
    [HideInInspector]
    public ContactFilter2D filter;
    [HideInInspector]
    public float Width = 0.5f;

    public event Action<Collider2D, Vector3> OnHitDetected;

    private bool _isActive = false;
    private readonly RaycastHit2D[] _castResults = new RaycastHit2D[16];

    public void Activate()
    {
        _isActive = true;
    }

    public void Deactivate()
    {
        _isActive = false;
    }

    public void SetFilter(int selfLayer)
    {
        int targetLayer = GetTargetFromLayer(selfLayer);
        filter = new ContactFilter2D()
        {
            useTriggers = true,
            useLayerMask = true,
            layerMask = 1 << targetLayer,
        };
    }

    void Update()
    {
        if (!_isActive) return;

        // X: Width of the blade check, Y: thickness of the box sweep
        Vector2 boxSize = new Vector2(Width, 0.1f);
        float boxAngle = transform.eulerAngles.z;

        // Modern, zero-allocation overload of BoxCast
        int hitCount = Physics2D.BoxCast(
            transform.position,
            boxSize,
            boxAngle,
            transform.up,
            filter,
            _castResults,
            Range
        );

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = _castResults[i];
            Collider2D other = hit.collider;
            if (other != null)
            {
                OnHitDetected?.Invoke(other, hit.point);
            }
        }
    }

    private int GetTargetFromLayer(int layerIndex)
    {
        if (layerIndex == LayerMask.NameToLayer("Enemy"))
            return LayerMask.NameToLayer("Player");
        if (layerIndex == LayerMask.NameToLayer("Player"))
            return LayerMask.NameToLayer("Enemy");
        return LayerMask.NameToLayer("Default");
    }
}
