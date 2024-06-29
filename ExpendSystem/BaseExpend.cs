using System;
using System.Collections;
using System.Collections.Generic;

using DG.Tweening;

using NaughtyAttributes;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

public abstract class BaseExpend : Interactable, ISaveableInteractable
{
    public override bool ShowFillBar => false;
    public int cost;
    [SerializeField] protected TextMeshProUGUI costText;


    [SerializeField] protected Image expendImage;
    public GameObject CostUI;
    [SerializeField] private GameObject rewardedAdUI;
    [SerializeField] protected Image payFillBar;

    [SerializeField] protected bool unlocksWithAd;

    [Header("Other")]
    [SerializeField] protected bool isShowFreeText = false;
    [ShowIf("isShowFreeText")][SerializeField] protected GameObject freeTextPanel;

    protected bool isBroken;
    private Tween fillbarTween;

    #region Save & Load
    public virtual string ID => InteractionId;

    public abstract string GetSerializedData();

    #endregion
    protected int baseCost;
    protected override void OnEnable()
    {
        base.OnEnable();
        InteractableSaveService.Instance.AddSaveable(this);
        EnableCorrectUI();
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (InteractableSaveService.IsAvailable())
            InteractableSaveService.Instance.RemoveSaveable(this);
    }

    protected IEnumerator UpdateText()
    {
        yield return new WaitForEndOfFrame();
        UpdateCostText();
    }

    private void EnableCorrectUI()
    {
        if (rewardedAdUI == null || CostUI == null) return;
        if (unlocksWithAd)
        {
            rewardedAdUI.SetActive(true);
            CostUI.SetActive(false);
        }
        else
        {
            rewardedAdUI.SetActive(false);
            CostUI.SetActive(true);
        }
    }

    public virtual void UpdateCostText()
    {
        if (costText != null && !unlocksWithAd)
        {
            CostUI.SetActive(true);

            if (isShowFreeText)
            {
                var isCostZero = (cost == 0);
                CostUI.SetActive(!isCostZero);
                freeTextPanel.SetActive(isCostZero);
            }

            costText.text = $"{cost}";

            // costText.text = $"{cost} $";
            if (expendImage != null)
                expendImage.gameObject.SetActive(true);
        }
    }

    protected virtual IEnumerator WaitForEndInteraction(IInteractor interactor, Action callback)
    {
        yield return new WaitForSeconds(ConfigurationService.Configurations.WaitBeforePayDelay);

        if (InteractionManager.Instance.CheckInRange(interactor.CharacterController.MyPosition, this) && !interactor.CharacterController.IsCharacterMoving())
            StartCoroutine(PayCost(interactor, callback, CheckExtraConditions));

        else base.StartInteraction(interactor, callback);
    }

