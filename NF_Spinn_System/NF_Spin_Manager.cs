using System;
using System.Collections;
using System.Collections.Generic;
using _Game.Scripts.Systems.StarUpgradeSystem;
using lib.Managers.AnalyticsSystem;
using LionStudios.Suite.Analytics;
using SRF.UI;
using UnityEngine;
using UnityEngine.Events;

public class NF_Spin_Manager : Singleton<NF_Spin_Manager>
{
    public static readonly int LegendaryCountWithoutNormalSpin = 20;
    
    public UnityEvent OnSpinRewardCollected = new UnityEvent();
    public UnityEvent OnSpinStarted = new UnityEvent();
    
    private const string CollectionPath = "Configurations/NF_Spin_Collection";
    private NF_Spin_Collection _nfSpinCollection;
    private Dictionary<int, SpinItemSaveData> SpinItemSaveDataDic = new Dictionary<int, SpinItemSaveData>();
    private List<SpinItem> allSpinItemsListNormal = new List<SpinItem>();
    private List<SpinItem> allSpinItemsListSpecial = new List<SpinItem>();
    private List<SpinItem> allAvailableSpinItemsNormal = new List<SpinItem>();
    private List<SpinItem> allAvailableSpinItemsSpecial = new List<SpinItem>();
    private List<GatchaSpin> _gatchaSpinsCurrentLevel = new List<GatchaSpin>();
    public bool isReloadNeed { set; get; } = true;
    public int CurrentSpinCount = 0;
    public int CurrentSpinCountWithoutLegendary = 0;

    public Transform lastClaimedSpinTab;

    private void OnEnable()
    {
        LevelManager.Instance.OnNewLevel.AddListener(ResetSaveData);
        LevelManager.Instance.OnNewLevel.AddListener(RefreshSpins);
    }

    private void OnDisable()
    {
        LevelManager.Instance.OnNewLevel.RemoveListener(ResetSaveData);
        LevelManager.Instance.OnNewLevel.RemoveListener(RefreshSpins);
    }

    private void Awake()
    {
        LoadData();
        
        GetCollection();
        
        RefreshSpins();
    }

    private void RefreshSpins()
    {
        isReloadNeed = true;
        GetCurrentLevelSpinsNormal();
        GetCurrentLevelSpinsSpecial();

        GetAvailableLevelSpins();
        
        GetGatchaSpinCurrentLevel();
    }

    private void GetCollection()
    {
        _nfSpinCollection = NF_Spin_Collection.LoadCollection(CollectionPath);
    }

    private void GetCurrentLevelSpinsNormal()
    {
        var listData = _nfSpinCollection.GetLevelSpinItems(LevelManager.Instance.ActiveCityId);

        allSpinItemsListNormal.Clear();
        
        foreach (var spinItem in listData)
        {
            allSpinItemsListNormal.Add(spinItem);
        }
    }

    private void GetCurrentLevelSpinsSpecial()
    {
        var listData = _nfSpinCollection.GetLevelSpinItemsSpecial(LevelManager.Instance.ActiveCityId);

        allSpinItemsListSpecial.Clear();
        
        foreach (var spinItem in listData)
        {
            allSpinItemsListSpecial.Add(spinItem);
        }
    }

    private void GetAvailableLevelSpins()
    {
        allAvailableSpinItemsNormal.Clear();
        
        foreach (var spinItem in allSpinItemsListNormal)
        {
            if(!SpinItemSaveDataDic.ContainsKey(spinItem.SpinItemID))
                allAvailableSpinItemsNormal.Add(spinItem);
        }

        allAvailableSpinItemsSpecial.Clear();
        
        foreach (var spinItem in allSpinItemsListSpecial)
        {
            if(!SpinItemSaveDataDic.ContainsKey(spinItem.SpinItemID))
                allAvailableSpinItemsSpecial.Add(spinItem);
        }
    }

    private void GetGatchaSpinCurrentLevel()
    {
        _gatchaSpinsCurrentLevel.Clear();
        
        var listData = _nfSpinCollection.GetLevelGatchaSpins(LevelManager.Instance.ActiveCityId);

        foreach (var gatchaSpin in listData)
        {
            _gatchaSpinsCurrentLevel.Add(gatchaSpin); 
        }
    }

