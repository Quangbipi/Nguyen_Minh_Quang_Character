using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class GoalVfxTrigger : MonoBehaviour
{
    [SerializeField] private ParticlePool particlePool;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private LayerMask ballLayerMask;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (particlePool == null || spawnPoint == null)
        {
            return;
        }

        int otherLayer = 1 << other.gameObject.layer;
        if ((ballLayerMask.value & otherLayer) == 0)
        {
            return;
        }

        particlePool.Play(spawnPoint.position, spawnPoint.rotation);
    }
}
