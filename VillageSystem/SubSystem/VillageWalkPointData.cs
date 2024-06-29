using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VillageWalkPointData : MonoBehaviour
{
    public enum WalkPointData
    {
        Petting = 0,
        Talk = 1,
        Fishing = 2,
        Idle = 3,
        Sit = 4,
        Tree = 5,
    }

    public WalkPointData _walkPointData;

    public bool isTaken;
    
    public Transform OtherTalkPlace;
    public bool readyForTalk;
    
    public Transform PetAnimal;
    public GameObject PetParticle;
    
    

    //TextDamageService.Add($"+{currentHomeData.rewardAmount} {currentHomeData.rewardType}" , activeMiniInfoVillagePanel.collectButton.transform , "FarmText" );
}
