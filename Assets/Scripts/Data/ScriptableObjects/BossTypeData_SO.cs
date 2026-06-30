using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct BossMinionSetup
{
    public EnemyTypeData_SO EnemyType;
    public int Tier;
    public int Amount;
}

[CreateAssetMenu(fileName = "BossTypeData_SO", menuName = "DataSO/BossTypeData_SO", order = 0)]
public class BossTypeData_SO : ScriptableObject
{
    [Header("=== Base Stats ===")]
    public int MaxHealth = 2000;
    public float AttackRange = 2.5f;

    [Header("=== Phase 1 Settings ===")]
    public float P1_MoveSpeed = 3f;
    public int P1_AtkDamage = 15;
    public float P1_AOECooldown = 8f;
    public float P1_AOETrackingTime = 2f;
    public float P1_SpawnCooldown = 15f;
    [Space]
    public List<BossMinionSetup> P1_Minions;

    [Header("=== Phase 2 Settings ===")]
    public float P2_MoveSpeed = 5.5f;
    public int P2_AtkDamage = 25;
    public float P2_AOECooldown = 5f;
    public float P2_AOEDistanceOffset = 2.5f;
    public float P2_SpawnCooldown = 8f;
    [Space]
    public List<BossMinionSetup> P2_Minions;

    [Header("=== VFX & Prefabs ===")]
    public GameObject AOEPrefab;
    public GameObject DeathVFXPrefab;
}