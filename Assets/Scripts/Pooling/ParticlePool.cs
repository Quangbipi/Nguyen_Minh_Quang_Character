using System.Collections.Generic;
using UnityEngine;

public sealed class ParticlePool : MonoBehaviour
{
    [SerializeField] private PooledParticle particlePrefab;
    [SerializeField, Min(0)] private int prewarmCount = 4;

    private readonly Queue<PooledParticle> availableParticles = new Queue<PooledParticle>();

    private void Awake()
    {
        if (particlePrefab == null)
        {
            Debug.LogError("ParticlePool requires a PooledParticle prefab.", this);
            return;
        }

        for (int i = 0; i < prewarmCount; i++)
        {
            availableParticles.Enqueue(CreateParticle());
        }
    }

    public void Play(Vector3 position, Quaternion rotation)
    {
        if (particlePrefab == null)
        {
            return;
        }

        PooledParticle particle = availableParticles.Count > 0
            ? availableParticles.Dequeue()
            : CreateParticle();

        particle.transform.SetParent(null, true);
        particle.Play(position, rotation);
    }

    internal void Release(PooledParticle particle)
    {
        if (particle == null || !particle.gameObject.activeSelf)
        {
            return;
        }

        particle.ResetForPool(transform);
        availableParticles.Enqueue(particle);
    }

    private PooledParticle CreateParticle()
    {
        PooledParticle particle = Instantiate(particlePrefab, transform);
        particle.Initialize(this);
        particle.ResetForPool(transform);
        return particle;
    }
}
