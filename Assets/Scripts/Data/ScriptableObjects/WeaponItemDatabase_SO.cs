using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponItemDatabase_SO", menuName = "DataSO/WeaponItemDatabase_SO")]
public class WeaponItemDatabase_SO : ScriptableObject
{
    [SerializeField] private List<WeaponItemData_SO> _items;
    public List<WeaponItemData_SO> Items => _items;

    // For fast access O(1)
    public Dictionary<string, WeaponItemData_SO> MasterLookup;
    public void Initialize()
    {
        MasterLookup = new Dictionary<string, WeaponItemData_SO>();
        foreach (var item in Items)
        {
            if (item != null && !MasterLookup.ContainsKey(item.Id))
            {
                MasterLookup.Add(item.Id, item);
            }
        }
    }
    public WeaponItemData_SO GetItemByID(string id)
    {
        if (MasterLookup == null) Initialize();
        return MasterLookup.TryGetValue(id, out var item) ? item : null;
    }
}
