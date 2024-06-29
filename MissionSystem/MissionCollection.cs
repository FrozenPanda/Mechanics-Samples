using System;
using System.Collections;
using System.Collections.Generic;
using lib.Managers.AnalyticsSystem;
using UnityEngine;
using UnityEngine.Serialization;


[CreateAssetMenu(fileName = "MissionCollection", menuName = "lib/MissionCollection")]
public class MissionCollection : ScriptableObject
{
    public List<CityMissionList> MissionList = new List<CityMissionList>();

    public List<MissionGeneralData> missionGeneralDatas = new List<MissionGeneralData>();

    private Dictionary<int, BaseMission> MissionListDic = new Dictionary<int, BaseMission>();
    
    public LeveledMissionCollection LeveledMissionCollection { get; private set; }

    public static MissionCollection LoadCollection(string collectionPath)
    {
        return Resources.Load<MissionCollection>(collectionPath);
    }

    public List<BaseMission> GetMissionList()
    {
        return null;
    }

    public void Load()
    {
        LoadLeveledMissions();
    }

    public void LoadLeveledMissions()
    {
        var activeCity = LevelManager.Instance.ActiveCityId;
        var activelevel = LevelManager.Instance.ActiveLevelId;

        foreach (CityMissionList cityMissionList in MissionList)
        {
            if (cityMissionList.CityId == activeCity)
            {
                foreach (LeveledMissionCollection leveledMissionCollection in cityMissionList.LeveledMissionCollections)
                {
                    if(leveledMissionCollection.LevelId != activelevel) continue;

                    LeveledMissionCollection = leveledMissionCollection;
                    leveledMissionCollection.ActiveInCityId = cityMissionList.CityId;
                    
                    break;
                }
                
                break;
            }
        }
    }

    public BaseMission GetMissionWithId(int id)
    {
        if (MissionListDic.ContainsKey(id))
        {
            return MissionListDic[id];
        }

        return null;
    }
}

public enum CurrencyMissionType
{
    Collect,
    Spend
}

public enum MissionType
{
    CollectProduct,
    CollectTotalProduct,
    CollectSpendCurrency,
    SpendProduct,
    UnlockGenerator,
    LevelUpGenerator,
    SideOrderCarComplete,
    OrderBoardComplete,
    AdsWatch,
    ManagerUpgrade,
    IdleUpgrade,
    AllMainOrders,
    AllIdleUpgrades,
    AllGeneratorsMax,
    AllMarketsMax,
    MarketAutomate,
    BuildingUpgrade,
    MarketUnlock,
    None,
}

[Serializable]
public class CityMissionList
{
    public int CityId;
    public List<LeveledMissionCollection> LeveledMissionCollections = new List<LeveledMissionCollection>();
}

[Serializable]
public class BaseMission
{
    public string Name;
    public int Id;
    public bool isActive = true;
    [HideInInspector]public Sprite missionSprite;
    [HideInInspector]public int UniqueId;
    [HideInInspector]public string Info;
    [HideInInspector]public string FinalName;
    public MissionType MissionType;
    protected bool ReleaseTabBool;

    protected MissionTab currentMissionTab;

    protected MissionSaveData SaveData;
    
    //Collect Product Missions
#if UNITY_EDITOR
    [DrawIf("MissionType", global::MissionType.CollectProduct)] [Searchable]
#endif 
    public PoolType CollectProductType = PoolType.Wheat;
    [DrawIf("MissionType" , global::MissionType.CollectProduct)]public IdleNumber CollectProductAmountWanted;
    [HideInInspector]public IdleNumber CollectProductAmountCurrent;
    
    //Collect Total Product Mission
#if UNITY_EDITOR
    [FormerlySerializedAs("CollectTotalProduct")] [DrawIf("MissionType", global::MissionType.CollectTotalProduct)] [Searchable]
#endif 
    public PoolType CollectTotalProductType = PoolType.Wheat;
    [DrawIf("MissionType" , global::MissionType.CollectTotalProduct)]public IdleNumber CollectTotalProductAmountWanted;
    [HideInInspector]public IdleNumber CollectTotalProductAmountCurrent;

