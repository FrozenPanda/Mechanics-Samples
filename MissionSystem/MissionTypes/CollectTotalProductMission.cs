using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectTotalProductMission : BaseMission
{
    // Start is called before the first frame update
    public override void InitilizeMission()
    {
        base.InitilizeMission();
        currentMissionTab.SetUItab(missionSprite , CollectTotalProductAmountWanted , CollectTotalProductAmountCurrent , GetInfoText());
    }

    protected override void GetSaveData()
    {
        base.GetSaveData();
        CollectTotalProductAmountCurrent = NFInventoryManager.Instance.GetCountInInventory(CollectTotalProductType); 
    }

    protected override void UpdateSaveData()
    {
        SaveData.CurrentAmount = CollectTotalProductAmountCurrent;
        base.UpdateSaveData();
    }

    protected override void AddListeners()
    {
        base.AddListeners();
        NFInventoryManager.Instance.OnAddedItem.AddListener(UpdateCurrentProgress);
        NFInventoryManager.Instance.OnRemoveItem.AddListener(UpdateCurrentProgress);
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
        NFInventoryManager.Instance.OnRemoveItem.RemoveListener(UpdateCurrentProgress);
        NFInventoryManager.Instance.OnAddedItem.RemoveListener(UpdateCurrentProgress);
    }

    public override void UpdateCurrentProgress(PoolType item , IdleNumber amount)
    {
        if(base.ReleaseTabBool)
            return;

        base.UpdateCurrentProgress(item , amount);
        if (CollectTotalProductType == item)
        {
            CollectTotalProductAmountCurrent = NFInventoryManager.Instance.GetCountInInventory(CollectTotalProductType); 
            UpdateSaveData();
        }
        
        CheckMissionComplete();
        UpdateMissionTab();
    }

    public override void CheckMissionComplete()
    {
        base.CheckMissionComplete();
        if(CollectTotalProductAmountCurrent >= CollectTotalProductAmountWanted)
            SetMissionComplete();
    }

    public override void UpdateMissionTab()
    {
        if(currentMissionTab == null)
            return;
        
        var fillamount = CollectTotalProductAmountCurrent.ToFloat() / CollectTotalProductAmountWanted.ToFloat();
        currentMissionTab.UpdateFillbar(fillamount);
        
        currentMissionTab.UpdateText(CollectTotalProductAmountCurrent);
        
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
        return $"Own {CollectTotalProductAmountWanted.ToString()} {CollectTotalProductType.ToString()}";
    }
}
