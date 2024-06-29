using System;
using System.Collections;
using System.Collections.Generic;
using _Game.Scripts.Systems.WeeklyEventSystem;
using UnityEngine;
using lib.Managers.AnalyticsSystem;
using UnityEngine.Events;

public class ExpendManager : Singleton<ExpendManager>
{
    public UnityEvent<int> OnNewTileExpend = new UnityEvent<int>();
    public UnityEvent OnTileExpend = new UnityEvent();

    private List<Expendable> allExpensions = new List<Expendable>();
    private List<Expendable> allCloseExpensions = new List<Expendable>();

    public int ContainerTileCount => containerTileCount;
    private int containerTileCount = 0;
    
    private Dictionary<ExpandableType, List<UnlockProductExpendable>> expandableTypesDictionary = new ();
    
    #region Id conflict check

    private Dictionary<int, ExpendableObjectInformation> expendablesDictionary = new Dictionary<int, ExpendableObjectInformation>();

    private class ExpendableObjectInformation
    {
        public string LevelName;
        public string ObjectName;
        public GameObject GameObject;

        public ExpendableObjectInformation(string levelName, string objectName, GameObject go)
        {
            LevelName = levelName;
            ObjectName = objectName;
            GameObject = go;
        }
    }

    bool isLoaded = false;
    private void Start()
    {
        if (!isLoaded)
        {
            LoadStateData();
            isLoaded = true;
        }
    }

    private void LoadStateData()
    {
        var stateData = DataService.Instance.GetData<Dictionary<StateType, int>>(DataType.STATE);
        if (stateData == null)
        {
            stateData = new Dictionary<StateType, int>();
        }
        if (!stateData.ContainsKey(StateType.TotalStationCount)) stateData[StateType.TotalStationCount] = 0;
        containerTileCount = stateData[StateType.TotalStationCount];
    }

    private void SaveStateData()
    {
        var stateData = DataService.Instance.GetData<Dictionary<StateType, int>>(DataType.STATE);
        stateData[StateType.TotalStationCount] = containerTileCount;
        DataService.Instance.SetData(DataType.STATE, stateData);
    }

    private void AddToExpandablesDictionary(Expendable expendable)
    {
        var levelName = LevelManager.Instance.GetLevelCurrentName();
        if (expendable == null) return;
        if (expendablesDictionary.ContainsKey(expendable.ExpandableId))
        {
            var otherInteractableInformation = expendablesDictionary[expendable.ExpandableId];
            if (otherInteractableInformation.GameObject != null &&
                !otherInteractableInformation.GameObject.Equals(null))
            {
                if (otherInteractableInformation.GameObject.Equals(expendable.gameObject))
                {
                    return;
                }
            }
            
            // Debug.Log($"<color=red> The Expandable ids of {otherInteractableInformation.LevelName} / {otherInteractableInformation.ObjectName} and  {levelName}/{expendable.gameObject.name} objects are the same.</color> " +
            //           $"\n <color=green> Please make the ids different :)</color>");
            
            // Loop lu şekilde aynı seviyelerin tekrar oynandığı oyunlarda exception göndermek yerine debug mesajı bastırılmalı.

            return;
        }

        expendablesDictionary.Add(expendable.ExpandableId, new ExpendableObjectInformation(levelName, expendable.gameObject.name, expendable.gameObject));
    }
    #endregion

    // private void OnEnable()
    // {
    //     //LevelManager.Instance.LevelLoaded.AddListener(UpdateAllCostTexts);
    // }
    //
    // private void OnDisable()
    // {
    //     if (LevelManager.IsAvailable())
    //     {
    //         //LevelManager.Instance.LevelLoaded.RemoveListener(UpdateAllCostTexts);
    //     }
    // }

    public bool IsExpended(Expendable expendable)
    {
        bool inEvent = EventManager.IsAvailable() && EventManager.Instance.InEvent;
        var inventoryType = (inEvent && PlayerModeManager.IsAvailable()) ? InventoryType.EventExpendTile : PlayerModeManager.Instance.GetActiveExpendTile();
        return expendable.ExpandableId == -1 || 
               (UnlockManager.IsAvailable() && UnlockManager.Instance.IsUnlocked(inventoryType, expendable.ExpandableId));
    }

