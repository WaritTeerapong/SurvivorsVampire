using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Pool;
using Unity.Netcode;

public enum PoolCategory
{
    Default,
    Projectiles,
    VFX,
    Enemies
}

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    public int DefaultCapacity = 20;
    public int MaxSize = 100;

    private Dictionary<GameObject, ObjectPool<GameObject>> _prefabToPoolMap = new Dictionary<GameObject, ObjectPool<GameObject>>();

    private Dictionary<GameObject, GameObject> _instanceToPrefabMap = new Dictionary<GameObject, GameObject>();

    private Dictionary<PoolCategory, Transform> _categoryFolders = new Dictionary<PoolCategory, Transform>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitFolder();
    }

    private void InitFolder()
    {
        foreach (PoolCategory category in System.Enum.GetValues(typeof(PoolCategory)))
        {
            GameObject folder = new GameObject($"{category}_Pool");
            folder.transform.SetParent(transform);
            _categoryFolders.Add(category, folder.transform);
        }
    }

    public GameObject SpawnObject(GameObject prefab, Vector3 position, Quaternion rotation, PoolCategory category = PoolCategory.Default)
    {
        if (prefab == null)
        {
            Debug.LogError("Prefab is null Bro!!");
            return null;
        }

        // Create a new pool for this prefab if it doesn't exist yet
        if (!_prefabToPoolMap.ContainsKey(prefab))
        {
            _prefabToPoolMap[prefab] = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    GameObject obj = Instantiate(prefab);

                    if (obj.GetComponent<NetworkObject>() == null)
                    {
                        obj.transform.SetParent(_categoryFolders[category]);
                    }

                    return obj;
                },
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj),
                collectionCheck: false,
                defaultCapacity: DefaultCapacity,
                maxSize: MaxSize
            );
        }

        // Get object from pool
        GameObject spawnedObj = _prefabToPoolMap[prefab].Get();
        spawnedObj.transform.position = position;
        spawnedObj.transform.rotation = rotation;

        // Remember which prefab this instance belongs to
        _instanceToPrefabMap[spawnedObj] = prefab;

        return spawnedObj;
    }

    public void ReturnObjectToPool(GameObject instance)
    {
        if (instance == null || !_instanceToPrefabMap.ContainsKey(instance))
        {
            if (instance != null) Destroy(instance);
            return;
        }

        GameObject originalPrefab = _instanceToPrefabMap[instance];

        if (_prefabToPoolMap.ContainsKey(originalPrefab))
        {
            _prefabToPoolMap[originalPrefab].Release(instance);
        }
    }
}
