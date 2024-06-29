using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NFVillagerClickRewardUI : MonoBehaviour
{
    [SerializeField]private TextMeshProUGUI text;
    [SerializeField]private Image iconImage;

    private float timer;
    public void InitText(string textSTR , Sprite iconSprite)
    {
        timer = 0f;
        //transform.localPosition = Vector3.zero;
        this.text.text = textSTR;
        iconImage.sprite = iconSprite;
        //this.text.transform.localPosition = new Vector3(text.transform.localPosition.x, 0f, text.transform.localPosition.z);
        //this.iconImage.transform.localPosition = Vector3.zero;
        Vector3 pos = iconImage.transform.localPosition;
        pos.y = 0f;
        iconImage.transform.localPosition = pos;
    }

    private void Update()
    {
        if (timer < 3f)
        {
            timer += Time.deltaTime;
            //text.transform.localPosition = Vector3.up * timer;
            iconImage.transform.localPosition += Vector3.up * Time.deltaTime;
        }
        else
        {
            timer = 0f;
            PoolingSystem.Instance.Destroy(PoolType.NFVillagerClickRewardUI , gameObject);
        }
    }
}
