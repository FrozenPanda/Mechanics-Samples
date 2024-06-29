using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _Game.Scripts.Systems.StarUpgradeSystem;

public class AllMarketMax : BaseMission
{
     public override void InitilizeMission()
    {
        base.InitilizeMission();
        var allMarkets = InteractionManager.Instance
            .GetAllAvailableInteractables<NFMarketPlace>(InteractableType.NFMarketPlace);
        AllMarketMaxTarget =
            new IdleNumber(
                InteractionManager.Instance
                    .GetAllAvailableInteractables<NFMarketPlace>(InteractableType.NFMarketPlace).Count,
                NumberDigits.Empty);
        //OrderBoardCompleteTarget = new IdleNumber(1f, NumberDigits.Empty);
        //var missionSprite = MissionManager.Instance.GetMissionSpriteByType(MissionType);
        currentMissionTab.SetUItab(missionSprite , AllMarketMaxCurrent , AllMarketMaxTarget , GetInfoText());
    }

    protected override void GetSaveData()
    {
        base.GetSaveData();
        AllMarketMaxCurrent = SaveData.CurrentAmount;
    }

    protected override void UpdateSaveData()
    {
        SaveData.CurrentAmount = AllMarketMaxCurrent;
        base.UpdateSaveData();
    }

    protected override void AddListeners()
    {
        base.AddListeners();
        StarUpgradeManager.Instance.OnProductMachineUpgraded.AddListener(UpdateCurrentProgress);
        MissionManager.Instance.OnMissionsRefreshed.AddListener(ReleaseTab);
    }

    public override void RemoveListeners()
    {
        base.RemoveListeners();
        StarUpgradeManager.Instance.OnProductMachineUpgraded.RemoveListener(UpdateCurrentProgress);
    }
    
    public override void ReleaseTab()
    {
        base.ReleaseTab();
        currentMissionTab = null;
        ReleaseTabBool = true;
    }
    
    public override void UpdateCurrentProgress()
    {
        //base.UpdateCurrentProgress();

        AllMarketMaxCurrent = new IdleNumber();
        var ProductContainersList = InteractionManager.Instance.GetAllAvailableInteractables<NFMarketPlace>(InteractableType.NFMarketPlace);
        foreach (var container in ProductContainersList)
        {
            int currentLevel = StarUpgradeManager.Instance.GetUpgradeLevel(container.ID, container.GetObjectType());
            int maxLevel = StarUpgradeManager.Instance.GetMaxUpgradeLevel(container.ID);

            bool isUpgradeLevelMax = (currentLevel >= maxLevel);

            if (isUpgradeLevelMax)
                AllMarketMaxCurrent += new IdleNumber(1f, NumberDigits.Empty);
        }
        
        
        UpdateSaveData();
        
        CheckMissionComplete();
        UpdateMissionTab();
    }

    public override void CheckMissionComplete()
    {
        base.CheckMissionComplete();
        if(AllMarketMaxCurrent >= AllMarketMaxTarget)
            SetMissionComplete();
    }

    public override void UpdateMissionTab()
    {
        if(currentMissionTab == null)
            return;
        
        var fillamount = AllMarketMaxCurrent.ToFloat() / AllMarketMaxTarget.ToFloat();
        currentMissionTab.UpdateFillbar(fillamount);
        
        currentMissionTab.UpdateText(AllgeneratorMaxCurrent);
        
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
        //return base.GetInfoText();
        return $"Level up max all markets";
    }
}
