using System;
using System.Collections;
using System.Collections.Generic;
using _Game.Scripts.HouseCoffee;
using _Game.Scripts.Systems.StarUpgradeSystem;
using _Game.Scripts.Systems.WeeklyEventSystem;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;

public class StickerManager : Singleton<StickerManager>, IExtraObjectUpgradeManager
{
    public int ReAttachmentCooldown => stickerCollection.ReAttachmentCooldown;
    
    private const string CollectionPath = "Configurations/ManagerCollection";
    public StickerCollection StickerCollection
    {
        get
        {
            if (stickerCollection == null)
                LoadCollection();

            return stickerCollection;
        }   
    }

    private StickerCollection stickerCollection;

    private Dictionary<string, Sticker> UsingStickersObjectToSticker
    {
        get
        {
            if (EventManager.Instance.InEvent) return eventUsingStickersObjectToSticker;
           
            PlayerMode currentMode = PlayerModeManager.Instance.GetActiveMode();
            if (!usingStickersObjectToSticker.ContainsKey(currentMode))
            {
                usingStickersObjectToSticker[currentMode] = new Dictionary<string, Sticker>();
            }

            return usingStickersObjectToSticker[currentMode];
        }
    }
    
    private Dictionary<Sticker, string> UsingStickersStickerToObject
    {
        get
        {
            if (EventManager.Instance.InEvent)
            {
  //              Debug.Log("### In Event");
                return eventUsingStickersStickerToObject;
            }
           
            PlayerMode currentMode = PlayerModeManager.Instance.GetActiveMode();
            if (!usingStickersStickerToObject.ContainsKey(currentMode))
            {
                usingStickersStickerToObject[currentMode] = new Dictionary<Sticker, string>();
            }
//            Debug.Log("### Player Mode : " + currentMode);
            return usingStickersStickerToObject[currentMode];
        }
    }

    private Dictionary<PlayerMode, Dictionary<string, Sticker>> usingStickersObjectToSticker = new Dictionary<PlayerMode, Dictionary<string, Sticker>>();
    private Dictionary<PlayerMode, Dictionary<Sticker, string>> usingStickersStickerToObject = new Dictionary<PlayerMode, Dictionary<Sticker, string>>();
    private Dictionary<string, Sticker> eventUsingStickersObjectToSticker = new Dictionary<string, Sticker>();
    private Dictionary<Sticker, string> eventUsingStickersStickerToObject = new Dictionary<Sticker, string>();
    private Dictionary<RarityType, StickerBoostData> StickerBoostDatasDic = new Dictionary<RarityType, StickerBoostData>();

    private Dictionary<int, StickerStateData> CurrentModeStickerStateDatas
    {
        get
        {
            if (EventManager.Instance.InEvent) return eventStickerStateDatas;
           
            PlayerMode currentMode = PlayerModeManager.Instance.GetActiveMode();
            return playerModeStickerStateDatas[currentMode];
        }   
    }
    
    private readonly Dictionary<PlayerMode, Dictionary<int, StickerStateData>> playerModeStickerStateDatas = new Dictionary<PlayerMode, Dictionary<int, StickerStateData>>();
    private Dictionary<int, StickerStateData> eventStickerStateDatas = new Dictionary<int, StickerStateData>();
    public UnityEvent<string, Sticker, bool> OnStickerStateChanged = new UnityEvent<string, Sticker, bool>();

    #region Save & Load
    
    private void Awake()
    {
        Load();
        if (RemoteConfigManager.Instance.GetBoolConfig("IsManagerAutoAssign"))
        {
            //TutorialManager.Instance.TutorialComplete(TutorialType.StickerSelect);
        }
        
        //LevelManager.Instance.LevelLoaded.AddListener(CheckAutoAssignStickers);
    }

    private void CheckAutoAssignStickers()
    {
        CheckAndAssignAllManager();
        LevelManager.Instance.LevelLoaded.RemoveListener(CheckAutoAssignStickers);
    }

