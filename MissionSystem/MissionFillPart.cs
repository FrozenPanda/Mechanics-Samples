using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionFillPart : MonoBehaviour
{
    [SerializeField]private List<MissionFillTab> Tabs = new List<MissionFillTab>();
    [SerializeField] private TextMeshProUGUI CurrentLevelText;
    [SerializeField] private TextMeshProUGUI NextLevelText;
    private int totalCompletedMissions;
    private int totalMission;

    [SerializeField] private Image gradientMask;
    [SerializeField] private GameObject gradientEffect;

    private void OnEnable()
    {
        MissionManager.Instance.OnMissionCompleted.AddListener(IncreaseFillBar);
    }

    private void OnDisable()
    {
        MissionManager.Instance.OnMissionCompleted.RemoveListener(IncreaseFillBar);
    }

    public void SetFillBar(int currentLevel, int totalMission, int totalCompletedOne)
    {
        CloseAllTabs();
        
        CurrentLevelText.text = (currentLevel + 1).ToString();
        NextLevelText.text = (currentLevel + 2).ToString();

        this.totalMission = totalMission;
        totalCompletedMissions = totalCompletedOne;
        
        if (totalMission > Tabs.Count) 
        {
            for (int i = 0; i < totalMission - Tabs.Count; i++)
            {
                GameObject tab = Instantiate(Tabs[i].gameObject, Tabs[i].transform.parent);
                Tabs.Add(tab.GetComponent<MissionFillTab>());
            }
        }
        
        for (int i = 0; i < totalMission; i++)
        {
            Tabs[i].gameObject.SetActive(true);
            if(i < totalCompletedOne)
                Tabs[i].FillTheBarInstant();
        }
        
        CheckGradientEffect();
    }

    public void IncreaseFillBar()
    {
        Tabs[totalCompletedMissions].FillTheBarSmooth();
        totalCompletedMissions++;
        CheckGradientEffect();
    }

    private void CheckGradientEffect()
    {
        if (totalMission > 0)
        {
            gradientMask.fillAmount = ((float)totalCompletedMissions / totalMission);
            gradientEffect.SetActive(true);
        }
        return;
        if(totalCompletedMissions >= totalMission)
            gradientEffect.SetActive(true);
        else
            gradientEffect.SetActive(false);
    }

    private void CloseAllTabs()
    {
        foreach (var tab in Tabs)
        {
            tab.gameObject.SetActive(false);
            tab.ResetBar();
        }
    }
}