    #region PublicFuncs

    public IdleNumber GetSpinCost()
    {
        if (LevelManager.Instance.ActiveLevelId >= 3)
        {
            return new IdleNumber(10f, NumberDigits.Empty);
        }

        return new IdleNumber(5f, NumberDigits.Empty);
    }

    public int IndexRangeByProduct(SpinItem _spinItem)
    {
        for (int i = _nfSpinCollection.ProductItemsImages.Count - 1; i >= 0; i--)
        {
            if (_spinItem.RewardBySecond >= _nfSpinCollection.ProductItemsImages[i].RangeBySecond)
            {
                return _nfSpinCollection.ProductItemsImages[i].ProductSpriteAmount;
            }
        }

        return 1;
    }

    public Sprite IndexRangeByCurrency(SpinItem _spinItem)
    {
        foreach (var data in _nfSpinCollection.CurrencyItemsImagesList)
        {
            if (data.CurrencyType == _spinItem.CurrencyType)
            {
                for (int i = _nfSpinCollection.CurrencyItemsImagesList.Count - 1; i >= 0; i--)
                {
                    if (_spinItem.RewardBySecond >= _nfSpinCollection.CurrencyItemsImagesList[i].Range && _spinItem.CurrencyType == _nfSpinCollection.CurrencyItemsImagesList[i].CurrencyType)
                    {
                        return _nfSpinCollection.CurrencyItemsImagesList[i].CurrencySprite;
                    }
                }
            }
        }

        return _nfSpinCollection.EmptySprite;
    }
    
    public PackageContent GetContentBySpinItem(SpinItem _spinItem)
    {
        PackageContent content = new PackageContent();
        ContentMod contentMod = new ContentMod();
        contentMod.Content = new Content();
        
        //for updating generators -- limited for performance issues
        PanelManager.Instance.OnStatsPanelOpened?.Invoke();
        
        if (_spinItem.SpinPackageRewardType == SpinPackageRewardType.Product)
        {
            CountByType _countByType = new CountByType();
            _countByType.PoolType = _spinItem.PoolType;

            IdleNumber gpmByPooltype = ResourceManager.Instance.GetProductionCountByType(_spinItem.PoolType);

            _countByType.Count = gpmByPooltype / 60f * _spinItem.RewardBySecond;

            if (_countByType.Count < new IdleNumber(1f, NumberDigits.Empty))
                _countByType.Count = new IdleNumber(500f, NumberDigits.Empty);
            
            contentMod.Content.ProductDatas.Add(_countByType);
            
        }else if (_spinItem.SpinPackageRewardType == SpinPackageRewardType.Chest)
        {
            
            LevelChestData levelChestData = new LevelChestData();
            levelChestData.ChestID = _spinItem.ChestID;
            levelChestData.ChestCount = 1;
            contentMod.Content.Chests.Add(levelChestData);
            
        }
        else if (_spinItem.SpinPackageRewardType == SpinPackageRewardType.Currency)
        {
            
            CurrencyByType _currencyByType = new CurrencyByType();
            _currencyByType.CurrencyType = _spinItem.CurrencyType;

            if (_spinItem.CurrencyType == CurrencyType.Coin)
            {
                IdleNumber gpsTotalCoin = GPSManager.Instance.GetAutomatedMarketTotalProduction();
                _currencyByType.Price = gpsTotalCoin * _spinItem.RewardBySecond;
            }
            else
            {
                _currencyByType.Price = new IdleNumber(_spinItem.RewardBySecond, NumberDigits.Empty);
            }
            
            contentMod.Content.Currencies.Add(_currencyByType);
        }
        
        contentMod.PackageMod = PackageMod.Mod1;
        content.ContentMods.Add(contentMod);

        return content;
    }
    
