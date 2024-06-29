using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SPentProductMission : BaseMission
{
    public override void InitilizeMission()
    {
        base.InitilizeMission();
        currentMissionTab.SetUItab(missionSprite , SpendProductAmountWanted , SpentProductAmountCurrent , GetInfoText());
    }

    protected override void GetSaveData()
    {
        base.GetSaveData();
        SpentProductAmountCurrent = SaveData.CurrentAmount;
    }

    protected override void UpdateSaveData()
    {
        SaveData.CurrentAmount = SpentProductAmountCurrent;
        base.UpdateSaveData();
    }

    protected override void AddListeners()
    {
        base.AddListeners();
        NFInventoryManager.Instance.OnRemoveItem.AddListener(UpdateCurrentProgress);
        MissionManager.Instance.OnMissionsRefreshed.AddListener(ReleaseTab);
    }

    public override void RemoveListeners()
    {
        base.RemoveListeners();
        NFInventoryManager.Instance.OnRemoveItem.RemoveListener(UpdateCurrentProgress);
    }
    
    public override void ReleaseTab()
    {
        base.ReleaseTab();
        currentMissionTab = null;
        ReleaseTabBool = true;
    }

    public override void UpdateCurrentProgress(PoolType item , IdleNumber amount)
    {
        base.UpdateCurrentProgress(item , amount);
        if (SpendProdcutType == item)
        {
            SpentProductAmountCurrent += amount;
            UpdateSaveData();
        }
        
        CheckMissionComplete();
        UpdateMissionTab();
    }

    public override void CheckMissionComplete()
    {
        base.CheckMissionComplete();
        if(SpentProductAmountCurrent >= SpendProductAmountWanted)
            SetMissionComplete();
    }

    public override void UpdateMissionTab()
    {
        if(currentMissionTab == null)
            return;
        
        var fillamount = SpentProductAmountCurrent.ToFloat() / SpendProductAmountWanted.ToFloat();
        currentMissionTab.UpdateFillbar(fillamount);
        
        currentMissionTab.UpdateText(SpentProductAmountCurrent);
        
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
        return $"Spend {SpendProductAmountWanted.ToString()} {SpendProdcutType.ToString()}";
    }
}
