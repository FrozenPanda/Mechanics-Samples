using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectCurrencyMission : BaseMission
{
    public override void InitilizeMission()
    {
        base.InitilizeMission();
        currentMissionTab.SetUItab(missionSprite , CurrencyAmountWanted , CurrencyAmountCurrent , GetInfoText());
    }
    
    protected override void GetSaveData()
    {
        base.GetSaveData();
        CurrencyAmountCurrent = SaveData.CurrentAmount;
    }

    protected override void UpdateSaveData()
    {
        if (SaveData != null)
            SaveData.CurrentAmount = CurrencyAmountCurrent;
        
        base.UpdateSaveData();
    }

    protected override void AddListeners()
    {
        base.AddListeners();
        IdleExchangeService.OnDoExchange[CurrencyType].AddListener(UpdateCurrentProgress);
        MissionManager.Instance.OnMissionsRefreshed.AddListener(ReleaseTab);
    }
    
    public override void RemoveListeners()
    {
        base.RemoveListeners();
        IdleExchangeService.OnDoExchange[CurrencyType].RemoveListener(UpdateCurrentProgress);
    }
    
    public override void ReleaseTab()
    {
        base.ReleaseTab();
        currentMissionTab = null;
        ReleaseTabBool = true;
    }
    

    public override void UpdateCurrentProgress(IdleNumber change, IdleNumber value)
    {
        if(currentMissionTab == null)
            return;
        
        if (CollectOrSpendCurrency == CurrencyMissionType.Spend && change < 0f)
        {
            CurrencyAmountCurrent -= change;
            UpdateSaveData();
        }
        else if (CollectOrSpendCurrency == CurrencyMissionType.Collect && change > 0f)
        {
            CurrencyAmountCurrent += change;
            UpdateSaveData();
        }
        
        UpdateSaveData();
        CheckMissionComplete();
        UpdateMissionTab();
    }

    public override void CheckMissionComplete()
    {
        base.CheckMissionComplete();
        
        if(CurrencyAmountCurrent >= CurrencyAmountWanted)
            SetMissionComplete();
    }

    public override void UpdateMissionTab()
    {
        var fillamount = CurrencyAmountCurrent.ToFloat() / CurrencyAmountWanted.ToFloat();
        currentMissionTab.UpdateFillbar(fillamount);
        currentMissionTab.UpdateText(CurrencyAmountCurrent);
        
        base.UpdateMissionTab();
    }

    public override void SetMissionComplete()
    {
        base.SetMissionComplete();
        //RemoveListeners();
    }

    public override void CompleteAction()
    {
        RemoveListeners();
        //MissionManager.Instance.DeleteMissionFromTheList(this);
        base.CompleteAction();
    }

    public override string GetInfoText()
    {
        if (CollectOrSpendCurrency == CurrencyMissionType.Collect)
            return $"Collect {CurrencyAmountWanted.ToString()} {CurrencyType.ToString()}s";
        if(CollectOrSpendCurrency == CurrencyMissionType.Spend)
            return $"Spend {CurrencyAmountWanted.ToString()} {CurrencyType.ToString()}s";
        return "";
    }
}
