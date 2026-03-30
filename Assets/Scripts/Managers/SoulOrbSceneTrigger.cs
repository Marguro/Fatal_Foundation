using UnityEngine;
using Unity.Netcode;
using Enemies;
using Inventory;

namespace Managers
{
    [RequireComponent(typeof(NetworkObject))]
    public class SoulOrbSceneTrigger : NetworkBehaviour
    {
        [Header("Patrol Settings")]
        [Tooltip("New patrol points to assign to the Enemy Patrol when SoulOrb is picked up.")]
        [SerializeField] private Transform[] newPatrolPoints;

        [Header("Environment Changes")]
        [Tooltip("The GameObject (like lights) to deactivate when SoulOrb is picked up.")]
        [SerializeField] private GameObject objectToDeactivate;

        [Header("Target Item")]
        [Tooltip("The name of the item that triggers this event when picked up (e.g., 'SoulOrb').")]
        [SerializeField] private string targetItemName = "SoulOrb";

        private NetworkVariable<bool> _isTriggered = new NetworkVariable<bool>();

        public override void OnNetworkSpawn()
        {
            PlayerInventory.OnItemPickedUpEvent += OnItemPickedUp;
            _isTriggered.OnValueChanged += OnTriggerStateChanged;
            
            // Sync current state for late joiners
            if (_isTriggered.Value)
            {
                ApplyVisualChanges();
            }
        }

        public override void OnNetworkDespawn()
        {
            PlayerInventory.OnItemPickedUpEvent -= OnItemPickedUp;
            _isTriggered.OnValueChanged -= OnTriggerStateChanged;
        }

        private void OnTriggerStateChanged(bool previousValue, bool newValue)
        {
            if (newValue)
            {
                ApplyVisualChanges();
            }
        }

        private void OnItemPickedUp(ItemData item)
        {
            if (item != null && item.itemName == targetItemName && IsSpawned)
            {
                if (!IsServer)
                {
                    // If client picked it up, ask server to trigger
                    RequestTriggerServerRpc();
                }
                else
                {
                    TriggerChangesServer();
                }
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestTriggerServerRpc()
        {
            TriggerChangesServer();
        }

        private void TriggerChangesServer()
        {
            if (!IsServer) return;
            if (_isTriggered.Value) return; // Prevent multiple triggers
            
            _isTriggered.Value = true;

            PatrolChaserAI[] activeEnemies = Object.FindObjectsByType<PatrolChaserAI>(FindObjectsSortMode.None);
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null)
                {
                    enemy.SetPatrolPoints(newPatrolPoints);
                }
            }

            Debug.Log("[SoulOrbSceneTrigger] Server: Changed enemy patrol points and updated network state.");
        }

        /// <summary>
        /// Can also be called directly by a UnityEvent on an Interactable if preferred.
        /// </summary>
        public void TriggerChanges()
        {
            if (IsServer)
            {
                TriggerChangesServer();
            }
            else
            {
                RequestTriggerServerRpc();
            }
        }

        private void ApplyVisualChanges()
        {
            if (objectToDeactivate != null)
            {
                objectToDeactivate.SetActive(false);
            }
            Debug.Log("[SoulOrbSceneTrigger] Applied visual changes (deactivated object) locally.");
        }
    }
}
