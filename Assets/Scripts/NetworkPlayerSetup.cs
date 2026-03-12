using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets.FirstPersonController.Scripts;
using StarterAssets.InputSystem;
using Inventory;

/// <summary>
/// แนบสคริปต์นี้ลง PlayerCapsule prefab
/// เมื่อ NetworkObject spawn แล้ว จะปิด component ที่ควรทำงานเฉพาะเครื่องเจ้าของ (Local Player)
/// ป้องกัน Client ทุกคนเห็นกล้องและรับ input ของ remote player
/// </summary>
[DisallowMultipleComponent]
public class NetworkPlayerSetup : NetworkBehaviour
{
        [Header("Local-Only Components (auto-filled in Reset)")]
        [SerializeField] private FirstPersonController firstPersonController;
        [SerializeField] private StarterAssetsInputs starterAssetsInputs;
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private InteractionSystem interactionSystem;

        [Header("Camera (disabled on remote players)")]
        [Tooltip("Drag 'PlayerFollowCamera' child GameObject here")]
        [SerializeField] private GameObject playerFollowCamera;

        // ถูกเรียกอัตโนมัติเมื่อ Add Component ใน Editor — เติม reference ให้เองทันที
        private void Reset()
        {
            AutoFillReferences();
        }

        // เติม reference อัตโนมัติจาก component บน GameObject เดียวกัน
        private void AutoFillReferences()
        {
            firstPersonController = GetComponent<FirstPersonController>();
            starterAssetsInputs   = GetComponent<StarterAssetsInputs>();
            playerInput           = GetComponent<PlayerInput>();
            playerInventory       = GetComponent<PlayerInventory>();
            interactionSystem     = GetComponent<InteractionSystem>();

            // หา PlayerFollowCamera ใน children
            foreach (Transform child in transform)
            {
                if (child.name == "PlayerFollowCamera")
                {
                    playerFollowCamera = child.gameObject;
                    break;
                }
            }
        }

        // NGO เรียก OnNetworkSpawn หลัง NetworkObject ถูก spawn ทั้งบน Host และ Client
        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                // ผู้เล่นเครื่องตัวเอง — ทุก component ทำงานปกติ ไม่ต้องทำอะไรเพิ่ม
                return;
            }

            // ── Remote Player (ไม่ใช่เจ้าของ) ──
            // ปิด Input & Movement
            if (firstPersonController != null) firstPersonController.enabled = false;
            if (starterAssetsInputs   != null) starterAssetsInputs.enabled   = false;
            if (playerInput           != null) playerInput.enabled            = false;

            // ปิด Inventory & Interaction (ทำงานเฉพาะเครื่องเจ้าของเท่านั้น)
            if (playerInventory   != null) playerInventory.enabled   = false;
            if (interactionSystem != null) interactionSystem.enabled = false;

            // ปิด Cinemachine Follow Camera ของ remote player
            // ถ้าเปิดทิ้งไว้ CinemachineBrain บนกล้องหลักจะยึดกล้องของ remote player แทน
            if (playerFollowCamera != null) playerFollowCamera.SetActive(false);
        }
}



