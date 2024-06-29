using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Game.Scripts.Systems.StarUpgradeSystem;
using _Game.Scripts.Systems.WeeklyEventSystem;
using CBS;
using lib.Managers.AnalyticsSystem;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class IdleUpgradeManager : Singleton<IdleUpgradeManager>
{
    private const string CollectionPath = "Configurations/IdleUpgradeCollection";
    private const string EventCollectionPath = "Configurations/EventIdleUpgradeCollection";

	private IdleUpgradeCollection currentIdleUpgradeCollection;
	private IdleUpgradeCollection idleUpgradeCollection;
	private IdleUpgradeCollection eventIdleUpgradeCollection;

    private Dictionary<int, IdleUpgradeType> purchasedUpgrades = new Dictionary<int, IdleUpgradeType>();
    private Dictionary<IdleUpgradeType, List<BaseIdleUpgrade>> purchasedUpgradesByType = new Dictionary<IdleUpgradeType, List<BaseIdleUpgrade>>();
    private bool isLoaded = false;

    public int UpgradeCounter => upgradeCounter;
    private int upgradeCounter = 0;

    private Dictionary<ExtraUpgradeManagerType, List<IExtraUpgradeManager>> ExtraUpgradeManagers;
    private Dictionary<ExtraUpgradeManagerType, List<IExtraUpgradeManager>> PreExtraUpgradeManagers;

    public UnityEvent OnIdleUpgradeCompleted = new UnityEvent();
    public UnityEvent<int> OnIdleUpgradeCompletedId = new UnityEvent<int>();

    private void LoadDictionaries()
    {
        ExtraUpgradeManagers = new Dictionary<ExtraUpgradeManagerType, List<IExtraUpgradeManager>>
        {
            {
                ExtraUpgradeManagerType.Character,
                new List<IExtraUpgradeManager>()
                {
                    CardManager.Instance,
                    LevelBuffManager.Instance
                    //TODO: Character-Based Extra Upgrade Managers, IExtraCharacterUpgradeManager 
                }
            },
            {
                ExtraUpgradeManagerType.Object,
                new List<IExtraUpgradeManager>()
                {
                    CardManager.Instance,
                    TmpProfitUpgradeManager.Instance,
                    LevelBuffManager.Instance,
                    //TODO: Object-Based Extra Upgrade Managers, IExtraObjectUpgradeManager
                }
            },
            {
                ExtraUpgradeManagerType.Genaral,
                new List<IExtraUpgradeManager>()
                {
                    CardManager.Instance,
                    LevelBuffManager.Instance,
                    TmpProfitUpgradeManager.Instance,
                    VillageManager.Instance
                    //TODO: Genaral Extra Upgrade Managers, IExtraGeneralUpgradeManager
                }
            }
        };

        PreExtraUpgradeManagers = new Dictionary<ExtraUpgradeManagerType, List<IExtraUpgradeManager>>
        {
            {
                ExtraUpgradeManagerType.Character,
                new List<IExtraUpgradeManager>()
                {
                    //TODO: Character-Based Extra Upgrade Managers, IExtraCharacterUpgradeManager 
                }
            },
            {
                ExtraUpgradeManagerType.Object,
                new List<IExtraUpgradeManager>()
                {
                    StickerManager.Instance,
                    StarUpgradeManager.Instance,
                    DirectorManager.Instance,
                    VillageManager.Instance,
                    //TODO: Object-Based Extra Upgrade Managers, IExtraObjectUpgradeManager
                }
            },
            {
                ExtraUpgradeManagerType.Genaral,
                new List<IExtraUpgradeManager>()
                {
                    DirectorManager.Instance
                    //TODO: Genaral Extra Upgrade Managers, IExtraGeneralUpgradeManager
                }
            }
        };
    }

    #region Public Functions

    

    public void BuyUpgrade(int upgradeId, IdleUpgradeType idleUpgradeType)
    {
       // purchasedUpgrades.Add(upgradeId, idleUpgradeType);
        purchasedUpgrades.TryAdd(upgradeId, idleUpgradeType);
        //IdleExchangeService.DoExchange(CurrencyService.ActiveCurrencyType, -GetUpgradeCost(upgradeId), out _, "IdleUpgrade");
        BaseIdleUpgrade purchasedIdleUpgrade = activeIdleUpgrades[upgradeId];

        if (!purchasedUpgradesByType.ContainsKey(idleUpgradeType)) purchasedUpgradesByType.Add(idleUpgradeType, new List<BaseIdleUpgrade>());
        purchasedUpgradesByType[idleUpgradeType].Add(purchasedIdleUpgrade);

        purchasedIdleUpgrade.GainUpgrade();

        UpgradeEvent(upgradeId);
        SaveData();

        OnIdleUpgradeCompleted?.Invoke();
        OnIdleUpgradeCompletedId?.Invoke(upgradeId);
    }

    private int GetRandomID()
    {
        int rnd = Random.Range(0, 100000);

        if (purchasedUpgrades.ContainsKey(rnd))
            return GetRandomID();

        return rnd;
    }

    private void UpgradeEvent(int upgradeId)
    {
        upgradeCounter++;
        SaveStateData();
        AnalyticsManager.Instance.IdleUpgradedEvent(LevelManager.Instance.GetLevelData(), upgradeCounter, upgradeId);
        //SmartlookStarter.Instance.SmartlookEvent(SmartLookEvent.First_Idle_Upgrade);
        /*if(upgradeCounter == 1 || (upgradeCounter % 10 == 0) || currentIdleUpgradeCollection.UpgradeDictionary.Count <= 10)
        {
            AnalyticsManager.Instance.IdleUpgradedEvent(LevelManager.Instance.GetLevelData(), upgradeCounter, upgradeId);
        }*/
    }

    #region Get & Has & Is Functions
    private readonly List<BaseIdleUpgrade> tmpUpgrades = new List<BaseIdleUpgrade>();

    public string GetStaffName(PoolType poolType)
    {
        return "x";
        //return currentIdleUpgradeCollection.GetStaffName(poolType);
    }
    
    public List<BaseIdleUpgrade> GetAvailableUpgrades(bool checkPrice = false)
    {
        tmpUpgrades.Clear();
        if (activeIdleUpgrades.Count <= 0)
        { 
            Debug.Log($"<color=red> IdleUpgrade dic is empty ! </color> ");
            return tmpUpgrades; 
        }

        var activeCurrencyAmount = IdleExchangeService.GetValue(CurrencyService.ActiveCurrencyType);

        foreach (var item in activeIdleUpgrades)
        {
            if (checkPrice && activeCurrencyAmount < GetUpgradeCost(item.Value.IdleUpgradeItem.Id)) continue;

            if (!IsUpgradePurchased(item.Key))
            {
                tmpUpgrades.Add(item.Value);
            }
        }

        tmpUpgrades.Sort((x, y) => ((GetUpgradeCost(x.IdleUpgradeItem.Id) < GetUpgradeCost(y.IdleUpgradeItem.Id)) ? -1 : 1));
        return tmpUpgrades;
    }


    public bool IsAnyBuyableUpgrade()
    {
        tmpUpgrades.Clear();
        if (activeIdleUpgrades.Count <= 0)
        {
            Debug.Log($"<color=red> IdleUpgrade dic is empty ! </color> ");
            return false;
        }

        var activeCurrencyAmount = IdleExchangeService.GetValue(CurrencyService.ActiveCurrencyType);
        if (purchasedUpgrades.Count == activeIdleUpgrades.Count) return false;

        foreach (var item in activeIdleUpgrades)
        {
            if (!IsUpgradePurchased(item.Key) && activeCurrencyAmount >= GetUpgradeCost(item.Value.IdleUpgradeItem.Id))
            {
                return true;
            }
        }
        return false;
    }

    public IdleUpgradeItem GetUpgradeableIdleUpgradeItem()
    {
        tmpUpgrades.Clear();
        if (activeIdleUpgrades.Count <= 0)
        {
            Debug.Log($"<color=red> IdleUpgrade dic is empty ! </color> ");
            return null;
        }

        var activeCurrencyAmount = IdleExchangeService.GetValue(CurrencyService.ActiveCurrencyType);
        if (purchasedUpgrades.Count == activeIdleUpgrades.Count) return null;

        foreach (var item in activeIdleUpgrades)
        {
            if (!IsUpgradePurchased(item.Key) && activeCurrencyAmount >= GetUpgradeCost(item.Value.IdleUpgradeItem.Id))
            {
                return item.Value.IdleUpgradeItem;
            }
        }

        return null;
    }

    public IdleNumber GetUpgradeCost(int upgradeId)
    {
        var upgradePrice = activeIdleUpgrades[upgradeId].IdleUpgradeItem.UpgradePrice;
        var cost = EventManager.Instance.InEvent ? upgradePrice : upgradePrice * PlayerModeManager.Instance.GetActiveModeMultiplier() * LevelManager.Instance.ActiveLevelHarderingMultiplier;
        return cost;
    }

    public const string ALL_NAME = "all";
    
    public float GetUpgradedValue(string objectId, ObjectDataType objectDataType, float baseValue, PoolType poolType = PoolType.Undefined, bool isNextLevel = false)
    {
        if (!isLoaded) return baseValue;
        var result = baseValue;

        foreach (var item in PreExtraUpgradeManagers[ExtraUpgradeManagerType.Object])
        {
            result = ((IExtraObjectUpgradeManager)item).GetUpgradedValue(objectId, objectDataType, result, poolType, isNextLevel);
        }

        IteratePurchasedUpgrades(IdleUpgradeType.ObjectUpgrade,
            (item) => { return item.IdleUpgradeItem.ObjectDataType == objectDataType && ( item.IdleUpgradeItem.ObjectIdLowerCase == ALL_NAME || item.IdleUpgradeItem.ObjectId == objectId); },
            (item) => { result = item.ApplyUpgrade(result); }
            );
        foreach (var item in ExtraUpgradeManagers[ExtraUpgradeManagerType.Object])
        {
            result = ((IExtraObjectUpgradeManager) item).GetUpgradedValue(objectId, objectDataType, result, poolType, isNextLevel);
        }
        return result;
    }

    public BaseIdleUpgrade GetUpgradeById(int id)
    {
        if (activeIdleUpgrades.ContainsKey(id))
        {
            return activeIdleUpgrades[id];
        }

        return null;
    }

    public IdleNumber GetUpgradedValue(string objectId, ObjectDataType objectDataType, IdleNumber baseValue, PoolType poolType = PoolType.Undefined, bool isNextLevel = false)
    {
        if (!isLoaded) return baseValue;
        var result = baseValue;

        foreach (var item in PreExtraUpgradeManagers[ExtraUpgradeManagerType.Object])
        {
            result = ((IExtraObjectUpgradeManager)item).GetUpgradedValue(objectId, objectDataType, result, poolType, isNextLevel);
        }

        IteratePurchasedUpgrades(IdleUpgradeType.ObjectUpgrade,
            (item) => { return ( item.IdleUpgradeItem.ObjectIdLowerCase == ALL_NAME || item.IdleUpgradeItem.ObjectId == objectId) && item.IdleUpgradeItem.ObjectDataType == objectDataType; },
            (item) => { result = item.ApplyUpgrade(result); }
            );
        foreach (var item in ExtraUpgradeManagers[ExtraUpgradeManagerType.Object])
        {
            result = (item as IExtraObjectUpgradeManager)?.GetUpgradedValue(objectId, objectDataType, result, poolType, isNextLevel);
        }

        return result;
    }

    public float GetUpgradedValue(PoolType characterPooltype, CharacterDataType characterDataType, float baseValue)
    {
        if (!isLoaded) return baseValue;
        var result = baseValue;

        foreach (var item in PreExtraUpgradeManagers[ExtraUpgradeManagerType.Character])
        {
            result = ((IExtraCharacterUpgradeManager)item).GetUpgradedValue(characterPooltype, characterDataType, result);
        }

        IteratePurchasedUpgrades(IdleUpgradeType.CharacterUpgrade,
            (item) => { return item.IdleUpgradeItem.CharacterPoolType == characterPooltype && item.IdleUpgradeItem.CharacterDataType == characterDataType; },
            (item) => { result = item.ApplyUpgrade(result); }
            );
        foreach (var item in ExtraUpgradeManagers[ExtraUpgradeManagerType.Character])
        {
            result = ((IExtraCharacterUpgradeManager) item).GetUpgradedValue(characterPooltype, characterDataType, result);
        }
        return result;
    }

    public IdleNumber GetUpgradedValue(PoolType characterPooltype, CharacterDataType characterDataType, IdleNumber baseValue)
    {
        if (!isLoaded) return baseValue;
        var result = baseValue;

        foreach (var item in PreExtraUpgradeManagers[ExtraUpgradeManagerType.Character])
        {
            result = ((IExtraCharacterUpgradeManager)item).GetUpgradedValue(characterPooltype, characterDataType, result);
        }

        IteratePurchasedUpgrades(IdleUpgradeType.CharacterUpgrade,
            (item) => { return item.IdleUpgradeItem.CharacterPoolType == characterPooltype && item.IdleUpgradeItem.CharacterDataType == characterDataType; },
            (item) => { result = item.ApplyUpgrade(result); }
            );
        foreach (var item in ExtraUpgradeManagers[ExtraUpgradeManagerType.Character])
        {
            result = (item as IExtraCharacterUpgradeManager)?.GetUpgradedValue(characterPooltype, characterDataType, result);
        }

        
        
        return result;
    }

    private int b = 1;
    private void Start()
    {
        tesst(i => i < 3, c => { Multiply(b, c); },ref b);
        Debug.Log(b);
    }

    private void tesst(Func<int , bool> comparerr , Action<int> action , ref int b)
    {
        
        for (int i = 0; i < 50; i++)
        {
            if(comparerr(i)) action.Invoke(i);
        }
    }

    private int Multiply(int b ,int a)
    {
        return b * a;
    }

    public float GetUpgradedValue(GeneralUpgradeType generalSettingType, float baseValue)
    {
        if (!isLoaded) return baseValue;
        var result = baseValue;

        foreach (var item in PreExtraUpgradeManagers[ExtraUpgradeManagerType.Genaral])
        {
            result = ((IExtraGeneralUpgradeManager)item).GetUpgradedValue(generalSettingType, result);
        }

        IteratePurchasedUpgrades(IdleUpgradeType.GeneralUpgrade,
            (item) => { return item.IdleUpgradeItem.GeneralUpgradeType == generalSettingType; },
            (item) => { result = item.ApplyUpgrade(result); }
            );
        foreach (var item in ExtraUpgradeManagers[ExtraUpgradeManagerType.Genaral])
        {
            result = ((IExtraGeneralUpgradeManager) item).GetUpgradedValue(generalSettingType, result);
        }
        return result;
    }

    public IdleNumber GetUpgradedValue(GeneralUpgradeType generalSettingType, IdleNumber baseValue)
    {
        if (!isLoaded) return baseValue;
        var result = baseValue;

        foreach (var item in PreExtraUpgradeManagers[ExtraUpgradeManagerType.Genaral])
        {
            result = ((IExtraGeneralUpgradeManager)item).GetUpgradedValue(generalSettingType, result);
        }

        IteratePurchasedUpgrades(IdleUpgradeType.GeneralUpgrade,
            (item) => { return item.IdleUpgradeItem.GeneralUpgradeType == generalSettingType; },
            (item) => { result = item.ApplyUpgrade(result); }
            );
        foreach (var item in ExtraUpgradeManagers[ExtraUpgradeManagerType.Genaral])
        {
            result = (item as IExtraGeneralUpgradeManager)?.GetUpgradedValue(generalSettingType, result);
        }
        return result;
    }
    
    public int GetUpgradedValue(PoolType staffType, int baseValue)
    {
        if (!isLoaded) return baseValue;
        var result = baseValue;

        IteratePurchasedUpgrades(IdleUpgradeType.UnlockStaffUpgrade,
            (item) =>
            {
                foreach (var unlockedStaffType in item.IdleUpgradeItem.UnlockedStaffTypeCounts)
                {
                    if (unlockedStaffType.CharacterType == staffType)
                        return true;
                }

                return false;
            },
            (item) =>
            {
                if(!(item is UnlockStaffUpgrade unlockStaffUpgrade)) return;
                result = unlockStaffUpgrade.ApplyUpgrade(staffType, result);
            }
        );

        return result;
    }

    public int GetRemainIdleUpgradeCount()
    {
        int amount = 0;
        foreach (var item in activeIdleUpgrades)
        {
            if (!IsUpgradePurchased(item.Key))
            {
                amount++;
            }
        }

        return amount;
    }

    public List<BaseIdleUpgrade> GetAllPurchacedUgradesByID(string objID)
    {
        List<BaseIdleUpgrade> PruchasedUpgrades = new List<BaseIdleUpgrade>();
        List<BaseIdleUpgrade> productTimeOnlUpgrades = new List<BaseIdleUpgrade>();
        List<BaseIdleUpgrade> RelatedPruchasedUpgrades = new List<BaseIdleUpgrade>();
        
        
        foreach (var VARIABLE in purchasedUpgradesByType)
        {
            if (VARIABLE.Key == IdleUpgradeType.ObjectUpgrade)
            {
                PruchasedUpgrades = purchasedUpgradesByType[IdleUpgradeType.ObjectUpgrade];
            }
        }

        foreach (var upgrade in PruchasedUpgrades)
        {
            if (upgrade.IdleUpgradeItem.ObjectDataType == ObjectDataType.ProductTime)
            {
                productTimeOnlUpgrades.Add(upgrade);
            }
        }

        foreach (var VARIABLE in productTimeOnlUpgrades)
        {
            if (VARIABLE.IdleUpgradeItem.ObjectId == objID)
            {
                RelatedPruchasedUpgrades.Add(VARIABLE);
            }
        }

        return RelatedPruchasedUpgrades;
    }

    public bool IsUpgradePurchased(int upgradeId)
    {
        return purchasedUpgrades.ContainsKey(upgradeId);
    }
    
    public static bool IsObjectIdAll(string objectId)
    {
        return objectId.ToLower().Equals(ALL_NAME);
    }
    #endregion

    #endregion

    #region Save & Load

    private readonly Dictionary<int, BaseIdleUpgrade> activeIdleUpgrades = new Dictionary<int, BaseIdleUpgrade>();
    private readonly Dictionary<IdleUpgradeType, Dictionary<int, BaseIdleUpgrade>> activeUpgradeDictionaryByType = new Dictionary<IdleUpgradeType, Dictionary<int, BaseIdleUpgrade>>();

    private void Awake()
    {
        Load();
        LoadStateData();
        //LevelManager.Instance.LevelLoaded.AddListener(ReloadData);
       // LevelManager.Instance.BeforeCityChanged.AddListener(ReloadData);
        LevelManager.Instance.CityLoaded.AddListener(ReloadData);
        LevelManager.Instance.LevelExpended.AddListener(ReloadData);
        LevelManager.Instance.LevelExpended.AddListener(LoadLeveledData);
        CheatManager.Instance.OnCheatEnabled.AddListener(ReloadData);
    }

    private void OnDestroy()
    {
        if (LevelManager.IsAvailable())
        {
            LevelManager.Instance.CityLoaded.RemoveListener(ReloadData);
            //LevelManager.Instance.LevelLoaded.RemoveListener(ReloadData);
         //   LevelManager.Instance.BeforeCityChanged.RemoveListener(ReloadData);
            LevelManager.Instance.LevelExpended.RemoveListener(ReloadData);
            LevelManager.Instance.LevelExpended.RemoveListener(LoadLeveledData);
            CheatManager.Instance.OnCheatEnabled.RemoveListener(ReloadData);
        }
    }

    private void Load()
    {
        //if (isLoaded) return;
        idleUpgradeCollection = IdleUpgradeCollection.Create(CollectionPath);
        eventIdleUpgradeCollection = IdleUpgradeCollection.Create(EventCollectionPath);

        bool inEvent = EventManager.Instance.InEvent;
        currentIdleUpgradeCollection = inEvent ? eventIdleUpgradeCollection : idleUpgradeCollection;
        currentIdleUpgradeCollection.Load();

        activeIdleUpgrades.Clear();
        activeUpgradeDictionaryByType.Clear();

        foreach (var upgrade in currentIdleUpgradeCollection.UpgradeDictionary)
        {
            if (!activeIdleUpgrades.ContainsKey(upgrade.Key))
                activeIdleUpgrades.Add(upgrade.Key, upgrade.Value);

            if (!activeUpgradeDictionaryByType.ContainsKey(upgrade.Value.IdleUpgradeItem.UpgradeType))
                activeUpgradeDictionaryByType.Add(upgrade.Value.IdleUpgradeItem.UpgradeType, new Dictionary<int, BaseIdleUpgrade>());
            if (!activeUpgradeDictionaryByType[upgrade.Value.IdleUpgradeItem.UpgradeType].ContainsKey(upgrade.Key))
                activeUpgradeDictionaryByType[upgrade.Value.IdleUpgradeItem.UpgradeType].Add(upgrade.Key, upgrade.Value);

        }
        
        if(!isLoaded) LoadDictionaries();
        LoadData(inEvent);
        isLoaded = true;
        
        IdleUpgradePanel _idleUpgradePanel = FindObjectOfType<IdleUpgradePanel>();
        if (_idleUpgradePanel)
        {
            // Debug.Log("Idle Upgrade Panele Ulaşıldı");
            _idleUpgradePanel.RearrangeUpdatesOnSceneLoaded();
        }
    }

    /*private void AddLeveledIdleUpgrades()
    {
        //if (isLoaded) return;
        idleUpgradeCollection ??= IdleUpgradeCollection.Create(CollectionPath);
        eventIdleUpgradeCollection ??= IdleUpgradeCollection.Create(EventCollectionPath);

        bool inEvent = EventManager.Instance.InEvent;
        currentIdleUpgradeCollection = inEvent ? eventIdleUpgradeCollection : idleUpgradeCollection;
        currentIdleUpgradeCollection.Load();

        if (!isLoaded) LoadDictionaries();
        LoadData(inEvent);
        isLoaded = true;

        IdleUpgradePanel _idleUpgradePanel = FindObjectOfType<IdleUpgradePanel>();
        if (_idleUpgradePanel)
        {
            // Debug.Log("Idle Upgrade Panele Ulaşıldı");
            _idleUpgradePanel.RearrangeUpdatesOnSceneLoaded();
        }
    }*/

    private void LoadData(bool inEvent)
    {
        var collectableData = DataService.Instance.GetData<Dictionary<InventoryType, List<int>>>(DataType.UNLOCK);
        var inventoryType = inEvent ? InventoryType.EventIdleUpgrades : PlayerModeManager.Instance.GetActiveIdleUpgrades();//InventoryType.IdleUpgrades;

        purchasedUpgrades.Clear();
        purchasedUpgradesByType.Clear();

        if (collectableData.ContainsKey(inventoryType))
        {
            foreach (var idleUpgradeId in collectableData[inventoryType])
            {
                if (activeIdleUpgrades.ContainsKey(idleUpgradeId))
                {
                    var upgrade = activeIdleUpgrades[idleUpgradeId];
                    purchasedUpgrades.Add(idleUpgradeId, upgrade.IdleUpgradeItem.UpgradeType);

                    if (!purchasedUpgradesByType.ContainsKey(upgrade.IdleUpgradeItem.UpgradeType)) purchasedUpgradesByType.Add(upgrade.IdleUpgradeItem.UpgradeType, new List<BaseIdleUpgrade>());
                    purchasedUpgradesByType[upgrade.IdleUpgradeItem.UpgradeType].Add(upgrade);
                }
            }
        }
    }

    private void SaveData()
    {
        if (!isLoaded) return;
        var collectableData = DataService.Instance.GetData<Dictionary<InventoryType, List<int>>>(DataType.UNLOCK);
        var inventoryType = EventManager.Instance.InEvent ? InventoryType.EventIdleUpgrades : PlayerModeManager.Instance.GetActiveIdleUpgrades(); //InventoryType.IdleUpgrades;

        if (!collectableData.ContainsKey(inventoryType))
        {
            collectableData[inventoryType] = new List<int>();
        }
        
        collectableData[inventoryType].Clear();
        
        foreach (var idleUpgradeId in purchasedUpgrades)
        {
            collectableData[inventoryType].Add(idleUpgradeId.Key);
        }
        
        DataService.Instance.SetData(DataType.UNLOCK, collectableData, true);
    }

    private void LoadStateData()
    {
        var stateData = DataService.Instance.GetData<Dictionary<StateType, int>>(DataType.STATE);
        if (stateData == null)
        {
            stateData = new Dictionary<StateType, int>();
        }
        if (!stateData.ContainsKey(StateType.TotalUpgradeCounter)) stateData[StateType.TotalUpgradeCounter] = 0;
        upgradeCounter = stateData[StateType.TotalUpgradeCounter];
    }

    private void SaveStateData()
    {
        var stateData = DataService.Instance.GetData<Dictionary<StateType, int>>(DataType.STATE);
        stateData[StateType.TotalUpgradeCounter] = UpgradeCounter;
        DataService.Instance.SetData(DataType.STATE, stateData);
    }    

    private void ReloadData()
    {
        upgradeCounter = 0;
        SaveStateData();
        Load();
    }

    private void LoadLeveledData()
    {
        currentIdleUpgradeCollection.Load();

        activeIdleUpgrades.Clear();
        activeUpgradeDictionaryByType.Clear();

        foreach (var upgrade in currentIdleUpgradeCollection.UpgradeDictionary)
        {
            if (!activeIdleUpgrades.ContainsKey(upgrade.Key))
                activeIdleUpgrades.Add(upgrade.Key, upgrade.Value);

            if (!activeUpgradeDictionaryByType.ContainsKey(upgrade.Value.IdleUpgradeItem.UpgradeType))
                activeUpgradeDictionaryByType.Add(upgrade.Value.IdleUpgradeItem.UpgradeType, new Dictionary<int, BaseIdleUpgrade>());
            if (!activeUpgradeDictionaryByType[upgrade.Value.IdleUpgradeItem.UpgradeType].ContainsKey(upgrade.Key))
                activeUpgradeDictionaryByType[upgrade.Value.IdleUpgradeItem.UpgradeType].Add(upgrade.Key, upgrade.Value);

        }

        IdleUpgradePanel _idleUpgradePanel = FindObjectOfType<IdleUpgradePanel>();
        if (_idleUpgradePanel)
        {
            // Debug.Log("Idle Upgrade Panele Ulaşıldı");
            _idleUpgradePanel.RearrangeUpdatesOnSceneLoaded();
        }
    }

    #endregion

    #region Private Funtions
    
    private void IteratePurchasedUpgrades(IdleUpgradeType type, Func<BaseIdleUpgrade, bool> comparer, Action<BaseIdleUpgrade> action)
    {
        if (activeIdleUpgrades.Count <= 0)
        {
            Debug.Log($"<color=red> IdleUpgrade dic is empty ! </color> ");
            return;
        }
        if (!activeUpgradeDictionaryByType.ContainsKey(type))
        {
            return;
        }

        if (!purchasedUpgradesByType.ContainsKey(type)) return;
        foreach (var item in purchasedUpgradesByType[type])
        {
           // var upgrade = currentIdleUpgradeCollection.UpgradeDictionaryByType[type][item.Key];
            if (comparer(item)) action(item);
        }
        /*
        foreach (var item in currentIdleUpgradeCollection.UpgradeDictionaryByType[type])
        {
            if (IsUpgradePurchased(item.Key) && comparer(item.Value))
            {
                action(item.Value);
            }
        }*/
    }
    #endregion
    
}
