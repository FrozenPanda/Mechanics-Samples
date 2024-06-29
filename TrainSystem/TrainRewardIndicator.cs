using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TrainRewardIndicator : MonoBehaviour
{
    [SerializeField] private Image ChestImage;
    [SerializeField] private Image FillBar;
    [SerializeField] private GameObject TickImage;

    private int totalOrderWanted;
    private int totalOrderCompleted;

    private void OnEnable()
    {
        TrainManager.Instance.onTrainSingleOrderComplete.AddListener(UpdateProgress);
    }

    private void OnDisable()
    {
        if(TrainManager.IsAvailable())
            TrainManager.Instance.onTrainSingleOrderComplete.RemoveListener(UpdateProgress);
    }

    private void Start()
    {
        UpdateProgress();

        if (!TutorialManager.Instance.IsTutorialCompleted(TutorialType.SecondOrderTutorial))
            TrainTutorialSecond.TrainCanvas = ChestImage.transform;
    }

    private void UpdateProgress()
    {
        var currentTrainProgressInfo = TrainManager.Instance.GetTrainInfo();
        totalOrderWanted = currentTrainProgressInfo.totalOrder;
        totalOrderCompleted = currentTrainProgressInfo.totalOrderCompleted;
        ChestImage.sprite = ChestManager.Instance.GetChestTypeDataById(currentTrainProgressInfo.chestID).ChestPanelImage;
        
        UpdateFillBar();
    }

    private void UpdateFillBar()
    {
        FillBar.fillAmount = (float)totalOrderCompleted / totalOrderWanted;
        
        if(totalOrderCompleted >= totalOrderWanted)
            TickImage.SetActive(true);
        else
            TickImage.SetActive(false);
    }
}
