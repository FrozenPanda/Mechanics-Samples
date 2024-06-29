using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;

public class VillageHome : MonoBehaviour
{
    [SerializeField]private List<Transform> WalkPosses;
    [SerializeField]private bool CanSpawnManager;
    [SerializeField]private int managerSkinId;
    
    [SerializeField] private int HomeID;
    private GameObject currentHome;
    private NFVillageContructionHouse _nfVillageContructionHouse;

    private int homeLevel;

    private NFvillageClaimReadyCanvas NFvillageClaimReadyCanvas =>
        _nfvillageClaimReadyCanvas ??= GetComponentInChildren<NFvillageClaimReadyCanvas>();
    private NFvillageClaimReadyCanvas _nfvillageClaimReadyCanvas;

    private void OnEnable()
    {
        VillageManager.Instance.OnHouseUpgraded.AddListener(HomeUpgraded);
        VillageManager.Instance.OnHouseUpgraded.AddListener(CheckOpenDisableConstructionArea);
        VillageManager.Instance.OnVillageHomeRewardClaimed.AddListener(CheckClaimableReward);
    }

    private void OnDisable()
    {
        VillageManager.Instance.OnHouseUpgraded.RemoveListener(HomeUpgraded);
        VillageManager.Instance.OnHouseUpgraded.RemoveListener(CheckOpenDisableConstructionArea);
        VillageManager.Instance.OnVillageHomeRewardClaimed.RemoveListener(CheckClaimableReward);
    }

    private void Awake()
    {
        VillageManager.Instance.AddMeToHomeList(this);
        GetComponent<CheckClickedToObject>().ClickedAction = TestOpenMiniPanel;
    }

    private void Start()
    {
        //homeLevel = VillageManager.Instance.GetHomeProgressDataByID(HomeID).level;
        SpawnHomeByLevel();
        SpawnManager();
        CheckClaimableReward();
        CheckOpenDisableConstructionArea(-1);
        TutorialReleated();
    }

    private void SpawnManager()
    {
        if(!CanSpawnManager)
            return;
        
        var manager = PoolingSystem.Instance.Create<NFVillageAI>(PoolType.NFVillagerAI, transform);
        
        manager.transform.localPosition = Vector3.zero;
        manager.transform.localScale = Vector3.one;
        manager.transform.localRotation = Quaternion.identity;

        var checkClickRewardCanGive = VillageManager.Instance.CheckIfManagerCanGiveReward(HomeID);
        manager.LoadManager(WalkPosses , managerSkinId , checkClickRewardCanGive.canManagerGiveReward , checkClickRewardCanGive.rewardType , checkClickRewardCanGive.rewardAmount , HomeID);
    }

    private void HomeUpgraded(int homeID)
    {
        if (HomeID == homeID)
        {
            SpawnHomeByLevel();
            homeLevel = VillageManager.Instance.GetHomeProgressDataByID(HomeID).level;
            var villageGiftBox = GetComponent<VillageGiftBox>();
            if(homeLevel == 1 && villageGiftBox != null)
                villageGiftBox.OnHomeUnlocked();
            ParticleManager.Instance.PlayParticle(PoolType.StarUpgradeParticle, transform.position + Vector3.up *3f);
            CheckClaimableReward();

            TutorialReleated();
        }
    }

    private void SpawnHomeByLevel()
    {
        if(currentHome != null)
            Destroy(currentHome);
        homeLevel = VillageManager.Instance.GetHomeProgressDataByID(HomeID).level;
        currentHome = Instantiate(VillageManager.Instance.GetHomeDataByID(HomeID, homeLevel).gameObject , transform);
        currentHome.transform.localPosition = Vector3.zero;
        if (homeLevel == 0)
        {
            var consData = currentHome.GetComponent<NFVillageContructionHouse>();
            if (consData != null)
            {
                _nfVillageContructionHouse = consData;
                _nfVillageContructionHouse.ContinueProgress(VillageManager.Instance.GetHomeProgressDataByID(HomeID).UpgradeProgress.ToFloat() / VillageManager.Instance.GetHomeProgressDataByID(HomeID).requiredAmountForUpgrade.ToFloat());
            }
        }
    }

    public int GetHomeID()
    {
        return HomeID;
    }

    protected Tween upgradeAnim1;
    public void PlayUpgradeAnim()
    {
        upgradeAnim1.Kill();
        DOTween.Kill(currentHome);
        currentHome.transform.localScale = Vector3.one;
        if (VillageManager.Instance.GetHomeProgressDataByID(HomeID).level > 0)
        {
            upgradeAnim1 = currentHome.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f).SetEase(Ease.InOutSine);
        }
        if (VillageManager.Instance.GetHomeProgressDataByID(HomeID).level < 1 && _nfVillageContructionHouse != null)
        {
            _nfVillageContructionHouse.ContinueProgress(VillageManager.Instance.GetHomeProgressDataByID(HomeID).UpgradeProgress.ToFloat() / VillageManager.Instance.GetHomeProgressDataByID(HomeID).requiredAmountForUpgrade.ToFloat());
        }
    }

    //For showing construction area
    private void CheckOpenDisableConstructionArea(int fakeInt)
    {
        var Homedata = VillageManager.Instance.GetHomeProgressDataByID(HomeID);
        if (Homedata.level < 1 && Homedata.UpgradeProgress.ToFloat() < 1f && _nfVillageContructionHouse != null)
        {
            if (HomeID > 0 && VillageManager.Instance.GetHomeProgressDataByID(HomeID - 1).level > 0)
            {
                //enableConstructionArea
                _nfVillageContructionHouse.EnableConstructionArea();
            }else if (HomeID == 0)
            {
                //enableConstructionArea
                _nfVillageContructionHouse.EnableConstructionArea();
            }
        }
    }

    private GameObject claimReadyParticle;
    private void CheckClaimableReward()
    {
        if (VillageManager.Instance.GetHomeRewardRemainTime(HomeID) <= 0 &&
            VillageManager.Instance.GetHomeProgressDataByID(HomeID).level > 0)
        {
            var data = VillageManager.Instance.GetMiniInfoPanelDataByHomeID(HomeID);
            NFvillageClaimReadyCanvas.ShowPanel(data.rewardType);
        }
        else
            NFvillageClaimReadyCanvas.HidePanel();
    }

    [ContextMenu("Info Panel")]
    public void TestOpenMiniPanel()
    {
        if(VillageManager.Instance.GetHomeProgressDataByID(HomeID).level < 1)
            return;
        
        MiniInfoVillagePanel.ShowCanvas(this);
        
        if(!TutorialManager.Instance.IsTutorialCompleted(TutorialType.VillageClaimTutorial))
            TutorialManager.Instance.TutorialComplete(TutorialType.VillageClaimTutorial);
    }

    private void TutorialReleated()
    {
        homeLevel = VillageManager.Instance.GetHomeProgressDataByID(HomeID).level;
        if (HomeID == 0 && homeLevel > 0 && !TutorialManager.Instance.IsTutorialCompleted(TutorialType.VillageClaimTutorial) && !TutorialManager.Instance.CheckTutorialPlaying(TutorialType.VillageClaimTutorial))
        {
            TutorialManager.Instance.CheckTutorial(TutorialType.VillageClaimTutorial);
            PointToObjectArrow.PointToObject(transform.position, 7f);
        }
    }
}
