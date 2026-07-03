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
    protected int _targetNumber = 1;

    protected void Awake()
    {
        InitializeWeapon();
    }

    public virtual void InitializeWeapon()
    {
        _detector = GetComponent<Detector>();
        _inventory = GetComponentInParent<PlayerInventory>();
        _playerStats = _inventory.GetComponent<PlayerRunTimeStats>();
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

    public virtual void PerformAttack()
    {
        if (_isCooldown) return;

        if (_detector == null)
        {
            return;
        }

        _targets = _detector.FindNearestTargets(_targetNumber);
        if (_targets.Count == 0)
        {
            return;
        }

        float playerAtkSpeed = _playerStats != null ? _playerStats.CurrentStats.Value.ATKSpeed : 0f;
        float totalAtkSpeed = _stat.ATKSpeed + playerAtkSpeed;

        if (totalAtkSpeed == 0)
        {
            return;
        }

        float playerAtkRange = _playerStats != null ? _playerStats.CurrentStats.Value.ATKRange : 0f;
        float totalRange = _stat.ATKRange + playerAtkRange;
        bool isAttacked = false;

        Player myPlayer = GetComponentInParent<Player>();
        Vector3 playerPos = myPlayer != null ? myPlayer.TargetPoint.position : transform.position;

        foreach (var target in _targets)
        {
            Vector3 targetPos = target.position;
            if (target.TryGetComponent<Enemy>(out Enemy e)) targetPos = e.TargetPoint.position;
            else if (target.TryGetComponent<Boss>(out Boss b)) targetPos = b.TargetPoint.position;

            float sqrDist = (targetPos - playerPos).sqrMagnitude;

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
                }
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
            int damage = GetTotalDamage();

            if (target.TryGetComponent<Enemy>(out Enemy enemy))
            {
                enemy.TakeDamage(damage);
            }
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
}