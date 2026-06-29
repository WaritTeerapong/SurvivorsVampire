using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

public class SelectCharacterUI : NetworkBehaviour
{
    public GameObject SelectionCanvas;
    public Button FoxBtn;
    public Button RatBtn;
    public Button CancelBtn;

    public GameObject[] CharacterPrefabs;
    public Transform LobbySpawnPoint;

    private NetworkVariable<ulong> FoxOwner = new NetworkVariable<ulong>(ulong.MaxValue);
    private NetworkVariable<ulong> RatOwner = new NetworkVariable<ulong>(ulong.MaxValue);
    private NetworkVariable<bool> IsRoomReady = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            GameSessionData.SpawnedDummies.Clear();
            IsRoomReady.Value = false;

            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLobbyLoaded;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsServer && NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLobbyLoaded;
        }
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

        if (!IsRoomReady.Value)
        {
            SelectionCanvas.SetActive(false);
            CancelBtn.gameObject.SetActive(false);
            return;
        }

        ulong myId = NetworkManager.Singleton.LocalClientId;
        bool iHaveSelected = (FoxOwner.Value == myId || RatOwner.Value == myId);

        SelectionCanvas.SetActive(!iHaveSelected);
        CancelBtn.gameObject.SetActive(iHaveSelected);

        FoxBtn.interactable = (FoxOwner.Value == ulong.MaxValue);
        RatBtn.interactable = (RatOwner.Value == ulong.MaxValue);
    }

    private void OnLobbyLoaded(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (sceneName == "WaitingRoomScene")
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
            if (FoxOwner.Value == id) FoxOwner.Value = ulong.MaxValue;
            if (RatOwner.Value == id) RatOwner.Value = ulong.MaxValue;
        }

        foreach (var kvp in GameSessionData.PlayerSelections)
        {
            ulong oldClientId = kvp.Key;
            int oldCharIndex = kvp.Value;

            if (oldCharIndex == 0) FoxOwner.Value = oldClientId;
            if (oldCharIndex == 1) RatOwner.Value = oldClientId;

            Vector3 spawnPos = LobbySpawnPoint != null ? LobbySpawnPoint.position : Vector3.zero;

            spawnPos += new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(-1.5f, 1.5f), 0);

            GameObject spawnedObj = Instantiate(CharacterPrefabs[oldCharIndex], spawnPos, Quaternion.identity);

            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(spawnedObj, gameObject.scene);

            NetworkObject netObj = spawnedObj.GetComponent<NetworkObject>();

            netObj.SpawnWithOwnership(oldClientId, true);
            GameSessionData.SpawnedDummies[oldClientId] = netObj;
        }

        IsRoomReady.Value = true;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestSelectCharacterServerRpc(int charIndex, ulong clientId)
    {
        if (!IsServer) return;
        if (charIndex == 0 && FoxOwner.Value != ulong.MaxValue) return;
        if (charIndex == 1 && RatOwner.Value != ulong.MaxValue) return;

        if (charIndex == 0) FoxOwner.Value = clientId;
        if (charIndex == 1) RatOwner.Value = clientId;

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
        if (FoxOwner.Value == clientId) FoxOwner.Value = ulong.MaxValue;
        if (RatOwner.Value == clientId) RatOwner.Value = ulong.MaxValue;

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