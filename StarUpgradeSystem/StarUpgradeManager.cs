using System;
using System.Collections.Generic;
using System.Linq;
using _Game.Scripts.Systems.WeeklyEventSystem;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Purchasing;

namespace _Game.Scripts.Systems.StarUpgradeSystem
{
    public class StarUpgradeManager : Singleton<StarUpgradeManager>, IExtraObjectUpgradeManager
    {
        // public static readonly UnityEvent<int, List<BonusUpgradeData>> OnStarUpgradePurchased = new UnityEvent<int, List<BonusUpgradeData>>();

        public UnityEvent OnProductMachineUpgraded = new UnityEvent();
        public UnityEvent OnUpgradeablePurchased = new UnityEvent();
        public UnityEvent OnMarketUnlocked = new UnityEvent();
        public UnityEvent<int> OnMarketCollected = new UnityEvent<int>();
        public UnityEvent<string> OnUpgradeablePurchasedWithID = new UnityEvent<string>();
        
        private const string CollectionPath = "Configurations/StarUpgradeCollection";
        private const string EventCollectionPath = "Configurations/EventStarUpgradeCollection";

        public StarUpgradeCollection StarUpgradeCollection => starUpgradeCollection;
        public StarUpgradeCollection EventStarUpgradeCollection => eventStarUpgradeCollection;

        private StarUpgradeCollection currentStarUpgradeCollection;
        private StarUpgradeCollection starUpgradeCollection;
        private StarUpgradeCollection eventStarUpgradeCollection;
        
        private Dictionary<string, Dictionary<PoolType,int>> purchasedUpgrades = new Dictionary<string, Dictionary<PoolType, int>>();

        private Dictionary<string, StarUpgradeBuildingLevelUpgradeableSave> upgradeableSaveDic = new Dictionary<string, StarUpgradeBuildingLevelUpgradeableSave>();

        private LevelStarUpgrades LevelStarUpgrades => currentStarUpgradeCollection.LevelStarUpgrades;

        

        #region Events
        public event Action<string> OnPreUpgrade; //arg0: upgrade id
        public event Action<string> OnPostUpgrade; //arg0: upgrade id
        #endregion
        private float lastSaveTime = 0;
        private float lastDataChangedTime = 0;
        private float forceSaveDuration = 5f; //5sn'de bir

        #region EventLeaderboard
        public static int TotalStartCountInEvent => totalStartCountInEvent;
        private static int totalStartCountInEvent;
        private float lastTotalStarCountSaveTime = 0;
        private float totalStarCountSaveDuration = 5f; //45sn'de bir
        private int lastSaveStarCount = 0;
        #endregion

        #region Public Functions

        public StarUpgradeData GetUpgradeByInteractableId(string interactableId)
        {
            return currentStarUpgradeCollection.GetStarUpgradeByInteractableId(interactableId);
        }

        public StarUpgradeData GetUpgradeByCollectablePoolType(PoolType collectablePoolType)
        {
            return currentStarUpgradeCollection.GetStarUpgradeByPoolType(collectablePoolType);
        }
        
        public StarUpgradeData GetUpgradeByCollectableId(int collectableId)
        {
            return currentStarUpgradeCollection.GetStarUpgradeByCollectableID(collectableId);
        }
        
        public CollectableObjectData GetObjectDataByCollectableId(int collectableId, PoolType poolType = PoolType.Undefined)
        {
            var starUpgradeData = GetUpgradeByCollectableId(collectableId);
            if (starUpgradeData == null) return null;

            var productType = poolType == PoolType.Undefined ? starUpgradeData.StarUpgradeProductionDataByPoolType.First().Key : poolType;
            return CollectableObjectService.GetCollectableObjectData(productType);
        }
        
        public CollectableObjectData GetObjectDataByInteractableId(string interactableId, PoolType poolType = PoolType.Undefined)
        {
            var starUpgradeData = GetUpgradeByInteractableId(interactableId);
            if (starUpgradeData == null) return null;

            var productType = poolType == PoolType.Undefined ? starUpgradeData.StarUpgradeProductionDataByPoolType.First().Key : poolType;
            return CollectableObjectService.GetCollectableObjectData(productType);
        }

        public int GetStarRequiredLevel(int starLevel, StarType starType = StarType.Purple)
        {
            return currentStarUpgradeCollection.GetStarRequiredLevel(starLevel, starType);
        }

        public int GetStarLevel(string interactableId, out StarType starType, PoolType poolType = PoolType.Undefined, bool isNextLevel = false)
        {
            var starUpgradeData = GetUpgradeByInteractableId(interactableId);
            var productType = poolType == PoolType.Undefined ? starUpgradeData.StarUpgradeProductionDataByPoolType.First().Key : poolType;

            var upgradeLevel = GetUpgradeLevel(interactableId, productType);
            return currentStarUpgradeCollection.GetStarLevel(isNextLevel ? upgradeLevel + 1 : upgradeLevel, out starType);
        }

        private int[] starLevels = new[] { 25, 50, 75, 100, 150, 200, 250, 350 };
        public int GetTotalStarLevel(int currentLevel)
        {
            for (int i = 0; i < starLevels.Length; i++)
            {
                if (currentLevel < starLevels[i])
                    return i;
            }

            return starLevels.Length;
        }

        public int GetMaxStarLevel(string interactableId, StarType starType)
        {
            return currentStarUpgradeCollection.GetMaxStarLevel(interactableId, starType);
        }

        #region UpgradeablePart

        public (List<UpgradeAbleCostTuple> data , int timer) GetUpgradeableCost(string interactionID)
        {
            var starUpgradeData = GetUpgradeByInteractableId(interactionID);
            var productType = starUpgradeData.StarUpgradeProductionDataByPoolType.First().Key;
            var currentUpgradeLevel = GetUpgradeLevel(interactionID, productType);
            var starLevel = currentStarUpgradeCollection.GetTotalStarLevel(currentUpgradeLevel);
            var data = currentStarUpgradeCollection.LevelStarUpgrades.GetCurrentUpgradeableCost(productType, starLevel);
            return data;
        }
        
        public bool NeedUpgrade(string interactableId)
        {
            var starUpgradeData = GetUpgradeByInteractableId(interactableId);
            var productType = starUpgradeData.StarUpgradeProductionDataByPoolType.First().Key;
            var currentUpgradeLevel = GetUpgradeLevel(interactableId, productType);
            if (currentUpgradeLevel % 25 == 0)
            {
                var starLevel = currentStarUpgradeCollection.GetTotalStarLevel(currentUpgradeLevel);
                if (upgradeableSaveDic.ContainsKey(interactableId))
                {
                    if (upgradeableSaveDic[interactableId].StarUpLeveledUp >= starLevel)
                    {
                        return true;
                    }
                    return false;
                }

                return false;
            }

            return true;
        }

