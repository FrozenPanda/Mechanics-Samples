using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrainRewardTab : MonoBehaviour
{
    [SerializeField]private TextMeshProUGUI rewardAmountText;
    [SerializeField]private Image rewardIconImage;
    
    public void SetRewardTab(Sprite rewardIcon , int rewardAmount)
    {
        rewardIconImage.sprite = rewardIcon;
        rewardAmountText.text = rewardAmount.ToString();
    }
}
