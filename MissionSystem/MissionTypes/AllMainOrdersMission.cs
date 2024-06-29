using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllMainOrdersMission : BaseMission
{
    public override void InitilizeMission()
    {
        base.InitilizeMission();
        //OrderBoardCompleteTarget = new IdleNumber(1f, NumberDigits.Empty);
        //var missionSprite = MissionManager.Instance.GetMissionSpriteByType(MissionType);
        currentMissionTab.SetUItab(missionSprite , new IdleNumber(1f, NumberDigits.Empty) , OrderBoardCompleteCurrent , GetInfoText());
    }

    protected override void GetSaveData()
    {
        base.GetSaveData();
        OrderBoardCompleteCurrent = SaveData.CurrentAmount;
        
        OrderBoardCompleteCurrent = OrderManager.Instance.GetRemainMainOrderCount() >= 1 ? new IdleNumber(0f , NumberDigits.Empty) : new IdleNumber(1f , NumberDigits.Empty);
    }

    protected override void UpdateSaveData()
    {
        SaveData.CurrentAmount = OrderBoardCompleteCurrent;
        base.UpdateSaveData();
    }

    protected override void AddListeners()
    {
        base.AddListeners();
        OrderCarManager.Instance.onOrderCarRewardGiven.AddListener(UpdateCurrentProgress);
        MissionManager.Instance.OnMissionsRefreshed.AddListener(ReleaseTab);
    }

    public override void RemoveListeners()
    {
        base.RemoveListeners();
        OrderCarManager.Instance.onOrderCarRewardGiven.RemoveListener(UpdateCurrentProgress);
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

        OrderBoardCompleteCurrent = OrderManager.Instance.GetRemainMainOrderCount() >= 1 ? new IdleNumber(0f , NumberDigits.Empty) : new IdleNumber(1f , NumberDigits.Empty);
        UpdateSaveData();
        
        CheckMissionComplete();
        UpdateMissionTab();
    }

    public override void CheckMissionComplete()
    {
        base.CheckMissionComplete();
        if(OrderManager.Instance.GetRemainMainOrderCount() < 1)
            SetMissionComplete();
    }

    public override void UpdateMissionTab()
    {
        if(currentMissionTab == null)
            return;
        
        var fillamount = OrderBoardCompleteCurrent.ToFloat() / new IdleNumber(1f , NumberDigits.Empty).ToFloat();
        currentMissionTab.UpdateFillbar(fillamount);
        
        currentMissionTab.UpdateText(OrderBoardCompleteCurrent);
        
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
        return $"Complete all main orders";
    }
}