    public void DisarmAllStickers()
    {
        // return;
        Debug.Log("DisarmAllStickers");
        // var usingStickersDictionary = new Dictionary<Sticker, string>(UsingStickersStickerToObject);
        foreach (var usingStickersToObjectDictionaries in usingStickersObjectToSticker.Values)
        {
            foreach (var usingStickerPair in usingStickersToObjectDictionaries)
            {
                DisarmSticker( usingStickerPair.Value.Id, usingStickerPair.Key, true, true);
            }
        }
    }
    
    public void DisarmCurrentModStickers(bool inEvent)
    {
        Debug.Log("DisarmAllStickers");
        PlayerMode currentMode = PlayerModeManager.Instance.GetActiveMode();
        AssignUsingObjectToStickersDictionaries(out var usingObjectToSticker, out _, false, inEvent, currentMode);
        var usingStickersDictionary = new Dictionary<string, Sticker>(usingObjectToSticker);
        foreach (var usingStickerPair in usingStickersDictionary)
        {
            DisarmSticker(usingStickerPair.Value.Id, usingStickerPair.Key, true, true);
        }
        
        var stickerStatesDic = GetStickerStatesDic(false, inEvent, currentMode);
        foreach (var stickerState  in stickerStatesDic)
        {
            SetState(stickerState.Key, StickerState.Usable, stickerStatesDic);
        }
    }

    private void Load()
    {
        LoadCollection();
        LoadData();
        LoadBoostData();
    }

    private void LoadCollection()
    {
        stickerCollection ??= Resources.Load<StickerCollection>(CollectionPath);
        stickerCollection.Load();
    }

    private void LoadData()
    {
        playerModeStickerStateDatas.Clear();
        eventStickerStateDatas.Clear();
        
        var ownedStickers = GetOwnedStickers();
        var eventStickerSaveDatas = DataService.Instance.GetData<Dictionary<int, StickerSaveData>>(DataType.EVENT_STICKERS_STATES);
        var playerModeStickerSaveDatas = DataService.Instance.GetData<Dictionary<PlayerMode, Dictionary<int, StickerSaveData>>>(DataType.STICKERS_STATES);
        
        foreach (var stickerSaveData in eventStickerSaveDatas)
        {
            eventStickerStateDatas.Add(stickerSaveData.Key, new StickerStateData(stickerSaveData.Value));
        }
        
        foreach (var stickerSaveDatas in playerModeStickerSaveDatas)
        {
            if (!playerModeStickerStateDatas.ContainsKey(stickerSaveDatas.Key))
            {
                playerModeStickerStateDatas[stickerSaveDatas.Key] = new Dictionary<int, StickerStateData>();
            }
            
            foreach (var stickerSaveData in stickerSaveDatas.Value)
            {
                playerModeStickerStateDatas[stickerSaveDatas.Key].Add(stickerSaveData.Key, new StickerStateData(stickerSaveData.Value));
            }
        }
        
        bool shouldSave = false;
        var allPlayerModes = PlayerModeManager.Instance.GetAllPlayerModes();
        foreach (var playerMode in allPlayerModes)
        {
            if(playerModeStickerStateDatas.ContainsKey(playerMode)) continue;
            playerModeStickerStateDatas[playerMode] = new Dictionary<int, StickerStateData>();
        }
        
        foreach (var playerModeStickerStateData in playerModeStickerStateDatas)
        {
            var modeStickerStateData = playerModeStickerStateData.Value;
            shouldSave |= LoadOwnedStickers(ref modeStickerStateData, ownedStickers, false, playerModeStickerStateData.Key);
        }
        
        shouldSave |= LoadOwnedStickers(ref eventStickerStateDatas, ownedStickers, true);
        
        if(shouldSave) SaveData();
    }

    private void LoadBoostData()
    {
        StickerBoostDatasDic.Clear();

        var dataList = stickerCollection.StickerBoostDatasList();

        foreach (var data in dataList)
        {
            StickerBoostDatasDic.TryAdd(data.StickerRarity, data);
        }
    }

