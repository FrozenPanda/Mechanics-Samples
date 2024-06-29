using System;
using System.Collections;
using System.Collections.Generic;
using _Game.Scripts.Systems.WeeklyEventSystem;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "lib/Idle Upgrade Collection", fileName = "IdleUpgradeCollection")]
public class IdleUpgradeCollection : ScriptableObject
{
    [SerializeField] private List<IdleUpgradeByCity> Cities = new List<IdleUpgradeByCity>();
    public List<IdleUpgradeByCity> CitiesRef => Cities;
    private Dictionary<int, BaseIdleUpgrade> upgradeDictionary = new Dictionary<int, BaseIdleUpgrade>();
    private Dictionary<IdleUpgradeType, Dictionary<int, BaseIdleUpgrade>> upgradeDictionaryByType = new Dictionary<IdleUpgradeType, Dictionary<int, BaseIdleUpgrade>>();
    
    public Dictionary<int, BaseIdleUpgrade> UpgradeDictionary
    {
        get
        {
            if (!isLoaded) Load();

            return upgradeDictionary;
        }
    }

    public Dictionary<IdleUpgradeType, Dictionary<int, BaseIdleUpgrade>> UpgradeDictionaryByType
    {
        get
        {
            if (!isLoaded) Load();

            return upgradeDictionaryByType;
        }
    }

    [NonSerialized] private bool isLoaded;
    private IdleUpgradeLevel currentIdleUpgradeLevel;

    public void Load()
    {
        var activeCity = LevelManager.Instance.ActiveCityId;
        var activeLevel = LevelManager.Instance.ActiveLevelId;
        LoadIdleUpgrades(activeCity, activeLevel);

        isLoaded = true;
    }

    private void LoadIdleUpgrades(int activeCity, int activeLevel)
    {
        upgradeDictionary.Clear();
        upgradeDictionaryByType.Clear();

        foreach (var city in Cities)
        {
            // city.FixCustomerType();
            // city.FixExpendId();
            // Debug.Log($"Upgrade city :{city.CityId}");
            // city.FixMoveSpeedUpgrades();
            if (city.CityId != activeCity) continue;
            
            foreach (var levelUgrades in city.Levels)
            {
                if (true)
                {
                    // levelUgrades.DisableChefUpgrades();
                    // levelUgrades.DisableContainerUpgrades(new List<string>(){"C11", "C12"});
                    // levelUgrades.BaristaToCourier();
                    // EditorUtility.SetDirty(levelUgrades);
                    currentIdleUpgradeLevel = levelUgrades;
                    int i = -1;
                    foreach (var item in currentIdleUpgradeLevel.Items)
                    {
                        ++i;
                        if (!item.IsActive) continue;

                        // var upgradePrice = EventManager.Instance.InEvent ? item.UpgradePrice : (item.UpgradePrice * PlayerModeManager.Instance.GetActiveModeMultiplier());
                        //   item.UpgradePriceString = upgradePrice.ToString();

                        // currentIdleUpgradeLevel.FixIdleUpgradeId();

                        var id = item.Id;

                        var copiedItem = new IdleUpgradeItem();
                        copiedItem.CopyIdleUpgadeItem(id, item);

                        var upgrade = UpgradeFactory.Create(copiedItem);

                        if (!upgradeDictionary.ContainsKey(id))
                        {
                            upgradeDictionary[id] = upgrade;
                        }
                        else
                        {
                            ColoredLogUtility.PrintColoredError($"{id} already added in dic. idx : {i}");
                            continue;
                        }

                        if (!upgradeDictionaryByType.ContainsKey(item.UpgradeType))
                        {
                            upgradeDictionaryByType[item.UpgradeType] = new Dictionary<int, BaseIdleUpgrade>();
                        }
                        upgradeDictionaryByType[item.UpgradeType].Add(id, upgrade);
                    }
                }
            }                  
            
            break;
        }
    }

    public static IdleUpgradeCollection Create(string collectionPath)
    {
        var upgradeCollection = Resources.Load<IdleUpgradeCollection>(collectionPath);
        return upgradeCollection;
    }

    public string GetStaffName(PoolType poolType)
    {
        if (currentIdleUpgradeLevel == null) return "NAN";
        
        if (poolType == PoolType.BaristaStaff || poolType == PoolType.CourierStaff)
        {
            return currentIdleUpgradeLevel.BaristaName;
        }
        else if (poolType == PoolType.CheffStaff)
        {
            return currentIdleUpgradeLevel.ChefName;
        }

        return "NAN";
    }
}

[Serializable]
public class IdleUpgradeByCity
{
    public int CityId;
    public List<IdleUpgradeLevel> Levels;

    public void FixMoveSpeedUpgrades()
    {
        foreach (var level in Levels)
        {
            Debug.Log($"Upgrade level :{level.Level}");
            level.FixMoveSpeedUpgrade();
            // EditorUtility.SetDirty(level);
        }
    }
    
