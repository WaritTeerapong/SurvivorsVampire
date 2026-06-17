using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance { get; private set; }

    public List<Transform> ActivePlayer = new List<Transform>();

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