        public void BuyUpgradeableLevel(string interactableID)
        {
            var starUpgradeData = GetUpgradeByInteractableId(interactableID);
            var productType = starUpgradeData.StarUpgradeProductionDataByPoolType.First().Key;
            var currentUpgradeLevel = GetUpgradeLevel(interactableID, productType);
            var starLevel = currentStarUpgradeCollection.GetTotalStarLevel(currentUpgradeLevel);

            /*if (starLevel == 2)
            {
                var container = InteractionManager.Instance.GetInteractableById(interactableID)
                    .GetComponent<NFProductContainer>();
                container.StartMarketConstruction();
            }*/

            if (upgradeableSaveDic.ContainsKey(interactableID))
            {
                upgradeableSaveDic[interactableID].StarUpLeveledUp = starLevel;
            }
            else
            {
                upgradeableSaveDic.Add(interactableID , new StarUpgradeBuildingLevelUpgradeableSave(starLevel));
            }

            DataService.Instance.SetData( DataType.STAR_UP_UPGRADEABLE_SAVE_DATA , upgradeableSaveDic , true);
            
            OnUpgradeablePurchased?.Invoke();
            OnUpgradeablePurchasedWithID?.Invoke(interactableID);
        }

        public int GetUpgradedMaxLevel(string interactionID)
        {
            /*var starUpgradeData = GetUpgradeByInteractableId(interactionID);
            var productType = starUpgradeData.StarUpgradeProductionDataByPoolType.First().Key;
            var starLevel = GetStarLevel(interactionID, out var starType, productType);*/
            var purchasedStar = GetBuildingUpgradedLevel(interactionID);
            return starUpgradeCollection.normalStarsRequiredLevels[purchasedStar - 1];
        }

        public int GetBuildingUpgradedLevel(string interacableID)
        {
            
            
            if (upgradeableSaveDic.ContainsKey(interacableID))
            {
                return upgradeableSaveDic[interacableID].StarUpLeveledUp + 1;
            }
            else
            {
                return 1;
            }
        }

        public static string GetRomainBuildingLevel(int level)
        {
            if (level == 1)
                return "I";
            if (level == 2)
                return "II";
            if (level == 3)
                return "III";
            if (level == 4)
                return "IV";
            if (level == 5)
                return "V";
            if (level == 6)
                return "VI";
            if (level == 7)
                return "VII";
            if (level == 8)
                return "VIII";
            if (level == 9)
                return "IX";
            if (level == 10)
                return "X";

            return "XX";
        }
        
        #endregion
        
        public bool CanUpgrade(string interactableId, PoolType poolType = PoolType.Undefined)
        {
            var starUpgradeData = GetUpgradeByInteractableId(interactableId);
            var productType = poolType == PoolType.Undefined ? starUpgradeData.StarUpgradeProductionDataByPoolType.First().Key : poolType;

            bool isMaxLevel = IsUpgradeLevelMax(interactableId, productType);
            
            if (isMaxLevel) return false;

            return CheckCanBuy(GetUpgradeCost(interactableId, productType));
        }

