using UnityEngine;
using Unity.Netcode;

public class PlayerCombat : NetworkBehaviour
{
    private PlayerInventory _inventory;
    private Player _player;

    private void Awake()
    {
        _inventory = GetComponent<PlayerInventory>();
        _player = GetComponent<Player>();
    }

    private void Update()
    {
        if (!IsOwner) return; // Only the local owner client should update weapon timers
        if (_inventory == null || _player == null) return;
        if (_player.IsDownOrDied) return;

        foreach (var weaponObj in _inventory.InstantiatedWeapons.Values)
        {
            if (weaponObj != null)
            {
                IWeapon weapon = weaponObj.GetComponent<IWeapon>();
                if (weapon != null)
                {
                    weapon.PrepareToAttack();
                }
            }
        }
    }


    [Rpc(SendTo.Server)]
    public void RequestPerformAttackRpc(string weaponId, ulong targetNetworkId)
    {
        PerformAttackRpc(weaponId, targetNetworkId);
    }

    [Rpc(SendTo.Everyone)]
    private void PerformAttackRpc(string weaponId, ulong targetNetworkId)
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
