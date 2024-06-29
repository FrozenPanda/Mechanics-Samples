using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Game.Scripts.Systems.StarUpgradeSystem
{
    [CreateAssetMenu(menuName = "lib/StarUpgradeCollection", fileName = "StarUpgradeCollection")]
    public class StarUpgradeCollection : ScriptableObject
    {
        public LevelStarUpgrades LevelStarUpgrades { get; private set; }
        
        [Header("Stars Required Levels")]
        [SerializeField] public List<StarRequiredLevel> requiredLevels;
        [SerializeField] public List<int> normalStarsRequiredLevels;
        [SerializeField] public List<int> greenStarsRequiredLevels;
        
        //[SerializeField] private List<BonusUpgradeData> bonusUpgrades;
        [SerializeField] public List<CityStarUpgrades> cityStarUpgradesList;

        private readonly Dictionary<string, StarUpgradeData> upgradesDictionaryByObjectId = new Dictionary<string, StarUpgradeData>();
        private readonly Dictionary<int, StarUpgradeData> upgradesDictionaryByCollectableId = new Dictionary<int, StarUpgradeData>();
        private readonly Dictionary<PoolType, StarUpgradeData> upgradesDictionaryByPoolType = new Dictionary<PoolType, StarUpgradeData>();

        private readonly Dictionary<int, CityStarUpgrades> cityStarUpgradesByCityId = new Dictionary<int, CityStarUpgrades>();

        //private readonly List<BonusUpgradeData> bonusUpgradeDatas = new List<BonusUpgradeData>();
        private readonly Dictionary<StarType, StarRequiredLevel> starRequiredLevelsDictionary = new Dictionary<StarType, StarRequiredLevel>();

        // private static bool _isCollectionLoaded = false;

        #region Load

        // public static StarUpgradeCollection LoadCollection(string collectionPath, bool checkCollectionLoaded = true)
        // {
        //     if (checkCollectionLoaded && _isCollectionLoaded) return _starUpgradeCollection;
        //
        //     _starUpgradeCollection =  Resources.Load<StarUpgradeCollection>(collectionPath);
        //     _isCollectionLoaded = true;
        //     return _starUpgradeCollection;
        // }  
        //

        public static StarUpgradeCollection LoadCollection(string collectionPath)
        {
            return Resources.Load<StarUpgradeCollection>(collectionPath);
        }

        public void LoadData()
        {
            var activeCity = LevelManager.Instance.ActiveCityId;
            var activeLevel = LevelManager.Instance.ActiveLevelId;
            
            LoadLevelStarUpgrades(activeCity, activeLevel);
            //LoadBonusUpgradeDatas();
            LoadDictionaries();
        }

        private void LoadLevelStarUpgrades(int activeCity, int activeLevel)
        {
            foreach(var cityUpgrades in cityStarUpgradesList)
            {
                if(cityUpgrades.CityId == activeCity)
                {
                    foreach (var levelUpgrades in cityUpgrades.levelStarUpgradesList)
                    {
                        if (levelUpgrades.Level != activeLevel) continue;

                        LevelStarUpgrades = levelUpgrades;
                        break;
                    }
                    break;
                }            
            }          
        }

        private void LoadDictionaries()
        {
            LoadUpgradesDictionaries();
            LoadRequiredLevelDictionary();
            LoadCityStarUpgradeDictionary();
        }

        private void LoadRequiredLevelDictionary()
        {
            if(starRequiredLevelsDictionary.Count > 0) return;
            starRequiredLevelsDictionary.Clear();
            
            foreach (var requiredLevel in requiredLevels)
            {
                starRequiredLevelsDictionary[requiredLevel.StarType] = requiredLevel;
            }
        }

        private void LoadUpgradesDictionaries()
        {
            upgradesDictionaryByObjectId.Clear();
            upgradesDictionaryByCollectableId.Clear();
            upgradesDictionaryByPoolType.Clear();

            foreach (var starUpgrade in LevelStarUpgrades.StarUpgrades)
            {
                upgradesDictionaryByObjectId[starUpgrade.InteractableId] = starUpgrade;
                upgradesDictionaryByCollectableId[starUpgrade.CollectableObjectId] = starUpgrade;
                upgradesDictionaryByPoolType[starUpgrade.StarUpgradeProductionDataByPoolType.First().Key] = starUpgrade;
            }
        }

        private void LoadCityStarUpgradeDictionary()
        {
            cityStarUpgradesByCityId.Clear();

            foreach (var cityStarUpgrade in cityStarUpgradesList)
            {
                if (!cityStarUpgradesByCityId.ContainsKey(cityStarUpgrade.CityId))
                {
                    cityStarUpgradesByCityId.Add(cityStarUpgrade.CityId, cityStarUpgrade);
                }
            }
        }

        /*private void LoadBonusUpgradeDatas()
        {
            bonusUpgradeDatas.Clear();
            foreach (var bonusUpgrade in bonusUpgrades)
            {
                bonusUpgradeDatas.Add(bonusUpgrade);
            }
        }*/

        #endregion

        #region Get Functions
        
        public StarUpgradeData GetStarUpgradeByInteractableId(string objectId)
        {
            if (!upgradesDictionaryByObjectId.ContainsKey(objectId))
                return null;
            return upgradesDictionaryByObjectId[objectId];
        }
        public StarUpgradeData GetStarUpgradeByPoolType(PoolType poolType)
        {
            if (!upgradesDictionaryByPoolType.ContainsKey(poolType))
                return null;
            return upgradesDictionaryByPoolType[poolType];
        }

        public Dictionary<PoolType, StarUpgradeData> GetAllStarUpgradePoolTypes()
        {
            return upgradesDictionaryByPoolType;
        }

        public StarUpgradeData GetStarUpgradeByCollectableID(int collectableId)
        {
            if (!upgradesDictionaryByCollectableId.ContainsKey(collectableId))
                return null;
            return upgradesDictionaryByCollectableId[collectableId];
        }

        public int GetStarRequiredLevel(int starLevel, bool isGreenStar = false)
        {
            var targetList = isGreenStar ? greenStarsRequiredLevels : normalStarsRequiredLevels;
            if (targetList.Count < starLevel)
            {
                Debug.Log($"star level : {starLevel} is bigger than {( isGreenStar ? "greenStarsRequiredLevels.Count" : "normalStarsRequiredLevels.Count")} : {targetList.Count} ");
                return 0;
            }
            
            var idx = (starLevel > 0 ) ? (starLevel - 1) : starLevel;
            return targetList[idx];
        }
        
        public int GetStarRequiredLevel(int starLevel, StarType starType)
        {
            var targetList = GetStarRequiredLevels(starType);
            if (targetList.Count < starLevel)
            {
                Debug.Log($"star level : {starLevel} is bigger than {starType} : {targetList.Count} ");
                return 0;
            }
            
            var idx = (starLevel > 0 ) ? (starLevel - 1) : starLevel;
            return targetList[idx];
        }

        private List<int> GetStarRequiredLevels(StarType starType)
        {
            var starLevelsObject = GetStarRequiredLevelObject(starType);
            return starLevelsObject?.RequiredLevel ?? new List<int>();
        }
        
        public StarRequiredLevel GetStarRequiredLevelObject(StarType starType)
        {
            return starRequiredLevelsDictionary.TryGetValue(starType, out var starRequiredLevel)
                ? starRequiredLevel : null;
        }
        
        public Color GetStarColor(StarType starType)
        {
            var starLevelsObject = GetStarRequiredLevelObject(starType);
            return starLevelsObject?.StarColor ?? Color.black;
        }
        
        public Color GetStarBGColor(StarType starType)
        {
            var starLevelsObject = GetStarRequiredLevelObject(starType);
            return starLevelsObject?.StarBGColor ?? Color.black;
        }

        public int GetStarLevel(int upgradeLevel, out StarType starType)
        {
            starType = StarType.Purple;
            foreach (var requiredLevel in requiredLevels)
            {
                int firstStarLevel = requiredLevel.RequiredLevel[0];
                if (upgradeLevel >= firstStarLevel && requiredLevel.StarType > starType)
                {
                    starType = requiredLevel.StarType;
                }
            }
            
            var targetList = GetStarRequiredLevels(starType);
            
            int currentLevel = 0;
            
            for (int i = 0; i < targetList.Count; i++)
            {
                if (upgradeLevel >= targetList[i])
                {
                    currentLevel = i + 1;
                    continue;
                } 
                return currentLevel;
            }

            return currentLevel;
        }

        private List<int> AllStarLevels = new List<int>();
        private bool setStarLevelList = false;
        public int GetTotalStarLevel(int upgradedLevel)
        {
            if (AllStarLevels.Count < 1)
            {
                foreach (var requiredLevel in requiredLevels)
                {
                    foreach (var level in requiredLevel.RequiredLevel)
                    {
                        AllStarLevels.Add(level);
                    }
                }

                setStarLevelList = true;
            }

            int starCollected = 0;

            foreach (var level in AllStarLevels)
            {
                if (upgradedLevel >= level)
                    starCollected++;
                else
                    break;
            }

            return starCollected;
        }
        
        public int GetMaxStarLevel(string interactableId, StarType starType)
        {
            var targetList = GetStarRequiredLevels(starType);
            int maxUpgradeLevel = StarUpgradeManager.Instance.GetMaxUpgradeLevel(interactableId);
            int maxStarLevel = 0;
            
            for (int i = 0; i < targetList.Count; i++)
            {
                if (maxUpgradeLevel >= targetList[i])
                {
                    maxStarLevel = i + 1;
                    continue;
                } 
                return maxStarLevel;
            }

            return maxStarLevel;
        } 

        public List<BonusUpgradeData> GetEarnedBonusUpgrades(string interactionId, PoolType poolType, int lowerBoundLevel, int upperBoundLevel)
        {
            var starUpgradeData = GetStarUpgradeByInteractableId(interactionId);
            var productionData = starUpgradeData.StarUpgradeProductionDataByPoolType[poolType];

            var earnedBonusUpgrades = new List<BonusUpgradeData>();
            if (productionData != null)
            {
                foreach (var bonusUpgrade in productionData.BonusUpgrades)
                {
                    if (lowerBoundLevel < bonusUpgrade.StarLevel && bonusUpgrade.StarLevel <= upperBoundLevel)
                    {
                        earnedBonusUpgrades.Add(bonusUpgrade);
                    }
                }
            }
            

            return earnedBonusUpgrades;
        }

        public int GetMaxStarCountByCityId(int level)
        {
            if (starRequiredLevelsDictionary == null || starRequiredLevelsDictionary.Count <= 0)
                LoadRequiredLevelDictionary();

            int starCount = 0;
            foreach(var star in starRequiredLevelsDictionary)
            {
                if (star.Value.RequiredLevel.Last() <= level)
                    starCount += star.Value.RequiredLevel.Count;
                else
                {
                    int idx = 0;
                    while (star.Value.RequiredLevel[idx] <= level)
                    {
                        starCount++;
                        idx++;
                    }
                    return starCount;
                }
            }

            return starCount;
        }

        public CityStarUpgrades GetStarUpgradeByCityId(int cityId)
        {
            if (cityStarUpgradesByCityId == null || cityStarUpgradesByCityId.Count <= 0)
                LoadCityStarUpgradeDictionary();

            if (cityStarUpgradesByCityId.ContainsKey(cityId))
                return cityStarUpgradesByCityId[cityId];
            return null;
        }
        #endregion
    }

    [Serializable]
    public class CityStarUpgrades
    {
        public int CityId;
        public List<LevelStarUpgrades> levelStarUpgradesList;
    }

    [Serializable]
    public class LevelStarUpgradesOld
    {
        public int Level;
        public int MaxUpgradeCount;
        public float UpgradeCostIncreaseMultiplier;
        public float UpgradeMultiplier;
        public List<StarUpgradeData> StarUpgrades;
    }

    [Serializable]
    public class ProductCostTuple
    {
#if UNITY_EDITOR
        [Searchable]  
#endif
        public PoolType ProductType;
        [Tooltip(("if pool type is undefined we will use currency"))]
        public CurrencyType CurrencyType;
        public IdleNumber Cost;
    }
    
    [Serializable]
    public class UnlockUpgradeableCostData
    {
        public int Timer;
        public List<UpgradeAbleCostTuple> Costs;
    }
    
    [Serializable]
    public class UpgradeAbleCostTuple
    {
        public UpgradeAbleCostType UpgradeAbleCostType;
/*#if UNITY_EDITOR
        [Searchable]  
#endif*/
        [DrawIf("UpgradeAbleCostType" , global::UpgradeAbleCostType.Currency)]public CurrencyType CurrencyType;
        [DrawIf("UpgradeAbleCostType" , global::UpgradeAbleCostType.Currency)]public IdleNumber CurrenyCost;
#if UNITY_EDITOR
        [DrawIf("UpgradeAbleCostType" , global::UpgradeAbleCostType.Product)][Searchable]  
#endif
        public PoolType PoolType;
        [DrawIf("UpgradeAbleCostType" , global::UpgradeAbleCostType.Product)]public IdleNumber ProductCost;
    }

    [Serializable]
    public class StarUpgradeData
    {
        public string Name;
        [Header("ID")]
        public int CollectableObjectId;
        [Tooltip("Interactable Id")] public string InteractableId;
        [Space]
        public float BaseInteractionTime;
        [Space]
        public int MaxUpgradeCount = -1;
        // public Sprite Icon;
        public ManagerAutomateRequirements ManagerAutomateRequirements;
        public List<ProductCostTuple> UnlockCostTuples;
        public UnlockUpgradeableCostData UnlockCostData;

        //public PoolType PoolType;

        //public List<ProductCostTuple> StartingCostTuples;
        //public List<StarUpgradeTypeData> UpgradedAreas;
        //public List<ProductCostTuple> Resources;
        //public List<BonusUpgradeData> BonusUpgrades;

        public List<StarUpgradeProductionData> StarUpgradeProductionDatas;
        public Dictionary<PoolType, StarUpgradeProductionData> StarUpgradeProductionDataByPoolType
        {
            get
            {
                if(starUpgradeProductionDataByPoolType == null)
                {
                    starUpgradeProductionDataByPoolType = new Dictionary<PoolType, StarUpgradeProductionData>();
                    foreach (var data in StarUpgradeProductionDatas)
                    {
                        if (!starUpgradeProductionDataByPoolType.ContainsKey(data.PoolType))
                            starUpgradeProductionDataByPoolType.Add(data.PoolType, data);
                    }
                }
                return starUpgradeProductionDataByPoolType;
            }
        }
        private Dictionary<PoolType, StarUpgradeProductionData> starUpgradeProductionDataByPoolType;

        public bool LowerThanStarUpgrade;

        public IdleNumber GetBaseValue(ObjectDataType objectDataType, PoolType productType = PoolType.Undefined)
        {
            var poolType = productType == PoolType.Undefined ? StarUpgradeProductionDataByPoolType.First().Key : productType;
            if (StarUpgradeProductionDataByPoolType.ContainsKey(poolType))
            {
                foreach (var item in StarUpgradeProductionDataByPoolType[poolType].UpgradedAreas)
                {
                    if (item.ObjectDataType == objectDataType)
                        return item.BaseValue;
                }
            }
            return new IdleNumber(0, NumberDigits.Empty);
        }

        public bool HasUpgradeFor(PoolType productType, ObjectDataType objectDataType)
        {
            if (StarUpgradeProductionDataByPoolType.ContainsKey(productType))
            {
                foreach (var item in StarUpgradeProductionDataByPoolType[productType].UpgradedAreas)
                {
                    if (item.ObjectDataType == objectDataType)
                        return true;
                }
            }
            return false;
        }
    }

    [Serializable]
    public class StarUpgradeProductionData
    {
        public PoolType PoolType;
        [Space]

        public int RequiredLevel;
        [Space]

        public List<ProductCostTuple> StartingCostTuples;
        public List<StarUpgradeTypeData> UpgradedAreas;
        public List<ProductCostTuple> Resources;
        public List<BonusUpgradeData> BonusUpgrades;
    }

    [Serializable]
    public class BonusUpgradeData
    {
        public string Name;
        // public Sprite Sprite;
        public int StarLevel;
        public ObjectDataType TargetObjectValue;
        public float BonusMultiplier;
        public float BonusAddition;
        [FormerlySerializedAs("RewardGem")] public IdleNumber RewardDollar;
    }

    [Serializable]
    public class StarUpgradeTypeData
    {
        public ObjectDataType ObjectDataType;
        public IdleNumber BaseValue;
    }
    
    [Serializable]
    public class StarRequiredLevel
    {
        public StarType StarType;
        public List<int> RequiredLevel;
        public Color StarColor;
        public Color StarBGColor;
    }
    
    [Serializable]
    public class ManagerAutomateRequirements
    {
        public RarityType MinRarity => minRarity;
        public int MinLevel => minLevel;
        
        [SerializeField] private RarityType minRarity;
        [SerializeField] private int minLevel;

        public bool CanAutomate(Sticker manager, out bool rarityEnough, out bool levelEnough)
        {
            if (manager == null)
            {
                rarityEnough = false;
                levelEnough = false;
                return false;
            }
            
            rarityEnough = (int)manager.RarityType >= (int)MinRarity;

            int level = DirectorManager.Instance.GetDirectorCardLevel(manager.Id);
            levelEnough = level >= MinLevel;

            return rarityEnough && levelEnough;
        }
        
        public bool CanAutomate(Sticker manager)
        {
            return CanAutomate(manager, out _, out _);
        }
    }

    [Serializable]
    public class StarLevelCostType
    {
        public int Level;
#if UNITY_EDITOR
        [Searchable]  
#endif
        public List<PoolType> CostTypes;

        public IdleNumber CostAmount;

        public UnlockUpgradeableCostData LevelUpCosts;
        //public List<UpgradeAbleCostTuple> LevelUpCostType;
    }

    [Serializable]
    public class StarLevelCostTypeByProductType
    {
        public PoolType ProductType;
        public List<StarLevelCostType> StarLevelCostTypes;

        private Dictionary<int, StarLevelCostType> StarLevelCostTypesByLevel
        {
            get
            {
                if (starLevelCostTypesByLevel == null) StarLevelCostTypesByProductTypeDic();
                return starLevelCostTypesByLevel;
            }

        }
        private Dictionary<int, StarLevelCostType> starLevelCostTypesByLevel;

        private void StarLevelCostTypesByProductTypeDic()
        {
            starLevelCostTypesByLevel = new Dictionary<int, StarLevelCostType>();
            foreach (var starLevelCostType in StarLevelCostTypes)
            {
                if (!starLevelCostTypesByLevel.ContainsKey(starLevelCostType.Level))
                {
                    starLevelCostTypesByLevel.Add(starLevelCostType.Level, starLevelCostType);
                }
            }
        }

        public StarLevelCostType GetStarLevelCostTypeByLevel(int level)
        {
            if (StarLevelCostTypesByLevel.Count > 0)
            {
                if (StarLevelCostTypesByLevel.ContainsKey(level) && StarLevelCostTypesByLevel[level].CostTypes.Count > 0)
                {
                    return StarLevelCostTypesByLevel[level];
                }
                else
                {
                    int costLevel = 0;

                    foreach (var costType in StarLevelCostTypesByLevel)
                    {
                        if(costType.Key > costLevel)
                        {
                            return StarLevelCostTypesByLevel.ContainsKey(costLevel) ? StarLevelCostTypesByLevel[costLevel] : null;
                        }
                        else
                        {
                            costLevel = costType.Key;
                        }
                    }
                }
            } 

            return null;
        }
    }
}

public enum UpgradeAbleCostType
{
    Currency,
    Product
}