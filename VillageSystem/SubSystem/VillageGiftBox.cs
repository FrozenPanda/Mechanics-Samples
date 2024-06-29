using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VillageGiftBox : MonoBehaviour
{
    [SerializeField] private int chestId;
    //[SerializeField] private int homeID;
    [SerializeField]private Transform giftBoxSpawnPos;
    public int ChestId => chestId;
    private GiftBoxUnlock giftBox;

    public void OnHomeUnlocked()
    {
        ChestManager.Instance.AddWaitingChest(chestId, 1);
        giftBox = PoolingSystem.Instance.Create<GiftBoxUnlock>(PoolType.ExpendableGiftBox);
        giftBox.InitializeBox(giftBoxSpawnPos.position, EarnAndOpenChest , comingFrom: chestId.ToString());
        giftBox.ShowGiftBoxObject(GetChestType());
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
    }
    
    private ChestType GetChestType()
    {
        var chestData = ChestManager.Instance.GetChestDataById(chestId);
        return chestData.ChestType;
    }
}
