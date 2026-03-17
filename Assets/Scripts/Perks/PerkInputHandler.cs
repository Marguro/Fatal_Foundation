using UnityEngine;
using UnityEngine.InputSystem;

namespace Perks
{
    /// <summary>
    /// Handles player input for opening the Perk menu (Tap/Button press)
    /// </summary>
    public class PerkInputHandler : MonoBehaviour
    {
        [SerializeField] private Key perkMenuKey = Key.P;
        [SerializeField] private float tapDuration = 0.3f;

        private float _lastTapTime = 0f;
        private bool _isTapping = false;

        private void Update()
        {
            // Method 1: Keyboard input
            if (Keyboard.current != null && Keyboard.current[perkMenuKey].wasPressedThisFrame)
            {
                OnPerkMenuRequested();
            }

            // Method 2: Touch input (Tap detection)
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == UnityEngine.TouchPhase.Began)
                {
                    _lastTapTime = Time.time;
                    _isTapping = true;
                }

                if (touch.phase == UnityEngine.TouchPhase.Ended && _isTapping)
                {
                    float tapDuration = Time.time - _lastTapTime;
                    if (tapDuration < this.tapDuration)
                    {
                        // Short tap detected
                        OnPerkMenuRequested();
                    }
                    _isTapping = false;
                }
            }
        }

        private void OnPerkMenuRequested()
        {
            if (PerkUIManager.Instance != null)
            {
                PerkUIManager.Instance.OpenPerkMenu();
                Debug.Log("[PerkInputHandler] Perk menu opened");
            }
        }
    }
}

