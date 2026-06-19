using UnityEngine;
using Unity.Netcode;

public class PlayerCombat : NetworkBehaviour
{
    private PlayerInventory _inventory;

    private void Awake()
    {
        _inventory = GetComponent<PlayerInventory>();
    }

    private void Update()
    {
        if (!IsOwner) return; // Only the local owner client should update weapon timers
        if (_inventory == null) return;

        foreach (var weaponObj in _inventory.InstantiatedWeapons.Values)
        {
            if (weaponObj != null)
            {
                IWeapon weapon = weaponObj.GetComponent<IWeapon>();
                if (weapon != null)
                {
                    weapon.PerformAttack();
                }
            }
        }
    }


    [Rpc(SendTo.Server)]
    public void RequestPerformAttackRpc(string weaponId, ulong targetNetworkId)
    {
        FireWeaponClientRpc(weaponId, targetNetworkId);
    }

    [Rpc(SendTo.Everyone)]
    private void FireWeaponClientRpc(string weaponId, ulong targetNetworkId)
    {
        if (_inventory != null && _inventory.InstantiatedWeapons.TryGetValue(weaponId, out GameObject weaponObj))
        {
            if (weaponObj != null)
            {
                BaseWeapon weaponScript = weaponObj.GetComponent<BaseWeapon>();
                if (weaponScript != null)
                {
                    if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkId, out NetworkObject targetObj))
                    {
                        weaponScript.Attack(targetObj.transform);
                    }
                }
            }
        }
    }
}
