using System.Collections.Generic;
using UnityEngine;

public class PlayerDetector : MonoBehaviour
{
    private CircleCollider2D _detectorCollider;
    private PlayerRunTimeStats _playerStats;
    private PlayerInventoryManager _inventory;
    private IWeapon _localWeapon; // If this detector is attached directly to a weapon prefab

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
        _inventory = GetComponentInParent<PlayerInventoryManager>();
        
        _localWeapon = GetComponent<IWeapon>();
        if (_localWeapon == null) _localWeapon = GetComponentInParent<IWeapon>();
        if (_localWeapon == null) _localWeapon = GetComponentInChildren<IWeapon>();
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

    // Call this when weapons are added/upgraded to recalculate radius
    public void UpdateRadius()
    {
        float playerRange = _playerStats != null ? _playerStats.CurrentStats.Value.ATKRange : 0f;
        float weaponRange = 0f;

        if (_localWeapon != null)
        {
            // If we are attached to a specific weapon, use that weapon's range
            weaponRange = _localWeapon.AtkRange;
        }
        else if (_inventory != null)
        {
            // If we are on the player, find the maximum range among all equipped weapons
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

        Debug.Log($"[Detector] UpdateRadius: playerRange={playerRange}, weaponRange={weaponRange}, finalRadius={playerRange + weaponRange}");

        if (_detectorCollider != null)
        {
            _detectorCollider.radius = playerRange + weaponRange;
        }
    }

    public List<Transform> FindNearestTargets(int maxTarget = 1)
    {
        _enemiesInRange.RemoveAll(enemy => enemy == null || !enemy.gameObject.activeInHierarchy);

        if (_enemiesInRange.Count == 0)
        {
            _nearestEnemies.Clear();
            return _nearestEnemies;
        }

        Vector3 playerPos = transform.position;
        _enemiesInRange.Sort((a, b) =>
        {
            float sqrDistA = (a.position - playerPos).sqrMagnitude;
            float sqrDistB = (b.position - playerPos).sqrMagnitude;
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

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Debug.Log($"[Detector] OnTriggerEnter2D: {other.name} entered detection range.");
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
