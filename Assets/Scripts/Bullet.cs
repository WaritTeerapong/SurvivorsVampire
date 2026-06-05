using Unity.Netcode;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float Speed = 15f;
    public float HitDistance = 0.2f;

    private Transform _target;
    private int _damage;
    private bool _isFired;

    public void Initialize(Transform target, int damage)
    {
        _target = target;
        _damage = damage;
        _isFired = true;

        // Failsafe: Return to pool after 5 seconds if it never hits
        Invoke(nameof(ReturnToPool), 5f);
    }

    private void Update()
    {
        if (!_isFired) return;

        if (_target == null || !_target.gameObject.activeInHierarchy)
        {
            ReturnToPool();
            return;
        }

        // Move towards target
        transform.position = Vector3.MoveTowards(transform.position, _target.position, Speed * Time.deltaTime);

        // Check impact
        if (Vector3.Distance(transform.position, _target.position) <= HitDistance)
        {
            HitTarget();
        }
    }

    private void HitTarget()
    {
        // Only the Server applies damage
        if (NetworkManager.Singleton.IsServer)
        {
            Enemy enemy = _target.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(_damage);
            }
        }

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        _isFired = false;
        CancelInvoke();

        // Return this game object to the global pool manager
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnObjectToPool(gameObject);
        }
        else
        {
            Destroy(gameObject); // Fallback if manager is destroyed
        }
    }
}