using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class BaseWeapon : MonoBehaviour, IWeapon
{
    [Header("Weapon Configuration")]
    public WeaponItemData_SO WeaponData;

    protected WeaponStat _stat;

    public float AtkRange => _stat.ATKRange;

    protected PlayerInventory _inventory;
    protected PlayerRunTimeStats _playerStats;
    protected PlayerCombat _playerCombat;
    protected Detector _detector;
    protected bool _isCooldown = false;
    protected List<Transform> _targets;
    protected int _maxTarget = 1;

    protected void Awake()
    {
        InitializeWeapon();
    }

    public virtual void InitializeWeapon()
    {
        _detector = GetComponent<Detector>();
        _inventory = GetComponentInParent<PlayerInventory>();
        if (_inventory != null)
        {
            _playerStats = _inventory.GetComponent<PlayerRunTimeStats>();
        }
        _playerCombat = GetComponentInParent<PlayerCombat>();

        // Set initial stats (Level 1)
        UpgradeWeapon(1);
    }

    public virtual void UpgradeWeapon(int level)
    {
        if (WeaponData != null)
        {
            _stat = WeaponData.GetBonusForLevel(level);
            if (_detector != null)
            {
                _detector.UpdateRadius();
            }
        }
    }

    public virtual void PrepareToAttack()
    {
        if (_isCooldown) return;
        if (_detector == null)
        {
            Debug.LogWarning($"[BaseWeapon] PerformAttack: _detector is null on {gameObject.name}");
            return;
        }

        //Detect Targets
        _targets = _detector.FindNearestTargets(_maxTarget);
        if (_targets.Count == 0)
        {
            return; // No target detected, silent return is expected
        }
        float playerAtkSpeed = _playerStats != null ? _playerStats.CurrentStats.Value.ATKSpeed : 0f;
        float totalAtkSpeed = _stat.ATKSpeed + playerAtkSpeed;
        if (totalAtkSpeed == 0)
        {
            Debug.LogWarning($"[BaseWeapon] PrepareToAttack: Total AtkSpeed is 0 on {gameObject.name}");
            return;
        }

        // Range check locally in the weapon against the nearest target
        float playerAtkRange = _playerStats != null ? _playerStats.CurrentStats.Value.ATKRange : 0f;
        float totalRange = _stat.ATKRange + playerAtkRange;
        bool isAttacked = false;

        // Request Attack to all target(s)
        foreach (var target in _targets)
        {
            float sqrDist = (target.position - transform.position).sqrMagnitude;

            // Target out of range
            if (sqrDist > totalRange * totalRange)
            {
                continue;
            }

            NetworkObject targetNetObj = target.GetComponent<NetworkObject>();
            if (targetNetObj != null)
            {
                
                if (_playerCombat != null)
                {
                    _playerCombat.RequestPerformAttackRpc(WeaponData.Id, targetNetObj.NetworkObjectId);
                    isAttacked = true;
                    Debug.Log($"[BaseWeapon] PrepareToAttack: {WeaponData.Id} RequestPerformAttackRpc to {targetNetObj.NetworkObjectId} ");
                }
            }
            else
            {
                Debug.LogWarning($"[BaseWeapon] PrepareToAttack: Target {target.name} has no NetworkObject component.");
            }
        }

        if (isAttacked)
        {
            StartCoroutine(Cooldown());
        }
    }


    #region Helper Function
    public virtual IEnumerator Cooldown()
    {
        _isCooldown = true;
        float playerAtkSpeed = _playerStats != null ? _playerStats.CurrentStats.Value.ATKSpeed : 0f;
        float totalAtkSpeed = _stat.ATKSpeed + playerAtkSpeed;
        float cooldown = totalAtkSpeed > 0 ? 1f / totalAtkSpeed : 1f;
        yield return new WaitForSeconds(cooldown);
        _isCooldown = false;
    }

    public virtual void Attack(Transform target)
    {
        // Default Melee behavior: apply damage directly to the target on the server
        if (NetworkManager.Singleton.IsServer && target != null)
        {
            int damage = GetTotalDamage();

            // Check for Enemy component
            if (target.TryGetComponent<Enemy>(out Enemy enemy))
            {
                enemy.TakeDamage(damage);
            }
            // Check for Boss component
            else if (target.TryGetComponent<Boss>(out Boss boss))
            {
                boss.TakeDamage(damage);
            }
        }
    }

    protected int GetTotalDamage()
    {
        int playerAtk = 0;
        if (_playerStats != null)
        {
            playerAtk = _playerStats.CurrentStats.Value.ATKDamage;
        }
        return Mathf.RoundToInt(_stat.ATKDamage + playerAtk);
    }
    #endregion



}