    protected virtual IEnumerator PayCost(IInteractor interactor, Action callback, Func<IInteractor, bool> checkExtraConditions)
    {
        yield break;
        //var decreaseAmount = ConfigurationService.Configurations.MoneyPerOneBale;
        //var carryController = interactor.CarryController;
        //var animMoveDelayRatio = interactor.CharacterController.GetCharacterValue(CharacterDataType.TransportTime);
        //var collectableData = CollectableObjectService.GetCollectableObjectData(1);
        //var moveDelay = animMoveDelayRatio / (cost > 1000f ? 2f : 1f);
        //var moveDuration = animMoveDelayRatio;
        //var soundPerMoney = ConfigurationService.Configurations.SoundPerMoney * (cost > 1000f ? 2f : 1f);

        //PanelManager.Instance.GamePlayPanel.ShowEnlargeEffectByType(CurrencyService.ActiveCurrencyType);
        //CheckExtraConditions(interactor);
        //bool isPaying = false;
        //int tmpCost = cost;
        //float pitchCount = 0;
        //while (!isBroken && cost > 0 && tmpCost > 0 && ExchangeService.GetValue(collectableData.CurrencyType) > 0 && (checkExtraConditions == null || checkExtraConditions(interactor)))
        //{
        //    if (interactor.CharacterType == CharacterType.MainCharacter)
        //    {
        //        if (pitchCount % soundPerMoney == 0)
        //            AudioService.Play(AudioType.MoneyDropSound, (pitchCount / soundPerMoney) * ConfigurationService.Configurations.PitchAccelaration);
        //        pitchCount++;
        //    }
        //    PlayHaptic(interactor);
        //    tmpCost -= decreaseAmount;
        //    isPaying = true;


        //    GameObject activeGameObject;
        //    CarryPoint cp = carryController.GetCarryPoint(CarryPointType.Back);
        //    Vector3 startPos = cp.GetNextSlotPosition(true, 0);

        //    //if (!collectableData.IsCollectAndDestroy)
        //    //{
        //    var droppedMoneys = carryController.DropObject(collectableData.CollectableObjectType, collectableData.Id, 1, true, true);
        //    activeGameObject = droppedMoneys[0].ActiveGameObject;
        //    if (activeGameObject == null)
        //    {
        //        var sourceObject = PoolingSystem.Instance.Create<SourceObject>(collectableData.CarryType);
        //        sourceObject.SetCollectedState(true);
        //        activeGameObject = sourceObject.gameObject;
        //    }
        //    else
        //    {
        //        startPos = activeGameObject.transform.position;
        //    }
        //    //}
        //    //else
        //    //{
        //    //    var sourceObject = PoolingSystem.Instance.Create<SourceObject>(collectableData.CarryType);
        //    //    sourceObject.SetCollectedState(true);
        //    //    activeGameObject = sourceObject.gameObject;
        //    //    ExchangeService.DoExchange(collectableData.CurrencyType, -collectableData.Cost, ExchangeMethod.UnChecked, out _);
        //    //}

        //    activeGameObject.transform.parent = null;
        //    activeGameObject.transform.position = startPos;

        //    //Vector3 slotPosition = (carryController.GetCarryPoint(collectableData.CarryPointsType) as CPI_BackCarryPoint).GetNextSlotPosition();
        //    //activeGameObject.transform.position = slotPosition;

        //    //activeGameObject.transform.rotation = Quaternion.Euler(carryController.GetCarryPoint(collectableData.CarryPointsType).GetTargetRotation());
        //    //Debug.Log("droppedMoneys->" + droppedMoneys.Count);


        //    yield return new WaitForSeconds(moveDelay);

        //    moveDelay *= 0.95f;
        //    var jp = Mathf.Max(GameUtility.GetJumpPower(activeGameObject.transform, CostUI.transform.position), 2f);
        //    activeGameObject.transform.DOJump(CostUI.transform.position, jp, 1, moveDuration).SetEase(ConfigurationService.Configurations.TileUnlockAnimEase).OnComplete(
        //        () =>
        //        {
        //            cost -= decreaseAmount;
        //            FillBarAnimation(moveDuration);
        //            if (cost < 0) cost = 0;
        //            UpdateCostText();
        //            PoolingSystem.Instance.Destroy(collectableData.CarryType, activeGameObject);
        //            isPaying = false;
        //        });
        //    //  yield return new WaitForSeconds(moveDuration);
        //}
        //yield return new WaitWhile(() => isPaying);

        ////if (collectableData.CurrencyType != CurrencyType.None) PanelManager.Instance.GamePlayPanel.HideEnlargeEffectByType(collectableData.CurrencyType);

        //if (tmpCost == 0) cost = 0;

        //if (!checkExtraConditions(interactor) || cost <= 0 || tmpCost <= 0 || ExchangeService.GetValue(collectableData.CurrencyType) <= 0)
        //{
        //    base.StartInteraction(interactor, callback);
        //}
    }

    protected void FillBarAnimation(float duration)
    {
        if (payFillBar == null) return;
        float decreaseFill = (baseCost - cost) / (float)baseCost;
        //Debug.Log("duration-> " +duration.ToString());
        //Debug.Log("fill " +decreaseFill.ToString());
        payFillBar.gameObject.SetActive(true);
        fillbarTween = payFillBar.DOFillAmount(decreaseFill, duration);
    }
    protected void ResetFillBarAnimation()
    {
        //Debug.Log("reset fil bar");
        if (payFillBar == null) return;
        payFillBar.gameObject.SetActive(false);
        fillbarTween.Kill();
        payFillBar.fillAmount = 0f;
    }

    protected virtual bool CheckExtraConditions(IInteractor interactor = null)
    {
        return true;
    }
}
