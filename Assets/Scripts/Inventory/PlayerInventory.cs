using StarterAssets;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using NaughtyAttributes;
using StarterAssets.FirstPersonController.Scripts;

namespace Inventory
{
    // เปลี่ยนจาก MonoBehaviour เป็น NetworkBehaviour เพื่อใช้ IsOwner / OnNetworkSpawn
    public class PlayerInventory : NetworkBehaviour
    {
        /// <summary>Instance ของผู้เล่นบนเครื่องนี้เท่านั้น (IsOwner)</summary>
        public static PlayerInventory Instance { get; private set; }

        /// <summary>ถูก invoke เมื่อ local player's inventory พร้อมใช้งาน</summary>
        public static event System.Action<PlayerInventory> OnLocalInstanceReady;

        [BoxGroup("Hand Anchor")]
        [SerializeField] private Transform handAnchor;

        [BoxGroup("Weight Settings")]
        [SerializeField] private float weightMultiplier = 0.5f;
        [BoxGroup("Weight Settings")]
        [SerializeField] private float minMoveSpeed = 0.5f;

        private const int SlotCount = 4;
        private ItemData[] _slots = new ItemData[SlotCount];
        private int _currentSlotIndex;
        private GameObject _currentHandObject;

        private FirstPersonController _fpsController;
        private float _baseMoveSpeed;
        private float _baseSprintSpeed;

        public int CurrentSlotIndex => _currentSlotIndex;
        public ItemData[] Slots => _slots;

        public float TotalWeight
        {
            get
            {
                float total = 0f;
                foreach (var item in _slots)
                    if (item != null) total += item.weight;
                return total;
            }
        }

        public event System.Action OnInventoryChanged;
        public event System.Action<int> OnSlotChanged;

        // Awake: ไม่ตั้ง singleton ที่นี่อีกต่อไป
        // (เดิมมี Destroy(gameObject) ซึ่งจะทำลาย remote player ทันที)
        private void Awake() { /* slots initialized by field initializer */ }

        // NGO เรียก OnNetworkSpawn หลัง NetworkObject spawn
        // ทำงานก่อน Start() จึงปลอดภัยกว่า Awake สำหรับการเช็ค ownership
        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                // Remote player: ปิด component นี้ — ไม่ต้องรัน Update, Start ฯลฯ
                enabled = false;
                return;
            }

            // Local owner player: ลงทะเบียน singleton และแจ้ง InventoryUI
            Instance = this;
            OnLocalInstanceReady?.Invoke(this);

            // เตรียม weight system (เดิมอยู่ใน Start)
            _fpsController = GetComponent<FirstPersonController>();
            if (_fpsController != null)
            {
                _baseMoveSpeed   = _fpsController.MoveSpeed;
                _baseSprintSpeed = _fpsController.SprintSpeed;
            }
            else
            {
                Debug.LogWarning("[PlayerInventory] ไม่พบ FirstPersonController — ระบบน้ำหนักจะไม่ทำงาน");
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner && Instance == this)
                Instance = null;
        }

        // Start ถูกลบ — ย้าย logic ไปที่ OnNetworkSpawn แล้ว
        // Update ทำงานเฉพาะเมื่อ enabled = true (owner เท่านั้น)
        private void Update()
        {
            HandleScrollInput();
            HandleDropInput();
            ApplyWeightPenalty();
        }

        private void HandleScrollInput()
        {
            float scrollY = 0f;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                scrollY = Mouse.current.scroll.ReadValue().y;
#else
            scrollY = Input.GetAxis("Mouse ScrollWheel");
#endif
            if (scrollY > 0f)      SwitchSlot(_currentSlotIndex - 1);
            else if (scrollY < 0f) SwitchSlot(_currentSlotIndex + 1);
        }

        private void HandleDropInput()
        {
            bool dropPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
                dropPressed = Keyboard.current.gKey.wasPressedThisFrame;
#else
            dropPressed = Input.GetKeyDown(KeyCode.G);
#endif
            if (dropPressed) DropItem();
        }

        public void SwitchSlot(int newIndex)
        {
            if (_slots[_currentSlotIndex] != null && _slots[_currentSlotIndex].isTwoHanded)
            {
                Debug.Log("[PlayerInventory] ไม่สามารถสลับ Slot ได้ — กำลังถือของสองมือ (ทิ้งก่อน)");
                return;
            }

            newIndex = ((newIndex % SlotCount) + SlotCount) % SlotCount;
            if (newIndex == _currentSlotIndex) return;

            _currentSlotIndex = newIndex;
            UpdateHandItem();
            OnSlotChanged?.Invoke(_currentSlotIndex);
        }

        public bool PickUpItem(ItemData item)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null)
                {
                    _slots[i] = item;
                    if (i == _currentSlotIndex) UpdateHandItem();
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
            Debug.Log("[PlayerInventory] กระเป๋าเต็ม! ไม่สามารถเก็บได้");
            return false;
        }

        public void DropItem()
        {
            if (_slots[_currentSlotIndex] == null) return;

            ItemData droppedItem = _slots[_currentSlotIndex];
            if (droppedItem.worldPrefab != null)
            {
                Vector3 dropPos = transform.position + transform.forward * 1.5f;
                Instantiate(droppedItem.worldPrefab, dropPos, Quaternion.identity);
            }

            _slots[_currentSlotIndex] = null;
            UpdateHandItem();
            OnInventoryChanged?.Invoke();
        }

        private void UpdateHandItem()
        {
            if (_currentHandObject != null)
                Destroy(_currentHandObject);

            ItemData currentItem = _slots[_currentSlotIndex];
            if (currentItem != null && currentItem.handPrefab != null && handAnchor != null)
            {
                _currentHandObject = Instantiate(currentItem.handPrefab, handAnchor);
                _currentHandObject.transform.localPosition = Vector3.zero;
                _currentHandObject.transform.localRotation = Quaternion.identity;
            }
        }

        private void ApplyWeightPenalty()
        {
            if (_fpsController == null) return;
            float penalty = TotalWeight * weightMultiplier;
            _fpsController.MoveSpeed   = Mathf.Max(minMoveSpeed, _baseMoveSpeed   - penalty);
            _fpsController.SprintSpeed = Mathf.Max(minMoveSpeed, _baseSprintSpeed - penalty);
        }
    }
}