    private bool LoadOwnedStickers(ref Dictionary<int, StickerStateData> stickerSaveDatas, List<Sticker> ownedStickers, bool inEvent, PlayerMode? playerMode = null)
    {
        bool shouldSave = false;
        
        foreach (var ownedSticker in ownedStickers)
        {
            if (!stickerSaveDatas.ContainsKey(ownedSticker.Id))
            {
                stickerSaveDatas[ownedSticker.Id] = new StickerStateData(StickerState.Usable, ownedSticker.Duration, ReAttachmentCooldown, ownedSticker.Name);
                shouldSave = true;
            }
            else
            {
                stickerSaveDatas[ownedSticker.Id].Initialize(ownedSticker.Duration, ReAttachmentCooldown, ownedSticker.Name);
                var stickerStateData = stickerSaveDatas[ownedSticker.Id];
                stickerStateData.CheckState(out bool isStateChanged, out _);
                if (!isStateChanged && stickerStateData.StickerState == StickerState.Using)
                {
                    UseSticker(ownedSticker.Id, stickerStateData.UsingObjectId, false, false, inEvent, playerMode);
                }
            }
        }

        return shouldSave;
    }

    private void SaveData()
    {
        var eventStickerSaveDatas = new Dictionary<int, StickerSaveData>();
        
        foreach (var stickerState in eventStickerStateDatas)
        {
            var stateData = stickerState.Value;
            eventStickerSaveDatas.Add(stickerState.Key, new StickerSaveData(stateData.StickerState, stateData.CooldownStartTime, stateData.UsingStartTime, stateData.UsingObjectId));
        }
        
        Dictionary<PlayerMode, Dictionary<int, StickerSaveData>> playerModeStickerSaveDatas = new Dictionary<PlayerMode, Dictionary<int, StickerSaveData>>();

        foreach (var stickerStateDatas in playerModeStickerStateDatas)
        {
            if (!playerModeStickerSaveDatas.ContainsKey(stickerStateDatas.Key))
                playerModeStickerSaveDatas[stickerStateDatas.Key] = new Dictionary<int, StickerSaveData>();
            
            foreach (var stickerStateData in stickerStateDatas.Value)
            {
                var stickerData = stickerStateData.Value;
                playerModeStickerSaveDatas[stickerStateDatas.Key].Add(stickerStateData.Key, new StickerSaveData(stickerData.StickerState, stickerData.CooldownStartTime, stickerData.UsingStartTime, stickerData.UsingObjectId));
            }
        }

        DataService.Instance.SetData(DataType.STICKERS_STATES, playerModeStickerSaveDatas, true);
        DataService.Instance.SetData(DataType.EVENT_STICKERS_STATES, eventStickerSaveDatas, true);
    }
    
    #endregion

    #region Others
    
    private void Update()
    {
        CheckStickersStates();
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            var rndContainer = InteractionManager.Instance.GetRandomAvailableContainer(InteractableType.ProductContainer);
            if (rndContainer != null)
            {
                UseSticker(1, rndContainer.ID);
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            var rndContainer = InteractionManager.Instance.GetRandomAvailableContainer(InteractableType.ProductContainer);
            if (rndContainer != null)
            {
                UseSticker(2, rndContainer.ID);
            }
        }
    }

    private float lastCheckTime;
    private void CheckStickersStates()
    {
        if(Time.time < lastCheckTime + 1f) return;
        lastCheckTime = Time.time;
        int timestamp = Timestamp.Now();

        bool shouldSave = false;
        foreach (var stickerStateData in CurrentModeStickerStateDatas)
        {
            var stickerStateValue = stickerStateData.Value;
            stickerStateValue.CheckState(timestamp, out bool isStateChanged, out string oldUsingObjectId);
            if (isStateChanged && stickerStateValue.StickerState == StickerState.InCoolDown)
            {
                Debug.Log($"Sticker id :{stickerStateData.Key} duration end!!");
                DisarmSticker(stickerStateData.Key, oldUsingObjectId);
            }
            shouldSave |= isStateChanged;
        }
        if(shouldSave) SaveData();
    }
    
    #endregion

    #region Set Functions

