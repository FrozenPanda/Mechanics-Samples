using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarketAutomateMission : BaseMission
{
    public override void InitilizeMission()
    {
        base.InitilizeMission();
        currentMissionTab.SetUItab(missionSprite , new IdleNumber(1f , NumberDigits.Empty) , CurrencyAmountCurrent , GetInfoText());
        UpdateCurrentProgress("" , new Sticker() , true);
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
        StickerManager.Instance.OnStickerStateChanged.AddListener(UpdateCurrentProgress);
        DirectorManager.Instance.OnDirectorCarUpgraded.AddListener(FakeUpdateProgress);
        MissionManager.Instance.OnMissionsRefreshed.AddListener(ReleaseTab);
    }
    
    public override void RemoveListeners()
    {
        base.RemoveListeners();
        DirectorManager.Instance.OnDirectorCarUpgraded.RemoveListener(FakeUpdateProgress);
        StickerManager.Instance.OnStickerStateChanged.RemoveListener(UpdateCurrentProgress);
    }
    
    public override void ReleaseTab()
    {
        base.ReleaseTab();
        currentMissionTab = null;
        ReleaseTabBool = true;
    }

    private void FakeUpdateProgress()
    {
        UpdateCurrentProgress("" , new Sticker() , true);
    }

    public override void UpdateCurrentProgress(string a , Sticker sticker , bool b)
    {
        if(currentMissionTab == null)
            return;

        var marketInteractable = InteractionManager.Instance.GetInteractableById(MarketID);
        
        if(marketInteractable == null)
            return;
        
        var market = InteractionManager.Instance.GetInteractableById(MarketID).GetComponent<NFMarketPlace>();
        
        if(market == null)
            return;
        
        var isAutomated = market.AutomateActiveState;
        
        if (isAutomated)
            CurrencyAmountCurrent = new IdleNumber(1f, NumberDigits.Empty);
        
        UpdateSaveData();
        CheckMissionComplete();
        UpdateMissionTab();
    }

    public override void CheckMissionComplete()
    {
        base.CheckMissionComplete();

        var marketInteractable = InteractionManager.Instance.GetInteractableById(MarketID);
        
        if(marketInteractable == null)
            return;
        
        var market = InteractionManager.Instance.GetInteractableById(MarketID).GetComponent<NFMarketPlace>();
        
        if(market == null)
            return;
        
        var isAutomated = market.AutomateActiveState;
        if(isAutomated)
            SetMissionComplete();
    }

    public override void UpdateMissionTab()
    {
        var fillamount = CurrencyAmountCurrent.ToFloat() / new IdleNumber(1f, NumberDigits.Empty).ToFloat();
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
        string info = AutomateMarketInfo;
        string repleacedOne = info.Replace("$", AutomateMarketInfoOverride);
        return repleacedOne;
    }
}
