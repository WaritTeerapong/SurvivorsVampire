using Unity.Netcode;
using UnityEngine;

public class WeaponDamageDealer : MonoBehaviour
{
    [HideInInspector]
    private int _damage;

    public void SetDamage(int damage)
    {
        _damage = damage;
    }

    public bool DealDamage(IDamageble damagableObj)
    {
        if (!NetworkManager.Singleton.IsServer) return false;
        if (damagableObj == null) return false;

        damagableObj.TakeDamage(_damage);

        return true;
    }
          
}
