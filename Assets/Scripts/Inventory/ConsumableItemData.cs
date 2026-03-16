using UnityEngine;
using NaughtyAttributes;
using Unity.Netcode;

namespace Inventory
{
    [CreateAssetMenu(fileName = "New Consumable", menuName = "FatalFoundation/Items/Consumable")]
    public class ConsumableItemData : ItemData
    {
        [BoxGroup("Consumable Stats")]
        [SerializeField] private float healAmount = 0f;
        [BoxGroup("Consumable Stats")]
        [SerializeField] private float staminaAmount = 0f;

        [BoxGroup("Effects")]
        [SerializeField] private AudioClip consumeSound;
        [BoxGroup("Effects")]
        [SerializeField] private GameObject consumeVFX;

        public override bool Use(GameObject character)
        {
            base.Use(character);

            if (character == null) return false;
            
            // Should add audio/vfx here if needed, usually on client.
            // But we don't have easy access to AudioSource on character from here without GetComponent.
            // Let's rely on PlayerInventory or another system for effects for now, 
            // or play sound at point.

            var netObj = character.GetComponent<NetworkObject>();
            if (netObj == null) return false;

            // Health (Server Authoritative)
            if (NetworkManager.Singleton.IsServer && healAmount > 0)
            {
                var health = character.GetComponent<HealthSystem>();
                if (health != null)
                {
                    health.Heal(healAmount);
                    Debug.Log($"[Consumable] Healed {character.name} for {healAmount}");
                }
            }

            // Stamina (Owner Authoritative)
            if (netObj.IsOwner && staminaAmount > 0)
            {
                var stamina = character.GetComponent<StaminaSystem>();
                if (stamina != null)
                {
                    stamina.RestoreStamina(staminaAmount);
                    Debug.Log($"[Consumable] Restored Stamina for {character.name}: {staminaAmount}");
                }
            }

            return true; // Simple consumable always consumed
        }
    }
}



