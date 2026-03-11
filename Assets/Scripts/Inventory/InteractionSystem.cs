using UnityEngine;
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
        public LayerMask interactableLayer = ~0; // default = ทุก Layer

        [Header("UI Prompt (Optional)")]
        [Tooltip("GameObject ที่แสดงข้อความ 'Press E' — ถ้าไม่มีก็ไม่ต้องใส่")]
        public GameObject interactPromptUI;

        // ─── Private Fields ───────────────────────────────────────────────────
        private WorldItem _lookingAt; // ไอเทมที่กำลังมองอยู่

        // ─── Unity Lifecycle ──────────────────────────────────────────────────
        private void Start()
        {
            // หา Camera อัตโนมัติถ้าไม่ได้ Assign
            if (playerCamera == null)
                playerCamera = Camera.main;

            if (interactPromptUI != null)
                interactPromptUI.SetActive(false);
        }

        private void Update()
        {
            CheckForInteractable();
            HandleInteractInput();
        }

        // ─── Interaction Logic ────────────────────────────────────────────────

        /// <summary>
        /// ยิง Raycast จาก Camera — ถ้าเจอ WorldItem แสดง Prompt
        /// </summary>
        private void CheckForInteractable()
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            WorldItem found = null;

            if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactableLayer))
            {
                found = hit.collider.GetComponent<WorldItem>();
            }

            // อัปเดต Prompt UI
            if (found != _lookingAt)
            {
                _lookingAt = found;
                if (interactPromptUI != null)
                    interactPromptUI.SetActive(_lookingAt != null);
            }
        }

        /// <summary>
        /// ตรวจสอบการกด E แล้วสั่ง Interact กับ WorldItem ที่มองอยู่
        /// </summary>
        private void HandleInteractInput()
        {
            bool interactPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
                interactPressed = Keyboard.current.eKey.wasPressedThisFrame;
#else
            interactPressed = Input.GetKeyDown(KeyCode.E);
#endif

            if (interactPressed && _lookingAt != null)
                _lookingAt.Interact();
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

