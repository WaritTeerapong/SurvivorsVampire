using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDataBase_SO", menuName = "DataSO/EnemyDataBase_SO")]
public class EnemyDataBase_SO : ScriptableObject
{
    public EnemyTypeData_SO[] EnemyType;

    public EnemyTier GetEnemyData(int typeIndex, int tier)
    {
        if (EnemyType == null || typeIndex < 0 || typeIndex >= EnemyType.Length)
        {
            Debug.LogError($"[EnemyDatabase] Cannot find Enemy Type Index {typeIndex}!");
            return new EnemyTier(); // Return empty struct
        }

        // Call the Setup method you already wrote in your EnemyTypeData_SO!
        return EnemyType[typeIndex].Setup(tier);
    }

}
