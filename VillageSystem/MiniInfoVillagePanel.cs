using System;
using System.Collections;
using System.Collections.Generic;
using _Game.Scripts.HouseCoffee;
using lib.Managers.AnalyticsSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MiniInfoVillagePanel : MonoBehaviour
{
    private const string MiniUnlockPanelPath = "3DUI/MiniInfoPanel";

    [Header("CameraZoomOptions")]
    [SerializeField] private float zoomAmount;
    [SerializeField] private float ZAmount;
    
    private static MiniInfoVillagePanel _activeMiniInfoVillagePanel;
    private static MiniInfoVillagePanel activeMiniInfoVillagePanel
    {
        get
        {
            if (_activeMiniInfoVillagePanel == null)
            {
                _activeMiniInfoVillagePanel = Instantiate(Resources.Load<MiniInfoVillagePanel>(MiniUnlockPanelPath));
                _activeMiniInfoVillagePanel.gameObject.SetActive(false);
                DontDestroyOnLoad(_activeMiniInfoVillagePanel.gameObject);
            } 
            return _activeMiniInfoVillagePanel;
        }
    }
    
    private static ScaleTweenController ScaleTweenController => _scaleTweenController ??= activeMiniInfoVillagePanel.GetComponent<ScaleTweenController>();
    private static ScaleTweenController _scaleTweenController;
    
    private WorldCanvasHideCheck WorldCanvasHideCheck => worldCanvasHideCheck ??= GetComponentInChildren<WorldCanvasHideCheck>();
    private WorldCanvasHideCheck worldCanvasHideCheck;
    
    private CameraController CameraController => cameraController ?? Camera.main.GetComponent<CameraController>();
    private CameraController cameraController;

    private VillageHome _villageHome;

    //Home Infos
    [SerializeField]public TextMeshProUGUI homeInfoText;
    [SerializeField]public TextMeshProUGUI homelevelText;
    [SerializeField]public TextMeshProUGUI skillName;
    [SerializeField]public TextMeshProUGUI skillEffectAmount;

    private float rewardRemainTimer;
    private float rewardInterval;
    [SerializeField]public Image FillBarImage;
    [SerializeField]public TextMeshProUGUI rewardRemainTimeText;

    [SerializeField]public Image RewardIcon;
    
    [SerializeField] public Button collectButton;

    [SerializeField] public VillageInfoPanelExtraInfoSystem extraInfoPanel;
    [SerializeField] public Button openExtraInfoPanelButton;
    
    private void Awake()
    {
        WorldCanvasHideCheck.HideCanvasAction = HideCanvas;
    }

    public static void ShowCanvas(VillageHome home)
    {
        activeMiniInfoVillagePanel.extraInfoPanel.gameObject.SetActive(false);
        
        activeMiniInfoVillagePanel._villageHome = home;
        activeMiniInfoVillagePanel.gameObject.SetActive(true);
        activeMiniInfoVillagePanel.SetPanelPos(home.transform);
        ScaleTweenController.ScaleTween(activeMiniInfoVillagePanel.transform, true);
        
        //Data
        var currentHomeData = VillageManager.Instance.GetMiniInfoPanelDataByHomeID(home.GetHomeID());
        activeMiniInfoVillagePanel.homeInfoText.text = $"{currentHomeData.HomeName}";
        activeMiniInfoVillagePanel.homelevelText.text = $"Level {currentHomeData.HomeLevel}";
        activeMiniInfoVillagePanel.skillName.text = currentHomeData.SkillName;

        activeMiniInfoVillagePanel.skillEffectAmount.text = currentHomeData.SkillAmount;

        activeMiniInfoVillagePanel.rewardRemainTimer = currentHomeData.RewardRemainTimer;
        activeMiniInfoVillagePanel.rewardInterval = currentHomeData.RewardInterval;

        activeMiniInfoVillagePanel.RewardIcon.sprite =
            CollectableObjectService.GetCollectableObjectData(currentHomeData.rewardType).Icon;
        
        activeMiniInfoVillagePanel.collectButton.onClick.RemoveAllListeners();
        activeMiniInfoVillagePanel.collectButton.onClick.AddListener(() =>
        {
            VillageManager.Instance.HomeRewardClaimed(home.GetHomeID());
            NFInventoryManager.Instance.AddItemToInventory(currentHomeData.rewardType , currentHomeData.rewardAmount);
            activeMiniInfoVillagePanel.rewardRemainTimer = activeMiniInfoVillagePanel.rewardInterval;
            TextDamageService.Add($"+{currentHomeData.rewardAmount} {currentHomeData.rewardType}" , activeMiniInfoVillagePanel.collectButton.transform , "FarmText" );
            AnalyticsManager.Instance.ItemsCollectedEvent("Village Home" , new List<ResourceData>(){new ResourceData(ResourceDataType.Resource , currentHomeData.rewardAmount , currentHomeData.rewardType.ToString())});
        });
        
        activeMiniInfoVillagePanel.ZoomInCamera();
        
        activeMiniInfoVillagePanel.openExtraInfoPanelButton.onClick.RemoveAllListeners();
        activeMiniInfoVillagePanel.openExtraInfoPanelButton.onClick.AddListener(() =>
        {
            activeMiniInfoVillagePanel.extraInfoPanel.Load(currentHomeData.HomeName , currentHomeData.SkillName , currentHomeData.ExtraInfoText);
            activeMiniInfoVillagePanel.extraInfoPanel.gameObject.SetActive(true);
        });
    }

    private void Update()
    {
        SetTimer();
    }

    private void SetTimer()
    {
        if (rewardRemainTimer > 0)
        {
            rewardRemainTimer -= Time.deltaTime;
            rewardRemainTimeText.text = TimeDisplayUtility.GetClockTimeText(rewardRemainTimer);
            FillBarImage.fillAmount = 1f - ( rewardRemainTimer / rewardInterval);
            activeMiniInfoVillagePanel.collectButton.interactable = false;
        }
        else
        {
            activeMiniInfoVillagePanel.collectButton.interactable = true;
            rewardRemainTimeText.text = "Claimable";
            FillBarImage.fillAmount = 1f;
        }
    }

    public static void HideCanvas()
    {
        activeMiniInfoVillagePanel.gameObject.SetActive(false);
        
        activeMiniInfoVillagePanel.ZoomOutCamera();
    }

    private void SetPanelPos(Transform pos)
    {
        transform.position = pos.position + Vector3.up * 5f;
    }

    public void ZoomInCamera()
    {
        VillageManager.Instance.ZoomToHome(activeMiniInfoVillagePanel._villageHome.transform , zoomAmount , ZAmount);
        //CameraController.StarFollowViialgeHome(activeMiniInfoVillagePanel._villageHome.transform , overridedSize: zoomAmount , zOffset: ZAmount , endAction: () => {});
    }

    public void ZoomOutCamera()
    {
        VillageManager.Instance.ZoomOutToHome();
    }
}
