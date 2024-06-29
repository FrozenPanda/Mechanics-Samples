using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SideOrderCarCompleteMission : BaseMission
{
    public override void InitilizeMission()
    {
        base.InitilizeMission();
        currentMissionTab.SetUItab(missionSprite , SideOrderCarCompleteTarget , SideOrderCarCompleteCurrent , GetInfoText());
        
    }

    protected override void GetSaveData()
    {
        base.GetSaveData();
        SideOrderCarCompleteCurrent = SaveData.CurrentAmount;
    }

    protected override void UpdateSaveData()
    {
        SaveData.CurrentAmount = SideOrderCarCompleteCurrent;
        base.UpdateSaveData();
    }

    protected override void AddListeners()
    {
        base.AddListeners();
        OrderManager.Instance.OnSideorderCarSent.AddListener(UpdateCurrentProgress);
        MissionManager.Instance.OnMissionsRefreshed.AddListener(ReleaseTab);
    }

    public override void RemoveListeners()
    {
        base.RemoveListeners();
        OrderManager.Instance.OnSideorderCarSent.RemoveListener(UpdateCurrentProgress);
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

        SideOrderCarCompleteCurrent += new IdleNumber(1, NumberDigits.Empty);
        UpdateSaveData();
        
        CheckMissionComplete();
        UpdateMissionTab();
    }

    public override void CheckMissionComplete()
    {
        base.CheckMissionComplete();
        if(SideOrderCarCompleteCurrent >= SideOrderCarCompleteTarget)
            SetMissionComplete();
    }

    public override void UpdateMissionTab()
    {
        if(currentMissionTab == null)
            return;
        
        var fillamount = SideOrderCarCompleteCurrent.ToFloat() / SideOrderCarCompleteTarget.ToFloat();
        currentMissionTab.UpdateFillbar(fillamount);
        
        currentMissionTab.UpdateText(SideOrderCarCompleteCurrent);
        
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
        return $"Complete {SideOrderCarCompleteTarget} drive-thru";
    }
}
