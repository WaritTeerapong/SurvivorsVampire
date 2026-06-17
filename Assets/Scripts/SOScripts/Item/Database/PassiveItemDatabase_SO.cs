using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PassiveItemDatabase_SO", menuName = "DataSO/PassiveItemDatabase_SO")]
public class PassiveItemDatabase_SO : ScriptableObject
{
    [SerializeField] private List<PassiveItemData_SO> _items;

    // For fast access O(1)
    public Dictionary<string, PassiveItemData_SO> _masterLookup;
    public void Initialize()
    {
        _masterLookup = new Dictionary<string, PassiveItemData_SO>();
        foreach (var item in _items)
        {
            if (item != null && !_masterLookup.ContainsKey(item.Id))
            {
                _masterLookup.Add(item.Id, item);
            }
        }
    }
    public PassiveItemData_SO GetItemByID(string id)
    {
        if (_masterLookup == null) Initialize();
        return _masterLookup.TryGetValue(id, out var item) ? item : null;
    }
}
