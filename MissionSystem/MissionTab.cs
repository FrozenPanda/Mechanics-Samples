using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MissionTab : MonoBehaviour
{
    public MissionPanel MissionPanel;
    
    public BaseMission currentMission;
    public Image MissionImage;
    public Image FillBar;
    public TextMeshProUGUI AimText;
    public TextMeshProUGUI AmountText;
    
    public GameObject RewardPart;
    public Button ClaimButton;
    public Button ShowInfoPanelButton;
    public bool isAvailable = true;
    public Image claimRewardImage;
    public TextMeshProUGUI claimText;

    public int siblingIndex;

    public GameObject DynamicPart;

    private Animator Animator;

    //For mission info panel
    public string PanelMissionInfo {get; private set;}
    public Sprite PanelMissionSprite{get; private set;}
    public IdleNumber PanelWantedAmount {get; private set;}
    public IdleNumber PanelCurrentAmount {get; private set;}
    public float PanelFillBarAmount{get; private set;}
    public PackageContent PanelShopPackage{get; private set;}

    private bool MovedToAnotherParent;
    private Transform realParent;

    private int missionPlaceIndex;

    private bool claimShowed = false;
    
    //istisnai bir bug için bazen claim part gözükmüyor androidde
    private float secondCheckForClaimTimer = 2f;
    private bool secondCheckBool;
    
    private void Awake()
    {
        realParent = transform.parent;
        siblingIndex = transform.GetSiblingIndex();
        Animator = GetComponent<Animator>();
    }

    public void InitiliazeMissionTab(BaseMission mission , int missionPlaceIndex = 0)
    {
        claimShowed = false;
        currentMission = mission;
        gameObject.SetActive(true);
        this.missionPlaceIndex = missionPlaceIndex;
        RewardPart.SetActive(false);
        mission.SetMissionTab(this);
        mission.InitilizeMission();
        SetData();
        SetInfoButton();
        SetClaimButton();
        isAvailable = false;
        MoveNewPlace();

        //secondCheckBool = false;
        secondCheckForClaimTimer = 2f;
    }

    public void OneMissionCompletedReorderIndex(int completedMissionIndex)
    {
        if(isAvailable)
            return;
        
        if (completedMissionIndex < missionPlaceIndex)
        {
            missionPlaceIndex--;
        }
    }

    private Vector3 startPos;
    private Vector3 endPos;
    private float MoveTimer;
    public void MoveNewPlace(bool fastForward = false)
    {
        if(isAvailable)
            return;

        startPos = transform.position;
        endPos = MissionPanel.GetMissionPlaceByIndex(missionPlaceIndex).position;

        if (fastForward)
        {
            MoveTimer = 1f;
            transform.position = endPos;
        }
        else
        {
            MoveTimer = 0f;
        }
    }

    private void Update()
    {
        Move();
        SecondCheckForBug();
    }

    private void Move()
    {
        if (MoveTimer < 1f)
        {
            MoveTimer += Time.deltaTime * 2f;
            transform.position = Vector3.Lerp(startPos , endPos , MoveTimer);
        }
        else
        {
            /*if (insideOfRealParent && Vector3.Distance(transform.position, endPos) > 0.01f)
                transform.position = Vector3.MoveTowards(transform.position, endPos, 50f * Time.deltaTime);*/
            return;
        }

        if (!MissionPanel.isDynamicPlaceNotAvailable)
        {
            transform.position = endPos;
            MoveTimer = 1f;
        }
    }

    private void SecondCheckForBug()
    {
        if(secondCheckBool)
            if (secondCheckForClaimTimer >= 0f)
                secondCheckForClaimTimer -= Time.deltaTime;

        if (secondCheckBool && secondCheckForClaimTimer < 0f)
        {
            RewardPart.SetActive(true);
            //Debug.Log("<<<<<<<<");
        }
    }

    private void SetData()
    {
        PanelMissionSprite = currentMission.missionSprite;
        PanelShopPackage = currentMission.Reward;
    }

    private void SetInfoButton()
    {
        ShowInfoPanelButton.onClick.RemoveAllListeners();
        ShowInfoPanelButton.onClick.AddListener(ShowInfoPanelAction);
    }

    private void SetClaimButton()
    {
        ClaimButton.onClick.RemoveAllListeners();
        ClaimButton.onClick.AddListener(ClaimFromInfoPanel);
    }

    public void ShowInfoPanelAction()
    {
        if (currentMission.UniqueId == 1)
        {
            /*if (TutorialManager.Instance.CheckTutorialPlaying(TutorialType.MissionTutorial))
            {
                //MissionTutorial.CheckTutorialCompleteState();
                TutorialFinger.StopFingerMove();
            }*/
        }
        
        if(TutorialManager.Instance.CheckTutorialPlaying(TutorialType.MarketAutomateTutorial))
            if(currentMission.UniqueId != 200)
                return;
        
        TutorialManager.Instance.IsMaskEnabledForClaim = false;
        if (PanelManager.Instance.TutorialMaskPanel != null)
        {
            if (TutorialManager.Instance.IsTutorialPlaying && (TutorialManager.Instance.CheckTutorialPlaying(TutorialType.MissionTutorialClaim)))
            {
                PanelManager.Instance.TutorialMaskPanel.HidePanel();
            }
            else
            {
            }
        }
        

        PanelManager.Instance.Show(PopupType.MissionInfoPanel , new MissionInfoPanelData(this));
    }

    public void SetUItab(Sprite missionSprite , IdleNumber wantedAmount , IdleNumber currentAmount, string aimText)
    {
        AimText.text = aimText;
        MissionImage.sprite = missionSprite;
        AmountText.text = currentAmount.ToString();

        PanelWantedAmount = wantedAmount;
        PanelCurrentAmount = currentAmount;
        PanelMissionInfo = aimText;
    }

    public void UpdateFillbar(float fillAmount)
    {
        FillBar.fillAmount = fillAmount;
        PanelFillBarAmount = fillAmount;

        if (fillAmount < 1)
        {
            RewardPart.SetActive(false);
            secondCheckBool = false;
            secondCheckForClaimTimer = 2f;
        }

        if (fillAmount >= 1f)
        {
            secondCheckBool = true;
        }
    }

    public void UpdateText(IdleNumber updatedInfo)
    {
        AmountText.text = updatedInfo.ToString();
        PanelCurrentAmount = updatedInfo;
    }

    public void ShowClaim()
    {
        //for cheat
        if(MissionManager.Instance.IsAllQuestCompleted())
            return;
        
        if(!TutorialManager.Instance.IsTutorialCompleted(TutorialType.MissionTutorialClaim))
            TutorialManager.Instance.CheckTutorialWithDelay(TutorialType.MissionTutorialClaim , 0.5f);
        //ShowClaimButton
        if(claimShowed)
            return;
        claimShowed = true;
        
        RewardPart.SetActive(true);
        var range = Random.Range(1, 3);
        if(range<2)
            Animator.SetTrigger("First");
        else
            Animator.SetTrigger("Second");
        //GetComponent<Animator>().Play("A" +Random.Range(1,3));
        UpdateClaimPart(currentMission.Reward);
        
        MissionManager.Instance.OnMissionCanClaim?.Invoke(this);
        /*ClaimButton.onClick.RemoveAllListeners();
        ClaimButton.onClick.AddListener(() =>
        {
            Claimed();
            currentMission.CompleteAction();
        });*/
    }

    public void Claimed()
    {
        //closeTab
        isAvailable = true;
        gameObject.SetActive(false);
        RewardPart.SetActive(false);
        
        if(!TutorialManager.Instance.IsTutorialCompleted(TutorialType.MissionTutorialClaim))
            TutorialManager.Instance.TutorialComplete(TutorialType.MissionTutorialClaim);
    }

    public void ClaimFromInfoPanel()
    {
        if(TutorialManager.Instance.CheckTutorialPlaying(TutorialType.OrderPanelIntroduceTutorial))
            return;
        
        if(TutorialManager.Instance.CheckTutorialPlaying(TutorialType.ClaimOrderCarRewardTutorial) || TutorialManager.Instance.CheckTutorialPlaying(TutorialType.ShopTutorial) || TutorialManager.Instance.CheckTutorialPlaying(TutorialType.VillageEnterTutorial))
            return;
        if (PanelManager.Instance.TutorialMaskPanel != null)
        {
            PanelManager.Instance.TutorialMaskPanel.HidePanel();
        }

        if (!TutorialManager.Instance.CheckTutorialPlaying(TutorialType.NotAutomateMarketTutorial))
        {
            TutorialFinger.StopFingerMove();
        }
        //TutorialManager.Instance.IsMaskEnabledForClaim = false;
        
        //TutorialManager.Instance.CheckTutorial(TutorialType.MissionTutorial);
        if (!TutorialManager.Instance.IsTutorialCompleted(TutorialType.MissionTutorialClaim))
        {
            TutorialManager.Instance.TutorialComplete(TutorialType.MissionTutorialClaim);
            TutorialFinger.StopFingerMove();
        }

        if (TutorialManager.Instance.CheckTutorialPlaying(TutorialType.CustomerOrderTutorial))
        {
            TutorialManager.Instance.TutorialComplete(TutorialType.CustomerOrderTutorial);
            TutorialFinger.StopFingerMove();
        }

        if (currentMission.UniqueId == 2)
        {
            //TutorialManager.Instance.CheckTutorialWithDelay(TutorialType.CustomerOrderTutorial , 0.1f);            
        }
        
        MissionManager.Instance.LastCompletedMissionPoint = transform.position;
        MissionPanel.OneMissionCompleted(missionPlaceIndex);
        Claimed();
        currentMission.CompleteAction();
        claimShowed = false;
        
        //for bug
        secondCheckBool = false;
        secondCheckForClaimTimer = 2f;

        TutorialManager.Instance.IsMaskEnabledForClaim = false;
    }
    
    private void UpdateClaimPart(PackageContent reward)
    {
        if (reward.HasCurrencyInPackage(PackageMod.Mod1))
        {
            var rewardCurrencies = reward.GetCurrencies(PackageMod.Mod1);
            var targetCurrencyByType = rewardCurrencies[0];

            claimRewardImage.sprite = CurrencyService.GetCurrencyItemSprite(rewardCurrencies[0].CurrencyType);
            var targetCurrency = targetCurrencyByType.Price;
            claimText.text = targetCurrency.ToString();

            return;
        }
        if (reward.HasChestInPackage(PackageMod.Mod1))
        {
            var rewardChests = reward.GetChests(PackageMod.Mod1);
            var targetChest = rewardChests[0];

            claimRewardImage.sprite = ChestManager.Instance.GetChestTypeDataById(targetChest.ChestID).ChestPanelImage;
            claimText.text = targetChest.ChestCount.ToString();

            return;
        }

        if (reward.HasProductInPackage(PackageMod.Mod1))
        {
            var rewardProducts = reward.GetProducts(PackageMod.Mod1);
            var targetProduct = rewardProducts[0];

            claimRewardImage.sprite = CollectableObjectService.GetObjectIcon(targetProduct.PoolType);
            claimText.text = targetProduct.Count.ToString();

            return;
        }
    }

    //Eğer generator a tılanıp kamera yaklaştığında aktif olan missionlar o generator ile alakalı ise ( collect product , level up generator ...)
    //Burası o missionları aktif yerlere taşıyorlar. 
    private Vector3 realPos;
    private bool insideOfRealParent = true;
    public void CheckMoveToNewParent(Transform newParent , PoolType generatorIn  , PoolType generatorOut , string generatorId)
    {
        if(MoveTimer < 1f)
            return;
        
        if(isAvailable)
            return;
        
        if(MovedToAnotherParent)
            return;
        
        /*if(MissionPanel.isDynamicPlaceNotAvailable)
            return;*/

        realPos = transform.position;

        insideOfRealParent = false;

        if (currentMission.MissionType == MissionType.CollectProduct)
        {
            if (currentMission.CollectProductType == generatorIn)
            {
                MovedToAnotherParent = true;
                transform.SetParent(newParent);
                return;
            }
        }

        if (currentMission.MissionType == MissionType.SpendProduct)
        {
            if (currentMission.SpendProdcutType == generatorOut)
            {
                MovedToAnotherParent = true;
                transform.SetParent(newParent);
                return;
            }
        }

        if (currentMission.MissionType == MissionType.LevelUpGenerator)
        {
            if (currentMission.LevelUpgeneratorId == generatorId)
            {
                MovedToAnotherParent = true;
                transform.SetParent(newParent);
                return;
            }
        }
        
        CheckMoveToRealParent(realParent);
    }

    public void CheckMoveToRealParent(Transform realParent)
    {
        if(!MovedToAnotherParent)
            return;
        
        transform.SetParent(realParent);
        transform.SetSiblingIndex(siblingIndex);
        MovedToAnotherParent = false;
        transform.position = realPos;
        insideOfRealParent = true;
    }
}
