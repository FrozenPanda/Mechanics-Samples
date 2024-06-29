using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class NF_SpinPanel_AnimationController : MonoBehaviour
{
    public GameObject ParticleEffect;
    public GameObject GoodLuckText;
    public GameObject RewardBG;

    public Image SpinSelectImage;

    //public Image FrameImage;
    public GameObject NormalFrameSprite;
    public GameObject WinFrameSprite;

    public GameObject EpicRewardParticle;

    public Transform RotationEffect;
    public Transform LuckyTextEffect;
    
    
    private SpinPanelAnimationTypeEnum _animationType;
    public void StartAnimation(SpinPanelAnimationTypeEnum _animationType)
    {
        this._animationType = _animationType;

        switch (_animationType)
        {
            case SpinPanelAnimationTypeEnum.Start:
                
                ParticleEffect.SetActive(false);
                GoodLuckText.SetActive(true);
                RewardBG.SetActive(false);
                SpinSelectImage.enabled = false;
                
                NormalFrameSprite.SetActive(true);
                WinFrameSprite.SetActive(false);
                
                EpicRewardParticle.SetActive(false);
                
                break;
            case SpinPanelAnimationTypeEnum.SpinStarted:
                
                ParticleEffect.SetActive(false);
                GoodLuckText.SetActive(true);
                RewardBG.SetActive(false);
                SpinSelectImage.enabled = true;

                LuckyTextEffect.DOScale(Vector3.one * 1.5f, 0.5f).SetLoops(-1, LoopType.Yoyo);
                
                break;
            case SpinPanelAnimationTypeEnum.WinNormalPrize:
                
                RewardBG.SetActive(true);
                GoodLuckText.SetActive(false);
                SpinSelectImage.enabled = false;
                ParticleEffect.SetActive(true);
                
                NormalFrameSprite.SetActive(false);
                WinFrameSprite.SetActive(true);
                
                break;
            case SpinPanelAnimationTypeEnum.WinEpicPrize:
                
                RewardBG.SetActive(true);
                GoodLuckText.SetActive(false);
                SpinSelectImage.enabled = false;
                ParticleEffect.SetActive(true);
                
                NormalFrameSprite.SetActive(false);
                WinFrameSprite.SetActive(true);
                EpicRewardParticle.SetActive(true);
                
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_animationType), _animationType, null);
        }
    }

    private Vector3 rotAngle = new Vector3(0f, 0f, 10f);
    private void Update()
    {
        RotationEffect.Rotate(rotAngle * Time.deltaTime);
    }
}

public enum SpinPanelAnimationTypeEnum
{
    Start,
    SpinStarted,
    WinNormalPrize,
    WinEpicPrize,
}