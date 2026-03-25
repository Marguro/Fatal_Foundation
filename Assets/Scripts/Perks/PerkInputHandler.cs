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

            // Method 2: Touch input (Tap detection) via the new Input System
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var primaryTouch = touchscreen.primaryTouch;

                if (primaryTouch.press.wasPressedThisFrame)
                {
                    _lastTapTime = Time.time;
                    _isTapping = true;
                }

                if (primaryTouch.press.wasReleasedThisFrame && _isTapping)
                {
                    float tapTime = Time.time - _lastTapTime;
                    if (tapTime < this.tapDuration)
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

