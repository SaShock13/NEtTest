using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Ball : NetworkBehaviour
{
    [SerializeField] private int damage = 20;
    [SerializeField] private float lifeTime = 5f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            rb.isKinematic = false;
            Invoke(nameof(DespawnSelf), lifeTime);
        }
        else
        {
            rb.isKinematic = true;
        }
    }

    private void DespawnSelf()
    {
        if (NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;


        var health = other.GetComponentInParent<PlayerHealth>();
        if(health != null)   
        {
            Debug.Log($"PlayerHealth found {this}");
            if (health.OwnerClientId == OwnerClientId) return;

            health.TakeDamage(damage);

            GotHitPlayerClientRpc(health.OwnerClientId);

            NetworkObject.Despawn(true);
        }
    }

    [ClientRpc]
    private void GotHitPlayerClientRpc(ulong playerId)
    {
        Debug.Log($"Player {playerId} got hit by ball");
    }
}
