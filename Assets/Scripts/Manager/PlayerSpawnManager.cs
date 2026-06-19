using UnityEngine;
using Unity.Netcode;
using Unity.Mathematics;

public class PlayerSpawnManager : NetworkBehaviour
{
    public static PlayerSpawnManager Instance { get; private set; }

    [SerializeField] private GameObject[] _characterPrefabs;
    [SerializeField] private Transform[] _spawnPoints;

    private bool _hasSpawnedEnemies = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            int spawnIndex = 0;
            foreach (var kvp in GameSessionData.PlayerSelections)
            {
                ulong clientId = kvp.Key;
                int charIndex = kvp.Value;

                Transform spawnPos = (_spawnPoints != null && _spawnPoints.Length > spawnIndex) ? _spawnPoints[spawnIndex] : transform;

                GameObject spawnedObj = Instantiate(_characterPrefabs[charIndex], spawnPos.position, quaternion.identity);
                spawnedObj.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

                spawnIndex++;
            }
        }
    }
}