    public void UseSticker(int stickerId, string objectId, bool setState = true, bool checkActiveMode = true, bool inEvent = false, PlayerMode? playerMode = null)
    {
        var sticker = GetStickerById(stickerId);
        if (sticker == null)
        {
            ColoredLogUtility.PrintColoredError($"Sticker id :{stickerId} is null!!");
            return;
        }

        var stickerStateData = GetStickerStateDataById(sticker.Id);
        if (stickerStateData == null || stickerStateData.IsInCooldown)
        {
            ColoredLogUtility.PrintColoredError($"Sticker Id :{sticker.Id}, {(stickerStateData != null ? "Sticker InCooldown" : "StickerStateData is null")} !!");
            return;
        }

        AssignUsingObjectToStickersDictionaries(out var usingObjectToSticker, out var usingStickerToObject, checkActiveMode, inEvent, playerMode);
        
        // Managerlar farklı dükkanlara takılabildiği için, herhangi bir dükkanda takılıysa, o dükkandan çıkarılıp istenen dükkana takılıyor.
        if (IsStickerUsing(ref usingStickerToObject, sticker))
        {
            DisarmSticker(sticker.Id);
            // ColoredLogUtility.PrintColoredError($"Sticker : {sticker.Name} already using from {(stickerStateData.UsingObjectId)}!!");
            // return;
        }
        
        // ColoredLogUtility.PrintColoredLog($"{sticker.Name} using from {objectId}", LogColor.Green);

        if (HasSticker(ref usingObjectToSticker, objectId))
        {
            DisarmSticker(GetStickerByObjectId(ref usingObjectToSticker, objectId).Id, objectId, checkActiveMode: checkActiveMode, inEvent: inEvent, playerMode: playerMode);
        }

        AddStickerToDictionaries(usingObjectToSticker, usingStickerToObject, sticker, objectId);

        if (setState)
        {
            var stickerStatesDic = GetStickerStatesDic(checkActiveMode, inEvent, playerMode);
            SetState(sticker.Id, StickerState.Using, stickerStatesDic, objectId: objectId);
        }

        var starUpgradeData = StarUpgradeManager.Instance.GetUpgradeByInteractableId(objectId);
        bool isAutomateActive = starUpgradeData?.ManagerAutomateRequirements.CanAutomate(sticker) ?? false;
        OnStickerStateChanged?.Invoke(objectId, sticker,  isAutomateActive);
    }

    private Dictionary<int, StickerStateData> GetStickerStatesDic(bool checkActiveMode, bool inEvent, PlayerMode? playerMode)
    {
        if (checkActiveMode)
        {
            return CurrentModeStickerStateDatas;
        }
        else
        {
            if (inEvent)
            {
                return eventStickerStateDatas;
            }
            else
            {
                if (playerMode == null)
                {
                    ColoredLogUtility.PrintColoredError("Player Mode is null!!");
                    return null;
                }
                return playerModeStickerStateDatas[playerMode.Value];
            }
        }
    }

    private void AssignUsingObjectToStickersDictionaries(out Dictionary<string, Sticker> usingObjectToSticker, out Dictionary<Sticker, string> usingStickerToObject, bool checkActiveMode, bool inEvent, PlayerMode? playerMode)
    {
        if (checkActiveMode)
        {
            usingObjectToSticker = UsingStickersObjectToSticker; 
            usingStickerToObject = UsingStickersStickerToObject; 
        }
        else
        {
            if (inEvent)
            {
                usingObjectToSticker = eventUsingStickersObjectToSticker; 
                usingStickerToObject = eventUsingStickersStickerToObject; 
            }
            else
            {
                if (!usingStickersObjectToSticker.ContainsKey(playerMode.Value))
                {
                    usingStickersObjectToSticker[playerMode.Value] = new Dictionary<string, Sticker>();
                }  
                
                if (!usingStickersStickerToObject.ContainsKey(playerMode.Value))
                {
                    usingStickersStickerToObject[playerMode.Value] = new Dictionary<Sticker, string>();
                }
                
                usingObjectToSticker = usingStickersObjectToSticker[playerMode.Value]; 
                usingStickerToObject = usingStickersStickerToObject[playerMode.Value]; 
            }
        }
    }

