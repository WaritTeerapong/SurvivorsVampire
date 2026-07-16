using Unity.Netcode;
using UnityEngine;

public static class NetworkBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void InitializeNetworkManager()
    {
        if (NetworkManager.Singleton == null)
        {
            GameObject prefab = Resources.Load<GameObject>("NetworkManager");
            if (prefab != null)
            {
                Object.Instantiate(prefab);
                Debug.Log("[Bootstrapper] Created NetworkManager. My brooo!!");
            }
            else
            {
                Debug.LogError("[Bootstraper] Can not find the NetworkManager in Resources Folder, My man!!");
            }
        }
    }
}
