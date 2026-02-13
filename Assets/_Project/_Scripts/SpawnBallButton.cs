using Unity.Netcode;
using UnityEngine;

public class SpawnBallButton : MonoBehaviour
{
    public void OnClickSpawnBall()
    {
        if (NetworkManager.Singleton == null) return;
        if (!NetworkManager.Singleton.IsListening) return;

        // Ищем локального игрока
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.ClientId != NetworkManager.Singleton.LocalClientId)
                continue;

            var playerObject = client.PlayerObject;
            if (playerObject == null) return;

            var spawner = playerObject.GetComponent<PlayerSpawner>();
            if (spawner == null) return;

            spawner.TrySpawnBall();
            return;
        }
    }
}
