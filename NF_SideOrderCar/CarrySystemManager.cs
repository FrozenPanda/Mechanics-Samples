using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class CarrySystemManager : Singleton<CarrySystemManager>
{
    public static int CarryManagerID = 12;
    
    private const string CollectionPath = "Configurations/CarrySystemCollection";

    public UnityEvent<int> OnCarrierWakedUp = new UnityEvent<int>();

    public UnityEvent<string, int> OnContainerCreated = new UnityEvent<string, int>();
    public UnityEvent<string, int> OnMarketConsumed = new UnityEvent<string, int>();

    public UnityEvent<string> OnRequestForAmount = new UnityEvent<string>();
    public UnityEvent<string , int> OnReceivedForAmount = new UnityEvent<string ,int>();

    private Dictionary<string, int> CarrierBoxAccumulateAmountSaveDic = new Dictionary<string, int>();
    
    private CarrySystemCollection _carrySystemCollection;
    private int currentSideOrderCarLevel = 0;
    private int currentInventoryHouseLevel = 0;
    
    public bool isManagerAutomateForSideCars { private set; get; }
    public int managerAutomateMinReqLevel { private set; get; }

    private void OnEnable()
    {
        DirectorManager.Instance.OnDirectorCarUpgraded?.AddListener(CheckManagerAutomateState);
        LevelManager.Instance.LevelExpended.AddListener(CheckManagerAutomateState);
        LevelManager.Instance.OnNewLevel.AddListener(ResetSaveData);
    }

    private void OnDisable()
    {
        DirectorManager.Instance.OnDirectorCarUpgraded?.RemoveListener(CheckManagerAutomateState);
        LevelManager.Instance.LevelExpended.RemoveListener(CheckManagerAutomateState);
        LevelManager.Instance.OnNewLevel.RemoveListener(ResetSaveData);
    }

    private void Awake()
    {
        LoadCollection();
        LoadData();
        CheckManagerAutomateState();
    }

    #region SideOrderCar

    public (int skinID, IdleNumber UpgradeCost, IdleNumber carryCapacity , int level , Sprite carIcon) GetCurrentDataSideOrderCar()
    {
        return (GetSkinID(), GetUpgradeCostSO(), GetCarryCapacitySO() , currentSideOrderCarLevel , GetCarIcon());
    }

    private int GetSkinID()
    {
        var starLevel = currentSideOrderCarLevel / 25;
        if (starLevel > _carrySystemCollection.SideOrderCarSkins.Count - 1)
            return _carrySystemCollection.SideOrderCarSkins.Last();
        return _carrySystemCollection.SideOrderCarSkins[starLevel];
    }

    private IdleNumber GetUpgradeCostSO()
    {
        int selectedList = -1;
        for (int i = _carrySystemCollection.SideOrderCarLevelUpCosts.Count - 1; i >= 0; i--)
        {
            if (currentSideOrderCarLevel >= _carrySystemCollection.SideOrderCarLevelUpCosts[i].StartLevel)
            {
                selectedList = i;
                break;
            }
        }

        int remainLevel = currentSideOrderCarLevel - _carrySystemCollection.SideOrderCarLevelUpCosts[selectedList].StartLevel;
        float multiply = Mathf.Pow(_carrySystemCollection.SideOrderCarLevelUpCosts[selectedList].IncreasePerLevel,
            remainLevel);

        return _carrySystemCollection.SideOrderCarLevelUpCosts[selectedList].startNumber * multiply;
    }

    private IdleNumber GetCarryCapacitySO()
    {
        int selectedList = -1;
        for (int i = _carrySystemCollection.SideOrderCarryCapacities.Count - 1; i >= 0; i--)
        {
            if (currentSideOrderCarLevel >= _carrySystemCollection.SideOrderCarryCapacities[i].StartLevel)
            {
                selectedList = i;
                break;
            }
        }

        int remainLevel = currentSideOrderCarLevel - _carrySystemCollection.SideOrderCarryCapacities[selectedList].StartLevel;
        float multiply = Mathf.Pow(_carrySystemCollection.SideOrderCarryCapacities[selectedList].IncreasePerLevel,
            remainLevel);

        var capacity = _carrySystemCollection.SideOrderCarryCapacities[selectedList].startNumber;
        capacity *= multiply;
        //capacity.RoundDecimals();
        return capacity;
    }

    private Sprite GetCarIcon()
    {
        var starLevel = currentSideOrderCarLevel / 25;
        if (starLevel > _carrySystemCollection.SideOrderCarSprites.Count - 1)
            return _carrySystemCollection.SideOrderCarSprites.Last();
        return _carrySystemCollection.SideOrderCarSprites[starLevel];
    }
    
    public void UpgradeSideOrderCar()
    {
        currentSideOrderCarLevel++;
        SaveData();
    }

    public float GetUpgradeableFillBarSO()
    {
        int nextStarUpgradeLevel = ((currentSideOrderCarLevel / 25) + 1) * 25;
        IdleNumber totalCost = new IdleNumber();
        IdleNumber currentMoney = IdleExchangeService.GetIdleValue(CurrencyType.Coin);
        for (int i = currentSideOrderCarLevel; i <= nextStarUpgradeLevel; i++)
        {
            totalCost += GetCostForSpesificLevelSO(i);

            if (totalCost >= currentMoney)
            {
                if (i == nextStarUpgradeLevel)
                    return 1f;
                return (i % 25) / 25f;
            }
        }

        return 1f;
    }

    private IdleNumber GetCostForSpesificLevelSO(int level)
    {
        int selectedList = -1;
        for (int i = _carrySystemCollection.SideOrderCarLevelUpCosts.Count - 1; i >= 0; i--)
        {
            if (level >= _carrySystemCollection.SideOrderCarLevelUpCosts[i].StartLevel)
            {
                selectedList = i;
                break;
            }
        }

        int remainLevel = level - _carrySystemCollection.SideOrderCarLevelUpCosts[selectedList].StartLevel;
        float multiply = Mathf.Pow(_carrySystemCollection.SideOrderCarLevelUpCosts[selectedList].IncreasePerLevel,
            remainLevel);

        return _carrySystemCollection.SideOrderCarLevelUpCosts[selectedList].startNumber * multiply;
    }

    #endregion

    #region InventoryHouse

    public (IdleNumber upgradeCost, IdleNumber carryCapacity, int level) GetCurrentDataInventoryHouse()
    {
        return (GetUpgradeCostIH(), GetCarryCapacityIH(), currentInventoryHouseLevel);
    }
    
    private IdleNumber GetUpgradeCostIH()
    {
        int selectedList = -1;
        for (int i = _carrySystemCollection.InventoryLevelUpCosts.Count - 1; i >= 0; i--)
        {
            if (currentInventoryHouseLevel >= _carrySystemCollection.InventoryLevelUpCosts[i].StartLevel)
            {
                selectedList = i;
                break;
            }
        }

        int remainLevel = currentInventoryHouseLevel - _carrySystemCollection.InventoryLevelUpCosts[selectedList].StartLevel;
        float multiply = Mathf.Pow(_carrySystemCollection.InventoryLevelUpCosts[selectedList].IncreasePerLevel,
            remainLevel);

        var capacity = _carrySystemCollection.InventoryLevelUpCosts[selectedList].startNumber;
        capacity *= multiply;
        //capacity.Round();
        return capacity;
    }
    
    public IdleNumber GetCarryCapacityIH()
    {
        int selectedList = -1;
        for (int i = _carrySystemCollection.InventoryCapacities.Count - 1; i >= 0; i--)
        {
            if (currentInventoryHouseLevel >= _carrySystemCollection.InventoryCapacities[i].StartLevel)
            {
                selectedList = i;
                break;
            }
        }

        int remainLevel = currentInventoryHouseLevel - _carrySystemCollection.InventoryCapacities[selectedList].StartLevel;
        float multiply = Mathf.Pow(_carrySystemCollection.InventoryCapacities[selectedList].IncreasePerLevel,
            remainLevel);

        var stickerBoost =
            StickerManager.Instance.ApplyUpgrades(new IdleNumber(1f, NumberDigits.Empty), 12, RarityType.Rare);

        var capacity = _carrySystemCollection.InventoryCapacities[selectedList].startNumber * multiply * stickerBoost;
        //capacity.Round();
        
        return capacity;
    }
    
    public void UpgradeInventoryHouse()
    {
        currentInventoryHouseLevel++;
        SaveData();
    }

    public IdleNumber GetInventoryFreeSpace()
    {
        var difference = GetCarryCapacityIH() - IdleExchangeService.GetIdleValue(CurrencyType.PotantialMarketCoin);
        if (difference < new IdleNumber(1f, NumberDigits.Empty))
            difference = new IdleNumber(0f, NumberDigits.Empty);
        return difference;
    }
    
    public float GetUpgradeableFillBarIH()
    {
        int nextStarUpgradeLevel = ((currentInventoryHouseLevel / 25) + 1) * 25;
        IdleNumber totalCost = new IdleNumber();
        IdleNumber currentMoney = IdleExchangeService.GetIdleValue(CurrencyType.Coin);
        for (int i = currentInventoryHouseLevel; i <= nextStarUpgradeLevel; i++)
        {
            totalCost += GetCostForSpesificLevelIH(i);

            if (totalCost >= currentMoney)
            {
                if (i == nextStarUpgradeLevel)
                    return 1f;
                return (i % 25) / 25f;
            }
        }

        return 1f;
    }

    private IdleNumber GetCostForSpesificLevelIH(int level)
    {
        int selectedList = -1;
        for (int i = _carrySystemCollection.InventoryLevelUpCosts.Count - 1; i >= 0; i--)
        {
            if (level >= _carrySystemCollection.InventoryLevelUpCosts[i].StartLevel)
            {
                selectedList = i;
                break;
            }
        }

        int remainLevel = level - _carrySystemCollection.InventoryLevelUpCosts[selectedList].StartLevel;
        float multiply = Mathf.Pow(_carrySystemCollection.InventoryLevelUpCosts[selectedList].IncreasePerLevel,
            remainLevel);

        return _carrySystemCollection.InventoryLevelUpCosts[selectedList].startNumber * multiply;
    }

    #endregion

    #region Carrier

    public int GetCarrierPathIndex(string id)
    {
        foreach (var data in _carrySystemCollection.CarrierPathInfos)
        {
            if (data.marketID == id)
            {
                return data.pathConnectIndex;
            }
        }

        return 1;
    }

    #endregion

    private void CheckManagerAutomateState()
    {
        var level = LevelManager.Instance.ActiveLevelId;
        var city = LevelManager.Instance.ActiveCityId;

        foreach (var data in _carrySystemCollection.ManagerAutomateStates)
        {
            if (data.cityID == city)
            {
                if (data.byLevel.Count > level)
                {
                    managerAutomateMinReqLevel = data.byLevel[level];
                }
                
                if (data.byLevel.Count > level && data.byLevel[level] <= DirectorManager.Instance.GetDirectorCardLevel(CarryManagerID))
                {
                    managerAutomateMinReqLevel = data.byLevel[level];
                    isManagerAutomateForSideCars = true;
                    return;
                }
            }

            isManagerAutomateForSideCars = false;
        }
    }

    public int GetUpgradeableLevel()
    {
        return 1;
    }

    private void LoadCollection()
    {
        _carrySystemCollection = CarrySystemCollection.LoadCollection(CollectionPath);
    }
    
    #region SaveLoad

    public void TrySaveCurrentBoxCount(string id , int amount)
    {
        if (CarrierBoxAccumulateAmountSaveDic.ContainsKey(id))
        {
            CarrierBoxAccumulateAmountSaveDic[id] = amount;
        }
        else
        {
            CarrierBoxAccumulateAmountSaveDic.Add(id , amount);
        }
        
        DataService.Instance.SetData(DataType.CARRIER_BOX_ACCUMULATE_AMOUNT_DATA , CarrierBoxAccumulateAmountSaveDic);
    }

    public int TryGetSaveValueForCarryAccumulater(string id)
    {
        if (CarrierBoxAccumulateAmountSaveDic.TryGetValue(id, out int amount))
        {
            return amount;
        }
        return 0;
    }
    
    private void LoadData()
    {
        var accumulateSaveData = DataService.Instance.GetData<Dictionary<string, int>>(DataType.CARRIER_BOX_ACCUMULATE_AMOUNT_DATA);

        foreach (var data in accumulateSaveData)
        {
            CarrierBoxAccumulateAmountSaveDic.TryAdd(data.Key, data.Value);
        }
        
        var stateData = DataService.Instance.GetData<Dictionary<StateType, int>>(DataType.STATE);

        if (stateData.ContainsKey(StateType.SideOrderCarLevel))
            currentSideOrderCarLevel = stateData[StateType.SideOrderCarLevel];
        else
            currentSideOrderCarLevel = 1;
        

        if (stateData.ContainsKey(StateType.InventoryHouseLevel))
            currentInventoryHouseLevel = stateData[StateType.InventoryHouseLevel];
        else
            currentInventoryHouseLevel = 1;
    }

    private void SaveData()
    {
        var stateData = DataService.Instance.GetData<Dictionary<StateType, int>>(DataType.STATE);

        stateData[StateType.SideOrderCarLevel] = currentSideOrderCarLevel;
        stateData[StateType.InventoryHouseLevel] = currentInventoryHouseLevel;
    }

    private void ResetSaveData()
    {
        CarrierBoxAccumulateAmountSaveDic.Clear();
        
        DataService.Instance.SetData(DataType.CARRIER_BOX_ACCUMULATE_AMOUNT_DATA , CarrierBoxAccumulateAmountSaveDic);
        
        currentSideOrderCarLevel = 1;
        currentInventoryHouseLevel = 1;
        SaveData();
    }

    #endregion
}
