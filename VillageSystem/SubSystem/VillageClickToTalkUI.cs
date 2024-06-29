using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VillageClickToTalkUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    private VillageClickToTalkEnum _toTalkEnum;
    private int talkIndex = 0;
    private string[] talks;
    private Action endAction;
    public float talkTimer = 5f;
    private float timer = 2f;
    private NFVillageAI _villageAI;
    public void StartTalk(string[] talks , NFVillageAI villageAI , Action endAction)
    {
        _villageAI = villageAI;
        this.endAction = endAction;
        this.talks = talks;
        talkIndex = 0;
        gameObject.SetActive(true);
        _toTalkEnum = VillageClickToTalkEnum.Typing;
    }
    
    private void Update()
    {
        switch (_toTalkEnum)
        {
            case VillageClickToTalkEnum.Idle:
                break;
            case VillageClickToTalkEnum.Typing:
                text.text = talks[talkIndex];
                timer = talkTimer;
                _toTalkEnum = VillageClickToTalkEnum.Waiting;
                break;
            case VillageClickToTalkEnum.Waiting:
                if (timer > 0f)
                {
                    timer -= Time.deltaTime;
                }
                else
                {
                    talkIndex++;
                    if (talkIndex <= talks.Length - 1)
                        _toTalkEnum = VillageClickToTalkEnum.Typing;
                    else
                    {
                        _toTalkEnum = VillageClickToTalkEnum.Idle;
                        //_villageAI.EndTalk();
                        endAction?.Invoke();
                        gameObject.SetActive(false);
                    }
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

enum VillageClickToTalkEnum
{
    Idle,
    Typing,
    Waiting
}
