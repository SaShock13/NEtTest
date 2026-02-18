using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;

public class CinemaSetup : NetworkBehaviour
{

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            SetupCamera();
        }
    }

    private void SetupCamera()
    {
        // Ищем FreeLook камеру в сцене (предполагается, что она одна)
        CinemachineCamera freeLook = FindAnyObjectByType<CinemachineCamera>();
        if (freeLook != null)
        {
            freeLook.Follow = transform;
            freeLook.LookAt = transform;
            Debug.Log("Cinemachine camera assigned to local player.");
        }
        else
        {
            Debug.LogError("No CinemachineFreeLook found in scene!");
        }
    }
}