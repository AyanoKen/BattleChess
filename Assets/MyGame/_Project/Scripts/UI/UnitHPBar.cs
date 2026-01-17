using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Security.Cryptography.X509Certificates;

public class UnitHPBar : MonoBehaviour
{
    [SerializeField] private Slider slider;

    [SerializeField] private TMP_Text fusionStack;

    UnitController unit;

    void Start()
    {
        unit = GetComponentInParent<UnitController>();

        slider.minValue = 0f;
        slider.maxValue = unit.maxHP;
        slider.value = unit.currentHP.Value;

        fusionStack.text = "";

        if (unit.fusionCount > 0)
        {
            fusionStack.text = "+1";
        }

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

    public void UpdateFusionCount(int count)
    {
        if (count <= 0)
        {
            fusionStack.text = "";
            return;
        }

        fusionStack.text = $"+{count}";
    }
}