    private void AddStickerToDictionaries(Dictionary<string, Sticker> objectToStickerDic, Dictionary<Sticker, string> stickerToObjectDic, Sticker sticker, string objectId)
    {
        if(!objectToStickerDic.ContainsKey(objectId))
            objectToStickerDic.Add(objectId, sticker);
        else
            ColoredLogUtility.PrintColoredError($"{objectId} already has a sticker!! Sticker : {objectToStickerDic[objectId].Name} ");

        if(!stickerToObjectDic.ContainsKey(sticker))
            stickerToObjectDic.Add(sticker, objectId);
        else
            ColoredLogUtility.PrintColoredError($"{sticker.Name} already attached to an object!! object : {stickerToObjectDic[sticker]} ");
    }
    
    private bool RemoveStickerFromDictionaries(Dictionary<string, Sticker> objectToStickerDic, Dictionary<Sticker, string> stickerToObjectDic, Sticker sticker, string objectId)
    {
        return false;
        return objectToStickerDic.Remove(objectId) && stickerToObjectDic.Remove(sticker);
    }

    public void DisarmSticker(int stickerId, string usingObjectId = null, bool setState = true, bool instantlyUsable = false, bool checkActiveMode = true, bool inEvent = false, PlayerMode? playerMode = null)
    {
        Sticker sticker = GetStickerById(stickerId);
        if (sticker == null)
        {
            ColoredLogUtility.PrintColoredError($"Sticker id :{stickerId} is null!!");
            return;
        }

        if (usingObjectId == null)
        {
            usingObjectId = GetObjectIdBySticker(sticker);;
            if (usingObjectId == string.Empty)
            {
                ColoredLogUtility.PrintColoredError($"UsingObjectId is empty!!");
                return;
            }
        }
        
        DisarmSticker(sticker, usingObjectId, setState, instantlyUsable, checkActiveMode, inEvent, playerMode);
    }
    
    public void DisarmSticker(Sticker sticker, string objectId, bool setState, bool instantlyUsable, bool checkActiveMode = true, bool inEvent = false, PlayerMode? playerMode = null)
    {
        // ColoredLogUtility.PrintColoredLog($"{sticker.Name} disarm from {objectId}", LogColor.Blue);

        AssignUsingObjectToStickersDictionaries(out var usingObjectToSticker, out var usingStickerToObject, checkActiveMode, inEvent, playerMode);
        
        bool isRemoved = RemoveStickerFromDictionaries(usingObjectToSticker, usingStickerToObject, sticker, objectId);

        if (!isRemoved)
        {
            ColoredLogUtility.PrintColoredError($"Sticker : {sticker.Name} is not using from {objectId}!!");
            return;
        }

        if (setState)
        {
            var stickerStatesDic = GetStickerStatesDic(checkActiveMode, inEvent, playerMode);
            StickerState state = instantlyUsable ? StickerState.Usable : StickerState.InCoolDown;
            SetState(sticker.Id, state, stickerStatesDic);
        }
        OnStickerStateChanged?.Invoke(objectId, null, false);
    }
    
    public void SetState(int stickerId, StickerState stickerState, Dictionary<int, StickerStateData> stickerStateDatasDic = null, string objectId = null)
    {
        StickerStateData stickerStateData = GetStickerStateDataById(stickerId, stickerStateDatasDic);
        stickerStateData.SetState(stickerState, objectId);
        SaveData();
    }

    #endregion
    
    #region Get Upgraded Value
    
    public float GetUpgradedValue(string objectId, ObjectDataType objectDataType, float baseValue, PoolType poolType, bool isNextLevel)
    {  
        if (!HasSticker(objectId, out Sticker sticker) || !sticker.CheckHasBoostByDataType(objectDataType))
        {
            return baseValue;
        }

        var boosts = sticker.GetStickerBoostByDataType(objectDataType);
        return ApplyUpgrades(boosts, baseValue);
    }
    
    private float ApplyUpgrades(List<StickerBoost> stickerBoosts, float baseValue)
    {
        if (stickerBoosts.Count == 0) return baseValue;
     
        float upgradedValue = baseValue;
        foreach (var stickerBoost in stickerBoosts)
        {
            upgradedValue *= stickerBoost.Multiplier;
            upgradedValue += stickerBoost.Addition;
        }
        return upgradedValue;
    }

