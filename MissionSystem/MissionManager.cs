using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MissionManager : Singleton<MissionManager>
{
    public UnityEvent<MissionTab> OnMissionCanClaim = new UnityEvent<MissionTab>();
    public UnityEvent OnMissionCompleted = new UnityEvent();
    public UnityEvent OnMissionsRefreshed = new UnityEvent();
    public UnityEvent OnMissionUIUpdated = new UnityEvent();
    public UnityEvent OnMissionCheatEnabled = new UnityEvent();
    public UnityEvent OnMissionCheatEnabledCurrentLevel = new UnityEvent();
    public UnityEvent<PoolType , PoolType , string> OnGeneratorFocused = new UnityEvent<PoolType, PoolType , string>();
    public UnityEvent<int> OnMissionStartedUniqueID = new UnityEvent<int>();

    private const string CollectionPath = "Configurations/MissionCollection";

    public MissionCollection MissionCollection => missionCollection;
    private MissionCollection missionCollection;

    private Dictionary<int, BaseMission> MissionDic = new Dictionary<int, BaseMission>();
    private Dictionary<int, MissionSaveData> MissionSaveDic = new Dictionary<int, MissionSaveData>();
    private Dictionary<MissionType, bool> ActiveMissionTypes = new Dictionary<MissionType, bool>();

    private Dictionary<int, LeveledMissionRewardSaveData> MissionRewardSaveDic =
        new Dictionary<int, LeveledMissionRewardSaveData>();

    private List<BaseMission> AvailableMission = new List<BaseMission>();

    private List<MissionGeneralData> MissionGeneralDatas = new List<MissionGeneralData>();

    private LeveledMissionCollection LeveledMissionCollection;
    private bool Loaded = false;

    private int totalMissionCount;
    private int totalCompletedMissionCount;
    
    public Vector3 LastCompletedMissionPoint { set; get; }
    
    public int LastMissionUniqueId { get; private set; }

    private int currentSetOfMissions;

    private void OnEnable()
    {
        LevelManager.Instance.LevelExpended.AddListener(RefreshMissionList);
        LevelManager.Instance.CityLoaded.AddListener(RefreshMissionList);
    }

    private void OnDisable()
    {
        if (LevelManager.IsAvailable())
        {
            LevelManager.Instance.LevelExpended.RemoveListener(RefreshMissionList);
            LevelManager.Instance.CityLoaded.RemoveListener(RefreshMissionList);
        }
    }

    private void Awake()
    {
        currentSetOfMissions = GetCurrentSetNumber();
        Load();
    }

    private void Start()
    {
        if (LevelManager.Instance.ActiveCityId != 99)
            ConfigurationService.Configurations.lastCityID = LevelManager.Instance.ActiveCityId;
    }

    private void Load()
    {
        missionCollection = global::MissionCollection.LoadCollection(CollectionPath);
        missionCollection.Load();
        missionCollection.LoadLeveledMissions();
        GetMissionGeneralDatas();
        LoadSaveData();
        //LeveledMissionCollection = missionCollection.LeveledMissionCollection;
        LoadDictionary();
    }

    private void GetMissionGeneralDatas()
    {
        var collectionMissionDataHolders = missionCollection.missionGeneralDatas;

        foreach (var data in collectionMissionDataHolders)
        {
            MissionGeneralDatas.Add(data);
        }
    }

    private void LoadDictionary()
    {
        ClearLevelMissions();
        ActiveMissionTypes.Clear();
        
        totalMissionCount = 0;
        totalCompletedMissionCount = 0;
        
        MissionDic.Clear();
        AvailableMission.Clear();
        
        LeveledMissionCollection = missionCollection.LeveledMissionCollection;

        foreach (var mission in LeveledMissionCollection.MissionSets[currentSetOfMissions].Missions)
        {
            if(!mission.isActive) continue;
            totalMissionCount++;
            var missionType = GetMissionBytype(mission);
            CreateUniqueId(ref missionType , LeveledMissionCollection);
            if (enabledCheat || IsMissionCompletedBefore(missionType.UniqueId))
            {
                totalCompletedMissionCount++;
                continue;
            }
            
            if (!MissionDic.ContainsKey(missionType.UniqueId))
            {
                MissionDic.Add(missionType.UniqueId , missionType);
                missionType.MissionType = mission.MissionType;
                /*missionType.UniqueId = LeveledMissionCollection.ActiveInCityId * 1000 +
                               LeveledMissionCollection.LevelId * 100 + mission.Id;*/
                
                AvailableMission.Add(missionType);
                //CreateUniqueId(ref mission , LeveledMissionCollection);
            }
        }
    }

    private void ClearLevelMissions()
    {
        foreach (BaseMission baseMission in AvailableMission)
        {
            baseMission.RemoveListeners();
            baseMission.ReleaseTab();
        }
    }

    public void RefreshMissionList()
    {
        //todo burayı daha sonra düzelt
        if (LevelManager.Instance.ActiveCityId != 99)
            ConfigurationService.Configurations.lastCityID = LevelManager.Instance.ActiveCityId;
        missionCollection.Load();
        LoadDictionary();
        OnMissionsRefreshed?.Invoke();
    }

    public int GetTotalMissionCountAtSameTime()
    {
        return LeveledMissionCollection.TotalMissionAtSameTime;
    }

    public BaseMission GetMissionItem(out bool isAnyMissionAvailable)
    {
        int index;
        if (AvailableMission.Count > 0)
        {
            index = TryToGetDifferentTypeMission(AvailableMission); 
            isAnyMissionAvailable = true;
            var currentMission = AvailableMission[index];
            AvailableMission.RemoveAt(index);
            LastMissionUniqueId = currentMission.UniqueId;

            if (!ActiveMissionTypes.ContainsKey(currentMission.MissionType))
                ActiveMissionTypes.Add(currentMission.MissionType , true);
            else
                ActiveMissionTypes[currentMission.MissionType] = true;
            
            OnMissionStartedUniqueID?.Invoke(currentMission.UniqueId);
            return currentMission;
        }
        else
        {
            isAnyMissionAvailable = false;
        }
        
        return null;
    }

    public bool IsMissionTypeActive(MissionType missionType)
    {
        if (ActiveMissionTypes.ContainsKey(missionType) && ActiveMissionTypes[missionType] == true)
            return true;
        return false;
    }
    
    private int TryToGetDifferentTypeMission(List<BaseMission> currentActiveMisions)
    {
        for (int i = 0; i < currentActiveMisions.Count; i++)
        {
            bool isSame = false;
            foreach (var activeMissionTypes in ActiveMissionTypes)
            {
                if(currentActiveMisions[i].MissionType == activeMissionTypes.Key)
                    if (activeMissionTypes.Value)
                        isSame = true;
            }

            if (!isSame)
                return i;
        }

        return 0;
    }

    public void DeleteMissionFromTheList(BaseMission mission)
    {
        if (ActiveMissionTypes.ContainsKey(mission.MissionType))
            ActiveMissionTypes[mission.MissionType] = false;
        
        AvailableMission.Remove(mission);
    }

    public void GiveMissionReward(BaseMission mission )
    {
        ShopPackageManager.Instance.GivePackageContent(mission.Reward, PackageMod.Mod1, collectWithAnim: true, isPromotionReward: true , fromPanel : PopupType.QuestRewardPanel); // Claim edildiginde verilecek
    }

    private void GiveLeveledMissionReward(PackageContent packageContent ,  bool withAnim = true)
    {
        ShopPackageManager.Instance.GivePackageContent(packageContent, PackageMod.Mod1, collectWithAnim: withAnim, isPromotionReward: true , canChestInstantOpen: !withAnim ,fromPanel : PopupType.QuestRewardPanel , isCheat: !withAnim); // Claim edildiginde verilecek
        //if(!withAnim)
            //ChestManager.Instance.EarnAllChestRewards();
        //OnMissionCompleted?.Invoke();
        if (LeveledMissionCollection.MissionSets.Count - 1 <= currentSetOfMissions)
        {
            if(!LevelManager.Instance.IsCityLastLevel())
                LevelManager.Instance.ExpendLevel();
            currentSetOfMissions = 0;
            
            var uniqueId = LeveledMissionCollection.ActiveInCityId * 1000 + LeveledMissionCollection.LevelId;
            SetSaveData(uniqueId , new LeveledMissionRewardSaveData());
            SaveData();
        }
        else
        {
            currentSetOfMissions++;
        }
        
        SaveCurrentSetNumber(currentSetOfMissions);
        LoadDictionary();
        OnMissionsRefreshed?.Invoke();
    }

    private bool IsMissionCompletedBefore(int uniqueId)
    {
        MissionSaveDic.TryGetValue(uniqueId, out MissionSaveData saveData);
        if (saveData != null)
            return saveData.CompletedBefore;
        return false;
    }

    public bool IsLeveledRewardTaken()
    {
        var uniqueId = LeveledMissionCollection.ActiveInCityId * 1000 + LeveledMissionCollection.LevelId;
        if (MissionRewardSaveDic.TryGetValue(uniqueId, out LeveledMissionRewardSaveData data))
        {
            if (data != null)
            {
                return true;
            }
        }

        return false;
    }

    public void GiveLeveledReward(bool withAnim = true)
    {
        GiveLeveledMissionReward(LeveledMissionCollection.MissionSets[currentSetOfMissions].Reward , withAnim);
    }

    public void OpenMissionLeveledRewardPanel()
    {
        PanelManager.Instance.Show(PopupType.MissionLeveledRewardPanel , new MissionLeveledRewardPanelData(LeveledMissionCollection.MissionSets[currentSetOfMissions].Reward , true , totalMissionCount, totalCompletedMissionCount));
    }

    #region GetSet

    public Sprite GetLeveledChestSprite()
    {
        var chests = LeveledMissionCollection.MissionSets[currentSetOfMissions].Reward.GetChests(PackageMod.Mod1);
        if (chests == null || chests.Count < 1)
            return null;
        var chestTypeData = ChestManager.Instance.GetChestTypeDataById(chests[0].ChestID);
        return chestTypeData.ChestPanelImage;
    }

    public Sprite GetMissionSpriteByMission(BaseMission mission)
    {
        /*if (mission.MissionType == MissionType.CollectProduct)
            return CollectableObjectService.GetCollectableObjectData(mission.CollectProductType).Icon;
        
        if (mission.MissionType == MissionType.SpendProduct)
            return CollectableObjectService.GetCollectableObjectData(mission.SpendProdcutType).Icon;*/

        if (mission.MissionType == MissionType.LevelUpGenerator && mission.LevelUpgeneratorId.StartsWith("M"))
        {
            foreach (var data in MissionGeneralDatas)
            {
                if (data.MissionType == MissionType.LevelUpGenerator)
                    return data.MarketSprite;
            }
        }

        if (mission.MissionType == MissionType.CollectSpendCurrency)
        {
            foreach (var data in MissionGeneralDatas)
            {
                if (mission.CurrencyType == data.CurrencyType &&
                    mission.CollectOrSpendCurrency == data.CurrencyMissionType)
                    return data.MissionSprite;
            }
            return CurrencyService.GetCurrencyItemSprite(mission.CurrencyType);
        }

        foreach (var data in MissionGeneralDatas)
        {
            if (data.MissionType == mission.MissionType)
                return data.MissionSprite;
        }

        return null;
    }

    public bool IsAllQuestCompleted()
    {
        if (enabledCheat)
            return true;
        
        return totalCompletedMissionCount >= totalMissionCount;
    }

    public void IncreaseCompleteMissionCount()
    {
        totalCompletedMissionCount++;
    }

    public int GetTotalCompletedMissionsCount()
    {
        return totalCompletedMissionCount;
    }

    public int GetTotalMissionCount()
    {
        return totalMissionCount;
    }

    public int GetRemainingMissionCountAtCurrentLevel()
    {
        return totalMissionCount - totalCompletedMissionCount;
    }

    #endregion

    #region MissionCreate
    //Bağzı durumlarda eski levelın missionu level bitse dahi devam edeceği için Unique id ile bakmaya karar verdim.
    private void CreateUniqueId(ref BaseMission mission , LeveledMissionCollection leveledMissionCollection)
    {
        mission.UniqueId = leveledMissionCollection.ActiveInCityId * 1000 + LeveledMissionCollection.LevelId * 100 +
                           mission.Id;
    }

    //Burada Missionların typeları değişiyor
    //Dataları set etmek için baseMissin GetData kısmında bulabilirsiniz.
    private BaseMission GetMissionBytype(BaseMission _baseMission)
    {
        var missionType = _baseMission.MissionType;

        switch (missionType)
        {
            case MissionType.CollectProduct:

                return new CollectProductMission()
                {
                    data = _baseMission,
                    Id = _baseMission.Id,
                    UniqueId = _baseMission.UniqueId,
                };
                
            case MissionType.SpendProduct:

                return new SPentProductMission()
                {
                    data = _baseMission,
                    Id = _baseMission.Id,
                    UniqueId = _baseMission.UniqueId,
                };
                
            case MissionType.CollectSpendCurrency:

                return new CollectCurrencyMission()
                {
                    data = _baseMission,
                    Id = _baseMission.Id,
                    UniqueId = _baseMission.UniqueId,
                };
                
            case MissionType.UnlockGenerator:

                return new UnlockGeneratorMission()
                {
                    data = _baseMission,
                    Id = _baseMission.Id,
                    UniqueId = _baseMission.UniqueId,
                };
                
            case MissionType.LevelUpGenerator:

                return new LevelUpGeneratorMission()
                {
                    data = _baseMission,
                    Id = _baseMission.Id,
                    UniqueId = _baseMission.UniqueId,
                };
                
            case MissionType.SideOrderCarComplete:

                return new SideOrderCarCompleteMission()
                {
                    data = _baseMission,
                    Id = _baseMission.Id,
                    UniqueId = _baseMission.UniqueId,
                };
            
            
            case MissionType.OrderBoardComplete:

                return new MainOrderCarCompleteMission()
                {
                    data = _baseMission,
                    Id = _baseMission.Id,
                    UniqueId = _baseMission.UniqueId
                };
                
            
            case MissionType.AdsWatch:

                return new WatchRewardedAdMission()
                {
                    data = _baseMission,
                    Id = _baseMission.Id,
                    UniqueId = _baseMission.UniqueId
                };


            case MissionType.ManagerUpgrade:

                return new ManagerUpgradeMission()
                {
                    data = _baseMission,
                    Id = _baseMission.Id,
                    UniqueId = _baseMission.UniqueId
                };

            case MissionType.IdleUpgrade:

                return new IdleUpgradeMission()
                {
                    data = _baseMission,
                    Id = _baseMission.Id,
                    UniqueId = _baseMission.UniqueId
                };
            
            case MissionType.CollectTotalProduct:

                return new CollectTotalProductMission()
                {
                    data = _baseMission,
                    Id = _baseMission.Id,
                    UniqueId = _baseMission.UniqueId
                };
            
            case MissionType.AllMainOrders:

                return new AllMainOrdersMission()
                {
                    OrderBoardCompleteTarget = new IdleNumber(1f , NumberDigits.Empty),
                    data = _baseMission,
                    Id = _baseMission.Id,
                    UniqueId = _baseMission.UniqueId
                };
            
            case MissionType.AllIdleUpgrades:

                return new AllIdleUpgradeMissions()
                {
                    data = _baseMission,
                    Id = _baseMission.Id,
                    UniqueId = _baseMission.UniqueId
                };
            
            case MissionType.AllGeneratorsMax:

                return new AllGeneratorMaxMission()
                {
                    data = _baseMission,
                    Id = _baseMission.Id,
                    UniqueId = _baseMission.UniqueId
                };
            
            case MissionType.AllMarketsMax:

                return new AllMarketMax()
                {
                    data = _baseMission,
                    Id = _baseMission.Id,
                    UniqueId = _baseMission.UniqueId
                };
            
            case MissionType.MarketAutomate:

                return new MarketAutomateMission()
                {
                    data = _baseMission,
                    Id = _baseMission.Id,
                    UniqueId = _baseMission.UniqueId
                };
            
            case MissionType.BuildingUpgrade:

                return new BuildingLevelUpMission()
                {
                    data = _baseMission,
                    Id = _baseMission.Id,
                    UniqueId = _baseMission.UniqueId
                };
            
            case MissionType.MarketUnlock:

                return new MarketUnlockMission()
                {
                    data = _baseMission,
                    Id = _baseMission.Id,
                    UniqueId = _baseMission.UniqueId
                };

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    #endregion
    
    #region SaveLoad
    
    private void LoadSaveData()
    {
        var saveDatas = DataService.Instance.GetData<Dictionary<int, MissionSaveData>>(DataType.MISSION_SAVE_DATA);
        
        var leveledSaveData =
            DataService.Instance.GetData<Dictionary<int, LeveledMissionRewardSaveData>>(DataType
                .MISSION_LEVELED_REWARD_SAVE_DATA);
        
        
        MissionSaveDic.Clear();

        foreach (var save in saveDatas)
        {
            MissionSaveDic.TryAdd(save.Key, save.Value);
        }
        
        MissionRewardSaveDic.Clear();

        foreach (var saveData in leveledSaveData)
        {
            MissionRewardSaveDic.TryAdd(saveData.Key, saveData.Value);
        }

        Loaded = true;
    }

    public void SetSaveData(int uniqueId , MissionSaveData saveData)
    {
        if(!Loaded)
            LoadSaveData();
        
        if (!MissionSaveDic.ContainsKey(uniqueId))
        {
            MissionSaveDic.Add(uniqueId , saveData);
        }
        else
        {
            MissionSaveDic[uniqueId] = saveData;
        }

        SaveData();
    }
    
    public void SetSaveData(int id , LeveledMissionRewardSaveData saveData)
    {
        if(!Loaded)
            LoadSaveData();

        MissionRewardSaveDic.TryAdd(id, saveData);

        SaveData();
    }

    public MissionSaveData GetSaveDataByUniqueId(int uniqueId)
    {
        if(!Loaded)
            LoadSaveData();
        
        if (MissionSaveDic.ContainsKey(uniqueId))
            return MissionSaveDic[uniqueId];
        else
            return new MissionSaveData();
    }
    
    private void SaveData()
    {
        if(!Loaded)
            LoadSaveData();
        
        DataService.Instance.SetData(DataType.MISSION_SAVE_DATA , MissionSaveDic , true);
        DataService.Instance.SetData(DataType.MISSION_LEVELED_REWARD_SAVE_DATA , MissionRewardSaveDic , true);
    }

    private int GetCurrentSetNumber()
    {
        var stateData = DataService.Instance.GetData<Dictionary<StateType, int>>(DataType.STATE);

        if (!stateData.ContainsKey(StateType.MissionCurrentLevelSet))
            return 0;
        return stateData[StateType.MissionCurrentLevelSet];
    }

    public void SaveCurrentSetNumber(int setNumber)
    {
        var stateData = DataService.Instance.GetData<Dictionary<StateType, int>>(DataType.STATE);

        if (!stateData.ContainsKey(StateType.MissionCurrentLevelSet))
            stateData.Add(StateType.MissionCurrentLevelSet, setNumber);
        else
            stateData[StateType.MissionCurrentLevelSet] = setNumber;
        
        DataService.Instance.SetData(DataType.STATE, stateData);
    }
    
    #endregion

    private bool enabledCheat;
    public void EnableMissionCompleteCheat()
    {
        totalCompletedMissionCount = totalMissionCount;
        enabledCheat = true;
        OnMissionCheatEnabled?.Invoke();
        OnMissionsRefreshed?.Invoke();
        OnMissionCompleted?.Invoke();
    }

    public void EnableMissionCompleteCurrentLevel()
    {
        totalCompletedMissionCount = totalMissionCount;
        OnMissionCheatEnabledCurrentLevel?.Invoke();
        OnMissionsRefreshed?.Invoke();
    }
}

public class MissionSaveData
{
    public IdleNumber CurrentAmount = new IdleNumber(0f, NumberDigits.Empty);
    public bool ActiveNow = false;
    public bool CompletedBefore = false;
    public int AttemptNumber = 0;
}

public class LeveledMissionRewardSaveData
{
    public int id = 0;
    public bool isGiven = false;
}
