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

        [Header("Visual Feedback")]
        [Tooltip("Outline หรือ Glow Effect — เปิด/ปิดเมื่อ Player มองมา")]
        public GameObject highlightEffect;

        [Header("Bob & Rotate Animation")]
        [Tooltip("เปิดใช้แอนิเมชั่น ลอย + หมุน")]
        public bool enableBobAnimation = true;

        [Tooltip("ความสูงที่ไอเทมลอยขึ้น-ลง (เมตร)")]
        public float bobHeight = 0.15f;

        [Tooltip("ความเร็วการลอย")]
        public float bobSpeed = 1.5f;

        [Tooltip("ความเร็วการหมุน (องศา/วินาที)")]
        public float rotateSpeed = 90f;

        // ─── Private Fields ───────────────────────────────────────────────────
        private Vector3 _startPosition;
        private float _bobTimer;

        // ─── Unity Lifecycle ──────────────────────────────────────────────────
        private void Start()
        {
            _startPosition = transform.position;

            // ซ่อน highlight เริ่มต้น
            if (highlightEffect != null)
                highlightEffect.SetActive(false);
        }

        private void Update()
        {
            if (!enableBobAnimation) return;

            // ── Bob (ลอยขึ้น-ลง) ──
            _bobTimer += Time.deltaTime * bobSpeed;
            float newY = _startPosition.y + Mathf.Sin(_bobTimer) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);

            // ── Rotate ──
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        }

        // ─── Public Methods ───────────────────────────────────────────────────

        /// <summary>
        /// เรียกโดย InteractionSystem เมื่อ Player กด E
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
                Destroy(gameObject);
            }
            else
            {
                Debug.Log($"[WorldItem] กระเป๋าเต็ม ไม่สามารถเก็บ '{itemData.itemName}' ได้");
            }
        }

        /// <summary>
        /// เปิด/ปิด Highlight Effect — เรียกโดย InteractionSystem อัตโนมัติ
        /// </summary>
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

#if UNITY_EDITOR
            if (itemData != null)
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 0.5f,
                    $"{itemData.itemName}\n{itemData.weight}kg | ${itemData.scrapValue}"
                );
#endif
        }
    }
}
