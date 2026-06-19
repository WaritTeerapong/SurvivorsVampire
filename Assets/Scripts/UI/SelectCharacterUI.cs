using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class SelectCharacterUI : MonoBehaviour
{
    public GameObject SelectionCanvas;
    public Button FoxBtn;
    public Button RatBtn;
    public Button CancelBtn;

    public GameObject[] CharacterPrefabs;
    public Transform LobbySpawnPoint;

    private NetworkVariable<ulong> FoxOwner = new NetworkVariable<ulong>(ulong.MaxValue);
    private NetworkVariable<ulong> RatOwner = new NetworkVariable<ulong>(ulong.MaxValue);

    private void Start()
    {
        FoxBtn.onClick.AddListener(() => RequestSelectCharacterServerRpc(0, NetworkManager.Singleton.LocalClientId));
        RatBtn.onClick.AddListener(() => RequestSelectCharacterServerRpc(1, NetworkManager.Singleton.LocalClientId));
        CancelBtn.onClick.AddListener(() => CancelSelectionServerRpc(NetworkManager.Singleton.LocalClientId));
    }

    private void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient) return;

        ulong myId = NetworkManager.Singleton.LocalClientId;
        bool iHaveSelected = (FoxOwner.Value == myId || RatOwner.Value == myId);

        SelectionCanvas.SetActive(!iHaveSelected);
        CancelBtn.gameObject.SetActive(iHaveSelected);

        FoxBtn.interactable = (FoxOwner.Value == ulong.MaxValue);
        RatBtn.interactable = (RatOwner.Value == ulong.MaxValue);
    }

    [Rpc(SendTo.Server)]
    private void RequestSelectCharacterServerRpc(int charIndex, ulong clientId)
    {
        if (charIndex == 0 && FoxOwner.Value != ulong.MaxValue) return;
        if (charIndex == 1 && RatOwner.Value != ulong.MaxValue) return;

        if (charIndex == 0) FoxOwner.Value = clientId;
        if (charIndex == 1) RatOwner.Value = clientId;

        GameSessionData.PlayerSelections[clientId] = charIndex;

        Vector3 spawnPos = LobbySpawnPoint != null ? LobbySpawnPoint.position : Vector3.zero;
        GameObject spawnedObj = Instantiate(CharacterPrefabs[charIndex], spawnPos, Quaternion.identity);
        NetworkObject netObj = spawnedObj.GetComponent<NetworkObject>();

        netObj.SpawnAsPlayerObject(clientId, true);
        GameSessionData.SpawnDummise[clientId] = netObj;
    }

    [Rpc(SendTo.Server)]
    private void CancelSelectionServerRpc(ulong clientId)
    {
        if (FoxOwner.Value == clientId) FoxOwner.Value = ulong.MaxValue;
        if (RatOwner.Value == clientId) RatOwner.Value = ulong.MaxValue;

        if (GameSessionData.SpawnDummise.TryGetValue(clientId, out NetworkObject netObj))
        {
            if (netObj != null) netObj.Despawn(true);
            GameSessionData.SpawnDummise.Remove(clientId);
        }

        if (GameSessionData.PlayerSelections.ContainsKey(clientId))
        {
            GameSessionData.PlayerSelections.Remove(clientId);
        }
    }
}