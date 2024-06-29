using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarrierOpen : MonoBehaviour
{
    public Transform EndPos;
    private void OnEnable()
    {
        TrainManager.Instance.onSendTrain.AddListener(StartAnim);
    }

    private void OnDisable()
    {
        TrainManager.Instance.onSendTrain.RemoveListener(StartAnim);
    }

    private void StartAnim()
    {
        PandaLibraryClasses.PandaLibrary.PandaRotate(transform , EndPos , 1f);
    }
}
