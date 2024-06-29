using System;
using System.Collections;
using System.Collections.Generic;
using ExternalPropertyAttributes;
using UnityEngine;

public class NFVillageContructionHouse : MonoBehaviour
{
    public List<ConstructionOpeningData> ConstructionOpeningDatas = new List<ConstructionOpeningData>();
    private int contstuctionIndex = 0;
    private int lastIndex = -5;
    private GameObject currentGameObject;
    
    public void ContinueProgress(float percent)
    {
        for (int i = 0; i < ConstructionOpeningDatas.Count; i++)
        {
            if (percent > ConstructionOpeningDatas[i].percentAmount)
            {
                contstuctionIndex = i;
            }
        }
        
        if(lastIndex == contstuctionIndex)
            return;

        lastIndex = contstuctionIndex;

        if (currentGameObject != null)
        {
            currentGameObject.SetActive(false);
            currentGameObject = ConstructionOpeningDatas[contstuctionIndex].building;
            currentGameObject.SetActive(true);
        }
        else
        {
            currentGameObject = ConstructionOpeningDatas[contstuctionIndex].building;
            currentGameObject.SetActive(true);
        }
    }

    public void EnableConstructionArea()
    {
        currentGameObject.SetActive(false);
        currentGameObject = ConstructionOpeningDatas[1].building;
        currentGameObject.SetActive(true);
    }
}

[Serializable]
public class ConstructionOpeningData
{
    [MinMaxSlider(0, 100)] public float percentAmount;
    public GameObject building;
}
