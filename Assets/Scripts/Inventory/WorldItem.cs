using NaughtyAttributes;
using DG.Tweening;
using Unity.Netcode;
using UnityEngine;

namespace Inventory
{
    [RequireComponent(typeof(NetworkObject))]
    public class WorldItem : NetworkBehaviour, IInteractable
    {
        [BoxGroup("Item Data")]
        public ItemData itemData;

        [BoxGroup("Visual Feedback")]
        [SerializeField] private GameObject highlightEffect;

        [BoxGroup("Visual Feedback")]
        [SerializeField] private float scanPulseScale = 1.2f;

        [BoxGroup("Visual Feedback")]
        [SerializeField] private float scanPulseDuration = 0.25f;

        [BoxGroup("Bob & Rotate Animation")]
        [SerializeField] private bool enableBobAnimation = true;

        [BoxGroup("Bob & Rotate Animation")]
        [SerializeField] private float bobHeight = 0.15f;

        [BoxGroup("Bob & Rotate Animation")]
        [SerializeField] private float bobSpeed = 1.5f;

        [BoxGroup("Bob & Rotate Animation")]
        [SerializeField] private float rotateSpeed = 90f;

        private Vector3 _startPosition;
        private float _bobTimer;
        private bool _lookHighlightActive;
        private bool _scanHighlightActive;
        private Vector3 _highlightDefaultScale = Vector3.one;
        private Tween _scanPulseTween;
        private Tween _scanTimerTween;

        public string PromptText
        {
            get
            {
                if (itemData == null) return "Press E to interact";
                return $"Press E to collect {itemData.itemName}";
            }
        }

        public override void OnNetworkSpawn()
        {
            if (highlightEffect != null)
            {
                _highlightDefaultScale = highlightEffect.transform.localScale;
                highlightEffect.SetActive(false);
            }
        }

        private void Start()
        {
            _startPosition = transform.position;
            if (highlightEffect != null)
                _highlightDefaultScale = highlightEffect.transform.localScale;
        }

        private void Update()
        {
            if (!enableBobAnimation) return;

            _bobTimer += Time.deltaTime * bobSpeed;
            float newY = _startPosition.y + Mathf.Sin(_bobTimer) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);

            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        }

        public void Interact()
        {
            Interact(PlayerInventory.Instance != null ? PlayerInventory.Instance.gameObject : null);
        }

        public void Interact(GameObject interactor)
        {
            if (itemData == null) return;

            PlayerInventory inventory = interactor != null
                ? interactor.GetComponent<PlayerInventory>()
                : PlayerInventory.Instance;

            if (inventory != null)
            {
               bool pickedUp = inventory.PickUpItem(itemData);
               if (pickedUp)
               {
                   RequestDespawnServerRpc();
               }
            }
        }

        public void SellAndDespawn()
        {
            if (TryGetComponent(out NetworkObject netObj) && netObj.IsSpawned)
            {
                RequestDespawnServerRpc();
                return;
            }

            Destroy(gameObject);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestDespawnServerRpc()
        {
            // Server validates and despawns
            GetComponent<NetworkObject>().Despawn();
        }
        
        public void SetHighlight(bool active)
        {
            // WorldItem highlight is scan-only; ignore look-at highlight requests.
            _lookHighlightActive = false;
            RefreshHighlightState();
        }

        public void SetScanHighlightForSeconds(float seconds)
        {
            if (seconds <= 0f)
            {
                _scanHighlightActive = false;
                StopScanPulse();
                RefreshHighlightState();
                return;
            }

            _scanHighlightActive = true;
            RefreshHighlightState();
            StartScanPulse();

            _scanTimerTween?.Kill();
            _scanTimerTween = DOVirtual.DelayedCall(seconds, () =>
            {
                _scanHighlightActive = false;
                StopScanPulse();
                RefreshHighlightState();
            }).SetTarget(this);
        }

        private void RefreshHighlightState()
        {
            if (highlightEffect == null) return;

            bool shouldShow = _lookHighlightActive || _scanHighlightActive;
            highlightEffect.SetActive(shouldShow);
        }

        private void StartScanPulse()
        {
            if (highlightEffect == null) return;

            StopScanPulse();
            Transform effectTransform = highlightEffect.transform;
            effectTransform.localScale = _highlightDefaultScale;

            _scanPulseTween = effectTransform
                .DOScale(_highlightDefaultScale * scanPulseScale, scanPulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetTarget(this);
        }

        private void StopScanPulse()
        {
            if (_scanPulseTween != null)
            {
                _scanPulseTween.Kill();
                _scanPulseTween = null;
            }

            if (highlightEffect != null)
                highlightEffect.transform.localScale = _highlightDefaultScale;
        }

        private void OnDisable()
        {
            _scanPulseTween?.Kill();
            _scanTimerTween?.Kill();
            _scanPulseTween = null;
            _scanTimerTween = null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.3f);

#if UNITY_EDITOR
            if (itemData != null)
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 0.5f,
                    $"{itemData.itemName}\n{itemData.weight}kg | ${itemData.scrapValue}"
                );
#endif
        }
    }
}