    public IdleNumber GetUpgradedValue(string objectId, ObjectDataType objectDataType, IdleNumber baseValue, PoolType poolType, bool isNextLevel)
    {
        if (!HasSticker(objectId, out Sticker sticker) || !sticker.CheckHasBoostByDataType(objectDataType))
        {
            return baseValue;
        }

        /*if (objectDataType == ObjectDataType.ProductCount)
        {
            var stickerProfitBoost = DirectorManager.Instance.GetDirectorMultiplier(sticker.Id);
            baseValue = new IdleNumber(baseValue * stickerProfitBoost);
        }*/
        
        var boosts = sticker.GetStickerBoostByDataType(objectDataType);
        
        return ApplyUpgrades(baseValue , sticker.Id , sticker.RarityType);
    }

    public IdleNumber ApplyUpgrades( IdleNumber baseValue , int stickerID , RarityType rarityType)
    {
        IdleNumber upgradedValue = baseValue;
        /*foreach (var stickerBoost in stickerBoosts)
        {
            upgradedValue *= stickerBoost.Multiplier;
            upgradedValue += stickerBoost.Addition;
        }*/
        var level = DirectorManager.Instance.GetDirectorCardLevel(stickerID);

        return ApplyUpgradeByRarityBoostType(baseValue , rarityType, level);
        
        return upgradedValue * GetLevelMultiplyInfo(stickerID , level);
    }

    private IdleNumber ApplyUpgradeByRarityBoostType(IdleNumber baseValue , RarityType stickerRarity , int stickerLevel )
    {
        return baseValue * GetStickerBoostValue(stickerRarity, stickerLevel);
    }

    public int GetStickerNextEvolveLevel(RarityType stickerRarity , int stickerLevel , out float evolveBonus)
    {
        StickerBoostDatasDic.TryGetValue(stickerRarity, out StickerBoostData boostData);
        int nextEvolvoLevel = -1;
        evolveBonus = 0;
        if (boostData != null)
        {
            for (int i = 0; i < boostData.StickerBoosts.Count; i++)
            {
                if (stickerLevel < boostData.StickerBoosts[i].StartLevel)
                {
                    nextEvolvoLevel = boostData.StickerBoosts[i].StartLevel;
                    //evolveBonus = new IdleNumber(boostData.StickerBoosts[i].Addition, NumberDigits.Empty);
                    evolveBonus = boostData.StickerBoosts[i].Multiply;
                    break;
                }
            }
        }

        return nextEvolvoLevel;
    }

    public IdleNumber GetStickerBoostByRarityType(RarityType stickerRarity , int stickerLevel)
    {
        return GetStickerBoostValue(stickerRarity, stickerLevel);
    }

    private IdleNumber GetStickerBoostValue(RarityType stickerRarity , int stickerLevel )
    {
        int currentLevel = 1;
        StickerBoostDatasDic.TryGetValue(stickerRarity, out StickerBoostData boostData);
        if (boostData != null)
        {
            IdleNumber upgradedBoost = new IdleNumber(boostData.BaseBoost, NumberDigits.Empty);

            for (int i = 0; i < boostData.StickerBoosts.Count; i++)
            {
                var isThereNextList = i < boostData.StickerBoosts.Count - 1;

                int setUntilThatLevel = isThereNextList
                    ? boostData.StickerBoosts[i + 1].StartLevel
                    : 5000;

                var addition = boostData.StickerBoosts[i].Addition;
                var multiply = boostData.StickerBoosts[i].Multiply;

                if (Mathf.Abs(multiply) > 0.001f)
                    upgradedBoost *= multiply;
                
                for (int j = 0; currentLevel < setUntilThatLevel - 1; j++)
                {
                    if (i > 0 && j == 0)
                    {
                        currentLevel++;
                        continue;
                    }
                    
                    if(currentLevel >= stickerLevel)
                        break;
                    
                    upgradedBoost += addition;
                    
                    currentLevel++;
                }
                
                if(currentLevel >= stickerLevel)
                    break;
            }

            return upgradedBoost;
        }

        return new IdleNumber(1f, NumberDigits.Empty);
    }

    #endregion

    #region Get Functions

