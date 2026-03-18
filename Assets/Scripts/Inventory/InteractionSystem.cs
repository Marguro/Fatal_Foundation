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
        [SerializeField] private string defaultPrompt = "Press E to interact";

        private IInteractable _lookingAt;

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
            IInteractable found = null;

            if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactableLayer, QueryTriggerInteraction.Collide))
            {
                found = hit.collider.GetComponent<IInteractable>()
                        ?? hit.collider.GetComponentInParent<IInteractable>()
                        ?? hit.collider.GetComponentInChildren<IInteractable>();
            }

            if (found != _lookingAt)
            {
                if (_lookingAt != null)
                    _lookingAt.SetHighlight(false);

                _lookingAt = found;

                if (_lookingAt != null)
                {
                    _lookingAt.SetHighlight(true);
                    UpdatePromptText(_lookingAt.PromptText);
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
                _lookingAt.Interact(gameObject);
        }


        private void UpdatePromptText(string prompt)
        {
            if (promptText == null) return;
            promptText.text = string.IsNullOrWhiteSpace(prompt) ? defaultPrompt : prompt;
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
