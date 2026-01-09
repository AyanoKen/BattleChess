using TMPro;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class KingHPUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text whiteKingHPText;
    [SerializeField] private TMP_Text blackKingHPText;

    UnitController whiteKing;
    UnitController blackKing;

    void Start()
    {
        Invoke(nameof(DelayedInit), 0.2f);
    }

    void DelayedInit()
    {
        FindKings();

        if (whiteKing != null)
            Subscribe(whiteKing, whiteKingHPText);

        if (blackKing != null)
            Subscribe(blackKing, blackKingHPText);
    }

    void FindKings()
    {
        var kings = FindObjectsOfType<UnitController>()
            .Where(u => u.unitType == UnitController.UnitType.King);

        foreach (var king in kings)
        {
            if (king.OwnerClientId == NetworkManager.ServerClientId)
            {
                whiteKing = king;
            }
            else
            {
                blackKing = king;
            }
        }
    }

    void Subscribe(UnitController king, TMP_Text text)
    {
        UpdateText(text, king.currentHP.Value);

        king.currentHP.OnValueChanged +=
            (oldValue, newValue) =>
            {
                UpdateText(text, newValue);
            };
    }

    void UpdateText(TMP_Text text, float hp)
    {
        text.text = $"King HP: {Mathf.CeilToInt(hp)}";
    }
}