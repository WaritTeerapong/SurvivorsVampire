using UnityEngine;
using Unity.Netcode;

public class PlayerSpawnManager : NetworkBehaviour
{
    public static PlayerSpawnManager Instance;

    [SerializeField] private GameObject[] _playerPrefabs;

    private bool _hasSpawnedEnemies = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [Rpc(SendTo.Server)]
    public void RequestSpawnPlayerRpc(int charaterIndex, ulong clinetId)
    {
        if (charaterIndex < 0 || charaterIndex >= _playerPrefabs.Length) return;

        GameObject playerInstance = Instantiate(_playerPrefabs[charaterIndex], Vector3.zero, Quaternion.identity);

        NetworkObject netObj = playerInstance.GetComponent<NetworkObject>();

        netObj.SpawnAsPlayerObject(clinetId);

        Debug.Log($"Spawned player for client {clinetId} with color index {charaterIndex}");

        if (!_hasSpawnedEnemies)
        {
            _hasSpawnedEnemies = true;
            if (EnemySpawnManager.Instance != null)
            {
                // EnemySpawnManager.Instance.SpawnEnemiesOnJoin();
                EnemySpawnManager.Instance.SpawnLoop();
            }
        }
    }
}