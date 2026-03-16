using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class StaminaSystem : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRegenRate = 10f;
    [SerializeField] private float sprintCostPerSecond = 20f;
    [SerializeField] private float regenDelay = 2f; 
        
    [SerializeField] private bool debugLogs;

    [FormerlySerializedAs("CurrentStamina")] public NetworkVariable<float> currentStamina = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner 
    );

    public event Action<float, float> OnStaminaChanged;

    private float _lastStaminaUseTime;

    public float MaxStamina => maxStamina;
    public float SprintCostPerSecond => sprintCostPerSecond;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            currentStamina.Value = maxStamina;
        }

        currentStamina.OnValueChanged += OnStaminaChangedCallback;
        // Initial update if needed locally, though value might be default until synched
    }

    public override void OnNetworkDespawn()
    {
        currentStamina.OnValueChanged -= OnStaminaChangedCallback;
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Regen Stamina if not recently used and not full
        if (Time.time - _lastStaminaUseTime >= regenDelay && currentStamina.Value < maxStamina)
        {
            float newStamina = Mathf.Clamp(currentStamina.Value + staminaRegenRate * Time.deltaTime, 0, maxStamina);
            if (Math.Abs(newStamina - currentStamina.Value) > 0.01f)
            {
                currentStamina.Value = newStamina;
            }
        }
    }

    private void OnStaminaChangedCallback(float oldValue, float newValue)
    {
        OnStaminaChanged?.Invoke(newValue, maxStamina);
        if (debugLogs) Debug.Log($"{name} Stamina changed: {newValue}/{maxStamina}");
    }

    /// <summary>
    /// Try to consume stamina. Returns true if successful.
    /// </summary>
    /// <param name="amount">Amount to consume</param>
    public bool TryConsumeStamina(float amount)
    {
        if (!IsOwner) return false;

        if (currentStamina.Value >= amount)
        {
            currentStamina.Value -= amount;
            _lastStaminaUseTime = Time.time;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Continuously consume stamina (e.g. for sprinting). Call this every frame while action is active.
    /// </summary>
    /// <param name="amountPerSecond">Rate of consumption</param>
    public bool TryConsumeStaminaContinuous(float amountPerSecond)
    {
        float amount = amountPerSecond * Time.deltaTime;
        return TryConsumeStamina(amount);
    }

    /// <summary>
    /// Restore stamina (e.g. from food/items).
    /// </summary>
    public void RestoreStamina(float amount)
    {
        if (!IsOwner) return;

        currentStamina.Value = Mathf.Clamp(currentStamina.Value + amount, 0, maxStamina);
    }

    public bool CanConsumeStamina(float amount)
    {
        return currentStamina.Value >= amount;
    }
}