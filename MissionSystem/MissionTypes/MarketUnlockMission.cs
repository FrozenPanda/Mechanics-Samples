using System.Collections;
using System.Collections.Generic;
using _Game.Scripts.Systems.StarUpgradeSystem;
using UnityEngine;

public class MarketUnlockMission : BaseMission
{
    public override void InitilizeMission()
    {
        base.InitilizeMission();
        currentMissionTab.SetUItab(missionSprite , new IdleNumber(1, NumberDigits.Empty) , new IdleNumber(0, NumberDigits.Empty) , GetInfoText());
    }

    protected override void GetSaveData()
    {
        base.GetSaveData();
    }

    protected override void UpdateSaveData()
    {
        base.UpdateSaveData();
    }

    protected override void AddListeners()
    {
        base.AddListeners();
        StarUpgradeManager.Instance.OnMarketUnlocked.AddListener(UpdateCurrentProgress);
        MissionManager.Instance.OnMissionsRefreshed.AddListener(ReleaseTab);
    }

    public override void RemoveListeners()
    {
        base.RemoveListeners();
        StarUpgradeManager.Instance.OnMarketUnlocked.RemoveListener(UpdateCurrentProgress);
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
        
        CheckMissionComplete();
        UpdateMissionTab();
    }

    public override void CheckMissionComplete()
    {
        base.CheckMissionComplete();
        
        var marketPlaces = InteractionManager.Instance.GetAllAvailableInteractables<NFMarketPlace>(InteractableType.NFMarketPlace);
        bool isEnabled = false;

        foreach (var market in marketPlaces)
        {
            if (market.NFMarketPlace.ID == MarketUnlockID)
                isEnabled = true;
        }
        
        if (isEnabled)
            SetMissionComplete();
    }

    public override void UpdateMissionTab()
    {
        if(currentMissionTab == null)
            return;
        
        var fillamount = 0;
        
        var marketPlaces = InteractionManager.Instance.GetAllAvailableInteractables<NFMarketPlace>(InteractableType.NFMarketPlace);
        bool isEnabled = false;

        foreach (var market in marketPlaces)
        {
            if (market.NFMarketPlace.ID == MarketUnlockID)
                isEnabled = true;
        }

        if (isEnabled)
        {
            fillamount = 1;
            currentMissionTab.UpdateText(new IdleNumber(1, NumberDigits.Empty));
        }
        else
        {
            fillamount = 0;
            currentMissionTab.UpdateText(new IdleNumber(0 , NumberDigits.Empty));
            
        }
        
        currentMissionTab.UpdateFillbar(fillamount);
        
        base.UpdateMissionTab();
    }

    public override void SetMissionComplete()
    {
        //MissionManager.Instance.DeleteMissionFromTheList(this);
        base.SetMissionComplete();
    }

    public override void CompleteAction()
    {
        RemoveListeners();
        base.CompleteAction();
        /*MissionManager.Instance.GiveMissionReward(this); 
        MissionManager.Instance.OnMissionCompleted?.Invoke();
        SaveData.CompletedBefore = true;
        UpdateSaveData();*/
    }

    public override string GetInfoText()
    {
        string info = UnlockMarketInfo;
        string repleacedOne = info.Replace("$", UnlockMarketInfoOverride);

        return repleacedOne;
        //return base.GetInfoText();
        //return $"Spend {CollectProductAmountWanted.ToString()} {CollectProductType.ToString()}";
    }
}