    public bool IsAllExpended(List<Expendable> expendables)
    {
        foreach (var expendable in expendables)
        {
            if (!IsExpended(expendable))
            {
                return false;
            }
        }
        return true;
    }

    public void Expend(Expendable expendable)
    {
        if (expendable == null) return;
        AddToExpensions(expendable);
        bool inEvent = EventManager.Instance.InEvent;
        var inventoryType = inEvent ? InventoryType.EventExpendTile : PlayerModeManager.Instance.GetActiveExpendTile();
        UnlockManager.Instance.Unlock(inventoryType, expendable.ExpandableId);

        ExpendTileEvent(expendable);

        allCloseExpensions.Remove(expendable);
        OnNewTileExpend?.Invoke(expendable.ExpandableId);
        OnTileExpend?.Invoke();
        //UpdateAllCostTexts();
    }

    public void AddToExpandableTypesMap(ExpandableType expandableType, UnlockProductExpendable unlockProductExpendable)
    {
        if(!expandableTypesDictionary.ContainsKey(expandableType)) 
            expandableTypesDictionary[expandableType] = new List<UnlockProductExpendable>();
        
        if (!expandableTypesDictionary[expandableType].Contains(unlockProductExpendable))
            expandableTypesDictionary[expandableType].Add(unlockProductExpendable);
    }

    public int GetExpandableCountByExpandableType(ExpandableType expandableType, bool onlyNotExpended)
    {
        if (!expandableTypesDictionary.TryGetValue(expandableType, out var wantedExpandableByTypes)) return 0;
        if (!onlyNotExpended) return wantedExpandableByTypes.Count;
        
        int wantedExpandableCount = 0;
        foreach (var wantedExpandable in wantedExpandableByTypes)
        {
            if(IsExpended(wantedExpandable)) continue;
            ++wantedExpandableCount;
        }

        return wantedExpandableCount;
    }
    
    public List<UnlockProductExpendable> GetExpendablesByExpendableType(ExpandableType expandableType, bool onlyNotExpended)
    {
        if (!expandableTypesDictionary.TryGetValue(expandableType, out var wantedExpandableByTypes)) return new List<UnlockProductExpendable>();
        if (!onlyNotExpended) return wantedExpandableByTypes;

        List<UnlockProductExpendable> wantedNotExpendedExpendables = new List<UnlockProductExpendable>();
        // wantedNotExpendedExpendables.AddRange(wantedExpandableByTypes);
        
        foreach (var wantedExpandable in wantedExpandableByTypes)
        {
            if(IsExpended(wantedExpandable)) continue;
            wantedNotExpendedExpendables.Add(wantedExpandable);
        }

        return wantedNotExpendedExpendables;
    }

    private void ClearExpandableTypesMap()
    {
        expandableTypesDictionary.Clear();
    }

    private void ExpendTileEvent(Expendable expendable)
    {
        Dictionary<string, float> logParameters = new Dictionary<string, float>();
        //logParameters.Add("TileCount", GetTileCount());
        logParameters.Add("TileId", expendable.ExpandableId);

        AnalyticsManager.Instance.CustomEvent("TileUnlock", logParameters);

        var nfProductContainer = expendable.GetComponentInChildren<NFProductContainer>(true);
        if (nfProductContainer != null)
        {
            containerTileCount++;
            SaveStateData();
            int levelData = LevelManager.Instance.GetLevelData();
            AnalyticsManager.Instance.StationOpenedEvent(levelData, containerTileCount);
            AnalyticsManager.Instance.GeneratorUnlockedEvent(levelData, nfProductContainer.InteractionID);
            /*if (levelData == 1 && containerTileCount == 1)
                SmartlookStarter.Instance.SmartlookEvent(SmartLookEvent.Level1_Statiton1_Unlock);
            if (levelData == 2 && containerTileCount == 1)
                SmartlookStarter.Instance.SmartlookEvent(SmartLookEvent.Level2_Statiton1_Unlock);
            if (levelData == 2 && containerTileCount == 2)
                SmartlookStarter.Instance.SmartlookEvent(SmartLookEvent.Level2_Statiton2_Unlock);*/
        }
    }
    /*public int GetTileCount()
    {
        int res = 0;
        foreach(var exp in allExpensions)
        {
            res += exp.TileValue;
        }
        return res;
    }*/

