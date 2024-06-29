using System.Collections;
using System.Collections.Generic;
using _Game.Scripts.Systems.StarUpgradeSystem;
using UnityEngine;

public class AllGeneratorMaxMission : BaseMission
{
    public override void InitilizeMission()
    {
        base.InitilizeMission();
        AllgeneratorMaxTarget =
            new IdleNumber(
                InteractionManager.Instance
                    .GetAllAvailableInteractables<NFProductContainer>(InteractableType.NFProductContainer).Count,
                NumberDigits.Empty);
        //OrderBoardCompleteTarget = new IdleNumber(1f, NumberDigits.Empty);
        //var missionSprite = MissionManager.Instance.GetMissionSpriteByType(MissionType);
        currentMissionTab.SetUItab(missionSprite , AllgeneratorMaxTarget , AllgeneratorMaxCurrent , GetInfoText());
    }

    protected override void GetSaveData()
    {
        base.GetSaveData();
        AllgeneratorMaxCurrent = SaveData.CurrentAmount;
    }

    protected override void UpdateSaveData()
    {
        SaveData.CurrentAmount = AllgeneratorMaxCurrent;
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

        AllgeneratorMaxCurrent = new IdleNumber();
        var ProductContainersList = InteractionManager.Instance.GetAllAvailableInteractables<NFProductContainer>(InteractableType.NFProductContainer);
        foreach (var container in ProductContainersList)
        {
            int currentLevel = StarUpgradeManager.Instance.GetUpgradeLevel(container.ID, container.GetObjectType());
            int maxLevel = StarUpgradeManager.Instance.GetMaxUpgradeLevel(container.ID);

            bool isUpgradeLevelMax = (currentLevel >= maxLevel);

            if (isUpgradeLevelMax)
                AllgeneratorMaxCurrent += new IdleNumber(1f, NumberDigits.Empty);
        }
        
        
        UpdateSaveData();
        
        CheckMissionComplete();
        UpdateMissionTab();
    }

    public override void CheckMissionComplete()
    {
        base.CheckMissionComplete();
        if(AllgeneratorMaxCurrent >= AllgeneratorMaxTarget)
            SetMissionComplete();
    }

    public override void UpdateMissionTab()
    {
        if(currentMissionTab == null)
            return;
        
        var fillamount = AllgeneratorMaxCurrent.ToFloat() / AllgeneratorMaxTarget.ToFloat();
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
        return $"Level up max all generators";
    }
}
