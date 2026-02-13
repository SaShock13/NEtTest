using Unity.Netcode;
using UnityEngine;

public class PlayerThrow : NetworkBehaviour
{
    [SerializeField] private NetworkObject ballPrefab;
    [SerializeField] private Transform handPos;
    [SerializeField] private float throwForce = 10f;

   

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Vector3 dir = transform.forward;
            ThrowBallServerRpc(handPos.position, dir);
        }
    }

    [ServerRpc]
    private void ThrowBallServerRpc(Vector3 position, Vector3 direction)
    {
        // 1) спавним шарик на сервере
        var ball = Instantiate(ballPrefab, position, Quaternion.identity);
        ball.Spawn();

        // 2) даем движение (сервер)
        var rb = ball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = direction.normalized * throwForce;
        }
    }
}
