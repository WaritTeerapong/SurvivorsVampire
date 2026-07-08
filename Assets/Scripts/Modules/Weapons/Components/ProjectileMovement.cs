using UnityEngine;

[DefaultExecutionOrder(-1)]
public class ProjectileMovement : MonoBehaviour
{
    [HideInInspector]
    public float Speed = 15f;
    private Vector3 _direction;
    private bool _isMoving = false;

    public void MoveInDirection(Vector3 direction)
    {
        _direction = direction.normalized;
        _isMoving = true;
    }

    public void Stop()
    {
        _isMoving = false;
    }

    void Update()
    {
        if (!_isMoving) return;
        transform.position += _direction * (Speed * Time.deltaTime);
    }
}
