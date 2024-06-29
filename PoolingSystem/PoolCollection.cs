using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PoolCollection", menuName = "lib/GameDependent/PoolCollection")]
public class PoolCollection : ScriptableObject
{
    public List<PoolGroup> List = new List<PoolGroup>();
    public int DefaultCount;
    public PoolExtendMethod ExtendMethod;
}

[Serializable]
public class PoolElement
{
#if UNITY_EDITOR
    [Searchable]
#endif
    public PoolType Type;

    public GameObject PoolObject
    {
        get
        {
            if (poolObject == null)
            {
                poolObject = Resources.Load<GameObject>(absolutePath);
            }

            return poolObject;
        }
    }

    private GameObject poolObject;
    public string Path;
    public int Count;
    private string absolutePath;

    public void SetPath(string groupPath)
    {
        absolutePath = groupPath + "/" + Path;
    }
}

public interface IPoolable
{
    void Init();
    void Compose();
    void Despose();
}

public enum PoolExtendMethod
{
    Extend,
    Loop,
    Block
}