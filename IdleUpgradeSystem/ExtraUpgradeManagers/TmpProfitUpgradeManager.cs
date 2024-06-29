using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TmpProfitUpgradeManager : Singleton<TmpProfitUpgradeManager>, IExtraObjectUpgradeManager , IExtraGeneralUpgradeManager
{
    public UnityEvent<TmpProfitUpgrade> OnProfitUpgradeStarted = new UnityEvent<TmpProfitUpgrade>();
    public UnityEvent OnProductionUpgradeStart = new UnityEvent();
    public UnityEvent OnProductionUpgradeEnded = new UnityEvent();

    public TmpProfitUpgradeCollection TmpProfitUpgradeCollection => tmpProfitUpgradeCollection;
    private TmpProfitUpgradeCollection tmpProfitUpgradeCollection;
    private Dictionary<int, TmpUpgradeData> tmpUpgradeDatas;
    private List<int> CleaningList = new List<int>();
    private bool isLoaded = false;

    public void StartTmpUpgrade(TmpProfitUpgrade upgrade)
    {
        if (upgrade.InstantProduction)
        {
            GiveInstantBoost(upgrade.LifeTime);
            return;
        }
        
        var now = Timestamp.Now();
        if (upgrade.LifeTime == -1)
        {
            if (!tmpUpgradeDatas.ContainsKey(upgrade.Id))
            {
                tmpUpgradeDatas.Add(upgrade.Id, new TmpUpgradeData()
                {
                    StartTime = now,
                    Id = upgrade.Id,
                    LifeTime = upgrade.LifeTime,
                    Multiplier = upgrade.Multiplier,
                    Addition = upgrade.Addition
                }); ;
                OnProductionUpgradeStart?.Invoke();
                SaveData();
                return;
            }
            else return;
        }

        if (tmpUpgradeDatas.ContainsKey(upgrade.Id))
        {
            var endTime = GetEndTime( tmpUpgradeDatas[upgrade.Id]);
            if (endTime > now)
            {
                tmpUpgradeDatas[upgrade.Id].LifeTime += upgrade.LifeTime;
            }
            else
            {
                tmpUpgradeDatas[upgrade.Id] = new TmpUpgradeData()
                {
                    StartTime = now,
                    Id = upgrade.Id,
                    LifeTime = upgrade.LifeTime,
                    Multiplier = upgrade.Multiplier,
                    Addition = upgrade.Addition
                };
            }
        }
        else
        {
            tmpUpgradeDatas.Add(upgrade.Id, new TmpUpgradeData()
            {
                StartTime = now,
                Id = upgrade.Id,
                LifeTime = upgrade.LifeTime,
                Multiplier = upgrade.Multiplier,
                Addition = upgrade.Addition
            });
        }
        SaveData();
        OnProfitUpgradeStarted?.Invoke(upgrade);
        OnProductionUpgradeStart?.Invoke();
    }

    private void Update()
    {
        CheckTmpUpgradeTimer();
    }

    
    private void CheckTmpUpgradeTimer()
    {
        CleaningList.Clear();
        foreach (var upgrades in tmpUpgradeDatas)
        {
            var RemainTime = GetUpgradeCountDown(upgrades.Key);
            if ( RemainTime <= 0f && RemainTime != -1)
            {
                CleaningList.Add(upgrades.Key);
                //tmpUpgradeDatas.Remove(upgrades.Key);
                //OnProductionUpgradeEnded?.Invoke();
            }
        }

        if (CleaningList.Count > 0)
        {
            foreach (var cleaningData in CleaningList)
            {
                if (tmpUpgradeDatas.ContainsKey(cleaningData))
                {
                    tmpUpgradeDatas.Remove(cleaningData);
                }
            }
            
            OnProductionUpgradeEnded?.Invoke();
        }
    }

    public void StartTmpUpgrade(int id)
    {
        StartTmpUpgrade(tmpProfitUpgradeCollection.GetTmpProfitUpgrade(id));
    }

    public void UpdateMultiplier(int id, float multiplier)
    {
        if (!tmpUpgradeDatas.ContainsKey(id)) return;

        tmpUpgradeDatas[id].Multiplier = multiplier;
    }

    public float GetUpgradeCountDown(int id)
    {
        if (!tmpUpgradeDatas.ContainsKey(id)) return 0;
        if (tmpUpgradeDatas[id].LifeTime == -1) return -1;
        var endTime = GetEndTime( tmpUpgradeDatas[id]);
        var remainingTime = endTime - Timestamp.Now();
        return remainingTime > 0 ? remainingTime : 0;
    }

    public bool HasBoost(int id)
    {
        if (!tmpUpgradeDatas.ContainsKey(id)) return false;
        return true;
    }

    public float GetUpgradeCountDown(params int[] ids)
    {
        if (ids.Length == 0) return 0;
        float totalCountdown = 0;
        foreach (var id in ids)
        {
            totalCountdown += GetUpgradeCountDown(id);
        }

        return totalCountdown;
    }
    
    public float GetUpgradedValue(string objectId, ObjectDataType objectDataType, float baseValue, PoolType poolType, bool isNextLevel)
    {
        float multiplier = 1f;
        float addition = 0f;

        var isMarketPlace = poolType.ToString().Contains("Coin");
        if (isMarketPlace) return baseValue;

        foreach (var item in tmpUpgradeDatas)
        {
            var upgradeItem = TmpProfitUpgradeCollection.GetTmpProfitUpgrade(item.Key);
            if(upgradeItem.UpgradeType == IdleUpgradeType.ObjectUpgrade && upgradeItem.ObjectUpgradeType == objectDataType &&
                (item.Value.LifeTime == -1 || GetEndTime(item.Value) > Timestamp.Now()))
            {
                multiplier *= item.Value.Multiplier;
                addition += item.Value.Addition;
            }
        }

        return baseValue * multiplier + addition;
    }

    public IdleNumber GetUpgradedValue(string objectId, ObjectDataType objectDataType, IdleNumber baseValue, PoolType poolType, bool isNextLevel)
    {
        float multiplier = 1f;
        float addition = 0f;

        var isMarketPlace = poolType.ToString().Contains("Coin");
        if (isMarketPlace) return baseValue;

        foreach (var item in tmpUpgradeDatas)
        {
            var upgradeItem = TmpProfitUpgradeCollection.GetTmpProfitUpgrade(item.Key);
            if( upgradeItem.UpgradeType == IdleUpgradeType.ObjectUpgrade && upgradeItem.ObjectUpgradeType == objectDataType &&
                (item.Value.LifeTime == -1 || GetEndTime(item.Value) > Timestamp.Now()))
            {
                multiplier *= item.Value.Multiplier;
                addition += item.Value.Addition;
            }
        }

        return baseValue * multiplier + addition;
    }

    private int GetEndTime(TmpUpgradeData tmpProfitUpgrade)
    {
        return tmpProfitUpgrade.LifeTime + tmpProfitUpgrade.StartTime;
    }
    
    public TmpProfitUpgrade GetTmpProfitUpgrade(int id)
    {
        return tmpProfitUpgradeCollection.GetTmpProfitUpgrade(id);
    }    
    
    public Sprite GetTmpProfitUpgradeIcon(int id)
    {
        TmpProfitUpgrade tmpProfitUpgrade = GetTmpProfitUpgrade(id);
        return tmpProfitUpgrade?.Icon;
    }    
    
    public int GetTmpProfitUpgradeLifeTime(int id)
    {
        TmpProfitUpgrade tmpProfitUpgrade = GetTmpProfitUpgrade(id);
        return tmpProfitUpgrade?.LifeTime ?? 0;
    }

    public float GetAllProfitBoostMultiple()
    {
        var totalMultiple = 1f;
        foreach(var tmp in tmpUpgradeDatas)
        {
            var tmpUpgradeItem = GetTmpProfitUpgrade(tmp.Value.Id);
            if(tmpUpgradeItem.ObjectUpgradeType == ObjectDataType.ProductCount)
            {
                totalMultiple *= tmp.Value.Multiplier;
            }
        }

        return totalMultiple;
    }

    public void ResetAll()
    {
        tmpUpgradeDatas.Clear();
        SaveData();
    }

    private void Awake()
    {
        Load();
        MediationManager.Instance.AdEndEvent.AddListener(UpdateTimeWithAd);
    }

    private void UpdateTimeWithAd(int adTime)
    {
        foreach (var item in tmpUpgradeDatas)
        {
            if(item.Value.LifeTime != -1 && Timestamp.Now() - item.Value.StartTime > 2)
            {
                item.Value.StartTime += adTime;
            }
        }
    }

    private void Load()
    {
        tmpProfitUpgradeCollection ??= TmpProfitUpgradeCollection.Create();
        tmpProfitUpgradeCollection.Load();

        LoadData();
        isLoaded = true;
    }
    private void LoadData()
    {
        tmpUpgradeDatas = DataService.Instance.GetData<Dictionary<int, TmpUpgradeData>>(DataType.TMP_UPGRADE);
    }

    private void SaveData()
    {
        if (!isLoaded) return;
        DataService.Instance.SetData(DataType.TMP_UPGRADE, tmpUpgradeDatas);
    }

    public float GetUpgradedValue(GeneralUpgradeType generalSettingType, float baseValue)
    {
        float multiplier = 1f;
        float addition = 0f;
        
        foreach (var item in tmpUpgradeDatas)
        {
            var upgradeItem = TmpProfitUpgradeCollection.GetTmpProfitUpgrade(item.Key);
            if( upgradeItem.UpgradeType == IdleUpgradeType.GeneralUpgrade && upgradeItem.GeneralUpgradeType == generalSettingType && (item.Value.LifeTime == -1 || GetEndTime(item.Value) > Timestamp.Now()))
            {
                multiplier *= item.Value.Multiplier;
                addition += item.Value.Addition;
            }
        }

        return baseValue * multiplier + addition;
    }

    public IdleNumber GetUpgradedValue(GeneralUpgradeType generalSettingType, IdleNumber baseValue)
    {
        float multiplier = 1f;
        float addition = 0f;
        
        foreach (var item in tmpUpgradeDatas)
        {
            var upgradeItem = TmpProfitUpgradeCollection.GetTmpProfitUpgrade(item.Key);
            if( upgradeItem.UpgradeType == IdleUpgradeType.GeneralUpgrade && upgradeItem.GeneralUpgradeType == generalSettingType && (item.Value.LifeTime == -1 || GetEndTime(item.Value) > Timestamp.Now()))
            {
                multiplier *= item.Value.Multiplier;
                addition += item.Value.Addition;
            }
        }

        return baseValue * multiplier + addition;
    }

    private void GiveInstantBoost(int time)
    {
        var productContainers = InteractionManager.Instance.GetAllAvailableInteractables<NFProductContainer>(InteractableType.NFProductContainer);
        var incomeDic = new Dictionary<PoolType, IdleNumber>();

        float levelMultiplier = LevelManager.Instance.ActiveLevel.ProductionTimeCalculationMultiplier;
        foreach (var productContainer in productContainers)
        {
            IdleNumber productCost = productContainer.GetProductionCount();
            var productionTime = productContainer.GetBaseInteractionTime();

            var productType = productContainer.GetObjectType();
            var income = (productCost / productionTime);
            income *= (time);
            income.Floor();

            if (!incomeDic.ContainsKey(productType))
            {
                incomeDic.Add(productType, income);
            }
            else
            {
                incomeDic[productType] += income;
            }
        }
        
        PackageContent upgradedPackageContent = new PackageContent();
        Content UpgradedContent = new Content();

        foreach (var dicData in incomeDic)
        {
            CountByType countByType = new CountByType();
            countByType.PoolType = dicData.Key;
            countByType.Count = dicData.Value;
            UpgradedContent.ProductDatas.Add(countByType);
        }
        
        ContentMod contentMod = new ContentMod();
        contentMod.Content = UpgradedContent;
        contentMod.PackageMod = PackageMod.Mod1;
        upgradedPackageContent.ContentMods.Add(contentMod);
        ShopPackageManager.Instance.GivePackageContent(upgradedPackageContent, PackageMod.Mod1, collectWithAnim: true, isPromotionReward: true , canChestInstantOpen: !true ,fromPanel : PopupType.QuestRewardPanel , isCheat: !true);
    }
}

public class TmpUpgradeData
{
    public int Id;
    public int StartTime;
    public int LifeTime;
    public float Multiplier;
    public float Addition;
}
