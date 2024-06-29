using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TmpProfitUpgradeCollection", menuName = "lib/TmpProfitUpgradeCollection")]
public class TmpProfitUpgradeCollection : ScriptableObject
{
    [SerializeField] private List<TmpProfitUpgrade> TmpProfitUpgrades = new List<TmpProfitUpgrade>();
    private const string COLLECTION_PATH = "Configurations/TmpProfitUpgradeCollection";

    private Dictionary<int, TmpProfitUpgrade> TmpProfitUpgradesById = new Dictionary<int, TmpProfitUpgrade>();

    [NonSerialized] private bool isLoaded;

    public void Load()
    {
        TmpProfitUpgradesById.Clear();
        foreach (var tmpProfitUpgrade in TmpProfitUpgrades)
        {
            TmpProfitUpgradesById.Add(tmpProfitUpgrade.Id, tmpProfitUpgrade);
        }
        isLoaded = true;
    }

    public TmpProfitUpgrade GetTmpProfitUpgrade(int id)
    {
        if (!isLoaded) Load();
        if (!TmpProfitUpgradesById.ContainsKey(id))
        {
            return new TmpProfitUpgrade()
            {
                Id = id,
                LifeTime = 1,
                Multiplier = 1,
                Name = "Invalid"
            };
        }
        return TmpProfitUpgradesById[id];
    }

    public static TmpProfitUpgradeCollection Create()
    {
        var tmpProfitUpgradeCollection = Resources.Load<TmpProfitUpgradeCollection>(COLLECTION_PATH);
        return tmpProfitUpgradeCollection;
    }
}

[Serializable]
public class TmpProfitUpgrade
{
    public int Id;
    public string Name;
    public string Info;
    public Sprite Icon;
    
    //Meta
    public IdleUpgradeType UpgradeType;
    public ObjectDataType ObjectUpgradeType;
    public GeneralUpgradeType GeneralUpgradeType;
    public int LifeTime;
    public float Multiplier;
    public float Addition;
    public bool InstantProduction;
}
