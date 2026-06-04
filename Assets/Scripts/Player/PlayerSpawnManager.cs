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
    public void RequestSpawnPlayerRpc(int colorIndex, ulong clinetId)
    {
        if (colorIndex < 0 || colorIndex >= _playerPrefabs.Length) return;

        GameObject playerInstance = Instantiate(_playerPrefabs[colorIndex], Vector3.zero, Quaternion.identity);

        NetworkObject netObj = playerInstance.GetComponent<NetworkObject>();

        netObj.SpawnAsPlayerObject(clinetId);

        Debug.Log($"Spawned player for client {clinetId} with color index {colorIndex}");

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