    //Spend Product Missions
#if UNITY_EDITOR
    [DrawIf("MissionType" , global::MissionType.SpendProduct)][Searchable]
#endif
    public PoolType SpendProdcutType;
    [DrawIf("MissionType" , global::MissionType.SpendProduct)]public IdleNumber SpendProductAmountWanted;
    [HideInInspector]public IdleNumber SpentProductAmountCurrent;
    
    //Collect Spent Currency Mission
    [DrawIf("MissionType" , global::MissionType.CollectSpendCurrency)]public CurrencyType CurrencyType;
    [DrawIf("MissionType" , global::MissionType.CollectSpendCurrency)]public CurrencyMissionType CollectOrSpendCurrency;
    [DrawIf("MissionType" , global::MissionType.CollectSpendCurrency)]public IdleNumber CurrencyAmountWanted;
    [HideInInspector]public IdleNumber CurrencyAmountCurrent;
    
    //Generator Unlock
    [DrawIf("MissionType" , global::MissionType.UnlockGenerator)]public string UnlockGeneratorId;
    [DrawIf("MissionType", global::MissionType.UnlockGenerator)] public string UnlockGeneratorInfo = "Unlock $ Generator";
    [DrawIf("MissionType", global::MissionType.UnlockGenerator)] public string UnlockGeneratorInfoOverride;

    //Generator Level Up
    [DrawIf("MissionType" , global::MissionType.LevelUpGenerator)]public string LevelUpgeneratorId;
    [DrawIf("MissionType" , global::MissionType.LevelUpGenerator)]public IdleNumber LevelUpGeneratorTarget;

    [DrawIf("MissionType", global::MissionType.LevelUpGenerator)] public string LevelUpGeneratorInfo = "LevelUp $ Generator X";
    [DrawIf("MissionType" , global::MissionType.LevelUpGenerator)]public string LevelUpGeneratorInfoOverride;
    [HideInInspector]public IdleNumber LevelUpGeneratorCurrent;

    //Watch Ads
    [DrawIf("MissionType", global::MissionType.AdsWatch)]
    public IdleNumber WatchAdsAmountWanted;
    [HideInInspector] public IdleNumber WatchAdsAmountCurrent;
    
    //Side Orders
    [DrawIf("MissionType" , global::MissionType.SideOrderCarComplete)]public IdleNumber SideOrderCarCompleteTarget;
    [HideInInspector]public IdleNumber SideOrderCarCompleteCurrent;
    
    //Order Board
    [DrawIf("MissionType" , global::MissionType.OrderBoardComplete)]public IdleNumber OrderBoardCompleteTarget;
    [HideInInspector]public IdleNumber OrderBoardCompleteCurrent;

    //Manager Upgrade
    //[DrawIf("MissionType", global::MissionType.ManagerUpgrade)] public int ManagerId;
    [DrawIf("MissionType", global::MissionType.ManagerUpgrade)] public IdleNumber ManagerLevelWanted;
    [HideInInspector]public IdleNumber ManagerLevelCurrent;

    //Idle Upgrade
    [DrawIf("MissionType", global::MissionType.IdleUpgrade)] public int IdleUpgradeCount;
    [HideInInspector] public int IdleUpgradeCurrent;
    
    //MarketAutomate
    [DrawIf("MissionType", global::MissionType.MarketAutomate)] public string MarketID;
    [DrawIf("MissionType", global::MissionType.MarketAutomate)] public string AutomateMarketInfo = "Automate $ Market";
    [DrawIf("MissionType", global::MissionType.MarketAutomate)] public string AutomateMarketInfoOverride;
    
