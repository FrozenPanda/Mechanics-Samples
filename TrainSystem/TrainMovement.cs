using System;
using System.Collections;
using System.Collections.Generic;
using lib.Managers.AnalyticsSystem;
using LionStudios.Suite.Analytics;
using UnityEngine;

public class TrainMovement : MonoBehaviour
{
    private Vector3 startPos;
    private Transform destinationPos;
    private Transform endPos;
    
    private Transform boxUnlockPos;

    [SerializeField]private Transform boxSpawnPos;

    public AnimationCurve StartToDestination;
    public AnimationCurve DestinationToEnd;
    [Range(0,3f)]
    public float speedMultiply;
    private float currentMoveTime = 0f;

    [SerializeField]private Animator TrainAnimator;

    public ParticleSystem smokeParticle;
    private ParticleSystem.EmissionModule smokeEmission;
    private bool isSmoking;
    private float smokeTime;

    private int TrainNumber;

    private void OnEnable()
    {
        TrainManager.Instance.onSendTrain.AddListener(SendTrain);
        TrainManager.Instance.onTrainSingleOrderComplete.AddListener(SingleOrderCompleted);

        LevelManager.Instance.RevisitEvent.AddListener(DestroySelf);
        LevelManager.Instance.BeforeCityChanged.AddListener(DestroySelf);

        smokeEmission = smokeParticle.emission;
        AudioService.Play(AudioType.TrainSound);
    }

    private void OnDisable()
    {
        TrainManager.Instance.onSendTrain.RemoveListener(SendTrain);
        TrainManager.Instance.onTrainSingleOrderComplete.RemoveListener(SingleOrderCompleted);

        if (LevelManager.IsAvailable())
        {
            LevelManager.Instance.RevisitEvent.RemoveListener(DestroySelf);
            LevelManager.Instance.BeforeCityChanged.RemoveListener(DestroySelf);
        }

    }

    private void Start()
    {
        TrainNumber = TrainManager.Instance.GetTrainNumber();
    }

    private enum trainMovement
    {
        Idle,
        ToDestination,
        AtDestination,
        Leaving,
    }

    private trainMovement _trainMovement;
    
    public void SetDestination(Transform destPos, Transform endPos , Transform boxUnlockPos)
    {
        startPos = transform.position;
        destinationPos = destPos;
        this.endPos = endPos;

        this.boxUnlockPos = boxUnlockPos;
        
        MoveTrain();
    }

    private void MoveTrain()
    {
        _trainMovement = trainMovement.ToDestination;
    }

    private void SendTrain()
    {
        GiveRewardedBox();
        AudioService.Play(AudioType.TrainSound);
        _trainMovement = trainMovement.Leaving;
        smokeTime = 100f;
    }

    [ContextMenu("SpawnTrainBox")]
    private void GiveRewardedBox()
    {
        if (TrainManager.Instance.GetTrainNumber() == 0)
        {
            ConversationManager.Instance.StartConversation(new List<ConversationInfo>()
            {
                new ConversationInfo(TalkingIntroducers.Father , "Congratulations honey, here's your reward for completing your train orders.")
            });
        }
        
        var boxUnlock = PoolingSystem.Instance.Create<GiftBoxUnlock>(PoolType.ExpendableGiftBox);
        var chests = ConfigurationService.Configurations.TrainDatas[TrainNumber].TrainCompleteRewards
            .GetChests(PackageMod.Mod1);
        var chestID = chests[0].ChestID;
        boxUnlock.InitializeBox(boxSpawnPos.position , () =>
        {
            //RewardGained();
            EarnAndOpenChest(chestID , boxSpawnPos);
            //ShopPackageManager.Instance.GivePackageContent(ConfigurationService.Configurations.TrainDatas[TrainNumber].TrainCompleteRewards , PackageMod.Mod1);
            boxUnlock.transform.parent.gameObject.SetActive(false);
        } , comingFrom: "G" + "Train");
        
        ChestManager.Instance.AddWaitingChest(chestID, 1);
        boxUnlock.ShowGiftBoxObject(GetChestType());
        var transformMover = PoolingSystem.Instance.Create<BoxMovement>(PoolType.TransformMover);
        transformMover.InitilizeMovement(boxUnlock.transform , boxUnlockPos.position , 1.5f);
        
        if(!TutorialManager.Instance.IsTutorialCompleted(TutorialType.TrainFirstChestOpenTutorial))
        {
            TutorialManager.Instance.CheckTutorial(TutorialType.TrainFirstChestOpenTutorial);
            PointToObjectArrow.PointToObject(boxUnlockPos.position, 4.5f);
        }

        Product spent = new Product();
        Product gain = new Product();
        gain.AddItem(ChestManager.Instance.GetChestTypeDataById(chestID).ChestType.ToString(), "Chest" , 1);
        AnalyticsManager.Instance.DiffEvent("Train Reward" , spent , gain);
    }

    private void RewardGained()
    {
        //ShopPackageManager.Instance.GivePackageContent(ConfigurationService.Configurations.TrainDatas[TrainManager.Instance.GetTrainNumber()].TrainCompleteRewards , PackageMod.Mod1);
    }
    
    private void EarnAndOpenChest(int chestId , Transform giftBoxSpawnPos)
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
    
    

    private void EndTrain()
    {
        //when train leave the city
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void SingleOrderCompleted()
    {
        //do animation
        if (!isSmoking)
        {
            isSmoking = true;
            smokeEmission.enabled = true;
        }
        
        smokeTime += 0.5f;
    }

    private void ControlSmoke()
    {
        if(!isSmoking)
            return;
        
            if (smokeTime > 0f)
                smokeTime -= Time.deltaTime;
            else
            {
                isSmoking = false;
                smokeEmission.enabled = false;
            }
    }

    private void DestroySelf()
    {
        Destroy(gameObject);
        //PoolingSystem.Instance.Destroy(PoolType.Train, this.gameObject);
    }

    private void Update()
    {
        ControlSmoke();
        
        switch (_trainMovement)
        {
            case trainMovement.Idle:
                break;
            case trainMovement.ToDestination:

                var moveSpeed = StartToDestination.Evaluate(currentMoveTime);

                TrainAnimator.SetFloat("MoveSpeed" , moveSpeed);
                
                currentMoveTime += Time.deltaTime * moveSpeed * speedMultiply;

                transform.position = Vector3.Lerp(startPos, destinationPos.position, currentMoveTime);

                if (currentMoveTime > 1f)
                {
                    _trainMovement = trainMovement.AtDestination;
                    currentMoveTime = 0f; 
                    TrainManager.Instance.StartCreateOrders();
                    smokeEmission.enabled = false;
                }
                
                break;
            case trainMovement.AtDestination:
                
                break;
            case trainMovement.Leaving:
                
                var moveSpeedLeaving = DestinationToEnd.Evaluate(currentMoveTime);

                TrainAnimator.SetFloat("MoveSpeed" , moveSpeedLeaving);
                
                currentMoveTime += Time.deltaTime * DestinationToEnd.Evaluate(currentMoveTime) * speedMultiply;

                transform.position = Vector3.Lerp(destinationPos.position, endPos.position, currentMoveTime);

                if (currentMoveTime > 1f)
                {
                    _trainMovement = trainMovement.Idle;
                    EndTrain();
                    currentMoveTime = 0f; 
                    //end
                }
                
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private ChestType GetChestType()
    {
        var currentTrainProgressInfo = TrainManager.Instance.GetTrainInfo();
        var chestData = ChestManager.Instance.GetChestDataById(currentTrainProgressInfo.chestID);
        return chestData.ChestType;
    }
}
