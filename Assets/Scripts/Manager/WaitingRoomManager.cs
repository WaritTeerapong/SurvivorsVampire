using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class WaitingRoomManager : NetworkBehaviour
{
    public TMP_Text CountdownText;

    private NetworkVariable<float> _countdownTimer = new NetworkVariable<float>(3f);
    private NetworkVariable<bool> _isCountingDown = new NetworkVariable<bool>(false);

    private HashSet<ulong> _playerInZone = new HashSet<ulong>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        Player player = other.GetComponent<Player>();
        if (player != null) _playerInZone.Add(player.OwnerClientId);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsServer) return;

        Player player = GetComponent<Player>();
        if (player != null) _playerInZone.Remove(player.OwnerClientId);
    }
    private void Update()
    {
        if (IsServer)
        {
            int totalConnected = NetworkManager.Singleton.ConnectedClientsIds.Count;
            int totalSelected = GameSessionData.PlayerSelections.Count;
            int playerReady = _playerInZone.Count;

            if (totalConnected > 0 && totalSelected == totalConnected && playerReady == totalConnected)
            {
                _isCountingDown.Value = true;
                _countdownTimer.Value -= Time.deltaTime;

                if (_countdownTimer.Value <= 0)
                {
                    NetworkManager.Singleton.SceneManager.LoadScene("Bob_Test_Scene", UnityEngine.SceneManagement.LoadSceneMode.Single);
                    this.enabled = false;
                }
            }
            else
            {
                _isCountingDown.Value = false;
                _countdownTimer.Value = 3f;
            }
        }

        if (_isCountingDown.Value)
        {
            CountdownText.gameObject.SetActive(true);
            CountdownText.text = $"Game Starts in: {Mathf.CeilToInt(_countdownTimer.Value)}";
        }
        else
        {
            CountdownText.gameObject.SetActive(false);
        }
    }
}