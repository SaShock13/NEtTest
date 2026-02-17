using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class ServerMovementWithAnim : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 4f;
    [SerializeField] private float mouseSensitivity = 2f;

    [Header("Visual (for host)")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float visualPosLerp = 18f;
    [SerializeField] private float visualRotLerp = 25f;

    // Input и yaw сервера
    private Vector2 serverInput;
    private float serverYaw;

    public NetworkVariable<bool> IsWalking = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Vector3 prevPos, currPos;
    private Quaternion prevRot, currRot;
    private float lastFixedTime;

    private float sendTimer;
    private const float sendRate = 1f / 60f;
    private float clientYaw;

    // Камера игрока (для направления движения)
    private Transform mainCamera;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
            mainCamera = Camera.main.transform;

        if (IsServer)
        {
            currPos = transform.position;
            currRot = transform.rotation;
            prevPos = currPos;
            prevRot = currRot;
            lastFixedTime = Time.time;

            if (IsOwner)
            {
                var nt = GetComponent<Unity.Netcode.Components.NetworkTransform>();
                if (nt != null) nt.enabled = false;
            }
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Поворот мышью (yaw локальный)
        clientYaw += Input.GetAxis("Mouse X") * mouseSensitivity;

        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        sendTimer += Time.deltaTime;
        if (sendTimer >= sendRate)
        {
            sendTimer = 0f;
            SendInputServerRpc(input, clientYaw);
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        // Двигаем игрока
        Vector3 move = new Vector3(serverInput.x, 0f, serverInput.y);
        bool walking = move.sqrMagnitude > 0.001f;

        if (walking)
        {
            move.Normalize();

            // Движение относительно камеры, если она есть
            Vector3 forward = mainCamera != null ? mainCamera.forward : transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 right = Vector3.Cross(Vector3.up, -forward); // правый вектор
            Vector3 desiredMove = forward * move.z + right * move.x;

            transform.position += desiredMove * speed * Time.fixedDeltaTime;

            // Поворот игрока в направлении движения
            transform.rotation = Quaternion.LookRotation(desiredMove, Vector3.up);
            serverYaw = transform.eulerAngles.y;
        }

        IsWalking.Value = walking;

        prevPos = currPos;
        currPos = transform.position;
        prevRot = currRot;
        currRot = transform.rotation;
        lastFixedTime = Time.time;
    }

    private void LateUpdate()
    {
        if (!IsOwner || !IsServer || visualRoot == null) return;

        float t = Mathf.Clamp01((Time.time - lastFixedTime) / Time.fixedDeltaTime);
        Vector3 targetPos = Vector3.Lerp(prevPos, currPos, t);
        Quaternion targetRot = Quaternion.Slerp(prevRot, currRot, t);

        float posT = 1f - Mathf.Exp(-visualPosLerp * Time.deltaTime);
        float rotT = 1f - Mathf.Exp(-visualRotLerp * Time.deltaTime);

        visualRoot.position = Vector3.Lerp(visualRoot.position, targetPos, posT);
        visualRoot.rotation = Quaternion.Slerp(visualRoot.rotation, targetRot, rotT);
    }

    [ServerRpc]
    private void SendInputServerRpc(Vector2 input, float yaw)
    {
        serverInput = input;
        serverYaw = yaw;
    }
}
