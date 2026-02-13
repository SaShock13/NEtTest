using Unity.Netcode;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private NetworkObject ballPrefab;

    public void TrySpawnBall()
    {
        // кнопку может нажать только локальный игрок
        if (!IsOwner) return;

        SpawnBallServerRpc();
    }

    [ServerRpc]
    private void SpawnBallServerRpc()
    {
        Vector3 pos = new Vector3(
            Random.Range(-3f, 3f),
            1f,
            Random.Range(-3f, 3f)
        );

        NetworkObject ball = Instantiate(ballPrefab, pos, Quaternion.identity);
        ball.Spawn();
    }
}
