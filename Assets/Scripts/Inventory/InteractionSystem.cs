using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FatalFoundation
{
    /// <summary>
    /// ระบบ Interaction — กด E เพื่อเก็บไอเทมในโลก
    /// ติดกับ Player GameObject
    /// </summary>
    public class InteractionSystem : MonoBehaviour
    {
        // ─── Inspector Fields ─────────────────────────────────────────────────
        [Header("Interaction Settings")]
        [Tooltip("ระยะ Raycast สำหรับตรวจจับไอเทม (เมตร)")]
        public float interactionRange = 3f;

        [Tooltip("Camera ของ Player — ถ้าไม่ได้ลากมาจะหา MainCamera อัตโนมัติ")]
        public Camera playerCamera;

        [Tooltip("LayerMask สำหรับ Interactable Objects (แนะนำสร้าง Layer ชื่อ 'Interactable')")]
        public LayerMask interactableLayer = ~0;

        [Header("UI Prompt")]
        [Tooltip("GameObject ที่มี Text แสดง 'Press E to pick up ...' — ถ้าไม่ใส่ก็ไม่มี prompt")]
        public GameObject interactPromptUI;

        [Tooltip("Text component สำหรับแสดงชื่อไอเทม (ลากตัว Text ใน Prompt มาใส่)")]
        public Text promptText;

        [Tooltip("ข้อความ prefix ก่อนชื่อไอเทม เช่น 'กด E เพื่อเก็บ '")]
        public string promptPrefix = "กด E เพื่อเก็บ ";

        // ─── Private Fields ───────────────────────────────────────────────────
        private WorldItem _lookingAt;

        // ─── Unity Lifecycle ──────────────────────────────────────────────────
        private void Start()
        {
            if (playerCamera == null)
                playerCamera = Camera.main;

            SetPromptVisible(false);
        }

        private void Update()
        {
            CheckForInteractable();
            HandleInteractInput();
        }

        // ─── Interaction Logic ────────────────────────────────────────────────

        private void CheckForInteractable()
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            WorldItem found = null;

            if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactableLayer))
                found = hit.collider.GetComponent<WorldItem>();

            // มีการเปลี่ยนแปลงไอเทมที่มองอยู่
            if (found != _lookingAt)
            {
                // ปิด highlight ไอเทมเดิม
                if (_lookingAt != null)
                    _lookingAt.SetHighlight(false);

                _lookingAt = found;

                // เปิด highlight ไอเทมใหม่ + แสดง prompt
                if (_lookingAt != null)
                {
                    _lookingAt.SetHighlight(true);
                    UpdatePromptText(_lookingAt.itemData);
                    SetPromptVisible(true);
                }
                else
                {
                    SetPromptVisible(false);
                }
            }
        }

        private void HandleInteractInput()
        {
            if (_lookingAt == null) return;

            bool interactPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
                interactPressed = Keyboard.current.eKey.wasPressedThisFrame;
#else
            interactPressed = Input.GetKeyDown(KeyCode.E);
#endif
            if (interactPressed)
                _lookingAt.Interact();
        }

        // ─── UI Helpers ───────────────────────────────────────────────────────

        private void UpdatePromptText(ItemData data)
        {
            if (promptText == null) return;

            if (data != null)
                promptText.text = $"{promptPrefix}<b>{data.itemName}</b>" +
                                  (data.weight > 0f ? $"  ({data.weight}kg)" : "");
            else
                promptText.text = promptPrefix;
        }

        private void SetPromptVisible(bool visible)
        {
            if (interactPromptUI != null)
                interactPromptUI.SetActive(visible);
        }

        // ─── Gizmos (Debug) ───────────────────────────────────────────────────
        private void OnDrawGizmosSelected()
        {
            if (playerCamera == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(playerCamera.transform.position,
                           playerCamera.transform.forward * interactionRange);
        }
    }
}
