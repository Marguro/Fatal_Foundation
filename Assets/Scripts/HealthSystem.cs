using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class HealthSystem : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool destroyOnDeath = true;

    [SerializeField] private bool debugLogs;

    [FormerlySerializedAs("CurrentHealth")] public NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        100f
    );

    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;

    public float MaxHealth => maxHealth;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }

        currentHealth.OnValueChanged += OnHealthChangedCallback;
        // Initial update
        OnHealthChanged?.Invoke(currentHealth.Value, maxHealth);
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChangedCallback;
    }

    private void OnHealthChangedCallback(float oldValue, float newValue)
    {
        OnHealthChanged?.Invoke(newValue, maxHealth);
        if (debugLogs) Debug.Log($"{name} Health changed: {newValue}/{maxHealth}");
    }

    /// <summary>
    /// Apply damage to this entity. Should only be called on the Server.
    /// </summary>
    /// <param name="amount">Amount of damage to take</param>
    public void TakeDamage(float amount)
    {
        if (!IsServer) return;

        if (currentHealth.Value <= 0) return; // Already dead

        currentHealth.Value = Mathf.Clamp(currentHealth.Value - amount, 0, maxHealth);

        if (currentHealth.Value <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Heal this entity. Should only be called on the Server.
    /// </summary>
    /// <param name="amount">Amount of health to restore</param>
    public void Heal(float amount)
    {
        if (!IsServer) return;
            
        if (currentHealth.Value <= 0) return; // Cannot heal dead? Or maybe yes. usually no.

        currentHealth.Value = Mathf.Clamp(currentHealth.Value + amount, 0, maxHealth);
    }

    private void Die()
    {
        OnDeath?.Invoke();
        if (debugLogs) Debug.Log($"{name} died.");

        // Custom death logic here or via event
        if (destroyOnDeath && IsServer)
        {
            // Optional: NetworkObject.Despawn(); 
            // Careful with destroying players
        }
    }
}