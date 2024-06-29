using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : Singleton<ParticleManager>
{
    public ParticleSystem PlayParticle(PoolType particleType, Vector3 pos, Transform parent = null)
    {
        ParticleSystem particleSystem = PoolingSystem.Instance.Create<ParticleSystem>(particleType, parent);
        particleSystem.transform.position = pos;
        var stopCaller = particleSystem.GetComponent<ParticleStopCaller>();
        if (stopCaller == null) stopCaller = particleSystem.gameObject.AddComponent<ParticleStopCaller>();
        stopCaller.particleType = particleType;

        particleSystem.Play();
        return particleSystem;
    }

    public void StopParticle(PoolType particleType, GameObject particle)
    {
        PoolingSystem.Instance.Destroy(particleType, particle);
    }
}