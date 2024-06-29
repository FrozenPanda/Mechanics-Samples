using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManagerUpgradeMission : BaseMission
{
    public override void InitilizeMission()
    {
        base.InitilizeMission();
        //var managerData = DirectorManager.Instance.GetDirectorCardDataById(ManagerId);
        //currentMissionTab.SetUItab(missionSprite, new IdleNumber(ManagerLevel, NumberDigits.Empty), new IdleNumber(managerData != null ? managerData.Level : 0, NumberDigits.Empty), GetInfoText());
        currentMissionTab.SetUItab(missionSprite, ManagerLevelWanted, ManagerLevelCurrent, GetInfoText());
    }

    protected override void GetSaveData()
    {
        base.GetSaveData();
        ManagerLevelCurrent = SaveData.CurrentAmount;
    }

    protected override void UpdateSaveData()
    {
        SaveData.CurrentAmount = ManagerLevelCurrent;
        base.UpdateSaveData();
    }

    protected override void AddListeners()
    {
        base.AddListeners();
        DirectorManager.Instance.DirectorCardUpgradeEvent.AddListener(UpdateCurrentProgress);
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
        DirectorManager.Instance.DirectorCardUpgradeEvent.RemoveListener(UpdateCurrentProgress);
    }

    public override void UpdateCurrentProgress(int id)
    {
        base.UpdateCurrentProgress(id);

        if (true)
        {
            ManagerLevelCurrent += new IdleNumber(1f, NumberDigits.Empty);
            UpdateSaveData();
            CheckMissionComplete();
            UpdateMissionTab();
        }
    }

    public override void CheckMissionComplete()
    {
        base.CheckMissionComplete();

        //var managerData = DirectorManager.Instance.GetDirectorCardDataById(ManagerId);

        if (ManagerLevelCurrent >= ManagerLevelWanted)
            SetMissionComplete();
    }

    public override void UpdateMissionTab()
    {
        if (currentMissionTab == null)
            return;

        /*var managerLevel = 0;
        var managerData = DirectorManager.Instance.GetDirectorCardDataById(ManagerId);
        if (managerData != null) managerLevel = managerData.Level;*/

        var fillamount = ManagerLevelCurrent.ToFloat() / ManagerLevelWanted.ToFloat();
        currentMissionTab.UpdateFillbar(fillamount);
        currentMissionTab.UpdateText(ManagerLevelCurrent);

        base.UpdateMissionTab();
    }

    public override string GetInfoText()
    {
        //return base.GetInfoText();
        //var managerData = StickerManager.Instance.GetStickerById(ManagerId);

        //return $"Upgrade {managerData.GetObjectNameBySeason(LevelManager.Instance.ActiveCityId).ToLower()} farmer lvl. {ManagerLevel}";
        return $"Upgrade farmer's lvl {ManagerLevelWanted.ToFloat()} times";
    }
}
