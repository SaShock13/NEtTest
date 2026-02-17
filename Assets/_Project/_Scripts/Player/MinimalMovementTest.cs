using Unity.Netcode;
using UnityEngine;

public class MinimalMovementTest : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private CharacterController controller;
    private Vector3 moveDirection;
    private float horizontal;
    private float vertical;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Простейший сбор ввода
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");

        // Отправляем на сервер
        if (horizontal != 0 || vertical != 0)
        {
            SubmitMoveServerRpc(horizontal, vertical);
        }
        else
        {
            SubmitMoveServerRpc(0, 0);
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        // Простейшее движение
        if (moveDirection.magnitude > 0.1f)
        {
            controller.Move(moveDirection * moveSpeed * Time.fixedDeltaTime);
        }
    }

    [ServerRpc]
    private void SubmitMoveServerRpc(float h, float v)
    {
        // Мирные координаты (вперед по Z)
        moveDirection = new Vector3(h, 0, v).normalized;

        // Логируем каждый вызов
        Debug.Log($"[SERVER] Got move: ({h}, {v}) -> {moveDirection}");
    }
}