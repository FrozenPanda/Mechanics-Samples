using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectProductMission : BaseMission
{
    public override void InitilizeMission()
    {
        base.InitilizeMission();
        currentMissionTab.SetUItab(missionSprite , CollectProductAmountWanted , CollectProductAmountCurrent , GetInfoText());
    }

    protected override void GetSaveData()
    {
        base.GetSaveData();
        CollectProductAmountCurrent = SaveData.CurrentAmount;
    }

    protected override void UpdateSaveData()
    {
        SaveData.CurrentAmount = CollectProductAmountCurrent;
        base.UpdateSaveData();
    }

    protected override void AddListeners()
    {
        base.AddListeners();
        NFInventoryManager.Instance.OnAddedItem.AddListener(UpdateCurrentProgress);
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
        NFInventoryManager.Instance.OnAddedItem.RemoveListener(UpdateCurrentProgress);
    }

    public override void UpdateCurrentProgress(PoolType item , IdleNumber amount)
    {
        if(base.ReleaseTabBool)
            return;

        base.UpdateCurrentProgress(item , amount);
        if (CollectProductType == item)
        {
            CollectProductAmountCurrent += amount;
            UpdateSaveData();
        }
        
        CheckMissionComplete();
        UpdateMissionTab();
    }

    public override void CheckMissionComplete()
    {
        base.CheckMissionComplete();
        if(CollectProductAmountCurrent >= CollectProductAmountWanted)
            SetMissionComplete();
    }

    public override void UpdateMissionTab()
    {
        if(currentMissionTab == null)
            return;
        
        var fillamount = CollectProductAmountCurrent.ToFloat() / CollectProductAmountWanted.ToFloat();
        currentMissionTab.UpdateFillbar(fillamount);
        
        currentMissionTab.UpdateText(CollectProductAmountCurrent);
        
        base.UpdateMissionTab();
    }

    public override void SetMissionComplete()
    {
        //RemoveListeners();
        //MissionManager.Instance.DeleteMissionFromTheList(this);
        base.SetMissionComplete();
    }

    public override void CompleteAction()
    {
        RemoveListeners();
        MissionManager.Instance.DeleteMissionFromTheList(this);
        base.CompleteAction();
    }

    public override string GetInfoText()
    {
        //return base.GetInfoText();
        return $"Produce {CollectProductAmountWanted.ToString()} {CollectProductType.ToString()}";
    }

    public void Deletteee()
    {
        
    }
}
