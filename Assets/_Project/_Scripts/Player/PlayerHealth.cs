
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterController controller;
    [SerializeField] private PlayerMovement movement;    

    private NetworkVariable<bool> IsDead = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> Hp = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (controller == null) controller = GetComponent<CharacterController>();
        if (movement == null) movement = GetComponent<PlayerMovement>();
    }

    public void TakeDamage(int damage)
    {
        if (!IsServer) return;
        if (IsDead.Value) return;
        if (Hp.Value <= 0) return;



        Hp.Value = Mathf.Max(0, Hp.Value - damage);

        Debug.Log($"Hp.Value {Hp.Value}");

        if (Hp.Value == 0)
        {
            IsDead.Value = true;

            // Анимация смерти всем + отключение логики на сервере
            DieServer();
            PlayDeathClientRpc();
        }
        else
        {
            // Анимация урона всем
            PlayHitClientRpc();
        }    
    }

    private void DieServer()
    {
        // отключаем движение на сервере
        if (movement != null) movement.enabled = false;

        if (controller != null) controller.enabled = false;

        // Despawn
        StartCoroutine(DespawnCoroutine());
    }

    [ClientRpc]
    private void PlayHitClientRpc()
    {
        if (animator != null)
            animator.SetTrigger("Hit");
    }

    [ClientRpc]
    private void PlayDeathClientRpc()
    {
        if (animator != null)
            animator.SetBool("Death", true);
    }


    IEnumerator DespawnCoroutine()
    {
        yield return new WaitForSeconds(4);
        NetworkObject.Despawn(true);
    }


}
