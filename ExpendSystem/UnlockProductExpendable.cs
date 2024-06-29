using System;
using System.Collections.Generic;
using _Game.Scripts.HouseCoffee;
using _Game.Scripts.Systems.StarUpgradeSystem;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;

[Tooltip("Bu sınıftan kalıtım alınırsa ExpandableType atanması yeni sınıf icin eklenmeli!!")]
public class UnlockProductExpendable : TileExpendable
{
    public virtual List<ProductCostTuple> UnlockCost => StarUpgradeManager.Instance.GetUnlockCost(TargetObjectId);
    public virtual (List<UpgradeAbleCostTuple> data , int timer) UnlockCostData => StarUpgradeManager.Instance.GetUnlockCostData(TargetObjectId);

    public bool underConstruction = false;

    private string TargetObjectId => ProductContainer.InteractionID;
    
    public bool IsRepairing {private get; set; }

    // [Header("UnlockProductExpendable")]
    // [SerializeField] private string targetObjectId;
    public SceneUpgradeArrowController UpgradeArrowController => upgradeArrowController ??= lockedPart.GetComponentInChildren<SceneUpgradeArrowController>(true);
    private SceneUpgradeArrowController upgradeArrowController;

    protected virtual ArrowPlacementType ArrowPlacementType
    {
        get
        {
            if (!setArrowPlacementTypeInCode) return arrowPlacementType;
            
            if (!isArrowPlacementTrySet && !GameUtility.IsNull(ProductContainer))
            {
                arrowPlacementType = ProductContainer.ArrowPlacementType;
                isArrowPlacementTrySet = true;
            }

            return arrowPlacementType;
        }
    }
    
    [SerializeField] protected bool setArrowPlacementTypeInCode = true;
    [HideIf("setArrowPlacementTypeInCode")][SerializeField] protected ArrowPlacementType arrowPlacementType = ArrowPlacementType.Center;
    private bool isArrowPlacementTrySet;
    [SerializeField] protected bool hasMiniUpgradePanelOffsetPos = false;
    [ShowIf("hasMiniUpgradePanelOffsetPos")][SerializeField] protected Vector3 miniUpgradePanelOffsetPos;
    [SerializeField] public Transform ruinModelPos;

    public virtual NFProductContainer ProductContainer => productContainer;
    protected NFProductContainer productContainer;

    public ExpendableGiftBox ExpendableGiftBox => expendableGiftBox ??= GetComponentInChildren<ExpendableGiftBox>();
    private ExpendableGiftBox expendableGiftBox;

    protected CheckClickedToObject CheckClickedToObject => checkClickedToObject ??= GetComponentInChildren<CheckClickedToObject>(true);
    protected CheckClickedToObject checkClickedToObject;

    public Transform RepairAnimationPosition => repairAnimationPosition;

    protected BoxCollider ruinModelCollider = null;

    [Header("Repair Animation position")]
    [SerializeField][Range(0.1f,1f)]public float RepairAnimDistance;
    [SerializeField] private Transform repairAnimationPosition;

    private ExpandableType ExpandableType
    {
        get
        {
            if (expandableType == ExpandableType.Undefined) InitExpandableType();
            return expandableType;
        }
    }
    private ExpandableType expandableType = ExpandableType.Undefined;

    public string ProductId => ProductContainer != null ? ProductContainer.ID : string.Empty;
    
    public virtual void UnlockExpandable()
    {
        TrackingService.TrackEvent(TrackType.ObjectUnlock, ExpandableType, ProductId);
        Debug.Log($"OBJECT UNLOCK => ExpandableType : {ExpandableType}, ProductId :{ProductId}");
        TileUnlocked();
        //CheckTutorial();
        OrderManager.Instance.SetLastCompleteOrderTime();

        if (!TutorialManager.Instance.IsTutorialCompleted(TutorialType.CraftMachineUnlockTutorial) &&
                LevelManager.Instance.GetLevelData() == CraftMachineUnlockTutorial.TUTORIAL_LEVEL &&
                ExpandableId == CraftMachineUnlockTutorial.EXPENDABLE_ID)
        {
            CraftMachineUnlockTutorial.CheckTutorialCompleteState();
            //TutorialManager.Instance.TutorialComplete(TutorialType.CraftMachineUnlockTutorial);
        }
        // ExpendManager.Instance.Expend(this);
        // var boxUnlock = PoolingSystem.Instance.Create<BoxUnlock>(PoolType.BoxUnlock);
        // var boxUnlockPos = transform.position;
        // boxUnlock.InitializeBox(boxUnlockPos, () => TileUnlocked(false));
    }
    
    

