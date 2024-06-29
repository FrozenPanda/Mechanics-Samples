using System.Collections;
using System.Collections.Generic;
using PandaLibraryClasses;
using UnityEngine;

public class Jump : MonoBehaviour
{
    public Transform topPart;
    public Transform ClaimPart;
    private Vector3 topPartStartPos;
    private Vector3 claimPartStartPos;
    
    [ContextMenu("StartAnim")]
    public void StartAnimation()
    {
        claimPartStartPos = ClaimPart.position;
        topPartStartPos = topPart.position;
        ClaimPart.position += Vector3.up * 10f;
        PandaLibraryClasses.PandaLibrary.PandaMove(topPart , topPart.transform.position + Vector3.up * 10f , 0.5f , 
            () => PandaLibrary.PandaCountDown(0.8f , () => PandaLibrary.PandaMove(topPart , topPartStartPos , 0.2f , 
                () => PandaLibrary.PandaMove(ClaimPart , claimPartStartPos , 0.4f))));
    }
}
