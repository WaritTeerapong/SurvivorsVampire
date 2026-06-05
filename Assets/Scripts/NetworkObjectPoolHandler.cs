using Unity.Netcode;
using UnityEngine;

public class NetworkObjectPoolHandler : INetworkPrefabInstanceHandler
{
    private GameObject _prefab;
    private PoolCategory _category;

    public NetworkObjectPoolHandler(GameObject prefab, PoolCategory category)
    {
        _prefab = prefab;
        _category = category;
    }

    public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
    {
        GameObject obj = ObjectPoolManager.Instance.SpawnObject(_prefab, position, rotation, _category);
        return obj.GetComponent<NetworkObject>();
    }
    public void Destroy(NetworkObject networkObject)
    {
        ObjectPoolManager.Instance.ReturnObjectToPool(networkObject.gameObject);
    }
}