using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class VillageManager : Singleton<VillageManager> , IExtraObjectUpgradeManager , IExtraGeneralUpgradeManager
{
    public readonly IdleNumber WoodUpgradePrice = new IdleNumber(2f, NumberDigits.Empty);
    
    private const int VILLAGE_CITY_ID = 99;

    public UnityEvent OnVillageUpgraderArrived = new UnityEvent();
    public UnityEvent<int> OnHouseUpgraded = new UnityEvent<int>();
    public UnityEvent<bool> OnVillageEntered = new UnityEvent<bool>();
    public UnityEvent<float> OnVillageHomeRewardClaimable = new UnityEvent<float>();
    public UnityEvent OnVillageHomeRewardClaimed = new UnityEvent();

    private const string CollectionPath = "Configurations/VillageCollection";
    private VillageCollection _villageCollection;

    private Dictionary<int, IdleNumber> MovingUpgraderData = new Dictionary<int, IdleNumber>();
    
    private Dictionary<int, VillageHomeData> VillageHomeDataDic = new Dictionary<int, VillageHomeData>();
    
    private Dictionary<int, VillageHomeSaveData> VillageHomeSaveDataDic = new Dictionary<int, VillageHomeSaveData>();
    private bool loaded;

    private List<(int HomeID, int Level, IdleNumber CurrentProgress)> CurrentProgressList =
        new List<(int HomeID, int Level, IdleNumber CurrentProgress)>();

    //public List<VillageHomeSaveData> TestList = new List<VillageHomeSaveData>();
    //public List<VillageHomeSaveData> TestList2 = new List<VillageHomeSaveData>();

    private List<VillageHome> AllHomeList = new List<VillageHome>();

    private Dictionary<PoolType, VillageUpgradeData> VillageUpgradeDatasDic =
        new Dictionary<PoolType, VillageUpgradeData>();

    private List<(PoolType, VillageUpgradeData)> VillageUpgradeDatasList = new List<(PoolType, VillageUpgradeData)>();

    private CameraController CameraController => cameraController ?? Camera.main.GetComponent<CameraController>();
    private CameraController cameraController;

    private int homeRewardInterval = 14400;
    private int ManagerRewardInterval = 300;

    private int MovingWoodCount = 0;
    
    private Transform UpgradeSpawnPoint
    {
        get
        {
            if (GameUtility.IsNull(_UpgradeSpawnPoint))
            {
                _UpgradeSpawnPoint = GameObject.Find("VillagerUpgraderSpawnPos").transform;
            }

            return _UpgradeSpawnPoint;
        }
    }

    private Transform _UpgradeSpawnPoint;

    [ContextMenu("TestSort")]
    public void TestSortList()
    {
        /*TestList2.Clear();

        foreach (var test in TestList)
        {
            TestList2.Add(test);
        }
        
        var sortedList  = TestList.OrderBy(a => a.Level).ThenBy(b => b.HomeID).ToList();
        TestList2 = sortedList;
        //TestList.Sort((l1 , l2) => l1.Level.CompareTo(l2.Level));
        //TestList.Sort((h1 , h2) => h1.HomeID.CompareTo(h2.HomeID));
        //BotActionTypesDatas.Sort((u1 ,u2) => u2.Priority.CompareTo(u1.Priority));*/
    }
    
    private void Awake()
    {
        LoadCollection();
        LoadSaveData();
        GetHomeDataFromCollection();
    }

    private void Start()
    {
        homeRewardInterval = RemoteConfigManager.Instance.GetIntConfig("VillageHomeRewardInterval", 14400);
        ManagerRewardInterval = RemoteConfigManager.Instance.GetIntConfig("ManagerVillageClickRewardInterval", 300);

        homeRewardInterval = ConfigurationService.Configurations.VillageHomeRewardIntervalBySecond;
    }

    private void LoadCollection()
    {
        _villageCollection = VillageCollection.LoadCollection(CollectionPath);
    }

    private void GetHomeDataFromCollection()
    {
        foreach (var homeData in _villageCollection.VillageHomeDatas)
        {
            VillageHomeDataDic.TryAdd(homeData.HomeID, homeData);
        }
    }

    private int PickUpgradeableHome()
    {
        CurrentProgressList.Clear();

        foreach (var homeData in VillageHomeDataDic)
        {
            CurrentProgressList.Add((homeData.Key , 0 , new IdleNumber()));
        }

        foreach (var homeSaveData in VillageHomeSaveDataDic)
        {
            foreach (var currentProgress in CurrentProgressList)
            {
                if (currentProgress.HomeID == homeSaveData.Key)
                {
                    var data = currentProgress;
                    CurrentProgressList.Remove(currentProgress);
                    CurrentProgressList.Add((data.HomeID , homeSaveData.Value.Level , homeSaveData.Value.UpgradeProgress));
                    break;
                }
            }
        }
        
        var sortedList  = CurrentProgressList.OrderBy(a => a.Level).ThenBy(b => b.HomeID).ToList();
        
        foreach (var homeListed in sortedList)
        {
            if (MovingUpgraderData.ContainsKey(homeListed.HomeID))
            {
                if (GetRemainUpgradeAmountForNextLevelByHomeID(homeListed.HomeID) >
                    MovingUpgraderData[homeListed.HomeID])
                    return homeListed.HomeID;
            }
            else
            {
                return homeListed.HomeID;
            }
        }
        
        return sortedList[0].HomeID;
        return 0;
    }
    
    #region PublicFunc

    private IdleNumber HomeUpgradedCurrent;
    public void HomeUpgraded(int homeID , IdleNumber upgradeAmount)
    {
        MovingUpgraderData[homeID] -= upgradeAmount;
        
        HomeUpgradedCurrent = GetCurrentHouseUpgradeByHomeID(homeID);
        HomeUpgradedCurrent += upgradeAmount;

        isRefreshNeed = true;

        VillageHomeSaveDataDic.TryGetValue(homeID, out VillageHomeSaveData saveData);
        if (saveData == null)
        {
            saveData = new VillageHomeSaveData()
            {
                HomeID = homeID,
                UpgradeProgress = new IdleNumber(0f , NumberDigits.Empty),
                Level = 0
            };
            
            VillageHomeSaveDataDic.TryAdd(homeID, saveData);
        }
        
        saveData.UpgradeProgress = HomeUpgradedCurrent;

        if (HomeUpgradedCurrent >= GetHomeDataByID(homeID, saveData.Level).UpgradePrice)
        {
            saveData.Level++;
            saveData.UpgradeProgress = new IdleNumber();
            VillageHomeSaveDataDic[homeID] = saveData;
            SaveData();
            OnHouseUpgraded?.Invoke(homeID);
            TryShowUpgradeInfoPanel();
        }
        else
        {
            SaveData();
        }
    }
    
    public void AddMeToHomeList(VillageHome home)
    {
        AllHomeList.Add(home);
    }
    
    
    private IdleNumber SelectedIdleNumber;
    [ContextMenu("SpawnUpgrader")]
    public void SpawnUpgrader()
    {
        var SelectedHome = PickUpgradeableHome();
        SelectedIdleNumber = WoodUpgradePrice;
        var upgrader = PoolingSystem.Instance.Create<VillageHomeUpgrader>(PoolType.VillageHomeUpgrader);
        MovingWoodCount++;
        upgrader.InitiliazeUpgrader(VillageUpCanvas.GetUpButton().position , GetHomePositionByHomeID(SelectedHome).position + Vector3.one * Random.Range(-1f,1f) , () =>
        {
            HomeUpgraded(SelectedHome , WoodUpgradePrice);
            AllHomeList[SelectedHome].PlayUpgradeAnim();
            OnVillageUpgraderArrived?.Invoke();
            ParticleManager.Instance.PlayParticle(PoolType.UpParticleVillage, GetHomePositionByHomeID(SelectedHome).position);
            MovingWoodCount--;
            IdleExchangeService.DoExchange(CurrencyType.VillageCoin, -WoodUpgradePrice, out _,
                "VillageUpgrade");
        } );
        
        HapticManager.Instance.Play(HapticTypes.LightImpact);

        if (!MovingUpgraderData.ContainsKey(SelectedHome))
            MovingUpgraderData.Add(SelectedHome, SelectedIdleNumber);
        else
            MovingUpgraderData[SelectedHome] += SelectedIdleNumber;
    }

    #endregion

    #region GetFuncs

    private Transform GetHomePositionByHomeID(int homeID)
    {
        foreach (var home in AllHomeList)
        {
            if (home.GetHomeID() == homeID)
                return home.transform;
        }

        return null;
    }

    public (int level, IdleNumber UpgradeProgress , IdleNumber requiredAmountForUpgrade) GetHomeProgressDataByID(int homeID)
    {
        VillageHomeSaveDataDic.TryGetValue(homeID, out VillageHomeSaveData data);
        if (data != null)
        {
            var homeData = GetVillageHomeDataByID(homeID);
            var required = homeData.HomeUpgradesRequirements[data.Level];
            return (data.Level, data.UpgradeProgress , required);
        }
        else
        {
            var homeData = GetVillageHomeDataByID(homeID);
            var required = homeData.HomeUpgradesRequirements[0];
            return (0, new IdleNumber() , required);
        }
    }

    public VillageHomeSaveData GetHomeSaveDataByID(int homeID)
    {
        VillageHomeSaveDataDic.TryGetValue(homeID, out VillageHomeSaveData data);
        return data;
    }

    private GameObject getHomeDataByIDGameObject;
    private IdleNumber getHomeDataByIDidIdleNumber;
    public (GameObject gameObject, IdleNumber UpgradePrice) GetHomeDataByID(int homeID , int level)
    {
        VillageHomeDataDic.TryGetValue(homeID, out VillageHomeData data);
        if (data == null)
            return (null, new IdleNumber());

        if (level > data.HomeUpgradeBuildings.Count - 1)
            getHomeDataByIDGameObject = data.HomeUpgradeBuildings.Last();
        else
            getHomeDataByIDGameObject = data.HomeUpgradeBuildings[level];

        if (level > data.HomeUpgradesRequirements.Count - 1)
            getHomeDataByIDidIdleNumber = data.HomeUpgradesRequirements.Last();
        else
            getHomeDataByIDidIdleNumber = data.HomeUpgradesRequirements[level];

        return (getHomeDataByIDGameObject, getHomeDataByIDidIdleNumber);
    }

    private IdleNumber GetCurrentHouseUpgradeByHomeID(int homeID)
    {
        var level = GetHomeProgressDataByID(homeID).level;
        var progress = GetHomeProgressDataByID(homeID).UpgradeProgress;
        return progress;
    }

    private IdleNumber GetRemainUpgradeAmountForNextLevelByHomeID(int homeID)
    {
        var level = GetHomeProgressDataByID(homeID).level;
        var progress = GetHomeProgressDataByID(homeID).UpgradeProgress;
        var requirementForNextLevel = GetHomeDataByID(homeID, level).UpgradePrice;
        return requirementForNextLevel - progress;
    }

    private VillageHomeData GetVillageHomeDataByID(int homeID)
    {
        VillageHomeDataDic.TryGetValue(homeID, out VillageHomeData data);
        return data;
    }

    public (int HomeLevel, string HomeName, string SkillName, string ExtraInfoText, string SkillAmount, float RewardRemainTimer , float RewardInterval , PoolType rewardType , IdleNumber rewardAmount)
        GetMiniInfoPanelDataByHomeID(int homeID)
    {
        var data = GetVillageHomeDataByID(homeID);
        var level = GetHomeProgressDataByID(homeID).level;
        var skillName = data.HomeUpgradeSkillName;
        var skillAmount = GetSkillBenefitInfo(data, level);
        
        //todo daha sonra düzeltilecek
        var @base = GPSManager.Instance.GetVillageHomeRewardCalculationByPoolType(data.ReleatedHomeSource[ConfigurationService.Configurations.lastCityID]);
        var rewardAmount = @base * ConfigurationService.Configurations.VillageHomeRewardMultiply;
        
        //For which city reward
        var stateData = DataService.Instance.GetData<Dictionary<StateType, int>>(DataType.STATE);
        int activeCityId = stateData[PlayerModeManager.Instance.GetActiveCityId()];
        
        return (GetHomeProgressDataByID(homeID).level, data.HomeName, skillName.ToString(), data.HomeUpgradeSkillInfo, skillAmount, GetHomeRewardRemainTime(homeID) , homeRewardInterval , data.ReleatedHomeSource[activeCityId % 3] , rewardAmount);
    }

    

    public int GetHomeRewardRemainTime(int homeID)
    {
        var data = GetHomeSaveDataByID(homeID);
        if (data != null)
        {
            var lastClaimDate = data.LastRewardClaimDate;
            return homeRewardInterval - (Timestamp.GetByDate(DateTime.Now) - lastClaimDate);
        }

        return 0;
    }

    public void HomeRewardClaimed(int homeID)
    {
        var data = GetHomeSaveDataByID(homeID);
        if (data != null)
        {
            data.LastRewardClaimDate = Timestamp.GetByDate(DateTime.Now);
            VillageHomeSaveDataDic[homeID] = data;
        }
        else
        {
            VillageHomeSaveData saveData = new VillageHomeSaveData();
            saveData.HomeID = homeID;
            saveData.LastRewardClaimDate = Timestamp.GetByDate(DateTime.Now);
            saveData.UpgradeProgress = new IdleNumber();
            VillageHomeSaveDataDic.TryAdd(homeID, saveData);
        }
        SaveData();
        OnVillageHomeRewardClaimed?.Invoke();
    }

    public void ManagerClickRewardClaimed(int homeID)
    {
        var data = GetHomeSaveDataByID(homeID);
        if (data != null)
        {
            data.LastManagerClickClaimDate = Timestamp.GetByDate(DateTime.Now);
            VillageHomeSaveDataDic[homeID] = data;
        }
        else
        {
            VillageHomeSaveData saveData = new VillageHomeSaveData();
            saveData.HomeID = homeID;
            saveData.LastManagerClickClaimDate = Timestamp.GetByDate(DateTime.Now);
            saveData.UpgradeProgress = new IdleNumber();
            VillageHomeSaveDataDic.TryAdd(homeID, saveData);
        }
        SaveData();
    }

    private List<NFProductContainer> allContainers = new List<NFProductContainer>();
    public (bool canManagerGiveReward , PoolType rewardType , IdleNumber rewardAmount) CheckIfManagerCanGiveReward(int homeID)
    {
        allContainers.Clear();
        PoolType releatedPoolType = PoolType.Undefined;
        var data = GetHomeSaveDataByID(homeID);
        if (data != null)
        {
            var lastClaimDate = data.LastManagerClickClaimDate;
            if (ManagerRewardInterval - (Timestamp.GetByDate(DateTime.Now) - lastClaimDate) < 0)
            {
                allContainers = InteractionManager.Instance.GetAllAvailableInteractables<NFProductContainer>(InteractableType.NFProductContainer);

                foreach (var releatedSources in GetVillageHomeDataByID(homeID).ReleatedHomeSource)
                {
                    foreach (var container in allContainers)
                    {
                        if (container.GetObjectType() == releatedSources)
                            releatedPoolType = releatedSources;
                    }
                }

                if (releatedPoolType != PoolType.Undefined)
                {
                    var amount = GPSManager.Instance.GetVillageHomeRewardCalculationByPoolType(releatedPoolType) * 60;
                    return (true, releatedPoolType, amount);
                }
                else
                {
                    return (false, PoolType.All, new IdleNumber());
                }
            }
        }
        else
        {
            allContainers = InteractionManager.Instance.GetAllAvailableInteractables<NFProductContainer>(InteractableType.NFProductContainer);

            foreach (var releatedSources in GetVillageHomeDataByID(homeID).ReleatedHomeSource)
            {
                foreach (var container in allContainers)
                {
                    if (container.GetObjectType() == releatedSources)
                        releatedPoolType = releatedSources;
                }
            }

            if (releatedPoolType != PoolType.Undefined)
            {
                var amount = GPSManager.Instance.GetVillageHomeRewardCalculationByPoolType(releatedPoolType) * 60;
                return (true, releatedPoolType, amount);
            }
            else
            {
                return (false, PoolType.All, new IdleNumber());
            }
        }

        return (false, PoolType.All, new IdleNumber());
    }

    public bool CheckAnyNoticeableEventAtVillage()
    {
        var selectedHome = PickUpgradeableHome();
        var data = GetHomeProgressDataByID(selectedHome);
        if (data.requiredAmountForUpgrade - data.UpgradeProgress < GetAvailableVillageCoinCount() )
        {
            return true;
        }

        foreach (var homeData in VillageHomeDataDic)
        {
            if (GetHomeProgressDataByID(homeData.Key).level > 0 && GetHomeRewardRemainTime(homeData.Key) <= 0)
            {
                return true;
            }
        }

        return false;
        //GetHomeRewardRemainTime
    }

    #endregion

    #region CalCulationsAndOthers
    
    private string GetSkillBenefitInfo(VillageHomeData data , int level)
    {
        if (data.UpgradeDatas[level].UpgradeType == IdleUpgradeType.GeneralUpgrade)
        {
            if (data.UpgradeDatas[level].GeneralUpgradeType == GeneralUpgradeType.RewardedBoostDuration)
            {
                var defaultValue = (int)RewardedBoostUpgradeManager.BOOST_DURATION;
                var extraOne = defaultValue * data.UpgradeDatas[level].Multiply;
                extraOne += data.UpgradeDatas[level].Additon;
                var difference = extraOne - defaultValue;
                difference /= 60;
                return $"+ {difference}min";
            }else if (data.UpgradeDatas[level].GeneralUpgradeType == GeneralUpgradeType.IdleTimeIncrease)
            {
                //var finalCalculation = data.UpgradeDatas[level].Additon / 14400 * 100f;
                var finalCalculation = data.UpgradeDatas[level].Additon / 60;
                return $"+ {finalCalculation}min";
            }
        }
        
        return GetSkillAmount(data.UpgradeDatas[level].GeneralUpgradeType,
            data.UpgradeDatas[level].Additon ,  data.UpgradeDatas[level].Multiply);
    }

    private string GetSkillAmount(GeneralUpgradeType generalUpgradeType , float addition , float multiply)
    {
        /*if (generalUpgradeType == GeneralUpgradeType.IdleTimeIncrease)
        {
            var finalCalculation = addition / 14400 * 100f;
            return finalCalculation.ToString("F1") + "%";
        }*/
        
        var finalValue = 100f * multiply + addition;

        /*
        if (generalUpgradeType == GeneralUpgradeType.TruckTime)
        {
            return 
        }*/
        if (finalValue - 100f > 0f)
        {
            return "+" +(finalValue - 100f).ToString("F1") + "%";
        }
        else
        {
            return (finalValue - 100f).ToString("F1") + "%";
        }
        
    }

    #endregion

    #region SaveLoad
    private void LoadSaveData()
    {
        var saveData = DataService.Instance.GetData<Dictionary<int, VillageHomeSaveData>>(DataType.VILLAGE_HOME_SAVE_DATA);
        
        VillageHomeSaveDataDic.Clear();

        foreach (var save in saveData)
        {
            VillageHomeSaveDataDic.TryAdd(save.Key, save.Value);
        }

        loaded = true;
    }

    private void SaveData()
    {
        if(!loaded)
            LoadSaveData();
        
        DataService.Instance.SetData(DataType.VILLAGE_HOME_SAVE_DATA , VillageHomeSaveDataDic);
    }
    #endregion

    #region EnterExitVillage
    
    [ContextMenu("Enter Village")]
    public void EnterVillage()
    {
        PanelManager.Instance.HideAllPanel();
        PanelManager.Instance.Show(PopupType.EnterVillagePanel , new VillageLoadPanelData(true));
        CoroutineDispatcher.StartCoroutine(() =>
        {
            LevelManager.Instance.EnterVillage();
            OnVillageEntered?.Invoke(true);
            //PlayerModeManager.Instance.ChangeMode(PlayerMode.Dawn);
            GetInventory();
            LevelManager.Instance.BeforeCityChanged?.Invoke();
            SetInventory();
            EnableUpCanvas();
            TryShowUpgradeInfoPanel();
            MovingWoodCount = 0;
        }, 0.5f);
    }
    
    [ContextMenu("Exit Village")]
    public void ExitVillage()
    {
        PanelManager.Instance.Show(PopupType.EnterVillagePanel , new VillageLoadPanelData(false));
        CoroutineDispatcher.StartCoroutine(() =>
        {
            LevelManager.Instance.ExitVillage();
            OnVillageEntered?.Invoke(false);
            //PanelManager.Instance.Show(PopupType.GamePlayPanel , new PanelData());
            //PlayerModeManager.Instance.ChangeMode(PlayerMode.Day);
            AllHomeList.Clear();
            GetInventory();
            //LevelManager.Instance.BeforeCityChanged?.Invoke();
            SetInventory();
            CameraController.StopFollowVillage();
            MiniInfoVillagePanel.HideCanvas();
            DisableUpCanvas();
            MiniUpgradeVillagePanel.HideCanvas();
        }, 0.5f);
    }
    
    #endregion

    #region GetUpgradedValues

    private bool isRefreshNeed = true;

    public float GetUpgradedValue(string objectId, ObjectDataType objectDataType, float baseValue,
        PoolType poolType = PoolType.Undefined, bool isNextLevel = false)
    {

        if (objectDataType != ObjectDataType.MarketInstaSell)
        {
            return baseValue;
        }
        
        CreateUpgradeValueDictionaries();
        if (poolType == PoolType.Undefined)
            poolType = GetPooltypeByInteractionID(objectId);
         
        foreach (var villageHomeData in VillageHomeDataDic)
        {
            foreach (var upgradeData in villageHomeData.Value.UpgradeDatas)
            {
                if (upgradeData.EffectLevel == GetHomeProgressDataByID(villageHomeData.Value.HomeID).level)
                {
                    if (objectDataType == ObjectDataType.MarketInstaSell)
                    {
                        if (upgradeData.UpgradeType == IdleUpgradeType.ObjectUpgrade && upgradeData.ObjectDataType == ObjectDataType.MarketInstaSell && objectId.StartsWith("M"))
                        {
                            var change = upgradeData.Additon;
                            var randomNumber = Random.Range(0, 100);

                            if (change > randomNumber)
                                return baseValue *= -1f;
                        }
                    }
                }
            }
        }
        
        /*foreach (var upgrade in VillageUpgradeDatasList)
        {
            if (upgrade.Item2.UpgradeType == IdleUpgradeType.ObjectUpgrade && poolType == upgrade.Item1 && objectDataType == upgrade.Item2.ObjectDataType)
            {
                baseValue *= upgrade.Item2.Multiply;
                baseValue += upgrade.Item2.Additon;
            }
        }*/

        return baseValue;
    }

    public IdleNumber GetUpgradedValue(string objectId, ObjectDataType objectDataType, IdleNumber baseValue,
        PoolType poolType = PoolType.Undefined, bool isNextLevel = false)
    {
        CreateUpgradeValueDictionaries();
        /*VillageUpgradeDatasDic.Clear();
        VillageUpgradeDatasList.Clear();

        foreach (var villageHomeData in VillageHomeDataDic)
        {
            foreach (var upgradeData in villageHomeData.Value.UpgradeDatas)
            {
                if (upgradeData.EffectLevel <= GetHomeProgressDataByID(villageHomeData.Value.HomeID).level)
                {
                    foreach (var poolTypes in villageHomeData.Value.ReleatedHomeSource)
                    {
                        //VillageUpgradeDatasDic.Add(poolTypes , upgradeData);
                        VillageUpgradeDatasList.Add((poolTypes , upgradeData));
                    }
                }
            }
        }*/

        if (poolType == PoolType.Undefined)
            poolType = GetPooltypeByInteractionID(objectId);
            
        
        foreach (var upgrade in VillageUpgradeDatasList)
        {
            if (upgrade.Item2.UpgradeType == IdleUpgradeType.ObjectUpgrade && poolType == upgrade.Item1 && objectDataType == upgrade.Item2.ObjectDataType)
            {
                baseValue *= upgrade.Item2.Multiply;
                baseValue += upgrade.Item2.Additon;
            }
        }

        return baseValue;
    }

    private void CreateUpgradeValueDictionaries()
    {
        if(!isRefreshNeed)
            return;

        isRefreshNeed = false;
        
        VillageUpgradeDatasDic.Clear();
        VillageUpgradeDatasList.Clear();

        foreach (var villageHomeData in VillageHomeDataDic)
        {
            foreach (var upgradeData in villageHomeData.Value.UpgradeDatas)
            {
                if (upgradeData.EffectLevel <= GetHomeProgressDataByID(villageHomeData.Value.HomeID).level)
                {
                    foreach (var poolTypes in villageHomeData.Value.ReleatedHomeSource)
                    {
                        //VillageUpgradeDatasDic.Add(poolTypes , upgradeData);
                        VillageUpgradeDatasList.Add((poolTypes , upgradeData));
                    }
                }
            }
        }
    }

    private PoolType GetPooltypeByInteractionID(string id)
    {
        var interactable = InteractionManager.Instance.GetInteractableById(id);
        if (interactable == null)
            return PoolType.Undefined;

        var containerr = interactable.GetComponent<NFProductContainer>();
        if (containerr == null)
            return PoolType.Undefined;

        return containerr.GetObjectType();
        
        var containers =
            InteractionManager.Instance.GetAllAvailableInteractables<NFProductContainer>(InteractableType
                .NFProductContainer);

        foreach (var container in containers)
        {
            if (container.ID == id)
                return container.GetObjectType();
        }
        
        var markets =
            InteractionManager.Instance.GetAllAvailableInteractables<NFMarketPlace>(InteractableType
                .NFMarketPlace);
        
        foreach (var market in markets)
        {
            if (market.ID == id)
                return market.GetObjectType();
        }

        return PoolType.Undefined;
    }

    public float GetUpgradedValue(GeneralUpgradeType generalSettingType, float baseValue)
    {
        VillageUpgradeDatasDic.Clear();
        VillageUpgradeDatasList.Clear();

        foreach (var villageHomeData in VillageHomeDataDic)
        {
            foreach (var upgradeData in villageHomeData.Value.UpgradeDatas)
            {
                //var requested = upgradeData.EffectLevel;
                //var current = GetHomeProgressDataByID(villageHomeData.Value.HomeID).level;
                if (upgradeData.EffectLevel == GetHomeProgressDataByID(villageHomeData.Value.HomeID).level)
                {
                    if (upgradeData.UpgradeType == IdleUpgradeType.GeneralUpgrade && upgradeData.GeneralUpgradeType == generalSettingType)
                    {
                        baseValue *= upgradeData.Multiply;
                        baseValue += upgradeData.Additon;
                    }
                }
            }
        }

        return baseValue;
    }

    public IdleNumber GetUpgradedValue(GeneralUpgradeType generalSettingType, IdleNumber baseValue)
    {
        VillageUpgradeDatasDic.Clear();
        VillageUpgradeDatasList.Clear();

        foreach (var villageHomeData in VillageHomeDataDic)
        {
            foreach (var upgradeData in villageHomeData.Value.UpgradeDatas)
            {
                if (upgradeData.EffectLevel <= GetHomeProgressDataByID(villageHomeData.Value.HomeID).level)
                {
                    if (upgradeData.UpgradeType == IdleUpgradeType.GeneralUpgrade && upgradeData.GeneralUpgradeType == generalSettingType)
                    {
                        baseValue *= upgradeData.Multiply;
                        baseValue += upgradeData.Additon;
                    }
                }
            }
        }

        return baseValue;
    }
    
    #endregion

    #region SceneBasedFunctions

    public UnityEvent<bool> OnZoomToHome = new UnityEvent<bool>();

    public void ZoomToHome(Transform home , float zoomAmount , float ZoffsetAmount)
    {
        OnZoomToHome?.Invoke(true);
        VillageUpCanvas.HideCanvas();
        CameraController.StarFollowViialgeHome(home.transform , overridedSize: zoomAmount , zOffset: ZoffsetAmount , endAction: () => {});
        MiniUpgradeVillagePanel.HideCanvas();
    }

    public void ZoomOutToHome()
    {
        OnZoomToHome?.Invoke(false);
        CameraController.StopFollowVillage();
        EnableUpCanvas();
        TryShowUpgradeInfoPanel();
    }

    private void EnableUpCanvas()
    {
        VillageUpCanvas.ShowCanvas();
    }

    private void DisableUpCanvas()
    {
        VillageUpCanvas.HideCanvas();
    }

    private List<(int HomeID, int Level, IdleNumber CurrentProgress)> CurrentProgressList2 =
        new List<(int HomeID, int Level, IdleNumber CurrentProgress)>();
    private void TryShowUpgradeInfoPanel()
    {
        CurrentProgressList2.Clear();
        foreach (var homeData in VillageHomeDataDic)
        {
            CurrentProgressList2.Add((homeData.Key , 0 , new IdleNumber()));
        }
        foreach (var homeSaveData in VillageHomeSaveDataDic)
        {
            foreach (var currentProgress in CurrentProgressList2)
            {
                if (currentProgress.HomeID == homeSaveData.Key)
                {
                    var data = currentProgress;
                    CurrentProgressList2.Remove(currentProgress);
                    CurrentProgressList2.Add((data.HomeID , homeSaveData.Value.Level , homeSaveData.Value.UpgradeProgress));
                    break;
                }
            }
        }
        var sortedList  = CurrentProgressList2.OrderBy(a => a.Level).ThenBy(b => b.HomeID).ToList();
        
        MiniUpgradeVillagePanel.ShowCanvas(GetHomePositionByHomeID(sortedList[0].HomeID) , sortedList[0].HomeID ,  GetVillageHomeDataByID(sortedList[0].HomeID).HomeName);
    }

    private List<string> villagerAvailableConversations = new List<string>();
    public string[] GetVillagerTalks(bool isClickTalk)
    {
        villagerAvailableConversations.Clear();

        foreach (var data in _villageCollection.VillagersConversationDatas)
        {
            if (data.isClickConversation == isClickTalk && data.allHousesUnlocked == isAllHouseUnlocked())
            {
                foreach (var conversation in data.conversations)
                {
                    villagerAvailableConversations.Add(conversation);
                }
            }
        }

        if (villagerAvailableConversations.Count < 1)
            return null;

        string[] talks = new[]
            { villagerAvailableConversations[Random.Range(0, villagerAvailableConversations.Count)] };
        return talks;
    }

    private bool isAllHouseUnlocked()
    {
        if (VillageHomeSaveDataDic.ContainsKey(6) && VillageHomeSaveDataDic[6].Level > 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public IdleNumber GetAvailableVillageCoinCount()
    {
        var totalCoin = IdleExchangeService.GetIdleValue(CurrencyType.VillageCoin);

        var availableOne = totalCoin - MovingWoodCount * WoodUpgradePrice;
        
        return availableOne;
    }

    #endregion

    #region BugFixAreaFixingLater

    private Dictionary<PoolType , IdleNumber> InventoryHolder = new Dictionary<PoolType, IdleNumber>();

    private void GetInventory()
    {
        InventoryHolder.Clear();

        var realInventory = NFInventoryManager.Instance.GetAllItems();

        foreach (var data in realInventory)
        {
            InventoryHolder.Add(data.Key , data.Value);
        }
    }

    private void SetInventory()
    {
        foreach (var data in InventoryHolder)
        {
            NFInventoryManager.Instance.AddItemToInventory(data.Key , data.Value);
        }
    }

    #endregion
}

[Serializable]
public class VillageHomeSaveData
{
    public int HomeID;
    public int Level;
    public IdleNumber UpgradeProgress = new IdleNumber();
    public int LastRewardClaimDate;
    public int LastManagerClickClaimDate;
}
