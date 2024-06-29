using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PoolingSystem: Singleton<PoolingSystem>
{
    private  bool isInitialized = false;
    private  PoolCollection PoolCollection;
    private  Dictionary<PoolType, PoolElement> PoolDictionary = new Dictionary<PoolType, PoolElement>();
    private  Dictionary<PoolType, Queue<GameObject>> Pool = new Dictionary<PoolType, Queue<GameObject>>();
    public List<PoolRep> PoolObjects = new List<PoolRep>();

    private const string PoolPath = "Configurations/PoolCollection";
    private static GameObject poolParent;
    
    public  void InstantiatePool()
    {
        PoolCollection = Resources.Load<PoolCollection>(PoolPath);
        poolParent = new GameObject("PoolParent");
        poolParent.AddComponent<DontDestroyOnLoad>();

        foreach (var poolGroup in PoolCollection.List)
        {
            foreach (var pool in poolGroup.PoolElements)
            {
                pool.SetPath(poolGroup.groupPath);
                int count = pool.Count > 0 ? pool.Count : PoolCollection.DefaultCount;
                Pool.Add(pool.Type, new Queue<GameObject>());
                PoolDictionary.Add(pool.Type,pool);
                for (int i = 0; i < count; i++)
                {
                    AddToPool(pool.Type, pool.PoolObject);
                }
            }
        }

        isInitialized = true;
    }

    public  T Create<T>(PoolType pooltype, Transform parent = null)
    {
        var go = Create(pooltype, parent);
        return go.GetComponent<T>();
    }

    public  GameObject Create(PoolType pooltype, Transform parent = null)
    {
        if (!isInitialized)
            InstantiatePool();

        if (Pool[pooltype].Count <= 0 && PoolCollection.ExtendMethod == PoolExtendMethod.Extend)
        {
            AddToPool(pooltype, PoolDictionary[pooltype].PoolObject);
        }

        var go = Pool[pooltype].Dequeue();
        PoolObjects.RemoveAll(gg => { return gg.go == go;});
        if (go == null)
        {
            return Create(pooltype, parent);
        }
        go.SetActive(true);

        if (parent != null && parent != go.transform.parent)
        {
            go.transform.SetParent(parent);
        }

        var poolable = go.GetComponent<IPoolable>();
        if (poolable != null)
            poolable.Compose();

        if (PoolCollection.ExtendMethod == PoolExtendMethod.Loop)
        {
            Pool[pooltype].Enqueue(go);
            PoolObjects.Add(new PoolRep() { go = go, pt = pooltype });
        }

        return go;
    }


    public  void Destroy(PoolType name, GameObject poolObject, bool changeParent = true)
    {
        if (!isInitialized)
            InstantiatePool();
        if (poolObject == null || !poolObject.activeInHierarchy) return;
        var poolable = poolObject.GetComponent<IPoolable>();
        if (poolable != null)
            poolable.Despose();

        if (PoolCollection.ExtendMethod != PoolExtendMethod.Loop)
        {
            Pool[name].Enqueue(poolObject);
            PoolObjects.Add(new PoolRep() {go = poolObject, pt = name });
        }
        if (changeParent)
        {
            if (poolObject.transform is RectTransform) poolObject.transform.SetParent(poolParent.transform);
            else poolObject.transform.parent = poolParent.transform;
        }

        poolObject.transform.position = Vector3.zero - Vector3.back * 10;

        poolObject.SetActive(false);
    }

    public void AddBatch(List<PoolType> objects, int count)
    {
        if (!isInitialized)
            InstantiatePool();
        foreach (var item in objects)
        {
            for (int i = 0; i < count; i++)
            {
                AddToPool(item, PoolDictionary[item].PoolObject);
            }
        }
    }

    private  void AddToPool(PoolType name, GameObject poolObject)
    {
        var go = GameObject.Instantiate(poolObject);
        go.SetActive(false);
        go.transform.SetParent(poolParent.transform);
        var poolable = go.GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolable.Init();
        }

        Pool[name].Enqueue(go);
        PoolObjects.Add(new PoolRep() { go = go, pt = name });
    }
}

[Serializable]
public class PoolRep
{
    public GameObject go;
    public PoolType pt;
}