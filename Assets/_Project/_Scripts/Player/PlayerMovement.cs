using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Сервер-авторитетное движение игрока:
/// - Клиент читает ввод (WASD + Space)
/// - Клиент отправляет ввод на сервер через ServerRpc
/// - Сервер выполняет движение (CharacterController.Move)
/// - Сервер обновляет аниматор (Speed, IsGrounded, VerticalSpeed)
/// </summary>
[RequireComponent(typeof(CharacterController), typeof(NetworkObject))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;          // скорость бега
    [SerializeField] private float rotateSpeed = 10f;       // скорость поворота
    [SerializeField] private float inputSendRate = 0.05f;   // частота отправки ввода на сервер (0.05 = 20 раз/сек)

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpHeight = 1.2f;       // высота прыжка (примерно)
    [SerializeField] private float gravity = -20f;          // гравитация (отрицательная)
    [SerializeField] private float groundedStickForce = -2f; // небольшая "прилипалка" к земле, чтобы не дрожало

    // ссылки
    private CharacterController controller;
    [SerializeField] private Animator animator;
    private Transform camTransform; // локальная камера владельца (для движения относительно камеры)

    // ----------------------------
    // CLIENT INPUT (только владелец)
    // ----------------------------

    private float inputHorizontal;
    private float inputVertical;

    // jumpPressed = true только в момент нажатия Space (один кадр)
    private bool jumpPressed;

    // Чтобы не слать одинаковые данные каждый раз
    private Vector3 lastSentDirection;
    private float sendTimer;

    // ----------------------------
    // SERVER MOVEMENT (серверная логика)
    // ----------------------------

    // targetMoveDirection — направление, которое пришло от клиента
    private Vector3 targetMoveDirection;

    // currentMoveDirection — сглаженное направление (чтобы не дергалось)
    private Vector3 currentMoveDirection;

    // вертикальная скорость (прыжок/падение)
    private float verticalVelocity;

    // флаг запроса прыжка (пришел от клиента)
    private bool serverJumpRequest;

    // состояние земли
    private bool isGrounded;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        // Animator может быть на корне или на дочернем объекте (модель)
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        // Камера нужна только владельцу (чтобы двигаться относительно камеры)
        if (IsOwner && Camera.main != null)
            camTransform = Camera.main.transform;
    }

    private void Update()
    {
        // ----------------------------
        // 1) ВЛАДЕЛЕЦ ЧИТАЕТ ВВОД
        // ----------------------------
        if (IsOwner)
        {
            inputHorizontal = Input.GetAxisRaw("Horizontal");
            inputVertical = Input.GetAxisRaw("Vertical");

            // Важно: прыжок считываем как "одно нажатие"
            // чтобы не было прыжка каждую отправку
            if (Input.GetKeyDown(KeyCode.Space))
                jumpPressed = true;
        }

        // ----------------------------
        // 2) СЕРВЕР ОБНОВЛЯЕТ АНИМАЦИЮ
        // ----------------------------
        if (IsServer)
        {
            // Speed — это "насколько быстро бежим"
            // currentMoveDirection.magnitude = 0..1
            float speed = currentMoveDirection.magnitude * moveSpeed;

            animator.SetFloat("Speed", speed);

            // Для прыжка/падения
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetFloat("VerticalSpeed", verticalVelocity);
        }
    }

    private void FixedUpdate()
    {
        // ----------------------------
        // 1) ВЛАДЕЛЕЦ ОТПРАВЛЯЕТ ВВОД НА СЕРВЕР
        // ----------------------------
        if (IsOwner)
        {
            sendTimer += Time.fixedDeltaTime;

            // ограничиваем частоту отправки
            if (sendTimer >= inputSendRate)
            {
                sendTimer = 0f;
                SendInputToServer();
            }
        }

        // ----------------------------
        // 2) СЕРВЕР ДВИГАЕТ ИГРОКА
        // ----------------------------
        if (IsServer)
        {
            // (A) Проверяем землю
            // CharacterController.isGrounded работает нормально,
            // но иногда может быть "дрожание" без groundedStickForce.
            isGrounded = controller.isGrounded;

            // Если мы на земле и падаем — фиксируем вертикальную скорость
            // чтобы игрок не подпрыгивал микроскопически.
            if (isGrounded && verticalVelocity < 0f)
                verticalVelocity = groundedStickForce;

            // (B) Прыжок: ТОЛЬКО если isGrounded
            // serverJumpRequest приходит от клиента.
            if (serverJumpRequest)
            {
                // Прыгать можно только если стоим на земле
                if (isGrounded)
                {
                    // Формула: v = sqrt(2 * jumpHeight * -gravity)
                    // Это дает прыжок примерно на jumpHeight.
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }

                // В любом случае сбрасываем запрос прыжка,
                // чтобы он не "висел" до следующего кадра.
                serverJumpRequest = false;
            }

            // (C) Гравитация всегда тянет вниз
            verticalVelocity += gravity * Time.fixedDeltaTime;

            // (D) Сглаживание горизонтального направления
            // targetMoveDirection приходит с клиента.
            currentMoveDirection = Vector3.Lerp(currentMoveDirection, targetMoveDirection, 0.5f);

            // (E) Итоговое движение
            Vector3 horizontalMove = currentMoveDirection * moveSpeed;

            // Важно: CharacterController.Move ожидает скорость * deltaTime,
            // поэтому мы формируем "скорость" и потом умножаем на dt.
            Vector3 finalMove = new Vector3(horizontalMove.x, verticalVelocity, horizontalMove.z);

            controller.Move(finalMove * Time.fixedDeltaTime);

            // (F) Поворот в сторону движения
            if (currentMoveDirection.magnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(currentMoveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.fixedDeltaTime);
            }
        }
    }

    /// <summary>
    /// Отправляет на сервер:
    /// - направление движения (относительно камеры)
    /// - флаг прыжка (один раз)
    /// </summary>
    private void SendInputToServer()
    {
        Vector3 direction = CalculateCameraRelativeDirection();

        // 1) Экономим трафик: отправляем направление только если оно изменилось
        bool directionChanged = Vector3.Distance(direction, lastSentDirection) > 0.01f;

        // 2) Прыжок отправляем всегда, если он был нажат
        // даже если направление не менялось.
        if (directionChanged || jumpPressed)
        {
            lastSentDirection = direction;

            // отправляем ввод на сервер
            SubmitMovementServerRpc(direction, jumpPressed);

            // сбрасываем прыжок после отправки
            jumpPressed = false;
        }
    }

    /// <summary>
    /// Рассчитывает направление движения относительно камеры:
    /// - W = вперед камеры
    /// - S = назад камеры
    /// - A/D = влево/вправо относительно камеры
    /// </summary>
    private Vector3 CalculateCameraRelativeDirection()
    {
        // Если ввода нет — стоим
        if (Mathf.Approximately(inputHorizontal, 0) && Mathf.Approximately(inputVertical, 0))
            return Vector3.zero;

        // Если камера не найдена — запасной вариант (мировые оси)
        if (camTransform == null)
            return new Vector3(inputHorizontal, 0, inputVertical).normalized;

        // Берем только поворот камеры по оси Y
        Quaternion cameraYRotation = Quaternion.Euler(0, camTransform.eulerAngles.y, 0);

        // inputDir = направление из клавиатуры
        Vector3 inputDir = new Vector3(inputHorizontal, 0, inputVertical).normalized;

        // Поворачиваем inputDir так, чтобы он стал относительным к камере
        return cameraYRotation * inputDir;
    }

    /// <summary>
    /// СерверRpc — вызывается клиентом.
    /// Тут сервер получает ввод и сохраняет в переменные,
    /// которые потом использует в FixedUpdate.
    /// </summary>
    [ServerRpc]
    private void SubmitMovementServerRpc(Vector3 direction, bool jump)
    {
        // сохраняем направление движения
        targetMoveDirection = direction;

        // сохраняем запрос прыжка (один раз)
        if (jump)
            serverJumpRequest = true;
    }
}