    public List<Sticker> GetAllStickers()
    {
        return StickerCollection.GetAllStickers();
    }
    
    public List<Sticker> GetStickersByCategoryType(PoolType objectType)
    {
        return StickerCollection.GetStickersByCategoryType(objectType);
    }
    
    public List<int> GetOwnedStickersIds()
    {
        return DirectorManager.Instance.GetUnlockedDirectorsIds();
    }

    public bool IsStickerUnlockedWithId(int stickerId)
    {
        var allEnableStickers = GetOwnedStickersIds();

        for (int i = 0; i < allEnableStickers.Count; i++)
        {
            if (stickerId == allEnableStickers[i])
            {
                return true;
            }
        }

        return false;
    }
    
    public List<Sticker> GetOwnedStickers()
    {
        List<int> ownedStickersIds = GetOwnedStickersIds();
        List<Sticker> ownedStickers = new List<Sticker>();
        
        foreach (var ownedStickersId in ownedStickersIds)
        {
            var sticker = GetStickerById(ownedStickersId);
            if(sticker == null) continue;
            ownedStickers.Add(sticker);
        }

        return ownedStickers;
    }    
    
    public List<Sticker> GetOwnedStickersByCategory(PoolType objectType)
    {
        List<Sticker> wantedCategoryStickers = GetStickersByCategoryType(objectType);
        List<Sticker> wantedStickers = new List<Sticker>();
        
        foreach (var wantedCategorySticker in wantedCategoryStickers)
        {
            if(DirectorManager.Instance.IsLock(wantedCategorySticker.Id)) continue;
            wantedStickers.Add(wantedCategorySticker);
        }

        return wantedStickers;
    }
    
    public string GetObjectIdBySticker(Sticker sticker)
    {
        return UsingStickersStickerToObject.TryGetValue(sticker, out string objectId) ? objectId : string.Empty;
    }
    
    public Sticker GetStickerByObjectId(string objectId)
    {
        return UsingStickersObjectToSticker.TryGetValue(objectId, out Sticker sticker) ? sticker : null;
    }
    
    public Sticker GetStickerByObjectId(ref Dictionary<string, Sticker> objectToStickerDic ,string objectId)
    {
        return objectToStickerDic.TryGetValue(objectId, out Sticker sticker) ? sticker : null;
    }

    public bool IsStickerUsing(Sticker sticker)
    {
        return UsingStickersStickerToObject.ContainsKey(sticker);
    }
    
    public bool IsStickerUsing(ref Dictionary<Sticker, string> stickerToObjectDic, Sticker sticker)
    {
        return stickerToObjectDic.ContainsKey(sticker);
    }
    
    public bool HasSticker(string objectId)
    {
        return UsingStickersObjectToSticker.ContainsKey(objectId);
    }

    public bool HasSticker(ref Dictionary<string, Sticker> objectToStickerDic, string objectId)
    {
        return objectToStickerDic.ContainsKey(objectId);
    }

    public void CheckAndAssignAllManager()
    {
        var productContainers = InteractionManager.Instance.GetAllAvailableInteractables<ProductContainer>(InteractableType.ProductContainer);

        foreach(var container in productContainers)
        {
            CheckAndAssignManager(container);       
        }
    }

    public void CheckAndAssignManager(ProductContainer productContainer)
    {
        if (HasSticker(productContainer.ID)) return;
        var ownedManagers = GetOwnedStickersByCategory(productContainer.GetObjectType());
        
        foreach (var manager in ownedManagers)
        {
            var managetStateData = GetStickerStateDataById(manager.Id);
            if (managetStateData.IsUsing || manager.RarityType != productContainer.GetAutomateRequirements().MinRarity) continue;
            UseSticker(manager.Id, productContainer.ID);
            productContainer.IsAutomateActive(out _);
            break;
        }
    }

    public Sticker GetStickerById(int id)
    {
        return stickerCollection.GetStickerById(id);
    }
    
    public StickerStateData GetStickerStateDataById(int id, Dictionary<int, StickerStateData> objectToStickerDic = null)
    {
        var activeDic = objectToStickerDic ?? CurrentModeStickerStateDatas;
        return activeDic.TryGetValue(id, out StickerStateData stickerStateData) ? stickerStateData : null;
    }

