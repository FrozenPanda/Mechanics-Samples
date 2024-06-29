using System;
using System.Collections;
using System.Collections.Generic;
using _Game.Scripts.Systems.StarUpgradeSystem;
using _Game.Scripts.Systems.WeeklyEventSystem;
using UnityEngine;
using UnityEngine.UI;

public class MissionNextLevelButtonController : MonoBehaviour
{
     //[SerializeField] private UpgradeArrowController UpgradeArrowController;
    [SerializeField] private Button buttonObject;
    private float CurrentScale;
    private float timer;
    public float IncreaseSizeRatio = 1.5f;
    public float IncreaseSizeSpeed = 1f;
    
    //[SerializeField] private bool isBouncing = false;
    //[SerializeField] private Vector3 startPos = Vector3.zero;

    //private RectTransform RectTransform => rectTransform ??= GetComponent<RectTransform>();
    //private RectTransform rectTransform;
    
    private enum ScaleState
    {
        Idle,
        Increasing,
        Decreasing
    }

    private ScaleState _scaleState;

    private void OnEnable()
    {
        buttonObject.onClick.AddListener(OnButtonClick);
        //InteractionManager.Instance.OnUpgradeMax.AddListener((objecId) => UpdateState());
        IdleExchangeService.OnDoExchange[CurrencyService.ActiveCurrencyType].AddListener(UpdateState);
        EventManager.Instance.OnBeforeEventStateChange.AddListener(StopListenCurrentCoinChange);
        EventManager.Instance.OnEventStateChanged.AddListener(StartListenCurrentCoinChange);
        LevelManager.Instance.LevelLoaded.AddListener(LevelLoaded);
        LevelManager.Instance.LevelExpended.AddListener(LevelLoaded);
        PlayerModeManager.Instance.OnBeforeModeChange.AddListener(StopListenCurrentCoinChange);
        PlayerModeManager.Instance.OnModeChanged.AddListener(StartListenCurrentCoinChange);
        MissionManager.Instance.OnMissionCompleted.AddListener(ListenQuestCompleteState);
        //TutorialManager.Instance.OnTutorialComplete.AddListener(CompleteTutorialListener);
        UpdateState();
    }

    private void OnButtonClick()
    {
        PanelManager.Instance.Show(PopupType.RenovatePanel, new PanelData());
    }

    private void StartListenCurrentCoinChange(PlayerMode mode)
    {
        StartListenCurrentCoinChange();
    }

    private void StartListenCurrentCoinChange(bool arg0 = false)
    {
        IdleExchangeService.OnDoExchange[CurrencyService.ActiveCurrencyType].AddListener(UpdateState);
        CoroutineDispatcher.ExecuteNextFrame(() => UpdateState());
    }

    private void ListenQuestCompleteState()
    {
        UpdateState();
    }

    private void StopListenCurrentCoinChange()
    {
        StopListenCurrentCoinChange(false);
    }

    private void StopListenCurrentCoinChange(bool arg0)
    {
        IdleExchangeService.OnDoExchange[CurrencyService.ActiveCurrencyType].RemoveListener(UpdateState);
    }

    private void OnDisable()
    {
        buttonObject.onClick.RemoveListener(OnButtonClick);
        
        if (StarUpgradeManager.IsAvailable() && EventManager.IsAvailable())
        {
            var currencyType = CurrencyService.ActiveCurrencyType;
            IdleExchangeService.OnDoExchange[currencyType].RemoveListener(UpdateState);
            EventManager.Instance.OnBeforeEventStateChange.RemoveListener(StopListenCurrentCoinChange);
            EventManager.Instance.OnEventStateChanged.RemoveListener(StartListenCurrentCoinChange);
        }

        if (LevelManager.IsAvailable())
        {
            LevelManager.Instance.LevelLoaded.RemoveListener(LevelLoaded);
            LevelManager.Instance.LevelExpended.AddListener(LevelLoaded);
        }
            

        if (PlayerModeManager.IsAvailable())
        {
            PlayerModeManager.Instance.OnBeforeModeChange.RemoveListener(StopListenCurrentCoinChange);
            PlayerModeManager.Instance.OnModeChanged.RemoveListener(StartListenCurrentCoinChange);
        }
        
        if(MissionManager.IsAvailable())
            MissionManager.Instance.OnMissionCompleted.RemoveListener(ListenQuestCompleteState);
    }

    private void LevelLoaded()
    {
        UpdateState();
        HelperFinger.StopFingerMove();
    }

    private void UpdateState(IdleNumber v = null, IdleNumber a = null)
    {
        //bool allMaxed = InteractionManager.Instance.IsAllReqInteractableMaxed(InteractableType.NFProductContainer);

        var currentCoin = IdleExchangeService.GetIdleValue(CurrencyService.ActiveCurrencyType);
        var requiredCoin = LevelManager.Instance.GetLevelEndCost();

        //allMaxed &= currentCoin >= requiredCoin;

        //var enoughCoin = currentCoin >= requiredCoin;
        var enoughCoin = true;
        var questsCompleted = MissionManager.Instance.IsAllQuestCompleted();
        if (enoughCoin && questsCompleted)
        {
            if (_scaleState == ScaleState.Idle)
            {
                timer = 1f;
                _scaleState = ScaleState.Increasing;
                buttonObject.interactable = true;
            }
            
            buttonObject.interactable = true;
        }
        else
        {
            buttonObject.interactable = false;
            _scaleState = ScaleState.Idle;
            timer = 1f;
            buttonObject.transform.localScale = Vector3.one;
        }
    }

    private void Update()
    {
        return;
        
        if (MissionManager.Instance.IsLeveledRewardTaken())
        {
            buttonObject.transform.localScale = Vector3.one;
            return;
        }

            switch (_scaleState)
        {
            case ScaleState.Idle:
                break;
            case ScaleState.Increasing:

                if (timer < IncreaseSizeRatio)
                    timer += Time.deltaTime * IncreaseSizeSpeed;
                else
                    _scaleState = ScaleState.Decreasing;
                
                buttonObject.transform.localScale = Vector3.one * timer;
                
                break;
            case ScaleState.Decreasing:
                
                if (timer > 1f)
                    timer -= Time.deltaTime * IncreaseSizeSpeed;
                else
                    _scaleState = ScaleState.Increasing;

                buttonObject.transform.localScale = Vector3.one * timer;
                
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
