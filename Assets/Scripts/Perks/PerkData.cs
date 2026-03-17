using UnityEngine;
using NaughtyAttributes;

namespace Perks
{
    [CreateAssetMenu(fileName = "New Perk", menuName = "FatalFoundation/Perks/Perk")]
    public class PerkData : ScriptableObject
    {
        [BoxGroup("Basic Info")]
        [SerializeField] public string perkName = "Unknown Perk";
        [BoxGroup("Basic Info")]
        [TextArea(3, 5)]
        [SerializeField] public string description = "No description";
        [BoxGroup("Basic Info")]
        [ShowAssetPreview]
        [SerializeField] public Sprite perkIcon;

        [BoxGroup("Cost")]
        [SerializeField] public int soulOrbCost = 1;

        [BoxGroup("Stats")]
        [SerializeField] public float healthBonus = 0f;
        [BoxGroup("Stats")]
        [SerializeField] public float damageBonus = 0f;
        [BoxGroup("Stats")]
        [SerializeField] public float speedBonus = 0f;
        [BoxGroup("Stats")]
        [SerializeField] public float staminaBonus = 0f;

        /// <summary>
        /// Called when the perk is selected/applied to the player
        /// </summary>
        public virtual void ApplyPerk(GameObject character)
        {
            if (character == null) return;

            Debug.Log($"[Perk] Applied perk: {perkName} to {character.name}");

            // Health bonus
            if (healthBonus > 0)
            {
                var health = character.GetComponent<HealthSystem>();
                if (health != null)
                {
                    health.Heal(healthBonus);
                    Debug.Log($"[Perk] {perkName} healed {character.name} for {healthBonus}");
                }
            }

            // You can add more bonus applications here as needed
            // For example: speed, damage multiplier, etc.
        }
    }
}

