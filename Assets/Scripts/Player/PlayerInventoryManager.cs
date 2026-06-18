using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct CurrentItemLevel : INetworkSerializable, System.IEquatable<CurrentItemLevel>
{
    // FixedString32Bytes need for NetworkList<T> Generic Constrain : unmanage type
    public Unity.Collections.FixedString32Bytes ItemId; 
    public int Level;

    public CurrentItemLevel(string id, int level)
    {
        ItemId = id;
        Level = level;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ItemId);
        serializer.SerializeValue(ref Level);
    }

    // System.IEquatable<CurrentItemLevel> need for NetworkList<T> Generic Constrain : Trigger OnListChanged Event
    public bool Equals(CurrentItemLevel other)
    {
        return ItemId.Equals(other.ItemId) && Level == other.Level;
    }
}

public class PlayerInventoryManager : NetworkBehaviour
{
    [Header("Databases")]
    public WeaponItemDatabase_SO WeaponDatabase;
    public PassiveItemDatabase_SO PassiveDatabase;

    // Using NetworkList over NetworkVariable because the dynamic allocate weapon to client

    // Networked representation of inventory
    // For each clients see thier weapon correctly
    public NetworkList<CurrentItemLevel> OwnedWeapons;
    public NetworkList<CurrentItemLevel> OwnedPassives;

    // Local dictionary to keep track of instantiated weapon prefabs on each client
    public Dictionary<string, GameObject> InstantiatedWeapons { get; private set; }
         
    private void Awake()
    {
        OwnedWeapons = new NetworkList<CurrentItemLevel>
        (
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server
        );

        OwnedPassives = new NetworkList<CurrentItemLevel>
        (
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server
        );

        InstantiatedWeapons = new Dictionary<string, GameObject>();

    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        OwnedWeapons.OnListChanged += OnWeaponsListChanged;
        OwnedPassives.OnListChanged += OnPassivesListChanged;

        RecreateAllWeaponVisuals();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        OwnedWeapons.OnListChanged -= OnWeaponsListChanged;
        OwnedPassives.OnListChanged -= OnPassivesListChanged;
        ClearAllWeaponVisuals();
    }

    private void OnWeaponsListChanged(NetworkListEvent<CurrentItemLevel> changeEvent)
    {

        switch (changeEvent.Type)
        {
            case NetworkListEvent<CurrentItemLevel>.EventType.Add:
                InstantiateWeaponVisual(changeEvent.Value.ItemId.ToString(), changeEvent.Value.Level);
                break;
            case NetworkListEvent<CurrentItemLevel>.EventType.Value:
                UpdateWeaponVisual(changeEvent.Value.ItemId.ToString(), changeEvent.Value.Level);
                break;
            case NetworkListEvent<CurrentItemLevel>.EventType.Remove:
                DestroyWeaponVisual(changeEvent.Value.ItemId.ToString());
                break;
            case NetworkListEvent<CurrentItemLevel>.EventType.Clear:
                ClearAllWeaponVisuals();
                break;
        }
    }

    private void OnPassivesListChanged(NetworkListEvent<CurrentItemLevel> changeEvent)
    {

        if (IsServer)
        {
            PlayerRunTimeStats stats = GetComponent<PlayerRunTimeStats>();
            if (stats != null)
            {
                stats.RecalculateStats();
            }
        }
    }

    private void RecreateAllWeaponVisuals()
    {
        ClearAllWeaponVisuals();
        foreach (var entry in OwnedWeapons)
        {
            InstantiateWeaponVisual(entry.ItemId.ToString(), entry.Level);
        }
    }

    private void InstantiateWeaponVisual(string id, int level)
    {
        if (WeaponDatabase == null) return;
        if (InstantiatedWeapons.ContainsKey(id)) return;

        WeaponItemData_SO weaponData = WeaponDatabase.GetItemByID(id);
        if (weaponData != null && weaponData.WeaponPrefab != null)
        {
            // Find or create "Weapons" container under player to instantiate as grandchildren
            Transform weaponsContainerTransform = transform.Find("Weapons");
            if (weaponsContainerTransform == null)
            {
                GameObject container = new GameObject("Weapons");
                container.transform.SetParent(transform, false);
                weaponsContainerTransform = container.transform;
            }
            
            GameObject weaponInstance = Instantiate(weaponData.WeaponPrefab, weaponsContainerTransform);
            weaponInstance.transform.localPosition = Vector3.zero;
            weaponInstance.transform.localRotation = Quaternion.identity;

            InstantiatedWeapons[id] = weaponInstance;

            NotifyWeaponUpgrade(weaponInstance, level);
        }
    }