    public override void OnExpend()
    {
        OnExpendScaleAnim(transform, duration:1f, endAction: base.OnExpend);
        CoroutineDispatcher.ExecuteWithDelay(1.5f, UpdateAStarPath);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (CheckClickedToObject != null)
        {
            CheckClickedToObject.ClickedAction = ClickedToExpendable;
            CheckClickedToObject.PlayHapticOnMouseUp = false;
        }

        IdleExchangeService.OnDoExchange[CurrencyService.ActiveCurrencyType].AddListener(OnDoExchange);
        //TutorialManager.Instance.OnTutorialComplete.AddListener(OnTutorialCompleteListener);
    }

    private void OnDoExchange(IdleNumber num1, IdleNumber num2)
    {
        //CheckCraftMachineUnlockTutorial();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (CheckClickedToObject != null)
        {
            CheckClickedToObject.ClickedAction = null;
            CheckClickedToObject.PlayHapticOnMouseUp = true;
        }

        IdleExchangeService.OnDoExchange[CurrencyService.ActiveCurrencyType].RemoveListener(OnDoExchange);
        //if(TutorialManager.IsAvailable()) TutorialManager.Instance.OnTutorialComplete.RemoveListener(OnTutorialCompleteListener);
    }

    protected override void Initialize()
    {
        base.Initialize();
        //CheckCraftMachineUnlockTutorial();
    }

    public override void SetState(bool _isExpended, bool isAnimated = false)
    {
        base.SetState(_isExpended, isAnimated);
        if (!_isExpended)
        {
            //LoadRuinModel();
            CoroutineDispatcher.ExecuteWithDelay(1, LoadRuinModel);
        }
    }

    protected virtual void LoadRuinModel()
    {
        var ruinModel = CollectableObjectService.GetCollectableObjectData(ProductContainer.GetObjectType()).MachineRuinFbx;
        if (ruinModel == null || ruinModelPos == null) return;

        var spawnedObject = GameObject.Instantiate(ruinModel, ruinModelPos);
        spawnedObject.transform.localPosition = Vector3.zero;
        spawnedObject.transform.localScale = Vector3.one;
        spawnedObject.transform.localRotation = Quaternion.identity;

        ruinModelCollider = spawnedObject.GetComponent<BoxCollider>();

        //UpgradeArrowController.transform.position += Vector3.up * ruinModelCollider.size.y;

        checkClickedToObject = spawnedObject.GetComponent<CheckClickedToObject>();

        CheckClickedToObject.ClickedAction = ClickedToExpendable;
        CheckClickedToObject.PlayHapticOnMouseUp = false;
    }

    protected virtual void Start()
    {
        InitExpandableType();
    }

    private void InitExpandableType()
    {
        if (expandableType != ExpandableType.Undefined) return;
        
        if (this is NCUnlockHouseExpandable)
            expandableType = ExpandableType.House;
        else if (this is UnlockTileExpendable)
            expandableType = ExpandableType.Tile;
        else
            expandableType = ExpandableType.Store;
                
        ExpendManager.Instance.AddToExpandableTypesMap(expandableType, this);
    }

    public override void SetExpendState(int expendedId = -1)
    {
        if (isExpended) return;
        if (RequiredObjects != null && RequiredObjects.Count > 0 && !ExpendManager.Instance.IsAllExpended(RequiredObjects))
        {
            lockedPart.SetActive(true);
            unlockedPart.SetActive(false);
            return;
        }
    }

    private void UpdateAStarPath()
    {
        bool isTileUnlocked = ExpendManager.Instance.IsExpended(this);
        if (!isTileUnlocked)
        {
            var colliders = lockedPart.GetComponentsInChildren<Collider>(true);
            Interactable.UpdateAStarPath(colliders);
        }
    }