    //BuildingUpgrade
    [DrawIf("MissionType", global::MissionType.BuildingUpgrade)] public string BuildingID;
    [DrawIf("MissionType", global::MissionType.BuildingUpgrade)] public IdleNumber BuildingUpgradeAmountWanted;
    [DrawIf("MissionType", global::MissionType.BuildingUpgrade)] public string BuildingUpgradeInfo = "Upgrade $ Building lvl to #";
    [DrawIf("MissionType" , global::MissionType.BuildingUpgrade)]public string BuildingUpgradeInfoOverride;
    [HideInInspector]public IdleNumber BuildingUpgradeAmountCurrent;
    
    //Market Unlock
    [DrawIf("MissionType", global::MissionType.MarketUnlock)] public string MarketUnlockID;
    [DrawIf("MissionType", global::MissionType.MarketUnlock)] public string UnlockMarketInfo = "Unlock $ Market";
    [DrawIf("MissionType", global::MissionType.MarketUnlock)] public string UnlockMarketInfoOverride;
    //All Main Order
    
    //All Idle Upgrade
    [HideInInspector]public IdleNumber AllIdleUpgradeCurrent = new IdleNumber();
    
    //All Generators Max
    [HideInInspector]public IdleNumber AllgeneratorMaxTarget = new IdleNumber();
    [HideInInspector]public IdleNumber AllgeneratorMaxCurrent = new IdleNumber();
    
    //All Markets Max
    [HideInInspector]public IdleNumber AllMarketMaxTarget = new IdleNumber();
    [HideInInspector]public IdleNumber AllMarketMaxCurrent = new IdleNumber();

    //Reward
    public PackageContent Reward;
    
    //Tutorial
    [Header("Tutorial")]
    public float StartTutorialDelayTime = 1f;
    public TutorialType StartTutorialWhenStart = TutorialType.None;
    public TutorialType StartTutorialWhenCompleted = TutorialType.None;
    public TutorialType StartTutorialWhenClaimed = TutorialType.None;
    public bool ShowTutorialAtBeginning;
    public bool CanCallOrderCar;
    public bool LockOrderCar;
    public bool ShowClaimTutorial;
    [HideInInspector]public bool ClosePanelsWhenCompleted;

    public virtual void SetMissionTab(MissionTab tab) => currentMissionTab = tab;
    public BaseMission data;
    
    public virtual void InitilizeMission()
    {
        GetData();
        GetSaveData();
        IncreaseAttemptNumber();
        GetMissionSprite();
        SetMissionUItab();
        AddListeners();
        UpdateMissionTab();
        CheckMissionComplete();
        StartStateCheckTutorial();
        SendMissionStartEvents();
    }

    public virtual void SetMissionUItab()
    {
        //currentMissionTab.SetUItab(missionSprite , LevelUpGeneratorTarget , LevelUpGeneratorCurrent , GetInfoText());
    }

    public virtual void GetMissionSprite()
    {
        missionSprite = MissionManager.Instance.GetMissionSpriteByMission(this);
    }

