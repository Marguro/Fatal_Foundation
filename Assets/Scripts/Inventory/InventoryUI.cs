using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FatalFoundation
{
    /// <summary>
    /// จัดการ UI ของ Inventory — 4 Slot Images, Highlight, และ Fade Auto-hide
    /// ติดกับ Canvas GameObject (ต้องมี CanvasGroup Component)
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class InventoryUI : MonoBehaviour
    {
        // ─── Inspector Fields ─────────────────────────────────────────────────
        [Header("Slot Background Images (4 slots)")]
        [Tooltip("Image พื้นหลังของแต่ละ Slot — ลาก 4 Image มาใส่")]
        public Image[] slotBackgrounds = new Image[4];

        [Header("Item Icon Images (4 slots)")]
        [Tooltip("Image ไอคอนไอเทมของแต่ละ Slot — ลาก 4 Image มาใส่")]
        public Image[] itemIcons = new Image[4];

        [Header("Slot Highlight Settings")]
        [Tooltip("สีปกติของ Slot")]
        public Color normalColor = new Color(1f, 1f, 1f, 0.5f);

        [Tooltip("สีเมื่อ Slot นั้นถูกเลือก")]
        public Color selectedColor = new Color(1f, 0.9f, 0.2f, 1f);

        [Tooltip("ขนาด Scale เมื่อ Slot ถูกเลือก")]
        public float selectedScale = 1.15f;

        [Header("Fade / Auto-hide Settings")]
        [Tooltip("จำนวนวินาทีที่ UI จะแสดงก่อนเริ่มจาง")]
        public float displayDuration = 3f;

        [Tooltip("ระยะเวลา (วินาที) ในการ Fade out")]
        public float fadeDuration = 0.5f;

        // ─── Private Fields ───────────────────────────────────────────────────
        private CanvasGroup _canvasGroup;
        private PlayerInventory _inventory;
        private Coroutine _fadeCoroutine;

        // ─── Unity Lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f; // ซ่อน UI ตั้งแต่เริ่ม
        }

        private void Start()
        {
            _inventory = PlayerInventory.Instance;

            if (_inventory == null)
            {
                Debug.LogError("[InventoryUI] ไม่พบ PlayerInventory.Instance — ตรวจสอบว่า PlayerInventory อยู่ใน Scene");
                return;
            }

            // Subscribe Events
            _inventory.OnInventoryChanged += RefreshUI;
            _inventory.OnSlotChanged      += OnSlotChanged;

            // แสดงสถานะเริ่มต้น
            RefreshUI();
        }

        private void OnDestroy()
        {
            if (_inventory != null)
            {
                _inventory.OnInventoryChanged -= RefreshUI;
                _inventory.OnSlotChanged      -= OnSlotChanged;
            }
        }

        // ─── Event Callbacks ──────────────────────────────────────────────────

        /// <summary>เรียกเมื่อสลับ Slot — อัปเดต Highlight แล้วแสดง UI</summary>
        private void OnSlotChanged(int index)
        {
            UpdateSlotHighlights();
            ShowUI();
        }

        /// <summary>เรียกเมื่อ Inventory เปลี่ยน — อัปเดต Icons แล้วแสดง UI</summary>
        private void RefreshUI()
        {
            UpdateItemIcons();
            UpdateSlotHighlights();
            ShowUI();
        }

        // ─── UI Update Methods ────────────────────────────────────────────────

        /// <summary>อัปเดต Sprite ไอคอนในแต่ละ Slot ตามข้อมูล Inventory</summary>
        private void UpdateItemIcons()
        {
            for (int i = 0; i < itemIcons.Length; i++)
            {
                if (itemIcons[i] == null) continue;

                ItemData item = _inventory.Slots[i];
                if (item != null && item.itemIcon != null)
                {
                    itemIcons[i].sprite  = item.itemIcon;
                    itemIcons[i].enabled = true;
                }
                else
                {
                    itemIcons[i].sprite  = null;
                    itemIcons[i].enabled = false;
                }
            }
        }

        /// <summary>เปลี่ยนสีและขนาดของ Slot Background ตาม Slot ที่เลือก</summary>
        private void UpdateSlotHighlights()
        {
            for (int i = 0; i < slotBackgrounds.Length; i++)
            {
                if (slotBackgrounds[i] == null) continue;

                bool isSelected = (i == _inventory.CurrentSlotIndex);
                slotBackgrounds[i].color                    = isSelected ? selectedColor : normalColor;
                slotBackgrounds[i].transform.localScale     = Vector3.one * (isSelected ? selectedScale : 1f);
            }
        }

        // ─── Fade System ──────────────────────────────────────────────────────

        /// <summary>แสดง UI ทันที แล้วเริ่ม Timer เพื่อ Fade out</summary>
        private void ShowUI()
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(ShowAndFadeCoroutine());
        }

        /// <summary>
        /// Coroutine: แสดง UI (alpha=1) ค้างไว้ displayDuration วินาที
        /// แล้วค่อยๆ Fade out ในช่วง fadeDuration วินาที
        /// </summary>
        private IEnumerator ShowAndFadeCoroutine()
        {
            // แสดงทันที
            _canvasGroup.alpha = 1f;

            // รอ displayDuration วินาที
            yield return new WaitForSeconds(displayDuration);

            // Fade out ทีละนิด
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed            += Time.deltaTime;
                _canvasGroup.alpha  = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                yield return null;
            }

            _canvasGroup.alpha = 0f;
            _fadeCoroutine     = null;
        }
    }
}