    public List<Sticker> GetStickersByRarityType(RarityType rarityType)
    {
        return stickerCollection.GetStickersByRarityType(rarityType);
    }

    public List<Sticker> GetStickersByBoostDataType(ObjectDataType objectDataType)
    {
        return stickerCollection.GetStickersByBoostDataType(objectDataType);
    }

    public int GetStickerRemainDuration(int id, out bool isLimitless)
    {
        isLimitless = false;
        var stickerStateData = GetStickerStateDataById(id);
        if (stickerStateData == null)
        {
            ColoredLogUtility.PrintColoredError("Sticker Save data is null!!");
            return -1;
        }
            
        return stickerStateData.GetRemainingDuration(out isLimitless);
    }
    
    public int GetStickerDuration(int id)
    {
        var sticker = GetStickerById(id);
        return sticker?.Duration ?? -1;
    }

    public float GetLevelMultiplyInfo(int id , int level)
    {
        var sticker = GetStickerById(id);
        var stickerBoosts = sticker.Boosts;
        
        return Mathf.Pow(stickerBoosts[0].Multiplier , level);
    }

    public float GetNextLevelMultiplyInfo(int id , int level , out bool isCurrentMaxLevel)
    {
        var cardData = DirectorManager.Instance.GetDirectorCardDataById(id);
        isCurrentMaxLevel = cardData == null || DirectorManager.Instance.IsMaxLevel(id);
        if (isCurrentMaxLevel)
            return 1f;
         
        return GetLevelMultiplyInfo(id , level + 1);
    }
    
    #endregion

    #region Has Functions

    public bool HasAnyUsingSticker(ObjectDataType objectDataType)
    {
        foreach (var sticker in UsingStickersObjectToSticker.Values)
        {
            if (sticker.CheckHasBoostByDataType(objectDataType))
                return true;
        }

        return false;
    }
    
    public bool HasAnyUsingSticker(ObjectDataType objectDataType, out List<string> usingObjects)
    {
        usingObjects = new List<string>();
        
        foreach (var pair in UsingStickersStickerToObject)
        {
            if (pair.Key.CheckHasBoostByDataType(objectDataType))
            {
                usingObjects.Add(pair.Value);
            }
        }

        return usingObjects.Count > 0;
    }

    public bool HasSticker(string objectId, out Sticker sticker)
    {
        return UsingStickersObjectToSticker.TryGetValue(objectId, out sticker);
    }

    #endregion

    public bool IsAutomateActive(Sticker sticker) => IsAutomateActive(sticker, out _, out _);
    
    public bool IsAutomateActive(Sticker sticker, out bool isRarityEnough, out bool isLevelEnough)
    {
        isLevelEnough = false;
        isRarityEnough = false;
        
        if (sticker == null) return false;
        var usingObjectId = GetObjectIdBySticker(sticker);
        if (usingObjectId == string.Empty) return false;

        return IsAutomateActive(usingObjectId, sticker, out isRarityEnough, out isLevelEnough);
    }
    
    public bool IsAutomateActive(string containerId, out Sticker sticker)
    {
        sticker = GetStickerByObjectId(containerId);
        if (sticker == null) return false;
        var starUpgradeData = StarUpgradeManager.Instance.GetUpgradeByInteractableId(containerId);
        return starUpgradeData.ManagerAutomateRequirements.CanAutomate(sticker);
    }

    public bool IsAutomateActive(string containerId, Sticker sticker) => IsAutomateActive(containerId, sticker, out _, out _);
    
    public bool IsAutomateActive(string containerId, Sticker sticker, out bool isRarityEnough, out bool isLevelEnough)
    {
        isRarityEnough = false;
        isLevelEnough = false;
        
        if (sticker == null || containerId == string.Empty) return false;
        var starUpgradeData = StarUpgradeManager.Instance.GetUpgradeByInteractableId(containerId);
        if (starUpgradeData == null) return false;
        return starUpgradeData.ManagerAutomateRequirements.CanAutomate(sticker, out isRarityEnough, out isLevelEnough);
    }
}