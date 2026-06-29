using Unity.Netcode;
using UnityEngine;

public class AutoTestStarter : MonoBehaviour
{
    [Header("Test Mode Settings")]
    public bool IsTestMode = true;

    private void Start()
    {
        if (!IsTestMode) return;

        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            StartAutoTest();
        }
    }

    private void StartAutoTest()
    {
#if UNITY_EDITOR
        bool hostStarted = false;
        try
        {
            hostStarted = NetworkManager.Singleton.StartHost();
        }
        catch
        {
            hostStarted = false;
        }

        if (hostStarted)
        {
            Debug.Log("[AutoTest] Host Started!!");
        }
        else
        {
            NetworkManager.Singleton.Shutdown();
            NetworkManager.Singleton.StartClient();
            Debug.Log("[AutoTest] Client Started");
        }
#endif
    }
}