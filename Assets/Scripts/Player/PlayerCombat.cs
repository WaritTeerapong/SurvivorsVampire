using UnityEngine;
using Unity.Netcode;

public class PlayerCombat : NetworkBehaviour
{
    private PlayerInventoryManager _inventory;

    private void Awake()
    {
        _inventory = GetComponent<PlayerInventoryManager>();
    }

    private void Update()
    {
        if (_inventory == null) return;

        foreach (var weaponObj in _inventory.InstantiatedWeapons.Values)
        {
            if (weaponObj == null) continue;

            IWeapon weapon = weaponObj.GetComponent<IWeapon>();
            if (weapon == null) return;

            weapon.PerformAttack();

        }
    }
}
