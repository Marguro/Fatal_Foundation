using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Inventory
{
    public class GroundItemScanner : MonoBehaviour
    {
        [BoxGroup("Scan Settings")]
        [SerializeField] private float scanRadius = 8f;

        [BoxGroup("Scan Settings")]
        [SerializeField] private float scanCooldown = 1.25f;

        [BoxGroup("Scan Settings")]
        [SerializeField] private float highlightDuration = 2f;

        [BoxGroup("Scan Settings")]
        [SerializeField] private LayerMask itemLayerMask = ~0;

        [BoxGroup("Scan Settings")]
        [SerializeField, Min(8)] private int maxColliders = 64;

        private Collider[] _scanResults;
        private readonly HashSet<WorldItem> _uniqueScanItems = new HashSet<WorldItem>();
        private float _nextAllowedScanTime;

        private void Awake()
        {
            _scanResults = new Collider[maxColliders];
        }

        private void Update()
        {
            if (!WasScanPressedThisFrame()) return;
            if (Time.time < _nextAllowedScanTime) return;

            RunScan();
            _nextAllowedScanTime = Time.time + scanCooldown;
        }

        private bool WasScanPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.V);
#endif
        }

        private void RunScan()
        {
            if (_scanResults == null || _scanResults.Length != maxColliders)
                _scanResults = new Collider[maxColliders];

            _uniqueScanItems.Clear();

            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                scanRadius,
                _scanResults,
                itemLayerMask,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < hitCount; i++)
            {
                Collider target = _scanResults[i];
                if (target == null) continue;

                WorldItem worldItem = target.GetComponent<WorldItem>()
                                      ?? target.GetComponentInParent<WorldItem>()
                                      ?? target.GetComponentInChildren<WorldItem>();

                if (worldItem == null) continue;
                if (_uniqueScanItems.Add(worldItem))
                    worldItem.SetScanHighlightForSeconds(highlightDuration);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, scanRadius);
        }
    }
}


