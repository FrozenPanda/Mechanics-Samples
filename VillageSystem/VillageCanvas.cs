using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VillageCanvas : MonoBehaviour
{
    [SerializeField] private GameObject BackButton;
    public static VillageCanvas VillageCanvasTransform;
    public Transform NotEnoughWoodTextPos;
    
    private void OnEnable()
    {
        VillageManager.Instance.OnZoomToHome.AddListener(EnableDisableCanvas);
        EnableDisableCanvas(false);
        VillageCanvasTransform = this;
    }

    private void OnDisable()
    {
        VillageManager.Instance.OnZoomToHome.RemoveListener(EnableDisableCanvas);
    }

    private void EnableDisableCanvas(bool zoomed)
    {
        if (!TutorialManager.Instance.IsTutorialCompleted(TutorialType.VillageClaimTutorial))
            zoomed = true;
        
        BackButton.SetActive(!zoomed);
    }

    public void UpgradeHouse()
    {
        VillageManager.Instance.SpawnUpgrader();
    }

    public void ReturnFarm()
    {
        VillageManager.Instance.ExitVillage();
    }

    
}
