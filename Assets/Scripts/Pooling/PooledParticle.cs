using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public sealed class PooledParticle : MonoBehaviour
{
    private ParticlePool owner;
    private ParticleSystem rootParticleSystem;
    private bool isPlaying;

    private void Awake()
    {
        rootParticleSystem = GetComponent<ParticleSystem>();
    }

    private void Update()
    {
        if (isPlaying && !rootParticleSystem.IsAlive(true))
        {
            isPlaying = false;
            owner.Release(this);
        }
    }

    internal void Initialize(ParticlePool particlePool)
    {
        owner = particlePool;

        if (rootParticleSystem == null)
        {
            rootParticleSystem = GetComponent<ParticleSystem>();
        }
    }

    internal void Play(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        gameObject.SetActive(true);
        isPlaying = true;
        rootParticleSystem.Play(true);
    }

    internal void ResetForPool(Transform poolTransform)
    {
        isPlaying = false;
        rootParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        transform.SetParent(poolTransform, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        gameObject.SetActive(false);
    }
}
