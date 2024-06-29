using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VillageUpCanvas : MonoBehaviour
{
    private const string MiniUnlockPanelPath = "3DUI/VillageUpCanvas";
    
    private static VillageUpCanvas _activeVillageUpCanvas;
    private static VillageUpCanvas activeVillageUpCanvas
    {
        get
        {
            if (_activeVillageUpCanvas == null)
            {
                _activeVillageUpCanvas = Instantiate(Resources.Load<VillageUpCanvas>(MiniUnlockPanelPath));
                _activeVillageUpCanvas.gameObject.SetActive(false);
                DontDestroyOnLoad(_activeVillageUpCanvas.gameObject);
            } 
            return _activeVillageUpCanvas;
        }
    }
    
    private CameraController CameraController => cameraController ?? Camera.main.GetComponent<CameraController>();
    private CameraController cameraController;

    [SerializeField] private Vector3 CamFollowPos;

    private IdleNumber availableVillageCoinCount;
    public Button UpButton;
    public Button NotEnoughWoodButton;
    [SerializeField] private TextMeshProUGUI woodText;

    private bool isShowing;
    private bool tutorialCompletedAlready;
    
    public static Transform GetUpButton()
    {
        return activeVillageUpCanvas.UpButton.transform;
    }

    private void Awake()
    {
        UpButton.onClick.RemoveAllListeners();
        UpButton.onClick.AddListener(UpButtonAction);
        NotEnoughWoodButton.onClick.AddListener(NotEnoughWoodAction);

        if (TutorialManager.Instance.IsTutorialCompleted(TutorialType.VillageUpgradeTutorial))
            tutorialCompletedAlready = true;
    }

    private void OnEnable()
    {
        IdleExchangeService.OnDoExchange[CurrencyType.VillageCoin].AddListener(ReloadPanel);
        tutorialCompletedAlready = false;
    }

    private void OnDisable()
    {
        IdleExchangeService.OnDoExchange[CurrencyType.VillageCoin].RemoveListener(ReloadPanel);
    }

    public static void ShowCanvas()
    {
        activeVillageUpCanvas.isShowing = true;
        
        activeVillageUpCanvas.gameObject.SetActive(true);

        activeVillageUpCanvas.ReloadPanel();
    }

    public static void HideCanvas()
    {
        activeVillageUpCanvas.gameObject.SetActive(false);

        activeVillageUpCanvas.isShowing = false;
    }

    public static void ReloadPanelOutside()
    {
        activeVillageUpCanvas.ReloadPanel();
    }

    private void ReloadPanel(IdleNumber a , IdleNumber b)
    {
        ReloadPanel();
    }

    public void ReloadPanel()
    {
        if(!isShowing)
            return;
        
        activeVillageUpCanvas.availableVillageCoinCount = VillageManager.Instance.GetAvailableVillageCoinCount();

        if (activeVillageUpCanvas.availableVillageCoinCount < VillageManager.Instance.WoodUpgradePrice)
        {
            activeVillageUpCanvas.UpButton.interactable = false;
            activeVillageUpCanvas.NotEnoughWoodButton.gameObject.SetActive(true);
        }
        else
        {
            activeVillageUpCanvas.UpButton.interactable = true;
            activeVillageUpCanvas.NotEnoughWoodButton.gameObject.SetActive(false);
        }

        woodText.text = availableVillageCoinCount.ToString();
    }

    private void UpButtonAction()
    {
        if(activeVillageUpCanvas.availableVillageCoinCount < VillageManager.Instance.WoodUpgradePrice)
            return;
        VillageManager.Instance.SpawnUpgrader();
        ReloadPanel();
        if (!tutorialCompletedAlready)
        {
            if (!TutorialManager.Instance.IsTutorialCompleted(TutorialType.VillageUpgradeTutorial))
            {
                TutorialManager.Instance.TutorialComplete(TutorialType.VillageUpgradeTutorial);
            }
            
            TutorialFinger.StopFingerMove();
            tutorialCompletedAlready = true;
        }
    }

    private void NotEnoughWoodAction()
    {
        WarningText.ShowWarning(VillageCanvas.VillageCanvasTransform.NotEnoughWoodTextPos , WarningTextType.DefaultWarning , WarningType.NotEnoughWood);
    }

    private void Update()
    {
        transform.position = CameraController.transform.position + CamFollowPos;
        PressButtonStateController();
    }
    
    #region ButtonPressedSate

    private float pressedDelay = 0.5f;
    private bool isPressed = false;
    public void PressedState(bool isPressed)
    {
        if (!isPressed)
        {
            _buttonState = ButtonState.Idle;
            pressedDelay = 0.15f;
        }
        else
        {
            _buttonState = ButtonState.Pressed;
        }
    }
    
    private enum ButtonState
    {
        Idle,
        Pressed,
        ActionTaking
    }

    private ButtonState _buttonState;

    private void PressButtonStateController()
    {
        switch (_buttonState)
        {
            case ButtonState.Idle:
                break;
            case ButtonState.Pressed:

                if (pressedDelay > 0f)
                    pressedDelay -= Time.deltaTime;
                else
                {
                    pressedDelay = 0.1f;
                    _buttonState = ButtonState.ActionTaking;
                }
                
                break;
            case ButtonState.ActionTaking:

                if (pressedDelay > 0f)
                {
                    pressedDelay -= Time.deltaTime;
                }
                else
                {
                    UpButtonAction();
                    pressedDelay = 0.1f;
                }
                
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    #endregion
}
