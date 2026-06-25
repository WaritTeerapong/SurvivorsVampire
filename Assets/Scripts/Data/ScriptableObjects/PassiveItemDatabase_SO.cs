using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PassiveItemDatabase_SO", menuName = "DataSO/PassiveItemDatabase_SO")]
public class PassiveItemDatabase_SO : ScriptableObject
{
    [SerializeField] private List<PassiveItemData_SO> _items;
    public List<PassiveItemData_SO> Items => _items;

    // For fast access O(1)
    public Dictionary<string, PassiveItemData_SO> MasterLookup;
    public void Initialize()
    {
        MasterLookup = new Dictionary<string, PassiveItemData_SO>();
        foreach (var item in Items)
        {
            if (item != null && !MasterLookup.ContainsKey(item.Id))
            {
                MasterLookup.Add(item.Id, item);
            }
        }
    }
    public PassiveItemData_SO GetItemByID(string id)
    {
        if (MasterLookup == null) Initialize();
        return MasterLookup.TryGetValue(id, out var item) ? item : null;
    }
}
