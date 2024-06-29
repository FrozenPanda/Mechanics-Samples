using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NFvillageClaimReadyCanvas : MonoBehaviour
{
    [SerializeField] private Image rewardImage;

    public void ShowPanel(PoolType reward)
    {
        gameObject.SetActive(true);
        Sprite asd = CollectableObjectService.GetCollectableObjectData(reward).Icon;
        rewardImage.sprite = asd;
    }

    public void HidePanel()
    {
        gameObject.SetActive(false);
    }
}
