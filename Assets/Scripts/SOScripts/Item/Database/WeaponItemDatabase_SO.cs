using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponItemDatabase_SO", menuName = "DataSO/WeaponItemDatabase_SO")]
public class WeaponItemDatabase_SO : ScriptableObject
{
    [SerializeField] private List<WeaponItemData_SO> _items;

    // For fast access O(1)
    public Dictionary<string, WeaponItemData_SO> _masterLookup;
    public void Initialize()
    {
        _masterLookup = new Dictionary<string, WeaponItemData_SO>();
        foreach (var item in _items)
        {
            if (item != null && !_masterLookup.ContainsKey(item.Id))
            {
                _masterLookup.Add(item.Id, item);
            }
        }
    }
    public WeaponItemData_SO GetItemByID(string id)
    {
        if (_masterLookup == null) Initialize();
        return _masterLookup.TryGetValue(id, out var item) ? item : null;
    }
}
