using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnlockGeneratorMission : BaseMission
{
    public override void InitilizeMission()
    {
        base.InitilizeMission();
        currentMissionTab.SetUItab(missionSprite , new IdleNumber(1, NumberDigits.Empty) , new IdleNumber(0, NumberDigits.Empty) , GetInfoText());
    }

    protected override void GetSaveData()
    {
        base.GetSaveData();
    }

    protected override void UpdateSaveData()
    {
        base.UpdateSaveData();
    }

    protected override void AddListeners()
    {
        base.AddListeners();
        ExpendManager.Instance.OnTileExpend.AddListener(UpdateCurrentProgress);
        MissionManager.Instance.OnMissionsRefreshed.AddListener(ReleaseTab);
    }

    public override void RemoveListeners()
    {
        base.RemoveListeners();
        ExpendManager.Instance.OnTileExpend.RemoveListener(UpdateCurrentProgress);
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
        if (CollectProductType == item)
        {
            CollectProductAmountCurrent += amount;
            UpdateSaveData();
        }
        
        CheckMissionComplete();
        UpdateMissionTab();
    }

    public override void UpdateCurrentProgress()
    {
        base.UpdateCurrentProgress();
        
        CheckMissionComplete();
        UpdateMissionTab();
    }

    public override void CheckMissionComplete()
    {
        base.CheckMissionComplete();
        if (ExpendManager.Instance.IsOpenedExpension(UnlockGeneratorId))
            SetMissionComplete();
    }

    public override void UpdateMissionTab()
    {
        if(currentMissionTab == null)
            return;
        
        var fillamount = 0;

        if (ExpendManager.Instance.IsOpenedExpension(UnlockGeneratorId))
        {
            fillamount = 1;
            currentMissionTab.UpdateText(new IdleNumber(1, NumberDigits.Empty));
        }
        else
        {
            fillamount = 0;
            currentMissionTab.UpdateText(new IdleNumber(0 , NumberDigits.Empty));
            
        }
        
        currentMissionTab.UpdateFillbar(fillamount);
        
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
        string info = UnlockGeneratorInfo;
        string repleacedOne = info.Replace("$", UnlockGeneratorInfoOverride);

        return repleacedOne;
        //return base.GetInfoText();
        //return $"Spend {CollectProductAmountWanted.ToString()} {CollectProductType.ToString()}";
    }
}
