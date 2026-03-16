using UnityEngine;
using Unity.Netcode;
using System;

namespace Systems
{
    public class HealthSystem : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private bool destroyOnDeath = true;

        [SerializeField] private bool debugLogs;

        public NetworkVariable<float> CurrentHealth = new NetworkVariable<float>(
            100f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public event Action<float, float> OnHealthChanged;
        public event Action OnDeath;

        public float MaxHealth => maxHealth;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                CurrentHealth.Value = maxHealth;
            }

            CurrentHealth.OnValueChanged += OnHealthChangedCallback;
            // Initial update
            OnHealthChanged?.Invoke(CurrentHealth.Value, maxHealth);
        }

        public override void OnNetworkDespawn()
        {
            CurrentHealth.OnValueChanged -= OnHealthChangedCallback;
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

            if (CurrentHealth.Value <= 0) return; // Already dead

            CurrentHealth.Value = Mathf.Clamp(CurrentHealth.Value - amount, 0, maxHealth);

            if (CurrentHealth.Value <= 0)
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
            
            if (CurrentHealth.Value <= 0) return; // Cannot heal dead? Or maybe yes. usually no.

            CurrentHealth.Value = Mathf.Clamp(CurrentHealth.Value + amount, 0, maxHealth);
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
}


