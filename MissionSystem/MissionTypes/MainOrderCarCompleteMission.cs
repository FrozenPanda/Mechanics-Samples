using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainOrderCarCompleteMission : BaseMission
{
    public override void InitilizeMission()
    {
        base.InitilizeMission();
        //var missionSprite = MissionManager.Instance.GetMissionSpriteByType(MissionType);
        currentMissionTab.SetUItab(missionSprite , OrderBoardCompleteTarget , OrderBoardCompleteCurrent , GetInfoText());
    }

    protected override void GetSaveData()
    {
        base.GetSaveData();
        OrderBoardCompleteCurrent = SaveData.CurrentAmount;
        var remainOrderCount = OrderManager.Instance.GetRemainMainOrderCount();
        if (OrderBoardCompleteTarget - OrderBoardCompleteCurrent > OrderManager.Instance.GetRemainMainOrderCount())
        {
            
            
            OrderBoardCompleteCurrent = OrderBoardCompleteTarget -
                                        new IdleNumber(OrderManager.Instance.GetRemainMainOrderCount(),
                                            NumberDigits.Empty);
        }
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

        OrderBoardCompleteCurrent += new IdleNumber(1, NumberDigits.Empty);
        UpdateSaveData();
        
        CheckMissionComplete();
        UpdateMissionTab();
    }

    public override void CheckMissionComplete()
    {
        base.CheckMissionComplete();
        if(OrderBoardCompleteCurrent >= OrderBoardCompleteTarget)
            SetMissionComplete();
    }

    public override void UpdateMissionTab()
    {
        if(currentMissionTab == null)
            return;
        
        var fillamount = OrderBoardCompleteCurrent.ToFloat() / OrderBoardCompleteTarget.ToFloat();
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
        return $"Complete {OrderBoardCompleteTarget} orders";
    }
}
