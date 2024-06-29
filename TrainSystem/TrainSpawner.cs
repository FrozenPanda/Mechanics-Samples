using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class TrainSpawner : MonoBehaviour
{
    public Transform trainDestinationPos;
    public Transform trainEndPos;
    public Transform boxUnlockPos;

    private Coroutine currentCoroutine;
    private float Timer = 5f;
    
    private void OnEnable()
    {
        TrainManager.Instance.onCallTrain.AddListener(SpawnTrain);
    }

    private void OnDisable()
    {
        TrainManager.Instance.onCallTrain.RemoveListener(SpawnTrain);
    }

    private void OnDestroy()
    {
        TrainManager.Instance.trainActive = false;
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(1f);
        StartCallTrain();
    }

    [ContextMenu("Test")]
    private void StartCallTrain()
    {
        TrainManager.Instance.TryCallTrain();
    }

    private void Awake()
    {
        /*if (TrainManager.Instance.CanSpawn())
        {
            CoroutineDispatcher.StartCoroutine(SpawnTrain, 7f);
        }*/
    }

    [ContextMenu("Spawn Train")]
    private void SpawnTrain()
    {
        /*if(TutorialManager.Instance.IsTutorialCompleted(TutorialType.TrainCompleted) && !TutorialManager.Instance.IsTutorialCompleted(TutorialType.VillageEnterTutorial))
            return;*/
        
        if (TrainManager.Instance.trainActive == false)
        {
            TrainManager.Instance.trainActive = true;
            currentCoroutine = StartCoroutine(SpawnTrainWithDelay(2f));
            //CoroutineDispatcher.StartCoroutine(SpawnTrainWithDelay, 5f);
        }
        else
        {
            return;
        }
    }

    IEnumerator SpawnTrainWithDelay(float waitSecond)
    {
        yield return new WaitForSeconds(waitSecond);

        if (PanelManager.Instance.IsAnyBigPanelShowing())
            StartCoroutine(SpawnTrainWithDelay(0.5f));
        else
            SpawnTrainNow();
    }
    
    private void SpawnTrainNow()
    {
        TrainMovement trainMovement = PoolingSystem.Instance.Create<TrainMovement>(PoolType.Train);
        trainMovement.transform.position = transform.position;
        trainMovement.transform.rotation = transform.rotation;
        trainMovement.SetDestination(trainDestinationPos , trainEndPos , boxUnlockPos);
    }

    [ContextMenu("SpawnTest")]
    public void TestSpawn()
    {
        GameObject spawnable = CollectableObjectService.GetObjectMachineFbx(PoolType.WheatAccumulate);
        Instantiate(spawnable, Vector3.zero, quaternion.identity);
    }
}
