using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleStopCaller : MonoBehaviour
{
    public PoolType particleType;
    private bool isStopped;
    
    void Start()
    {
        var main = GetComponent<ParticleSystem>().main;
        main.stopAction = ParticleSystemStopAction.Callback;
    }

    private void OnEnable()
    {
        isStopped = false;
    }

    private void OnDisable()
    {
        if (isStopped) return;
        isStopped = true;
        Invoke( nameof(OnParticleSystemStopped), .01f ); 
    }
    
    public void OnParticleSystemStopped()
    {
        isStopped = true;
        ParticleManager.Instance.StopParticle(particleType, this.gameObject);
    }

}