    protected virtual void GetData()
    {
        Id = data.Id;
        //UniqueId = data.UniqueId;
        CollectProductType = data.CollectProductType;
        Info = data.Info;
        MissionType = data.MissionType;
        CollectProductAmountWanted = data.CollectProductAmountWanted;
        Reward = data.Reward;
        SpendProdcutType = data.SpendProdcutType;
        SpendProductAmountWanted = data.SpendProductAmountWanted;
        CurrencyType = data.CurrencyType;
        CollectOrSpendCurrency = data.CollectOrSpendCurrency;
        CurrencyAmountWanted = data.CurrencyAmountWanted;
        UnlockGeneratorId = data.UnlockGeneratorId;
        LevelUpgeneratorId = data.LevelUpgeneratorId;
        LevelUpGeneratorTarget = data.LevelUpGeneratorTarget;
        SideOrderCarCompleteTarget = data.SideOrderCarCompleteTarget;
        OrderBoardCompleteTarget = data.OrderBoardCompleteTarget;
        UnlockGeneratorInfo = data.UnlockGeneratorInfo;
        UnlockGeneratorInfoOverride = data.UnlockGeneratorInfoOverride;
        LevelUpGeneratorInfo = data.LevelUpGeneratorInfo;
        LevelUpGeneratorInfoOverride = data.LevelUpGeneratorInfoOverride;
        WatchAdsAmountWanted = data.WatchAdsAmountWanted;
        WatchAdsAmountCurrent = data.WatchAdsAmountCurrent;
        StartTutorialDelayTime = data.StartTutorialDelayTime;
        StartTutorialWhenCompleted = data.StartTutorialWhenCompleted;
        StartTutorialWhenClaimed = data.StartTutorialWhenClaimed;
        StartTutorialWhenStart = data.StartTutorialWhenStart;
        ClosePanelsWhenCompleted = data.ClosePanelsWhenCompleted;
        //ManagerId = data.ManagerId;
        //ManagerLevel = data.ManagerLevel;
        ManagerLevelWanted = data.ManagerLevelWanted;
        MarketID = data.MarketID;
        AutomateMarketInfo = data.AutomateMarketInfo;
        AutomateMarketInfoOverride = data.AutomateMarketInfoOverride;
        
        //Idle Upgrade
        IdleUpgradeCount = data.IdleUpgradeCount;
        IdleUpgradeCurrent = data.IdleUpgradeCurrent;
        
        CanCallOrderCar = data.CanCallOrderCar;
        LockOrderCar = data.LockOrderCar;
        ShowClaimTutorial = data.ShowClaimTutorial;
        ShowTutorialAtBeginning = data.ShowTutorialAtBeginning;
        CollectTotalProductType = data.CollectTotalProductType;
        CollectTotalProductAmountWanted = data.CollectTotalProductAmountWanted;
        CollectTotalProductAmountCurrent = data.CollectTotalProductAmountCurrent;
        AllIdleUpgradeCurrent = data.AllIdleUpgradeCurrent;
        
        BuildingID = data.BuildingID;
        BuildingUpgradeInfo = data.BuildingUpgradeInfo;
        BuildingUpgradeInfoOverride = data.BuildingUpgradeInfoOverride;
        BuildingUpgradeAmountWanted = data.BuildingUpgradeAmountWanted;
        BuildingUpgradeAmountCurrent = data.BuildingUpgradeAmountCurrent;
        
        //MarketUnlock
        MarketUnlockID = data.MarketUnlockID;
        UnlockMarketInfo = data.UnlockMarketInfo;
        UnlockMarketInfoOverride = data.UnlockMarketInfoOverride;
        
        AllgeneratorMaxTarget =
            new IdleNumber(
                InteractionManager.Instance
                    .GetAllAvailableInteractables<NFProductContainer>(InteractableType.NFProductContainer).Count,
                NumberDigits.Empty);
        AllgeneratorMaxCurrent = data.AllgeneratorMaxCurrent;
        AllMarketMaxTarget = new IdleNumber(
            InteractionManager.Instance
                .GetAllAvailableInteractables<NFMarketPlace>(InteractableType.NFMarketPlace).Count,
            NumberDigits.Empty);
        AllMarketMaxCurrent = data.AllMarketMaxCurrent;


        FinalName = GetMissionFinalName(MissionType);
        //currentMissionTab = data.currentMissionTab;
    }

    protected virtual void AddListeners(){ }

    public virtual void RemoveListeners(){ }

    protected virtual void GetSaveData()
    {
        SaveData = MissionManager.Instance.GetSaveDataByUniqueId(UniqueId);
    }

    protected virtual void IncreaseAttemptNumber()
    {
        SaveData.AttemptNumber++;
        UpdateSaveData();
    }

    protected virtual void UpdateSaveData()
    {
        SaveData.ActiveNow = true;
        MissionManager.Instance.SetSaveData(UniqueId , this.SaveData);
    }
    
