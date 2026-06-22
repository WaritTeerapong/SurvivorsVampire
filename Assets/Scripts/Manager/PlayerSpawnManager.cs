using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PlayerSpawnManager : NetworkBehaviour
{
    public static PlayerSpawnManager Instance { get; private set; }

    [SerializeField] private GameObject[] _characterPrefabs;
    [SerializeField] private Transform[] _spawnPoints;

    // Test Mode Settings
    private bool _isTestMode = false;
    private int _testSpawnIndex = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            if (GameSessionData.PlayerSelections.Count == 0)
            {
                _isTestMode = true;
                Debug.Log("[PlayerSpawnManager] Enter Test Mode");

                SpawnPlayerForTestMode(NetworkManager.Singleton.LocalClientId);
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedInTestMode;
            }
            else
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            if (_isTestMode)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedInTestMode;
            }
            else if (NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
            }
        }
    }

    private void OnSceneLoaded(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (sceneName == "Bob_Test_Scene")
        {
            int spawnIndex = 0;
            foreach (var kvp in GameSessionData.PlayerSelections)
            {
                ulong clientId = kvp.Key;
                int charIndex = kvp.Value;

                Transform spawnPos = (_spawnPoints != null && _spawnPoints.Length > spawnIndex) ? _spawnPoints[spawnIndex] : transform;

                GameObject spawnedObj = Instantiate(_characterPrefabs[charIndex], spawnPos.position, Quaternion.identity);

                spawnedObj.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

                spawnIndex++;
            }
        }
    }

    private void OnClientConnectedInTestMode(ulong clientId)
    {
        if (clientId == NetworkManager.ServerClientId) return;

        SpawnPlayerForTestMode(clientId);
    }

    private void SpawnPlayerForTestMode(ulong clientId)
    {
        if (_characterPrefabs == null || _characterPrefabs.Length == 0) return;

        int charIndex = _testSpawnIndex % _characterPrefabs.Length;

        Transform spawnPos = (_spawnPoints != null && _spawnPoints.Length > _testSpawnIndex) ? _spawnPoints[_testSpawnIndex] : transform;

        GameObject spawnedObj = Instantiate(_characterPrefabs[charIndex], spawnPos.position, Quaternion.identity);
        NetworkObject netObj = spawnedObj.GetComponent<NetworkObject>();
        if (netObj != null) netObj.SpawnAsPlayerObject(clientId, true);

        GameSessionData.PlayerSelections[clientId] = charIndex;

        Debug.Log($"[PlayerSpawnManager] Test Mode: spawn {_characterPrefabs[charIndex].name} toClient ID [{clientId}]");

        _testSpawnIndex++;
    }

}