using TMPro;
using Unity.Netcode;
using UnityEngine;

public class GoldUI : MonoBehaviour
{
    [SerializeField] private TMP_Text goldText;

    void Start()
    {
        if (GamePhaseManager.Instance == null)
            return;

        var gm = GamePhaseManager.Instance;

        if (NetworkManager.Singleton.LocalClientId ==
            NetworkManager.ServerClientId)
        {
            gm.HostGold.OnValueChanged += OnGoldChanged;
            UpdateText(gm.HostGold.Value);
        }
        else
        {
            gm.ClientGold.OnValueChanged += OnGoldChanged;
            UpdateText(gm.ClientGold.Value);
        }
    }

    void OnGoldChanged(int oldValue, int newValue)
    {
        UpdateText(newValue);
    }

    void UpdateText(int gold)
    {
        goldText.text = $"Gold: {gold}";
    }

    void OnDestroy()
    {
        if (GamePhaseManager.Instance == null)
            return;

        var gm = GamePhaseManager.Instance;

        gm.HostGold.OnValueChanged -= OnGoldChanged;
        gm.ClientGold.OnValueChanged -= OnGoldChanged;
    }
}
