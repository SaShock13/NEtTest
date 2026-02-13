using Unity.Netcode;
using UnityEngine;

public class BonusSpawner : NetworkBehaviour
{
    [SerializeField] private NetworkObject ballPrefab;

    public void SpawnBall()
    {
        // Вызываем RPC только если сеть запущена
        if (!NetworkManager.Singleton.IsListening)
            return;

        // Если мы сервер (Host тоже сервер) — можно спавнить напрямую
        if (IsServer)
        {
            SpawnBallServer();
        }
        else
        {
            SpawnBallServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnBallServerRpc()
    {
        SpawnBallServer();
    }


    private void SpawnBallServer()
    {
        Vector3 pos = new Vector3(Random.Range(-3f, 3f), 1f, Random.Range(-3f, 3f));
        NetworkObject ball = Instantiate(ballPrefab, pos, Quaternion.identity);
        ball.Spawn();
    }

}
