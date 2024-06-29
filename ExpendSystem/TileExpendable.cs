using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using NaughtyAttributes;
using Pathfinding;

public class TileExpendable : Expendable
{
    public GameObject SeeAreaObject => SeeArea;

    [Header("Tile")]
    [SerializeField] private GameObject SeeArea;
    [SerializeField] protected GameObject Ground;

    [Header("Scale Up Anim")]
    [SerializeField] private GameObject scaleUpAnimObject;

    [Header("Expend Anim")]
    [SerializeField] private bool doScaleAnimToUnlockPart = true;
    [SerializeField] private bool doScaleNonInteractableObjects = false;
    [ShowIf("doScaleNonInteractableObjects")][SerializeField] private List<Transform> nonInteractableObjects;

    //private NavmeshCut navmeshCut;
    private float tileUIEnlargeRatio;
    private float tileUIEnlargeTime;
    private bool isShowingIndicator;

    private bool canInteractorSee = false;

    protected override void Initialize()
    {
        //navmeshCut = GetComponent<NavmeshCut>();

        tileUIEnlargeRatio = ConfigurationService.Configurations.TileUIEnlargeRatio;
        tileUIEnlargeTime = ConfigurationService.Configurations.TileUIEnlargeTime;

        base.Initialize();
    }

    public override bool LoadInteraction(IInteractor _interactor)
    {
        if (!isCharacterWaiting)
        {
            isCharacterWaiting = true;
            characterWaitStartTime = Time.time;
        }

        bool hasCoins = cost == 0 || ExchangeService.GetValue(CurrencyType.Coin) > 0;
        bool isCharacterSaler = _interactor.CharacterType == CharacterType.MainCharacter;
        bool requirementsMet = (RequiredObjects == null || RequiredObjects.Count <= 0 || ExpendManager.Instance.IsAllExpended(RequiredObjects));

        if (!isExpended && (hasCoins || unlocksWithAd) && CheckExtraConditions(_interactor) && isCharacterSaler && requirementsMet)
        {
            isBroken = false;
            return base.LoadInteraction(_interactor);
        }
        else
        {
            return false;
        }
    }

    protected override IEnumerator WaitForEndInteraction(IInteractor interactor, Action callback)
    {
        yield return new WaitForSeconds(ConfigurationService.Configurations.WaitBeforePayDelay);

        if (InteractionManager.Instance.CheckInRange(interactor.CharacterController.MyPosition, this) && CheckExtraConditions(interactor))
            StartCoroutine(PayCost(interactor, callback, CheckExtraConditions));

        else base.StartInteraction(interactor, callback);
    }

    public override void SetState(bool _isExpended, bool isAnimated = false)
    {
        base.SetState(_isExpended, isAnimated);

        /*if (!isAnimated)
        {
            if(navmeshCut != null)
                navmeshCut.enabled = !_isExpended;
        }*/
        if (isAnimated)
        {
            var disabledCollider = GameUtility.DisableChildrenColliders(unlockedPart);

            unlockedPart.SetActive(showAlwaysLockedPart || _isExpended);
            SetChildrenExpendState(false);

            Action expendedAction = () =>
            {
                GameUtility.EnableColliders(disabledCollider);
                Expend();
            };
            
            if (doScaleAnimToUnlockPart)
            {
                OnExpendScaleAnim(unlockedPart.transform, endAction: () =>
                {
                   expendedAction.Invoke();
                });
            }
            else
            {
                if (doScaleNonInteractableObjects && nonInteractableObjects.Count > 0)
                {
                    float onExpendAnimDelay = 0f;
                    for (int i = 0; i < nonInteractableObjects.Count; i++)
                    {
                        var nonInteractableObject = nonInteractableObjects[i];
                        OnExpendScaleAnim(nonInteractableObject, delay:onExpendAnimDelay, endAction: () =>
                        {
                            var colliders = nonInteractableObject.GetComponentsInChildren<Collider>(true);
                            UpdateAStarPath(colliders);
                        });
                        
                        onExpendAnimDelay += .2f;
                    }
                    
                    expendedAction.Invoke();
                }
                else
                {
                    expendedAction.Invoke();
                }
            }
            // GameUtility.EnableColliders(disabledCollider);
            // Expend();
            // unlockedPart.transform.DOScale(Vector3.one, 0f).SetEase(Ease.InOutElastic).OnComplete(() =>
            // {
            //     GameUtility.EnableColliders(disabledCollider);
            //     Expend();
            // });
        }
    }
    

    private Vector3? defaultScale;

    public override void ShowCapacity()
    {
        if (!isShowingIndicator)
        {
            isShowingIndicator = true;
            var targetObject = scaleUpAnimObject ? scaleUpAnimObject : lockedPart;
            defaultScale ??= targetObject.transform.localScale;

            var disabledCollider = GameUtility.DisableChildrenColliders(targetObject);
            targetObject.transform.DOScale(defaultScale.Value + (Vector3.one * tileUIEnlargeRatio), tileUIEnlargeTime).OnComplete(() =>
            {
                GameUtility.EnableColliders(disabledCollider);
            });
        }
    }

    public override void HideCapacity()
    {
        if (isShowingIndicator)
        {
            isShowingIndicator = false;
            var targetObject = scaleUpAnimObject ? scaleUpAnimObject : lockedPart;
            defaultScale ??= targetObject.transform.localScale;

            var disabledCollider = GameUtility.DisableChildrenColliders(targetObject);
            targetObject.transform.DOScale(defaultScale.Value, tileUIEnlargeTime).OnComplete(() =>
            {
                GameUtility.EnableColliders(disabledCollider);
            });
        }
    }


    /*protected override void CheckWarningMessageState()
    {
        if (canInteractorSee)
        {
            if ((Time.time - characterWaitStartTime) > requiredCharacterWaitTime)
            {
                if (warningText == null && Time.time > (hideWarningTime + hideWarningTimeDelay))
                    ShowWarningText();
                else if (warningText != null && (Time.time - showingWarningStartTime) > hideWarningTimeDelay)
                    HideWarningText(false);
            }
        }
        else
            HideWarningText(true);
    }*/

    void RayBetweenInteractor(IInteractor interactor)
    {
        RaycastHit hit;

        if (Physics.Raycast(interactor.CharacterController.MyPosition, interactor.CarryController.transform.TransformDirection(Vector3.forward), out hit, Mathf.Infinity))
        {
            Debug.DrawRay(interactor.CharacterController.MyPosition, interactor.CarryController.transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
            if (hit.transform == SeeArea.transform)
            {
                canInteractorSee = true;
            }
            else
            {
                canInteractorSee = false;
            }
        }
        else
        {
            canInteractorSee = false;
        }
    }

    protected override bool CheckExtraConditions(IInteractor interactor)
    {
        if (interactionData.isUseBeacon) return true;
        RayBetweenInteractor(interactor);
        return canInteractorSee;
    }
}
