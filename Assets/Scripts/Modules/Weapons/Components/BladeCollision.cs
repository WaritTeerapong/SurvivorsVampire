using System;
using UnityEngine;

public class BladeCollision : MonoBehaviour
{
    [HideInInspector]
    public float Range;
    [HideInInspector]
    public LayerMask TargetLayer;
    [HideInInspector]
    public float Width = 0.5f;

    public event Action<Collider2D, Vector3> OnHitDetected;

    private bool _isActive = false;
    private readonly RaycastHit2D[] _castResults = new RaycastHit2D[20];

    public void Activate()
    {
        _isActive = true;
    }

    public void Deactivate()
    {
        _isActive = false;
    }

    void Update()
    {
        if (!_isActive) return;

        // X: Width of the blade check, Y: thickness of the box sweep
        Vector2 boxSize = new Vector2(Width, 0.1f);
        float boxAngle = transform.eulerAngles.z;

        ContactFilter2D filter = new ContactFilter2D
        {
            useTriggers = true,
            useLayerMask = true,
            layerMask = TargetLayer
        };

        // Modern, zero-allocation overload of BoxCast
        int count = Physics2D.BoxCast(
            transform.position,
            boxSize,
            boxAngle,
            transform.up,
            filter,
            _castResults,
            Range
        );

        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = _castResults[i];
            Collider2D other = hit.collider;
            if (other != null)
            {
                OnHitDetected?.Invoke(other, hit.point);
            }
        }
    }
}
