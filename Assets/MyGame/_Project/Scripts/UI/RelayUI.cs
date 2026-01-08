using UnityEngine;
using TMPro;

public class RelayUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text hostCodeText;

    [Header("Managers")]
    [SerializeField] private RelayHostManager relayHost;
    [SerializeField] private RelayClientManager relayClient;

    public async void OnHostClicked()
    {
        hostCodeText.text = "Creating room...";
        string code = await relayHost.StartRelayHost(1);
        hostCodeText.text = $"Join Code: {code}";
    }

    public async void OnJoinClicked()
    {
        string code = joinCodeInput.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("Join code empty");
            return;
        }

        await relayClient.StartRelayClient(code);
    }
}
