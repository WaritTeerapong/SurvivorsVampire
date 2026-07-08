using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class XPDropManager : NetworkBehaviour
{
    public static XPDropManager Instance { get; private set; }

    [Header("Prefab")]
    public GameObject XPPrefab;

    [HideInInspector]
    public List<XPOrb> ActiveXPOrbs = new List<XPOrb>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (XPPrefab != null && IsServer)
        {
            NetworkManager.Singleton.PrefabHandler.AddHandler(
                XPPrefab,
                new NetworkObjectPoolHandler(XPPrefab, PoolCategory.XP)
            );
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (XPPrefab != null && NetworkManager.Singleton != null && IsServer)
        {
            NetworkManager.Singleton.PrefabHandler.RemoveHandler(XPPrefab);
        }

        ActiveXPOrbs.Clear();
    }

    public void DropXP(Vector3 position, int xpValue)
    {
        if (!IsServer || XPPrefab == null) return;
        GameObject xpObj = ObjectPoolManager.Instance.SpawnObject<GameObject>(
            XPPrefab, position, Quaternion.identity, PoolCategory.XP
        );

        if (xpObj == null) return;
        NetworkObject netObj = xpObj.GetComponent<NetworkObject>();

        if (netObj == null) return;

        XPOrb orb = xpObj.GetComponent<XPOrb>();
        orb.Initialize(xpValue);

        if (!netObj.IsSpawned) netObj.Spawn(true);

        ActiveXPOrbs.Add(orb);
    }

    public void RemoveXP(XPOrb orb)
    {
        if (ActiveXPOrbs.Contains(orb))
        {
            ActiveXPOrbs.Remove(orb);
        }
    }

    public void PullAllXPToPlayers()
    {
        if (!IsServer) return;

        // Create a copy of the list to iterate safely and avoid modification errors
        List<XPOrb> orbsToPull = new List<XPOrb>(ActiveXPOrbs);
        foreach (XPOrb orb in orbsToPull)
        {
            if (orb != null && orb.IsSpawned)
            {
                orb.StartHoming();
            }
        }
    }
}