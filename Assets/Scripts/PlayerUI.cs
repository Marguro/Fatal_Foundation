using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    [Header("System References")]
    public HealthSystem healthSystem;
    public StaminaSystem staminaSystem;

    [Header("UI References")]
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI staminaText;

    private void OnEnable()
    {
        if (healthSystem != null)
            healthSystem.OnHealthChanged += UpdateHealthUI;
        
        if (staminaSystem != null)
            staminaSystem.OnStaminaChanged += UpdateStaminaUI;
    }

    private void OnDisable()
    {
        if (healthSystem != null)
            healthSystem.OnHealthChanged -= UpdateHealthUI;
        
        if (staminaSystem != null)
            staminaSystem.OnStaminaChanged -= UpdateStaminaUI;
    }

    void Start()
    {
        if (healthSystem != null)
            UpdateHealthUI(healthSystem.currentHealth.Value, healthSystem.MaxHealth);
            
        if (staminaSystem != null)
            UpdateStaminaUI(staminaSystem.currentStamina.Value, staminaSystem.MaxStamina);
    }

    private void UpdateHealthUI(float current, float max)
    {
        if (healthText != null)
        {
            healthText.text = $"Health {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
        }
    }

    private void UpdateStaminaUI(float current, float max)
    {
        if (staminaText != null)
        {
            staminaText.text = $"Stamina {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
        }
    }
}
