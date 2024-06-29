using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _Game.Scripts.Systems.StarUpgradeSystem;
using System.Linq;

public class LevelUpGeneratorMission : BaseMission
{
     public override void InitilizeMission()
    {
        base.InitilizeMission();
        currentMissionTab.SetUItab(missionSprite , LevelUpGeneratorTarget , LevelUpGeneratorCurrent , GetInfoText());
    }

     protected override void GetSaveData()
    {
        base.GetSaveData();
        //LevelUpGeneratorCurrent = SaveData.CurrentAmount;
        
        var starData = StarUpgradeManager.Instance.GetUpgradeByInteractableId(LevelUpgeneratorId);
        var pool = starData.StarUpgradeProductionDataByPoolType.First().Key;
        var generatorLevel = StarUpgradeManager.Instance.GetUpgradeLevel(LevelUpgeneratorId , pool);
        LevelUpGeneratorCurrent = new IdleNumber((float)generatorLevel, NumberDigits.Empty);
    }

    protected override void UpdateSaveData()
    {
        if (SaveData != null)
            SaveData.CurrentAmount = LevelUpGeneratorCurrent;
        
        base.UpdateSaveData();
    }

    protected override void AddListeners()
    {
        base.AddListeners();
        StarUpgradeManager.Instance.OnProductMachineUpgraded.AddListener(UpdateCurrentProgress);
        MissionManager.Instance.OnMissionsRefreshed.AddListener(ReleaseTab);
    }
    
    public override void RemoveListeners()
    {
        base.RemoveListeners();
        StarUpgradeManager.Instance.OnProductMachineUpgraded.RemoveListener(UpdateCurrentProgress);
    }

    public override void ReleaseTab()
    {
        base.ReleaseTab();
        currentMissionTab = null;
        ReleaseTabBool = true;
    }
    
    public override void UpdateCurrentProgress()
    {
        base.UpdateCurrentProgress();
        
        var starData = StarUpgradeManager.Instance.GetUpgradeByInteractableId(LevelUpgeneratorId);
        var pool = starData.StarUpgradeProductionDataByPoolType.First().Key;
        var generatorLevel = StarUpgradeManager.Instance.GetUpgradeLevel(LevelUpgeneratorId , pool);
        LevelUpGeneratorCurrent = new IdleNumber((float)generatorLevel, NumberDigits.Empty);
        
        UpdateSaveData();
        CheckMissionComplete();
        UpdateMissionTab();
    }

    public override void CheckMissionComplete()
    {
        base.CheckMissionComplete();
        
        if(LevelUpGeneratorCurrent >= LevelUpGeneratorTarget)
            SetMissionComplete();
    }

    public override void UpdateMissionTab()
    {
        if(currentMissionTab == null)
            return;
        
        var fillamount = LevelUpGeneratorCurrent.ToFloat() / LevelUpGeneratorTarget.ToFloat();
        currentMissionTab.UpdateFillbar(fillamount);
        currentMissionTab.UpdateText(LevelUpGeneratorCurrent);
        base.UpdateMissionTab();
    }

    public override void SetMissionComplete()
    {
        base.SetMissionComplete();
        RemoveListeners();
    }

    public override void CompleteAction()
    {
        RemoveListeners();
        //MissionManager.Instance.DeleteMissionFromTheList(this);
        base.CompleteAction();
    }

    public override string GetInfoText()
    {
        string info = LevelUpGeneratorInfo;
        string repleacedOne = info.Replace("$", LevelUpGeneratorInfoOverride).Replace("#", LevelUpGeneratorTarget.ToString());
        return repleacedOne;
    }
}