    public virtual void UpdateCurrentProgress(){}

    public virtual void UpdateCurrentProgress(int id){ }

    public virtual void UpdateCurrentProgress(PoolType item , IdleNumber amount){ }
    
    public virtual void UpdateCurrentProgress(CurrencyType item , IdleNumber amount){}

    public virtual void UpdateCurrentProgress(IdleNumber a, IdleNumber b) { }

    public virtual void UpdateCurrentProgress(string a, Sticker sticker, bool b){ }
    
    public virtual void UpdateCurrentProgress(string id){}

    public virtual void UpdateMissionTab() {MissionManager.Instance.OnMissionUIUpdated?.Invoke(); }

    public virtual void CheckMissionComplete() { }
    
    public virtual void SetMissionComplete(){ 
        if(currentMissionTab != null)currentMissionTab.ShowClaim();
        CompleteStateCheckTutorial();
    }

    protected bool completedBefore = false;
    public virtual void CompleteAction(){ 
        if(completedBefore)
            return;
        completedBefore = true;
        MissionManager.Instance.GiveMissionReward(this); 
        MissionManager.Instance.IncreaseCompleteMissionCount();
        MissionManager.Instance.OnMissionCompleted?.Invoke();
        MissionManager.Instance.DeleteMissionFromTheList(this);
        SaveData.CompletedBefore = true;
        UpdateSaveData();
        ClaimedStateCheckTutorial();
        SendMissionCompleteEvents();
    }

    public virtual void ReleaseTab()
    {
        ReleaseTabBool = true;
        currentMissionTab = null;
    }

    public virtual string GetInfoText() { return Info; }

