using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RelayUI : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private RelayConnection relay;

    [Header("UI")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text joinCodeText;

    private void Awake()
    {
        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);

        statusText.text = "Ready";
        joinCodeText.text = "";
    }

    private async void OnHostClicked()
    {
        try
        {
            statusText.text = "Creating Relay...";
            hostButton.interactable = false;
            joinButton.interactable = false;

            string code = await relay.StartRelayHostAsync();

            joinCodeText.text = $"Join Code: {code}";
            statusText.text = "Host started";
        }
        catch (Exception e)
        {
            statusText.text = "Host error (see Console)";
            Debug.LogError(e);

            hostButton.interactable = true;
            joinButton.interactable = true;
        }
    }

    private async void OnJoinClicked()
    {
        string code = joinCodeInput.text.Trim();

        if (string.IsNullOrEmpty(code))
        {
            statusText.text = "Enter Join Code!";
            return;
        }

        try
        {
            statusText.text = "Joining Relay...";
            hostButton.interactable = false;
            joinButton.interactable = false;

            await relay.StartRelayClientAsync(code);

            statusText.text = "Client started";
        }
        catch (Exception e)
        {
            statusText.text = "Join error (see Console)";
            Debug.LogError(e);

            hostButton.interactable = true;
            joinButton.interactable = true;
        }
    }
}
