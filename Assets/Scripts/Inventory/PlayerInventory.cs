using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;
using NaughtyAttributes;

namespace Inventory
{
    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        [BoxGroup("Hand Anchor")]
        public Transform handAnchor;

        [BoxGroup("Weight Settings")]
        public float weightMultiplier = 0.5f;
        [BoxGroup("Weight Settings")]
        public float minMoveSpeed = 0.5f;

        private const int SLOT_COUNT = 4;
        private ItemData[] _slots = new ItemData[SLOT_COUNT];
        private int _currentSlotIndex = 0;
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

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
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

            newIndex = ((newIndex % SLOT_COUNT) + SLOT_COUNT) % SLOT_COUNT;

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

