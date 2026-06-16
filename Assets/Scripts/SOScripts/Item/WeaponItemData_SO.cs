using JetBrains.Annotations;
using UnityEngine;

[System.Serializable]
public struct WeaponStat
{
    public float AtkDamage; 
    public float AtkRange;
    public float AtkSpeed;
}

[CreateAssetMenu(fileName = "WeaponItemData_So", menuName = "DataSO/Item/WeaponItemData_SO")]
public class WeaponItemData_SO : ItemData_Base<WeaponStat>
{
    public GameObject WeaponPrefab;
}
