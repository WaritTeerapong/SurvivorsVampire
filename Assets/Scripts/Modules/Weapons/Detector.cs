using System.Collections.Generic;
using UnityEngine;

public class Detector : MonoBehaviour
{
    private CircleCollider2D _detectorCollider;
    private PlayerRunTimeStats _playerStats;
    private PlayerInventory _inventory;
    private IWeapon _localWeapon;

    private List<Transform> _enemiesInRange = new List<Transform>();
    private List<Transform> _nearestEnemies = new List<Transform>();

    void Awake()
    {
        _detectorCollider = GetComponent<CircleCollider2D>();
        if (_detectorCollider != null)
        {
            _detectorCollider.isTrigger = true;
        }

        _playerStats = GetComponentInParent<PlayerRunTimeStats>();
        _inventory = GetComponentInParent<PlayerInventory>();

        _localWeapon = GetComponent<IWeapon>();
    }

    void Start()
    {
        UpdateRadius();
        if (_playerStats != null)
        {
            _playerStats.OnStatChanged += HandleStatChanged;
        }
    }

    void OnDestroy()
    {
        if (_playerStats != null)
        {
            _playerStats.OnStatChanged -= HandleStatChanged;
        }
    }

    private void HandleStatChanged(PlayerStats stats)
    {
        UpdateRadius();
    }

    public void UpdateRadius()
    {
        float playerRange = _playerStats != null ? _playerStats.CurrentStats.Value.ATKRange : 0f;
        float weaponRange = 0f;

        if (_localWeapon != null)
        {
            weaponRange = _localWeapon.AtkRange;
        }
        else if (_inventory != null)
        {
            foreach (var weaponObj in _inventory.InstantiatedWeapons.Values)
            {
                if (weaponObj != null)
                {
                    IWeapon weapon = weaponObj.GetComponent<IWeapon>();
                    if (weapon != null)
                    {
                        weaponRange = Mathf.Max(weaponRange, weapon.AtkRange);
                    }
                }
            }
        }

        if (_detectorCollider != null)
        {
            _detectorCollider.radius = playerRange + weaponRange;
        }
    }

    public List<Transform> FindNearestTargets(int maxTarget = 1)
    {
        // Filter out null, inactive, and dead enemies
        _enemiesInRange.RemoveAll(enemy => enemy == null || !enemy.gameObject.activeInHierarchy || IsEnemyDead(enemy));

        if (_enemiesInRange.Count == 0)
        {
            _nearestEnemies.Clear();
            return _nearestEnemies;
        }

        Player myPlayer = GetComponentInParent<Player>();
        Vector3 playerPos = myPlayer != null ? myPlayer.TargetPoint.position : transform.position;

        _enemiesInRange.Sort((a, b) =>
        {
            Vector3 posA = a.position;
            if (a.TryGetComponent<IDamageble>(out IDamageble dA)) posA = dA.TargetPoint.position;

            Vector3 posB = b.position;
            if (b.TryGetComponent<IDamageble>(out IDamageble dB)) posB = dB.TargetPoint.position;

            float sqrDistA = (posA - playerPos).sqrMagnitude;
            float sqrDistB = (posB - playerPos).sqrMagnitude;
            return sqrDistA.CompareTo(sqrDistB);
        });

        int count = Mathf.Min(maxTarget, _enemiesInRange.Count);
        _nearestEnemies.Clear();

        for (int i = 0; i < count; i++)
        {
            _nearestEnemies.Add(_enemiesInRange[i]);
        }

        return _nearestEnemies;
    }

    private bool IsEnemyDead(Transform enemyTransform)
    {
        if (enemyTransform.TryGetComponent<Enemy>(out Enemy enemy))
        {
            return enemy.CurrentStats.Value.CurrentHealth <= 0;
        }
        return false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (!_enemiesInRange.Contains(other.transform))
            {
                _enemiesInRange.Add(other.transform);
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            _enemiesInRange.Remove(other.transform);
        }
    }
}