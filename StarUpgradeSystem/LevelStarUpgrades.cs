using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Game.Scripts.Systems.StarUpgradeSystem
{
    public class LevelStarUpgrades : ScriptableObject
    {
        public int Level;
        public int MaxUpgradeCount;
        public float UpgradeCostIncreaseMultiplier;
        public float UpgradeCostIncreaseMultiplierMarket;
        public float UpgradeMultiplier;
        public float UpgradeAddition;
        public float UpgradeAdditionMultiplier;
        public List<StarLevelCostTypeByProductType> StarLevelCostTypes;
        public List<StarUpgradeData> StarUpgrades;

        private Dictionary<PoolType, StarLevelCostTypeByProductType> StarLevelCostTypesByProductType
        {
            get
            {
                if (starLevelCostTypesByProductType == null) StarLevelCostTypesByProductTypeDic();
                return starLevelCostTypesByProductType;
            }

        }
        private Dictionary<PoolType, StarLevelCostTypeByProductType> starLevelCostTypesByProductType;

        private void StarLevelCostTypesByProductTypeDic()
        {
            starLevelCostTypesByProductType = new Dictionary<PoolType, StarLevelCostTypeByProductType>();
            foreach (var starLevelCostType in StarLevelCostTypes)
            {
                if (!starLevelCostTypesByProductType.ContainsKey(starLevelCostType.ProductType))
                {
                    starLevelCostTypesByProductType.Add(starLevelCostType.ProductType, starLevelCostType);
                }
            }
        }

        public StarLevelCostTypeByProductType GetStarLevelCostTypeByProductType(PoolType poolType)
        {
            if (StarLevelCostTypesByProductType.Count > 0 && StarLevelCostTypesByProductType.ContainsKey(poolType))
                return StarLevelCostTypesByProductType[poolType];

            return null;
        }

        
        public (List<UpgradeAbleCostTuple> costs , int timer) GetCurrentUpgradeableCost(PoolType poolType , int star)
        {
            StarLevelCostTypeByProductType currentTypeByProductType = new StarLevelCostTypeByProductType();

            foreach (var varCostType in StarLevelCostTypes)
            {
                if (varCostType.ProductType == poolType)
                {
                    currentTypeByProductType = varCostType;
                    break;
                }
            }

            var costTuples = new List<UpgradeAbleCostTuple>();
            var data = new UnlockUpgradeableCostData();
            foreach (var costs in currentTypeByProductType.StarLevelCostTypes)
            {
                if (costs.Level == star)
                {
                    data = costs.LevelUpCosts;
                    
                    foreach (var type in data.Costs)
                    {
                        costTuples.Add(type);
                    }
                    
                    break;
                }
            }

            return (costTuples , data.Timer);
        }
    }
}


