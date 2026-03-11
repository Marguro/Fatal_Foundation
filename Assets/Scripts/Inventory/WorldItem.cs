using UnityEngine;

namespace FatalFoundation
{
    /// <summary>
    /// ติดกับ GameObject ไอเทมที่วางอยู่ในโลก
    /// เมื่อ Player กด E จะเรียก Interact() เพื่อเก็บไอเทมเข้า Inventory
    /// </summary>
    public class WorldItem : MonoBehaviour
    {
        [Header("Item Data")]
        [Tooltip("ลาก ItemData (ScriptableObject) มาใส่ที่นี่")]
        public ItemData itemData;

        [Header("Visual Feedback (Optional)")]
        [Tooltip("Outline หรือ Glow Effect — เปิด/ปิดเมื่อ Player มองมา (ไม่จำเป็น)")]
        public GameObject highlightEffect;

        // ─── Public Methods ───────────────────────────────────────────────────

        /// <summary>
        /// เรียกโดย InteractionSystem เมื่อ Player กด E
        /// ลองเก็บไอเทมเข้า Inventory — ถ้าสำเร็จจะ Destroy GameObject นี้
        /// </summary>
        public void Interact()
        {
            if (itemData == null)
            {
                Debug.LogWarning($"[WorldItem] {gameObject.name} ไม่มี ItemData — กรุณา Assign ใน Inspector");
                return;
            }

            if (PlayerInventory.Instance == null)
            {
                Debug.LogWarning("[WorldItem] ไม่พบ PlayerInventory.Instance ใน Scene");
                return;
            }

            bool pickedUp = PlayerInventory.Instance.PickUpItem(itemData);

            if (pickedUp)
            {
                // ลบออกจาก Scene
                Destroy(gameObject);
            }
            else
            {
                Debug.Log($"[WorldItem] กระเป๋าเต็ม ไม่สามารถเก็บ '{itemData.itemName}' ได้");
            }
        }

        /// <summary>เปิด/ปิด Highlight Effect (เรียกจาก InteractionSystem ถ้าต้องการ)</summary>
        public void SetHighlight(bool active)
        {
            if (highlightEffect != null)
                highlightEffect.SetActive(active);
        }

        // ─── Gizmos (Editor Debug) ────────────────────────────────────────────
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.3f);

            if (itemData != null)
            {
#if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 0.5f,
                    $"{itemData.itemName}\n{itemData.weight}kg | ${itemData.scrapValue}"
                );
#endif
            }
        }
    }
}

