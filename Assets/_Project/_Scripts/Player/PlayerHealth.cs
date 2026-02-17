
using Unity.Netcode;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    public NetworkVariable<int> Hp = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public void TakeDamage(int damage)
    {
        if (!IsServer) return;
        /// damage получает только плеер 1Ё!!!

        if (Hp.Value <= 0) return;

        Hp.Value = Mathf.Max(0, Hp.Value - damage);

        Debug.Log($"Hp.Value {Hp.Value}");

        if (Hp.Value == 0)
        {
            Death();
        }
    }

    private void Death()
    {
        // Пока просто лог
        Debug.Log($"Player {OwnerClientId} died");
        NetworkObject.Despawn(true);
    }
}
