using JetBrains.Annotations;
using UnityEngine;

[System.Serializable]
public struct WeaponStat
{
    public float ATKDamage; 
    public float ATKRange;
    public float ATKSpeed;
}

[CreateAssetMenu(fileName = "WeaponItemData_So", menuName = "DataSO/Item/WeaponItemData_SO")]
public class WeaponItemData_SO : ItemData_Base<WeaponStat>
{
    public GameObject WeaponPrefab;
}
