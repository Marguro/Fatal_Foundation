using NaughtyAttributes;
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
                highlightEffect.SetActive(false);
        }

        private void Start()
        {
            _startPosition = transform.position;
            // Highlight disabled in OnNetworkSpawn or here is fine
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
            if (highlightEffect != null)
                highlightEffect.SetActive(active);
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
