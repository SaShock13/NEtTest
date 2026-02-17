using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine;

public class PlayerCinemachineSetup : NetworkBehaviour
{
    [Header("Camera Offset")]
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 2f, -6f);
    [SerializeField] private float smoothPosTime = 0.08f;
    [SerializeField] private float smoothRotTime = 0.05f;

    private Transform smoothTarget;
    private Vector3 smoothVelocity;
    private CinemachineCamera cmCam;
    private Transform originalParent;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        cmCam = FindFirstObjectByType<CinemachineCamera>();
        if (cmCam == null) return;

        // Smooth target — позиция игрока
        smoothTarget = new GameObject("CameraSmoothTarget").transform;
        smoothTarget.position = transform.position;
        smoothTarget.rotation = transform.rotation;

        originalParent = cmCam.transform.parent;
        cmCam.transform.SetParent(smoothTarget, false);
        cmCam.transform.localPosition = cameraOffset;
        cmCam.transform.localRotation = Quaternion.identity;

        // Отключаем Follow/LookAt
        foreach (var c in cmCam.GetComponents<MonoBehaviour>())
            if (c.GetType().Name.Contains("Follow") || c.GetType().Name.Contains("LookAt"))
                c.enabled = false;
    }

    private void LateUpdate()
    {
        if (!IsOwner || smoothTarget == null) return;

        // Smooth позиция и поворот
        smoothTarget.position = Vector3.SmoothDamp(smoothTarget.position, transform.position, ref smoothVelocity, smoothPosTime);
        float rotT = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(smoothRotTime, 0.001f));
        smoothTarget.rotation = Quaternion.Slerp(smoothTarget.rotation, transform.rotation, rotT);
    }

    public override void OnNetworkDespawn()
    {
        if (cmCam != null)
            cmCam.transform.SetParent(originalParent, true);

        if (smoothTarget != null)
            Destroy(smoothTarget.gameObject);
    }
}
