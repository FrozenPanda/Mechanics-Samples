using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Pathfinding;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class Expendable : BaseExpend
{
    public virtual int ExpandableId => id;
    public List<Expendable> RequiredObjects => requiredObjects;

    [Header("Expend")]
    [FormerlySerializedAs("Id")]
    [SerializeField] private int id;
    [FormerlySerializedAs("RequiredObjects")][SerializeField] private List<Expendable> requiredObjects;

    [Header("Locked and unlocked parts")]
    [SerializeField] protected bool hideLockedPartWhenExpend = true;
    [SerializeField] protected bool showAlwaysLockedPart = false;
    [SerializeField] protected GameObject lockedPart;
    [SerializeField] protected GameObject unlockedPart;

    [Header("UI")]
    [SerializeField] private bool AlwaysShow;


    [Header("Animation")]
    //[SerializeField] private Transform tileIconSpawnPos;
    [SerializeField] protected float coinDecreaseTime = 0.5f;

    public override InteractableType InteractableType => InteractableType.Expendable;
    public override bool ShowFillBar => false;

    protected bool isExpended;

    private NavmeshCut[] navMeshCutters;

    #region Warning Text

    protected bool isCharacterWaiting;
    protected float characterWaitStartTime;
    protected float requiredCharacterWaitTime = 1;
    protected float showingWarningStartTime;
    protected float hideWarningTimeDelay = 5;
    private float requiredTimeToWaitBeforeShowingAgain = 5;
    protected float hideWarningTime;
    protected GameObject warningText;
    private Tween tween;
    private Vector3? defaultScale;

    #endregion

    protected override void OnEnable()
    {
        base.OnEnable();
        //InteractableSaveService.Instance.AddSaveable(this);
        ExpendManager.Instance.OnNewTileExpend.AddListener(SetExpendState);
        navMeshCutters = GetComponentsInChildren<NavmeshCut>(true);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (ExpendManager.IsAvailable())
            ExpendManager.Instance.OnNewTileExpend.RemoveListener(SetExpendState);

    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (ExpendManager.IsAvailable())
        {
            ExpendManager.Instance.RemoveFromAllExpensionLists(this);
        }

    }

    protected override void Initialize()
    {
        base.Initialize();
        isExpended = ExpendManager.Instance.IsExpended(this);
        if (isExpended) ExpendManager.Instance.AddToExpensions(this);
        else ExpendManager.Instance.AddCloseExpension(this);

        SetState(isExpended);
        UpdateCostText();
        StartCoroutine(UpdateText());
    }

    #region Save & Load
    public override string ID => InteractionId;
    protected override void LoadData()
    {
        string serializedData = InteractableSaveService.Instance.GetSerializedData(this);
        if (serializedData == null)
        {
            baseCost = cost;
            return;
        }
        var saveCost = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpendableSaveData>(serializedData);
        cost = saveCost.Cost;
        baseCost = saveCost.BaseCost;
        if (payFillBar != null) payFillBar.fillAmount = (baseCost - cost) / (float)baseCost;
        UpdateCostText();
    }
    public override string GetSerializedData()
    {
        ExpendableSaveData saveData = new ExpendableSaveData();
        saveData.Cost = cost;
        saveData.BaseCost = baseCost;
        return Newtonsoft.Json.JsonConvert.SerializeObject(saveData);
    }
    #endregion

    public virtual void SetExpendState(int expendedId = -1)
    {
        if (isExpended) return;
        if (requiredObjects != null && requiredObjects.Count > 0 && !ExpendManager.Instance.IsAllExpended(requiredObjects))
        {
            if (hideLockedPartWhenExpend) lockedPart.SetActive(false);
            unlockedPart.SetActive(showAlwaysLockedPart);
            return;
        }


        // if (!TutorialManager.Instance.IsTutorialCompleted(TutorialType.DropSourceTutorial))
        // {
        //     var isTutorialExpendable = TileUnlockTutorial.EXPENDABLE_ID.Equals(id);
        //     lockedPart.SetActive(isTutorialExpendable);
        //     if (!isTutorialExpendable)
        //     {
        //         if(tutorialAction != null) TutorialManager.Instance.OnTutorialComplete.RemoveListener(tutorialAction);
        //         tutorialAction ??= ((tutorialType) =>
        //         {
        //             if (tutorialType == TutorialType.DropSourceTutorial)
        //                 lockedPart.SetActive(true);
        //         });
        //         TutorialManager.Instance.OnTutorialComplete.AddListener(tutorialAction);
        //     }
        // }
        // else
        {
            // Debug.Log("SetExpendState4 : " + requiredObjects.Count + ", " + gameObject.name, gameObject);
            lockedPart.SetActive(true);
        }
    }

    public virtual void SetState(bool _isExpended, bool isAnimated = false)
    {
        if (lockedPart != null)
        {
            if (hideLockedPartWhenExpend && isExpended && isAnimated)
            {
                var disabledCollider = GameUtility.DisableChildrenColliders(lockedPart);
                lockedPart.gameObject.transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.OutQuad).SetDelay(0.2f).OnComplete(() =>
                {
                    lockedPart.SetActive(!_isExpended);
                    GameUtility.EnableColliders(disabledCollider);
                });
            }
            else
            {
                if (hideLockedPartWhenExpend) lockedPart.SetActive(!_isExpended);
            }
        }

        if (!isAnimated)
        {
            unlockedPart.SetActive(showAlwaysLockedPart || _isExpended);
        }
        SetExpendState();
    }

    protected void UpdateNavmeshCutters()
    {
        foreach (var cutter in navMeshCutters)
        {
            cutter.enabled = false;
            cutter.enabled = true;
        }
    }

    protected IEnumerator DestroyParticle(GameObject particle)
    {
        yield return new WaitForSeconds(1.25f);
        if (!particle) ParticleManager.Instance.StopParticle(PoolType.NewTileParticle, particle);
    }

    /*protected void SpawnTileIcon()
    {
        GameObject tileIcon = PoolingSystem.Instance.Create(PoolType.TileIcon);

        tileIcon.transform.position = tileIconSpawnPos.position;

        Vector2 randomMoveDirection = UnityEngine.Random.insideUnitCircle * 1.1f;
        Vector3 p1 = tileIcon.transform.localPosition + new Vector3(randomMoveDirection.x, 0.1f, randomMoveDirection.y);

        tileIcon.transform.DOMove(p1, 0.5f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            GameObject tileIconUI = PoolingSystem.Instance.Create(PoolType.TileIconUI, PanelManager.Instance.PanelCanvas);

            tileIconUI.transform.position = Camera.main.WorldToScreenPoint(tileIcon.transform.position);

            PoolingSystem.Instance.Destroy(PoolType.TileIcon, tileIcon);
            tileIconUI.transform.DOMove(FindObjectOfType<TileBlock>().IncomeImageTr.position, 0.5f).SetEase(Ease.Linear).OnComplete(() =>
            {
                PoolingSystem.Instance.Destroy(PoolType.TileIconUI, tileIconUI);
                FindObjectOfType<TileBlock>().UpdateTileText();
            });
        });
    }*/


    protected virtual void Expend()
    {
        SetChildrenExpendState(true);
        // CheckNewItemUnlocked(allUnlockedInteractables);
    }

    protected void SetChildrenExpendState(bool expendState)
    {
        SetChildrenExpendState<Interactable>(expendState);
    }

    protected void SetChildrenExpendState<T>(bool expendState) where T : Interactable
    {
        var unlockedInteractables = unlockedPart.GetComponentsInChildren<T>(true);
        List<T> allUnlockedInteractables = null;
        if (expendState) allUnlockedInteractables = new List<T>();

        if (unlockedInteractables.Length > 0)
        {
            foreach (var interactable in unlockedInteractables)
            {
                if (interactable != null)
                {
                    if (expendState)
                    {
                        if (!allUnlockedInteractables.Contains(interactable))
                        {
                            interactable.OnExpend();
                            allUnlockedInteractables.Add(interactable);
                        }
                    }
                    else
                    {
                        interactable.SetExpendState(false);
                    }
                }
            }
        }
    }

    public override bool IsAvailable()
    {
        return !isExpended && base.IsAvailable();
    }

    public override void StartInteraction(IInteractor interactor, Action callback)
    {
        /*if (unlocksWithAd)
		{
			MediationManager.Instance.ShowRewarded("TileAdUnlock", () =>
			{
                TileUnlocked();
			});
			return;
		}*/
        /*else */
        if (cost <= 0)
        {
            base.StartInteraction(interactor, callback);
            return;
        }
        StartCoroutine(WaitForEndInteraction(interactor, callback));
    }

    public override void EndInteraction(IInteractor interactor, bool isCompleted = true, Func<bool> checkOnEnd = null)
    {
        isBroken = true;
        if (cost <= 0)
        {
            TileUnlocked();
        }
        base.EndInteraction(interactor);
    }

    public virtual void TileUnlocked(bool expend = true)
    {
        if (expend) ExpendManager.Instance.Expend(this);

        AudioService.Play(AudioType.TileUnlockSound);
        isExpended = true;
        SetState(true, true);
        //CheckTutorialState();
    }

    //private static void CheckTutorialState()
    //{
    //    if (!TutorialManager.Instance.IsTutorialCompleted(TutorialType.TileUnlockTutorial) &&
    //        TutorialManager.Instance.CheckTutorialPlaying(TutorialType.TileUnlockTutorial))
    //    {
    //        TutorialManager.Instance.OnTutorialComplete.Invoke(TutorialType.TileUnlockTutorial);
    //    }
    //}

    // private void CheckNewItemUnlocked(List<Interactable> allUnlockedInteractables)
    // {
    //     foreach (var unlockedInteractable in allUnlockedInteractables)
    //     {
    //         var outline = unlockedInteractable.GetComponentInChildren<Outline>(true);
    //         if (outline == null) continue;
    //         outline.enabled = true;
    //         if (unlockedInteractable is ContainerInteractable containerInteractable) containerInteractable.ShowIcon(true);
    //         float hideDelay = ConfigurationService.Configurations.NewItemUnlockedTextHideDelay;
    //         NewItemUnlocked.ShowWarning("NEW SHIRT!", outline.transform.position, 0.75f, hideDelay);
    //         PanelManager.Instance.Show(PopupType.OffScreenIndicatorPanel, new OffScreenIndicatorPanelData(outline.gameObject));
    //         StartCoroutine(HideNewItemIndicatorWithDelay(unlockedInteractable, outline, hideDelay));
    //     }
    // }

    private IEnumerator HideNewItemIndicatorWithDelay(Interactable interactable, Outline outline, float hideDelay)
    {
        yield return new WaitForSeconds(hideDelay);
        outline.enabled = false;
        PanelManager.Instance.Hide(PopupType.OffScreenIndicatorPanel);
        if (interactable is ContainerInteractable containerInteractable)
        {
            //containerInteractable.UpdateIcon();
            if (containerInteractable.ShelfAmountCanvasParticle != null)
                containerInteractable.ShelfAmountCanvasParticle.gameObject.SetActive(false);
        }
    }
}

public class ExpendableSaveData
{
    public int Cost;
    public int BaseCost;
}
