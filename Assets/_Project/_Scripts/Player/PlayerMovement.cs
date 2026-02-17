using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(NetworkObject))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 10f;
    [SerializeField] private float inputSendRate = 0.05f; // 20 раз в секунду

    private CharacterController controller;
    [SerializeField] private Animator animator;
    private Transform camTransform; // локальная камера (только для владельца)

    // Client-side input
    private float inputHorizontal;
    private float inputVertical;
    private Vector3 lastSentDirection;
    private float sendTimer;

    // Server-side movement
    private Vector3 currentMoveDirection;
    private Vector3 targetMoveDirection;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if(animator == null) animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (IsOwner && Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        // Владелец: читаем ввод каждый кадр
        if (IsOwner)
        {
            inputHorizontal = Input.GetAxisRaw("Horizontal");
            inputVertical = Input.GetAxisRaw("Vertical");
        }

        // Сервер: обновляем анимацию
        if (IsServer)
        {
            float speed = currentMoveDirection.magnitude * moveSpeed;
            animator.SetFloat("Speed", speed);
        }
    }

    private void FixedUpdate()
    {
        // Владелец: отправляем направление на сервер с ограниченной частотой
        if (IsOwner)
        {
            sendTimer += Time.fixedDeltaTime;
            if (sendTimer >= inputSendRate)
            {
                sendTimer = 0f;
                SendInputToServer();
            }
        }

        // Сервер: движение
        if (IsServer)
        {
            // Плавно приближаемся к целевому направлению (избегаем рывков)
            currentMoveDirection = Vector3.Lerp(currentMoveDirection, targetMoveDirection, 0.5f);

            if (currentMoveDirection.magnitude > 0.1f)
            {
                // Движение
                Vector3 move = currentMoveDirection * moveSpeed * Time.fixedDeltaTime;
                controller.Move(move);

                // Поворот в сторону движения
                Quaternion targetRot = Quaternion.LookRotation(currentMoveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.fixedDeltaTime);
            }
        }
    }

    private void SendInputToServer()
    {
        Vector3 direction = CalculateCameraRelativeDirection();

        // Отправляем только если направление изменилось (экономия трафика)
        if (Vector3.Distance(direction, lastSentDirection) > 0.01f)
        {
            lastSentDirection = direction;
            SubmitMovementServerRpc(direction);
        }
    }

    private Vector3 CalculateCameraRelativeDirection()
    {
        // Если нет ввода – возвращаем ноль
        if (Mathf.Approximately(inputHorizontal, 0) && Mathf.Approximately(inputVertical, 0))
            return Vector3.zero;

        // Если камера не найдена – используем мировые оси (запасной вариант)
        if (camTransform == null)
            return new Vector3(inputHorizontal, 0, inputVertical).normalized;

        // Получаем поворот камеры только по Y
        Quaternion cameraYRotation = Quaternion.Euler(0, camTransform.eulerAngles.y, 0);
        Vector3 inputDir = new Vector3(inputHorizontal, 0, inputVertical).normalized;

        // Поворачиваем направление ввода на угол камеры
        return cameraYRotation * inputDir;
    }

    [ServerRpc]
    private void SubmitMovementServerRpc(Vector3 direction)
    {
        targetMoveDirection = direction;
    }

    // Для визуальной отладки (опционально)
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && IsServer)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, currentMoveDirection * 2f);
        }
    }
}