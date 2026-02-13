using Unity.Netcode;
using UnityEngine;

public class ServerMovementWithAnim : NetworkBehaviour
{
    [SerializeField] private float speed = 4f;
    [SerializeField] private Transform headCube;
    [SerializeField] private float rotateSpeed = 360f;

    // Сервер хранит input игрока
    private Vector2 serverInput;

    // Для анимации всем
    private NetworkVariable<bool> isWalking = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private float sendTimer;
    private const float SendRate = 0.05f; // 20 раз/сек

    private void Update()
    {
        // Клиент-владелец читает input
        if (IsOwner)
        {
            Vector2 input = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            );

            // чтобы не спамить RPC каждый кадр — шлём 20 раз в секунду
            sendTimer += Time.deltaTime;
            if (sendTimer >= SendRate)
            {
                sendTimer = 0f;
                SendInputServerRpc(input);
            }
        }

        // Визуал на всех (анимация)
        if (headCube != null && isWalking.Value)
        {
            headCube.Rotate(0f, rotateSpeed * Time.deltaTime, 0f);
        }
    }

    private void FixedUpdate()
    {
        // Двигает только сервер
        if (!IsServer) return;

        Vector3 move = new Vector3(serverInput.x, 0f, serverInput.y);
        bool walkingNow = move.sqrMagnitude > 0.001f;

        if (walkingNow)
        {
            move.Normalize();
            transform.position += move * speed * Time.fixedDeltaTime;
        }

        isWalking.Value = walkingNow;
    }

    [ServerRpc]
    private void SendInputServerRpc(Vector2 input)
    {
        serverInput = input;
    }
}
