using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WatchRewardedAdMission : BaseMission
{
    public override void InitilizeMission()
    {
        base.InitilizeMission();
        currentMissionTab.SetUItab(missionSprite , WatchAdsAmountWanted , WatchAdsAmountCurrent , GetInfoText());
    }
    
    protected override void GetSaveData()
    {
        base.GetSaveData();
        WatchAdsAmountCurrent = SaveData.CurrentAmount;
    }

    protected override void UpdateSaveData()
    {
        if (SaveData != null)
            SaveData.CurrentAmount = WatchAdsAmountCurrent;
        
        base.UpdateSaveData();
    }

    protected override void AddListeners()
    {
        base.AddListeners();
       // RewardedBoostUpgradeManager.Instance.OnAdsWatched.AddListener(UpdateCurrentProgress);
        MediationManager.Instance.AdShown.AddListener(UpdateCurrentProgress);
        MissionManager.Instance.OnMissionsRefreshed.AddListener(ReleaseTab);
    }
    
    public override void RemoveListeners()
    {
        base.RemoveListeners();
       // RewardedBoostUpgradeManager.Instance.OnAdsWatched.AddListener(UpdateCurrentProgress);
        MediationManager.Instance.AdShown.RemoveListener(UpdateCurrentProgress);
    }
    
    public override void ReleaseTab()
    {
        base.ReleaseTab();
        currentMissionTab = null;
        ReleaseTabBool = true;
    }
    
    public override void UpdateCurrentProgress()
    {
        base.UpdateCurrentProgress();
        
        if(currentMissionTab == null)
            return;

        WatchAdsAmountCurrent += 1;
        
        UpdateSaveData();
        CheckMissionComplete();
        UpdateMissionTab();
    }

    public override void CheckMissionComplete()
    {
        base.CheckMissionComplete();
        
        if(WatchAdsAmountCurrent >= WatchAdsAmountWanted)
            SetMissionComplete();
    }

    public override void UpdateMissionTab()
    {
        var fillamount = WatchAdsAmountCurrent.ToFloat() / WatchAdsAmountWanted.ToFloat();
        currentMissionTab.UpdateFillbar(fillamount);
        currentMissionTab.UpdateText(WatchAdsAmountCurrent);
        
        base.UpdateMissionTab();
    }

    public override void SetMissionComplete()
    {
        base.SetMissionComplete();
        //RemoveListeners();
    }

    public override void CompleteAction()
    {
        RemoveListeners();
        //MissionManager.Instance.DeleteMissionFromTheList(this);
        base.CompleteAction();
    }

    public override string GetInfoText()
    {
        if(WatchAdsAmountWanted > 1)
         return $"Get {WatchAdsAmountWanted} boosts";
        return $"Get {WatchAdsAmountWanted} boost";
    }
}
