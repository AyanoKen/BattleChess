using UnityEngine;
using System.Collections;

public class PromotionUIManager : MonoBehaviour
{
    public static PromotionUIManager Instance;

    [SerializeField] private GameObject promotionPanel;

    ulong activeUnitId;

    void Awake()
    {
        Instance = this;

        if (promotionPanel != null)
        {
            promotionPanel.SetActive(false);
        }
    }

    void Start()
    {
        if (GamePhaseManager.Instance != null)
        {
            GamePhaseManager.Instance.CurrentPhase.OnValueChanged
                += OnPhaseChanged;
        }
        else
        {
            StartCoroutine(WaitForGamePhaseManager());
        }
    }

    IEnumerator WaitForGamePhaseManager()
    {
        while (GamePhaseManager.Instance == null)
            yield return null;

        GamePhaseManager.Instance.CurrentPhase.OnValueChanged
            += OnPhaseChanged;
    }

    void OnDisable()
    {
        if (GamePhaseManager.Instance != null)
        {
            GamePhaseManager.Instance.CurrentPhase.OnValueChanged
                -= OnPhaseChanged;
        }
    }

    public void ChooseKnight()
    {
        GamePhaseManager.Instance.SubmitPromotionChoiceServerRpc(
                activeUnitId,
                UnitController.UnitType.Knight
            );
        CloseUI();
    }

    public void ChooseBishop()
    {
        GamePhaseManager.Instance.SubmitPromotionChoiceServerRpc(
                activeUnitId,
                UnitController.UnitType.Bishop
            );
        CloseUI();
    }

    public void ChooseRook()
    {
        CloseUI();
    }

    void CloseUI()
    {
        if (promotionPanel != null)
        {
            promotionPanel.SetActive(false);
        }
    }

    public void ShowPromotionUI(ulong unitId)
    {
        activeUnitId = unitId;

        if (promotionPanel != null)
        {
            promotionPanel.SetActive(true);   
        }
    }

    void OnPhaseChanged(
        GamePhaseManager.GamePhase oldPhase,
        GamePhaseManager.GamePhase newPhase)
    {
        if (newPhase != GamePhaseManager.GamePhase.Prep)
        {
            CloseUI();
        }
    }

}
