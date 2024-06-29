using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Game.Scripts.Systems.StarUpgradeSystem;
using ExternalPropertyAttributes;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class TrainManager : Singleton<TrainManager>
{
    private const string TRAIN_ARRIVE_NOTIFICATION = "TrainArriveNotification";
    private const string TRAIN_NEXT_COOLDOWN = "TrainCooldown";
    
    private List<NFProductContainer> ProductContainersList = new List<NFProductContainer>();

    private List<TrainOrderCanvas> availableOrderList = new List<TrainOrderCanvas>();

    public UnityEvent onCallTrain = new UnityEvent();
    public UnityEvent onSendTrain = new UnityEvent();
    public UnityEvent onTrainAtDestination = new UnityEvent();
    public UnityEvent onTrainSingleOrderComplete = new UnityEvent();
    public UnityEvent<TrainOrderCanvas> onSingleTrainOrderCanComplete = new UnityEvent<TrainOrderCanvas>();
    public UnityEvent<TrainOrderCanvas> onSingleTrainOrderCanNotComplete = new UnityEvent<TrainOrderCanvas>();

    private bool canProduce;
    private int totalOrder;
    private int currentOrder;
    
    //for total order at max time at secreen
    private int totalOrderAtScreen;
    private int totalMaxOrderAtScreen;
    private int totalShownOrder;
    
    //total amount of order completed for success train event
    private int totalCompleteOrder;
    private float orderCreateTime;

    private TrainData currentTrainDataData;

    private float currentCountDown;
    public bool trainActive;
    private bool trainCalledAlready;
    public TrainSpawner TrainSpawner { get; set; }

    //if train didnt complete within old city
    private bool isTrainPassesWithinCities;
    
    private void OnEnable()
    {
        //CallTrain();
        
        TryCallTrain();

        GetCurrentTrainData();
        
        totalMaxOrderAtScreen = currentTrainDataData.MaxOrderAtOneTime;
        
        //LevelManager.Instance.CityLoaded.AddListener(ResetOrderData);
        LevelManager.Instance.LevelExpended.AddListener(TryCallTrain);
        LevelManager.Instance.BeforeCityChanged.AddListener(CheckTrainStayWithinTwoCities);
        LevelManager.Instance.BeforeCityChanged.AddListener(ResetTrainDataWithCityChanged);
    }

    private void OnDisable() 
    {
        if (LevelManager.IsAvailable())
        {
            //LevelManager.Instance.CityLoaded.RemoveListener(ResetOrderData);
            LevelManager.Instance.LevelExpended.RemoveListener(TryCallTrain);
            LevelManager.Instance.BeforeCityChanged.RemoveListener(CheckTrainStayWithinTwoCities);
            LevelManager.Instance.BeforeCityChanged.RemoveListener(ResetTrainDataWithCityChanged);
        }
    }

    private void Start()
    {
        currentCountDown = GetCurrentCountDown();
    }

    private void GetCurrentTrainData()
    {
        int trainNumber = GetTrainNumber();

        if (trainNumber <= ConfigurationService.Configurations.TrainDatas.Count - 1)
            currentTrainDataData = ConfigurationService.Configurations.TrainDatas[trainNumber];
        else
            currentTrainDataData = ConfigurationService.Configurations.TrainDatas.Last();
    }

    public void CallTrain()
    {
        totalCompleteOrder = GetCompletedTrainOrder();
        totalShownOrder = totalCompleteOrder;
        GetCurrentTrainData();
        availableOrderList.Clear();
        onCallTrain?.Invoke();
        //trainActive = true;
    }

    public void SendTrain()
    {
        totalOrderAtScreen = 0;
        onSendTrain?.Invoke();
        trainActive = false;
        currentCountDown = GetCurrentCountDown();
        trainCalledAlready = false;
    }

    public void TryCallTrain()
    {
        if(CanSpawn())
            CallTrain();
        else
        {
            
        }
    }

    public bool CanSpawn()
    {
        LoadData();

        

        if (trainActive)
            return false;
        
        if (LevelManager.Instance.ActiveCityId == 0)
        {
            if (LevelManager.Instance.ActiveLevelId < 3)
            {
                return false;
            }
            else
            {
                if (GetRemainTrainTime() > 0)
                    return false;
                return true;
            }
        }
        else
        {
            if (GetRemainTrainTime() > 0)
                return false;
            return true;
        }
    }

    public void StartCreateOrders()
    {
        GetAllProducts();
        totalOrder = currentTrainDataData.totalTrainOrder;
        totalMaxOrderAtScreen = currentTrainDataData.MaxOrderAtOneTime;
        
        canProduce = true;
        orderCreateTime = currentTrainDataData.timeBetweenTrainOrder;

        TryToStartSecondTrainTutorial();
    }

    public void AddMeToList(TrainOrderCanvas _trainOrderCanvas)
    {
        if (availableOrderList.Contains(_trainOrderCanvas))
        {
            return;
        }
        
        availableOrderList.Add(_trainOrderCanvas);
    }

    public void DeleteMeFromList(TrainOrderCanvas _trainOrderCanvas)
    {
        availableOrderList.Remove(_trainOrderCanvas);
    }

    private void Update()
    {
        ControlCountdown();
        
        if (!canProduce)
            return;
        
        if(availableOrderList.Count < 1)
            return;
        
        if(totalOrderAtScreen >= totalMaxOrderAtScreen || totalShownOrder >= totalOrder)
            return;

        if (orderCreateTime > 0f)
        {
            orderCreateTime -= Time.deltaTime;
        }
        else
        {
            orderCreateTime = currentTrainDataData.timeBetweenTrainOrder;
            CreateTrainOrder();
            
            if (totalShownOrder >= totalOrder)
            {
                canProduce = false;
            }
        }
    }

    private void ControlCountdown()
    {
        if (currentCountDown > 0f)
        {
            currentCountDown -= Time.deltaTime;
        }
        else
        {
            if (!trainCalledAlready && GetRemainTrainTime() <= 0)
            {
                trainCalledAlready = true;
                TryCallTrain();
            }
        }
    }

    public float GetCurrentCountDown()
    {
        return GetRemainTrainTime();
    }

    [ContextMenu("CreateOrder")]
    private void CreateTrainOrder()
    {
        if (availableOrderList.Count < 1)
            return;

        var randomOrder = GetRandomOrder();
        if (randomOrder == null) return;

        var randomOne = Random.Range(0, availableOrderList.Count);
        availableOrderList[randomOne].SetOrder(randomOrder);
        availableOrderList.RemoveAt(randomOne);
        
        currentOrder++;
        totalOrderAtScreen++;
        totalShownOrder++;
    }

    private void GetAllProducts()
    {
        ProductContainersList = InteractionManager.Instance.GetAllAvailableInteractables<NFProductContainer>(InteractableType.NFProductContainer);
    }

    private Order GetRandomOrder()
    {
        GetAllProducts();
        if (ProductContainersList.Count <= 0) return null;

        var randomProductContainer = ProductContainersList[Random.Range(0, ProductContainersList.Count)];

        var itemType = randomProductContainer.GetObjectType();

        if (ProductContainersList.Count < 5 && !IsCityChangedTrain() && GetTrainNumber() > 0)
        {
            Order nextLevelOrder = GetNextLevelOrder();
            if (nextLevelOrder != null)
                return nextLevelOrder;
        }
        
        var productionCount = IdleUpgradeManager.Instance.GetUpgradedValue(randomProductContainer.InteractionID, ObjectDataType.ProductCount, randomProductContainer.ObjectData.GetBaseValue(ObjectDataType.ProductCount));
        var productionSpeed = randomProductContainer.GetInteractionTime(null);
        var perTickperMinute = 60f / productionSpeed;
        var GPM = productionCount * perTickperMinute;
        Vector2 randomRange = currentTrainDataData.RandomGPMorderRequest;
        var itemCount = GPM * Random.Range(randomRange.x , randomRange.y);

        return new Order(itemType, itemCount);
    }

    private Order GetNextLevelOrder()
    {
        int cityID = -1;
        foreach (var container in ProductContainersList)
        {
            foreach (var nextLevelTuples in TrainNextLevelRequestList)
            {
                if (container.GetObjectType() == nextLevelTuples.Item3)
                {
                    cityID = nextLevelTuples.Item1;
                    break;
                }
            }
        }

        if (cityID < 0)
            return null;

        var currentLevel = LevelManager.Instance.ActiveLevel.LevelId;

        List<(int, string, PoolType, int)> NextLevelRandomRequest = new List<(int, string, PoolType, int)>();
        foreach (var nextLevelData in TrainNextLevelRequestList)
        {
            if (nextLevelData.Item1 == cityID && nextLevelData.Item4 < currentLevel + 4)
            {
                NextLevelRandomRequest.Add(nextLevelData);
                
            }
        }

        if (NextLevelRandomRequest.Count > 0)
        {
            var randomNextOrderSelection = NextLevelRandomRequest[(Random.Range(0, NextLevelRandomRequest.Count))];
            
            var generator = InteractionManager.Instance.GetInteractableById(randomNextOrderSelection.Item2);

            if (generator != null)
            {
                var container = generator.GetComponent<NFProductContainer>();

                if (container != null)
                {
                    var productionCount2 = IdleUpgradeManager.Instance.GetUpgradedValue(container.InteractionID, ObjectDataType.ProductCount, container.ObjectData.GetBaseValue(ObjectDataType.ProductCount));
                    var productionSpeed2 = container.GetInteractionTime(null);
                    var perTickperMinute2 = 60f / productionSpeed2;
                    var GPM2 = productionCount2 * perTickperMinute2;
                    Vector2 randomRange2 = currentTrainDataData.RandomGPMorderRequest;
                    var itemCount2 = GPM2 * Random.Range(randomRange2.x , randomRange2.y);

                    return new Order(randomNextOrderSelection.Item3, itemCount2);
                }
            }
            
            var starUpgrade = StarUpgradeManager.Instance.GetUpgradeByInteractableId(randomNextOrderSelection.Item2);
            float productionSpeed = 10f;

            if (starUpgrade != null)
                productionSpeed = starUpgrade.BaseInteractionTime;
            var perTickperMinute = 60f / productionSpeed;
            
            var productionCount = StarUpgradeManager.Instance.GetUpgradedValueAtCurrentLevel(randomNextOrderSelection.Item2,
                ObjectDataType.ProductCount, new IdleNumber(5f , NumberDigits.Empty));

            var GPM = productionCount * perTickperMinute;
            Vector2 randomRange = currentTrainDataData.RandomGPMorderRequest;
            var itemCount = GPM * Random.Range(randomRange.x , randomRange.y);
            
            return new Order(randomNextOrderSelection.Item3, itemCount);
        }

        return null;
    }

    public void TrainOrderCompleted()
    {
        totalOrderAtScreen--;
        totalCompleteOrder++;
        
        AudioService.Play(AudioType.NFcollectItemSound);
        
        SaveSessionData(totalCompleteOrder);
        
        onTrainSingleOrderComplete?.Invoke();
        
        if (totalCompleteOrder >= totalOrder)
        {
            AllOrdersCompleted();    
        }
    }

    public void AllOrdersCompleted()
    {
        //onSendTrain?.Invoke();
        canProduce = false;
        if (!TutorialManager.Instance.IsTutorialCompleted(TutorialType.TrainCompleted))
            TutorialManager.Instance.TutorialComplete(TutorialType.TrainCompleted);

        //For tutorial train cooldown will get from game config
        if (GetTrainNumber() < 1)
        {
            SendTrainWithCooldown(currentTrainDataData.NextTrainCooldown);
        }
        else
        {
            var trainCooldown = RemoteConfigManager.Instance.GetIntConfig(TRAIN_NEXT_COOLDOWN, 14400);
            SendTrainWithCooldown(trainCooldown);
        }
        ResetOrderData();
        SetCityTrainToLevelTrain();
    }

    #region SaveLoadSystem
    private Dictionary<string, int> stateData = new Dictionary<string, int>();
    private bool loaded;
    private void LoadData()
    {
        if(!loaded)
            LoadSessionData();
    }
    
    private void LoadSessionData()
    {
        stateData = DataService.Instance.GetData< Dictionary<string, int> > (DataType.TUTORIAL_SIDECAR_STATE);
        loaded = true;
    }
    
    private int GetCompletedTrainOrder()
    {
        int completedTrainOrder;
        LoadSessionData();
        if (stateData.ContainsKey("TrainOrderCount"))
            completedTrainOrder = stateData["TrainOrderCount"];
        else
        {
            stateData.Add("TrainOrderCount" , 0);
            return 0;
        }

        return completedTrainOrder;
    }
    
    private void SaveSessionData(int totalCompletedTrainOrder)
    {
        if(!loaded)
            LoadSessionData();

        if (totalCompletedTrainOrder >= 0)
        {
            stateData["TrainOrderCount"] = totalCompletedTrainOrder;
        }
        
        DataService.Instance.SetData(DataType.TUTORIAL_SIDECAR_STATE, stateData , true);
    }

    private void ResetOrderData()
    {
        if (stateData.ContainsKey("TrainOrderCount"))
            stateData["TrainOrderCount"] = 0;

        currentOrder = 0;
        totalShownOrder = 0;
        
        totalOrderAtScreen = 0;
        availableOrderList.Clear();
        
        SaveSessionData(0);
    }

    #endregion

    public int GetTrainNumber()
    {
        LoadData();
        
        if (!stateData.ContainsKey("TrainNumber"))
        {
            stateData.Add("TrainNumber" , 0);
            return 0;
        }
        else
        {
            return stateData["TrainNumber"];
        }
    }

    private void SendTrainWithCooldown(int seconds)
    {
        var decreasedTime = VillageManager.Instance.GetUpgradedValue(GeneralUpgradeType.TrainTimeDecrease, seconds);
        seconds = (int)decreasedTime;
        
        if (!stateData.ContainsKey("TrainCooldown"))
            stateData.Add("TrainCooldown", seconds);
        else
            stateData["TrainCooldown"] = seconds;

        if (!stateData.ContainsKey("TrainLastCompleteTime"))
            stateData.Add("TrainLastCompleteTime", Timestamp.GetByDate(DateTime.Now));
        else
            stateData["TrainLastCompleteTime"] = Timestamp.GetByDate(DateTime.Now);

        stateData["TrainNumber"] = GetTrainNumber() + 1;

        if (RemoteConfigManager.Instance.GetBoolConfig("IsTrainNotifEnabled", true))
        {
            NotificationManager.Instance.CancelLocalNotification(TRAIN_ARRIVE_NOTIFICATION);
            NotificationManager.Instance.CreateLocalNotifications(TRAIN_ARRIVE_NOTIFICATION, "The train is about to arrive at the farm!", seconds, false);    
            //NotificationManager.Instance.CreateLocalNotifications(TRAIN_ARRIVE_NOTIFICATION, "The train is about to arrive at the farm!", 200, false);
            //Debug.Log("Notificationnn collectFreeChest 200 second");
        }
        
        SendTrain();
        
        DataService.Instance.SetData(DataType.TUTORIAL_SIDECAR_STATE, stateData , true);
    }

    private int cooldown = 0;
    private int remainingTrainTime = 0;
    private int trainLastCompleteTime = 0;
    private int GetRemainTrainTime()
    {
        LoadData();
        
        if (stateData.ContainsKey("TrainCooldown"))
            cooldown = stateData["TrainCooldown"];
        else
            cooldown = 0;

        if (stateData.ContainsKey("TrainLastCompleteTime"))
        {
            trainLastCompleteTime = stateData["TrainLastCompleteTime"];
            return  cooldown - (Timestamp.GetByDate(DateTime.Now) - trainLastCompleteTime);
        }
        return 0;
    }
    
    //ForFakeProduction
    //City ID , Container ID , PoolType , OpeningOrder
    private List<(int, string, PoolType , int)> TrainNextLevelRequestList = new List<(int, string, PoolType , int)>()
    {
        (0, "G1", PoolType.Wheat , 1),
        (0, "G2", PoolType.Flour , 2),
        (0, "G3", PoolType.Bread , 3),
        (0, "G4", PoolType.Feed, 4),
        (0, "G5", PoolType.Egg, 4),
        (1, "G1", PoolType.Corn , 1),
        (1, "G2", PoolType.CornFlour, 2),
        (1, "G3", PoolType.Cookie,3),
        (1, "G4", PoolType.CowFeed,4),
        (1, "G5", PoolType.Milk,4),
        (2, "G1", PoolType.Carrot,1),
        (2, "G2", PoolType.Cabbage,2),
        (2, "G3", PoolType.FarmerSoup,3),
        (2, "G4", PoolType.GoatMilk,4),
        (2, "G5", PoolType.Butter,4),
    };

    #region TrainCityChanges

    private void CheckTrainStayWithinTwoCities()
    {
        if (trainActive)
        {
            if (!stateData.ContainsKey("TrainTwoCities"))
            {
                stateData.Add("TrainTwoCities" , 1);
            }
            else
            {
                stateData["TrainTwoCities"] = 1;
            }

            isTrainPassesWithinCities = true;
        }
        else
        {
            isTrainPassesWithinCities = false;
        }
        
        SaveSessionData(-1);
    }

    private void ResetTrainDataWithCityChanged()
    {
        canProduce = false;
        availableOrderList.Clear();
        totalOrderAtScreen = 0;
        currentCountDown = 20f;
        trainActive = false;
        trainCalledAlready = false;
    }

    private void SetCityTrainToLevelTrain()
    {
        if (!stateData.ContainsKey("TrainTwoCities"))
        {
            stateData.Add("TrainTwoCities" , 0);
        }
        else
        {
            stateData["TrainTwoCities"] = 0;
        }

        isTrainPassesWithinCities = false;
    }

    private bool IsCityChangedTrain()
    {
        if (stateData.ContainsKey("TrainTwoCities"))
        {
            if (stateData["TrainTwoCities"] == 1)
                return true;
            else
                return false;
        }
        else
        {
            return false;
        }
    }

    #endregion

    public (int totalOrder, int totalOrderCompleted, int chestID) GetTrainInfo()
    {
        var chests = currentTrainDataData.TrainCompleteRewards.GetChests(PackageMod.Mod1);
        var chestID = ChestManager.Instance.GetChestTypeDataById(chests[0].ChestID);
        return (currentTrainDataData.totalTrainOrder, totalCompleteOrder, chests[0].ChestID);
    }

    private void TryToStartSecondTrainTutorial()
    {
        return;
        if (!TutorialManager.Instance.IsTutorialCompleted(TutorialType.TrainTutorialSecond) && GetTrainNumber() == 1)
        {
            TutorialManager.Instance.CheckTutorial(TutorialType.TrainTutorialSecond);
        }
    }
}

[Serializable]
public class TrainData
{
    [MinMaxSlider(0,10)]public Vector2 RandomGPMorderRequest;

    [Range(1, 20)] public int MaxOrderAtOneTime;
    [Range(1, 100)] public int totalTrainOrder;
    [Range(0, 5f)] public float timeBetweenTrainOrder;
    public int NextTrainCooldown = 300;
    public PackageContent TrainCompleteRewards;
}