    protected string GetMissionFinalName(MissionType type)
    {
        switch (type)
        {
            case MissionType.CollectProduct:
                return $"CollectProduct_{CollectProductType}x{CollectProductAmountWanted}";
                break;
            case MissionType.CollectTotalProduct:
                return $"HaveProductInInventory_{CollectTotalProductType}_x{CollectTotalProductAmountWanted}";
                break;
            case MissionType.CollectSpendCurrency:
                if (CollectOrSpendCurrency == CurrencyMissionType.Spend)
                    return $"SpendCurrency_{CurrencyType}_x{CurrencyAmountWanted}";
                else
                    return $"CollectCurrency_{CurrencyType}_x{CurrencyAmountWanted}";
                break;
            case MissionType.SpendProduct:
                return $"SpendProduct_{SpendProdcutType}_x{SpendProductAmountWanted}";
                break;
            case MissionType.UnlockGenerator:
                return $"UnlockGenerator_{UnlockGeneratorInfoOverride}";
                break;
            case MissionType.LevelUpGenerator:
                return $"GeneratorLevelUp_{LevelUpGeneratorInfoOverride}_x{LevelUpGeneratorTarget}";
                break;
            case MissionType.SideOrderCarComplete:
                return $"CompleteExpressOrders_x{SideOrderCarCompleteTarget}";
                break;
            case MissionType.OrderBoardComplete:
                return $"MainOrderComplete_x{OrderBoardCompleteTarget}";
                break;
            case MissionType.AdsWatch:
                return $"WatchAds_x{WatchAdsAmountWanted}";
                break;
            case MissionType.ManagerUpgrade:
                return $"ManagerLevelUpgrade_{ManagerLevelWanted}_level";
                //return $"ManagerLevelUpgrade_Id:{ManagerId}_level{ManagerLevel}";
                break;
            case MissionType.IdleUpgrade:
                return $"Spin_x{IdleUpgradeCount}_times";
                break;
            case MissionType.AllMainOrders:
                return $"CompleteAllMainOrders";
                break;
            case MissionType.AllIdleUpgrades:
                return $"CompleteAllIdleUpgrades";
                break;
            case MissionType.AllGeneratorsMax:
                return $"AllGeneratorsMaxLevel";
                break;
            case MissionType.AllMarketsMax:
                break;
            case MissionType.MarketAutomate:
                return $"Market Automate {AutomateMarketInfoOverride}";
                break;
            case MissionType.BuildingUpgrade:
                return $"{BuildingUpgradeInfoOverride} build level up";
            case MissionType.MarketUnlock:
                return $"{UnlockMarketInfoOverride} market unlock";
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        return "";
    }

    #region analytics
    
    private void SendMissionStartEvents()
    {
        AnalyticsManager.Instance.MissionStartEvent(this);
    }

    private void SendMissionCompleteEvents()
    {
        AnalyticsManager.Instance.MissionCompletedEvent(this);
    }
    
    #endregion
    #region TutorialStates

    protected virtual void StartStateCheckTutorial()
    {
        if(!ConfigurationService.Configurations.CheckTutorial)
            return;
        
        if(StartTutorialWhenStart != TutorialType.None)
            TutorialManager.Instance.CheckTutorialWithDelay(StartTutorialWhenStart , StartTutorialDelayTime);
        
        if(CanCallOrderCar)
            OrderCarManager.Instance.onTutorialOrderCall?.Invoke();

        if (LockOrderCar)
            OrderCarManager.Instance.OrderCarLockedByMission = true;

        if (ShowTutorialAtBeginning)
        {
            CoroutineDispatcher.StartCoroutine(() =>
            {
                var missionTab = PanelManager.Instance.GamePlayPanel.MissionTab;
                var button = missionTab.GetComponent<RectTransform>();
                var fingerPos = button.sizeDelta.y / 2 * Vector3.up;
                TutorialFinger.ClickFinger(fingerPos, 1f, button, showWithTutorialCanvas: true, tutorialRadius: 2f);
                TutorialManager.Instance.IsMaskEnabledForClaim = true;
            }, 0.2f);
        }
    }

    protected virtual void CompleteStateCheckTutorial()
    {
        if(!ConfigurationService.Configurations.CheckTutorial)
            return;
        
        if(StartTutorialWhenCompleted != TutorialType.None)
            TutorialManager.Instance.CheckTutorialWithDelay(StartTutorialWhenCompleted);

        if (ShowClaimTutorial)
        {
            //PanelManager.Instance.Hide(PopupType.UpgradePanel);
            CoroutineDispatcher.StartCoroutine(() =>
            {
                var missionTab = PanelManager.Instance.GamePlayPanel.MissionTab;
                var button = missionTab.GetComponent<RectTransform>();
                var fingerPos = button.sizeDelta.y / 2 * Vector3.up;
                TutorialFinger.ClickFinger(fingerPos, 1f, button, showWithTutorialCanvas: true, tutorialRadius: 2f);
                TutorialManager.Instance.IsMaskEnabledForClaim = true;
            }, 0.2f);
        }
    }

    protected virtual void ClaimedStateCheckTutorial()
    {
        if(!ConfigurationService.Configurations.CheckTutorial)
            return;
        
        if(StartTutorialWhenClaimed != TutorialType.None)
            TutorialManager.Instance.CheckTutorial(StartTutorialWhenClaimed);

        if (LockOrderCar)
            OrderCarManager.Instance.OrderCarLockedByMission = false;

        if (ShowClaimTutorial)
        {
            TutorialManager.Instance.IsMaskEnabledForClaim = false;
            PanelManager.Instance.TutorialMaskPanel.HidePanel();
            TutorialFinger.StopFingerMove();
        }
    }

    #endregion

}

[Serializable]
public class MissionGeneralData
{
    public MissionType MissionType;
    [DrawIf("MissionType" , global::MissionType.CollectSpendCurrency)]public CurrencyType CurrencyType;
    [DrawIf("MissionType" , global::MissionType.CollectSpendCurrency)]public CurrencyMissionType CurrencyMissionType;
    public Sprite MissionSprite;
    [DrawIf("MissionType" , global::MissionType.LevelUpGenerator)]public Sprite MarketSprite;
}
