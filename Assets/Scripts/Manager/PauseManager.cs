using Unity.Netcode;
using UnityEngine;

public class PauseManager : NetworkBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("Pause State")]
    public NetworkVariable<bool> IsGamePaused = new NetworkVariable<bool>();

    public NetworkList<ulong> PlayersInPause = new NetworkList<ulong>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        IsGamePaused.OnValueChanged += OnPauseStateChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        IsGamePaused.OnValueChanged -= OnPauseStateChanged;
    }

    private void OnPauseStateChanged(bool previousValue, bool newValue)
    {
        Time.timeScale = newValue ? 0f : 1f;
    }

    [Rpc(SendTo.Server)]
    public void ToggleSettingServerRpc(ulong clientID, bool isPausing)
    {
        if (isPausing)
        {
            if (!PlayersInPause.Contains(clientID))
            {
                PlayersInPause.Add(clientID);
            }
            IsGamePaused.Value = true;
        }
        else
        {
            if (PlayersInPause.Contains(clientID))
            {
                PlayersInPause.Remove(clientID);
            }

            if (PlayersInPause.Count == 0)
            {
                IsGamePaused.Value = false;
            }
        }
    }

}
