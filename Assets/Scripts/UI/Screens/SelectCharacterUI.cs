using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

public class SelectCharacterUI : NetworkBehaviour
{
    [Header("=== UI References ===")]
    public GameObject SelectionCanvas;
    public Button FoxBtn;
    public Button RatBtn;
    public Button CancelBtn;

    [Header("=== Spawning Settings ===")]
    public GameObject[] CharacterPrefabs;
    public Transform LobbySpawnPoint;

    private NetworkVariable<ulong> _foxOwner = new NetworkVariable<ulong>(ulong.MaxValue);
    private NetworkVariable<ulong> _ratOwner = new NetworkVariable<ulong>(ulong.MaxValue);
    private NetworkVariable<bool> _isRoomReady = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            // 1. Force Reset ค่า NetworkVariable ทุกครั้งเมื่อ Server เริ่มทำงานใหม่
            _foxOwner.Value = ulong.MaxValue;
            _ratOwner.Value = ulong.MaxValue;
            _isRoomReady.Value = false;

            // 2. Clear Static Data ให้สะอาดหมดจด
            GameSessionData.SpawnedDummies.Clear();
            GameSessionData.PlayerSelections.Clear();

            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLobbyLoaded;
            // 3. เพิ่ม Callback เพื่อดักจับตอนคนออก
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsServer)
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLobbyLoaded;
                // อย่าลืมลบ Callback ออกตอน Despawn
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        // เคลียร์ Dummy ที่เขาถืออยู่
        if (GameSessionData.SpawnedDummies.TryGetValue(clientId, out NetworkObject netObj))
        {
            if (netObj != null && netObj.IsSpawned) netObj.Despawn(true);
            GameSessionData.SpawnedDummies.Remove(clientId);
        }

        // เคลียร์ Selection
        if (GameSessionData.PlayerSelections.ContainsKey(clientId))
        {
            GameSessionData.PlayerSelections.Remove(clientId);
        }

        // Reset สถานะตัวละครที่เขาเคยเลือก
        if (_foxOwner.Value == clientId) _foxOwner.Value = ulong.MaxValue;
        if (_ratOwner.Value == clientId) _ratOwner.Value = ulong.MaxValue;
    }

    private void Start()
    {
        FoxBtn.onClick.AddListener(() => RequestSelectCharacterServerRpc(0, NetworkManager.Singleton.LocalClientId));
        RatBtn.onClick.AddListener(() => RequestSelectCharacterServerRpc(1, NetworkManager.Singleton.LocalClientId));
        CancelBtn.onClick.AddListener(() => CancelSelectionServerRpc(NetworkManager.Singleton.LocalClientId));
    }

    private void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient) return;

        if (!_isRoomReady.Value)
        {
            SelectionCanvas.SetActive(false);
            CancelBtn.gameObject.SetActive(false);
            return;
        }

        ulong myId = NetworkManager.Singleton.LocalClientId;
        bool iHaveSelected = (_foxOwner.Value == myId || _ratOwner.Value == myId);

        SelectionCanvas.SetActive(!iHaveSelected);
        CancelBtn.gameObject.SetActive(iHaveSelected);

        FoxBtn.interactable = (_foxOwner.Value == ulong.MaxValue);
        RatBtn.interactable = (_ratOwner.Value == ulong.MaxValue);
    }

    private void OnLobbyLoaded(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (sceneName == "WaitingRoomScene") // Ensure this matches your exact scene name
        {
            RestoreLobbyState();
        }
    }

    private void RestoreLobbyState()
    {
        List<ulong> disconnectedClients = new List<ulong>();
        foreach (var clientId in GameSessionData.PlayerSelections.Keys)
        {
            if (!NetworkManager.Singleton.ConnectedClientsIds.Contains(clientId))
            {
                disconnectedClients.Add(clientId);
            }
        }

        foreach (var id in disconnectedClients)
        {
            GameSessionData.PlayerSelections.Remove(id);
            if (_foxOwner.Value == id) _foxOwner.Value = ulong.MaxValue;
            if (_ratOwner.Value == id) _ratOwner.Value = ulong.MaxValue;
        }

        foreach (var kvp in GameSessionData.PlayerSelections)
        {
            ulong oldClientId = kvp.Key;
            int oldCharIndex = kvp.Value;

            if (oldCharIndex == 0) _foxOwner.Value = oldClientId;
            if (oldCharIndex == 1) _ratOwner.Value = oldClientId;

            Vector3 spawnPos = LobbySpawnPoint != null ? LobbySpawnPoint.position : Vector3.zero;
            spawnPos += new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(-1.5f, 1.5f), 0);

            GameObject spawnedObj = Instantiate(CharacterPrefabs[oldCharIndex], spawnPos, Quaternion.identity);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(spawnedObj, gameObject.scene);

            NetworkObject netObj = spawnedObj.GetComponent<NetworkObject>();
            netObj.SpawnWithOwnership(oldClientId, true);

            GameSessionData.SpawnedDummies[oldClientId] = netObj;
        }

        _isRoomReady.Value = true;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestSelectCharacterServerRpc(int charIndex, ulong clientId)
    {
        if (!IsServer) return;
        if (charIndex == 0 && _foxOwner.Value != ulong.MaxValue) return;
        if (charIndex == 1 && _ratOwner.Value != ulong.MaxValue) return;

        if (charIndex == 0) _foxOwner.Value = clientId;
        if (charIndex == 1) _ratOwner.Value = clientId;

        GameSessionData.PlayerSelections[clientId] = charIndex;

        Vector3 spawnPos = LobbySpawnPoint != null ? LobbySpawnPoint.position : Vector3.zero;
        spawnPos += new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(-1.5f, 1.5f), 0);

        GameObject spawnedObj = Instantiate(CharacterPrefabs[charIndex], spawnPos, Quaternion.identity);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(spawnedObj, gameObject.scene);

        NetworkObject netObj = spawnedObj.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(clientId, true);

        GameSessionData.SpawnedDummies[clientId] = netObj;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void CancelSelectionServerRpc(ulong clientId)
    {
        if (!IsServer) return;

        if (_foxOwner.Value == clientId) _foxOwner.Value = ulong.MaxValue;
        if (_ratOwner.Value == clientId) _ratOwner.Value = ulong.MaxValue;

        if (GameSessionData.SpawnedDummies.TryGetValue(clientId, out NetworkObject netObj))
        {
            if (netObj != null && netObj.IsSpawned) netObj.Despawn(true);
            GameSessionData.SpawnedDummies.Remove(clientId);
        }

        if (GameSessionData.PlayerSelections.ContainsKey(clientId))
        {
            GameSessionData.PlayerSelections.Remove(clientId);
        }
    }
}