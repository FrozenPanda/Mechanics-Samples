using System;
using System.Collections;
using System.Collections.Generic;
using _Game.Scripts.Systems.WeeklyEventSystem;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class RewardedBoostUpgradeManager : Singleton<RewardedBoostUpgradeManager>
{
    public const float BOOST_DURATION = 30f * 60f; //5 minute
    private const float BOOST_START_VALUE = 2f; //5 minute
    private float boostMultiplier = -1f;
    private const int REWARDED_BOOST_ID = 4;

    private int CurrentWatchedAds = 0;

    [FormerlySerializedAs("OnChestAdsShown")] public UnityEvent OnPrizeAdsShown = new UnityEvent();
    public UnityEvent OnPrizeDataReset = new UnityEvent();
    
    private Dictionary<string, string> stateData = new Dictionary<string, string>();
    private bool loaded;

    public UnityEvent OnAdsWatched = new UnityEvent();

    private void Awake()
    {
        CardManager.Instance.CardUpdated.AddListener(OnCardUpdated);
        boostMultiplier = CardManager.Instance.GetUpgradedValue(GeneralUpgradeType.RewardedBoostValue, BOOST_START_VALUE);
    }

    private void OnEnable()
    {
        LoadSessionData();
        TryToResetPrizeEarnInterval();
    }

    private void OnDisable()
    {
        
    }

    private void OnCardUpdated(IdleUpgradeType upgradeType)
    {
        if(upgradeType == IdleUpgradeType.GeneralUpgrade)
        {
            boostMultiplier = CardManager.Instance.GetUpgradedValue(GeneralUpgradeType.RewardedBoostValue, BOOST_START_VALUE);
            TmpProfitUpgradeManager.Instance.UpdateMultiplier(REWARDED_BOOST_ID, boostMultiplier);
        }
    }

    public void StartBoost()
    {
        //TODO: Will be added to Rewarded Add
        MediationManager.Instance.ShowRewarded("boost_from_ui", $"all_production_x{GetBoostMultiplier().ToString()}", () =>
        {
            GainBoost();
        });
    }

    public void ShowRewardedAdForChest()
    {
        MediationManager.Instance.ShowRewarded("rewarded_ad_for_chest", $"all_production_x{GetBoostMultiplier().ToString()}",() =>
        {
            CurrentWatchedAds = GetAdsWatchAmountToday();
            CurrentWatchedAds++;
            SetAdsWatchAmountToday(CurrentWatchedAds);
            CheckGainBoostAvailability();
            OnAdsWatched?.Invoke();
        });
    }
    
    public void ShowRewardedAdForChestFree()
    {
        MediationManager.Instance.AdShown?.Invoke();
        CurrentWatchedAds = GetAdsWatchAmountToday();
        CurrentWatchedAds++;
        SetAdsWatchAmountToday(CurrentWatchedAds);
        CheckGainBoostAvailability();
        OnAdsWatched?.Invoke();
    }

    public void GainBoostWithGem()
    {
        MediationManager.Instance.AdShown?.Invoke();
        CurrentWatchedAds = GetAdsWatchAmountToday();
        CurrentWatchedAds++;
        SetAdsWatchAmountToday(CurrentWatchedAds);
        //CheckGainBoostAvailability();
        //OnAdsWatched?.Invoke();
        GainBoost();
        GainBoost();
    }

    private void CheckGainBoostAvailability()
    {
        var maxTime = ConfigurationService.Configurations.MaxBoostTime * 60 * 60;
        var currentTotalBoostTime = GetBoostCountDown();
        if(maxTime > currentTotalBoostTime)
            GainBoost();
    }

    public void GainBoost()
    {
        TmpProfitUpgradeManager.Instance.StartTmpUpgrade(CreateTmpBoost());
        var newMult = GetBoostMultiplier();
        WarningText.ShowWarning(WarningTextType.DefaultWarning, "All PRODUCTION x" + newMult.ToString(), 125f, true, 125f, 1.5f, 2f, CurrencyService.GetCurrencyItem(CurrencyService.ActiveCurrencyType).Sprite);
        TryToSetFirstAdsWatchTime();
        OnPrizeAdsShown?.Invoke();
    }

    public TmpProfitUpgrade CreateTmpBoost()
    {
        return new TmpProfitUpgrade()
        {
            Id = REWARDED_BOOST_ID,
            LifeTime = (int)GetBoostDuration(),
            Name = "RewardedBoost",
            Multiplier = GetBoostMultiplier(),
        };
    }

    public float GetBoostDuration()
    {
         return VillageManager.Instance.GetUpgradedValue(GeneralUpgradeType.RewardedBoostDuration , BOOST_DURATION);
    }

    public float GetBoostMultiplier()
    {
        if(boostMultiplier < 0f)
        {
            boostMultiplier = VillageManager.Instance.GetUpgradedValue(GeneralUpgradeType.RewardedBoostValue, BOOST_START_VALUE);
        }

        return boostMultiplier;
    }

    public void GivePrize()
    {
        ShopPackageManager.Instance.GivePackageContent(ConfigurationService.Configurations.AdsWatchReward , PackageMod.Mod1);
        AdsPrizeEarnedToday();
    }

    public float GetBoostCountDown()
    {
        return TmpProfitUpgradeManager.Instance.GetUpgradeCountDown(REWARDED_BOOST_ID, MysteriousPanel.MYSTERIOUS_BOX_ID);
    }

    public bool CanPrizeEarned()
    {
        if (stateData.ContainsKey("AdsPrizeEarned"))
            return false;
        return true;
    }

    public float GetNextPriceCoolDown()
    {
        if(!stateData.ContainsKey("FirstTimeAdsShownTime"))
            return 0;
        DateTime firstClaimTime = StringToDateTime(stateData["FirstTimeAdsShownTime"]);
        DateTime currentTime = DateTime.Now;
        var timeNeedForReset = ConfigurationService.Configurations.ResetTimeInterval * 60 * 60; //todo bunu gameconfigden çek
        double missingSecond = (currentTime - firstClaimTime).TotalSeconds;
        return (float)(timeNeedForReset - missingSecond);
    }

    #region SaveLoad

    private void LoadSessionData()
    {
        stateData = DataService.Instance.GetData< Dictionary<string, string> > (DataType.INTERACTABLE);

        loaded = true;
    }
    
    private void SaveSessionData()
    {
        if(!loaded)
            LoadSessionData();
        DataService.Instance.SetData(DataType.INTERACTABLE, stateData);
    }
    
    public int GetAdsWatchAmountToday()
    {
        if (!stateData.ContainsKey("TotalAdsWatchedToday"))
            CurrentWatchedAds = 0;
        else
        {
            var TotalWatchAdsString = stateData["TotalAdsWatchedToday"];
            CurrentWatchedAds = int.Parse(TotalWatchAdsString);
        }
            
        return CurrentWatchedAds;
    }

    private void SetAdsWatchAmountToday(int amount)
    {
        if (!stateData.ContainsKey("TotalAdsWatchedToday"))
            stateData.Add("TotalAdsWatchedToday" , amount.ToString());
        else
        {
            stateData["TotalAdsWatchedToday"] = amount.ToString();
        }
        
        SaveSessionData();
    }

    private void TryToSetFirstAdsWatchTime()
    {
        if (!stateData.ContainsKey("FirstTimeAdsShownTime"))
        {
            stateData.Add("FirstTimeAdsShownTime" , DateTime.Now.ToString());
            SaveSessionData();
            TryToResetPrizeEarnInterval();
        }
    }

    public void AdsPrizeEarnedToday()
    {
        if (!stateData.ContainsKey("AdsPrizeEarned"))
        {
            stateData.Add("AdsPrizeEarned" , "true");
            SaveSessionData();
        }
    }

    private void ResetAdsPrizeEarnedToday()
    {
        if (stateData.ContainsKey("AdsPrizeEarned"))
            stateData.Remove("AdsPrizeEarned");

        if (stateData.ContainsKey("FirstTimeAdsShownTime"))
            stateData.Remove("FirstTimeAdsShownTime");

        if (stateData.ContainsKey("TotalAdsWatchedToday"))
            stateData.Remove("TotalAdsWatchedToday");
        
        SaveSessionData();
        
        OnPrizeDataReset?.Invoke();
    }

    private void TryToResetPrizeEarnInterval()
    {
        if(!stateData.ContainsKey("FirstTimeAdsShownTime"))
            return;
        DateTime firstClaimTime = StringToDateTime(stateData["FirstTimeAdsShownTime"]);
        DateTime currentTime = DateTime.Now;
        var timeNeedForReset = ConfigurationService.Configurations.ResetTimeInterval * 60 * 60; //todo bunu gameconfigden çek
        double missingSecond = (currentTime - firstClaimTime).TotalSeconds;
        if(missingSecond > timeNeedForReset)
            ResetAdsPrizeEarnedToday();
        else
        {
            Invoke("ResetAdsPrizeEarnedToday" , (float)(timeNeedForReset - missingSecond));
        }
    }
    
    #endregion
    
    private DateTime StringToDateTime(string DateTime)
    {
        DateTime _date = Convert.ToDateTime(DateTime);
        return _date;
    }
}
