using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnknownManager : MonoBehaviour
{
    [SerializeField] private PoolType managerOpenParticle;

    private ScaleTweenController ScaleTweenController => scaleTweenController??= GetComponent<ScaleTweenController>();
    private ScaleTweenController scaleTweenController;

    //private CheckClickedToObject CheckClickedToObject => checkClickedToObject ??= GetComponentInParent<CheckClickedToObject>();
    //private CheckClickedToObject checkClickedToObject;

    private const string SkinsPath = "Skins/";

    private GameObject unknownManagerObject;
    private NFProductContainer NFProductContainer;

    private bool loaded;

    /*private void Awake()
    {
        CheckClickedToObject.ClickedAction = ShowManagerAssignPanel;
        CheckClickedToObject.PlayHapticOnMouseUp = false;
    }*/

    public void Load(NFProductContainer nfProductContainer)
    {
        loaded = true;
        NFProductContainer = nfProductContainer;
        CheckHaveManager(true);
    }

    private void Start()
    {
        TryToEnableManager();
    }

    private void TryToEnableManager()
    {
        CheckHaveManager(false);
    }

    public void CheckHaveManager(bool isUseParticle)
    {
        if(NFProductContainer == null)
            return;
        
        //if (LevelManager.Instance.IsTutorialLevel()) return;

        if(unknownManagerObject != null)
        {
            unknownManagerObject.SetActive(false);
        }
        if (StickerManager.Instance.HasSticker(NFProductContainer.InteractionID, out Sticker sticker))
        {
            /*var path = SkinsPath + sticker.SkinId.ToString();
            var skin = Instantiate(Resources.Load<GameObject>(path), transform);
            skin.transform.localPosition = Vector3.zero;
            skin.transform.localScale = Vector3.one;
            skin.transform.localRotation = Quaternion.identity;*/
            SpawnManager(isUseParticle);
        }
        /*else
        {
            unknownManagerObject = PoolingSystem.Instance.Create(PoolType.UnknownManager, transform);
            unknownManagerObject.transform.localPosition = Vector3.zero;
            unknownManagerObject.transform.localScale = Vector3.one;
            unknownManagerObject.SetActive(true);
            ScaleTweenController.ScaleTween(transform, true);
        }*/
    }

    /*private void ShowManagerAssignPanel()
    {
        if (!TutorialManager.Instance.IsTutorialCompleted(TutorialType.FirstStarUpgradeTutorial) || PanelManager.Instance.IsAnyPanelShowed(PopupType.GamePlayPanel)) return;

        PanelManager.Instance.Show(PopupType.ManagerAssignPanel, new ManagerAssignPanelData(NFProductContainer, ()=> CheckHaveManager(true)));
    }*/

    private bool spawnedBefore;
    public void SpawnManager(bool isUseParticle)
    {
        if(spawnedBefore)
            return;
        
        spawnedBefore = true;

        var manager = PoolingSystem.Instance.Create<NFManagerAI>(PoolType.NFManager, transform);
        manager.transform.localPosition = Vector3.zero;
        manager.transform.localScale = Vector3.one;
        manager.transform.localRotation = Quaternion.identity;

        manager.LoadManager(NFProductContainer);

        if (isUseParticle) PlayManagerParticle();
    }

    private void PlayManagerParticle()
    {
        var particle = ParticleManager.Instance.PlayParticle(managerOpenParticle, Vector3.zero, transform);
        particle.transform.localPosition = Vector3.zero;
        particle.transform.localScale = Vector3.one;
    }
}
