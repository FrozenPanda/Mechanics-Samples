using System;
using System.Collections;
using System.Collections.Generic;
using _Game.Scripts.Systems.WeeklyEventSystem;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MissionPanel : MonoBehaviour
{
    public List<MissionTab> MissionTabs;
    public List<Transform> MissionTabPlaces;
    [SerializeField]private Transform MissionInitiatePlace;
    private List<MissionTab> AvailableTabs = new List<MissionTab>();
    private int totalMissionCountSameTime;
    private int currentActiveMissions = 0;
    
    public MissionFillPart MissionFillPart;
    //Fill Part
    public GameObject DynamicPart;
    public Transform DynamicTabParent;
    public Transform ActiveTabParent;
    [FormerlySerializedAs("ShowRenovatePanelButton")] public GameObject ShowClaimRewardInfoButton;
    public GameObject ShowRenovatePanelButton;
    
    public bool isDynamicPlaceNotAvailable { private set; get; }

    public GameObject RenovateButton;
    public GameObject CannotRenovateButton;
    public GameObject ClaimButton;
    public Image ClaimButtonChestSprite;
    
    private bool isFirstQuest = true;

    public Image LevelEndRewardChestIcon;
    
    
    //public GameObject ClaimRewardButton;
    private void Awake()
    {
        //totalMissionCountSameTime = MissionManager.Instance.GetTotalMissionCountAtSameTime();
        //SetMissionPlaces();
    }

    private void Start()
    {
        RefreshPanel();
    }

    private void RefreshPanel()
    {
        LevelEndRewardChestIcon.sprite = MissionManager.Instance.GetLeveledChestSprite();
        ClaimButtonChestSprite.sprite = MissionManager.Instance.GetLeveledChestSprite();
        SetRenovateClaimPart();
        totalMissionCountSameTime = MissionManager.Instance.GetTotalMissionCountAtSameTime();
        //SetMissionPlaces();
        CloseAllTabs();
        GetNewMission(); 
        if(cheatEnabledBool)
            return;
        MissionFillPart.SetFillBar(LevelManager.Instance.ActiveLevelId , MissionManager.Instance.GetTotalMissionCount() , MissionManager.Instance.GetTotalCompletedMissionsCount());
    }

    private void SetMissionPlaces()
    {
        var remainingMission = MissionManager.Instance.GetRemainingMissionCountAtCurrentLevel();
        int tabPlacesCount =
            remainingMission <= totalMissionCountSameTime ? remainingMission : totalMissionCountSameTime;
        foreach (var tab in MissionTabPlaces)
        {
            tab.gameObject.SetActive(false);
        }

        for (int i = 0; i < tabPlacesCount; i++)
        {
            MissionTabPlaces[i].gameObject.SetActive(true);
        }
    }

    private void OnEnable()
    {
        MissionManager.Instance.OnMissionCompleted.AddListener(GetNewMission);
        MissionManager.Instance.OnMissionsRefreshed.AddListener(RefreshPanel);
        MissionManager.Instance.OnGeneratorFocused.AddListener(CheckMissionCanShow);
        MissionManager.Instance.OnMissionCheatEnabled?.AddListener(CheatEnabled);
        MissionManager.Instance.OnMissionCheatEnabledCurrentLevel.AddListener(EnableCheatCurrentLevel);
        IdleExchangeService.OnDoExchange[CurrencyService.ActiveCurrencyType].AddListener(ChangeRenovateButtonActive);
    }

    private void OnDisable()
    {
        if (MissionManager.IsAvailable())
        {
            MissionManager.Instance.OnMissionCompleted.RemoveListener(GetNewMission);
            MissionManager.Instance.OnMissionsRefreshed.RemoveListener(RefreshPanel);
            MissionManager.Instance.OnGeneratorFocused.RemoveListener(CheckMissionCanShow);
            MissionManager.Instance.OnMissionCheatEnabled?.RemoveListener(CheatEnabled);
            MissionManager.Instance.OnMissionCheatEnabledCurrentLevel.RemoveListener(EnableCheatCurrentLevel);
        }
    }

    private void GetNewMission()
    {
        SetRenovateClaimPart();
        if(cheatEnabledBool)
            return;
        //GetMissionItem();
        StartCoroutine(GetNewMissionWithDelay());
    }

    //bunun biraz delaylı olma sebebi mission yerleri sürekli yer değiştiriyor ve bir frame sonrasını veriyoruz çünkü
    //grid layout açık olan mission placeleri ilk baş yerlerine oturtuyor
    private IEnumerator GetNewMissionWithDelay()
    {
        SetMissionPlaces();
        yield return new WaitForEndOfFrame();
        MoveMissions();
        GetMissionItem();
    }

    private void MoveMissions()
    {
        foreach (var tab in MissionTabs)
        {
            tab.MoveNewPlace(isFirstQuest);
            isFirstQuest = false;
        }
    }

    [ContextMenu("Get Mission Item")]
    public void GetMissionItem()
    {
        GetAvailableTabs();
        
        if(MissionManager.Instance.IsAllQuestCompleted())
            return;
        
        for (int i = 0; i < AvailableTabs.Count && i < totalMissionCountSameTime ; i++)
        {
            if(currentActiveMissions >= totalMissionCountSameTime)
                break;
            
            BaseMission currentMission = MissionManager.Instance.GetMissionItem(out bool anyMissionAvailable);
            if(!anyMissionAvailable)
                break;

            AvailableTabs[i].transform.position = MissionInitiatePlace.position;
            AvailableTabs[i].InitiliazeMissionTab(currentMission , currentActiveMissions);
            currentActiveMissions++;
        }
    }

    private void CloseAllTabs()
    {
        currentActiveMissions = 0;
        foreach (var tab in MissionTabs)
        {
            tab.gameObject.SetActive(false);
            tab.isAvailable = true;
            tab.RewardPart.SetActive(false);
        }
    }

    private void GetAvailableTabs()
    {
        AvailableTabs.Clear();

        foreach (var tab in MissionTabs)
        {
            if(tab.isAvailable)
                AvailableTabs.Add(tab);
        }
    }

    //if panel is disabled but focused generator releated with this mission
    public void CheckMissionCanShow(PoolType inProduct , PoolType outProduct , string generatorId)
    {
        foreach (var tab in MissionTabs)
        {
            tab.CheckMoveToNewParent(ActiveTabParent , inProduct , outProduct , generatorId);
        }
    }

    public void FakeEnableDisable(bool enable)
    {
        if(enable)
            EnableItself();
        else
            HideItself();
    }
    
    public void HideItself()
    {
        DynamicPart.SetActive(false);
        ShowClaimRewardInfoButton.gameObject.SetActive(false);
        ShowRenovatePanelButton.gameObject.SetActive(false);
        MissionFillPart.gameObject.transform.localScale = Vector3.zero;
        isDynamicPlaceNotAvailable = false;
    }

    public void EnableItself()
    {
        if(ConversationManager.Instance.KeepMaskPanel)
            return;
        
        isDynamicPlaceNotAvailable = true;
        
        MissionFillPart.gameObject.transform.localScale = Vector3.one;
        
        DynamicPart.SetActive(true);
        ShowClaimRewardInfoButton.gameObject.SetActive(true);
        ShowRenovatePanelButton.gameObject.SetActive(true);
        
        foreach (var tab in MissionTabs)
        {
            tab.CheckMoveToRealParent(DynamicTabParent);
        }
    }

    public void OneMissionCompleted(int placeIndex)
    {
        currentActiveMissions--;
        foreach (var tab in MissionTabs)
        {
            tab.OneMissionCompletedReorderIndex(placeIndex);
        }
    }

    public Transform GetMissionPlaceByIndex(int index)
    {
        return MissionTabPlaces[index];
    }

    public void OpenRandomMissionInfo()
    {
        AvailableTabs[0].ShowInfoPanelAction();
    }

    private bool cheatEnabledBool;
    private void CheatEnabled()
    {
        
        cheatEnabledBool = true;
        CloseAllTabs();
        MissionFillPart.SetFillBar(LevelManager.Instance.ActiveLevelId , MissionManager.Instance.GetTotalMissionCount() , MissionManager.Instance.GetTotalMissionCount());
    }

    private void SetRenovateClaimPart() 
    {
        if (MissionManager.Instance.IsAllQuestCompleted())
        {
            if (MissionManager.Instance.IsLeveledRewardTaken())
            {
                //RenovateButton.SetActive(true);
                //ClaimButton.SetActive(false);
                //OpenRenovatepanel();
                if (LevelManager.Instance.IsCityLastLevel())
                {
                    RenovateButton.SetActive(true);
                    ClaimButton.SetActive(false);
                }
                else
                {
                   // LevelManager.Instance.ExpendLevel();
                }
            }
            else
            {
                RenovateButton.SetActive(false);
                ClaimButton.SetActive(true);
                if(BotManager.IsAvailable())
                    BotManager.Instance.OnLeveledMissionRewardCanCollect?.Invoke();
            }
            
            ChangeRenovateButtonActive();
        }
        else
        {
            RenovateButton.SetActive(false);
            ClaimButton.SetActive(false);
            CannotRenovateButton.SetActive(false);
        }
    }

    public void OpenRenovatepanel()
    {
        PanelManager.Instance.Show(PopupType.RenovatePanel, new PanelData());
        return;
    }

    public void OpenClaimPanel()
    {
        MissionManager.Instance.OpenMissionLeveledRewardPanel();
    }
    
    public void OpenMissionLeveledRewardPanel()
    {
        MissionManager.Instance.OpenMissionLeveledRewardPanel();
    }

    private void ChangeRenovateButtonActive(IdleNumber v = null, IdleNumber a = null)
    {
        if(!MissionManager.Instance.IsLeveledRewardTaken())
            return;
        
        var currentCoin = IdleExchangeService.GetIdleValue(CurrencyService.ActiveCurrencyType);
        var requiredCoin = LevelManager.Instance.GetLevelEndCost();

        //allMaxed &= currentCoin >= requiredCoin;

        var enoughCoin = currentCoin >= requiredCoin;

        if (TutorialManager.Instance.CheckTutorialPlaying(TutorialType.ExpendTutorial) || TutorialManager.Instance.IsTutorialCompleted(TutorialType.ExpendTutorial))
        {
            RenovateButton.SetActive(enoughCoin);
            CannotRenovateButton.SetActive(!enoughCoin);
        }
        else
        {
            CannotRenovateButton.SetActive(false);
            RenovateButton.SetActive(false);
        }
        
        if(BotManager.IsAvailable() && enoughCoin && LevelManager.Instance.IsCityLastLevel())
            BotManager.Instance.OnRenovateButtonEnable?.Invoke("Renovate" , () =>
            {
                IdleNumber requiredCoin = LevelManager.Instance.GetLevelEndCost();

                bool inEvent = EventManager.Instance.InEvent;
                IdleExchangeService.DoExchange(CurrencyService.GetCoinType(inEvent), -requiredCoin, out var value, "ExpendLevel");

                LevelManager.Instance.ExpendLevel(); 
            });
        
    }

    private void EnableCheatCurrentLevel()
    {
        currentActiveMissions = 0;
        foreach (var tab in MissionTabs)
        {
            tab.gameObject.SetActive(false);
            tab.isAvailable = true;
            tab.RewardPart.SetActive(false);
            tab.currentMission = null;
        }
    }
}