    public void FixCustomerType()
    {
        foreach (var level in Levels)
        {
            bool isCarLevel = IsCarLevel(CityId, level.Level);
            // if(isCarLevel) Debug.Log($"Car level name: " + level.name);
            level.FixCustomerType(/*isCarLevel*/ false);
            // EditorUtility.SetDirty(level);
        }
    }    
    
    public void FixExpendId()
    {
        foreach (var level in Levels)
        {
            level.FixExpendId();
            // EditorUtility.SetDirty(level);
        }
    }
    
    public void DisableChefUpgrades(int activeLevel)
    {
        foreach (var level in Levels)
        {
            if(level.Level != activeLevel) continue;
            level.DisableChefUpgrades();
        }
    }

    private bool IsCarLevel(int cityId, int levelId)
    {
        List<int> carLevels = new List<int>() {105, 205, 306, 405};
        int id = (cityId + 1) * 100 + levelId;
        // Debug.Log($"id : {id}, isCarLevel : " + carLevels.Contains(id));
        return carLevels.Contains(id);
    }
}

public enum IdleUpgradeType
{
    ObjectUpgrade = 1,
    CharacterUpgrade = 2,
    UnlockStaffUpgrade = 3,
    UnlockObjectUpgrade = 4,
    GeneralUpgrade = 5,
}

[Serializable]
public class IdleUpgradeItem
{
    public bool IsActive = true;
    public int Id;

    #region Meta
    [Header("Meta")]
    public string Name;
    public StationType StationType;
    // public string UpgradeMessage;

    #endregion

    [Space]
    public IdleUpgradeType UpgradeType;
    
    [Header("General Upgrade")]
    public GeneralUpgradeType GeneralUpgradeType;
    
    [Header("Object Upgrade")]
    public string ObjectId;
    
    [HideInInspector]public string ObjectIdLowerCase
    {
        get { if (objectIdLowerCase == String.Empty) {
                objectIdLowerCase = ObjectId.ToLower();
            }
            return objectIdLowerCase;
        }
    }
    private string objectIdLowerCase = String.Empty;
    public ObjectDataType ObjectDataType;

    [Header("Character Upgrade")]
    public PoolType CharacterPoolType;
    public CharacterDataType CharacterDataType;

    [Header("Unlock Object Upgrade")]
    public List<int> UnlockedObjectExpendableId;
    public bool ShowBoxUnlock = true;
    
    [Header("Unlock Character Upgrade")]
    public List<StaffTypeCount> UnlockedStaffTypeCounts;

    [Space]
    [Header("Upgrade Mul / Add")]
    public float Multiplier;
    public float Addition;
    
    [Space]
    [Header("Upgrade Price")]
    public IdleNumber UpgradePrice;
    [HideInInspector] public string UpgradePriceString => IdleUpgradeManager.Instance.GetUpgradeCost(Id).ToString();

    public void CopyIdleUpgadeItem(int id, IdleUpgradeItem upgradeItem)
    {
        Addition = upgradeItem.Addition;
        ShowBoxUnlock = upgradeItem.ShowBoxUnlock;
        StationType = upgradeItem.StationType;
        UnlockedStaffTypeCounts = upgradeItem.UnlockedStaffTypeCounts;
        CharacterDataType = upgradeItem.CharacterDataType;
        CharacterPoolType = upgradeItem.CharacterPoolType;
        GeneralUpgradeType = upgradeItem.GeneralUpgradeType;
        Id = id;
        IsActive = upgradeItem.IsActive;
        Multiplier = upgradeItem.Multiplier;
        Name = upgradeItem.Name;
        ObjectDataType = upgradeItem.ObjectDataType;
        ObjectId = upgradeItem.ObjectId;
        UnlockedObjectExpendableId = upgradeItem.UnlockedObjectExpendableId;
        UpgradePrice = upgradeItem.UpgradePrice;
        UpgradeType = upgradeItem.UpgradeType;
    }
}

public enum GeneralUpgradeType // Ex. Tip ratio
{
    //TipRatio = 1, //
    RewardedBoostValue = 2,
    CustomerCountWithUnlock = 3,
    IdleTimeIncrease = 4,
    TipCollectTime = 5,
    //TipWorth = 6, //
    RewardedBoostDuration = 7,
    IdleWorth = 8,
    //InstantDishRatio = 9, //
    InvestorOfferIncrease = 10,
    InvestorGemRatio = 11,
    InvestorGemCount = 12,
    LevelStartMoney = 13,
    //PerfectDishRatio = 14, //
    //PerfectDishIncome = 15, //
    CarCustomerCountWithUnlock = 16,
    VehicleCustomerCountWithUnlock = 17,
    InvestorDollarRatio = 18,
    InvestorDollarCount = 19,
    TruckTime = 20,
    UnlockOrderBoard = 21,
    CoinProfit = 22,
    TipRatio = 23,
    TipWorth = 24,
    TrainTimeDecrease = 25,
    InventoryCapacity = 26,
}