using Unity.Netcode;
using UnityEngine;

public class NetworkUICantroller : NetworkBehaviour
{
    
    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
    } 
    
    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }
}
