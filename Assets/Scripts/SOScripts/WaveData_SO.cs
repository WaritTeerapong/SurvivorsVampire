using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct EnemyTypeWeight
{
    public GameObject EnemyPrefab;
    public EnemyTypeData_SO EnemyType;
    [Range(0f, 100f)] public float Weight;
}

[System.Serializable]
public struct EnemyTierWeight
{
    [Range(1, 3)] public int Tier;
    [Range(0f, 100f)] public float Weight;
}

[CreateAssetMenu(fileName = "WaveData_SO", menuName = "DataSO/WaveData_SO", order = 0)]
public class WaveData_SO : ScriptableObject
{
    [Header("Wave Settings")]
    public float WaveDuration = 30f;
    public float RestTime = 5f;
    public float SpawnCD = 1f;
    public int MaxActiveEnemies = 50;

    [Header("Enemy Spawns")]
    public List<EnemyTypeWeight> AllowedEnemyTypes;
    public List<EnemyTierWeight> AllowedEnemyTiers;
}