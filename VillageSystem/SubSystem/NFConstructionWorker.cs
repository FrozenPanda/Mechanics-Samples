using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NFConstructionWorker : MonoBehaviour
{
    public Animator Animator;

    public string animName;

    private void OnEnable()
    {
        Animator.Play(animName);
    }

    public void Start()
    {
        Animator.Play(animName);

        StartCoroutine(ChangeAnim());
    }

    private IEnumerator ChangeAnim()
    {
        yield return new WaitForSeconds(0.2f);
        
        Animator.Play(animName);
    }
}