    public SpinItem GetSpinItem(SpinType spinType)
    {
        SpinItem selected = new SpinItem();
        if (spinType == SpinType.NormalSpin)
        {
            if (allAvailableSpinItemsNormal.Count > 0)
            {
                selected = allAvailableSpinItemsNormal[0];
                allAvailableSpinItemsNormal.RemoveAt(0);
                return selected;
            }
        }else if (spinType == SpinType.SpecialSpin)
        {
            if (allAvailableSpinItemsSpecial.Count > 0)
            {
                selected = allAvailableSpinItemsSpecial[0];
                allSpinItemsListSpecial.RemoveAt(0);
                return selected;
            }
        }

        return null;
    }
    
    public void SpinItemTaken(SpinItem spinItem)
    {
        CurrentSpinCount++;
        CurrentSpinCountWithoutLegendary++;
        
        SpinItemSaveDataDic.TryAdd(spinItem.SpinItemID, new SpinItemSaveData());

        Product spent = new Product();
        VirtualCurrency virtualCurreny = new VirtualCurrency("SpinToken", "Virtual Currency", 5);
        spent.AddVirtualCurrency(virtualCurreny);
        AnalyticsManager.Instance.DiffEvent("Spin Wheel" , spent , GetAnalytic(spinItem));
        
        SaveData();
    }

    public int GetRemainLegendarySpinCount()
    {
        return LegendaryCountWithoutNormalSpin - CurrentSpinCountWithoutLegendary;
    }

    #endregion

    #region PrivateFunc

    private void ReloadCollection()
    {
        isReloadNeed = true;
    }

    #endregion

    #region GetFunctions

    public int TryGetGatchaSpinReward()
    {
        foreach (var gatchaSpin in _gatchaSpinsCurrentLevel)
        {
            if (gatchaSpin.SpinNumber == CurrentSpinCount)
            {
                return gatchaSpin.GiveSpinID;
            }
        }

        return -1;
    }

    public List<SpinItem> GetAvailableNormalSpins()
    {
        return allAvailableSpinItemsNormal;
    }

    public List<SpinItem> GetAvailableSpecialSpins()
    {
        return allAvailableSpinItemsSpecial;
    }
    
    public (Sprite, string ,string, int , int) GetSpinImageAndTextForContent(SpinItem spinItem)
    {
        if (spinItem.PackageContent.Boosts.Count > 0)
        {
            var boosts = spinItem.PackageContent.Boosts[0];
            return (TmpProfitUpgradeManager.Instance.GetTmpProfitUpgradeIcon(boosts.Id),
                TmpProfitUpgradeManager.Instance.GetTmpProfitUpgradeLifeTime(boosts.Id).ToString() + "sec" , $"Get instant production" , 1 , spinItem.RewardBySecond);
        }
        
        if (spinItem.PackageContent.Chests.Count > 0)
        {
            var targetChest = spinItem.PackageContent.Chests[0];
            return (ChestManager.Instance.GetChestTypeDataById(targetChest.ChestID).ChestPanelImage,
                targetChest.ChestCount.ToString() , $"Earn chest" , 1 , spinItem.RewardBySecond);
        }
        
        if (spinItem.PackageContent.Currencies.Count > 0)
        {
            return (CurrencyService.GetCurrencyItemSprite(spinItem.PackageContent.Currencies[0].CurrencyType),
                spinItem.PackageContent.Currencies[0].Price.ToString() , $"Earn {spinItem.PackageContent.Currencies[0].Price} {spinItem.PackageContent.Currencies[0].CurrencyType}" , (int)spinItem.PackageContent.Currencies[0].Price.ToFloat() , spinItem.RewardBySecond);
        }
        
        if (spinItem.PackageContent.ManagerDatas.Count > 0)
        {
            
        }
        
        if (spinItem.PackageContent.ProductDatas.Count > 0)
        {
            var targetProduct = spinItem.PackageContent.ProductDatas[0];
            return (CollectableObjectService.GetObjectIcon(targetProduct.PoolType), targetProduct.Count.ToString() , $"Earn {spinItem.PackageContent.ProductDatas[0].Count} {spinItem.PackageContent.ProductDatas[0].PoolType}" , (int)spinItem.PackageContent.ProductDatas[0].Count.ToFloat() , spinItem.RewardBySecond);
        }

        return (null, "" , "" , 1 , 1);
    }

