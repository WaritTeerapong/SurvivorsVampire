using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Pool;
using Unity.Netcode;

public enum PoolCategory
{
    Default,
    Projectiles,
    VFX,
    UIVFX,
    Enemies,
    XP,
    DamagePopup,
    Audio
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

    private void CreatePool(GameObject prefab, PoolCategory category)
    {
        _prefabToPoolMap[prefab] = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                bool wasActive = prefab.activeSelf;
                prefab.SetActive(false);

                GameObject obj = Instantiate(prefab);

                prefab.SetActive(wasActive);
                return obj;
            },
            actionOnGet: (obj) =>
            {
            },
            actionOnRelease: (obj) =>
            {
                obj.SetActive(false);

                if (obj.TryGetComponent<NetworkObject>(out _))
                {
                    obj.transform.SetParent(null);
                }
                else
                {
                    obj.transform.SetParent(_categoryFolders[category]);
                }
            },
            actionOnDestroy: (obj) => Destroy(obj),
            collectionCheck: false,
            defaultCapacity: DefaultCapacity,
            maxSize: MaxSize
        );
    }

    public T SpawnObject<T>(GameObject prefab, Vector3 position, Quaternion rotation, PoolCategory category = PoolCategory.Default, Transform parent = null) where T : UnityEngine.Object
    {
        if (prefab == null) return null;

        if (!_prefabToPoolMap.ContainsKey(prefab))
        {
            CreatePool(prefab, category);
        }

        GameObject spawnedObj = _prefabToPoolMap[prefab].Get();

        if (spawnedObj == null)
        {
            return SpawnObject<T>(prefab, position, rotation, category, parent);
        }

        if (parent != null)
        {
            spawnedObj.transform.SetParent(parent, false);
        }
        else
        {
            spawnedObj.transform.SetParent(null);
        }

        spawnedObj.transform.position = position;
        spawnedObj.transform.rotation = rotation;

        spawnedObj.SetActive(true);

        _instanceToPrefabMap[spawnedObj] = prefab;

        if (typeof(T) == typeof(GameObject))
        {
            return spawnedObj as T;
        }

        T component = spawnedObj.GetComponent<T>();
        if (component == null)
        {
            Debug.LogError($"[ObjectPool] หา Component {typeof(T)} ไม่เจอใน {prefab.name}");
            return null;
        }

        return component;
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