    public bool IsOpenedExpension(Expendable expendable)
    {
        return !allCloseExpensions.Contains(expendable);
    }

    public bool IsOpenedExpension(string expandId)
    {
        foreach (var expendable in allExpensions)
        {
            if (expendable.ID == expandId)
                return true;
        }

        return false;
    }

    public void AddCloseExpension(Expendable expendable)
    {
        AddToExpandablesDictionary(expendable);
        allCloseExpensions.Add(expendable);
    }

    public void AddToExpensions(Expendable expendable)
    {
        AddToExpandablesDictionary(expendable);
        allExpensions.Add(expendable);
    }

    public void RemoveFromAllExpensionLists(Expendable expendable)
    {
        if (allCloseExpensions.Contains(expendable)) allCloseExpensions.Remove(expendable);
        if (allExpensions.Contains(expendable)) allExpensions.Remove(expendable);
    }

    /*public void UpdateAllCostTexts()
    {
        foreach(var expension in allCloseExpensions)
        {
            expension.UpdateCostText();
        }

        FindObjectOfType<TileBlock>().UpdateTileText();
    }*/

    public List<Expendable> GetAllCloseExpension()
    {
        return allCloseExpensions;
    }

    public Expendable GetCloseExpendableWithId(int expencionId)
    {
        foreach (var expension in allCloseExpensions)
        {
            if (expension.ExpandableId == expencionId)
                return expension;
        }

        return null;
    }

    private void OnEnable()
    {
        EventService.OnGeneralUpgradePurchased.AddListener(TryApplyGeneralUpgrade);
        if (LevelManager.IsAvailable())
        {
            //LevelManager.Instance.LevelLoaded.AddListener(LevelLoaded);
            LevelManager.Instance.CityLoaded.AddListener(LevelLoaded);
        }
    }

    private void OnDisable()
    {
        EventService.OnGeneralUpgradePurchased.RemoveListener(TryApplyGeneralUpgrade);
        if (LevelManager.IsAvailable())
        {
            //LevelManager.Instance.LevelLoaded.RemoveListener(LevelLoaded);
            LevelManager.Instance.CityLoaded.RemoveListener(LevelLoaded);
        }
    }

    private void TryApplyGeneralUpgrade(IdleUpgradeItem idlUpgradeItem)
    {
        var generalUpgradeType = idlUpgradeItem.GeneralUpgradeType; 
        
        if ( generalUpgradeType != GeneralUpgradeType.CustomerCountWithUnlock && 
             generalUpgradeType != GeneralUpgradeType.CarCustomerCountWithUnlock &&
             generalUpgradeType != GeneralUpgradeType.VehicleCustomerCountWithUnlock)
        {
            return;
        }
        
        var unlockedExpendables = idlUpgradeItem.UnlockedObjectExpendableId;
        ExpendTiles(unlockedExpendables, idlUpgradeItem.ShowBoxUnlock);
    }

    private void ExpendTiles(List<int> unlockedExpendables, bool showBoxUnlock)
    {
        if(unlockedExpendables.Count == 0 ) return;
        
        foreach (var unlockedExpendable in unlockedExpendables)
        {
            var targetExpendable = GetCloseExpendableWithId(unlockedExpendable);
            if(targetExpendable == null) return;
            Expend(targetExpendable);
            Action tileUnlockedAction = () => targetExpendable.TileUnlocked(false);

            if (showBoxUnlock)
            {
                var boxUnlock = PoolingSystem.Instance.Create<BoxUnlock>(PoolType.BoxUnlock);
                var boxUnlockPos = targetExpendable.transform.position;
                boxUnlock.InitializeBox(boxUnlockPos, tileUnlockedAction);
            }
            else
            {
                tileUnlockedAction?.Invoke();
            }
        }
    }

    private void LevelLoaded()
    {
        ResetContainerTileCount();
        ClearExpandableTypesMap();
    }
    
    private void ResetContainerTileCount()
    {
        containerTileCount = 0;
        SaveStateData();
    }
}
