using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance { get; private set; }

    public List<Transform> ActivePlayer = new List<Transform>();

    public event Action OnAddPlayer;

    public event System.Action<PlayerInventoryManager> OnLocalPlayerAdded;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddPlayer(Transform player)
    {
        if (!ActivePlayer.Contains(player))
        {
            ActivePlayer.Add(player);

            NetworkObject netObj = player.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsOwner)
            {
                PlayerInventoryManager inventory = player.GetComponent<PlayerInventoryManager>();
                if (inventory != null)
                {
                    OnLocalPlayerAdded?.Invoke(inventory);
                }
            }

            OnAddPlayer?.Invoke();
        }
    }

    public void RemovePlayer(Transform player)
    {
        if (ActivePlayer.Contains(player))
        {
            ActivePlayer.Remove(player);
        }
    }
}