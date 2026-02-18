using TMPro;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine;

public class PlayerVisual : NetworkBehaviour
{
    private Camera cam;

    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Color ownerColor;

    public NetworkVariable<FixedString32Bytes> PlayerNameVar =
        new NetworkVariable<FixedString32Bytes>(
            "",
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );


    private void Start()
    {
        cam = Camera.main;
    }

    public override void OnNetworkSpawn()
    {
        // обновить сразу
        UpdateName(PlayerNameVar.Value);

        // подписка на изменения
        PlayerNameVar.OnValueChanged += OnNameChanged;

        // если это мой игрок — сервер установит имя
        if (IsOwner)
        {
            string myName = $"Player {OwnerClientId}";
            SetNameServerRpc(myName);
            nameText.color = ownerColor;
        }

    }

    public override void OnNetworkDespawn()
    {
        PlayerNameVar.OnValueChanged -= OnNameChanged;
    }

    private void OnNameChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
    {
        UpdateName(newValue);
    }

    private void UpdateName(FixedString32Bytes value)
    {
        if (nameText != null)
            nameText.text = value.ToString();
    }

    [ServerRpc]
    private void SetNameServerRpc(string newName)
    {
        PlayerNameVar.Value = newName;
    }

    private void LateUpdate()
    {
        if (cam == null) return;
       // if(IsOwner) return;
        nameText.transform.forward = cam.transform.forward;
    }
}
