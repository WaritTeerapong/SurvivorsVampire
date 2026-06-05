using System;
using Unity.Netcode;
using UnityEngine;

public class Enemy : NetworkBehaviour
{
    [Header("Enemy Stats")]
    public int MaxHealth = 20;
    public int CurrentHealth = 20;
    public float LifeTimer = 3f;

    public event Action OnEnemyDespawned;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            ResetStats();
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsServer)
        {
            OnEnemyDespawned?.Invoke();
            OnEnemyDespawned = null;
        }
    }

    public void ResetStats()
    {
        CurrentHealth = MaxHealth;
    }

    public void TakeDamage(int damage)
    {
        // Only the Server calculates health and damage
        if (!IsServer) return;

        CurrentHealth -= damage;
        if (CurrentHealth <= 0)
        {
            Despawn();
        }
    }

    private void Despawn()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            // 'true' will destroy the GameObject in the scene along with despawning it
            NetworkObject.Despawn(true);
        }
    }
}