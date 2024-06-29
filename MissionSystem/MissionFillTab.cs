using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MissionFillTab : MonoBehaviour
{
    [SerializeField]private Image FillPart;
    private float timer;
    private bool smoothFillProgressStarted;
    
    public void FillTheBarInstant()
    {
        FillPart.fillAmount = 1f;
    }

    public void FillTheBarSmooth()
    {
        timer = 0f;
        smoothFillProgressStarted = true;
    }

    public void ResetBar()
    {
        FillPart.fillAmount = 0f;
    }

    private void Update()
    {
        if (smoothFillProgressStarted && timer < 1f)
        {
            timer += Time.deltaTime;
            FillPart.fillAmount = timer;
        }
    }
}
