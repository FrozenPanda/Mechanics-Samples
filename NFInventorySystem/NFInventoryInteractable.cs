using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using static Dreamteck.Splines.SplineThreading.ThreadDef;

public class NFInventoryInteractable : Interactable
{
    private readonly int ManagerID = 12;
    private CheckClickedToObject CheckClickedToObject => checkClickedToObject ??= GetComponent<CheckClickedToObject>();
    private CheckClickedToObject checkClickedToObject;

    public Transform TakeResourcePoint => takeResourcePoint;
    [SerializeField] private Transform takeResourcePoint;
    
    public Transform DropResourcePoint => dropResourcePoint;
    [SerializeField] private Transform dropResourcePoint;

    public Transform InventoryPath;
    public Transform InvestorSpawnPos;
    public Transform ManagerSpawnPos;
    
    public override InteractableType InteractableType => InteractableType.NFInventory;

    private void Awake()
    {
        CheckClickedToObject.ClickedAction = ShowSideCarUpgradePanel;
    }

    private void Start()
    {
        SpawnManager();
    }

    protected override void Initialize()
    {
        base.Initialize();
        CoroutineDispatcher.ExecuteNextFrame(() =>
        {
            if (IsExpend)
            {
                //CheckTutorial();
            }
        });
    }

    private void ShowSideCarUpgradePanel()
    {
        PanelManager.Instance.Show(PopupType.SideOrderCarUpgradePanel , new PanelData());
    }
    
    private void ShowInventoryPanel()
    {
        if (PanelManager.Instance.IsAnyPanelShowed(PopupType.GamePlayPanel))
            return;

        if (TutorialManager.Instance.IsTutorialCompleted(TutorialType.InventoryTutorial) ||
             TutorialManager.Instance.CheckTutorialPlaying(TutorialType.InventoryTutorial))
        {
            //CheckTutorialCompleted();
            PanelManager.Instance.Show(PopupType.InventoryPanel, new PanelData());
        }
    }

    protected Tween upgradeAnim1;
    private float lastParticleTime = -1f;
    private float particleDelay = 1f;
    public void UnitUpgraded()
    {
        upgradeAnim1.Kill();
        DOTween.Kill(gameObject);
        transform.localScale = Vector3.one;
        upgradeAnim1 = transform.DOPunchScale(Vector3.one * 0.1f, 0.2f).SetEase(Ease.InOutSine);
        
        if (lastParticleTime > 0f && lastParticleTime + particleDelay < Time.time)
        {
            lastParticleTime = Time.time;
            ParticleManager.Instance.PlayParticle(PoolType.UpParticle, transform.position);
                    
        }else if (lastParticleTime < 0f)
        {
                   
            lastParticleTime = Time.time;
            ParticleManager.Instance.PlayParticle(PoolType.UpParticle, transform.position);
        }
    }

    private void SpawnManager()
    {
        var manager = PoolingSystem.Instance.Create<NFManagerAI>(PoolType.NFManager, transform);
        manager.transform.position = ManagerSpawnPos.position;
        manager.transform.rotation = ManagerSpawnPos.rotation;
        manager.LoadManager(ManagerID);
    }

    /*private void CheckTutorial()
    {
        var tutorialType = TutorialType.InventoryTutorial;
        if (TutorialManager.Instance.IsTutorialCompleted(TutorialType.CollectFirstProductTutorial) && !TutorialManager.Instance.IsTutorialCompleted(tutorialType))
        {
            TutorialManager.Instance.CheckTutorial(tutorialType);
        }
    }*/

    /*private void CheckTutorialCompleted()
    {
        var tutorialType = TutorialType.InventoryTutorial;
        if (!TutorialManager.Instance.IsTutorialCompleted(tutorialType))
        {
            InventoryTutorial.CheckTutorialCompleteState();
            TutorialManager.Instance.TutorialComplete(tutorialType);
        }
    }*/
}