        public static bool CheckCanBuy(List<ProductCostTuple> productCostTuples)
        {
            foreach (var productCostTuple in productCostTuples)
            {
                //Debug.Log("<<<< elimizde:" + GetOwnedByType(productCostTuple).number + " istenen:" + productCostTuple.Cost.number + " bool:" + (GetOwnedByType(productCostTuple) < productCostTuple.Cost));
                if (GetOwnedByType(productCostTuple) < productCostTuple.Cost)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool CheckHaveCosts(List<UpgradeAbleCostTuple> costs)
        {
            bool doHave = true;

            foreach (var cost in costs)
            {
                if (cost.UpgradeAbleCostType == UpgradeAbleCostType.Currency)
                {
                    if (IdleExchangeService.GetIdleValue(cost.CurrencyType) < cost.CurrenyCost)
                    {
                        doHave = false;
                        break;
                    }
                }else if (cost.UpgradeAbleCostType == UpgradeAbleCostType.Product)
                {
                    if (cost.ProductCost > NFInventoryManager.Instance.GetCountInInventory(cost.PoolType))
                    {
                        doHave = false;
                        break;
                    }
                }
            }

            return doHave;
        }

        public static void PayCosts(List<UpgradeAbleCostTuple> costs)
        {
            foreach (var cost in costs)
            {
                if (cost.UpgradeAbleCostType == UpgradeAbleCostType.Currency)
                {
                    IdleExchangeService.DoExchange(cost.CurrencyType, -cost.CurrenyCost, out _, "UnlockUpgradeCost");
                }else if (cost.UpgradeAbleCostType == UpgradeAbleCostType.Product)
                {
                    NFInventoryManager.Instance.RemoveItemToInventory(cost.PoolType , cost.ProductCost);
                }
            }
        }

        public float GetUpgradedValue(string interactableId, ObjectDataType objectDataType, float baseValue, PoolType poolType = PoolType.Undefined, bool isNextLevel = false)
        {
            if (objectDataType == ObjectDataType.ExtraLevel || objectDataType == ObjectDataType.OrderTime || objectDataType == ObjectDataType.UpdateCost) return baseValue;

            var starUpgradeData = GetUpgradeByInteractableId(interactableId);
            var productType = poolType == PoolType.Undefined ? starUpgradeData.StarUpgradeProductionDataByPoolType.First().Key : poolType;

            var upgradeLevel = GetUpgradeLevel(interactableId, productType);
            if (isNextLevel) upgradeLevel++;

            var bonusUpgrades = GetBonusUpgradesList(interactableId, productType, objectDataType, isNextLevel);
            
            bool useBaseValue = (upgradeLevel == 1 && bonusUpgrades.Count == 0);
            if (useBaseValue) return baseValue;

            var filteredUpgrades = starUpgradeData.HasUpgradeFor(productType, objectDataType);
            
            return ApplyUpgrades(upgradeLevel, bonusUpgrades, baseValue, filteredUpgrades, objectDataType != ObjectDataType.ObjectCount);
        }

        public IdleNumber GetUpgradedValue(string interactableId, ObjectDataType objectDataType, IdleNumber baseValue, PoolType poolType = PoolType.Undefined, bool isNextLevel = false)
        {
            if (objectDataType == ObjectDataType.ExtraLevel || objectDataType == ObjectDataType.OrderTime || objectDataType == ObjectDataType.UpdateCost) return baseValue;
            var starUpgradeData = GetUpgradeByInteractableId(interactableId);
            var productType = poolType == PoolType.Undefined ? starUpgradeData.StarUpgradeProductionDataByPoolType.First().Key : poolType;

            var upgradeLevel = GetUpgradeLevel(interactableId, productType);
            if (isNextLevel) upgradeLevel++;

            var bonusUpgrades = GetBonusUpgradesList(interactableId, productType, objectDataType, isNextLevel);
            
            bool useBaseValue = (upgradeLevel == 1 && bonusUpgrades.Count == 0);
            if (useBaseValue) return baseValue;


            var filteredUpgrades = starUpgradeData.HasUpgradeFor(productType, objectDataType);

            if (objectDataType == ObjectDataType.ProductCount || objectDataType == ObjectDataType.UpdateCost)
                filteredUpgrades = true;
            
            return ApplyUpgrades(upgradeLevel, bonusUpgrades, baseValue, filteredUpgrades, objectDataType != ObjectDataType.ObjectCount);
        }

        public IdleNumber GetUpgradedValueAtCurrentLevel(string interactableId, ObjectDataType objectDataType, IdleNumber baseValue, PoolType poolType = PoolType.Undefined , int currentLevel = 25)
        {
            if (objectDataType == ObjectDataType.ExtraLevel || objectDataType == ObjectDataType.OrderTime || objectDataType == ObjectDataType.UpdateCost) return baseValue;
            var starUpgradeData = GetUpgradeByInteractableId(interactableId);
            var productType = poolType == PoolType.Undefined ? starUpgradeData.StarUpgradeProductionDataByPoolType.First().Key : poolType;

            var upgradeLevel = currentLevel;

            var bonusUpgrades = GetBonusUpgradesList(interactableId, productType, objectDataType, false);
            
            bool useBaseValue = (upgradeLevel == 1 && bonusUpgrades.Count == 0);
            if (useBaseValue) return baseValue;


            var filteredUpgrades = starUpgradeData.HasUpgradeFor(productType, objectDataType);
            
            return ApplyUpgrades(upgradeLevel, bonusUpgrades, baseValue, filteredUpgrades, objectDataType != ObjectDataType.ObjectCount);
        }

        public int GetUpgradeLevel(string interactableId, PoolType poolType = PoolType.Undefined, bool ignoreBoost = false)
        {
            var level = 1;
            if (IsUpgradePurchased(interactableId, poolType))
            {
                level = purchasedUpgrades[interactableId][poolType];
            }
            if(!ignoreBoost)
                level += (int)IdleUpgradeManager.Instance.GetUpgradedValue(interactableId, ObjectDataType.ExtraLevel, 0, poolType);
            return level;
        }

        public int GetMaxUpgradeLevel(string interactableId = "")
        {
            if(interactableId != "")
            {
                var starUpgradeData = GetUpgradeByInteractableId(interactableId);
                if (starUpgradeData != null && starUpgradeData.MaxUpgradeCount != -1)
                    return starUpgradeData.MaxUpgradeCount;
            }
            return LevelStarUpgrades.MaxUpgradeCount;
        }

        Dictionary<int, IdleNumber> starUpgradeCostCache = new Dictionary<int, IdleNumber>();

        public bool CanUpgradeUntilStarUpgrade(string containerID , PoolType activeProductType)
        {
            bool isMaxLevel = IsUpgradeLevelMax(containerID, activeProductType);
            var currentStarRequiredLevel = 2;
            StarType starType = StarType.Green;
            int currentStarLevel = GetStarLevel(containerID, out starType, activeProductType);
            bool isMaxStarLevel = (currentStarLevel == GetMaxStarLevel(containerID, starType));
            if (isMaxStarLevel)
            {
                currentStarRequiredLevel = GetStarRequiredLevel(1, starType + 1);
            }
            else
            {
                currentStarRequiredLevel = GetStarRequiredLevel(currentStarLevel + 1, starType);
            }
            int upgradableLevel = isMaxLevel ? 0 : StarUpgradeManager.Instance.GetMoneyEnoughUpgradeLevel(containerID, currentStarRequiredLevel, activeProductType);

            if (upgradableLevel >= currentStarRequiredLevel)
                return true;
            return false;
        }

        public List<ProductCostTuple> GetUpgradeCost(string interactableId, PoolType poolType = PoolType.Undefined)
        {
            var currentUpgradeLevel = GetUpgradeLevel(interactableId, poolType) - 1;
            var starUpgradeData = currentStarUpgradeCollection.GetStarUpgradeByInteractableId(interactableId);
            var productType = poolType == PoolType.Undefined ? starUpgradeData.StarUpgradeProductionDataByPoolType.First().Key : poolType;

            var productionData = starUpgradeData.StarUpgradeProductionDataByPoolType[productType];

            var result = productionData.StartingCostTuples;
            var upgradeMultiplier = productType.ToString().Contains("Coin") ? LevelStarUpgrades.UpgradeCostIncreaseMultiplierMarket : LevelStarUpgrades.UpgradeCostIncreaseMultiplier;

            //var starLevel = GetStarLevel(interactableId, out var starType, poolType);
            var starLevel = GetTotalStarLevel(currentUpgradeLevel + 1);
            
            List<ProductCostTuple> newCosts = new List<ProductCostTuple>();

            var starLevelCostTypeByProductType = LevelStarUpgrades.GetStarLevelCostTypeByProductType(productionData.PoolType);
            if(starLevelCostTypeByProductType != null)
            {
                var starLevelCostTypeByLevel = starLevelCostTypeByProductType.GetStarLevelCostTypeByLevel(starLevel);
                if(starLevelCostTypeByLevel != null)
                {
                    for(int i = 0; i < starLevelCostTypeByLevel.CostTypes.Count; i++)
                    {
                        //var productCostTuple = i < result.Count ? result[i] : result.Last();
                        var productCostTuple = new ProductCostTuple();
                        productCostTuple.ProductType = starLevelCostTypeByLevel.CostTypes[0];
                        productCostTuple.Cost = starLevelCostTypeByLevel.CostAmount;
                        newCosts.Add(new ProductCostTuple()
                        {
                            Cost = GetCostforType(interactableId, productCostTuple.Cost, currentUpgradeLevel,
                                upgradeMultiplier , true) * ResourceManager.Instance.GetStarUpgradeMultiplier(starLevelCostTypeByLevel.CostTypes[i], productCostTuple.ProductType),
                            CurrencyType = productCostTuple.CurrencyType,
                            ProductType = starLevelCostTypeByLevel.CostTypes[i]
                        });

                        if (newCosts[i].ProductType == PoolType.MarketCoin)
                        {
                            newCosts[i].ProductType = PoolType.Undefined;
                            newCosts[i].CurrencyType = CurrencyType.Coin;
                        }
                    }

                    return newCosts;
                }

            }

            foreach (var productCostTuple in result)
            {
                newCosts.Add(new ProductCostTuple()
                {
                    Cost = GetCostforType(interactableId, productCostTuple.Cost, currentUpgradeLevel,
                        upgradeMultiplier),
                    CurrencyType = productCostTuple.CurrencyType,
                    ProductType = productCostTuple.ProductType
                });
            }

            return newCosts;
        }

        public List<ProductCostTuple> GetResourcesList(string interactableId, PoolType poolType = PoolType.Undefined)
        {
            var starUpgradeData = currentStarUpgradeCollection.GetStarUpgradeByInteractableId(interactableId);
            var productType = poolType == PoolType.Undefined ? starUpgradeData.StarUpgradeProductionDataByPoolType.First().Key : poolType;

            return starUpgradeData.StarUpgradeProductionDataByPoolType[productType].Resources;
        }

        public bool LowerThanStarUpgrade(string interactableId)
        {
            return currentStarUpgradeCollection.GetStarUpgradeByInteractableId(interactableId).LowerThanStarUpgrade;
        }

        private StarType starType = StarType.Green;
        private PoolType productType;
        private int currentStarLevel = 0;
        private int previousStarRequiredLevel = 0;
        private IdleNumber GetCostforType(string interactableId ,IdleNumber  initialValue,int currentUpgradeLevel, float upgradeMultiplier , bool overrideCost = false)
        {
            
            if (overrideCost)
            {
                currentUpgradeLevel++;
                for (int i = 0; i < currentStarUpgradeCollection.normalStarsRequiredLevels.Count; i++)
                {
                    if (currentUpgradeLevel < currentStarUpgradeCollection.normalStarsRequiredLevels[i] && i > 0)
                    {
                        currentUpgradeLevel -= currentStarUpgradeCollection.normalStarsRequiredLevels[i - 1];
                        break;
                    }
                }
            }
            
            IdleNumber result = initialValue;
            while (currentUpgradeLevel > 100)
            {
                result *= Mathf.Pow(upgradeMultiplier, 100);
                currentUpgradeLevel -= 100;
            }
            
            result *= Mathf.Pow(upgradeMultiplier, currentUpgradeLevel);
            if (!EventManager.Instance.InEvent)
            {
                result *= (PlayerModeManager.Instance.GetActiveModeMultiplier() * LevelManager.Instance.ActiveLevelHarderingMultiplier);
            }
            // starUpgradeCostCache.Add(actualLevel, result / startResult);
            //todo buradaki cost managerın katkısıyla yanlış geliyor alttaki fonksiyona geçici olarak costcalculation bool u göndererek yaptım ama değişmesi gerekmekte
            result = IdleUpgradeManager.Instance.GetUpgradedValue(interactableId, ObjectDataType.UpdateCost, result);
            return result;
        }

        public int GetMaxLevel(string interactableId)
        {
            if (interactableId != "")
            {
                var starUpgradeData = GetUpgradeByInteractableId(interactableId);
                if (starUpgradeData != null && starUpgradeData.MaxUpgradeCount != -1)
                    return starUpgradeData.MaxUpgradeCount;
            }
            return LevelStarUpgrades.MaxUpgradeCount;
        }
        
        public int GetMoneyEnoughUpgradeLevel(string interactableId, int currentStarRequiredLevel, PoolType poolType = PoolType.Undefined)
        {
            var starUpgradeData = GetUpgradeByInteractableId(interactableId);
            var productType = poolType == PoolType.Undefined ? starUpgradeData.StarUpgradeProductionDataByPoolType.First().Key : poolType;

            var upgradeCosts = GetUpgradeCost(interactableId, productType);
            var currentUpgradeLevel = GetUpgradeLevel(interactableId, productType) - 1;
            var upgradeMultiplier = productType.ToString().Contains("Coin") ? LevelStarUpgrades.UpgradeCostIncreaseMultiplierMarket : LevelStarUpgrades.UpgradeCostIncreaseMultiplier;

            int minMoneyEnoughUpgrade = Int32.MaxValue;
            
            foreach (var upgradeCost in upgradeCosts)
            {
                int tmpMinUpgrade = GetUpgradeAvailableLevelforType(interactableId, currentUpgradeLevel, upgradeCost.Cost,
                    GetOwnedByType(upgradeCost), upgradeMultiplier, currentStarRequiredLevel);

                if (tmpMinUpgrade < minMoneyEnoughUpgrade)
                {
                    minMoneyEnoughUpgrade = tmpMinUpgrade;
                }
            }
            // starUpgradeCostCache.Add(actualLevel, result / startResult);
            return  minMoneyEnoughUpgrade;
        }

        private int GetUpgradeAvailableLevelforType(string interactableId ,int  currentUpgradeLevel , IdleNumber upgradeCost , IdleNumber ownedCoin , float upgradeMultiplier ,  int currentStarRequiredLevel )
        {
            int moneyEnoughUpgradeLevel = currentUpgradeLevel;
            IdleNumber totalReqCoin = new IdleNumber(0, NumberDigits.Empty);

            totalReqCoin += upgradeCost;
            if (ownedCoin < totalReqCoin) return moneyEnoughUpgradeLevel;

            ++moneyEnoughUpgradeLevel;
            
            do
            {
                ++moneyEnoughUpgradeLevel;
                upgradeCost *= upgradeMultiplier;
                upgradeCost = IdleUpgradeManager.Instance.GetUpgradedValue(interactableId, ObjectDataType.UpdateCost, upgradeCost);
                totalReqCoin += upgradeCost;
            } while (moneyEnoughUpgradeLevel <= currentStarRequiredLevel && ownedCoin >= totalReqCoin);

            return moneyEnoughUpgradeLevel;
        }

        public IdleNumber GetCostForLevel(string interactableId, int currentUpgradeLevel, int targetUpgradeLevel, IdleNumber upgradeCost)
        {
            var starUpgradeData = GetUpgradeByInteractableId(interactableId);
            var productType = starUpgradeData.StarUpgradeProductionDataByPoolType.First().Key;

            var upgradeMultiplier = productType.ToString().Contains("Coin") ? LevelStarUpgrades.UpgradeCostIncreaseMultiplierMarket : LevelStarUpgrades.UpgradeCostIncreaseMultiplier;

            int moneyEnoughUpgradeLevel = currentUpgradeLevel;
            IdleNumber totalReqCoin = new IdleNumber(0, NumberDigits.Empty);

            do
            {
                upgradeCost *= upgradeMultiplier;
                upgradeCost = IdleUpgradeManager.Instance.GetUpgradedValue(interactableId, ObjectDataType.UpdateCost, upgradeCost);
                totalReqCoin += upgradeCost;
                ++moneyEnoughUpgradeLevel;

            } while (moneyEnoughUpgradeLevel <= targetUpgradeLevel);

            return totalReqCoin;
        }

        private static IdleNumber GetOwnedByType(ProductCostTuple productCostTuple)
        {
            if (productCostTuple.ProductType == PoolType.Undefined)
            {
                if (productCostTuple.CurrencyType == CurrencyType.Coin)
                {
                    return IdleExchangeService.GetIdleValue(CurrencyService.ActiveCurrencyType);
                }
                return IdleExchangeService.GetIdleValue(productCostTuple.CurrencyType);
            }
            else
            {
                return NFInventoryManager.Instance.GetCountInInventory(productCostTuple.ProductType);

            }
        }

        public (List<UpgradeAbleCostTuple> data, int timer) GetUnlockCostData(string interactableID)
        {
            var starUpgrade = currentStarUpgradeCollection.GetStarUpgradeByInteractableId(interactableID);
            if (starUpgrade == null)
            {
                Debug.Log("starUpgrade is null!!");
                return new(new List<UpgradeAbleCostTuple>(), 0);

                
            }
            
            List<UpgradeAbleCostTuple> costList = new List<UpgradeAbleCostTuple>();
            int timer = starUpgrade.UnlockCostData.Timer;

            foreach (var cost in starUpgrade.UnlockCostData.Costs)
            {
                costList.Add(cost);
            }

            return (costList, timer);
        }
        
        public List<ProductCostTuple> GetUnlockCost(string interactableId)
        {
            var starUpgrade = currentStarUpgradeCollection.GetStarUpgradeByInteractableId(interactableId);
            if (starUpgrade == null)
            {
                Debug.Log("starUpgrade is null!!");
                return new List<ProductCostTuple>()
                {
                    new ProductCostTuple()
                    {
                        Cost = new IdleNumber(0, NumberDigits.Empty), ProductType = PoolType.Undefined,
                        CurrencyType = CurrencyType.Coin
                    }
                };
            }

            List<ProductCostTuple> newProductCostTuple = new List<ProductCostTuple>();
            
            foreach (var cost in starUpgrade.UnlockCostTuples)
            {
                newProductCostTuple.Add(new ProductCostTuple()
                {
                    Cost =  cost.Cost,
                    ProductType = cost.ProductType,
                    CurrencyType = cost.CurrencyType
                });
            }

            return newProductCostTuple;
        }

        private void Start()
        {
            //GetUpgradeableCost(PoolType.Walker);
        }
        
        public List<ProductCostTuple> UpdateUnlockCost(List<ProductCostTuple> baseCost)
        {
            List<ProductCostTuple> newCostTuples = new List<ProductCostTuple>();

            foreach (var productCostTuple in baseCost)
            {
                newCostTuples.Add(new ProductCostTuple()
                {
                    Cost = productCostTuple.Cost * (PlayerModeManager.Instance.GetActiveModeMultiplier() * LevelManager.Instance.ActiveLevelHarderingMultiplier),
                    ProductType = productCostTuple.ProductType,
                    CurrencyType = productCostTuple.CurrencyType
                });
            }

            return newCostTuples;
        }

        public List<BonusUpgradeData> BuyUpgrade(string interactableId, PoolType poolType = PoolType.Undefined, int upgradeLevel = 1)
        {

            if (!CanUpgrade(interactableId, poolType))
            {
                Debug.Log($"{interactableId} cannot upgrade!!");
                return null;
            }
            
            OnPreUpgrade?.Invoke(interactableId);
            var upgradeCost = GetUpgradeCost(interactableId, poolType);

            foreach (var cost in upgradeCost)
            {
                SpendByType(cost);
            }

            var starUpgradeData = GetUpgradeByInteractableId(interactableId);
            var productType = poolType == PoolType.Undefined ? starUpgradeData.StarUpgradeProductionDataByPoolType.First().Key : poolType;

            var oldStarLevel = GetStarLevel(interactableId, out StarType oldStarType, productType);
            IncreaseUpgradeLevel(interactableId, productType, upgradeLevel);
            var newStarLevel = GetStarLevel(interactableId, out StarType newStarType, productType);
            SaveData();
            
            var bonusUpgrades = GetEarnedBonusUpgrades(interactableId, productType, oldStarLevel, oldStarType, newStarLevel, newStarType);
            // OnStarUpgradePurchased.Invoke(upgradeId, bonusUpgrades);
            OnPostUpgrade?.Invoke(interactableId);
            if (newStarLevel != oldStarLevel)
            {
                //Debug.Log("Burası yıldızın arttığı yer");
                if(EventManager.Instance.InEvent)
                    totalStartCountInEvent++;

                newStarLevel = GetTotalStarLevel(interactableId, newStarLevel, newStarType); 
                TrackingService.TrackEvent(TrackType.StarUpgraded, interactableId, newStarLevel);
            }
            
            TrackingService.TrackEvent(TrackType.UpgradeStore, interactableId, upgradeLevel);

            return bonusUpgrades;
        }

        private void SpendByType(ProductCostTuple productCostTuple)
        {
            if (productCostTuple.ProductType == PoolType.Undefined)
            {
                if (productCostTuple.CurrencyType == CurrencyType.Coin)
                {
                    IdleExchangeService.DoExchange(CurrencyService.ActiveCurrencyType, -productCostTuple.Cost, out _, "StarUpgrade");
                    return;
                }
                IdleExchangeService.DoExchange(productCostTuple.CurrencyType, -productCostTuple.Cost, out _, "StarUpgrade");
            }
            else
            {
                //bunu yapmamızın sebebi upgrade panel costunda 2 gösteriyor ama arka plande 1.3f gibi bir değer yani oyuncu 2 görüyor ama sistem onu 1 eksiltiyor o yüzden
                //eğer numberdigits empty ise oyuncu ne görüyorsa onu düşürüyoruz sistemden
                var costRounded = productCostTuple.Cost;

                if (costRounded.digits == NumberDigits.Empty)
                    costRounded.number = (int)Math.Ceiling(costRounded.number);
                
                NFInventoryManager.Instance.RemoveItemToInventory(productCostTuple.ProductType , costRounded);
            }
        }
        
        

        public bool IsUpgradeLevelMax(string interactableId, PoolType poolType = PoolType.Undefined)
        {
            return IsUpgradePurchased(interactableId, poolType) && GetUpgradeLevel(interactableId, poolType, true) >= GetMaxUpgradeLevel(interactableId);
        }
        #endregion

        #region Private Functions
    
        #region Init

        public void ReloadData()
        {
            Load();
        }

        private void ReloadToExpend()
        {
            Debug.Log("reload to expend");
            currentStarUpgradeCollection = EventManager.Instance.InEvent ? eventStarUpgradeCollection : starUpgradeCollection;
            currentStarUpgradeCollection.LoadData();
        }

        #endregion
    
        #region Save & Load
        private void Awake()
        {
            Load();
            LoadStarCountData();
            LevelManager.Instance.RevisitEvent.AddListener(ReloadData);
            LevelManager.Instance.CityLoaded.AddListener(ReloadData);
            LevelManager.Instance.LevelExpended.AddListener(ReloadToExpend);
            LevelManager.Instance.OnNewLevel.AddListener(ResetUpgradeableData);
            LoadUpgradeableData();
        }

        private void Update()
        {
            if(Time.time >= lastSaveTime + forceSaveDuration && lastDataChangedTime > lastSaveTime)
            {
                //Debug.Log("forced save data");
                lastSaveTime = Time.time;
                SaveData(true);
            }
            if(Time.time >= lastTotalStarCountSaveTime + totalStarCountSaveDuration)
            {
                UpdateStarCountData(true);
            }
        }

        private void OnDestroy()
        {
            if (LevelManager.IsAvailable())
            {
                LevelManager.Instance.RevisitEvent.RemoveListener(ReloadData);
                LevelManager.Instance.CityLoaded.RemoveListener(ReloadData);
                LevelManager.Instance.LevelExpended.RemoveListener(ReloadToExpend);
                LevelManager.Instance.OnNewLevel.RemoveListener(ResetUpgradeableData);
            }
        }
        
        private void Load()
        {
            starUpgradeCollection ??= StarUpgradeCollection.LoadCollection(CollectionPath);
            eventStarUpgradeCollection ??= StarUpgradeCollection.LoadCollection(EventCollectionPath);

            /*
            foreach (var item in starUpgradeCollection.cityStarUpgradesList)
            {
                foreach (var item1 in item.levelStarUpgradesList)
                {
                    var _LevelStarUpgradesNew = new LevelStarUpgrades()
                    {
                        StarUpgrades = item1.StarUpgrades,
                        Level = item1.Level,
                        MaxUpgradeCount = item1.MaxUpgradeCount,
                        UpgradeCostIncreaseMultiplier = item1.UpgradeCostIncreaseMultiplier,
                        UpgradeMultiplier = item1.UpgradeMultiplier
                    };
                    UnityEditor.EditorUtility.SetDirty(_LevelStarUpgradesNew);
                    UnityEditor.AssetDatabase.CreateAsset(_LevelStarUpgradesNew, "Assets/_Game/---Data---/StarUpgrades/City_" + item.CityId+"/city_" + item.CityId + "_level_SU_" + item1.Level + ".asset");
                }
            }

            foreach (var item in eventStarUpgradeCollection.cityStarUpgradesList)
            {
                foreach (var item1 in item.levelStarUpgradesList)
                {
                    var _LevelStarUpgradesNew = new LevelStarUpgrades()
                    {
                        StarUpgrades = item1.StarUpgrades,
                        Level = item1.Level,
                        MaxUpgradeCount = item1.MaxUpgradeCount,
                        UpgradeCostIncreaseMultiplier = item1.UpgradeCostIncreaseMultiplier,
                        UpgradeMultiplier = item1.UpgradeMultiplier
                    };
                    UnityEditor.EditorUtility.SetDirty(_LevelStarUpgradesNew);
                    UnityEditor.AssetDatabase.CreateAsset(_LevelStarUpgradesNew, "Assets/_Game/---Data---/StarUpgrades/City_" + item.CityId + "/city_" + item.CityId + "_level_SU_" + item1.Level + ".asset");
                }
            }
            */
            currentStarUpgradeCollection = EventManager.Instance.InEvent ? eventStarUpgradeCollection : starUpgradeCollection;
            currentStarUpgradeCollection.LoadData();
            
            List<PoolType> obects = new List<PoolType>();
            var allPooltypes = currentStarUpgradeCollection.GetAllStarUpgradePoolTypes();
            foreach (var item in allPooltypes)
            {
                if(item.Key != PoolType.Undefined)
                    obects.Add(item.Key);
            }
            PoolingSystem.Instance.AddBatch(obects,3);
            starUpgradeCacheFloat.Clear();
            starUpgradeCacheIdle.Clear();
            starUpgradeCostCache.Clear();
            LoadPurchasedUpgrades();
            
        }

        private void LoadStarCountData()
        {
            var stateData = DataService.Instance.GetData<Dictionary<StateType, int>>(DataType.STATE);
            if (stateData != null && stateData.ContainsKey(StateType.TotalStarCountInEvent))
                totalStartCountInEvent = stateData[StateType.TotalStarCountInEvent];
            else
                totalStartCountInEvent = 0;

            lastSaveStarCount = -1;
        }

        public static void ResetTotalStartCountInEvent()
        {
            var stateData = DataService.Instance.GetData<Dictionary<StateType, int>>(DataType.STATE);
            stateData[StateType.TotalStarCountInEvent] = 0;
            DataService.Instance.SetData(DataType.STATE, stateData, true);
            totalStartCountInEvent = 0;
        }

        public void UpdateStarCountData(bool isUpdate)
        {
            lastTotalStarCountSaveTime = Time.time;

            var stateData = DataService.Instance.GetData<Dictionary<StateType, int>>(DataType.STATE);
            stateData[StateType.TotalStarCountInEvent] = TotalStartCountInEvent;

            if(isUpdate)
                UpdateLeaderboardData(stateData);

            DataService.Instance.SetData(DataType.STATE, stateData, true);
        }

        public void UpdateLeaderboardData(Dictionary<StateType, int> stateData)
        {
            if (stateData.ContainsKey(StateType.EventPlayDuration) && lastSaveStarCount != TotalStartCountInEvent)
            {
                var totalStarCount = TotalStartCountInEvent;

                var metaData = DataService.Instance.GetData<Dictionary<MetaDataType, string>>(DataType.METADATA);
                if (metaData != null && metaData.ContainsKey(MetaDataType.ShopName) &&
                    metaData[MetaDataType.ShopName] == ConfigurationService.Configurations.CheaterName)
                {
                    totalStarCount /= 2;
                }

                var points = (1000000 - (stateData[StateType.EventPlayDuration] * 60 + (int)(Time.time % 60)) + (totalStarCount * 1000000));
                LeaderboardManager.Instance.UpdateLeaderboard(LeaderboardManager.Instance.GetEventStatisticType(), points);
                lastSaveStarCount = TotalStartCountInEvent;
            }
        }

        private void LoadPurchasedUpgrades()
        {
            // Debug.Log("mode: " + PlayerModeManager.Instance.GetActiveMode());
            purchasedUpgrades.Clear();
            var upgrades = DataService.Instance.GetData<Dictionary<string, Dictionary<PoolType,int>>>(GetDataType());
            if (upgrades == null) return;
            foreach (var upgrade in upgrades)
            {
                purchasedUpgrades.Add(upgrade.Key, upgrade.Value);
            }
        }

        private void LoadUpgradeableData()
        {
            upgradeableSaveDic.Clear();
            var upgradeableData = DataService.Instance.GetData<Dictionary<string, StarUpgradeBuildingLevelUpgradeableSave>>(DataType.STAR_UP_UPGRADEABLE_SAVE_DATA);
            

            foreach (var data in upgradeableData)
            {
                upgradeableSaveDic.Add(data.Key , data.Value);
            }
        }

        private void ResetUpgradeableData()
        {
            upgradeableSaveDic.Clear();
            
            DataService.Instance.SetData( DataType.STAR_UP_UPGRADEABLE_SAVE_DATA , upgradeableSaveDic , true);
        }

        public List<BonusUpgradeData> LoadPurchasedUpgradeWithCheat(string id, Dictionary<PoolType,int> upgrade)
        {
            var productType = upgrade.Keys.First();

            var oldStarLevel = GetStarLevel(id, out StarType oldStarType, productType);
            IncreaseUpgradeLevel(id, productType, upgrade[productType]);
            var newStarLevel = GetStarLevel(id, out StarType newStarType, productType);

            var bonusUpgrades = GetEarnedBonusUpgrades(id, productType, oldStarLevel, oldStarType, newStarLevel, newStarType);

            SaveData(true);

            return bonusUpgrades;
        }

        private void SaveData(bool isForceSave = false)
        {
            //if(!isForceSave) Debug.Log("save data");
            lastDataChangedTime = Time.time;
            var upgrades = new Dictionary<string, Dictionary<PoolType,int>>();
            
            foreach (var upgrade in purchasedUpgrades)
            {
                upgrades.Add(upgrade.Key, upgrade.Value);
            }
            
            DataService.Instance.SetData(GetDataType(), upgrades, isForceSave);
        }

        private static DataType GetDataType()
        {
            return EventManager.Instance.InEvent ? DataType.EVENT_STAR_UPGRADE : PlayerModeManager.Instance.GetActiveStarUpgrade();
        }
        #endregion

        #region Get & Has & Is
    
        private Dictionary<int, BonusUpgradeData>  GetBonusUpgradesList(string interactableId, PoolType poolType, ObjectDataType objectDataType, bool isNextLevel = false)
        {
            var targetBonusUpgrades = new Dictionary<int, BonusUpgradeData>();
            var starUpgradeData = GetObjectDataByInteractableId(interactableId);
            var bonusUpgrades = GetEarnedBonusUpgrades(interactableId, starUpgradeData.PoolType, - 1, GetStarLevel(interactableId, out StarType starType, poolType, isNextLevel), starType);

            foreach (var bonusUpgrade in bonusUpgrades)
            {
                if (bonusUpgrade.TargetObjectValue == objectDataType)
                {
                    var level = bonusUpgrade.StarLevel;
                    bool isGreen = false;
                    if (level > currentStarUpgradeCollection.normalStarsRequiredLevels.Count)
                    {
                        level -= currentStarUpgradeCollection.normalStarsRequiredLevels.Count;
                        isGreen = true;
                    }
                    var reqLevel = isGreen ? currentStarUpgradeCollection.greenStarsRequiredLevels[level - 1] : currentStarUpgradeCollection.normalStarsRequiredLevels[level - 1];

                    targetBonusUpgrades.Add(reqLevel, bonusUpgrade);
                }
            }
       
            return targetBonusUpgrades;
        }    
        
        private List<BonusUpgradeData> GetEarnedBonusUpgrades(string interactionId, PoolType poolType, int oldStarLevel, int newStarLevel, StarType starType)
        {
            newStarLevel = GetTotalStarLevel(interactionId, newStarLevel, starType);
            return currentStarUpgradeCollection.GetEarnedBonusUpgrades(interactionId, poolType, oldStarLevel, newStarLevel);
        }
        
        private List<BonusUpgradeData> GetEarnedBonusUpgrades(string interactionId, PoolType poolType, int oldStarLevel, StarType oldStarType, int newStarLevel, StarType newStarType)
        {
            if (oldStarType != newStarType)
            {
                // Normal Star => Green Star
                newStarLevel = GetTotalStarLevel(interactionId, newStarLevel, newStarType);
            }
            return currentStarUpgradeCollection.GetEarnedBonusUpgrades(interactionId, poolType, oldStarLevel, newStarLevel);
        }

        public Color GetStarColor(StarType starType)
        {
            return currentStarUpgradeCollection.GetStarColor(starType);
        }
        
        public Color GetStarBGColor(StarType starType)
        {
            return currentStarUpgradeCollection.GetStarBGColor(starType);
        }
        
        public int GetTotalStarLevel(string objectId, PoolType poolType)
        {
            int starLevel = GetStarLevel(objectId, out StarType starType, poolType);
            int totalStarLevel = GetTotalStarLevel(objectId, starLevel, starType);
            return totalStarLevel;
        }

        /// <summary>
        /// Yeşil yıldızların sayısı ile mor yıldızlar toplanarak döndürülecek.
        /// Yesil 1 ise => 6 (5 tanesi mor yıldızdan)
        /// Yesil 3 ise => 8 (5 tanesi mor yıldızdan)
        /// Yeşil değil ve 3 => 3 (Mor olanlar için kendi değeri)
        /// </summary>
        /// <param name="starLevel"></param>
        /// <param name="starType"></param>
        /// <returns></returns>
        private int GetTotalStarLevel(string objectId, int starLevel, StarType starType)
        {
            foreach (int sType in Enum.GetValues(typeof(StarType)))  
            {  
                if(sType < (int)starType)
                    starLevel += GetMaxStarLevel(objectId, (StarType)sType);
            } 
            
            return starLevel;
        }

        private bool IsUpgradePurchased(string objectId, PoolType poolType)
        {
            return purchasedUpgrades.ContainsKey(objectId) && purchasedUpgrades[objectId].ContainsKey(poolType);
        }

        #endregion

        #region Other


        Dictionary<int, Dictionary<bool, float>> starUpgradeCacheFloat = new Dictionary<int, Dictionary<bool, float>>();

        private float ApplyUpgrades(int upgradeLevel, Dictionary<int, BonusUpgradeData> bonusUpgrades, float baseValue, bool applyMultiplier, bool additionUpgrade)
        {
            var upgradeMultiplier = LevelStarUpgrades.UpgradeMultiplier;
            var upgradeAddition = LevelStarUpgrades.UpgradeAddition;
            var upgradeAdditionMultiplier = LevelStarUpgrades.UpgradeAdditionMultiplier;

            var upgradedValue = baseValue;
            var actualLevel = upgradeLevel;
           
            float finalMultiplier = (applyMultiplier ? upgradeMultiplier : 1f);
            float finalAddition = (applyMultiplier ? upgradeAddition : 0f);

            if (upgradeLevel > 250)
            {
                foreach (var bonusUpgrade in bonusUpgrades)
                {
                    if (bonusUpgrade.Key <= upgradeLevel)
                    {
                        upgradedValue = ApplyBonusUpgrades(bonusUpgrade.Value, upgradedValue);
                        if (additionUpgrade)
                        {
                            upgradeAddition = ApplyBonusUpgrades(bonusUpgrade.Value, upgradeAddition);
                            upgradeAddition *= upgradeAdditionMultiplier;
                        }
                    }
                }

                if (applyMultiplier)
                {
                    upgradedValue *= Mathf.Pow(upgradeMultiplier, upgradeLevel);
                    upgradedValue += upgradeAddition * upgradeLevel * upgradeAdditionMultiplier;
                }

                return upgradedValue;
            }

            for (int i = 0; i < (upgradeLevel - 1); i++)
            {
                if (bonusUpgrades.ContainsKey(i + 2))
                {
                    if (additionUpgrade)
                    {
                        finalAddition = ApplyBonusUpgrades(bonusUpgrades[i + 2], finalAddition);
                        finalAddition *= upgradeAdditionMultiplier;
                    }
                }
            }

            for (int i = 0; i < (upgradeLevel - 1); i++)
            {
                upgradedValue *= finalMultiplier;
                upgradedValue += finalAddition;

                if (applyMultiplier)
                {
                    if ((upgradedValue) - Mathf.Floor(upgradedValue) >= 0.4f)
                    {
                        upgradedValue = Mathf.Floor(upgradedValue) + 1f;
                    }
                    else
                    {
                        upgradedValue = Mathf.Floor(upgradedValue);
                    }
                }
            }

            for (int i = 0; i < (upgradeLevel - 1); i++)
            {
                if (bonusUpgrades.ContainsKey(i + 2))
                {
                    upgradedValue = ApplyBonusUpgrades(bonusUpgrades[i + 2], upgradedValue);
                }
            }/*
            if (!starUpgradeCacheFloat.ContainsKey(actualLevel))
            {
                starUpgradeCacheFloat.Add(actualLevel, new Dictionary<bool, float>());
            }
            starUpgradeCacheFloat[actualLevel].Add(applyMultiplier, (upgradedValue / baseValue));
            */
            return upgradedValue;
        }
    
        private float ApplyBonusUpgrades(BonusUpgradeData bonusUpgrade, float baseValue)
        {
            var upgradedValue = baseValue;
                upgradedValue *= bonusUpgrade.BonusMultiplier;
                upgradedValue += bonusUpgrade.BonusAddition;
            return upgradedValue;
        }

        Dictionary<int, Dictionary<bool, IdleNumber>> starUpgradeCacheIdle = new Dictionary<int, Dictionary<bool, IdleNumber>>();


        private IdleNumber ApplyUpgrades(int upgradeLevel, Dictionary<int, BonusUpgradeData> bonusUpgrades, IdleNumber baseValue, bool applyMultiplier, bool additionUpgrade)
        {
            var upgradeMultiplier = LevelStarUpgrades.UpgradeMultiplier;
            var upgradeAddition = LevelStarUpgrades.UpgradeAddition;
            var upgradeAdditionMultiplier = LevelStarUpgrades.UpgradeAdditionMultiplier;

            var upgradedValue = baseValue;
            var actualLevel = upgradeLevel;

            if (upgradeLevel > 250)
            {
                int realLevel = upgradeLevel;
                
                foreach (var bonusUpgrade in bonusUpgrades)
                {
                    if (bonusUpgrade.Key <= realLevel)
                    {
                        upgradedValue = ApplyBonusUpgrades(bonusUpgrade.Value, upgradedValue);
                        if (additionUpgrade)
                        {
                            upgradeAddition = ApplyBonusUpgrades(bonusUpgrade.Value, upgradeAddition);
                            upgradeAddition *= upgradeMultiplier;
                        }
                    }
                }
                
                if (applyMultiplier)
                {
                    while (upgradeLevel > 100)
                    {
                        upgradedValue *= Mathf.Pow(upgradeMultiplier, 100);
                        upgradedValue += upgradeAddition * 100 * upgradeAdditionMultiplier;
                        upgradeLevel -= 100;
                    }
                    upgradedValue *= Mathf.Pow(upgradeMultiplier, upgradeLevel);
                    upgradedValue += upgradeAddition * upgradeLevel * upgradeAdditionMultiplier;
                }

                return upgradedValue;
            }

            for (int i = 0; i < (upgradeLevel - 1); i++)
            {
                upgradedValue *= (applyMultiplier ? upgradeMultiplier : 1f);
                upgradedValue += (applyMultiplier ? upgradeAddition : 0f);

                if (bonusUpgrades.ContainsKey(i + 2))
                {
                    upgradedValue = ApplyBonusUpgrades(bonusUpgrades[i + 2], upgradedValue);
                    if (additionUpgrade)
                    {
                        upgradeAddition = ApplyBonusUpgrades(bonusUpgrades[i + 2], upgradeAddition);
                        upgradeAddition *= upgradeAdditionMultiplier;
                    }
                }

                /*if (applyMultiplier)
                {
                    float floating = (upgradedValue.Number) - Mathf.Floor(upgradedValue.Number);
                    if (floating >= 0.4f)
                    {
                        upgradedValue += (1 - floating);
                    }
                    else
                    {
                        upgradedValue += -floating;
                    }
                }*/
            }

            for (int i = 0; i < (upgradeLevel - 1); i++)
            {
                if (bonusUpgrades.ContainsKey(i + 2))
                {
                   // upgradedValue = ApplyBonusUpgrades(bonusUpgrades[i + 2], upgradedValue);
                }
            }
            /*
            if (!starUpgradeCacheIdle.ContainsKey(actualLevel))
            {
                starUpgradeCacheIdle.Add(actualLevel, new Dictionary<bool, IdleNumber>());
            }
            starUpgradeCacheIdle[actualLevel].Add(applyMultiplier, (upgradedValue / baseValue));
            */
            return upgradedValue;
        }

        private IdleNumber ApplyBonusUpgrades(BonusUpgradeData bonusUpgrade, IdleNumber baseValue)
        {
            var upgradedValue = baseValue;
                upgradedValue *= bonusUpgrade.BonusMultiplier;
                upgradedValue += bonusUpgrade.BonusAddition;
            return upgradedValue;
        }

        private void IncreaseUpgradeLevel(string interactableId, PoolType poolType, int upgradeLevel = 1)
        {
            if (!purchasedUpgrades.ContainsKey(interactableId))
            {
                // Debug.Log($"{objectId} not in the dictionary!!");
                purchasedUpgrades[interactableId] = new Dictionary<PoolType, int>();
            }

            if (!purchasedUpgrades[interactableId].ContainsKey(poolType))
            {
                purchasedUpgrades[interactableId].Add(poolType, 1);
            }

            purchasedUpgrades[interactableId][poolType] += upgradeLevel;

            if (IsUpgradeLevelMax(interactableId, poolType))
            {
                // Debug.Log("Upgrade Level >= MAX!!");
                purchasedUpgrades[interactableId][poolType] = GetMaxLevel(interactableId);
            }
        }

        #endregion
    
        #endregion
    }
}

public enum StarType
{
    Purple = 1,
    Green = 2,
    Yellow = 3,
    Red = 4,
}

[Serializable]
public class StarUpgradeBuildingLevelUpgradeableSave
{
    public int StarUpLeveledUp;

    public StarUpgradeBuildingLevelUpgradeableSave(int starUpLeveledUp)
    {
        StarUpLeveledUp = starUpLeveledUp;
    }
}
