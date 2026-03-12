using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Inventory
{
    public class InteractionSystem : MonoBehaviour
    {
        [BoxGroup("Interaction Settings")]
        [SerializeField] private float interactionRange = 3f;

        [BoxGroup("Interaction Settings")]
        [Required("Insert MainCamera to this")]
        [SerializeField] private Camera playerCamera;

        [BoxGroup("Interaction Settings")]
        [SerializeField] private LayerMask interactableLayer = ~0;

        [BoxGroup("UI Prompt")]
        [SerializeField] private GameObject interactPromptUI;

        [BoxGroup("UI Prompt")]
        [SerializeField] private Text promptText;

        [BoxGroup("UI Prompt")]
        [SerializeField] private string promptPrefix = "Press E to collect";

        private WorldItem _lookingAt;

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


        private void CheckForInteractable()
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            WorldItem found = null;

            if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactableLayer))
                found = hit.collider.GetComponent<WorldItem>();

            if (found != _lookingAt)
            {
                if (_lookingAt != null)
                    _lookingAt.SetHighlight(false);

                _lookingAt = found;

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

        private void OnDrawGizmosSelected()
        {
            if (playerCamera == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(playerCamera.transform.position,
                           playerCamera.transform.forward * interactionRange);
        }
    }
}
