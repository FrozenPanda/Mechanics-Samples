using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainTimer : OrderCar3dTimer
{
    protected override void OnEnable()
    {
        TrainManager.Instance.onSendTrain.AddListener(CheckTruckTimer);
    }

    protected override void OnDisable()
    {
        TrainManager.Instance.onSendTrain.RemoveListener(CheckTruckTimer);
    }

    protected override void Start()
    {
        CheckTruckTimer();
    }

    protected override void CheckTruckTimer()
    {
        totalTime = TrainManager.Instance.GetCurrentCountDown();
        
        if (totalTime > 0f)
        {
            EnableTimer();
        }
        else
        {
            holder.SetActive(false);
        }
    }
}
