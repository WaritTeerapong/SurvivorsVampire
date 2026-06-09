using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "WaveData_SO", menuName = "DataSO/WaveDatabase_SO", order = 0)]
public class WaveDatabase_SO : ScriptableObject
{
    public List<WaveData_SO> Waves;
}