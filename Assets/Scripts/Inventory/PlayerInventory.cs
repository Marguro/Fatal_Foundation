using UnityEngine;
using StarterAssets;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FatalFoundation
{
    /// <summary>
    /// Core Inventory Logic — ติดกับ GameObject ของ Player
    /// จัดการ 4 ช่อง, การสลับ Slot, การเก็บ/ทิ้งไอเทม และน้ำหนัก
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        // ─── Singleton ────────────────────────────────────────────────────────
        public static PlayerInventory Instance { get; private set; }

        // ─── Inspector Fields ─────────────────────────────────────────────────
        [Header("Hand Anchor")]
        [Tooltip("Transform ตำแหน่งที่จะ Spawn handPrefab (เช่น กระดูกมือของ Player)")]
        public Transform handAnchor;

        [Header("Weight Settings")]
        [Tooltip("ตัวคูณน้ำหนัก: finalSpeed = baseSpeed - (TotalWeight * weightMultiplier)")]
        public float weightMultiplier = 0.5f;

        [Tooltip("ความเร็วขั้นต่ำ — ป้องกันการที่ Player ติดที่หรือเดินถอยหลัง")]
        public float minMoveSpeed = 0.5f;

        // ─── Private Fields ───────────────────────────────────────────────────
        private const int SLOT_COUNT = 4;
        private ItemData[] _slots = new ItemData[SLOT_COUNT];
        private int _currentSlotIndex = 0;
        private GameObject _currentHandObject;

        // อ้างอิง FirstPersonController เพื่อปรับ speed
        private FirstPersonController _fpsController;
        private float _baseMoveSpeed;
        private float _baseSprintSpeed;

        // ─── Public Properties ────────────────────────────────────────────────
        /// <summary>Index ของ Slot ที่ถืออยู่ (0-3)</summary>
        public int CurrentSlotIndex => _currentSlotIndex;

        /// <summary>Array ของไอเทมทั้ง 4 ช่อง (read-only)</summary>
        public ItemData[] Slots => _slots;

        /// <summary>รวมน้ำหนักไอเทมทุกชิ้นในกระเป๋า</summary>
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

        // ─── Events (ใช้โดย InventoryUI) ──────────────────────────────────────
        /// <summary>เรียกเมื่อเนื้อหา Inventory เปลี่ยน (เก็บ/ทิ้ง)</summary>
        public event System.Action OnInventoryChanged;

        /// <summary>เรียกเมื่อสลับ Slot พร้อมส่ง index ใหม่</summary>
        public event System.Action<int> OnSlotChanged;

        // ─── Unity Lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // หา FirstPersonController บน GameObject เดียวกัน
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

        private void Update()
        {
            HandleScrollInput();
            HandleDropInput();
            ApplyWeightPenalty();
        }

        // ─── Input Handling ───────────────────────────────────────────────────

        /// <summary>อ่าน Scroll Wheel แล้วสลับ Slot</summary>
        private void HandleScrollInput()
        {
            float scrollY = 0f;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                scrollY = Mouse.current.scroll.ReadValue().y;
#else
            scrollY = Input.GetAxis("Mouse ScrollWheel");
#endif

            if (scrollY > 0f)       SwitchSlot(_currentSlotIndex - 1);
            else if (scrollY < 0f)  SwitchSlot(_currentSlotIndex + 1);
        }

        /// <summary>กด G เพื่อทิ้งไอเทมที่ถืออยู่</summary>
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

        // ─── Slot Switching ───────────────────────────────────────────────────

        /// <summary>
        /// สลับไปยัง Slot ที่กำหนด
        /// ถ้า Slot ปัจจุบันถือของสองมืออยู่จะไม่สามารถสลับได้
        /// </summary>
        public void SwitchSlot(int newIndex)
        {
            // ตรวจสอบ Two-Handed lock
            if (_slots[_currentSlotIndex] != null && _slots[_currentSlotIndex].isTwoHanded)
            {
                Debug.Log("[PlayerInventory] ไม่สามารถสลับ Slot ได้ — กำลังถือของสองมือ (ทิ้งก่อน)");
                return;
            }

            // Wrap-around (4 → 0, -1 → 3)
            newIndex = ((newIndex % SLOT_COUNT) + SLOT_COUNT) % SLOT_COUNT;

            if (newIndex == _currentSlotIndex) return;

            _currentSlotIndex = newIndex;
            UpdateHandItem();
            OnSlotChanged?.Invoke(_currentSlotIndex);
        }

        // ─── Item Management ──────────────────────────────────────────────────

        /// <summary>
        /// เก็บไอเทม — หาช่องว่างแรกที่มีแล้วใส่
        /// Return: true = สำเร็จ, false = กระเป๋าเต็ม
        /// </summary>
        public bool PickUpItem(ItemData item)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null)
                {
                    _slots[i] = item;

                    // ถ้าเก็บลงใน Slot ที่ถืออยู่ให้อัปเดต hand object ด้วย
                    if (i == _currentSlotIndex) UpdateHandItem();

                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }

            Debug.Log("[PlayerInventory] กระเป๋าเต็ม! ไม่สามารถเก็บได้");
            return false;
        }

        /// <summary>
        /// ทิ้งไอเทมจาก Slot ปัจจุบัน — Spawn worldPrefab ข้างหน้า Player
        /// </summary>
        public void DropItem()
        {
            if (_slots[_currentSlotIndex] == null) return;

            ItemData droppedItem = _slots[_currentSlotIndex];

            // Spawn worldPrefab หน้า Player
            if (droppedItem.worldPrefab != null)
            {
                Vector3 dropPos = transform.position + transform.forward * 1.5f;
                Instantiate(droppedItem.worldPrefab, dropPos, Quaternion.identity);
            }

            // ล้าง Slot
            _slots[_currentSlotIndex] = null;
            UpdateHandItem();
            OnInventoryChanged?.Invoke();
        }

        // ─── Hand Visuals ─────────────────────────────────────────────────────

        /// <summary>
        /// Destroy hand object เดิม แล้ว Spawn ของใน Slot ปัจจุบัน (ถ้ามี)
        /// </summary>
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

        // ─── Weight Penalty ───────────────────────────────────────────────────

        /// <summary>
        /// ปรับ MoveSpeed และ SprintSpeed ของ FirstPersonController ตามน้ำหนักรวม
        /// สูตร: finalSpeed = baseSpeed - (TotalWeight * weightMultiplier)
        /// </summary>
        private void ApplyWeightPenalty()
        {
            if (_fpsController == null) return;

            float penalty = TotalWeight * weightMultiplier;
            _fpsController.MoveSpeed   = Mathf.Max(minMoveSpeed, _baseMoveSpeed   - penalty);
            _fpsController.SprintSpeed = Mathf.Max(minMoveSpeed, _baseSprintSpeed - penalty);
        }
    }
}

