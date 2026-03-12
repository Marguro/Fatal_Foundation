using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes;

namespace Inventory
{
    [RequireComponent(typeof(CanvasGroup))]
    public class InventoryUI : MonoBehaviour
    {
        [BoxGroup("Slot Background Images (4 slots)")]
        [SerializeField] private Image[] slotBackgrounds = new Image[4];

        [BoxGroup("Item Icon Images (4 slots)")]
        [SerializeField] private Image[] itemIcons = new Image[4];

        [BoxGroup("Slot Highlight Settings")]
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.5f);

        [BoxGroup("Slot Highlight Settings")]
        [SerializeField] private Color selectedColor = new Color(1f, 0.9f, 0.2f, 1f);

        [BoxGroup("Slot Highlight Settings")]
        [SerializeField] private float selectedScale = 1.15f;

        private CanvasGroup _canvasGroup;
        private PlayerInventory _inventory;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 1f;
        }

        private void Start()
        {
            _inventory = PlayerInventory.Instance;

            if (_inventory == null)
            {
                Debug.LogError("[InventoryUI] not found PlayerInventory.Instance — ตรวจสอบว่า PlayerInventory อยู่ใน Scene");
                return;
            }

            _inventory.OnInventoryChanged += RefreshUI;
            _inventory.OnSlotChanged      += OnSlotChanged;

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

        private void OnSlotChanged(int index)
        {
            UpdateSlotHighlights();
        }

        private void RefreshUI()
        {
            UpdateItemIcons();
            UpdateSlotHighlights();
        }

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

        private void UpdateSlotHighlights()
        {
            for (int i = 0; i < slotBackgrounds.Length; i++)
            {
                if (slotBackgrounds[i] == null) continue;

                bool isSelected = (i == _inventory.CurrentSlotIndex);
                slotBackgrounds[i].color                = isSelected ? selectedColor : normalColor;
                slotBackgrounds[i].transform.localScale = Vector3.one * (isSelected ? selectedScale : 1f);
            }
        }
    }
}
