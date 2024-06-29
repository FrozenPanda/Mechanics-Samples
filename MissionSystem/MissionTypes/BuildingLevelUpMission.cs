using System.Collections;
using System.Collections.Generic;
using _Game.Scripts.Systems.StarUpgradeSystem;
using UnityEngine;

public class BuildingLevelUpMission : BaseMission
{
    public override void InitilizeMission()
    {
        base.InitilizeMission();
        currentMissionTab.SetUItab(missionSprite , BuildingUpgradeAmountWanted , BuildingUpgradeAmountCurrent , GetInfoText());
    }

     protected override void GetSaveData()
    {
        base.GetSaveData();
        BuildingUpgradeAmountCurrent = new IdleNumber(StarUpgradeManager.Instance.GetBuildingUpgradedLevel(BuildingID) , NumberDigits.Empty);
    }

    protected override void UpdateSaveData()
    {
        if (SaveData != null)
            SaveData.CurrentAmount = BuildingUpgradeAmountCurrent;
        
        base.UpdateSaveData();
    }

    protected override void AddListeners()
    {
        base.AddListeners();
        StarUpgradeManager.Instance.OnUpgradeablePurchasedWithID.AddListener(UpdateCurrentProgress);
        MissionManager.Instance.OnMissionsRefreshed.AddListener(ReleaseTab);
    }
    
    public override void RemoveListeners()
    {
        base.RemoveListeners();
        StarUpgradeManager.Instance.OnUpgradeablePurchasedWithID.RemoveListener(UpdateCurrentProgress);
    }

    public override void ReleaseTab()
    {
        base.ReleaseTab();
        currentMissionTab = null;
        ReleaseTabBool = true;
    }
    
    public override void UpdateCurrentProgress(string id)
    {
        base.UpdateCurrentProgress();

        if (id == BuildingID)
        {
            BuildingUpgradeAmountCurrent = new IdleNumber(StarUpgradeManager.Instance.GetBuildingUpgradedLevel(BuildingID) , NumberDigits.Empty);
        }
        
        UpdateSaveData();
        CheckMissionComplete();
        UpdateMissionTab();
    }

    public override void CheckMissionComplete()
    {
        base.CheckMissionComplete();
        
        if(BuildingUpgradeAmountCurrent >= BuildingUpgradeAmountWanted)
            SetMissionComplete();
    }

    public override void UpdateMissionTab()
    {
        if(currentMissionTab == null)
            return;
        
        var fillamount = BuildingUpgradeAmountCurrent.ToFloat() / BuildingUpgradeAmountWanted.ToFloat();
        currentMissionTab.UpdateFillbar(fillamount);
        currentMissionTab.UpdateText(BuildingUpgradeAmountCurrent);
        base.UpdateMissionTab();
    }

    public override void SetMissionComplete()
    {
        base.SetMissionComplete();
        RemoveListeners();
    }

    public override void CompleteAction()
    {
        RemoveListeners();
        //MissionManager.Instance.DeleteMissionFromTheList(this);
        base.CompleteAction();
    }

    public override string GetInfoText()
    {
        string info = BuildingUpgradeInfo;
        string repleacedOne = info.Replace("$", BuildingUpgradeInfoOverride).Replace("#" , BuildingUpgradeAmountWanted.ToString());
        return repleacedOne;
    }
}
