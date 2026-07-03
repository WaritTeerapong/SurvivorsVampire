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
    public string BossName = "BOSS NAME";
    public int MaxHealth = 2000;
    public float MeleeAttackRange = 1.5f;
    public float RangedAttackRange = 5.0f;

    [Header("=== Phase 1 Settings ===")]
    public float P1_MoveSpeed = 3f;
    public int P1_MeleeDamage = 20;
    public int P1_RangedDamage = 10;
    public int P1_AOEDamage = 25; // Added AOE Damage for Phase 1
    public float P1_BulletSpeed = 8f;
    public float P1_AtkCooldown = 2f;
    public float P1_AOECooldown = 8f;
    public float P1_AOETrackingTime = 2f;
    public float P1_AOETrackingSpeed = 3.5f;
    public float P1_SpawnCooldown = 15f;
    [Space]
    public List<BossMinionSetup> P1_Minions;

    [Header("=== Phase 2 Settings ===")]
    public float P2_MoveSpeed = 5.5f;
    public int P2_MeleeDamage = 35;
    public int P2_RangedDamage = 15;
    public int P2_AOEDamage = 40; // Added AOE Damage for Phase 2
    public float P2_BulletSpeed = 12f;
    public float P2_AtkCooldown = 1.2f;
    public float P2_AOECooldown = 5f;
    public float P2_AOEDistanceOffset = 2.5f;
    public float P2_SpawnCooldown = 8f;
    [Space]
    public List<BossMinionSetup> P2_Minions;

    [Header("=== VFX & Prefabs ===")]
    public GameObject AOEPrefab;
    public GameObject BulletPrefab;
    public GameObject BulletHitVFXPrefab;
    public GameObject DeathVFXPrefab;
}