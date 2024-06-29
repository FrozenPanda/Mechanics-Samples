using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour
{
    [SerializeField] private Image itemImage;
    [SerializeField] private TextMeshProUGUI itemCount;
    [SerializeField] private TextMeshProUGUI itemName;

    public void LoadItem(PoolType poolType, IdleNumber count)
    {
        itemImage.sprite = (poolType == PoolType.Money) ? CurrencyService.GetCurrencyItemSprite(CurrencyType.Coin) : CollectableObjectService.GetObjectIcon(poolType);
        itemCount.text = $"x{count}";
        itemName.text = (poolType == PoolType.Money) ? "Coin" : poolType.ToString();
    }
}
