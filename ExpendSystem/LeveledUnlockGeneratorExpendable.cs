using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Game.Scripts.HouseCoffee;
using _Game.Scripts.Systems.StarUpgradeSystem;
using Pathfinding;
using UnityEngine;

public class LeveledUnlockGeneratorExpendable : UnlockProductExpendable
{
    [SerializeField] private NFProductContainer nfProductContainer;
    [SerializeField] private int unlockLevel;
    [SerializeField] private List<NFTrainWorkerAI> workers;
    [SerializeField] private GameObject unlockArea;
    [SerializeField] private GameObject readyPart;
    [SerializeField] private GameObject ruinParticle;

    private Camera CurrentCamera => currentCamera != null ? currentCamera : (currentCamera = CameraManager.Instance.GetCurrentCamera());
    private Camera currentCamera;

    public override NFProductContainer ProductContainer => nfProductContainer;
    public int UnlockLevel => unlockLevel;

    private GameObject ruinObject;
    protected override void OnEnable()
    {
        base.OnEnable();
        IdleExchangeService.OnDoExchange[CurrencyService.ActiveCurrencyType].AddListener(OnCoinChanged);
        NFInventoryManager.Instance.OnItemCountChanged.AddListener(OnResourceChanged);
        LevelManager.Instance.LevelExpended.AddListener(LoadRuinModel);
        
        if (CheckClickedToObject != null)
        {
            CheckClickedToObject.ClickedAction = ClickAction;
            CheckClickedToObject.PlayHapticOnMouseUp = false;
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        IdleExchangeService.OnDoExchange[CurrencyService.ActiveCurrencyType].RemoveListener(OnCoinChanged);
        NFInventoryManager.Instance.OnItemCountChanged.RemoveListener(OnResourceChanged);
        if (LevelManager.IsAvailable())
            LevelManager.Instance.LevelExpended.RemoveListener(LoadRuinModel);
    }

    private void OnLevelExpended()
    {
        if (IsExpend) return;
        SetState(false);
    }

    protected override void Start()
    {
        base.Start();
        
        TryOpenExpendable();
    }

    protected void TryOpenExpendable()
    {
        if(ExpendManager.Instance.IsExpended(this))
            return;
        var unlockData = BuildingUnlockTimerManager.Instance.GetUnlockTime(ID);
        if (unlockData.isRegistered)
        {
            if (unlockData.unlockType == UnlockType.BuildingUnlock)
            {
                if (unlockData.remainTime > 0f)
                {
                    UnderConstruction = true;
                    readyPart.gameObject.SetActive(false);
                    var construction = PoolingSystem.Instance.Create<BuildingConstructionTimer>(PoolType.BuildingUnlockTimer);
                    construction.transform.position = transform.position;
                    CurrentBuildingConstructionTimer = construction;
                    construction.openingOne = this;
                    construction.Load(unlockData.remainTime , () => construction.openingOne.UnlockExpandable() , ID);
                }
                else
                {
                    UnlockExpandable();
                }
            }
            else
            {
                UnlockExpandable();
            }
        }
    }

    private bool UnderConstruction = false;
    protected BuildingConstructionTimer CurrentBuildingConstructionTimer;
    public override void StartConstruction()
    {
        base.StartConstruction();
        
        BuildingUnlockTimerManager.Instance.AddToList(this.ID , this.UnlockCostData.timer , _unlockType: UnlockType.BuildingUnlock);
                
            if (this.UnlockCostData.timer > 0)
            {
                UnderConstruction = true;
                readyPart.gameObject.SetActive(false);
                //ConstructionContinue = true;
                this.underConstruction = true;
                var construction = PoolingSystem.Instance.Create<BuildingConstructionTimer>(PoolType.BuildingUnlockTimer);
                construction.transform.position = this.transform.position;
                CurrentBuildingConstructionTimer = construction;
                construction.openingOne = this;
                construction.Load(this.UnlockCostData.timer , () => construction.openingOne.UnlockExpandable() , ID);
                    
            }
            else
            {
                this.UnlockExpandable();
            }
        
    }

    /*public override void SetExpendState(int expendedId = -1)
    {
        if (expendedId != ExpandableId || IsExpend) return;
        base.SetExpendState(expendedId);
    }

    public override void SetExpendState(bool state, bool isFirst = false)
    {
        var level = LevelManager.Instance.GetLevelData() % 100;
        var canUnlock = (unlockLevel - 1) <= level;

        state = canUnlock;

        base.SetExpendState(state, isFirst);

    }

    public override void SetState(bool _isExpended, bool isAnimated = false)
    {
        var level = LevelManager.Instance.GetLevelData() % 100;
        var canUnlock = (unlockLevel - 1) <= level;

        _isExpended = canUnlock;
        base.SetState(_isExpended, isAnimated);
    }*/

    protected override void LoadRuinModel()
    {
        if (ruinObject != null) Destroy(ruinObject);

        if (TutorialManager.Instance.IsTutorialCompleted(TutorialType.CraftMachineUnlockTutorial))
            shouldCheckTutorial = false;

        var level = LevelManager.Instance.GetLevelData() % 100;
        var isInUnlockedLevel = unlockLevel <= level;

        var collectableObjectData = CollectableObjectService.GetCollectableObjectData(ProductContainer.GetObjectType());
        //var ruinModel = isInUnlockedLevel ? collectableObjectData.AttachmentFBXs[0] : collectableObjectData.MachineRuinFbx;
        var ruinModel = collectableObjectData.MachineRuinFbx;

        if (ruinModel == null || ruinModelPos == null) return;

        ruinObject = GameObject.Instantiate(ruinModel, ruinModelPos);
        ruinObject.transform.localPosition = Vector3.zero;
        ruinObject.transform.localScale = Vector3.one;
        ruinObject.transform.localRotation = Quaternion.identity;

        ruinModelCollider = CheckClickedToObject.GetComponent<BoxCollider>();
        //ruinModelCollider.enabled = true;

        //UpgradeArrowController.transform.position += Vector3.up * ruinModelCollider.size.y;
        var upgradeArrowPos = UpgradeArrowController.transform.position;
        UpgradeArrowController.transform.position = new Vector3(upgradeArrowPos.x, ruinModelCollider.size.y, upgradeArrowPos.z);

        //CheckClickedToObject.ClickedAction = ClickAction;
        //CheckClickedToObject.PlayHapticOnMouseUp = false;

        //if (isInUnlockedLevel) LoadWorkersWave();
        if (isInUnlockedLevel)
        {
            HideWorkers();
        }
        else LoadWorkersWork();
        
        /*if (UnlockCost[0].Cost < IdleExchangeService.GetIdleValue(CurrencyType.Coin))
        {
            readyPart.SetActive(true);
        }
        else
        {
            readyPart.SetActive(false);
        }*/
        
        OnCoinChanged(new IdleNumber() , new IdleNumber());

        
        
        unlockArea.SetActive(!isInUnlockedLevel);
        //ruinObject.SetActive(isInUnlockedLevel);
    }

    private void HideWorkers()
    {
        foreach (var worker in workers)
        {
            worker.gameObject.SetActive(false);
        }
    }

    private void LoadWorkersWave()
    {
        foreach(var worker in workers)
        {
            Debug.Log("Load wave");

            var point = worker.FirstPoint;
            var workerObject = worker.gameObject;
            worker.enabled = false;

            workerObject.GetComponent<AILerp>().enabled = false;

            workerObject.transform.parent = point;
            workerObject.transform.localPosition = Vector3.zero;
            workerObject.transform.rotation = Quaternion.Euler(transform.rotation.x, CurrentCamera.transform.rotation.y + 180f, transform.rotation.z);
            workerObject.GetComponent<CharacterAnimationController>().ActiveAnimationController.PlayAnimation(AnimationType.Wave);
        }
    }

    private void LoadWorkersWork()
    {
        foreach (var worker in workers)
        {
            worker.StartWork();
        }
    }

    private void ClickAction()
    {
        var level = LevelManager.Instance.GetLevelData() % 100;

        if (MiniUnlockPanel.UnlockProductExpendable == this ||
             PanelManager.Instance.IsAnyBigPanelShowing())
        {
            return;
        }

        if (unlockLevel <= level)
            CoroutineDispatcher.ExecuteNextFrame(TryShowMiniUnlockPanel);
        else
            WarningText.ShowWarning(WarningTextType.DefaultWarning, $"Unlocked at level {unlockLevel}");
    }

    protected override void ShowMiniUnlockPanel()
    {
        base.ShowMiniUnlockPanel();
        
        /*if(UnderConstruction)
            ConstructionInfoPanel.ShowCanvas(CurrentBuildingConstructionTimer , ID , transform);*/
    }

    protected bool shouldCheckTutorial = true;
    protected bool sendDataToBot = false;
    private bool CoinEnoughForUnlock;
    private void OnCoinChanged(IdleNumber changeAmount, IdleNumber finalAmount)
    {
        /*if (UnlockCost.Count > 0 && UnlockCost[0].Cost < IdleExchangeService.GetIdleValue(CurrencyType.Coin))
        {
            readyPart.SetActive(true);
        }
        else if(UnlockCost.Count < 1)
        {
            readyPart.SetActive(true);
        }*/

        /*if (UnlockCost.Count > 0)
        {
            if (UnlockCost[0].Cost < IdleExchangeService.GetIdleValue(CurrencyType.Coin))
            {
                if (shouldCheckTutorial && LevelManager.Instance.ActiveLevelId == 1 &&
                    !TutorialManager.Instance.IsTutorialCompleted(TutorialType.CraftMachineUnlockTutorial) && !TutorialManager.Instance.CheckTutorialPlaying(TutorialType.CraftMachineUnlockTutorial))
                {
                    TutorialManager.Instance.CheckTutorialWithDelay(TutorialType.CraftMachineUnlockTutorial , 0.5f);
                    shouldCheckTutorial = false;
                }
                readyPart.SetActive(true);
                
                if (!sendDataToBot && BotManager.IsAvailable())
                {
                    //BotManager.Instance.OnContainerUnlockable?.Invoke(this);
                    //sendDataToBot = true;
                }
            }
            else
                readyPart.SetActive(false);
        }
        else
        {
            readyPart.SetActive(true);
            
            if (!sendDataToBot && BotManager.IsAvailable())
            {
                BotManager.Instance.OnContainerUnlockable?.Invoke(this);
                sendDataToBot = true;
            }
        }*/
        
        CheckFinalForUnlock();
    }

    protected bool ResourceEnoughForUnlock;
    private void OnResourceChanged()
    {
        CheckFinalForUnlock();
    }

    protected void CheckFinalForUnlock()
    {
        if (!UnderConstruction && StarUpgradeManager.CheckHaveCosts(UnlockCostData.data))
        {
            readyPart.SetActive(true);
        }
        else
        {
            readyPart.SetActive(false);
        }
    }
}
