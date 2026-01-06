using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class PhaseTimerUI : MonoBehaviour
{
    [SerializeField] private Slider timerSlider;

    GamePhaseManager gpm;

    float currentPhaseDuration;

    void Start()
    {
        gpm = GamePhaseManager.Instance;
        if (gpm == null)
            return;

        gpm.CurrentPhase.OnValueChanged += OnPhaseChanged;

        OnPhaseChanged(
            GamePhaseManager.GamePhase.Prep,
            gpm.CurrentPhase.Value
        );
    } 

    void OnDestroy()
    {
        if (gpm != null)
        {
            gpm.CurrentPhase.OnValueChanged -= OnPhaseChanged;
        }
    }

    void Update()
    {
        if (gpm == null || timerSlider == null)
            return;

        if (currentPhaseDuration <= 0f)
            return;

        float t = Mathf.Clamp01(
            gpm.PhaseTimer.Value / currentPhaseDuration
        );

        timerSlider.value = t;
    }

    void OnPhaseChanged(
        GamePhaseManager.GamePhase oldPhase,
        GamePhaseManager.GamePhase newPhase)
    {
        switch (newPhase)
        {
            case GamePhaseManager.GamePhase.Prep:
                currentPhaseDuration = gpm.PrepDuration;
                timerSlider.gameObject.SetActive(true);
                break;

            case GamePhaseManager.GamePhase.Battle:
                currentPhaseDuration = gpm.BattleDuration;
                timerSlider.gameObject.SetActive(true);
                break;

            default:
                timerSlider.gameObject.SetActive(false);
                break;
        }
    }
}
