using System;
using System.Collections;
using System.Collections.Generic;
using MonsterLove.StateMachine;
using UnityEngine;
using Random = UnityEngine.Random;

public class NFAnimalAI : BaseAIController<NFVillageAnimalBrainState , NFVillageAnimalDriver>
{
    
    private AnimationType currentAnimType;
    public int SkinID = 201;
    
    public override CharacterType CharacterType => CharacterType.NFVillageAnimal;
    private const string workEndKey = "WorkEnd";
    private int idleCount = 0;
    
    private CheckClickedToObject CheckClickedToObject => checkClickedToObject ??= GetComponent<CheckClickedToObject>();
    private CheckClickedToObject checkClickedToObject;

    [SerializeField] private LayerMask movePosLayer;

    private float defaultWalkSpeed;
    private int clickCountForFasterMove = 0;
    private float clickTimerForFasterMove = 3f;
    
    private void Start()
    {
        GetComponent<NFSpecificSkinController>().SetSpecificSkin(SkinID);
        ChangeState(NFVillageAnimalBrainState.Idle);
        //defaultWalkSpeed = GetComponent<BasicCharacterController>().GetSpeed();
        CheckClickedToObject.ClickedAction = ClickAction;
    }

    private void FixedUpdate()
    {
        fsm?.Driver.FixedUpdate?.Invoke();
    }

    private void Idle_Enter()
    {
        if (defaultWalkSpeed > 1)
        {
            MovementController.SetSpeed(defaultWalkSpeed * (clickCountForFasterMove+1));
        }
        timer = Random.Range(3, 10f);
        currentAnimType = AnimationType.NFVillageAnimalIdle;
        AnimationController.PlayAnimation(AnimationType.NFVillageAnimalIdle);
        //AnimationController.SubscribeCustomEvent(OnSubscribeCustomEvent);
    }

    private float timer = 0f;
    private void Idle_FixedUpdate()
    {
        if (timer > 0f)
            timer -= Time.deltaTime;
        else
        {
            ChangeState(NFVillageAnimalBrainState.Walk);
        }
    }

    private void Walk_FixedUpdate()
    {
        if (!MovementController.IsMoving())
        {
            if (defaultWalkSpeed <= 0)
            {
                defaultWalkSpeed = MovementController.GetSpeed();
            }
            var targetPosses = Physics.OverlapSphere(transform.position, 30f, movePosLayer);
            if(targetPosses.Length < 1)
                return;
            var targetPos = targetPosses[Random.Range(0, targetPosses.Length)];
            currentAnimType = AnimationType.NFVillageAnimalWalk;
            AnimationController.PlayAnimation(AnimationType.NFVillageAnimalWalk);
            MovementController.Move(targetPos.transform.position , CharacterController , () =>
            {
                idleCount = Random.Range(1, 3);
                ChangeState(NFVillageAnimalBrainState.Idle);
                //MovementController.Stop();
            });
        }
        
        CheckDestinationReached();
    }
    
    private void StopAnimation()
    {
        //AnimationController.UnSubscribeCustomEvent(OnSubscribeCustomEvent);
        ChangeState(NFVillageAnimalBrainState.Walk);
    }

    private void ClickAction()
    {
        if (defaultWalkSpeed > 0)
        {
            MovementController.SetSpeed(MovementController.GetSpeed() * (clickCountForFasterMove+1));
        }
        clickCountForFasterMove++;
        MovementController.Stop();
        ChangeState(NFVillageAnimalBrainState.Walk);
    }

    private void Update()
    {
        if (clickCountForFasterMove > 0)
        {
            if (clickTimerForFasterMove > 0)
            {
                clickTimerForFasterMove -= Time.deltaTime;
            }
            else
            {
                clickCountForFasterMove--;
                clickTimerForFasterMove = 3f;
            }
        }
    }
}

public enum NFVillageAnimalBrainState
{
    Idle,
    Walk
}

public class NFVillageAnimalDriver
{
    public StateEvent FixedUpdate;
}
