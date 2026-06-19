using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PlayerSpawnManager : NetworkBehaviour
{
    public static PlayerSpawnManager Instance { get; private set; }

    [SerializeField] private GameObject[] _characterPrefabs;
    [SerializeField] private Transform[] _spawnPoints;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
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
}