using UnityEngine;
using UnityEngine.UI;

public class UnitHPBar : MonoBehaviour
{
    [SerializeField] private Slider slider;

    UnitController unit;

    void Start()
    {
        unit = GetComponentInParent<UnitController>();

        slider.minValue = 0f;
        slider.maxValue = unit.maxHP;
        slider.value = unit.currentHP.Value;

        unit.currentHP.OnValueChanged += OnHPChanged;
    }

    void OnHPChanged(float oldValue, float newValue)
    {
        slider.value = newValue;
    }

    void LateUpdate()
    {
        if (Camera.main != null)
            transform.forward = Camera.main.transform.forward;
    }

    void OnDestroy()
    {
        if (unit != null)
            unit.currentHP.OnValueChanged -= OnHPChanged;
    }
}