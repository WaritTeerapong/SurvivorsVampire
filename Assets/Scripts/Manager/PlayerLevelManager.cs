using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerLevelManager : NetworkBehaviour
{
    public static PlayerLevelManager Instance { get; private set; }

    [Header("Databases")]
    public LevelData_SO LevelData;
    public WeaponItemDatabase_SO WeaponDatabase;
    public PassiveItemDatabase_SO PassiveItemDatabase;

    public NetworkVariable<int> SharedLevel = new NetworkVariable<int>(1);
    public NetworkVariable<int> SharedXP = new NetworkVariable<int>(0);
    public NetworkVariable<int> SharedXPNeeded = new NetworkVariable<int>(0);

    public event Action OnLevelUp;
    public event Action OnGainXP;

    private PlayerInventory _playerInventory;
    [SerializeField] private Dictionary<string,int> _upgradableItemPool; // keep <id,nextLevel>

    

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _playerInventory = GetComponent<PlayerInventory>();
        _upgradableItemPool = new Dictionary<string, int>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        SharedLevel.OnValueChanged += HandleLevelChanged;
        SharedXP.OnValueChanged += HandleXPChanged;

        // Subscribe to inventory changes to keep pool updated dynamically
        if (_playerInventory != null)
        {
            _playerInventory.OwnedWeapons.OnListChanged += OnWeaponsListChanged;
            _playerInventory.OwnedPassives.OnListChanged += OnPassivesListChanged;
        }

        if (IsServer)
        {
            if (LevelData != null)
            {
                SharedLevel.Value = LevelData.Levels[0].Level;
                SharedXP.Value = 0;
                SharedXPNeeded.Value = LevelData.Levels[1].XPNeeded;
            }
        }

        // Initialize pool after network synchronization is complete
        InitPool();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        SharedLevel.OnValueChanged -= HandleLevelChanged;
        SharedXP.OnValueChanged -= HandleXPChanged;

        // Unsubscribe from inventory changes
        if (_playerInventory != null)
        {
            _playerInventory.OwnedWeapons.OnListChanged -= OnWeaponsListChanged;
            _playerInventory.OwnedPassives.OnListChanged -= OnPassivesListChanged;
        }

        OnLevelUp = null;
        OnGainXP = null;
    }



    [Rpc(SendTo.Server)]
    public void RequestGainXPRpc(int amount)
    {
        GainXP(amount);
    }
    private void GainXP(int incomingXP)
    {
        if (!IsServer) return;
        if (SharedXPNeeded.Value == -1) return;

        SharedXP.Value += incomingXP;

        while (SharedXP.Value >= SharedXPNeeded.Value && SharedXPNeeded.Value != -1)
        {
            SharedXP.Value -= SharedXPNeeded.Value;
            SharedLevel.Value++;    // OnLevelUp event fire!
            SharedXPNeeded.Value = LevelData.GetNeededXPForLevel(SharedLevel.Value + 1);

            if (SharedXPNeeded.Value == -1)
            {
                SharedXP.Value = 0;
                break;
            }
        }
    }

    // to send ItemName,Icon, and stat(according to level)
    public Dictionary<string,int> RandomUpgradeItem(int amount)
    {
        Dictionary<string, int> _selectedItem = new Dictionary<string, int>();
        List<string> _availableItemId = new List<string>(_upgradableItemPool.Keys);
        
        if (amount <= 0) return _selectedItem;

        // take all item in the pool
        if(_availableItemId.Count <= amount)
        {
            foreach(string id in _availableItemId) {
                ItemData_Base itemData = (ItemData_Base)WeaponDatabase?.GetItemByID(id) ?? PassiveItemDatabase?.GetItemByID(id);
                int nextLevel = _upgradableItemPool[id];

                if (itemData == null) continue;
                _selectedItem.Add(id, nextLevel);
            }
        }
        else
        {
            // randomly pick item in the pool
            for (int i = 0; i < amount; i++)
            {
                int randomIndex = Random.Range(0, _availableItemId.Count);
                string id = _availableItemId[randomIndex];
                int nextLevel = _upgradableItemPool[id];

                _selectedItem.Add(id, nextLevel);

                _availableItemId.RemoveAt(randomIndex);
            }
        }

        Debug.LogWarning($"Random Pool : {_selectedItem.Count}");
        return _selectedItem;
    }

    #region Callback functions
    private void HandleLevelChanged(int previousValue, int newValue)
    {
        OnLevelUp?.Invoke();
    }

    private void HandleXPChanged(int previousValue, int newValue)
    {
        OnGainXP?.Invoke();
    }

    private void OnWeaponsListChanged(NetworkListEvent<CurrentItemLevel> changeEvent)
    {
        // Update pool when add or upgrade item to networkList
        if (changeEvent.Type == NetworkListEvent<CurrentItemLevel>.EventType.Add ||
            changeEvent.Type == NetworkListEvent<CurrentItemLevel>.EventType.Value)
        {
            UpdatePool(changeEvent.Value.ItemId.ToString());
        }
    }

    private void OnPassivesListChanged(NetworkListEvent<CurrentItemLevel> changeEvent)
    {
        // Update pool when add or upgrade item to networkList
        if (changeEvent.Type == NetworkListEvent<CurrentItemLevel>.EventType.Add ||
            changeEvent.Type == NetworkListEvent<CurrentItemLevel>.EventType.Value)
        {
            UpdatePool(changeEvent.Value.ItemId.ToString());
        }
    }
    #endregion

    #region Pool functions
    private void InitPool()
    {
        // This logic will keep the pool up-to-date when reconnect to server

        _upgradableItemPool.Clear();

        // 1. Populate pool with level 1 base items from databases
        AddDatabaseItemsToPool(WeaponDatabase?.Items);
        AddDatabaseItemsToPool(PassiveItemDatabase?.Items);

        if (_playerInventory == null) return;

        // 2. Adjust pool based on player's starting inventory
        AdjustPoolFromOwnedItems(_playerInventory.OwnedWeapons);
        AdjustPoolFromOwnedItems(_playerInventory.OwnedPassives);

        Debug.LogWarning($"[InitPool] {_upgradableItemPool.Count} ");
    }

      private void UpdatePool(string id)
    {
        // 1. Safety check to avoid KeyNotFoundException
        if (!_upgradableItemPool.TryGetValue(id, out int currentLevel))
        { 
            Debug.Log($"[UpgradePool] Id:{id} not found in pool."); 
            return; 
        } 

        // 2. Query databases directly using the non-generic base class
        ItemData_Base item = (ItemData_Base)WeaponDatabase.GetItemByID(id) ?? PassiveItemDatabase.GetItemByID(id);
        if (item == null) return;

        // 3. Increment level and remove if next upgrade level exceeds maxLevel
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
    #endregion

    #region Helper Functions

    // Populates all database items into the pool at Level 1
    private void AddDatabaseItemsToPool<TItem>(List<TItem> items) where TItem : ItemData_Base
    {
        if (items == null) return;
        foreach (var item in items)
        {
            if (item != null) _upgradableItemPool[item.Id] = 1;
        }
    }

    // Updates levels or removes maxed-out items from the pool
    private void AdjustPoolFromOwnedItems(NetworkList<CurrentItemLevel> ownedList)
    {
        foreach (var entry in ownedList)
        {
            string id = entry.ItemId.ToString();

            // Find the item definition in either database
            ItemData_Base item = (ItemData_Base)WeaponDatabase?.GetItemByID(id) ?? PassiveItemDatabase?.GetItemByID(id);
            if (item != null)
            {
                if (entry.Level >= item.MaxLevel)
                {
                    _upgradableItemPool.Remove(id);
                }
                else
                {
                    _upgradableItemPool[id] = entry.Level + 1; // Show next upgrade level
                }
            }
        }
    }

    #endregion
}