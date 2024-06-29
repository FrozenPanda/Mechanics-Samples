using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEditor;

[CreateAssetMenu(fileName = "LeveledMissionCollection",  menuName = "lib/LeveledMissionCollection")]
public class LeveledMissionCollection : ScriptableObject
{
    public int LevelId;
    public bool IsActive;
    public int TotalMissionAtSameTime = 1;
    [HideInInspector]public int ActiveInCityId;

    public List<MissionSet> MissionSets;

    public void SetMissionIDs()
    {
        int x = 0;

        foreach (var set in MissionSets)
        {
            foreach (var mission in set.Missions)
            {
                mission.Id = x;
                mission.Name = mission.MissionType.ToString();

                if (mission.MissionType == MissionType.LevelUpGenerator)
                {
                    string extraInfo = $" ID:{mission.LevelUpgeneratorId}, IO:{mission.LevelUpGeneratorInfoOverride}, I:{mission.LevelUpGeneratorInfo}";
                    mission.Name += extraInfo;
                }else if (mission.MissionType == MissionType.BuildingUpgrade)
                {
                    string extraInfo = $" ID:{mission.BuildingID}, IO:{mission.BuildingUpgradeInfoOverride}, I:{mission.BuildingUpgradeInfo}";
                    mission.Name += extraInfo;
                }else if (mission.MissionType == MissionType.MarketUnlock)
                {
                    string extraInfo = $" ID:{mission.MarketUnlockID}, IO:{mission.UnlockMarketInfoOverride}, I:{mission.UnlockMarketInfo}";
                    mission.Name += extraInfo;
                }else if (mission.MissionType == MissionType.UnlockGenerator)
                {
                    string extraInfo = $" ID:{mission.UnlockGeneratorId}, IO:{mission.UnlockGeneratorInfoOverride}, I:{mission.UnlockGeneratorInfo}";
                    mission.Name += extraInfo;
                }else if (mission.MissionType == MissionType.MarketAutomate)
                {
                    string extraInfo = $" ID:{mission.MarketID}, IO:{mission.AutomateMarketInfoOverride}, I{mission.AutomateMarketInfo}";
                    mission.Name += extraInfo;
                }else if (mission.MissionType == MissionType.SpendProduct)
                {
                    string extraInfo = $" SpendProduct:--{mission.SpendProdcutType}--";
                    mission.Name += extraInfo;
                }else if (mission.MissionType == MissionType.CollectProduct)
                {
                    string extraInfo = $" CollectProduct:--{mission.CollectProductType}--";
                    mission.Name += extraInfo;
                }
                
                x++;
            }
        }
        
        #if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        #endif
    }

    public void CalculateCurrencies()
    {
        /*IdleNumber totalGem = new IdleNumber();
        IdleNumber totalDollar = new IdleNumber();
        IdleNumber totalCoin = new IdleNumber();

        foreach (var mission in Missions)
        {
            if (mission.Reward.HasCurrencyInPackage(PackageMod.Mod1) && mission.isActive)
            {
                var currencies = mission.Reward.GetCurrencies(PackageMod.Mod1);

                foreach (var currency in currencies)
                {
                    if (currency.CurrencyType == CurrencyType.Dollar)
                    {
                        Debug.Log($"Id {mission.Id} has {currency.Price} dollar");
                        totalDollar += currency.Price;
                    }else if (currency.CurrencyType == CurrencyType.Gem)
                    {
                        Debug.Log($"Id {mission.Id} has {currency.Price} gem");
                        totalGem += currency.Price;
                    }else if (currency.CurrencyType == CurrencyType.Coin)
                    {
                        Debug.Log($"Id {mission.Id} has {currency.Price} coin");
                        totalCoin += currency.Price;
                    }
                    
                    Debug.Log($"Total Dolar: {totalDollar} , Total Gem: {totalGem} , Total Coin: {totalCoin}");
                }
            }
        }
        
        Debug.Log($"Finally Dolar: {totalDollar} , Total Gem: {totalGem} , Total Coin: {totalCoin}");*/
    }
}

[Serializable]
public class MissionSet
{
    public List<BaseMission> Missions;
    public PackageContent Reward;
}

#if UNITY_EDITOR
[CustomEditor(typeof(LeveledMissionCollection))]
public class LeveledMissionCollectionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LeveledMissionCollection myScript = (LeveledMissionCollection)target;
        if(GUILayout.Button("ReOrder Missions IDs"))
        {
            myScript.SetMissionIDs();
        }

        if (GUILayout.Button("CalculateCurrencies"))
        {
            myScript.CalculateCurrencies();
        }
    }
}
#endif
