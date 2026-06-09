using Unity.Netcode;
using UnityEngine;

public class XPDropManager : NetworkBehaviour
{
    public static XPDropManager Instance { get; private set; }

    [Header("Prefab")]
    public GameObject XPPrefab;

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
    }

    public void DropXP(Vector3 position, int xpValue)
    {
        if (!IsServer || XPPrefab == null) return;

        GameObject xpObj = ObjectPoolManager.Instance.SpawnObject(
            XPPrefab, position, Quaternion.identity, PoolCategory.XP
        );

        if (xpObj != null)
        {
            NetworkObject netObj = xpObj.GetComponent<NetworkObject>();

            if (netObj != null && !netObj.IsSpawned)
            {
                xpObj.GetComponent<XPOrb>().Initialize(xpValue);
                netObj.Spawn(true);
            }
            else if (netObj != null && netObj.IsSpawned)
            {
                xpObj.GetComponent<XPOrb>().Initialize(xpValue);
            }
        }
    }
}