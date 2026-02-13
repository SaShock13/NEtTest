using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float _speed = 5f;

    void Update()
    {
        if (!IsOwner) return;

        var hor = Input.GetAxis("Horizontal");
        var vert = Input.GetAxis("Vertical");
        var inDirection  = new Vector3(hor,0,vert);

        if(inDirection.sqrMagnitude > 0 )
        {
            MoveServerRpc(inDirection);
        }
    }

    [ServerRpc]
    private void MoveServerRpc(Vector3 inDirection)
    {
        transform.position += inDirection * _speed * Time.deltaTime;
    }
}
