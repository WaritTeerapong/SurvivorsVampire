using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerUpgradePool : MonoBehaviour
{
    private PlayerInventoryManager _playerInventory;
    private Dictionary<string, int> _upgradableItemPool = new Dictionary<string, int>();

    private WeaponItemDatabase_SO WeaponDatabase => _playerInventory != null ? _playerInventory.WeaponDatabase : null;
    private PassiveItemDatabase_SO PassiveItemDatabase => _playerInventory != null ? _playerInventory.PassiveDatabase : null;

    public void Initialize(PlayerInventoryManager inventory)
    {
        _playerInventory = inventory;

        // Subscribe to inventory changes to keep pool updated dynamically
        _playerInventory.OwnedWeapons.OnListChanged += OnWeaponsListChanged;
        _playerInventory.OwnedPassives.OnListChanged += OnPassivesListChanged;

        InitPool();
    }

    public void ClearPool()
    {
        if (_playerInventory != null)
        {
            _playerInventory.OwnedWeapons.OnListChanged -= OnWeaponsListChanged;
            _playerInventory.OwnedPassives.OnListChanged -= OnPassivesListChanged;
            _playerInventory = null;
        }
        _upgradableItemPool.Clear();
    }

    private void OnDestroy()
    {
        ClearPool();
    }

    private void OnWeaponsListChanged(NetworkListEvent<CurrentItemLevel> changeEvent)
    {
        // Update Pool on Add and ValueChange event
        if (changeEvent.Type == NetworkListEvent<CurrentItemLevel>.EventType.Add ||
            changeEvent.Type == NetworkListEvent<CurrentItemLevel>.EventType.Value)
        {
            UpdatePool(changeEvent.Value.ItemId.ToString());
        }
    }

    private void OnPassivesListChanged(NetworkListEvent<CurrentItemLevel> changeEvent)
    {
        // Update Pool on Add and ValueChange event
        if (changeEvent.Type == NetworkListEvent<CurrentItemLevel>.EventType.Add ||
            changeEvent.Type == NetworkListEvent<CurrentItemLevel>.EventType.Value)
        {
            UpdatePool(changeEvent.Value.ItemId.ToString());
        }
    }

    private void InitPool()
    {
        _upgradableItemPool.Clear();

        AddDatabaseItemsToPool(WeaponDatabase?.Items);
        AddDatabaseItemsToPool(PassiveItemDatabase?.Items);

        if (_playerInventory == null) return;

        AdjustPoolFromOwnedItems(_playerInventory.OwnedWeapons);
        AdjustPoolFromOwnedItems(_playerInventory.OwnedPassives);

        Debug.LogWarning($"[InitPool] {_upgradableItemPool.Count} items in pool.");
    }

    private void UpdatePool(string id)
    {
        if (!_upgradableItemPool.TryGetValue(id, out int currentLevel))
        {
            Debug.Log($"[UpgradePool] Id:{id} not found in pool.");
            return;
        }

        ItemData_Base item = (ItemData_Base)WeaponDatabase?.GetItemByID(id) ?? PassiveItemDatabase?.GetItemByID(id);
        if (item == null) return;

        int nextLevel = currentLevel + 1;
        if (nextLevel > item.MaxLevel)
        {
            _upgradableItemPool.Remove(id);
            Debug.Log($"[UpgradePool] {item.ItemName} reached max level! Removed from pool.");
        }
        else
        {
            _upgradableItemPool[id] = nextLevel;
            Debug.Log($"[UpgradePool] Upgraded next level for {item.ItemName} to Level {nextLevel}.");
        }
    }

    public Dictionary<string, int> RandomUpgradeItem(int amount)
    {
        Dictionary<string, int> selectedItem = new Dictionary<string, int>();
        List<string> availableItemId = new List<string>(_upgradableItemPool.Keys);

        if (amount <= 0) return selectedItem;

        if (availableItemId.Count <= amount)
        {
            foreach (string id in availableItemId)
            {
                ItemData_Base itemData = (ItemData_Base)WeaponDatabase?.GetItemByID(id) ?? PassiveItemDatabase?.GetItemByID(id);
                int nextLevel = _upgradableItemPool[id];

                if (itemData == null) continue;
                selectedItem.Add(id, nextLevel);
            }
        }
        else
        {
            for (int i = 0; i < amount; i++)
            {
                int randomIndex = Random.Range(0, availableItemId.Count);
                string id = availableItemId[randomIndex];
                int nextLevel = _upgradableItemPool[id];

                selectedItem.Add(id, nextLevel);
                availableItemId.RemoveAt(randomIndex);
            }
        }

        Debug.LogWarning($"Random Pool: {selectedItem.Count}");
        return selectedItem;
    }

    private void AddDatabaseItemsToPool<TItem>(List<TItem> items) where TItem : ItemData_Base
    {
        if (items == null) return;
        foreach (var item in items)
        {
            if (item != null) _upgradableItemPool[item.Id] = 1;
        }
    }

    private void AdjustPoolFromOwnedItems(NetworkList<CurrentItemLevel> ownedList)
    {
        foreach (var entry in ownedList)
        {
            string id = entry.ItemId.ToString();
            ItemData_Base item = (ItemData_Base)WeaponDatabase?.GetItemByID(id) ?? PassiveItemDatabase?.GetItemByID(id);
            if (item != null)
            {
                if (entry.Level >= item.MaxLevel)
                {
                    _upgradableItemPool.Remove(id);
                }
                else
                {
                    _upgradableItemPool[id] = entry.Level + 1;
                }
            }
        }
    }
}

