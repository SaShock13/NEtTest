using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using Unity.Networking.Transport.Relay;

public class RelayConnection : MonoBehaviour
{
    [SerializeField] private int maxPlayers = 4;

    public string LastJoinCode { get; private set; }

    private bool _servicesInitialized;

    private async Task InitServices()
    {
        if (_servicesInitialized) return;

        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        _servicesInitialized = true;
    }

    public async Task<string> StartRelayHostAsync()
    {
        await InitServices();

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();

        RelayServerData relayServerData = allocation.ToRelayServerData("dtls");
        utp.SetRelayServerData(relayServerData);

        NetworkManager.Singleton.StartHost();

        LastJoinCode = joinCode;
        Debug.Log($"[RELAY] Host started. JoinCode = {joinCode}");

        return joinCode;
    }

    public async Task StartRelayClientAsync(string joinCode)
    {
        await InitServices();

        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();

        RelayServerData relayServerData = joinAllocation.ToRelayServerData("dtls");
        utp.SetRelayServerData(relayServerData);

        NetworkManager.Singleton.StartClient();

        Debug.Log($"[RELAY] Client started. Code = {joinCode}");
    }
}
