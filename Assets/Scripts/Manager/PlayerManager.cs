using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance { get; private set; }

    public List<Player> AllPlayers = new List<Player>();
    public List<Transform> ActiveTargets = new List<Transform>();

    public event Action OnWipeout;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddPlayer(Player player)
    {
        if (!AllPlayers.Contains(player)) AllPlayers.Add(player);
        if (!ActiveTargets.Contains(player.transform)) ActiveTargets.Add(player.transform);
    }

    public void RemoveActiveTarget(Transform playerTransform)
    {
        if (ActiveTargets.Contains(playerTransform))
        {
            ActiveTargets.Remove(playerTransform);
        }

        // เช็ก Game Over ทันทีที่มีคนล้ม
        CheckWipeout();
    }

    private void CheckWipeout()
    {
        if (!IsServer) return;
        if (AllPlayers.Count == 0) return;

        bool allDeadOrDown = true;
        foreach (Player p in AllPlayers)
        {
            if (!p.IsDownOrDied)
            {
                allDeadOrDown = false;
                break;
            }
        }

        if (allDeadOrDown)
        {
            foreach (Player p in AllPlayers)
            {
                if (p.IsSpawned)
                {
                    p.ForceGhostRpc();
                }
            }

            OnWipeout?.Invoke();
        }
    }
}