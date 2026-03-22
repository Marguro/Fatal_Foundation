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
        [ShowAssetPreview]
        public Sprite itemIcon;

        [BoxGroup("Prefabs")]
        [ShowAssetPreview]
        public GameObject worldPrefab;
        [BoxGroup("Prefabs")]
        [ShowAssetPreview]
        public GameObject handPrefab;

        [BoxGroup("Item Properties")]
        public float weight = 1f;
        [BoxGroup("Item Properties")]
        public int scrapValue;
        [BoxGroup("Item Properties")]
        public bool isTwoHanded;

        [BoxGroup("Hand Anchor")]
        [Tooltip("When enabled, this item overrides HandAnchor rotation while equipped.")]
        public bool useHandAnchorRotationOverride;
        [BoxGroup("Hand Anchor")]
        [Tooltip("Rotation (Euler) relative to lookPitchSource while this item is equipped.")]
        public Vector3 handAnchorRotationEuler;
        [BoxGroup("Hand Item")]
        [Tooltip("Local position applied to the instantiated hand item GameObject (not HandAnchor).")]
        public Vector3 handItemLocalPosition;

        public virtual bool Use(GameObject character)
        {
            Debug.Log($"Using item: {itemName}");
            return false;
        }
    }
}

