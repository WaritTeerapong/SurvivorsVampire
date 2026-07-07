using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

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

        if (_detector == null) return;
        

        // Detected Targets
        _targets = _detector.FindNearestTargets(_maxTarget);
        if (_targets.Count == 0) return; 


        // Get Total AtkSpeed & AtkRange
        float totalAtkSpeed = GetTotalATKSpeed();
        float totalAtkRange = GetTotalATKRange();
        bool isAttacked = false;

        Player myPlayer = GetComponentInParent<Player>();
        Vector3 playerPos = myPlayer != null ? myPlayer.TargetPoint.position : transform.position;

        foreach (var target in _targets)
        {
            Vector3 targetPos = target.position;
            if (target.TryGetComponent<IDamageble>(out IDamageble d)) targetPos = d.TargetPoint.position;

            float sqrDist = (targetPos - playerPos).sqrMagnitude;

            // Target out of range
            if (sqrDist > totalAtkRange * totalAtkRange)
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
        if (NetworkManager.Singleton.IsServer && target != null)
        {
            int damage = GetTotalATKDamage();

            if (target.TryGetComponent<IDamageble>(out IDamageble damageable))
            {
                damageable.TakeDamage(damage);
            }
        }
    }

    protected int GetTotalATKDamage()
    {
        float totalATKDamage = 0;
        if (_playerStats != null)
        {
            totalATKDamage = _stat.ATKDamage + _playerStats.CurrentStats.Value.ATKDamage;
        }
        return Mathf.RoundToInt(totalATKDamage);
    }

    protected float GetTotalATKSpeed()
    {
        float totalATKSpeed = 1;
        if (_playerStats != null)
        {
            totalATKSpeed = _stat.ATKDamage + _playerStats.CurrentStats.Value.ATKDamage;

            // prevent from ATKspeed = 0
            totalATKSpeed = totalATKSpeed == 0 ? 1f : totalATKSpeed;
        }
        return totalATKSpeed;

    }

    protected float GetTotalATKRange()
    {
        float totalATKRange = 0;
        if (_playerStats != null)
        {
            totalATKRange = _stat.ATKRange + _playerStats.CurrentStats.Value.ATKRange;
        }

        return totalATKRange;
    }

    protected void SetInstanceLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetInstanceLayerRecursively(child.gameObject, newLayer);
        }
    }
}