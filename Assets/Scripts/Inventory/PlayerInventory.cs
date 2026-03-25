using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using NaughtyAttributes;
using StarterAssets.FirstPersonController.Scripts;
using Unity.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Inventory
{
    public class PlayerInventory : NetworkBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        public static event System.Action<PlayerInventory> OnLocalInstanceReady;

        [BoxGroup("Hand Anchor")]
        [SerializeField] private Transform handAnchor;
        [BoxGroup("Hand Anchor")]
        [SerializeField] private bool followCameraPitch = true;
        [BoxGroup("Hand Anchor")]
        [SerializeField] private Transform lookPitchSource;

        [BoxGroup("Items Database")]
        [SerializeField] private List<ItemData> allGameItems = new List<ItemData>();
        [BoxGroup("Items Database")]
        [SerializeField] private ItemData flashlightItemData;
        private readonly NetworkVariable<FixedString64Bytes> _netCurrentItemName = new NetworkVariable<FixedString64Bytes>();
        private readonly NetworkVariable<bool> _netFlashlightEnabled = new NetworkVariable<bool>();

        [BoxGroup("Weight Settings")]
        [SerializeField] private float weightMultiplier = 0.5f;
        [BoxGroup("Weight Settings")]
        [SerializeField] private float minMoveSpeed = 0.5f;

        [BoxGroup("Drop Settings")]
        [SerializeField] private float dropForwardDistance = 1.25f;
        [BoxGroup("Drop Settings")]
        [SerializeField] private float dropRaycastStartHeight = 1.5f;
        [BoxGroup("Drop Settings")]
        [SerializeField] private float dropRaycastDistance = 5f;
        [BoxGroup("Drop Settings")]
        [SerializeField] private float dropGroundOffset = 0.05f;
        [BoxGroup("Drop Settings")]
        [SerializeField] private LayerMask dropGroundMask = ~0;

        private const int SlotCount = 4;
        private ItemData[] _slots = new ItemData[SlotCount];
        private int _currentSlotIndex;
        private GameObject _currentHandObject;
        private Light[] _currentHandLights;

        private FirstPersonController _fpsController;
        private float _baseMoveSpeed;
        private float _baseSprintSpeed;
        private Vector3 _handAnchorLocalPosFromLookSource;
        private Quaternion _handAnchorLocalRotFromLookSource;
        private bool _hasHandAnchorOffset;
        private bool _appliedFlashlightState;

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

        public override void OnNetworkSpawn()
        {
            _netCurrentItemName.OnValueChanged += OnHeldItemChanged;
            _netFlashlightEnabled.OnValueChanged += OnFlashlightStateChanged;

            if (!IsOwner)
            {
                if (!_netCurrentItemName.Value.IsEmpty)
                    UpdateRemoteHandVisual(_netCurrentItemName.Value.ToString());

                ApplyCurrentHandFlashlightState(_netFlashlightEnabled.Value);
                return;
            }

            Instance = this;
            OnLocalInstanceReady?.Invoke(this);

            _fpsController = GetComponent<FirstPersonController>();
            if (_fpsController != null)
            {
                _baseMoveSpeed   = _fpsController.MoveSpeed;
                _baseSprintSpeed = _fpsController.SprintSpeed;

                // Use FPS camera target as default pitch source if not assigned manually.
                if (lookPitchSource == null && _fpsController.CinemachineCameraTarget != null)
                {
                    lookPitchSource = _fpsController.CinemachineCameraTarget.transform;
                }
            }
            else
            {
                Debug.LogWarning("[PlayerInventory] ไม่พบ FirstPersonController — ระบบน้ำหนักจะไม่ทำงาน");
            }

            CacheHandAnchorOffsetFromLookSource();
        }

        public override void OnNetworkDespawn()
        {
            _netCurrentItemName.OnValueChanged -= OnHeldItemChanged;
            _netFlashlightEnabled.OnValueChanged -= OnFlashlightStateChanged;
            if (IsOwner && Instance == this)
                Instance = null;
        }

        private void OnHeldItemChanged(FixedString64Bytes oldName, FixedString64Bytes newName)
        {
            if (IsOwner) return;
            UpdateRemoteHandVisual(newName.ToString());
        }

        private void OnFlashlightStateChanged(bool oldValue, bool newValue)
        {
            ApplyCurrentHandFlashlightState(newValue);
        }

        private void UpdateRemoteHandVisual(string itemName)
        {
            if (_currentHandObject != null) Destroy(_currentHandObject);
            _currentHandLights = null;
            if (string.IsNullOrEmpty(itemName)) return;

            ItemData data = allGameItems.FirstOrDefault(i => i.itemName == itemName);
            if (data != null && data.handPrefab != null)
            {
                _currentHandObject = Instantiate(data.handPrefab, handAnchor);
                _currentHandObject.transform.localPosition = data.handItemLocalPosition;
                _currentHandObject.transform.localRotation = GetRemoteHandItemLocalRotation(data);
                _currentHandLights = _currentHandObject.GetComponentsInChildren<Light>(true);
                ApplyCurrentHandFlashlightState(_netFlashlightEnabled.Value);
            }
        }

        private Quaternion GetRemoteHandItemLocalRotation(ItemData itemData)
        {
            if (itemData != null && itemData.useHandAnchorRotationOverride)
                return Quaternion.Euler(itemData.handAnchorRotationEuler);

            return Quaternion.identity;
        }
        
        [ServerRpc]
        private void UpdateHeldItemServerRpc(string newItemName)
        {
            _netCurrentItemName.Value = newItemName;
        }

        private void Update()
        {
            if (!IsOwner)
                return;

            HandleScrollInput();
            HandleDropInput();
            HandleFlashlightToggleInput();
            HandleUseInput();
            ApplyWeightPenalty();
        }

        private void LateUpdate()
        {
            SyncHandAnchorWithLookPitch();
        }

        private void SyncHandAnchorWithLookPitch()
        {
            if (!IsOwner || !followCameraPitch || handAnchor == null || lookPitchSource == null)
                return;

            if (!_hasHandAnchorOffset)
                CacheHandAnchorOffsetFromLookSource();

            if (!_hasHandAnchorOffset)
                return;

            Vector3 worldPos = lookPitchSource.TransformPoint(_handAnchorLocalPosFromLookSource);
            Quaternion localRot = GetEquippedHandAnchorLocalRotation();
            Quaternion worldRot = lookPitchSource.rotation * localRot;
            handAnchor.SetPositionAndRotation(worldPos, worldRot);
        }

        private Quaternion GetEquippedHandAnchorLocalRotation()
        {
            ItemData equippedItem = _slots[_currentSlotIndex];
            if (equippedItem != null && equippedItem.useHandAnchorRotationOverride)
                return Quaternion.Euler(equippedItem.handAnchorRotationEuler);

            return _handAnchorLocalRotFromLookSource;
        }

        private void CacheHandAnchorOffsetFromLookSource()
        {
            if (handAnchor == null || lookPitchSource == null)
            {
                _hasHandAnchorOffset = false;
                return;
            }

            _handAnchorLocalPosFromLookSource = lookPitchSource.InverseTransformPoint(handAnchor.position);
            _handAnchorLocalRotFromLookSource = Quaternion.Inverse(lookPitchSource.rotation) * handAnchor.rotation;
            _hasHandAnchorOffset = true;
        }

        private void HandleUseInput()
        {
            bool usePressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                usePressed = Mouse.current.leftButton.wasPressedThisFrame;
#else
            usePressed = Input.GetMouseButtonDown(0);
#endif
            if (usePressed)
            {
                if (IsHoldingFlashlight()) return;
                UseCurrentItem();
            }
        }

        private void HandleFlashlightToggleInput()
        {
            bool togglePressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
                togglePressed = Keyboard.current.fKey.wasPressedThisFrame;
#else
            togglePressed = Input.GetKeyDown(KeyCode.F);
#endif

            if (!togglePressed || !IsHoldingFlashlight())
                return;

            bool nextState = !_appliedFlashlightState;
            ApplyCurrentHandFlashlightState(nextState);
            SetFlashlightStateServerRpc(nextState);
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
            Debug.Log("[PlayerInventory] Inventory is full.้");
            return false;
        }

        public void DropItem()
        {
            if (_slots[_currentSlotIndex] == null) return;

            ItemData droppedItem = _slots[_currentSlotIndex];
            bool droppedFlashlightState = IsItemFlashlight(droppedItem) && _netFlashlightEnabled.Value;

            if (droppedItem.worldPrefab != null)
            {
                RequestDropItemServerRpc(droppedItem.itemName, droppedFlashlightState);
            }

            _slots[_currentSlotIndex] = null;
            UpdateHandItem();
            OnInventoryChanged?.Invoke();
        }

        public void UseCurrentItem()
        {
            if (_slots[_currentSlotIndex] == null) return;

            // Use locally (Owner/Client Auth logic + Visuals)
            bool consumed = _slots[_currentSlotIndex].Use(gameObject);

            // Use on server (Server Auth logic)
            // If Host (IsServer + IsOwner), local Use already ran both logics.
            if (!IsServer)
            {
                UseItemServerRpc(_slots[_currentSlotIndex].itemName);
            }

            if (consumed)
            {
                _slots[_currentSlotIndex] = null;
                UpdateHandItem();
                OnInventoryChanged?.Invoke();
            }
        }

        [ServerRpc]
        private void UseItemServerRpc(string itemName)
        {
            // If Host, we already used it locally.
            if (IsOwner) return;

            ItemData item = allGameItems.FirstOrDefault(i => i.itemName == itemName);
            if (item != null)
            {
                item.Use(gameObject);
            }
        }

        [ServerRpc]
        private void RequestDropItemServerRpc(string itemName, bool droppedFlashlightState)
        {
             ItemData data = allGameItems.FirstOrDefault(i => i.itemName == itemName);
             if (data != null && data.worldPrefab != null)
             {
             Vector3 dropPos = GetGroundedDropPosition();
             Quaternion dropRotation = GetDropRotationForItem(data);
             GameObject spawnedItem = Instantiate(data.worldPrefab, dropPos, dropRotation);
             
             // Check if registered in NetworkManager (Host check)
             if (NetworkManager.Singleton != null && NetworkManager.Singleton.NetworkConfig != null)
             {
                 var collection = NetworkManager.Singleton.NetworkConfig.Prefabs;
                 // This check is a bit complex due to different ways to register, but simple list check helps
                 // We can check if the hash or prefab is in the list.
                 // For now, let's just warn if the list is empty or small.
                 if (collection.Prefabs.Count == 0)
                 {
                     Debug.LogError("[PlayerInventory] NetworkManager has NO registered prefabs! Dropped item will not spawn on clients.");
                 }
             }

             // Auto-fix for 2D sprites wanting to live in 3D world
             if (spawnedItem.GetComponent<SpriteRenderer>() != null)
             {
                 // Add Billboard if missing
                 if (spawnedItem.GetComponent<Billboard>() == null)
                 {
                     spawnedItem.AddComponent<Billboard>();
                 }

                 // Warn about collider
                 var col3D = spawnedItem.GetComponent<Collider>();
                 var col2D = spawnedItem.GetComponent<Collider2D>();
                 
                 if (col2D != null)
                 {
                      Debug.LogError($"[PlayerInventory] Item '{itemName}' has a 2D Collider! It will fall through the 3D ground. PLEASE REMOVE Collider2D and add a BoxCollider (3D) or SphereCollider (3D).");
                 }
                 else if (col3D == null)
                 {
                      Debug.LogWarning($"[PlayerInventory] Item '{itemName}' has NO Collider! It will fall through the ground.");
                 }
             }

             if (spawnedItem.TryGetComponent(out NetworkObject netObj))
             {
                 try
                 {
                     netObj.Spawn();
                     ApplyDroppedFlashlightStateClientRpc(netObj, droppedFlashlightState);
                     Debug.Log($"[PlayerInventory] Successfully spawned '{itemName}' at {dropPos}");
                 }
                 catch (System.Exception e)
                 {
                     Debug.LogError($"[PlayerInventory] FAILED to spawn '{itemName}'. Is the prefab registered in NetworkManager? Error: {e.Message}");
                     Destroy(spawnedItem); // Cleanup ghost
                 }
             }
             }
        }

        [ClientRpc]
        private void ApplyDroppedFlashlightStateClientRpc(NetworkObjectReference droppedItemRef, bool isOn)
        {
            if (!droppedItemRef.TryGet(out NetworkObject droppedItemObject) || droppedItemObject == null)
                return;

            droppedItemObject.gameObject.SendMessage("SetOn", isOn, SendMessageOptions.DontRequireReceiver);
        }

        private Quaternion GetDropRotationForItem(ItemData itemData)
        {
            if (itemData != null && itemData.useHandAnchorRotationOverride)
                return Quaternion.Euler(itemData.handAnchorRotationEuler);

            return Quaternion.identity;
        }

        private Vector3 GetGroundedDropPosition()
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = transform.forward;
            }
            forward.Normalize();

            Vector3 probeOrigin = transform.position + forward * dropForwardDistance + Vector3.up * dropRaycastStartHeight;

            if (Physics.Raycast(probeOrigin, Vector3.down, out RaycastHit hit, dropRaycastDistance, dropGroundMask, QueryTriggerInteraction.Ignore))
            {
                return hit.point + Vector3.up * dropGroundOffset;
            }

            // Fallback: keep item near feet so it does not spawn too high if no ground was hit.
            return transform.position + forward * dropForwardDistance + Vector3.up * dropGroundOffset;
        }

        private void UpdateHandItem()
        {
            if (_currentHandObject != null)
                Destroy(_currentHandObject);
            _currentHandLights = null;

            ItemData currentItem = _slots[_currentSlotIndex];
            string itemName = "";
            
            if (currentItem != null)
            {
                itemName = currentItem.itemName;
                if (currentItem.handPrefab != null && handAnchor != null)
                {
                    _currentHandObject = Instantiate(currentItem.handPrefab, handAnchor);
                    _currentHandObject.transform.localPosition = currentItem.handItemLocalPosition;
                    _currentHandObject.transform.localRotation = Quaternion.identity;
                    _currentHandLights = _currentHandObject.GetComponentsInChildren<Light>(true);
                }
            }

            if (!IsHoldingFlashlight())
            {
                _appliedFlashlightState = _netFlashlightEnabled.Value;
            }
            else
            {
                ApplyCurrentHandFlashlightState(_netFlashlightEnabled.Value);
            }
            
            if (IsOwner)
            {
                UpdateHeldItemServerRpc(itemName);
            }
        }

        private bool IsHoldingFlashlight()
        {
            return IsItemFlashlight(_slots[_currentSlotIndex]);
        }

        private bool IsItemFlashlight(ItemData item)
        {
            return item != null && flashlightItemData != null && item == flashlightItemData;
        }

        private void ApplyCurrentHandFlashlightState(bool isOn)
        {
            _appliedFlashlightState = isOn;

            if (_currentHandObject != null)
            {
                _currentHandObject.SendMessage("SetOn", isOn, SendMessageOptions.DontRequireReceiver);
            }

            if (_currentHandLights == null)
                return;

            foreach (var lightComp in _currentHandLights)
            {
                if (lightComp != null)
                    lightComp.enabled = isOn;
            }
        }

        [ServerRpc]
        private void SetFlashlightStateServerRpc(bool isOn)
        {
            _netFlashlightEnabled.Value = isOn;
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (allGameItems == null || allGameItems.Count == 0)
            {
                var guids = UnityEditor.AssetDatabase.FindAssets("t:ItemData");
                allGameItems = new List<ItemData>();
                foreach (var guid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(path);
                    if (asset != null) allGameItems.Add(asset);
                }
            }
        }
#endif

        private void ApplyWeightPenalty()
        {
            if (_fpsController == null) return;
            float penalty = TotalWeight * weightMultiplier;
            _fpsController.MoveSpeed   = Mathf.Max(minMoveSpeed, _baseMoveSpeed   - penalty);
            _fpsController.SprintSpeed = Mathf.Max(minMoveSpeed, _baseSprintSpeed - penalty);
        }
    }
}