    private void ClickedToExpendable()
    {
        if ( MiniUnlockPanel.UnlockProductExpendable == this ||
             PanelManager.Instance.IsAnyBigPanelShowing())
        {
            return;
        }
        CoroutineDispatcher.ExecuteNextFrame(TryShowMiniUnlockPanel);
    }

    public void TryShowMiniUnlockPanelFromMission()
    {
        if(IsRepairing ||MiniUpgradePanel._IsShowing || MiniUnlockPanel._IsShowing || !TutorialManager.Instance.IsTutorialCompleted(TutorialType.ComingAnimationTutorial)) return;
        ShowMiniUnlockPanelFromMission();
    }

    protected void TryShowMiniUnlockPanel()
    {
        if(IsRepairing ||MiniUpgradePanel._IsShowing || MiniUnlockPanel._IsShowing || !TutorialManager.Instance.IsTutorialCompleted(TutorialType.ComingAnimationTutorial) || TutorialManager.Instance.CheckTutorialPlaying(TutorialType.MarketAutomateTutorial)) return;
        ShowMiniUnlockPanel();
    }
    
    protected virtual void ShowMiniUnlockPanelFromMission()
    {
        if (!GameUtility.IsNull(UpgradeArrowController))
        {
            UpgradeArrowController.IsThisObjectPanelShowing = true;
            UpgradeArrowController.HidePanel();
        }
        MiniUnlockPanel.ShowCanvas(GetUnlockPanelPos() + new Vector3(0f, -2f ,-2f), ArrowPlacementType, this);
        AudioService.Play(AudioType.MiniUpgradeSound);
        CheckClickedToObject.PlayHaptic();
    }

    protected virtual void ShowMiniUnlockPanel()
    {
        if (underConstruction)
        {
            
            return;
        }
        
        if (!GameUtility.IsNull(UpgradeArrowController))
        {
            UpgradeArrowController.IsThisObjectPanelShowing = true;
            UpgradeArrowController.HidePanel();
        }
        MiniUnlockPanel.ShowCanvas(GetUnlockPanelPos(), ArrowPlacementType, this);
        AudioService.Play(AudioType.MiniUpgradeSound);
        CheckClickedToObject.PlayHaptic();
    }

    private Vector3 GetUnlockPanelPos()
    {
        if (GameUtility.IsNull(CheckClickedToObject)) return transform.position;
        
        Vector3 pos = CheckClickedToObject.transform.position;
        if (ProductContainer != null) pos += ProductContainer.ElevationOffset;
        if (hasMiniUpgradePanelOffsetPos) pos += miniUpgradePanelOffsetPos;

        var lastPos = Vector3.zero;
        lastPos.x = pos.x;
        lastPos.z = pos.z;

        if (ruinModelCollider != null) lastPos += Vector3.up * ruinModelCollider.size.y;

        return lastPos;
    }

    

    /*private void CheckCraftMachineUnlockTutorial()
    {
        if (TutorialManager.Instance.IsTutorialCompleted(TutorialType.FirstOrderTutorial) &&
            !TutorialManager.Instance.IsTutorialCompleted(TutorialType.CraftMachineUnlockTutorial) &&
            LevelManager.Instance.GetLevelData() == CraftMachineUnlockTutorial.TUTORIAL_LEVEL &&
            ExpandableId == CraftMachineUnlockTutorial.EXPENDABLE_ID && StarUpgradeManager.CheckCanBuy(UnlockCost))
        {
            TutorialManager.Instance.CheckTutorial(TutorialType.CraftMachineUnlockTutorial);
        }
    }*/

    /*private void OnTutorialCompleteListener(TutorialType tutorialType)
    {
        if(tutorialType == TutorialType.FirstOrderTutorial)
        {
            CoroutineDispatcher.StartCoroutine(() =>
            {
                CheckCraftMachineUnlockTutorial();
            }, 4f);
        }
    }*/

    public virtual void MiniUnlockPanelHidden(bool updateUI)
    {
        if (updateUI && !GameUtility.IsNull(UpgradeArrowController))
        {
            UpgradeArrowController.IsThisObjectPanelShowing = false;
            UpgradeArrowController.UpdateUI();
        }
    }

    public virtual void StartConstruction()
    {
        
    }
}

public enum ExpandableType
{
    Undefined = 0,
    Store = 1,
    House = 2,
    Tile = 3,
}