    public Product GetAnalytic(SpinItem spinItem)
    {
        Product gainSpinProduct = new Product();
        
        if (spinItem.SpinItemType == SpinItemType.Package)
        {
            if (spinItem.PackageContent.Boosts.Count > 0)
            {
                var boosts = spinItem.PackageContent.Boosts[0];
                gainSpinProduct.AddItem("Boost" + TmpProfitUpgradeManager.Instance.GetTmpProfitUpgradeLifeTime(boosts.Id) , "Boost" , 1);
                /*return (TmpProfitUpgradeManager.Instance.GetTmpProfitUpgradeIcon(boosts.Id),
                    TmpProfitUpgradeManager.Instance.GetTmpProfitUpgradeLifeTime(boosts.Id).ToString() + "sec" , $"Get instant production" , 1);*/
            }
        
            if (spinItem.PackageContent.Chests.Count > 0)
            {
                var targetChest = spinItem.PackageContent.Chests[0];
                gainSpinProduct.AddItem(ChestManager.Instance.GetChestTypeDataById(targetChest.ChestID).ChestType.ToString() , "Chest" , 1);
                /*return (ChestManager.Instance.GetChestTypeDataById(targetChest.ChestID).ChestPanelImage,
                    targetChest.ChestCount.ToString() , $"Earn chest" , 1);*/
            }
        
            if (spinItem.PackageContent.Currencies.Count > 0)
            {
                VirtualCurrency virtualCurrency = new VirtualCurrency(
                    spinItem.PackageContent.Currencies[0].CurrencyType.ToString(), "Virtual Currency",
                    (int)(int)spinItem.PackageContent.Currencies[0].Price.ToFloat());
                gainSpinProduct.AddVirtualCurrency(virtualCurrency);
                /*return (CurrencyService.GetCurrencyItemSprite(spinItem.PackageContent.Currencies[0].CurrencyType),
                    spinItem.PackageContent.Currencies[0].Price.ToString() , $"Earn {spinItem.PackageContent.Currencies[0].Price} {spinItem.PackageContent.Currencies[0].CurrencyType}" , (int)spinItem.PackageContent.Currencies[0].Price.ToFloat());*/
            }
        
            if (spinItem.PackageContent.ManagerDatas.Count > 0)
            {
            
            }
        
            if (spinItem.PackageContent.ProductDatas.Count > 0)
            {
                var targetProduct = spinItem.PackageContent.ProductDatas[0];
                gainSpinProduct.AddItem(targetProduct.PoolType.ToString() , "Product" , (int)spinItem.PackageContent.ProductDatas[0].Count.ToFloat());
                //return (CollectableObjectService.GetObjectIcon(targetProduct.PoolType), targetProduct.Count.ToString() , $"Earn {spinItem.PackageContent.ProductDatas[0].Count} {spinItem.PackageContent.ProductDatas[0].PoolType}" , (int)spinItem.PackageContent.ProductDatas[0].Count.ToFloat());
            }
        }
        else if (spinItem.SpinItemType == SpinItemType.IdleUpgrade)
        {
            var collectableObjectData = StarUpgradeManager.Instance.GetObjectDataByInteractableId(spinItem.IdleUpgradeItem.ObjectId);
            if (spinItem.IdleUpgradeType == IdleUpgradeType.ObjectUpgrade)
            {
                if(spinItem.IdleUpgradeItem.ObjectDataType == ObjectDataType.ProductTime)
                    gainSpinProduct.AddItem("Production Time Decrease_" + collectableObjectData.PoolType.ToString() , "Spin Upgrade" , 1);
                else if(spinItem.IdleUpgradeItem.ObjectDataType == ObjectDataType.ObjectCount)
                    gainSpinProduct.AddItem("Production Increase_" + collectableObjectData.PoolType.ToString() , "Spin Upgrade" , 1);
                else if(spinItem.IdleUpgradeItem.ObjectDataType == ObjectDataType.ProductResourceCount)
                    gainSpinProduct.AddItem("Less input_" + collectableObjectData.PoolType.ToString() , "Spin Upgrade" , 1);
            }else if (spinItem.IdleUpgradeType == IdleUpgradeType.GeneralUpgrade)
            {
                if (spinItem.IdleUpgradeItem.GeneralUpgradeType == GeneralUpgradeType.UnlockOrderBoard)
                {
                    gainSpinProduct.AddItem("Order Board Repaired" , "Spin Upgrade" , 1);
                }else if (spinItem.IdleUpgradeItem.GeneralUpgradeType == GeneralUpgradeType.TruckTime)
                {
                    gainSpinProduct.AddItem("Main Order Car Speed Increase" , "Spin Upgrade" , 1);
                }
            }
        }

        return gainSpinProduct;
    }