    private void UpdateWeaponVisual(string id, int level)
    {
        if (InstantiatedWeapons.TryGetValue(id, out GameObject weaponInstance))
        {
            NotifyWeaponUpgrade(weaponInstance, level);
        }
        else
        {
            InstantiateWeaponVisual(id, level);
        }
    }

    private void DestroyWeaponVisual(string id)
    {
        if (InstantiatedWeapons.TryGetValue(id, out GameObject weaponInstance))
        {
            if (weaponInstance != null)
            {
                Destroy(weaponInstance);
            }
            InstantiatedWeapons.Remove(id);
        }
    }

    private void ClearAllWeaponVisuals()
    {
        foreach (var kvp in InstantiatedWeapons)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        InstantiatedWeapons.Clear();
    }

    private void NotifyWeaponUpgrade(GameObject weaponInstance, int level)
    {
        weaponInstance.SendMessage("InitializeWeapon", this, SendMessageOptions.DontRequireReceiver);
        weaponInstance.SendMessage("UpgradeWeapon", level, SendMessageOptions.DontRequireReceiver);
    }

    // --- Public Getters ---
    public int GetPassiveItemLevel(string id)
    {
        foreach (var entry in OwnedPassives)
        {
            if (entry.ItemId.ToString() == id) return entry.Level;
        }
        return 0;
    }

    public int GetWeaponItemLevel(string id)
    {
        foreach (var entry in OwnedWeapons)
        {
            if (entry.ItemId.ToString() == id) return entry.Level;
        }
        return 0;
    }

    // --- Server-authoritative APIs ---
    [Rpc(SendTo.Server)]
    public void AddOrUpgradeWeaponServerRpc(string id)
    {
        AddOrUpgradeWeapon(id);
    }

    [Rpc(SendTo.Server)]
    public void AddOrUpgradePassiveServerRpc(string id)
    {
        AddOrUpgradePassive(id);
    }

    public void AddOrUpgradeWeapon(string id)
    {
        if (!IsServer) return;

        for (int i = 0; i < OwnedWeapons.Count; i++)
        {
            if (OwnedWeapons[i].ItemId.ToString() == id)
            {
                WeaponItemData_SO weaponData = WeaponDatabase != null ? WeaponDatabase.GetItemByID(id) : null;
                int maxLevel = weaponData != null ? weaponData.MaxLevel : 99;
                if (OwnedWeapons[i].Level < maxLevel)
                {
                    OwnedWeapons[i] = new CurrentItemLevel(id, OwnedWeapons[i].Level + 1);
                }
                Debug.LogWarning($"[AddOrUpgradeWeapon] {OwnedWeapons[i].ItemId} {OwnedWeapons[i].Level}");
                return;
            }
        }

        OwnedWeapons.Add(new CurrentItemLevel(id, 1));
    }

    public void AddOrUpgradePassive(string id)
    {
        if (!IsServer) return;

        for (int i = 0; i < OwnedPassives.Count; i++)
        {
            if (OwnedPassives[i].ItemId.ToString() == id)
            {
                PassiveItemData_SO passiveData = PassiveDatabase != null ? PassiveDatabase.GetItemByID(id) : null;
                int maxLevel = passiveData != null ? passiveData.MaxLevel : 99;
                if (OwnedPassives[i].Level < maxLevel)
                {
                    OwnedPassives[i] = new CurrentItemLevel(id, OwnedPassives[i].Level + 1);
                }
                Debug.LogWarning($"[AddOrUpgradePassive] {OwnedPassives[i].ItemId} {OwnedWeapons[i].Level}");
                return;
            }
        }

        OwnedPassives.Add(new CurrentItemLevel(id, 1));
    }

    public void RemoveWeapon(string id)
    {
        if (!IsServer) return;

        for (int i = 0; i < OwnedWeapons.Count; i++)
        {
            if (OwnedWeapons[i].ItemId.ToString() == id)
            {
                OwnedWeapons.RemoveAt(i);
                return;
            }
        }
    }

    public void RemovePassive(string id)
    {
        if (!IsServer) return;

        for (int i = 0; i < OwnedPassives.Count; i++)
        {
            if (OwnedPassives[i].ItemId.ToString() == id)
            {
                OwnedPassives.RemoveAt(i);
                return;
            }
        }
    }
}
