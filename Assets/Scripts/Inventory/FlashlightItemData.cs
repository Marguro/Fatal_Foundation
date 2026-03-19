using UnityEngine;

namespace Inventory
{
    [CreateAssetMenu(fileName = "New Flashlight", menuName = "FatalFoundation/Items/Flashlight")]
    public class FlashlightItemData : ItemData
    {
        public override bool Use(GameObject character)
        {
            // Flashlight uses F toggle via PlayerInventory, not primary use input.
            return false;
        }
    }
}

