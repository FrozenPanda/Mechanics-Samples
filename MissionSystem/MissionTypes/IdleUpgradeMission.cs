using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleUpgradeMission : BaseMission
{
    public override void InitilizeMission()
    {
        base.InitilizeMission();
        //var completedIdleUpgradeCount = IdleUpgradeManager.Instance.UpgradeCounter;
        currentMissionTab.SetUItab(missionSprite, new IdleNumber(IdleUpgradeCount, NumberDigits.Empty), new IdleNumber(IdleUpgradeCurrent, NumberDigits.Empty), GetInfoText());
    }

    protected override void GetSaveData()
    {
        base.GetSaveData();
        IdleUpgradeCurrent = (int)SaveData.CurrentAmount.ToFloat();
    }

    protected override void UpdateSaveData()
    {
        base.UpdateSaveData();
        SaveData.CurrentAmount = new IdleNumber(IdleUpgradeCurrent, NumberDigits.Empty);
    }

    protected override void AddListeners()
    {
        base.AddListeners();
        //IdleUpgradeManager.Instance.OnIdleUpgradeCompleted.AddListener(UpdateCurrentProgress);
        NF_Spin_Manager.Instance.OnSpinRewardCollected.AddListener(UpdateCurrentProgress);
        MissionManager.Instance.OnMissionsRefreshed.AddListener(ReleaseTab);
    }

    public override void ReleaseTab()
    {
        base.ReleaseTab();
        currentMissionTab = null;
        ReleaseTabBool = true;
    }

    public override void RemoveListeners()
    {
        base.RemoveListeners();
        //IdleUpgradeManager.Instance.OnIdleUpgradeCompleted.RemoveListener(UpdateCurrentProgress);
        NF_Spin_Manager.Instance.OnSpinRewardCollected.RemoveListener(UpdateCurrentProgress);
    }

    public override void UpdateCurrentProgress()
    {
        base.UpdateCurrentProgress();

        IdleUpgradeCurrent++;
        
        UpdateSaveData();
        CheckMissionComplete();
        UpdateMissionTab();
    }

    public override void CheckMissionComplete()
    {
        base.CheckMissionComplete();

        //var completedIdleUpgradeCount = IdleUpgradeManager.Instance.UpgradeCounter;

        if (IdleUpgradeCurrent >= IdleUpgradeCount)
            SetMissionComplete();
    }

    public override void UpdateMissionTab()
    {
        if (currentMissionTab == null)
            return;

        //var completedIdleUpgradeCount = IdleUpgradeManager.Instance.UpgradeCounter;

        var fillamount = (float)IdleUpgradeCurrent / IdleUpgradeCount;
        currentMissionTab.UpdateFillbar(fillamount);
        currentMissionTab.UpdateText(new IdleNumber(IdleUpgradeCurrent, NumberDigits.Empty));

        base.UpdateMissionTab();
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

        return $"Spin {IdleUpgradeCount} " + (IdleUpgradeCount > 1 ? "times" : "time");
    }

}
