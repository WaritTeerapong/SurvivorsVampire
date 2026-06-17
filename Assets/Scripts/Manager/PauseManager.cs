using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PauseManager : NetworkBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("Pause State")]
    public NetworkVariable<bool> IsGamePaused = new NetworkVariable<bool>();
    public NetworkList<ulong> PlayersInPause = new NetworkList<ulong>();

    public NetworkList<ulong> PlayersSelectingUpgrade = new NetworkList<ulong>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        IsGamePaused.OnValueChanged += OnPauseStateChanged;

        if (!IsServer) return;

        PlayersInPause.OnListChanged += CheckPauseState;
        PlayersSelectingUpgrade.OnListChanged += CheckPauseState;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        IsGamePaused.OnValueChanged -= OnPauseStateChanged;

        if (!IsServer) return;

        PlayersInPause.OnListChanged -= CheckPauseState;
        PlayersSelectingUpgrade.OnListChanged -= CheckPauseState;
    }

    private void CheckPauseState(NetworkListEvent<ulong> changeEvent)
    {
        IsGamePaused.Value = (PlayersInPause.Count > 0 || PlayersSelectingUpgrade.Count > 0);
    }

    private void OnPauseStateChanged(bool previousValue, bool newValue)
    {
        Time.timeScale = newValue ? 0f : 1f;
    }

    [Rpc(SendTo.Server)]
    public void ToggleSettingServerRpc(ulong clientID, bool isPausing)
    {
        if (isPausing && !PlayersInPause.Contains(clientID)) PlayersInPause.Add(clientID);
        else if (!isPausing && PlayersInPause.Contains(clientID)) PlayersInPause.Remove(clientID);
    }

    [Rpc(SendTo.Server)]
    public void ToggleLevelUpPauseServerRpc(ulong clientID, bool isSelecting)
    {
        if (isSelecting && !PlayersSelectingUpgrade.Contains(clientID)) PlayersSelectingUpgrade.Add(clientID);
        else if (!isSelecting && PlayersSelectingUpgrade.Contains(clientID)) PlayersSelectingUpgrade.Remove(clientID);
    }

}
