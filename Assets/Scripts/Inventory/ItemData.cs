using UnityEngine;
using NaughtyAttributes;

namespace Inventory
{
    [CreateAssetMenu(fileName = "New ItemData", menuName = "FatalFoundation/ItemData")]
    public class ItemData : ScriptableObject
    {
        [BoxGroup("Basic Info")]
        public string itemName = "Unknown Item";
        [BoxGroup("Basic Info")]
        public Sprite itemIcon;

        [BoxGroup("Prefabs")]
        public GameObject worldPrefab;
        [BoxGroup("Prefabs")]
        public GameObject handPrefab;

        [BoxGroup("Item Properties")]
        public float weight = 1f;
        [BoxGroup("Item Properties")]
        public int scrapValue;
        [BoxGroup("Item Properties")]
        public bool isTwoHanded;
    }
}

