using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using System;

public class ExpendableGiftBox : MonoBehaviour
{
    [SerializeField] private int chestId;

    [SerializeField] private bool isShowBox;
    [SerializeField] [ShowIf("isShowBox")] private Transform giftBoxSpawnPos;

    public int ChestId => chestId;

    private Expendable Expendable => expendable ??= GetComponent<Expendable>();
    private Expendable expendable;


    private NiceCityProductContainer NiceCityProductContainer => niceCityProductContainer ??= GetComponentInChildren<NiceCityProductContainer>(true);
    private NiceCityProductContainer niceCityProductContainer;

    private GiftBoxUnlock giftBox;

    private void OnEnable()
    {
        ExpendManager.Instance.OnNewTileExpend.AddListener(OnExpendListener);
    }

    private void OnDisable()
    {
        if (ExpendManager.IsAvailable())
        {
            ExpendManager.Instance.OnNewTileExpend.RemoveListener(OnExpendListener);
        }
    }

    private void OnExpendListener(int expendId)
    {
        if(expendId == Expendable.ExpandableId)
        {
            ChestManager.Instance.AddWaitingChest(chestId, 1);

            if (isShowBox)
            {
                giftBox = PoolingSystem.Instance.Create<GiftBoxUnlock>(PoolType.ExpendableGiftBox);
                giftBox.InitializeBox(giftBoxSpawnPos.position, EarnAndOpenChest , comingFrom: "G" + chestId.ToString());
                giftBox.ShowGiftBoxObject(GetChestType());
                if(RemoteConfigManager.Instance.GetBoolConfig("IsShopAutoCollectEnabled",false) && NiceCityProductContainer != null)
                    StartCoroutine(WaitAndOpen());

                CheckOpenChestTutorial(expendId);
            }
            else
            {
                EarnAndOpenChest();
            }
        }
    }

    private IEnumerator WaitAndOpen()
    {
        yield return new WaitForSeconds(0.5f);
        giftBox.UnlockBox();
    }

    private void EarnAndOpenChest()
    {
        var chest = ChestManager.Instance.GetChestDataById(chestId);
        if (!chest.IsRewardedChest)
        {
            ChestManager.Instance.OpenChest(chestId, PopupType.Undefined, false, true/*, NiceCityProductContainer*/);
        }
        else
        {
            PanelManager.Instance.Show(PopupType.RewardedChestPanel, new RewardedChestPanelData(chestId, giftBoxSpawnPos, null));
        }

        CheckOpenChestTutorialComplete(Expendable.ExpandableId);
    }

    private ChestType GetChestType()
    {
        var chestData = ChestManager.Instance.GetChestDataById(chestId);
        return chestData.ChestType;
    }

    private void CheckOpenChestTutorial(int expendableId)
    {
        if(!TutorialManager.Instance.IsTutorialCompleted(TutorialType.OpenChestTutorial) && expendableId == OpenChestTutorial.EXPENDABLE_ID)
        {
            //TutorialManager.Instance.CheckTutorial(TutorialType.OpenChestTutorial);
            MissionManager.Instance.OnMissionStartedUniqueID.AddListener(WaitForPointToObject);
        }
    }

    private void CheckOpenChestTutorialComplete(int expendableId)
    {
        if (!TutorialManager.Instance.IsTutorialCompleted(TutorialType.OpenChestTutorial) && expendableId == OpenChestTutorial.EXPENDABLE_ID)
        {
            OpenChestTutorial.CheckTutorialCompleteState();
            //TutorialManager.Instance.TutorialComplete(TutorialType.OpenChestTutorial);
        }
    }

    private void WaitForPointToObject(int uniqueID)
    {
        if (uniqueID == 101)
        {
            PointToObjectArrow.PointToObject(giftBox.transform.position, 3f);
            MissionManager.Instance.OnMissionStartedUniqueID.RemoveListener(WaitForPointToObject);
        }
    }
}
