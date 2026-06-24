using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections;

public class NetworkDisconnectHandler : MonoBehaviour
{
    private static NetworkDisconnectHandler Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer && (clientId == NetworkManager.ServerClientId || clientId == NetworkManager.Singleton.LocalClientId))
        {
            Debug.Log("[Network] Host is Gone for good!! Going to Main Menu...");

            Time.timeScale = 1f;
            // TODO: Change to load "CoreScene" instead of "MainMenuScene" to safely reset managers and avoid destroying SceneController singleton
            SceneManager.LoadScene("MainMenuScene");
        }

        if (NetworkManager.Singleton.IsServer && clientId != NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log($"[Network] Client [{clientId}] is disconnected");

            if (GameSessionData.PlayerSelections.ContainsKey(clientId))
                GameSessionData.PlayerSelections.Remove(clientId);

            if (GameSessionData.SpawnedDummies.ContainsKey(clientId))
                GameSessionData.SpawnedDummies.Remove(clientId);
            
            if (PauseManager.Instance != null)
            {
                if (PauseManager.Instance.PlayersInPause.Contains(clientId))
                    PauseManager.Instance.PlayersInPause.Remove(clientId);

                if (PauseManager.Instance.PlayersSelectingUpgrade.Contains(clientId))
                    PauseManager.Instance.PlayersSelectingUpgrade.Remove(clientId);
            }
        }
    }

    public static void ReturnToMainMenu()
    {
        Time.timeScale = 1f;

        if (Instance != null)
        {
            Instance.StartCoroutine(Instance.QuitRoutine());
        }
        else
        {
            if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
            // TODO: Change to load "CoreScene" instead of "MainMenuScene" to safely reset managers and avoid destroying SceneController singleton
            SceneManager.LoadScene("MainMenuScene");
        }
    }

    private IEnumerator QuitRoutine()
    {
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    if (clientId != NetworkManager.Singleton.LocalClientId)
                    {
                        NetworkManager.Singleton.DisconnectClient(clientId);
                    }
                }

                yield return new WaitForSecondsRealtime(0.1f);
            }

            NetworkManager.Singleton.Shutdown();

            yield return new WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
        }

        Time.timeScale = 1f;
        // TODO: Change to load "CoreScene" instead of "MainMenuScene" to safely reset managers and avoid destroying SceneController singleton
        SceneManager.LoadScene("MainMenuScene");
    }
}