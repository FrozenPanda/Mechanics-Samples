using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionGradient : MonoBehaviour
{
    public Transform startPos;
    public Transform endPos;
    private float timer;
    public float slideSpeed;
    void Update()
    {
        if (timer < 1f)
            timer += Time.deltaTime * slideSpeed;
        else
        {
            timer = 0;
        }
        transform.position = Vector3.Lerp(startPos.position, endPos.position, timer);
    }
}
