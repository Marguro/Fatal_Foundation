using UnityEngine;
using NaughtyAttributes;

namespace Inventory
{
    [CreateAssetMenu(fileName = "New SoulOrbItemData", menuName = "FatalFoundation/SoulOrbItemData")]
    public class SoulOrbItemData : ItemData
    {
        [BoxGroup("Soul Orb Info")]
        [Tooltip("How much Soul Orb currency this item provides if consumed directly (optional).")]
        public int soulOrbValue = 1;

        public override bool Use(GameObject character)
        {
            // Optional: If you want the player to "Consume" the Soul Orb from their hand
            // to turn it into currency. If they just need to carry it to escape, leave this empty or return false.
            Debug.Log($"[{itemName}] Cannot be used directly, must be carried to escape.");
            return false;
        }
    }
}

