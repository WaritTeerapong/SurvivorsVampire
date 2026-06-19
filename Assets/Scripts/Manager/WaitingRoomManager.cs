using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Collections.Generic;

public class WaitingRoomManager : NetworkBehaviour
{
    [Header("UI")]
    public TMP_Text CountdownText;

    private NetworkVariable<float> _countdownTimer = new NetworkVariable<float>(3f);
    private NetworkVariable<bool> _isCountingDown = new NetworkVariable<bool>(false);

    private Collider2D _zoneCollider;
    private ContactFilter2D _filter;
    private List<Collider2D> _overlappedColliders = new List<Collider2D>();

    private void Awake()
    {
        _zoneCollider = GetComponent<Collider2D>();

        _filter = ContactFilter2D.noFilter;
    }

    private void Update()
    {
        if (IsServer)
        {
            int totalConnected = NetworkManager.Singleton.ConnectedClientsIds.Count;
            int totalSelected = GameSessionData.PlayerSelections.Count;

            _zoneCollider.Overlap(_filter, _overlappedColliders);

            HashSet<ulong> playersInZone = new HashSet<ulong>();

            foreach (var col in _overlappedColliders)
            {
                Player player = col.GetComponentInParent<Player>();
                if (player != null && player.IsSpawned)
                {
                    playersInZone.Add(player.OwnerClientId);
                }
            }

            int playersReady = playersInZone.Count;

            if (totalConnected > 0 && totalSelected == totalConnected && playersReady == totalConnected)
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