    #endregion
    
    #region SaveLoad

    private void LoadData()
    {
        var saveData = DataService.Instance.GetData<Dictionary<int, SpinItemSaveData>>(DataType.SPIN_ITEM_SAVE_DATA);
        
        SpinItemSaveDataDic.Clear();

        foreach (var spinItemSaveData in saveData)
        {
            SpinItemSaveDataDic.TryAdd(spinItemSaveData.Key, spinItemSaveData.Value);
        }
        
        var stateData = DataService.Instance.GetData<Dictionary<StateType, int>>(DataType.STATE);
        if (stateData.ContainsKey(StateType.NF_Spinner_Spin_Count))
        {
            CurrentSpinCount = stateData[StateType.NF_Spinner_Spin_Count];
            //DataService.Instance.SetData(DataType.STATE, stateData);
        }
        else
        {
            CurrentSpinCount = 0;
        }

        if (stateData.ContainsKey(StateType.NF_Spinner_Spin_Count_WithoutLedendary))
        {
            CurrentSpinCountWithoutLegendary = stateData[StateType.NF_Spinner_Spin_Count_WithoutLedendary];
        }
        else
        {
            CurrentSpinCountWithoutLegendary = 0;
        }
    }

    private void SaveData()
    {
        DataService.Instance.SetData(DataType.SPIN_ITEM_SAVE_DATA , SpinItemSaveDataDic);
        
        var stateData = DataService.Instance.GetData<Dictionary<StateType, int>>(DataType.STATE);
        if (stateData.ContainsKey(StateType.NF_Spinner_Spin_Count))
        {
            stateData[StateType.NF_Spinner_Spin_Count] = CurrentSpinCount;
            //DataService.Instance.SetData(DataType.STATE, stateData);
        }
        else
        {
            stateData.Add(StateType.NF_Spinner_Spin_Count , CurrentSpinCount);
            //DataService.Instance.SetData(DataType.STATE, stateData);
        }
        
        if (stateData.ContainsKey(StateType.NF_Spinner_Spin_Count_WithoutLedendary))
        {
            stateData[StateType.NF_Spinner_Spin_Count_WithoutLedendary] = CurrentSpinCountWithoutLegendary;
            //DataService.Instance.SetData(DataType.STATE, stateData);
        }
        else
        {
            stateData.Add(StateType.NF_Spinner_Spin_Count_WithoutLedendary , CurrentSpinCountWithoutLegendary);
            //DataService.Instance.SetData(DataType.STATE, stateData);
        }
        
        DataService.Instance.SetData(DataType.STATE, stateData);
    }

    public void ResetLegendarySpinCount()
    {
        CurrentSpinCountWithoutLegendary = 0;
        SaveData();
    }

    private void ResetSaveData()
    {
        SpinItemSaveDataDic.Clear();
        
        DataService.Instance.SetData(DataType.SPIN_ITEM_SAVE_DATA , SpinItemSaveDataDic);
        
        var stateData = DataService.Instance.GetData<Dictionary<StateType, int>>(DataType.STATE);
        CurrentSpinCount = 0;
        if (stateData.ContainsKey(StateType.NF_Spinner_Spin_Count))
        {
            stateData[StateType.NF_Spinner_Spin_Count] = 0;
            //DataService.Instance.SetData(DataType.STATE, stateData);
        }
        else
        {
            stateData.Add(StateType.NF_Spinner_Spin_Count , 0);
            //DataService.Instance.SetData(DataType.STATE, stateData);
        }
        
        DataService.Instance.SetData(DataType.STATE, stateData);
    }

    #endregion
}


public class SpinItemSaveData
{
    
}

public enum SpinType
{
    NormalSpin,
    SpecialSpin
}
